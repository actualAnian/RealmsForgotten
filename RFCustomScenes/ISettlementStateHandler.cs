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
    public interface ISettlementStateHandler
    {
        void OnSettlementStartConsequence(MenuCallbackArgs args);
        bool OnSettlementStartCondition(MenuCallbackArgs args);
        void OnSettlementStartOnInit(MenuCallbackArgs args);
        bool OnSettlementLeaveCondition(MenuCallbackArgs args);
        bool OnSettlementWaitStartOnCondition(MenuCallbackArgs args);
        bool OnSettlementWaitEndCondition(MenuCallbackArgs args);
        void OnSettlementWaitEndConsequence(MenuCallbackArgs args);
        void OnWaitMenuTillEnterTick(MenuCallbackArgs args, CampaignTime dt);
        void OnSettlementWaitInit(MenuCallbackArgs args);
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
            GameTexts.SetVariable("RF_SETTLEMENT_EXPLORE_TEXT", "Explore");
            if (waitHours != 0) return false;
            if (CharacterObject.PlayerCharacter.HitPoints < 25)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=rf_too_wounded}You are too wounded to explore the area!", null);
            }
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
            GameTexts.SetVariable("RF_SETTLEMENT_WAIT_START_TEXT", "Prepare");
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

        public void OnSettlementWaitInit(MenuCallbackArgs args)
        {
            GameTexts.SetVariable("RF_SETTLEMENT_WAIT_TEXT", "As the scouts tell you of the best approach time, you prepare for whatever danger might be inside");
        }
    }
    public class ArenaSettlementStateHandler : ISettlementStateHandler
    {
        private CustomSettlementBuildData? CurrentBuildData;
        internal ArenaState currentState = ArenaState.FightStage1;
        private static RFCustomSettlement currentSettlement;
        private float waitHours;
        private bool hasToWait = false;
        private float waitProgress;

        private bool? playerWon = null;

        private static readonly int arenaBattlesStartTime = 18;
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
            if(currentState == ArenaState.Captured) GameTexts.SetVariable("RF_SETTLEMENT_EXPLORE_TEXT", "Listen to the arena master.");
            else
                GameTexts.SetVariable("RF_SETTLEMENT_EXPLORE_TEXT", "Kill to stay alive");
            if (currentState == ArenaState.Visiting) { args.IsEnabled = false; args.Tooltip = new TextObject("{=rf_arena_visiting}Consider yourself lucky, that you are not one of those poor sods enslaved and hurled into the arena for the enjoyment of the masses", null); }
            return !hasToWait;
        }

        public void OnSettlementStartConsequence(MenuCallbackArgs args)
        {
            if(currentState == ArenaState.Captured)
            {
                RFMissions.StartExploreMission(currentSettlement.CustomScene, CurrentBuildData, new Action(OnArenaMasterTalkEnd));
            }
            else
                RFMissions.OpenArenaMission("arena_test", this);
        }
        private void OnArenaMasterTalkEnd()
        {
            SetWaitTimer((int)Math.Floor(ChooseWaitTime()));
        }

        private float ChooseWaitTime()
        {
            float waitTime;
            float currentTime = CampaignTime.Now.CurrentHourInDay;
            if (currentTime < arenaBattlesStartTime) waitTime = arenaBattlesStartTime - currentTime;
            else waitTime = 24 - currentTime + arenaBattlesStartTime;
            return waitTime;
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
                    GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "You are thrown into a prison cell, and left alone. Assessing the situation, you take a look at your cell - it must have been occupied till recently, you shudder when you think about what happened to the previous prisoner. The noise of footsteps makes you come back from your thought, as a silhouette appears on the other side of your cell's bars");
                    break;
                case ArenaState.FightStage1:
                    if(!hasToWait)
                        GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "You are too numb to laugh when you are brought your armor and weapon for the battle, it's clear they want a show, with you as a main actor!");
                    else
                        GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "You are left in your cell to wait for your first battle the following day");
                    //RFMissions.OpenArenaMission("arena_test");
                    break;
                case ArenaState.FightStage2:
                    if(!hasToWait)
                        GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "Servants come to take you to the arena again, you know what to expect by now...");
                    else
                        GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "As if nothing has changed, you are unceremoniously thrown into your cell, to wait for the next battle.");
                    //RFMissions.OpenArenaMission("arena_test");
                    break;
                case ArenaState.FightStage3:
                    if(!hasToWait)
                        GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "You hear rumour that the best fighters are given freedom, could it be the chance for you?");
                    else
                        GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "This is starting to become a routine you realise, while resting before another match. Regardless, you are too tired to dwell on it");
                    //RFMissions.OpenArenaMission("arena_test");
                    break;
                case ArenaState.Finishing:
                    GameMenu.SwitchToMenu("rf_arena_finish");
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
            
            bool result = currentState == ArenaState.Visiting;
            if (!result)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("Slaves are not allowed to leave themselves");
            }
            return true;
        }

        public bool OnSettlementWaitEndCondition(MenuCallbackArgs args)
        {
            return true;
        }

        public bool OnSettlementWaitStartOnCondition(MenuCallbackArgs args)
        {
            GameTexts.SetVariable("RF_SETTLEMENT_WAIT_START_TEXT", "Prepare for the next battle");
            return hasToWait;
        }

        public void OnSettlementWaitEndConsequence(MenuCallbackArgs args)
        {
            hasToWait = false;
            waitProgress = 0;
            GameMenu.SwitchToMenu("rf_settlement_start");
        }

        public void OnSettlementWaitInit(MenuCallbackArgs args)
        {
            GameTexts.SetVariable("RF_SETTLEMENT_WAIT_TEXT", "As you wait in your cell, You reach a state of bleak solidarity with other slaves, unsure of what will happen to you tomorrow...");
        }

        internal void OnPlayerBattleWin()
        {
            switch (currentState)
            {
                case ArenaSettlementStateHandler.ArenaState.FightStage1:
                    currentState = ArenaSettlementStateHandler.ArenaState.FightStage2;
                    hasToWait = true;
                    break;
                case ArenaSettlementStateHandler.ArenaState.FightStage2:
                    currentState = ArenaSettlementStateHandler.ArenaState.FightStage3;
                    hasToWait = true;
                    break;
                case ArenaSettlementStateHandler.ArenaState.FightStage3:
                    currentState = ArenaSettlementStateHandler.ArenaState.Finishing;
                    break;
            }

        }

        internal void OnPlayerBattleLoss()
        {
            throw new NotImplementedException();
        }
    }
}
