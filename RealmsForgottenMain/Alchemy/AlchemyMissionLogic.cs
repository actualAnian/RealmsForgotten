using RealmsForgotten.Alchemy.Bombs;
using RealmsForgotten.Alchemy.OnHitEffects;
using RealmsForgotten.UI.FloatingText;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Mission;

namespace RealmsForgotten.Alchemy
{
    internal record ActiveBomb
    {
        public ActiveBomb(GameEntity entity, ParticleSystem particle, AbstractBomb bomb)
        {
            Entity = entity;
            Particle = particle;
            Bomb = bomb;
            Elapsed = 0f;
        }
        public int FloatingTextId { get; set; } 

        public GameEntity Entity { get; set; }
        public ParticleSystem Particle { get; set; }
        public AbstractBomb Bomb { get; set; }
        public Vec3 Position { get; set; }
        public float Elapsed { get; set; }
    }

    public class AlchemyMissionLogic : MissionBehavior
    {
        public static AlchemyMissionLogic? Instance 
        {
            get
            {
                var logic = Current?.GetMissionBehavior<AlchemyMissionLogic>();
                if (logic == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Error, AlchemyMissionLogic instance not found!", new Color(1, 0, 0)));
                    return null;
                }
                return logic;
            }
        }
        public void AddMissileEffect(Missile missile, IOnHitEffect effect)
        {
            if (_misslesWithEffects.TryGetValue(missile, out var effects))
                effects.Add(effect);
            else
                _misslesWithEffects[missile] = new List<IOnHitEffect> { effect };
        }
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        readonly Dictionary<Missile, List<IOnHitEffect>> _misslesWithEffects = new();
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
                _projectileGrid.Add(missile.GetPosition().AsVec2, missile);
        }
        bool _setNewPlayerBombType = false;
        public override void OnMissionTick(float dt)
        {
            if (_setNewPlayerBombType)
            {
                PlayerBombManager.Instance.SetBombToNewType();
                _setNewPlayerBombType = false;
            }
            BuildGrids();
            if (_activeBombs.Count == 0)
                return;
            _areaCheckAccumulator += dt;
            //if (!(_areaCheckAccumulator >= AreaCheckInterval))
            //    return;

            for (int i = _activeBombs.Count - 1; i >= 0; i--)
            {
                var bomb = _activeBombs[i];
                bomb.Bomb.OnTick(dt);
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
            var nearbyMissiles = _projectileGrid.QueryBox(bomb.Bomb.Bounds);
            HashSet<Missile> currentlyInside = new();
            foreach (var missile in nearbyMissiles)
            {
                if (!bomb.Bomb.Contains(missile.GetPosition()))
                    continue;

                currentlyInside.Add(missile);
                if (!bomb.Bomb.InsideProjectiles.Contains(missile))
                {
                    bomb.Bomb.InsideProjectiles.Add(missile);
                    bomb.Bomb.OnProjectileEntered(missile);
                }
            }
            _missilesToRemove.Clear();

            foreach (var missile in bomb.Bomb.InsideProjectiles)
                if (!currentlyInside.Contains(missile))
                    _missilesToRemove.Add(missile);

            foreach (var missile in _missilesToRemove)
            {
                bomb.Bomb.InsideProjectiles.Remove(missile);
                bomb.Bomb.OnProjectileLeft(missile);
            }
        }
        private readonly List<Agent> _agentsToRemove = new();
        private void CheckAgentsInBombAreas(ActiveBomb bomb)
        {
            var nearbyAgents = _agentGrid.QueryBox(bomb.Bomb.Bounds);
            HashSet<Agent> currentlyInside = new();
            foreach (var agent in nearbyAgents)
            {
                //if (agent.IsMount) continue;
                if (!bomb.Bomb.Contains(agent.Position))
                    continue;
                currentlyInside.Add(agent);
                if (!bomb.Bomb.InsideAgents.Contains(agent))
                {
                    bomb.Bomb.InsideAgents.Add(agent);
                    bomb.Bomb.OnAgentEntered(agent);
                }
            }
            _agentsToRemove.Clear();

            foreach (var agent in bomb.Bomb.InsideAgents)
                if (!currentlyInside.Contains(agent))
                    _agentsToRemove.Add(agent);

            foreach (var agent in _agentsToRemove)
            {
                bomb.Bomb.InsideAgents.Remove(agent);
                bomb.Bomb.OnLeft(agent);
            }
        }
        private void RemoveExpiredBomb(ActiveBomb active)
        {
            RemoveTextFromFloatingTextManager(active);
            active.Entity.RemoveAllParticleSystems();
            foreach (var a in active.Bomb.InsideAgents)
                if (a != null && a.IsActive())
                    active.Bomb.OnLeft(a);
            _activeBombs.Remove(active);
        }
        private void ApplyMissleEffects(Missile missile, Agent victim)
        {
            if (_misslesWithEffects.TryGetValue(missile, out List<IOnHitEffect> effects) == false) return;
            effects.ForEach(e => e.OnHit(victim));
        }
        private void CheckForInteractions(AbstractBomb newBomb)
        {
            foreach(var existingBomb in _activeBombs)
            {
                if (existingBomb.Bomb.Contains(newBomb.Center))
                {
                    existingBomb.Bomb.OnInteraction(newBomb);
                    newBomb.OnInteraction(existingBomb.Bomb);
                }
            }
        }
        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            Missile? missile = null;
            foreach (var m in Current.MissilesList)
            {
                if (m.Index == collisionData.AffectorWeaponSlotOrMissileIndex)
                {
                    missile = m;
                    break;
                }
            }
            if (missile == null) return;
            ApplyMissleEffects(missile, victim);
            var bomb = AlchemicalBombFactory.GetBombType(attacker, missile.Entity.GetGlobalFrame().origin, missile.Weapon.Item);
            if (bomb == null) return;

            if (ParticleSystemManager.GetRuntimeIdByName(bomb.ParticleId) == -1)
                InformationManager.DisplayMessage(new InformationMessage("Error, Particle with id: " + bomb.ParticleId + "not found", new Color(1, 0, 0)));

            MatrixFrame localFrame = new(Mat3.Identity, new(0, 0, 0));
            GameEntity childEntity = GameEntity.CreateEmpty(Current.Scene);
            childEntity.SetGlobalFrame(missile.Entity.GetGlobalFrame());
            ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(bomb.ParticleId, childEntity, ref localFrame);

            var position = childEntity.GlobalPosition;
            var newBomb = new ActiveBomb(childEntity, particle, bomb);
            _activeBombs.Add(newBomb);
            AddTextToFloatingTextManager(newBomb);
            CheckAgentsInBombAreas(newBomb);
            CheckForInteractions(newBomb.Bomb);
        }
        private void AddTextToFloatingTextManager(ActiveBomb bomb)
        {
            int id = FloatingTextManager.Instance.AddText(() => { return bomb.Bomb.Description; }, () => { return bomb.Bomb.TextPositionInMission; }, bomb.Bomb.TextColor);
            bomb.FloatingTextId = id;
        }
        private void RemoveTextFromFloatingTextManager(ActiveBomb bomb)
        {
            FloatingTextManager.Instance.Remove(bomb.FloatingTextId);
        }
        public override void OnDeploymentFinished()
        {
            var playerAgent = Current.Agents.FirstOrDefault(a => a.IsPlayerControlled);
            if (playerAgent != null)
                PlayerBombManager.Instance.AddPlayerBombsOnMissionStart(playerAgent);
        }
        public override void OnAfterDeploymentFinished()
        {
            var playerAgent = Current.Agents.FirstOrDefault(a => a.IsPlayerControlled);
            if (playerAgent != null)
                PlayerBombManager.Instance.AddPlayerBombsOnMissionStart(playerAgent);
        }
        //public override void OnAgentControllerSetToPlayer(Agent agent)
        //{
        //    PlayerBombManager.Instance.AddPlayerBombsOnMissionStart(agent);
        //}
        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            if (!shooterAgent.IsPlayerControlled) return;
            var itemShot = shooterAgent.Equipment[weaponIndex].Item;
            var bombDefinition = BaseBombDefinitions.GetBombDefinition(itemShot);
            if (bombDefinition == null) return;
            PlayerBombManager.Instance.OnBombFired(bombDefinition, shooterAgent, weaponIndex, ref _setNewPlayerBombType);
        }
    }
}