using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class CustomSettlementsCampaignBehavior : CampaignBehaviorBase
    {

        private List<RFCustomSettlement>? customSettlementComponents;
        internal static List<Settlement>? customSettlements;
        private static RFCustomSettlement currentSettlement;

        public CustomSettlementsCampaignBehavior()
        {
            if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.SettlementComponent is RFCustomSettlement settlement)
                currentSettlement = settlement;
        }
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
            starter.AddGameMenu("rf_settlement_start", "{=!}{RF_SETTLEMENT_MAIN_TEXT}", new OnInitDelegate(this.game_menu_rf_settlement_start_on_init), GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption("rf_settlement_start", "start", "{=!}{RF_SETTLEMENT_EXPLORE_TEXT}", new GameMenuOption.OnConditionDelegate(this.game_menu_rf_settlement_start_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_rf_settlement_start_on_consequence), false, -1, false);
            starter.AddGameMenuOption("rf_settlement_start", "wait", "{=!}{RF_SETTLEMENT_WAIT_START_TEXT}", new GameMenuOption.OnConditionDelegate(this.game_menu_rf_settlement_wait_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_rf_settlement_wait_on_consequence), false, -1, false);

            starter.AddGameMenuOption("rf_settlement_start", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_leave_on_consequence), true, -1, false);
            starter.AddWaitGameMenu("rf_settlement_wait_menu", "{=!}{RF_SETTLEMENT_WAIT_TEXT}", delegate(MenuCallbackArgs args) { currentSettlement.StateHandler.OnSettlementWaitInit(args);  args.MenuContext.GameMenu.StartWait();}, new OnConditionDelegate(this.wait_menu_on_condition), new OnConsequenceDelegate(this.wait_menu_on_consequence), new OnTickDelegate(this.game_menu_wait_till_can_enter_menu_on_tick), GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption, GameOverlays.MenuOverlayType.None, 0, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption("rf_settlement_wait_menu", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_leave_on_consequence), true, -1, false);
        }

#pragma warning disable IDE1006 // Naming Styles
        private void wait_menu_on_consequence(MenuCallbackArgs args)
        {
            currentSettlement.StateHandler.OnSettlementWaitEndConsequence(args);
        }

        private bool wait_menu_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return currentSettlement.StateHandler.OnSettlementWaitEndCondition(args);
        }
        private bool game_menu_rf_settlement_wait_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return currentSettlement.StateHandler.OnSettlementWaitStartOnCondition(args);
        }

        private void game_menu_rf_settlement_wait_on_consequence(MenuCallbackArgs args)
        {
           GameMenu.SwitchToMenu("rf_settlement_wait_menu");
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
            return currentSettlement.StateHandler.OnSettlementLeaveCondition(args);
        }
        private void game_menu_rf_settlement_start_on_consequence(MenuCallbackArgs args)
        {
            currentSettlement.StateHandler.OnSettlementStartConsequence(args);
        }
        private bool game_menu_rf_settlement_start_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            return currentSettlement.StateHandler.OnSettlementStartCondition(args);
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
