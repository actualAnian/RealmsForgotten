using TaleWorlds.Core;
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

namespace RealmsForgotten.RFEffects
{
    public class HealingPotionMissionBehavior : MissionBehavior
    {
        int soundIndex;
        ItemObject elixirObject;
        int elixirAmount;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public HealingPotionMissionBehavior(ItemRosterElement item)
        {
            soundIndex = SoundEvent.GetEventIdFromString("realmsforgotten/drink");
            elixirObject = item.EquipmentElement.Item;
            elixirAmount = item.Amount;
        }
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (Input.IsKeyPressed(InputKey.Q) && elixirAmount > 0 && Agent.Main?.Health < 100)
            {
                DrinkElixir();
            }
        }

        public void DrinkElixir()
        {
            MobileParty.MainParty.ItemRoster.AddToCounts(elixirObject, -1);
            var ma = Agent.Main;
            var oldHealth = ma.Health;
            ma.Health += 20;
            if (ma.Health > ma.HealthLimit) ma.Health = ma.HealthLimit;
            var msg = new TextObject("{=yCLS6x8c04f1C}Healed for {HEAL_AMOUNT} HP").SetTextVariable("HEAL_AMOUNT", ma.Health - oldHealth);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            Mission.MakeSound(soundIndex, Vec3.Zero, false, true, -1, -1);
            elixirAmount--;


        }
    }
}