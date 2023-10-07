using RealmsForgotten.RFCustomSettlements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RealmsForgotten.RFCustomSettlements.CustomSettlementsCampaignBehavior;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.GameMenus;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using System.Text.RegularExpressions;

namespace RFCustomSettlements
{
    internal interface ISettlementStateHandler
    {
        void OnSettlementStartConsequence(MenuCallbackArgs args);
        bool OnSettlementStartCondition(MenuCallbackArgs args);
        void OnSettlementStartOnInit(MenuCallbackArgs args);
        bool OnSettlementLeaveCondition(MenuCallbackArgs args);
        bool OnSettlementWaitStartOnCondition(MenuCallbackArgs args);
        bool OnSettlementWaitEndCondition(MenuCallbackArgs args);
        void OnSettlementWaitEndConsequence(MenuCallbackArgs args);
        void OnWaitMenuTillEnterTick(MenuCallbackArgs args, CampaignTime dt);
        public void InitHandler(CustomSettlementBuildData buildData);
        public bool IsInitialized();
    }

    internal class ExploreSettlementStateHandler : ISettlementStateHandler
    {
        internal class NextSceneData
        {
            public enum RFExploreState
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
            internal RFExploreState currentState = RFExploreState.BeforeStart;

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
                currentState = RFExploreState.BeforeStart;
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
        private RFCustomSettlement currentSettlement;
        private CustomSettlementBuildData? CurrentBuildData;
        private float waitHours;
        private float waitProgress;
        public ExploreSettlementStateHandler(RFCustomSettlement settlement)
        {
            currentSettlement = settlement;
        }
        public void InitHandler(CustomSettlementBuildData buildData)
        {
            CurrentBuildData = buildData;
        }
        public bool IsInitialized()
        {
            return CurrentBuildData != null;
        }
        public bool OnSettlementStartCondition(MenuCallbackArgs args)
        {
            if (waitHours != 0) return false;
            if (CharacterObject.PlayerCharacter.HitPoints < 25)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=rf_too_wounded}You are too wounded to explore the area!", null);
            }
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            return true;
        }

