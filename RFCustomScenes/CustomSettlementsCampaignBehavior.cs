using Helpers;
using RealmsForgotten.HuntableHerds;
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
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class CustomSettlementsCampaignBehavior : CampaignBehaviorBase
    {
        internal class NextSceneData
        {
            public enum RFSettlementState
            {
                BeforeStart,
                SwitchScene,
                Finished
            }
            internal ItemRoster? itemLoot;
            internal int goldLoot;
            internal static NextSceneData? _instance;
            internal bool shouldSwitchScenes = false;
            internal string? newSceneId;
            internal TroopRoster? playerTroopRoster;
            internal RFSettlementState currentState = RFSettlementState.BeforeStart;

            public static NextSceneData Instance
            {
                get
                {
                    _instance ??= new NextSceneData();
                    return _instance;
                }
            }

            internal void ResetData()
            {
                goldLoot = 0;
                itemLoot = new();
                shouldSwitchScenes = false;
                playerTroopRoster = TroopRoster.CreateDummyTroopRoster();
                newSceneId = null;
                currentState = RFSettlementState.BeforeStart;
            }

            internal void OnTroopKilled(CharacterObject character)
            {
                playerTroopRoster?.RemoveTroop(character, 1);
            }

            internal void OnTroopWounded(CharacterObject character)
            {
                playerTroopRoster?.RemoveTroop(character, 1);

            }
        }
        private List<RFCustomSettlement>? customSettlementComponents;
        private List<Settlement>? customSettlements;
        private static CustomSettlementBuildData? CurrentBuildData;
        private static RFCustomSettlement? currentSettlement;


        private readonly int NightStart = 23;
        private readonly int NightEnd = 2;
        private float waitHours;
        private float waitProgress;
        private bool isNightScene = true;
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
            starter.AddWaitGameMenu("rf_settlement_wait_till_night_menu", "Waiting until nightfall", delegate(MenuCallbackArgs args) { args.MenuContext.GameMenu.StartWait(); }, delegate (MenuCallbackArgs args) { 
                return currentSettlement.canEnterAnytime == false;
            }, delegate (MenuCallbackArgs args) { GameMenu.SwitchToMenu("rf_settlement_start"); }, new OnTickDelegate(this.game_menu_wait_till_night_menu_on_tick), GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption, GameOverlays.MenuOverlayType.None, waitHours, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption("rf_settlement_wait_till_night_menu", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_ruin_leave_on_consequence), true, -1, false);
        }
        private bool game_menu_rf_settlement_prepare_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return !CanEnter();
        }

        private void game_menu_rf_settlement_prepare_on_consequence(MenuCallbackArgs args)
        {
           GameMenu.SwitchToMenu("rf_settlement_wait_till_night_menu");
        }
        private void game_menu_wait_till_night_menu_on_tick(MenuCallbackArgs args, CampaignTime dt)
        {
            waitProgress += (float)dt.ToHours;
            if (waitHours.ApproximatelyEqualsTo(0f, 1E-05f))
            {
                CalculateSettlementExploreTime();
            }
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(waitProgress / waitHours);
        }

