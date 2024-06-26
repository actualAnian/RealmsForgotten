﻿using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.InputSystem;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using SandBox.GameComponents;

namespace RealmsForgotten.Behaviors
{
    
    public class PotionsMissionBehavior : MissionBehavior
    {
        private int soundIndex;
        public static bool berserkerMode;
        
        private ItemRosterElement elixir;
        private ItemRosterElement berserker;
        private (int, int, int, int) oldSkillsValues;
        private Timer timer;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public PotionsMissionBehavior(ItemRosterElement elixirPotion, ItemRosterElement berserkerPotion)
        {
            soundIndex = SoundEvent.GetEventIdFromString("realmsforgotten/drink");

            timer = new(Time.ApplicationTime, 0f, false);
            elixir = elixirPotion;
            berserker = berserkerPotion;
            berserkerMode = false;

        }
        protected override void OnEndMission()
        {
            base.OnEndMission();
            if(berserkerMode)
                GiveBerserkerSkills(0, false);

        }
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Agent.Main == null)
                return;
            if (elixir.Amount > 0 && Input.IsKeyReleased(SubModule.Instance.KeysConfig[nameof(CustomSettings.UseHealKey)]) && Agent.Main.Health < 100)
            {
                DrinkElixir();
            }

            if (berserker.Amount > 0 && Input.IsKeyReleased(SubModule.Instance.KeysConfig[nameof(CustomSettings.UseBerserkerKey)]))
            {
                if(timer.Check(Time.ApplicationTime))
                {
                    DrinkBerserker();
                    timer = new(Time.ApplicationTime, 45f, false);
                }
                else
                {
                    var msg = new TextObject("{=berserker_warning}You're already on berserker mode!");
                    InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
                }
            }
            if(berserkerMode && timer.Check(Time.ApplicationTime))
            {
                GiveBerserkerSkills(0, false);
                var msg = new TextObject("{=berserker_deactivated}Berserker mode deactivated!");
                InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Color.FromUint(0xFFFF0000)));
                berserkerMode = false;
            }
        }
        private void GiveBerserkerSkills(int value, bool isActivating = true)
        {
            var ma = Agent.Main;
            if (ma == null)
                return;
            CharacterObject character = ma.Character as CharacterObject;
            Hero mainHero = character.HeroObject;

            if(isActivating)
                oldSkillsValues = (mainHero.GetSkillValue(DefaultSkills.OneHanded), mainHero.GetSkillValue(DefaultSkills.TwoHanded), mainHero.GetSkillValue(DefaultSkills.Polearm), mainHero.GetSkillValue(DefaultSkills.Athletics));
            
            mainHero.SetSkillValue(DefaultSkills.OneHanded, oldSkillsValues.Item1 + value);
            mainHero.SetSkillValue(DefaultSkills.TwoHanded, oldSkillsValues.Item2 + value); 
            mainHero.SetSkillValue(DefaultSkills.Polearm, oldSkillsValues.Item3 + value);
            mainHero.SetSkillValue(DefaultSkills.Athletics, oldSkillsValues.Item4 + value);

            ma.UpdateCustomDrivenProperties();
            ma.UpdateAgentStats();
            ma.UpdateAgentProperties();
        }
        private void DrinkBerserker()
        {
            MobileParty.MainParty.ItemRoster.AddToCounts(berserker.EquipmentElement.Item, -1);

            GiveBerserkerSkills(1000);

            var msg = new TextObject("{=berserker_activated}Berserker mode activated!");
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Color.FromUint(0xFFFF0000)));
            Mission.MakeSound(soundIndex, Vec3.Zero, false, true, -1, -1);
            berserker.Amount--;
            timer.Reset(Time.ApplicationTime);
            berserkerMode = true;
        }
        private void DrinkElixir()
        {
            MobileParty.MainParty.ItemRoster.AddToCounts(elixir.EquipmentElement.Item, -1);
            var ma = Agent.Main;
            var oldHealth = ma.Health;
            ma.Health += 20;
            if (ma.Health > ma.HealthLimit) ma.Health = ma.HealthLimit;
            var msg = new TextObject("{=heal_notification}Healed for {HEAL_AMOUNT} HP").SetTextVariable("HEAL_AMOUNT", ma.Health - oldHealth);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            Mission.MakeSound(soundIndex, Vec3.Zero, false, true, -1, -1);
            elixir.Amount--;


        }
    }
}