using RealmsForgotten.Career.CareerPointsSystem;
using RealmsForgotten.ObjectExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Ability
{
    public class AbilityData
    {
        public int Duration { get; private set; }
        public int Cooldown { get; private set; }
        public AbilityData(int duration, int cooldown, Dictionary<ActionTrigger, List<Delegate>> baseActions, Dictionary<ActionTrigger, List<Delegate>> upgradedActions)
        {
            Duration = duration;
            Cooldown = cooldown;
            BaseActions = baseActions;
            UpgradedActions = upgradedActions;
        }

        public enum ActionTrigger
        {
            OnActivate,
            OnDeactivate,
            OnAgentHit,
            OnTroopPreHit
        }
        public Dictionary<ActionTrigger, List<Delegate>> BaseActions { get; set; }
        public Dictionary<ActionTrigger, List<Delegate>> UpgradedActions { get; set; }
    }
    public static class AbilityEffects
    {
        public static List<Agent> agentsWithProperties = new();
        private static readonly float battleCrySwingSpeedMult = 0.2f;
        public static void GiveBerserkerEffects()
        {
            Agent.Main.AgentDrivenProperties.SwingSpeedMultiplier *= (1 + battleCrySwingSpeedMult);
            IEnumerable<Agent> agents = Mission.Current.Agents.Where(a => a.BelongsToMainParty());
            foreach (Agent agent in agents)
            {
                agentsWithProperties.Add(agent);
                agent.AgentDrivenProperties.SwingSpeedMultiplier *= (1 + battleCrySwingSpeedMult);
                agent.UpdateCustomDrivenProperties();
            }
        }
        public static void CheckAgents()
        {
            for (int i = agentsWithProperties.Count - 1; i >= 0; i--)
            {
                if (!agentsWithProperties[i].IsActive()) agentsWithProperties.RemoveAt(i);
            }
        }
        public static void RemoveBerserkerEffects()
        {
            CheckAgents();
            for (int i = agentsWithProperties.Count; i >= 0; i--)
            {
                Agent agent = agentsWithProperties[i];
                agent.AgentDrivenProperties.SwingSpeedMultiplier /= 1.2f;
                agent.UpdateCustomDrivenProperties();
                agentsWithProperties.RemoveAt(i);
            }
        }
        public static void GiveThirtyMeleeResistanceToInfantry(Agent attacker, Agent victim, float[] additionalDamagePercentages, float[] resistancePercentages)
        {
            if (victim.BelongsToMainParty() && victim.Character != null && victim.Character.IsInfantry) resistancePercentages[1] += 0.3f;
        }
        public static void IncreaseInfantryMorale()
        {
            foreach (Agent? agent in Mission.Current.PlayerTeam.ActiveAgents)
            {
                if (agent.BelongsToMainParty() && agent.Character != null && agent.Character.IsInfantry)
                    agent.ChangeMorale(20);
            }
        }

        public static void DamageAttackerIfShieldBlocked(Agent attacker, Agent victim, MissionWeapon weapon, Blow blow, AttackCollisionData colData)
        {
            if (!colData.AttackBlockedWithShield) return;

            static double ExponentialFunction(double x, double targetValue, double baseValue)
            {
                double k = -Math.Log(1 - (targetValue / targetValue)) / baseValue;
                return targetValue * (1 - Math.Exp(-k * x));
            }
            CareerObject career = PlayerCareerExtension.GetCareer()!;
            bool isAbilityUpgraded = career.Ability.IsUpgraded;
            int damage = 20;
            if (isAbilityUpgraded && PlayerCareerExtension.PointsSystem is DeedsPointsSystem dps && dps.AllDeedsPoints() - 400 > 0)
            {
                damage += (int)Math.Min(60, ExponentialFunction(dps.AllDeedsPoints() - 400, 60, 400));
            }
            Blow divineBlow = new(victim.Index)
            {
                DamageType = DamageTypes.Blunt,
                BoneIndex = victim.Monster.HeadLookDirectionBoneIndex,
                GlobalPosition = victim.Position,
                BaseMagnitude = 2000f,
                SwingDirection = victim.LookDirection,
                InflictedDamage = damage
            };
            divineBlow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
            AttackCollisionData attackCollisionDataForDebugPurpose = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(false, false, false, true, false, false, false, false, false, false, false, false, 
                CombatCollisionResult.StrikeAgent, -1, 0, 2, blow.BoneIndex, BoneBodyPartType.Head, victim.Monster.MainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1,
                CombatHitResultFlags.NormalHit, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, Vec3.Up, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, victim.Velocity,
                Vec3.Up);

            attacker.RegisterBlow(divineBlow, attackCollisionDataForDebugPurpose);
        }
    }
}