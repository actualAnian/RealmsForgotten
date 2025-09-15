using RealmsForgotten.Career.CareerPointsSystem;
using RealmsForgotten.ObjectExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using RealmsForgotten.RFEffects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;

namespace RealmsForgotten.Career.Ability
{
    public class AbilityData
    {
        public int Duration { get; private set; }
        public int Cooldown { get; private set; }
        public AbilityData(int duration, int cooldown,
            Dictionary<ActionTrigger, List<Delegate>> baseActions,
            Dictionary<ActionTrigger, List<Delegate>> upgradedActions)
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

    public class CareerBurningAgentData
    {
        public float EndTime;
        public float NextTickTime;
        public GameEntity ParticleEntity;
    }

    public static class AbilityEffects
    {
        public static Dictionary<Agent, CareerBurningAgentData> BurningAgentsFromAbility = new();
        private static List<GameEntity> TroopWeaponParticles = new();

        // NOVO: Lista para armazenar o dano de burning pendente
        public static List<(Agent victim, int damage)> PendingBurningDamage = new();


        public static void TryApplyFireDotOnHit(Agent attacker, Agent victim, MissionWeapon weapon)
        {
            if (attacker == null || !attacker.BelongsToMainParty() || attacker.IsMainAgent) return;
            if (victim == null || !victim.IsActive() || Mission.Current == null) return;
            var career = PlayerCareerExtension.GetCareer();
            if (career == null) return;
            float currentTime = Mission.Current.CurrentTime;
            float duration = career.Ability.IsUpgraded ? 10f : 6f;
            foreach (var id in PlayerCareerExtension.GetAllCareerChoices())
            {
                var ch = RFCareerChoices.GetChoice(id);
                if (ch?.Passive != null && ch.Description.ToString().Contains("Ignite has +2s longer duration"))
                {
                    duration += 2f;
                    break;
                }
            }
            if (BurningAgentsFromAbility.TryGetValue(victim, out var existingData))
            {
                existingData.EndTime = currentTime + duration;
            }
            else
            {
                string fireParticleName = "fire_ground";
                GameEntity particleEntity = null;
                if (ParticleSystemManager.GetRuntimeIdByName(fireParticleName) != -1)
                {
                    TOWParticleSystem.ApplyParticleToAgent(victim, fireParticleName, out particleEntity);
                }
                BurningAgentsFromAbility.Add(victim, new CareerBurningAgentData
                {
                    EndTime = currentTime + duration,
                    NextTickTime = currentTime + 0.1f,
                    ParticleEntity = particleEntity
                });
            }
        }

