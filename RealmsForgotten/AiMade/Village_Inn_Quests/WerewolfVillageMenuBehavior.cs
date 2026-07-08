using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Village_Inn_Quests
{
    public class WerewolfVillageMenuBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            AddGameMenus(campaignGameStarter);
        }

        private void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption(
                "village",
                "rf_werewolf_confront_option",
                "Confront the beast (at night)",
                Condition_ShowAndEnable,
                Consequence_Start,
                false,
                -1,
                false
            );
        }

        private bool Condition_ShowAndEnable(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Default;

            bool isNight = Campaign.Current.IsNight;
            bool isVillage = Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsVillage;

            return isNight && isVillage;
        }

        private void Consequence_Start(MenuCallbackArgs args)
        {
            try
            {
                // Get the werewolf character
                CharacterObject werewolfCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("werewolf");

                if (werewolfCharacter == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Error: Werewolf character not found", Colors.Red));
                    return;
                }

                // Create troops directly without creating a mobile party
                TroopRoster enemyRoster = new TroopRoster(null);

                // Add werewolves based on player strength
                int werewolfCount = MBRandom.RandomInt(3, 8);
                int playerStrength = MobileParty.MainParty.Party.TotalStrength;

                if (playerStrength > 100)
                {
                    werewolfCount = MBRandom.RandomInt(5, 10);
                }

                enemyRoster.AddToCounts(werewolfCharacter, werewolfCount);

                // Start battle directly without creating a mobile party
                PlayerEncounter.Start();
                PlayerEncounter.Current.SetupFields(PartyBase.MainParty, null);

                // Create a temporary party just for the battle
                PartyBase enemyParty = PartyBase.CreateParty("werewolf_encounter_" + MBRandom.RandomInt(1000000));
                enemyParty.MemberRoster.Clear();

                foreach (TroopRosterElement element in enemyRoster.GetTroopRoster())
                {
                    enemyParty.MemberRoster.AddToCounts(element.Character, element.Number);
                }

                PlayerEncounter.Current.SetupFields(PartyBase.MainParty, enemyParty);
                PlayerEncounter.StartBattle();

                InformationManager.DisplayMessage(new InformationMessage("The werewolves emerge from the shadows!", Colors.Red));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error: {ex.Message}", Colors.Red));
            }
        }
    }
}