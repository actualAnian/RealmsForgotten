using RealmsForgotten.Career.Ability;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.AgentOrigins;
using System.Collections.Generic;
using System.Linq;

namespace RealmsForgotten.Career.Logic
{
    public class CareerPerkMissionBehavior : MissionLogic
    {
        private bool _didWizardImmunitiesInit = false;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            AbilityEffects.BurningAgentsFromAbility.Clear();
            AbilityEffects.RemoveArcaneSurgeFromWeapons();
            AbilityEffects.PendingBurningDamage.Clear();
            AbilityEffects.StopDivineRestoration();
        }

        protected override void OnEndMission()
        {
            AbilityEffects.StopDivineRestoration();
            base.OnEndMission();
        }

        public override void OnAgentRemoved(
            Agent affectedAgent,
            Agent affectorAgent,
            AgentState agentState,
            KillingBlow blow)
        {
            if (affectedAgent != null)
            {
                // Limpeza do efeito de burning. Essa lógica é OK aqui.
                if (AbilityEffects.BurningAgentsFromAbility.TryGetValue(affectedAgent, out var burningData))
                {
                    // Remover partícula imediatamente ao remover o agente.
                    // Isso é crucial para evitar acessar uma partícula associada a um agente que não existe mais.
                    burningData.ParticleEntity?.Remove(0);
                    AbilityEffects.BurningAgentsFromAbility.Remove(affectedAgent);
                }
            }

            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            try
            {
                if (!PlayerCareerExtension.HasAnyCareer() || affectorAgent == null) return;

                if (affectorAgent.IsMainAgent)
                {
                    var choices = PlayerCareerExtension.GetAllCareerChoices();
                    foreach (var choiceID in choices)
                    {
                        CareerChoiceObject choice = RFCareerChoices.GetChoice(choiceID);
                        if (choice?.Passive?.PassiveEffectType == PassiveEffectType.OnKill)
                        {
                            choice.Passive.Activate();
                        }
                    }
                }
                else
                {
                    bool hasCompanionHealOnKill = false;
                    foreach (var id in PlayerCareerExtension.GetAllCareerChoices())
                    {
                        var ch = RFCareerChoices.GetChoice(id);
                        if (ch != null && ch.Description.ToString().Contains("Companions gain 5 HP on kill"))
                        {
                            hasCompanionHealOnKill = true;
                            break;
                        }
                    }

                    if (hasCompanionHealOnKill && IsPlayerCompanionAgent(affectorAgent))
                    {
                        float healAmount = 5f;
                        affectorAgent.Health = MathF.Min(affectorAgent.HealthLimit, affectorAgent.Health + healAmount);
                    }
                }
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("[RF Career] OnAgentRemoved Error: " + ex.Message, Colors.Red));
            }
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            CareerObject? career = PlayerCareerExtension.GetCareer();
            if (career == null) return;

