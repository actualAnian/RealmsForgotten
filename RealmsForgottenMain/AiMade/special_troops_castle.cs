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
    internal class ExampleConfig
    {
        public string DisplayName;
        public List<string> TroopIds;

        public ExampleConfig(string displayName, List<string> troopIds)
        {
            DisplayName = displayName;
            TroopIds = troopIds;
        }
    }
    internal class HouseTroopsCastleBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, ExampleConfig> _configs = new Dictionary<string, ExampleConfig>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _configs["castle_EW7"] = new ExampleConfig("Hire Anorite High Templars", new List<string> { "anorit_high_templar" });
            AddGameMenus(starter);
            _configs["castle_EM1"] = new ExampleConfig("Hire Red Mage Elite", new List<string> { "red_mage_elite" });
            AddGameMenus(starter);
        }

        private void AddGameMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenu("castle_recruit_troops", "You approach the Castellan's Chambers. You see him going over upkeep costs for the Castle.", null, GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);

            starter.AddGameMenuOption("castle", "castle_recruit_troops_option", "{=ADODRECRUITTROOPSCASTLE}Visit the Castellan's Chambers", args => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return IsConfigAvailableForCurrentSettlement();
            }, args => {
                GameMenu.SwitchToMenu("castle_recruit_troops");
            });

            foreach (var config in _configs)
            {
                starter.AddGameMenuOption("castle_recruit_troops", "recruit_" + config.Key, config.Value.DisplayName,
                    args => {
                        args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                        return IsBuyTroopsOptionAvailable(config.Key);
                    },
                    args => ShowTroopPurchaseDialog(config.Key));
            }

            starter.AddGameMenuOption("castle_recruit_troops", "castle_recruit_troops_back", "{=ADODRECRUITBACKCASTLE}Back to the courtyard", args => {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, args => {
                GameMenu.SwitchToMenu("castle");
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

            InformationManager.ShowTextInquiry(new TextInquiryData("Select Quantity", $"How many {troop.Name}'s do you wish to recruit? Each costs {troopCost} gold coins.", true, true, "Recruit", "Cancel", quantityText =>
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
            InformationManager.ShowInquiry(new InquiryData("Confirm Purchase", $"Are you sure you want to recruit {quantity} {troop.Name}(s) for {totalCost} gold coins?", true, true, "Confirm", "Cancel", () =>
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
                GameMenu.SwitchToMenu("castle");
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Your coffers lack the gold needed to recruit our soldiers."));
            }
        }

        private int CalculateTroopCost(CharacterObject troop)
        {
            return troop.Level * 10;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}