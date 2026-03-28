using RealmsForgotten.CustomSkills;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.RFEffects
{
    public class MeteorMissionLogic : MissionLogic
    {
        readonly List<MeteorData> _currentMeteors = new();
        private static readonly Random _random = new();
        const float TimeBetweenProjectiles = 0.1f;
        const int BaseProjectilesPerCast = 4;
        int GetMeteorProjectilesPerCast(Agent caster)
        {
            var projectilesFromSkill = Campaign.Current != null ? caster.Character.GetSkillValue(RFSkills.Arcane) / 10 : 1;
            return BaseProjectilesPerCast + projectilesFromSkill;
        }
        readonly ItemObject meteorProjectile = MBObjectManager.Instance.GetObject<ItemObject>("meteor");
        readonly ItemObject meteorSpell = MBObjectManager.Instance.GetObject<ItemObject>("rfmisc_meteor_spell");
        public static Vec3 RandomizeXY(Vec3 original)
        {
            float offsetX = (float)(_random.NextDouble() * 10 - 5);
            float offsetY = (float)(_random.NextDouble() * 10 - 5);

            return new Vec3(original.X + offsetX, original.Y + offsetY, original.Z);
        }
        List<Vec3> ChooseVectors(Vec3 original, Agent caster)
        {
            var list = new List<Vec3>();
            for (int i = 0; i < GetMeteorProjectilesPerCast(caster); i++)
            {
                list.Add(RandomizeXY(original));
            }
            return list;
        }
        public override void OnMissionTick(float dt)
        {
            if (_currentMeteors.Count == 0) return;
            for (int i = _currentMeteors.Count - 1; i >= 0; i--)
            {
                MeteorData? meteorData = _currentMeteors[i];
                meteorData.TickTillNextProjectile -= dt;
                if (meteorData.TickTillNextProjectile < 0)
                {
                    MeteorLogic.FireMeteor(meteorData.Caster, meteorData.ProjectilePositions.First(), meteorProjectile);
                    meteorData.ProjectilePositions.RemoveAt(0);
                    meteorData.TickTillNextProjectile = 0.1f;
                    if (meteorData.ProjectilePositions.Count == 0) _currentMeteors.Remove(meteorData);
                }
            }
        }
        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            var missle = Mission.Current.MissilesList.FirstOrDefault(m => m.Index == collisionData.AffectorWeaponSlotOrMissileIndex);
            if (missle == null) return;
            var spell = missle.Weapon.Item;
            if (spell.StringId == meteorSpell.StringId)
                _currentMeteors.Add(new MeteorData(TimeBetweenProjectiles, ChooseVectors(collisionData.CollisionGlobalPosition, attacker), attacker));
        }
    }
    public class MeteorData
    {
        public MeteorData(float tickTillNextProjectile, List<Vec3> projectilePositions, Agent caster)
        {
            TickTillNextProjectile = tickTillNextProjectile;
            ProjectilePositions = projectilePositions;
            Caster = caster;
        }
        public float TickTillNextProjectile { get; set; }
        public List<Vec3> ProjectilePositions { get; set; }
        public Agent Caster { get; private set; }
    }

}
