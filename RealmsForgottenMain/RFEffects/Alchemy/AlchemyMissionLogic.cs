using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Mission;

namespace RealmsForgotten.RFEffects.Alchemy
{
    public class AlchemyMissionLogic : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        private record ActiveBomb
        {
            public ActiveBomb(GameEntity entity, ParticleSystem particle, IAlchemicalBomb bomb, Vec3 position)
            {
                Entity = entity;
                Particle = particle;
                Bomb = bomb;
                Position = position;
                InsideAgents = new HashSet<Agent>();
                Elapsed = 0f;
            }

            public GameEntity Entity { get; set; }
            public ParticleSystem Particle { get; set; }
            public IAlchemicalBomb Bomb { get; set; }
            public Vec3 Position { get; set; }
            public float Elapsed { get; set; }
            public HashSet<Agent> InsideAgents { get; } = new();
            public HashSet<Missile> InsideProjectiles { get; } = new();
        }

        private readonly List<ActiveBomb> _activeBombs = new();
        private float _areaCheckAccumulator = 0f;
        private const float AreaCheckInterval = 0.5f;
        private readonly SpatialGrid<Agent> _agentGrid = new(10f); 
        private readonly SpatialGrid<Missile> _projectileGrid = new(10f);
        private void BuildGrids()
        {
            _agentGrid.Clear();
            _projectileGrid.Clear();

            foreach (var agent in Current.AllAgents)
            {
                if (agent.IsMount)
                    continue;

                _agentGrid.Add(agent.Position.AsVec2, agent);
            }

            foreach (var missile in Current.MissilesList)
            {
                _projectileGrid.Add(missile.GetPosition().AsVec2, missile);
            }
        }
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            BuildGrids();
            if (_activeBombs.Count == 0)
                return;
            _areaCheckAccumulator += dt;
            //if (!(_areaCheckAccumulator >= AreaCheckInterval))
            //    return;

            for (int i = _activeBombs.Count - 1; i >= 0; i--)
            {
                var bomb = _activeBombs[i];
                bomb.Elapsed += _areaCheckAccumulator;
                if (bomb.Elapsed >= bomb.Bomb.Duration)
                {
                    RemoveExpiredBomb(bomb);
                }
                else
                {
                    CheckAgentsInBombAreas(bomb);
                    CheckProjectilesInBombAreas(bomb);
                }
            }
            _areaCheckAccumulator = 0f;
        }
        private readonly List<Missile> _missilesToRemove = new();
        private void CheckProjectilesInBombAreas(ActiveBomb bomb)
        {
            float radius = bomb.Bomb.Size;
            float radiusSq = radius * radius;

            var nearbyMissiles = _projectileGrid.QueryCircle(bomb.Position.AsVec2, radius);

            HashSet<Missile> currentlyInside = new();

            foreach (var missile in nearbyMissiles)
            {
                var distSq = missile.GetPosition().AsVec2.DistanceSquared(bomb.Position.AsVec2);

                if (distSq > radiusSq)
                    continue;

                currentlyInside.Add(missile);

                if (!bomb.InsideProjectiles.Contains(missile))
                {
                    bomb.InsideProjectiles.Add(missile);
                    bomb.Bomb.OnProjectileEntered(missile);
                }
            }
            _missilesToRemove.Clear();

            foreach (var missile in bomb.InsideProjectiles)
                if (!currentlyInside.Contains(missile))
                    _missilesToRemove.Add(missile);

            foreach (var missile in _missilesToRemove)
            {
                bomb.InsideProjectiles.Remove(missile);
                bomb.Bomb.OnProjectileLeft(missile);
            }
        }
        private readonly List<Agent> _agentsToRemove = new();
        private void CheckAgentsInBombAreas(ActiveBomb bomb)
        {
            float radius = bomb.Bomb.Size;
            float radiusSq = radius * radius;

            var nearbyAgents = _agentGrid.QueryCircle(bomb.Position.AsVec2, radius);

            HashSet<Agent> currentlyInside = new();
            foreach (var agent in nearbyAgents)
            {
                if (agent.IsMount) continue;
                var inside = bomb.Position.Distance(agent.Position) <= bomb.Bomb.Size;
                if (inside)
                {
                    currentlyInside.Add(agent);
                    if (!bomb.InsideAgents.Contains(agent))
                    {
                        bomb.InsideAgents.Add(agent);
                        bomb.Bomb.OnEntered(agent);
                    }
                }
            }
            _agentsToRemove.Clear();

            foreach (var agent in bomb.InsideAgents)
                if (!currentlyInside.Contains(agent))
                    _agentsToRemove.Add(agent);

            foreach (var agent in _agentsToRemove)
            {
                bomb.InsideAgents.Remove(agent);
                bomb.Bomb.OnLeft(agent);
            }
        }
        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            //Random random = new();
            //var xDiff = random.NextFloat();
            //var xVec = velocity.X + ((xDiff / 5) - 0.1f);
            //var yDiff = random.NextFloat();
            //var yVec = velocity.Y + ((yDiff / 5) - 0.1f);
            
            //var test = Agent.Main.Velocity;
            //var missle = Current.MissilesList.Last();
            //int a = 5;
            ////missle.Entity.GetLocalFrame;
            //missle.SetVelocity(new(xVec, yVec, velocity.z));
        }
        private void RemoveExpiredBomb(ActiveBomb active)
        {
            active.Entity.RemoveAllParticleSystems();
            foreach (var a in active.InsideAgents)
                if (a != null && a.IsActive())
                    active.Bomb.OnLeft(a);
            _activeBombs.Remove(active);
        }
        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            Missile? missle = null;
            foreach (var m in Current.MissilesList)
            {
                if (m.Index == collisionData.AffectorWeaponSlotOrMissileIndex)
                {
                    missle = m;
                    break;
                }
            }
            if (missle == null) return;
            var bomb = AlchemicalBombFactory.GetBombType(missle.Weapon.Item);
            if (bomb == null) return;

            if (ParticleSystemManager.GetRuntimeIdByName(bomb.ParticleId) == -1)
                InformationManager.DisplayMessage(new InformationMessage("Error, Particle with id: " + bomb.ParticleId + "not found", new Color(1, 0, 0)));

            MatrixFrame localFrame = new(Mat3.Identity, new(0, 0, 0));
            GameEntity childEntity = GameEntity.CreateEmpty(Current.Scene);
            childEntity.SetGlobalFrame(missle.Entity.GetGlobalFrame());
            ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(bomb.ParticleId, childEntity, ref localFrame);

            var position = childEntity.GlobalPosition;
            var newBomb = new ActiveBomb(childEntity, particle, bomb, position);
            _activeBombs.Add(newBomb);
            CheckAgentsInBombAreas(newBomb);
        }
    }
}