            var ability = career.Ability;
            if (ability.IsActiveInMission)
            {
                ability.OnAgentHit(affectedAgent, affectorAgent, affectorWeapon, blow, attackCollisionData);
            }
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);

            try
            {
                if (!PlayerCareerExtension.HasAnyCareer()) return;

                bool hasPerk = false;
                foreach (var id in PlayerCareerExtension.GetAllCareerChoices())
                {
                    var ch = RFCareerChoices.GetChoice(id);
                    if (ch != null && ch.Description.ToString().Contains("Increase your companions hitpoints by 40"))
                    {
                        hasPerk = true;
                        break;
                    }
                }

                if (hasPerk && IsPlayerCompanionAgent(agent))
                {
                    agent.HealthLimit += 40f;
                    agent.Health = agent.HealthLimit;
                }
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("[RF Career] OnAgentCreated Error: " + ex.Message, Colors.Red));
            }
        }

        private bool IsPlayerCompanionAgent(Agent agent)
        {
            if (agent == null || agent.IsMainAgent || agent.Character == null)
                return false;

            var hero = (agent.Character as CharacterObject)?.HeroObject;
            return hero != null && hero.IsPlayerCompanion;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (Mission.Current == null) return;
            float currentTime = Mission.Current.CurrentTime;

            // --- Lógica de Burning Damage Over Time (DoT) ---
            if (AbilityEffects.BurningAgentsFromAbility.Count > 0)
            {
                var agentsToProcess = AbilityEffects.BurningAgentsFromAbility.Keys.ToList();
                var agentsToRemoveFromBurning = new List<Agent>();

                foreach (Agent victim in agentsToProcess)
                {
                    // VERIFICAÇÃO INICIAL CRÍTICA: Se o agente não está ativo OU se AgentVisuals é nulo (não visível/preparado para renderização)
                    if (victim == null || !victim.IsActive() || victim.AgentVisuals == null)
                    {
                        agentsToRemoveFromBurning.Add(victim);
                        continue;
                    }

                    if (AbilityEffects.BurningAgentsFromAbility.TryGetValue(victim, out var data))
                    {
                        if (currentTime >= data.EndTime)
                        {
                            agentsToRemoveFromBurning.Add(victim);
                            continue;
                        }

                        // Tentar atualizar a posição da partícula.
                        // Usar try-catch aqui É MAIS CRÍTICO AGORA.
                        // Também verificar se data.ParticleEntity existe antes de tentar manipulá-lo.
                        if (data.ParticleEntity != null)
                        {
                            try
                            {
                                data.ParticleEntity.SetGlobalFrame(victim.AgentVisuals.GetGlobalFrame());
                            }
                            catch (System.Exception ex)
                            {
                                InformationManager.DisplayMessage(new InformationMessage($"[RF Career] Burning particle update error for {victim.Name}: {ex.Message}", Colors.Yellow));
                                agentsToRemoveFromBurning.Add(victim);
                                continue;
                            }
                        }
                        else // Se a partícula for nula, o efeito não está visualmente ativo, remova-o.
                        {
                            agentsToRemoveFromBurning.Add(victim);
                            continue;
                        }


                        if (currentTime >= data.NextTickTime)
                        {
                            if (victim.IsActive() && victim.AgentVisuals != null) // VERIFICAÇÃO DUPLA ANTES DE ADICIONAR DANO À FILA
                            {
                                int damage = MBRandom.RandomInt(5, 10);
                                AbilityEffects.PendingBurningDamage.Add((victim, damage));
                                data.NextTickTime = currentTime + 2f;
                            }
                            else
                            {
                                agentsToRemoveFromBurning.Add(victim);
                            }
                        }
                    }
                }

                foreach (Agent agentToRemove in agentsToRemoveFromBurning)
                {
                    if (AbilityEffects.BurningAgentsFromAbility.TryGetValue(agentToRemove, out var burningData))
                    {
                        burningData.ParticleEntity?.Remove(0);
                        AbilityEffects.BurningAgentsFromAbility.Remove(agentToRemove);
                    }
                }
            }

            ApplyPendingBurningDamage();


            // --- Lógica de Wizard Immunities Init (Inalterada) ---
            if (!_didWizardImmunitiesInit && currentTime > 0.2f)
            {
                _didWizardImmunitiesInit = true;

                float meleeSec = 0f;
                float rangedSec = 0f;

                foreach (var id in PlayerCareerExtension.GetAllCareerChoices())
                {
                    var ch = RFCareerChoices.GetChoice(id);
                    if (ch?.Passive == null) continue;

                    var s = ch.Description.ToString();
                    if (s.Contains("Immune to melee damage for 30s"))
                        meleeSec = MathF.Max(meleeSec, ch.Passive.EffectMagnitude);
                    if (s.Contains("Immune to ranged damage for 30s"))
                        rangedSec = MathF.Max(rangedSec, ch.Passive.EffectMagnitude);
                }

                if (meleeSec > 0f) AbilityEffects.StartPlayerMeleeImmunity(meleeSec);
                if (rangedSec > 0f) AbilityEffects.StartPlayerRangedImmunity(rangedSec);
            }

            var career = PlayerCareerExtension.GetCareer();
            if (career?.StringId == "cleric" && career.Ability.IsActiveInMission)
            {
                AbilityEffects.TickDivineRestoration(dt);
            }
            else if (career?.StringId == "cleric")
            {
                AbilityEffects.StopDivineRestoration();
            }
        }

        private void ApplyPendingBurningDamage()
        {
            var damagesToApply = new List<(Agent victim, int damage)>(AbilityEffects.PendingBurningDamage);
            AbilityEffects.PendingBurningDamage.Clear();

            foreach (var (victim, damage) in damagesToApply)
            {
                // CRÍTICO: Última verificação de validade ANTES de aplicar o dano, e também se AgentVisuals é nulo.
                if (victim != null && victim.IsActive() && victim.AgentVisuals != null)
                {
                    try
                    {
                        Blow blow = CreateMagicBlow(victim, damage);
                        AttackCollisionData collisionData = CreateCollisionDataForMagic(victim, blow);
                        victim.RegisterBlow(blow, collisionData);
                    }
                    catch (System.Exception ex)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"[RF Career] Burning damage application error for {victim.Name}: {ex.Message}", Colors.Red));
                        if (AbilityEffects.BurningAgentsFromAbility.TryGetValue(victim, out var burningData))
                        {
                            burningData.ParticleEntity?.Remove(0);
                            AbilityEffects.BurningAgentsFromAbility.Remove(victim);
                        }
                    }
                }
                else
                {
                    // Se o agente não está mais ativo ou visível, limpe o efeito de burning.
                    if (AbilityEffects.BurningAgentsFromAbility.TryGetValue(victim, out var burningData))
                    {
                        burningData.ParticleEntity?.Remove(0);
                        AbilityEffects.BurningAgentsFromAbility.Remove(victim);
                    }
                }
            }
        }


        private Blow CreateMagicBlow(Agent victim, int damage)
        {
            Blow blow = new Blow(victim.Index)
            {
                DamageType = DamageTypes.Blunt,
                // Mantido BlowFlag, mas considere testar com BlowFlags.None se o problema persistir.
                BlowFlag = BlowFlags.ShrugOff | BlowFlags.NoSound,
                BoneIndex = victim.Monster.HeadLookDirectionBoneIndex,
                GlobalPosition = victim.Position,
                BaseMagnitude = 0f,
                InflictedDamage = damage,
                SwingDirection = victim.LookDirection,
                DamageCalculated = true
            };
            blow.Direction = blow.SwingDirection;
            blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
            return blow;
        }

        private AttackCollisionData CreateCollisionDataForMagic(Agent victim, Blow blow)
        {
            return AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                false, false, false, false, false, false, false, false, false, false, false, false,
                CombatCollisionResult.StrikeAgent,
                -1, 0, victim.Index,
                blow.BoneIndex, BoneBodyPartType.Head,
                victim.Monster.MainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1,
                CombatHitResultFlags.NormalHit,
                0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f,
                victim.LookDirection, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, victim.Velocity,
                Vec3.Zero);
        }
    }
}
