using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class CustomSettlementsCampaignBehavior : CampaignBehaviorBase
    {

        private List<RFCustomSettlement>? customSettlementComponents;
        private List<Settlement>? customSettlements;
        private static RFCustomSettlement? currentSettlement;

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
            starter.AddGameMenu("rf_settlement_start", "{=rf_settlement_start} You came across a captivating location!", new OnInitDelegate(this.game_menu_rf_settlement_start_on_init), GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption("rf_settlement_start", "explore", "{=rf_explore}Explore", new GameMenuOption.OnConditionDelegate(this.game_menu_rf_settlement_explore_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_rf_settlement_explore_on_consequence), false, -1, false);
            starter.AddGameMenuOption("rf_settlement_start", "prepare", "{=rf_explore}Prepare", new GameMenuOption.OnConditionDelegate(this.game_menu_rf_settlement_prepare_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_rf_settlement_prepare_on_consequence), false, -1, false);

            starter.AddGameMenuOption("rf_settlement_start", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_leave_on_consequence), true, -1, false);
            starter.AddWaitGameMenu("rf_settlement_wait_till_can_enter_menu", "Waiting until nightfall", delegate(MenuCallbackArgs args) { args.MenuContext.GameMenu.StartWait(); }, delegate (MenuCallbackArgs args) { 
                return currentSettlement.CanEnterAnytime == false;
            }, delegate (MenuCallbackArgs args) { GameMenu.SwitchToMenu("rf_settlement_start"); }, new OnTickDelegate(this.game_menu_wait_till_can_enter_menu_on_tick), GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption, GameOverlays.MenuOverlayType.None, 0, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption("rf_settlement_wait_till_can_enter_menu", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_leave_on_consequence), true, -1, false);
        }
#pragma warning disable IDE1006 // Naming Styles
        private bool game_menu_rf_settlement_prepare_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return !CanEnter();
        }

        private void game_menu_rf_settlement_prepare_on_consequence(MenuCallbackArgs args)
        {
           GameMenu.SwitchToMenu("rf_settlement_wait_till_can_enter_menu");
        }
        private void game_menu_wait_till_can_enter_menu_on_tick(MenuCallbackArgs args, CampaignTime dt)
        {
            currentSettlement.StateHandler.OnWaitMenuTillEnterTick(args, dt);
        }
        //private static void UpdateMenuLocations()
        //{
        //    // is here to add a possibility of dynamically changing ui
        //}

        [GameMenuEventHandler("rf_settlement_start", "leave", GameMenuEventHandler.EventType.OnConsequence)]
        private void game_menu_leave_on_consequence(MenuCallbackArgs args)
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
            currentSettlement.StateHandler.OnSettlementExploreConsequence(args);
        }
        private bool game_menu_rf_settlement_explore_on_condition(MenuCallbackArgs args)
        {
            if (!CanEnter()) return false;

            return currentSettlement.StateHandler.OnSettlementExploreCondition(args);
        }
        private bool CanEnter()
        {
            if (currentSettlement.CanEnterAnytime) return true;
            if (currentSettlement.EnterStart > currentSettlement.EnterEnd) return CampaignTime.Now.CurrentHourInDay >= currentSettlement.EnterStart || CampaignTime.Now.CurrentHourInDay <= currentSettlement.EnterEnd;
            else return CampaignTime.Now.CurrentHourInDay >= currentSettlement.EnterStart && CampaignTime.Now.CurrentHourInDay <= currentSettlement.EnterEnd;
        }

        [GameMenuInitializationHandler("rf_settlement_start")]
        private void game_menu_rf_settlement_start_on_init(MenuCallbackArgs args)
        {
            currentSettlement = (RFCustomSettlement)Settlement.CurrentSettlement.SettlementComponent;
            if (!currentSettlement.StateHandler.IsInitialized()) 
                currentSettlement.StateHandler.InitHandler(CustomSettlementBuildData.allCustomSettlementBuildDatas[currentSettlement.CustomScene]);
            currentSettlement.StateHandler.OnSettlementStartOnInit(args);

            args.MenuContext.SetBackgroundMeshName(currentSettlement.BackgroundMeshName);
        }
#pragma warning restore IDE1006 // Naming Styles
        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                customSettlementComponents = (from Settlement settlement in customSettlements
                                              select (RFCustomSettlement)settlement.SettlementComponent).ToList();
            }
            if(customSettlementComponents != null)
                dataStore.SyncData<List<RFCustomSettlement>>("ruinComponents", ref customSettlementComponents);
        }
    }
}
