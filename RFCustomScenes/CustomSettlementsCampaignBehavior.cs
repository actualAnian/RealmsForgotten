using Helpers;
using RFCustomSettlements;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class CustomSettlementsCampaignBehavior : CampaignBehaviorBase
    {
        private List<RFCustomSettlement> customSettlementComponents;
        private List<Settlement>? customSettlements;

        internal static ItemRoster itemLoot;
        internal static int goldLoot;
        internal static bool finishedMission;
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.FillSettlementList));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.AddGameMenus));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.FillSettlementList));
        }

        private void FillSettlementList(CampaignGameStarter starter)
        {
            customSettlements = (from x in Campaign.Current.Settlements
                                    where x.SettlementComponent != null && x.SettlementComponent is RFCustomSettlement
                                      select x).ToList<Settlement>();
        }

        private void AddGameMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenu("rf_settlement_start", "{=rf_settlement_start}What's this???", new OnInitDelegate(this.game_menu_rf_settlement_start_on_init), GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption("rf_settlement_start", "explore", "{=rf_explore}Explore", new GameMenuOption.OnConditionDelegate(this.game_menu_rf_settlement_explore_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_rf_settlement_explore_on_consequence), false, -1, false);
            starter.AddGameMenuOption("rf_settlement_start", "wait", "{=zEoHYEUS}Wait here for some time", new GameMenuOption.OnConditionDelegate(this.game_menu_rf_settlement_wait_on_condition), delegate (MenuCallbackArgs x)
            {
                GameMenu.SwitchToMenu("rf_settlement_wait_menu");
            }, false, -1, false, null); 

            starter.AddGameMenuOption("rf_settlement_start", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_ruin_leave_on_consequence), true, -1, false);
            starter.AddWaitGameMenu("rf_settlement_wait_menu", "{=rf_wait_menu}You are waiting", new OnInitDelegate(this.game_menu_rf_settlement_wait_menu_on_init), new OnConditionDelegate(this.game_menu_rf_settlement_wait_menu_on_condition), null, null, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 0f, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption("rf_settlement_wait_menu", "rf_wait_leave", "{=UqDNAZqM}Stop waiting", new GameMenuOption.OnConditionDelegate(this.back_on_condition), delegate (MenuCallbackArgs args)
            {
                PlayerEncounter.Current.IsPlayerWaiting = false;

                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                GameMenu.SwitchToMenu("rf_settlement_start");

            }, true, -1, false, null);
        }
        private bool back_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private bool game_menu_rf_settlement_wait_menu_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return true;
        }

        [GameMenuInitializationHandler("rf_settlement_wait_menu")]
        private void game_menu_rf_settlement_wait_menu_on_init(MenuCallbackArgs args)
        {
            args.MenuContext.GameMenu.StartWait();
            //UpdateMenuLocations();
            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Current.IsPlayerWaiting = true;
            }
        }

        private static void UpdateMenuLocations()
        {
            // is here to add a possibility of dynamically changing ui
        }

        [GameMenuEventHandler("rf_settlement_start", "leave", GameMenuEventHandler.EventType.OnConsequence)]
        private void game_menu_ruin_leave_on_consequence(MenuCallbackArgs args)
        {
            PlayerEncounter.LeaveSettlement();
            PlayerEncounter.Finish(true);
            Campaign.Current.SaveHandler.SignalAutoSave();
        }

        private bool leave_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty;
        }
        private void game_menu_rf_settlement_explore_on_consequence(MenuCallbackArgs args)
        {
            var rfSettlement = Settlement.CurrentSettlement.SettlementComponent as RFCustomSettlement;
            if(rfSettlement != null) CustomSettlementMission.StartCustomSettlementMission(rfSettlement.CustomScene);
        }

        private bool game_menu_rf_settlement_wait_on_condition(MenuCallbackArgs args)
        {
            bool shouldBeDisabled;
            TextObject disabledText;
            bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.WaitInSettlement, out shouldBeDisabled, out disabledText);
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
        }

        private bool game_menu_rf_settlement_explore_on_condition(MenuCallbackArgs args)
        {
            bool result;
            if (Settlement.CurrentSettlement.SettlementComponent is RFCustomSettlement settlementComponent)
            {
                if (CharacterObject.PlayerCharacter.HitPoints < 25)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=rf_too_wounded}You are too wounded to explore the ruin!", null);
                }
                if (settlementComponent.IsRaided)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=rf_raided}You were here just a little while ago. There is nothing left to find, you should come back later.", null);
                }
                args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                result = true;
            }
            else
            {
                result = false;
            }
            return result;

        }

        [GameMenuInitializationHandler("rf_settlement_start")]
        private void game_menu_rf_settlement_start_on_init(MenuCallbackArgs args)
        {
            if (Settlement.CurrentSettlement.SettlementComponent == null || Settlement.CurrentSettlement.SettlementComponent is not RFCustomSettlement) return;
  
            if(finishedMission)
            {
                finishedMission = false;
                Hero.MainHero.ChangeHeroGold(goldLoot);
                TextObject goldText = new TextObject("Total Gold Loot: {CHANGE}{GOLD_ICON}", null);
                goldText.SetTextVariable("CHANGE", goldLoot);
                goldText.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");

                InformationManager.DisplayMessage(new InformationMessage(goldText.ToString(), "event:/ui/notification/coins_positive"));
                InventoryManager.OpenScreenAsReceiveItems(itemLoot, new TextObject("Loot"), null);
            }
            //this.currentRuin = (Settlement.CurrentSettlement.SettlementComponent as RFCustomSettlement);
            //GameTexts.SetVariable("RUIN_TEXT", this.currentRuin.Settlement.EncyclopediaText);
            //if (MobileParty.MainParty.CurrentSettlement != null)
            //{
            //    PlayerEncounter.LeaveSettlement();
            //}
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                customSettlementComponents = (from Settlement settlement in customSettlements
                                              select (RFCustomSettlement)settlement.SettlementComponent).ToList();
            }

            dataStore.SyncData<List<RFCustomSettlement>>("ruinComponents", ref customSettlementComponents);
        }
    }
}
