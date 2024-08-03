using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.Module1
{
    internal class TavernRecruitmentBehavior : CampaignBehaviorBase
    {
        // Recruitment settings
        private readonly string tavernMenuId = "town_backstreet";
        private Dictionary<string, List<string>> cultureTroopMap;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Initialize culture-to-troop mapping
            InitializeCultureTroopMap();

            // Add the recruitment option directly to the tavern
            AddTavernRecruitmentOption(starter);
        }

        private void InitializeCultureTroopMap()
        {
            cultureTroopMap = new Dictionary<string, List<string>>
            {
                { "empire", new List<string> { "legion_of_the_betrayed_tier_1", "hidden_hand_tier_1", "embers_of_flame_tier_1", "eleftheroi_tier_1" } },
                { "aserai", new List<string> { "jawwal_tier_1", "ghilman_tier_1", "beni_zilal_tier_1" } },
                { "battania", new List<string> { "wolfskins_tier_1", "forest_elveans_tier_1" } },
                { "sturgia", new List<string> { "skolderbrotva_tier_1", "lakepike_tier_1", "forest_people_tier_1" } },
                { "khuzait", new List<string> { "karakhuzaits_tier_1" } },
                { "vlandia", new List<string> { "company_of_the_boar_tier_1", "brotherhood_of_woods_tier_1" } },
                { "giant", new List<string> { "jawwal_tier_1", "jawwal_tier_1" } },
                { "aqarun", new List<string> { "jawwal_tier_1", "jawwal_tier_1" } },
                { "mage", new List<string> { "jawwal_tier_1", "jawwal_tier_1" } },
                // Add more culture mappings here
            };
        }

        private void AddTavernRecruitmentOption(CampaignGameStarter starter)
        {
            // Add the mercenary recruitment option directly within the tavern
            starter.AddGameMenuOption(tavernMenuId, "recruit_mercenaries", "{=HIREMERCENARIES}Hire Mercenaries",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    return true;
                },
                args => ShowMercenaryPurchaseDialog(Settlement.CurrentSettlement.Culture.StringId));

            // Add a back option to return to the main town menu
            starter.AddGameMenuOption(tavernMenuId, "back_to_town", "{=BACKTOTOWN}Back to the town", args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, args => GameMenu.SwitchToMenu("town"));
        }

        private void ShowMercenaryPurchaseDialog(string cultureId)
        {
            var troopIds = cultureTroopMap.ContainsKey(cultureId) ? cultureTroopMap[cultureId] : new List<string> { "default_troop_id" };
            var troops = troopIds.Select(MBObjectManager.Instance.GetObject<CharacterObject>).ToList();

            string title = new TextObject("Hire Mercenaries", null).ToString();
            List<InquiryElement> options = troops.Select(troop => new InquiryElement(troop, troop.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(troop)))).ToList();

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, string.Empty, options, true, 1, 1, GameTexts.FindText("str_done", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), elements => OnMercenaryTypeSelected(elements), null, "", false), false, false);
        }

        private void OnMercenaryTypeSelected(List<InquiryElement> elements)
        {
            CharacterObject troop = elements.FirstOrDefault()?.Identifier as CharacterObject;
            if (troop != null)
            {
                ShowMercenaryQuantitySelection(troop);
            }
        }

        private void ShowMercenaryQuantitySelection(CharacterObject troop)
        {
            int maxQuantity = PartyBase.MainParty.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
            int troopCost = CalculateTroopCost(troop);

            InformationManager.ShowTextInquiry(new TextInquiryData("Select Quantity", $"How many {troop.Name}'s do you wish to recruit? Each costs {troopCost} gold coins.", true, true, "Recruit", "Cancel", quantityText =>
            {
                if (int.TryParse(quantityText, out int quantity) && quantity > 0 && quantity <= maxQuantity)
                {
                    int totalCost = troopCost * quantity;
                    ConfirmMercenaryPurchase(troop, quantity, totalCost);
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("You do not have enough space in your party to recruit all of these troops or you entered an incorrect amount."));
                }
            }, null));
        }

        private void ConfirmMercenaryPurchase(CharacterObject troop, int quantity, int totalCost)
        {
            InformationManager.ShowInquiry(new InquiryData("Confirm Purchase", $"Are you sure you want to recruit {quantity} {troop.Name}(s) for {totalCost} gold coins?", true, true, "Confirm", "Cancel", () =>
            {
                FinalizeMercenaryPurchase(troop, quantity, totalCost);
            }, null));
        }

        private void FinalizeMercenaryPurchase(CharacterObject troop, int quantity, int totalCost)
        {
            if (Hero.MainHero.Gold >= totalCost)
            {
                MobileParty.MainParty.AddElementToMemberRoster(troop, quantity, false);
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, totalCost, false);
                InformationManager.DisplayMessage(new InformationMessage($"Recruited {quantity} {troop.Name}(s) for {totalCost} gold coins."));
                GameMenu.SwitchToMenu(tavernMenuId);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Your coffers lack the gold needed to recruit these soldiers."));
            }
        }

        private int CalculateTroopCost(CharacterObject troop)
        {
            return troop.Level * 10;
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Sync cultureTroopMap
            Dictionary<string, List<string>> tempCultureTroopMap = cultureTroopMap;
            dataStore.SyncData("cultureTroopMap", ref tempCultureTroopMap);
            if (dataStore.IsLoading)
            {
                cultureTroopMap = tempCultureTroopMap;
            }

            // Add logging
            InformationManager.DisplayMessage(new InformationMessage($"SyncData called for {this.GetType().Name}"));
        }
    }
}