        public static void ApplyArcaneSurgeToWeapons()
        {
            if (Mission.Current == null) return;
            //RemoveArcaneSurgeFromWeapons();

            string weaponFireParticleId = "fire_sword";
            //string weaponFireParticleId = "harmonic_convergence";
            if (ParticleSystemManager.GetRuntimeIdByName(weaponFireParticleId) == -1)
            {
                InformationManager.DisplayMessage(new InformationMessage($"[Arcane Surge ERROR] Particle '{weaponFireParticleId}' not found!", Colors.Red));
                return;
            }

            foreach (var agent in Mission.Current.Agents.Where(a => a.IsHuman && a.BelongsToMainParty() && !a.IsMainAgent))
            {
                try
                {
                    EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                    if (wieldedItemIndex == EquipmentIndex.None) continue;

                    MissionWeapon weapon = agent.WieldedWeapon;
                    if (weapon.IsEmpty || weapon.CurrentUsageItem.IsShield) continue;

                    if (RFEffectsLibrary.CurrentWeaponEffects.ContainsKey(weapon.Item.StringId)) continue;

                    Skeleton skeleton = agent.AgentVisuals.GetSkeleton();

                    int weaponLength = weapon.GetWeaponStatsData()[0].WeaponLength;
                    GameEntity? weaponEntity = null;
                    for (int i = 1; i < weaponLength / 10; i++)
                    {
                        TOWParticleSystem.ApplyParticleToWeapon(agent, weaponFireParticleId, wieldedItemIndex, i * 0.1f, skeleton, out weaponEntity);
                    }
                    if (weaponEntity != null)
                    {
                        TroopWeaponParticles.Add(weaponEntity);
                    }
                }
                catch (Exception ex)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"[Arcane Surge ERROR] Failed to apply effect to {agent.Name}: {ex.Message}", Colors.Red));
                }
            }
        }

        public static void RemoveArcaneSurgeFromWeapons()
        {
            foreach (GameEntity particleEntity in TroopWeaponParticles)
                particleEntity.RemoveAllParticleSystems();
            TroopWeaponParticles.Clear();
        }

        public static List<Agent> agentsWithProperties = new();
        private static readonly float battleCrySwingSpeedMult = 0.2f;

        public static void GiveBerserkerEffects()
        {
            try
            {
                int soundId = SoundEvent.GetEventIdFromString("berzerker_horn");
                if (soundId != -1)
                {
                    SoundEvent soundEvent = SoundEvent.CreateEvent(soundId, Mission.Current.Scene);
                    soundEvent.SetPosition(Agent.Main.Position);
                    soundEvent.Play();
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("[Berserker Sound] Evento 'berzerker_horn' não encontrado.", Colors.Red));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"[Berserker Sound ERROR] {ex.Message}", Colors.Red));
            }

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
            for (int i = agentsWithProperties.Count - 1; i >= 0; i--)
            {
                Agent agent = agentsWithProperties[i];
                agent.AgentDrivenProperties.SwingSpeedMultiplier /= (1 + battleCrySwingSpeedMult);
                agent.UpdateCustomDrivenProperties();
                agentsWithProperties.RemoveAt(i);
            }
        }

        public static void GiveThirtyMeleeResistanceToInfantry(Agent attacker, Agent victim, float[] additionalDamagePercentages, float[] resistancePercentages)
        {
            if (victim.BelongsToMainParty() && victim.Character != null && victim.Character.IsInfantry)
                resistancePercentages[1] += 0.3f;

            if (victim == Agent.Main)
            {
                if (IsPlayerMeleeImmune())
                    resistancePercentages[(int)DamageType.PhysicalMelee] += 1.0f;

                if (IsPlayerRangedImmune())
                    resistancePercentages[(int)DamageType.PhysicalRanged] += 1.0f;
            }
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

            CareerObject career = PlayerCareerExtension.GetCareer()!;
            bool isAbilityUpgraded = career.Ability.IsUpgraded;
            int damage = 20;

            if (isAbilityUpgraded && PlayerCareerExtension.PointsSystem is DeedsPointsSystem dps)
            {
                int excess = Math.Max(0, dps.AllDeedsPoints() - 400);
                damage += Math.Min(60, (excess * 60) / 400);
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

            AttackCollisionData attackCollisionDataForDebugPurpose =
                AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                    false, false, false, true, false, false, false, false, false, false, false, false,
                    CombatCollisionResult.StrikeAgent, -1, 0, 2, blow.BoneIndex, BoneBodyPartType.Head,
                    victim.Monster.MainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1,
                    CombatHitResultFlags.NormalHit, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f,
                    Vec3.Up, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, victim.Velocity,
                    Vec3.Up);

            attacker.RegisterBlow(divineBlow, attackCollisionDataForDebugPurpose);
        }
        public static float GetSpellAmmoPercent()
        {
            float sum = 0f;
            foreach (var id in PlayerCareerExtension.GetAllCareerChoices())
            {
                var ch = RFCareerChoices.GetChoice(id);
                if (ch?.Passive == null) continue;
                if (ch.Description.ToString().Contains("Increase spell stack amount by 10%"))
                    sum += 0.10f;
            }
            return sum;
        }

        public static float GetSpellAOEPercent()
        {
            float sum = 0f;
            foreach (var id in PlayerCareerExtension.GetAllCareerChoices())
            {
                var ch = RFCareerChoices.GetChoice(id);
                if (ch?.Passive == null) continue;
                var d = ch.Description.ToString();
                if (d.Contains("Increase spell area effect by 5%")) sum += 0.05f;
                if (d.Contains("Increase spell area effect by 10%")) sum += 0.10f;
            }
            return sum;
        }

        public static float PlayerMeleeImmuneUntil = 0f;
        public static float PlayerRangedImmuneUntil = 0f;

        public static void StartPlayerMeleeImmunity(float seconds)
        {
            if (Mission.Current == null) return;
            PlayerMeleeImmuneUntil = MathF.Max(PlayerMeleeImmuneUntil, Mission.Current.CurrentTime + seconds);
        }

        public static void StartPlayerRangedImmunity(float seconds)
        {
            if (Mission.Current == null) return;
            PlayerRangedImmuneUntil = MathF.Max(PlayerRangedImmuneUntil, Mission.Current.CurrentTime + seconds);
        }

        public static bool IsPlayerMeleeImmune() => Mission.Current != null && Mission.Current.CurrentTime < PlayerMeleeImmuneUntil;
        public static bool IsPlayerRangedImmune() => Mission.Current != null && Mission.Current.CurrentTime < PlayerRangedImmuneUntil;

        public static void GiveItemById(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return;
            try
            {
                var item = Game.Current.ObjectManager.GetObject<ItemObject>(stringId);
                if (item == null) return;

                var mainParty = MobileParty.MainParty;
                if (mainParty != null)
                {
                    mainParty.ItemRoster.AddToCounts(item, 1);
                    InformationManager.DisplayMessage(new InformationMessage($"[Wizard] Added '{item.Name}' to inventory."));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"[Wizard ERROR] Could not give item '{stringId}': {ex.Message}"));
            }
        }
    }
}