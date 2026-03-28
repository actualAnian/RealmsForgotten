using BehaviorTrees;
using BehaviorTreeWrapper;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchTree;
using RealmsForgotten.RFEffects;
using RealmsForgotten.Utility;
using RealmsForgotten.Utility.Magic;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class WingedWitchFinalMissionLogic : MissionLogic
    {
        private const float TimeBetweenProjectiles = 2f;
        private const float ZValueOfTeleportingProjectile = 34f;
        private const string IdOfTeleportingItem = "witch_teleport_projectile";
        private static readonly Tuple<int, int, int, int> XYTeleportingProjectilesBox = new(181, 208, 238, 255);
        private const int DistanceBetweenLastProjectile = 2;
        private readonly Random _random = new();
        static Vec2 pointA = new(209.4466f, 176.995f);
        static Vec2 pointB = new(177.7665f, 176.204f);
        const float ZValueToTeleportTo = 22f;
        private ItemObject? _teleportProjectile;
        private ItemObject TeleportingProjectile => _teleportProjectile ??= MBObjectManager.Instance.GetObject<ItemObject>(IdOfTeleportingItem);
        private bool _isPlayerDead = false;
        private float _playerDeadTimer = 0f;
        private bool _questNotified = false;
        public bool SpawnTeleportingProjectiles { get; set; }
        private float _timeSinceLastProjectile = 0f;
        private Vec2 _lastProjectilePosition = new(181, 238);
        Agent? _witch;
        private void HandleSpawningTeleportingProjectiles(float dt)
        {
            if (_witch == null || !SpawnTeleportingProjectiles) return;
            _timeSinceLastProjectile += dt;
            if (_timeSinceLastProjectile < TimeBetweenProjectiles) return;
            _timeSinceLastProjectile = 0f;
            float xValue = ChooseCoordinateOfNextProjectile(XYTeleportingProjectilesBox.Item1, XYTeleportingProjectilesBox.Item2, _lastProjectilePosition.X);
            float yValue = ChooseCoordinateOfNextProjectile(XYTeleportingProjectilesBox.Item3, XYTeleportingProjectilesBox.Item4, _lastProjectilePosition.Y);
            var newPos = new Vec3(xValue, yValue, ZValueOfTeleportingProjectile);
            //newPos = Agent.Main.Position + new Vec3(0, 0, 5f);
            MeteorLogic.FireMeteor(_witch, newPos, TeleportingProjectile);
        }
        private float ChooseCoordinateOfNextProjectile(float minValue, float maxValue, float lastPosition)
        {
            float availableLeftSpace = Math.Max(0, lastPosition - DistanceBetweenLastProjectile - minValue);
            float availableRightSpace = Math.Max(0, maxValue - (lastPosition + DistanceBetweenLastProjectile));
            var nextValue = _random.NextFloat();
            nextValue *= availableRightSpace + availableLeftSpace;
            if (nextValue > availableLeftSpace)
                nextValue += maxValue - (availableLeftSpace + availableRightSpace);
            else nextValue += minValue;
            return nextValue;
        }
        public override void OnRegisterBlow(Agent attacker, Agent victim, WeakGameEntity realHitEntity, Blow b, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon)
        {
            var missle = Mission.Current.MissilesList.FirstOrDefault(m => m.Index == b.WeaponRecord.AffectorWeaponSlotOrMissileIndex);
            if (missle == null) return;
            var item = missle.Weapon.Item;
            if (item.StringId == IdOfTeleportingItem)
                Teleport.TeleportToPosition(victim, new Vec3(RFMaths.GetRandomPointOnLine(pointA, pointB), ZValueToTeleportTo));
        }
        bool isInitialized = false;
        float timer = 0;
        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            base.OnMissileHit(attacker, victim, isCanceled, collisionData);
        }
        public override void OnMissionTick(float dt)
        {
            timer += dt;
            if (timer < 1) return;
            if (!isInitialized)
            {
                _witch = Mission.Agents.FirstOrDefault(agent => agent.Character?.StringId == "evil_witch");
                isInitialized = true;
                InitializeWitch(_witch);
            }
            _witch = Mission.Current.PlayerEnemyTeam.ActiveAgents.FirstOrDefault(a => a.Character.StringId == "evil_witch");
            HandleSpawningTeleportingProjectiles(dt);
            base.OnMissionTick(dt);
            if (_isPlayerDead)
            {
                _playerDeadTimer += dt;
                if (_playerDeadTimer > 4f)
                {
                    _isPlayerDead = false;
                    KillCharacterAction.ApplyByWounds(Hero.MainHero, true);
                }
            }
        }
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent.IsHero && affectedAgent.Character.StringId == Hero.MainHero.CharacterObject.StringId)
            {
                _isPlayerDead = true;
            }

            if (!_questNotified &&
                affectedAgent?.Character?.StringId == "winged_evil_witch" &&
                agentState == AgentState.Killed)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "DEBUG: Winged Witch defeated inside mission!", Colors.Green));

                NinthQuest.OnWitchDefeatedInNinthQuest();
                _questNotified = true;
            }
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }
        public void InitializeWitch(Agent witch)
        {
            BTRegister.RegisterClass("FinalWitchTree", objects => FinalWitchFightTree.BuildTree(objects));
            witch.AddComponent(new BehaviorTreeAgentComponent(witch, "FinalWitchTree"));
        }
    }
}