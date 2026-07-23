using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Ability
{
    public class ClassAbility
    {
        public static List<ClassAbility> All = new();

        public static void RegisterAll()
        {
            // Static list + one RegisterAll per game start: without this Clear,
            // every new campaign in the same session appended 4 more abilities
            // and .First()-style lookups kept returning the FIRST session's
            // instances (stale delegates over dead mission state).
            All.Clear();

            // Mercenary ability
            AbilityData mercAbilityData = new(15, 90,
                baseActions: new()
                {
                    [AbilityData.ActionTrigger.OnTroopPreHit] = new() {
                        ((Agent attacker, Agent victim, float[] additionalDamagePercentages, float[] resistancePercentages) =>
                            AbilityEffects.GiveThirtyMeleeResistanceToInfantry(attacker, victim, additionalDamagePercentages, resistancePercentages))
                    },
                    [AbilityData.ActionTrigger.OnActivate] = new() {
                        () => AbilityEffects.IncreaseInfantryMorale()
                    },
                },
                upgradedActions: new()
                {
                    [AbilityData.ActionTrigger.OnActivate] = new() { () => AbilityEffects.GiveBerserkerEffects() },
                    [AbilityData.ActionTrigger.OnDeactivate] = new() { () => AbilityEffects.RemoveBerserkerEffects() }
                });

            // Knight ability
            AbilityData knightAbilityData = new(15, 90,
                baseActions: new()
                {
                    [AbilityData.ActionTrigger.OnAgentHit] = new() {
                        (Agent attacker, Agent victim, MissionWeapon weapon, Blow blow, AttackCollisionData colData) =>
                            AbilityEffects.DamageAttackerIfShieldBlocked(attacker, victim, weapon, blow, colData)
                    }
                },
                upgradedActions: new());

            // Wizard ability
            AbilityData wizardAbilityData = new(
                duration: 15,
                cooldown: 90,
                baseActions: new()
                {
                    // GATILHO 1: Ao ativar, aplica o efeito visual nas armas.
                    [AbilityData.ActionTrigger.OnActivate] = new() {
                        () => AbilityEffects.ApplyArcaneSurgeToWeapons()
                    },
                    // GATILHO 2: Ao desativar (quando o tempo acaba), remove o efeito visual.
                    [AbilityData.ActionTrigger.OnDeactivate] = new() {
                        () => AbilityEffects.RemoveArcaneSurgeFromWeapons()
                    },
                    // GATILHO 3: Ao acertar um inimigo, aplica o dano de fogo (DoT).
                    [AbilityData.ActionTrigger.OnAgentHit] = new() {
                        (Agent affectedAgent, Agent affectorAgent, MissionWeapon affectorWeapon, Blow blow, AttackCollisionData col) =>
                        {
                            AbilityEffects.TryApplyFireDotOnHit(affectorAgent, affectedAgent, affectorWeapon);
                        }
                    }
                },
                upgradedActions: new() { });

            AbilityData clericAbilityData = new(
                duration: 12,
                cooldown: 90,
                baseActions: new()
                {
                    [AbilityData.ActionTrigger.OnActivate] = new() {
                        () => AbilityEffects.StartDivineRestoration()
                    },
                    [AbilityData.ActionTrigger.OnDeactivate] = new() {
                        () => AbilityEffects.StopDivineRestoration()
                    }
                },
                upgradedActions: new() { },
                upgradedDuration: 18);

            All.Add(new ClassAbility(
                "merc_ability",
                "{=rf_mercenary_ability_name}Battle Cry",
                "battle_cry_perk_a", "battle_cry_perk_b",
                "{=rf_career_battle_cry_desc} For the next 15 seconds, all your troops get 50% melee damage resistance.",
                "{=rf_career_battle_cry_upgr_desc} During the effect of battle cry, your infantry additionally get berzerker potion effect (+20% attack speed)",
                mercAbilityData));

            All.Add(new ClassAbility(
                "knight_ability",
                "{=rf_knight_ability_name}Divine Shield",
                "divine_shield_perk_a", "divine_shield_perk_b",
                "{=rf_career_battle_cry_desc} Every attack blocked by your shield, deals 20 damage back to the enemy.",
                "{=rf_career_battle_cry_upgr_desc} The reflection damage scales exponentially with deeds points, +0 damage at 400 deeds points, up to +60 dmg at 800 deeds points",
                knightAbilityData));

            All.Add(new ClassAbility(
                "wizard_ability",
                "{=rf_wizard_ability_name}Arcane Surge",
                "wizard_ability_icon_a", "wizard_ability_icon_b",
                "{=rf_wizard_ability_desc}For 15 seconds, your troops’ weapons are imbued with magical fire, igniting enemies on hit.",
                "{=rf_wizard_ability_desc_up}The same, with stronger magical fire (longer duration).",
                wizardAbilityData));

            All.Add(new ClassAbility(
                "cleric_ability",
                "{=rf_cleric_ability_name}Divine Restoration",
                "cleric_ability_perk_a", "cleric_ability_perk_b",
                "{=rf_cleric_ability_desc}For a short time, sacred recovery restores your health over time.",
                "{=rf_cleric_ability_desc_up}Divine Restoration heals more strongly and lasts longer.",
                clericAbilityData));
        }

        public enum AbilityType
        {
            TroopBuff
        }

        private int _coolDownLeft = 0;
        private readonly Timer _timer;
        private float _cooldown_end_time;
        private float _durationEndTime = 0;
        private readonly int cooldown;
        private readonly int duration;

        private readonly string sprite;
        private readonly string spriteUpgraded;

        public bool IsUpgraded
        {
            get => RFCareerCampaignBehavior.Instance.ClassInfo.IsAbilityUpgraded;
            set => RFCareerCampaignBehavior.Instance.ClassInfo.IsAbilityUpgraded = value;
        }

        public bool IsEnabled
        {
            get => RFCareerCampaignBehavior.Instance.ClassInfo.IsAbilityActive;
            set => RFCareerCampaignBehavior.Instance.ClassInfo.IsAbilityActive = value;
        }

        public bool IsActiveInMission => _durationEndTime > Mission.Current.CurrentTime;
        public string CurrentSprite => IsUpgraded ? spriteUpgraded : sprite;
        public string Sprite => sprite;
        public string SpriteUpgraded => spriteUpgraded;
        private readonly string description;
        private readonly string descriptionUpdated;
        private readonly string name;
        public string Description => new TextObject(description).ToString();
        public string DescriptionUpgraded => new TextObject(descriptionUpdated).ToString();
        public string Name => new TextObject(name).ToString();
        private readonly AbilityData data;
        public delegate void OnTroopHitDelegate(Agent attacker, Agent victim, ref float[] additionalDamagePercentages, ref float[] resistancePercentages);
        public string StringId { get; private set; }
        public int GetCoolDownLeft() => _coolDownLeft;

        public ClassAbility(string sstringId, string nname, string ssprite, string sspriteUpgraded, string ddescription, string ddescriptionUpdated, AbilityData ddata)
        {
            StringId = sstringId;
            name = nname;
            sprite = ssprite;
            spriteUpgraded = sspriteUpgraded;
            description = ddescription;
            descriptionUpdated = ddescriptionUpdated;
            _timer = new Timer(1000);
            _timer.Elapsed += TimerElapsed;
            _timer.Enabled = false;
            data = ddata;
            duration = data.Duration;
            cooldown = data.Cooldown;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (Mission.Current == null)
            {
                FinalizeTimer();
                return;
            }

            _coolDownLeft = (int)(_cooldown_end_time - Mission.Current.CurrentTime);
            if (_coolDownLeft <= 0)
            {
                FinalizeTimer();
                Deactivate(Agent.Main);
            }
        }

        private void FinalizeTimer()
        {
            _coolDownLeft = 0;
            _timer.Stop();
        }

        public bool IsDisabled(Agent casterAgent)
        {
            if (IsOnCooldown())
                return true;
            return false;
        }

        public void SetCoolDown()
        {
            _coolDownLeft = cooldown;
            int activeDuration = IsUpgraded ? data.UpgradedDuration : duration;
            _durationEndTime = Mission.Current.CurrentTime + activeDuration + AbilityEffects.GetAdditionalAbilityDuration(StringId);
            _cooldown_end_time = Mission.Current.CurrentTime + _coolDownLeft + 0.8f;
            _timer.Start();
        }

        public bool IsOnCooldown() => _timer.Enabled;

        public virtual bool CanActivate(Agent agent)
        {
            if (IsDisabled(agent)) return false;
            if (!agent.IsActive() || agent.Health <= 0) return false;
            return true;
        }

        public bool TryActivate(Agent casterAgent)
        {
            if (CanActivate(casterAgent))
            {
                Activate(casterAgent);
                return true;
            }
            return false;
        }

        private void InvokeActions(AbilityData.ActionTrigger trigger,
                                   Dictionary<AbilityData.ActionTrigger, List<Delegate>> actionDict,
                                   params object[] par)
        {
            actionDict.TryGetValue(trigger, out var actions);
            if (actions == null) return;

            switch (trigger)
            {
                case AbilityData.ActionTrigger.OnActivate:
                case AbilityData.ActionTrigger.OnDeactivate:
                    foreach (var action in actions.OfType<Action>())
                        action();
                    break;
                case AbilityData.ActionTrigger.OnTroopPreHit:
                    {
                        var attacker = (Agent)par[0];
                        var victim = (Agent)par[1];
                        float[] additionalDamage = (float[])par[2];
                        float[] resistancePercentages = (float[])par[3];
                        foreach (var action in actions.OfType<Action<Agent, Agent, float[], float[]>>())
                            action(attacker, victim, additionalDamage, resistancePercentages);
                    }
                    break;
                case AbilityData.ActionTrigger.OnAgentHit:
                    {
                        var attacker = (Agent)par[0];
                        var victim = (Agent)par[1];
                        var weapon = (MissionWeapon)par[2];
                        var blow = (Blow)par[3];
                        var collisionData = (AttackCollisionData)par[4];
                        foreach (var action in actions.OfType<Action<Agent, Agent, MissionWeapon, Blow, AttackCollisionData>>())
                            action(attacker, victim, weapon, blow, collisionData);
                    }
                    break;
            }
        }

        protected virtual void Activate(Agent casterAgent)
        {
            SetCoolDown();
            InvokeActions(AbilityData.ActionTrigger.OnActivate, data.BaseActions);
            if (IsUpgraded)
                InvokeActions(AbilityData.ActionTrigger.OnActivate, data.UpgradedActions);
        }

        internal void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            InvokeActions(AbilityData.ActionTrigger.OnAgentHit, data.BaseActions, affectorAgent, affectedAgent, affectorWeapon, blow, attackCollisionData);
            if (IsUpgraded)
                InvokeActions(AbilityData.ActionTrigger.OnAgentHit, data.UpgradedActions, affectorAgent, affectedAgent, affectorWeapon, blow, attackCollisionData);
        }

        internal void OnTroopPreHit(Agent attacker, Agent victim, ref float[] additionalDamagePercentages, ref float[] resistancePercentages)
        {
            InvokeActions(AbilityData.ActionTrigger.OnTroopPreHit, data.BaseActions, attacker, victim, additionalDamagePercentages, resistancePercentages);
            if (IsUpgraded)
                InvokeActions(AbilityData.ActionTrigger.OnTroopPreHit, data.UpgradedActions, attacker, victim, additionalDamagePercentages, resistancePercentages);
        }

        protected virtual void Deactivate(Agent casterAgent)
        {
            InvokeActions(AbilityData.ActionTrigger.OnDeactivate, data.BaseActions);
            if (IsUpgraded)
                InvokeActions(AbilityData.ActionTrigger.OnDeactivate, data.UpgradedActions);
        }
    }
}
