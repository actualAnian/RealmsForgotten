using RFCustomSettlements.Dialogues;
using RFCustomSettlements.Quests;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class CustomSettlementsCampaignBehavior : CampaignBehaviorBase
    {
        private List<RFCustomSettlement>? customSettlementComponents;
        internal static List<Settlement>? customSettlements;
        private static RFCustomSettlement? currentSettlement;
        public static Dictionary<string, QuestData> AllQuests { get; set; } = new();
        [SaveableField(1)]
        private static Dictionary<string, int> _dialogueStates = new();
        public static Dictionary<string, int> DialogueStates { get { return _dialogueStates; } }
        public CustomSettlementsCampaignBehavior()
        {
            if (MobileParty.MainParty != null && Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.SettlementComponent is RFCustomSettlement settlement)
                currentSettlement = settlement;
        }
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.FillSettlementList));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.FillSettlementList));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, () => {
                int a = 5;
                //test = new CustomSettlementQuest("test_1", () => { return false; });
                
            });
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddGameMenus(starter);
            AddDialogs(starter);
        }

        private void AddDialogs(CampaignGameStarter starter)
        {
            foreach(DialogueLine line in DialogueParser.allDialogues) 
            {
                if(line.IsPlayerLine)
                {
                    starter.AddPlayerLine(line.LineId + line.Text.First() + line.Text.GetHashCode().ToString(), line.InputId, line.GoToLineId, line.Text, line.Condition, line.Consequence, 100, null, null);
                }
                else starter.AddDialogLine(line.LineId + line.Text.First() + line.Text.GetHashCode().ToString(), line.InputId, line.GoToLineId, line.Text, line.Condition, line.Consequence, 100, null);
            }
            starter.AddDialogLine("Custom_Settlements_CrashSave", "start", "close_window", "I forgot what I was gonna say, report this to the mod authors.", delegate () {
            if (Mission.Current == null) return false;
            CustomSettlementMissionLogic? logic = Mission.Current.GetMissionBehavior<CustomSettlementMissionLogic>();
            return logic != null ;
            }, null, 100, null);
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
            starter.AddWaitGameMenu("rf_settlement_wait_menu", "{=!}{RF_SETTLEMENT_WAIT_TEXT}", delegate (MenuCallbackArgs args) { if (currentSettlement == null) return; currentSettlement.StateHandler.OnSettlementWaitInit(args); args.MenuContext.GameMenu.StartWait(); }, new OnConditionDelegate(this.wait_menu_on_condition), new OnConsequenceDelegate(this.wait_menu_on_consequence), new OnTickDelegate(this.game_menu_wait_till_can_enter_menu_on_tick), GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption, GameOverlays.MenuOverlayType.None, 0, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption("rf_settlement_wait_menu", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_leave_on_consequence), true, -1, false);
        }

#pragma warning disable IDE1006 // Naming Styles
        private void wait_menu_on_consequence(MenuCallbackArgs args)
        {
            if(currentSettlement == null) return;
            currentSettlement.StateHandler.OnSettlementWaitEndConsequence(args);
        }

        private bool wait_menu_on_condition(MenuCallbackArgs args)
        {
            if (currentSettlement == null) return false;
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return currentSettlement.StateHandler.OnSettlementWaitEndCondition(args);
        }
        private bool game_menu_rf_settlement_wait_on_condition(MenuCallbackArgs args)
        {
            if (currentSettlement == null) return false;
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return currentSettlement.StateHandler.OnSettlementWaitStartOnCondition(args);
        }

        private void game_menu_rf_settlement_wait_on_consequence(MenuCallbackArgs args)
        {
           GameMenu.SwitchToMenu("rf_settlement_wait_menu");
        }
        private void game_menu_wait_till_can_enter_menu_on_tick(MenuCallbackArgs args, CampaignTime dt)
        {
            if (currentSettlement == null) return;
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
            if (currentSettlement == null) return false;
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return currentSettlement.StateHandler.OnSettlementLeaveCondition(args);
        }
        private void game_menu_rf_settlement_start_on_consequence(MenuCallbackArgs args)
        {
            if (currentSettlement == null) return;
            currentSettlement.StateHandler.OnSettlementStartConsequence(args);
        }
        private bool game_menu_rf_settlement_start_on_condition(MenuCallbackArgs args)
        {
            if (currentSettlement == null) return false;
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            return currentSettlement.StateHandler.OnSettlementStartCondition(args);
        }

        [GameMenuInitializationHandler("rf_settlement_start")]
        private void game_menu_rf_settlement_start_on_init(MenuCallbackArgs args)
        {
            currentSettlement = (RFCustomSettlement)Settlement.CurrentSettlement.SettlementComponent;

            if (!currentSettlement.StateHandler.IsInitialized())
                try
                {
                    if (CustomSettlementBuildData.allCustomSettlementBuildDatas.ContainsKey(currentSettlement.CustomScene))
                        currentSettlement.StateHandler.InitHandler(CustomSettlementBuildData.allCustomSettlementBuildDatas[currentSettlement.CustomScene]);
                }
                catch(Exception)
                {
                    HuntableHerds.SubModule.PrintDebugMessage("Error loading the data for this settlement");
                    PlayerEncounter.LeaveSettlement();
                }
            currentSettlement.StateHandler.OnSettlementStartOnInit(args);

            args.MenuContext.SetBackgroundMeshName(currentSettlement.BackgroundMeshName);
        }
#pragma warning restore IDE1006 // Naming Styles
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("custSetDialStates", ref _dialogueStates);
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
