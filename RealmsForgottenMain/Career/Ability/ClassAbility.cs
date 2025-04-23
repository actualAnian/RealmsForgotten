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
            AbilityData mercAbilityData = new(15, 15, baseActions: new()
            {
                [AbilityData.ActionTrigger.OnTroopPreHit] = new() { ((Agent attacker, Agent victim, float[] additionalDamagePercentages, float[] resistancePercentages) => AbilityEffects.GiveThirtyMeleeResistanceToInfantry(attacker, victim, additionalDamagePercentages, resistancePercentages)) },
                [AbilityData.ActionTrigger.OnActivate] = new() { () => AbilityEffects.IncreaseInfantryMorale() },
            },
            upgradedActions: new()
            {
                [AbilityData.ActionTrigger.OnActivate] = new() { () => AbilityEffects.GiveBerserkerEffects() },
                [AbilityData.ActionTrigger.OnDeactivate] = new() { () => AbilityEffects.RemoveBerserkerEffects() }
            });
            AbilityData knightAbilityData = new(15, 15, baseActions: new()
            {
                [AbilityData.ActionTrigger.OnAgentHit] = new() { (Agent attacker, Agent victim, MissionWeapon weapon, Blow blow, AttackCollisionData colData) => AbilityEffects.DamageAttackerIfShieldBlocked(attacker, victim, weapon, blow, colData) }
            },
            upgradedActions: new()
            {
            });
            AbilityData lightwardenAbilityData = new(15, 15, baseActions: new()
            {
                [AbilityData.ActionTrigger.OnActivate] = new() { () => AbilityEffects.GiveRadiantBenedictionEffects() },
                [AbilityData.ActionTrigger.OnDeactivate] = new() { () => AbilityEffects.RemoveRadiantBenedictionEffects() }
            },
            upgradedActions: new()
            {
            });

            //Instance
            All.Add(new ClassAbility("merc_ability", "{=rf_mercenary_ability_name}Battle Cry", "battle_cry_perk_a", "battle_cry_perk_b", "{=rf_career_battle_cry_desc} For the next 15 seconds, all your troops get 50% melee damage resistance.", "{=rf_career_battle_cry_upgr_desc} During the effect of battle cry, your infantry additionally get berzerker potion effect (+20% attack speed)", mercAbilityData));
            All.Add(new ClassAbility("knight_ability", "{=rf_knight_ability_name}Divine Shield", "divine_shield_perk_a", "divine_shield_perk_b", "{=rf_career_battle_cry_desc} Every attack blocked by your shield, deals 20 damage back to the enemy.", "{=rf_career_battle_cry_upgr_desc} The reflection damage scales exponentially with deeds points, +0 damage at 400 deeds points, up to +60 dmg at 800 deeds points", knightAbilityData));
            All.Add(new ClassAbility("lightwarden_ability", "{=rf_lightwarden_ability_name}Radiant Benediction", "radiant_benediction_perk_a", "radiant_benediction_perk_b", "{=rf_career_battle_cry_desc} When is activated, heals all of your troops by 30% in a range of 5 meters.", "{=rf_career_battle_cry_upgr_desc} When is activated, heals all of your troops by 60% in a range of 15 meters.", lightwardenAbilityData));
        }
        public enum AbilityType
        {
            TroopBuff
        }
        // timer
        private int _coolDownLeft = 0;
        private readonly Timer _timer;
        private float _cooldown_end_time;
        private float _durationEndTime = 0;
        private readonly int cooldown;
        private readonly int duration;

        // data
        private readonly string sprite;
        private readonly string spriteUpgraded;
        public bool IsUpgraded { get; set; } = false;
        public bool IsEnabled { get; set; } = false;
        public bool IsActiveInMission 
        {
            get
            {
                return _durationEndTime > Mission.Current.CurrentTime;
            }
        }
        public string CurrentSprite
        {
            get
            {
                return IsUpgraded? spriteUpgraded : sprite;
            }
        }
        public string Sprite
        {
            get
            {
                return sprite;
            }
        }
        public string SpriteUpgraded
        {
            get
            {
                return spriteUpgraded;
            }
        }
        public string Description { get { return new TextObject(description).ToString(); } }
        public string DescriptionUpgraded { get { return new TextObject(descriptionUpdated).ToString(); } }
        private readonly string description;
        private readonly string descriptionUpdated;
        private readonly string name;
        public string Name { get { return new TextObject(name).ToString(); } }
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
            IsUpgraded = false;
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
            //disabledReason = new TextObject("{=!}Enabled");
            if (IsOnCooldown())
            {
                //disabledReason = new TextObject("{=!}On cooldown");
                return true;
            }
            return false;
        }
        public void SetCoolDown()
        {
            _coolDownLeft = cooldown;
            _durationEndTime = Mission.Current.CurrentTime + duration; 
            _cooldown_end_time = Mission.Current.CurrentTime + _coolDownLeft + 0.8f; //Adjustment was needed for natural tick on UI
            _timer.Start();
        }
        public bool IsOnCooldown() => _timer.Enabled;
        public virtual bool CanActivate(Agent agent)
        {
            if (IsDisabled(agent))
            {
                return false;
            }
            if (!agent.IsActive() || agent.Health <= 0)
            {
                return false;
            }
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
        private void InvokeActions(AbilityData.ActionTrigger trigger, Dictionary<AbilityData.ActionTrigger, List<Delegate>> actionDict, params object[] par )
        {
            switch (trigger)
            {
                case AbilityData.ActionTrigger.OnActivate or AbilityData.ActionTrigger.OnDeactivate:
                    actionDict.TryGetValue(AbilityData.ActionTrigger.OnActivate, out List<Delegate>? actions);
                    if (actions == null) return;
                    actions.OfType<Action>().ToList().ForEach(a => a());
                    break;
                case AbilityData.ActionTrigger.OnTroopPreHit:
                    actionDict.TryGetValue(AbilityData.ActionTrigger.OnTroopPreHit, out actions);
                    if (actions == null) return;
                    var attacker = (Agent)par[0];
                    var victim = (Agent)par[1];
                    float[] additionalDamage = ((float[])par[2]);
                    float[] resistancePercentages = ((float[])par[3]);
                    foreach (var action in actions)
                        if (action is Action<Agent, Agent, float[], float[]> troopHitDelegate)
                            troopHitDelegate(attacker, victim, additionalDamage, resistancePercentages);
                    break;
                case AbilityData.ActionTrigger.OnAgentHit:
                    actionDict.TryGetValue(AbilityData.ActionTrigger.OnAgentHit, out actions);
                    if (actions == null) return;
                    attacker = (Agent)par[0];
                    victim = (Agent)par[1];
                    MissionWeapon weapon = (MissionWeapon)par[2];
                    Blow blow = (Blow)par[3];
                    AttackCollisionData collisionData = (AttackCollisionData)par[4];
                    foreach (var action in actions)
                        if (action is Action<Agent, Agent, MissionWeapon, Blow, AttackCollisionData> agentHitDelegate)
                            agentHitDelegate(attacker, victim, weapon, blow, collisionData);
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