        public void OnSettlementStartConsequence(MenuCallbackArgs args)
        {
            try
            {
                int playerMaximumTroopCount = currentSettlement.MaxPlayersideTroops;
                TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
                TroopRoster strongestAndPriorTroops = MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, playerMaximumTroopCount, true);
                troopRoster.Add(strongestAndPriorTroops);
                Campaign campaign = Campaign.Current;
                args.MenuContext.OpenTroopSelection(MobileParty.MainParty.MemberRoster, troopRoster, new Func<CharacterObject, bool>(this.CanChangeStatusOfTroop), new Action<TroopRoster>(this.OnTroopRosterManageDone), playerMaximumTroopCount, 1);
            }
            catch
            {
                RealmsForgotten.HuntableHerds.SubModule.PrintDebugMessage($"error in the settlement_bandits.xml, couldn't find {currentSettlement.CustomScene}");
            }
        }
        private void OnTroopRosterManageDone(TroopRoster roster)
        {
            RFMissions.StartExploreMission(currentSettlement.CustomScene, CurrentBuildData);
        }
        private bool CanChangeStatusOfTroop(CharacterObject character)
        {
            return !character.IsPlayerCharacter && !character.IsNotTransferableInHideouts;
        }
        public void OnSettlementStartOnInit(MenuCallbackArgs args)
        {
            string ? newSceneID;
            
            GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", $"{Settlement.CurrentSettlement.EncyclopediaText}");
            switch (NextSceneData.Instance.currentState)
            {
                case NextSceneData.RFExploreState.BeforeStart:
                    waitHours = 0;
                    waitProgress = 0;
                    float currentHourInDay = CampaignTime.Now.CurrentHourInDay;
                    if (currentHourInDay < currentSettlement.EnterStart || currentHourInDay > currentSettlement.EnterEnd)
                        CalculateSettlementExploreTime();
                    break;
                case NextSceneData.RFExploreState.SwitchScene:
                    newSceneID = NextSceneData.Instance.newSceneId;
                    try
                    {
                        CurrentBuildData = CustomSettlementBuildData.allCustomSettlementBuildDatas[newSceneID];
                        RFMissions.StartExploreMission(newSceneID, CurrentBuildData);
                        return;
                    }
                    catch
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Error trying to load scene: {newSceneID}"));
                    }
                    break;
                case NextSceneData.RFExploreState.Finished:
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
        }
        private void CalculateSettlementExploreTime()
        {
            waitHours = (currentSettlement.EnterStart > CampaignTime.Now.CurrentHourInDay) ? (currentSettlement.EnterStart - CampaignTime.Now.CurrentHourInDay) : (24f - CampaignTime.Now.CurrentHourInDay + currentSettlement.EnterStart);
        }

        public void OnWaitMenuTillEnterTick(MenuCallbackArgs args, CampaignTime dt)
        {
            waitProgress += (float)dt.ToHours;
            if (waitHours.ApproximatelyEqualsTo(0f, 1E-05f))
            {
                CalculateSettlementExploreTime();
            }
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(waitProgress / waitHours);
        }

        public bool OnSettlementLeaveCondition(MenuCallbackArgs args)
        {
            return MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty;
        }

        public bool OnSettlementWaitEndCondition(MenuCallbackArgs args)
        {
            return currentSettlement.CanEnterAnytime == false && waitHours != 0;
        }

        public bool OnSettlementWaitStartOnCondition(MenuCallbackArgs args)
        {
            return !CanEnter();
        }
        private bool CanEnter()
        {
            if (currentSettlement.CanEnterAnytime) return true;
            if (currentSettlement.EnterStart > currentSettlement.EnterEnd) return CampaignTime.Now.CurrentHourInDay >= currentSettlement.EnterStart || CampaignTime.Now.CurrentHourInDay <= currentSettlement.EnterEnd;
            else return CampaignTime.Now.CurrentHourInDay >= currentSettlement.EnterStart && CampaignTime.Now.CurrentHourInDay <= currentSettlement.EnterEnd;
        }

        public void OnSettlementWaitEndConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("rf_settlement_start");
        }
    }
    internal class ArenaSettlementStateHandler : ISettlementStateHandler
    {
        private CustomSettlementBuildData? CurrentBuildData;
        internal static ArenaState currentState = ArenaState.FightStage1;
        private RFCustomSettlement currentSettlement;
        private float waitHours;
        private bool hasToWait = false;
        private float waitProgress;
        public ArenaSettlementStateHandler(RFCustomSettlement settlement) => currentSettlement = settlement;
        public enum ArenaState
        {
            Visiting,
            Captured,
            FightStage1,
            FightStage2,
            FightStage3,
            Finishing
        }
        public void InitHandler(CustomSettlementBuildData buildData)
        {
            CurrentBuildData = buildData;
        }

        public bool IsInitialized()
        {
            return CurrentBuildData != null;
        }

        public bool OnSettlementStartCondition(MenuCallbackArgs args)
        {
            return !hasToWait;
        }

        public void OnSettlementStartConsequence(MenuCallbackArgs args)
        {
            RFMissions.OpenArenaMission("arena_test");
           // RFMissions.StartExploreMission(currentSettlement.CustomScene, CurrentBuildData);
        }

        public void OnSettlementStartOnInit(MenuCallbackArgs args)
        {
            GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "You are in the arena!");
            if (hasToWait) SetWaitTimer(24);
            switch(currentState)
            {
                case ArenaState.Visiting:
                    break; 
                case ArenaState.Captured:
                    break;
                case ArenaState.FightStage1:
                    //RFMissions.OpenArenaMission("arena_test");
                    break;
                case ArenaState.FightStage2:
                    //RFMissions.OpenArenaMission("arena_test");
                    break;
                case ArenaState.FightStage3:
                    //RFMissions.OpenArenaMission("arena_test");
                    break;
                case ArenaState.Finishing: 
                    break;
            }
        }

        public void OnWaitMenuTillEnterTick(MenuCallbackArgs args, CampaignTime dt)
        {
            waitProgress += (float)dt.ToHours;
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(waitProgress / waitHours);
        }

        internal void SetWaitTimer(int time)
        {
            waitHours = time; 
            hasToWait = true;
        }

        public bool OnSettlementLeaveCondition(MenuCallbackArgs args)
        {
            
            bool result = currentState == ArenaState.Visiting || currentState == ArenaState.Finishing;
            if (!result) args.IsEnabled = false;

            return true;
        }

        public bool OnSettlementWaitEndCondition(MenuCallbackArgs args)
        {
            return true;
        }

        public bool OnSettlementWaitStartOnCondition(MenuCallbackArgs args)
        {
            return hasToWait;
        }

        public void OnSettlementWaitEndConsequence(MenuCallbackArgs args)
        {
            hasToWait = false;
            waitProgress = 0;
            GameMenu.SwitchToMenu("rf_settlement_start");
        }
    }
}