#pragma warning disable IDE1006 // Naming Styles
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
            RFCustomSettlement curSettlement;
            if (Settlement.CurrentSettlement.SettlementComponent == null || (curSettlement = ((RFCustomSettlement)Settlement.CurrentSettlement.SettlementComponent)) == null) return;
            args.MenuContext.SetBackgroundMeshName(curSettlement.WaitMeshName);
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
            RFCustomSettlement? rfSettlement;
            if ((rfSettlement = Settlement.CurrentSettlement.SettlementComponent as RFCustomSettlement) == null  || rfSettlement.CustomScene == null) return;
            try
            {
                int playerMaximumTroopCount = CurrentBuildData.maxPlayersideTroops;
                TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
                TroopRoster strongestAndPriorTroops = MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, playerMaximumTroopCount, true);
                troopRoster.Add(strongestAndPriorTroops);
                Campaign campaign = Campaign.Current;
                args.MenuContext.OpenTroopSelection(MobileParty.MainParty.MemberRoster, troopRoster, new Func<CharacterObject, bool>(this.CanChangeStatusOfTroop), new Action<TroopRoster>(this.OnTroopRosterManageDone), playerMaximumTroopCount, 1);
            }
            catch
            {
                HuntableHerds.SubModule.PrintDebugMessage($"error in the settlement_bandits.xml, couldn't find {rfSettlement.CustomScene}");
            }
        }

        private void OnTroopRosterManageDone(TroopRoster roster)
        {
            if (Settlement.CurrentSettlement.SettlementComponent is RFCustomSettlement rfSettlement && rfSettlement.CustomScene != null)
            {
                NextSceneData.Instance.playerTroopRoster = roster;
                CustomSettlementMission.StartCustomSettlementMission(rfSettlement.CustomScene, CurrentBuildData);
            }
        }

        private bool CanChangeStatusOfTroop(CharacterObject character)
        {
            return !character.IsPlayerCharacter && !character.IsNotTransferableInHideouts;
        }
        private void CalculateSettlementExploreTime()
        {
            waitHours = (currentSettlement.enterStart > CampaignTime.Now.CurrentHourInDay) ? (currentSettlement.enterStart - CampaignTime.Now.CurrentHourInDay) : (24f - CampaignTime.Now.CurrentHourInDay + currentSettlement.enterStart);
        }

        private bool game_menu_rf_settlement_wait_on_condition(MenuCallbackArgs args)
        {
            bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.WaitInSettlement, out bool shouldBeDisabled, out TextObject disabledText);
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;

            return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
        }

        private bool game_menu_rf_settlement_explore_on_condition(MenuCallbackArgs args)
        {
            if (!CanEnter()) return false;
            if (CharacterObject.PlayerCharacter.HitPoints < 25)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=rf_too_wounded}You are too wounded to explore the area!", null);
            }
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            return true;
        }

        private bool CanEnter()
        {
            if (currentSettlement.canEnterAnytime) return true;
            if (currentSettlement.enterStart > currentSettlement.enterEnd) return CampaignTime.Now.CurrentHourInDay >= currentSettlement.enterStart || CampaignTime.Now.CurrentHourInDay <= currentSettlement.enterEnd;
            else return CampaignTime.Now.CurrentHourInDay >= currentSettlement.enterStart && CampaignTime.Now.CurrentHourInDay <= currentSettlement.enterEnd;

        }

        [GameMenuInitializationHandler("rf_settlement_start")]
        private void game_menu_rf_settlement_start_on_init(MenuCallbackArgs args)
        {

            currentSettlement = (RFCustomSettlement)Settlement.CurrentSettlement.SettlementComponent;
            CurrentBuildData = CustomSettlementBuildData.allCustomSettlementBuildDatas[currentSettlement.CustomScene];


            string? newSceneID;

            switch(NextSceneData.Instance.currentState)
            {
                case NextSceneData.RFSettlementState.BeforeStart:
                    waitHours = 0;
                    waitProgress = 0;
                    float currentHourInDay = CampaignTime.Now.CurrentHourInDay;
                    if (currentHourInDay < currentSettlement.enterStart || currentHourInDay > currentSettlement.enterEnd)
                        CalculateSettlementExploreTime();
                    break;
                case NextSceneData.RFSettlementState.SwitchScene:
                    newSceneID = NextSceneData.Instance.newSceneId;
                    try
                    {
                        CurrentBuildData = CustomSettlementBuildData.allCustomSettlementBuildDatas[newSceneID];
                        CustomSettlementMission.StartCustomSettlementMission(newSceneID, CurrentBuildData);
                        return;
                    }
                    catch
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Error trying to load scene: {newSceneID}"));
                    }
                    break;
                case NextSceneData.RFSettlementState.Finished:
                    if (NextSceneData.Instance.goldLoot > 0)
                    {
                        Hero.MainHero.ChangeHeroGold(NextSceneData.Instance.goldLoot);
                        TextObject goldText = new("Total Gold Loot: {CHANGE}{GOLD_ICON}", null);
                        goldText.SetTextVariable("CHANGE", NextSceneData.Instance.goldLoot);
                        goldText.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");

                        InformationManager.DisplayMessage(new InformationMessage(goldText.ToString(), "event:/ui/notification/coins_positive"));
                    }
                    if (!NextSceneData.Instance.itemLoot.IsEmpty())
                        InventoryManager.OpenScreenAsReceiveItems(NextSceneData.Instance.itemLoot, new TextObject("Loot"), null);
                    NextSceneData.Instance.ResetData();
                    break;
            }
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
