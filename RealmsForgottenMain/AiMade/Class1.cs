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

namespace RealmsForgotten.AiMade
{
    internal class HouseTroopsTownsBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, ExampleConfig> _configs = new Dictionary<string, ExampleConfig>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _configs["town_EW3"] = new ExampleConfig("Hire Anorite Priests", new List<string> { "anorit_high_templar" });
            _configs["town_EM1"] = new ExampleConfig("Hire Red Mages", new List<string> { "red_mage" });
            AddGameMenus(starter);
        }

        private void AddGameMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenu("town_recruit_troops", "You approach the Castellan's chambers. You see him going over upkeep costs for the Town.", null, GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);

            starter.AddGameMenuOption("town_keep", "town_recruit_troops_option", "{=ADODRECRUITTROOPSTOWN}Visit the Castellan's Chambers", args => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return IsConfigAvailableForCurrentSettlement();
            }, args => {
                GameMenu.SwitchToMenu("town_recruit_troops");
            });

            foreach (var config in _configs)
            {
                starter.AddGameMenuOption("town_recruit_troops", "recruit_" + config.Key, config.Value.DisplayName,
                    args => {
                        args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                        return IsBuyTroopsOptionAvailable(config.Key);
                    },
                    args => ShowTroopPurchaseDialog(config.Key));
            }

            starter.AddGameMenuOption("town_recruit_troops", "town_recruit_troops_back", "{=ADODRECRUITBACKTOWN}Back to the keep", args => {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, args => {
                GameMenu.SwitchToMenu("town_keep");
            });
        }


        private bool IsConfigAvailableForCurrentSettlement()
        {
            Settlement currentSettlement = Settlement.CurrentSettlement;
            return currentSettlement != null && _configs.ContainsKey(currentSettlement.StringId);
        }

        private bool IsBuyTroopsOptionAvailable(string settlementId)
        {
            Settlement currentSettlement = Settlement.CurrentSettlement;
            return currentSettlement != null && currentSettlement.StringId == settlementId;
        }

        private void ShowTroopPurchaseDialog(string settlementId)
        {
            var troopIds = _configs[settlementId].TroopIds;
            var troops = troopIds.Select(MBObjectManager.Instance.GetObject<CharacterObject>).ToList();

            string title = new TextObject(_configs[settlementId].DisplayName, null).ToString();
            List<InquiryElement> options = troops.Select(troop => new InquiryElement(troop, troop.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(troop)))).ToList();

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, string.Empty, options, true, 1, 1, GameTexts.FindText("str_done", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), elements => OnTroopTypeSelected(elements, settlementId), null, "", false), false, false);
        }

        private void OnTroopTypeSelected(List<InquiryElement> elements, string settlementId)
        {
            CharacterObject troop = elements.FirstOrDefault()?.Identifier as CharacterObject;
            if (troop != null)
            {
                ShowTroopQuantitySelection(troop, settlementId);
            }
        }

        private void ShowTroopQuantitySelection(CharacterObject troop, string settlementId)
        {
            int maxQuantity = PartyBase.MainParty.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
            int troopCost = CalculateTroopCost(troop);

            InformationManager.ShowTextInquiry(new TextInquiryData("Select Quantity", $"How many {troop.Name}'s do you wish to recruit? Each costs {troopCost} Gold Dragons.", true, true, "Recruit", "Cancel", quantityText =>
            {
                if (int.TryParse(quantityText, out int quantity) && quantity > 0 && quantity <= maxQuantity)
                {
                    int totalCost = troopCost * quantity;
                    ConfirmTroopPurchase(troop, quantity, totalCost, settlementId);
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("You do not have enough space in your party to recruit all of these troops or you entered an Incorrect amount."));
                }
            }, null));
        }

        private void ConfirmTroopPurchase(CharacterObject troop, int quantity, int totalCost, string settlementId)
        {
            InformationManager.ShowInquiry(new InquiryData("Confirm Purchase", $"Are you sure you want to recruit {quantity} {troop.Name}(s) for {totalCost} Gold Dragons?", true, true, "Confirm", "Cancel", () =>
            {
                FinalizeTroopPurchase(troop, quantity, totalCost, settlementId);
            }, null));
        }

        private void FinalizeTroopPurchase(CharacterObject troop, int quantity, int totalCost, string settlementId)
        {
            if (Hero.MainHero.Gold >= totalCost)
            {
                MobileParty.MainParty.AddElementToMemberRoster(troop, quantity, false);
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, totalCost, false);
                InformationManager.DisplayMessage(new InformationMessage($"Recruited {quantity} {troop.Name}(s) for {totalCost} gold coins."));
                GameMenu.SwitchToMenu("town");
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Your coffers lack the Gold needed to recruit our soldiers."));
            }
        }

        private int CalculateTroopCost(CharacterObject troop)
        {
            return troop.Level * 10;
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Sync the _configs dictionary
            Dictionary<string, ExampleConfig> tempConfigs = _configs;
            dataStore.SyncData("HouseTroopsTownsBehavior_configs", ref tempConfigs);
            if (dataStore.IsLoading)
            {
                _configs = tempConfigs;
            }
          
        }
    }
}

