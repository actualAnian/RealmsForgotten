using RealmsForgotten.RFCustomSettlements;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using System.Collections.Generic;
using RFCustomSettlements;
using static RFCustomSettlements.ArenaBuildData;
using System.Linq;
using TaleWorlds.Library;

namespace RealmsForgotten.RFCustomSettlements
{
    public class ArenaSettlementStateHandler : ISettlementStateHandler
    {
        private CustomSettlementBuildData? CurrentBuildData;
        internal ArenaState currentState = ArenaState.Visiting;
        internal ArenaBuildData.ArenaChallenge? currentChallenge;
        private readonly RFCustomSettlement currentSettlement;
        private ArenaBuildData? _buildData;
        private float waitHours = 0;
        internal bool hasToWait = false;
        private float waitProgress;

        private static readonly int arenaBattlesStartTime = 18;

        internal ArenaBuildData BuildData { get => _buildData; set => _buildData = value; }

        public ArenaSettlementStateHandler(RFCustomSettlement settlement)
        {
            currentSettlement = settlement;
        }

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
                RFMissions.OpenArenaMission("arena_test", ChooseNextStageData(), OnBattleEnd);
        }

        private ArenaBuildData.StageData ChooseNextStageData()
        {
            switch (currentState)
            {
                case ArenaState.FightStage1:
                    return currentChallenge.StageDatas[0];
                case ArenaState.FightStage2:
                    return currentChallenge.StageDatas[1];
                case ArenaState.FightStage3:
                    return currentChallenge.StageDatas[2];
                default:
                    return currentChallenge.StageDatas[0];
            };
        }

        private void OnBattleEnd(bool isPlayerWinner)
        {
            if (isPlayerWinner)
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
            else
                GameMenu.SwitchToMenu("rf_arena_player_lost");
        }

        private void OnArenaMasterTalkEnd()
        {
            SetWaitTimer((int)Math.Floor(ChooseWaitTime()));
            ChooseChallenge();
        }

        private void ChooseChallenge()
        {
            currentChallenge = BuildData.Challenges.GetRandomElementInefficiently();
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
            if (hasToWait && waitHours == 0) SetWaitTimer(24);
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
                    break;
                case ArenaState.FightStage2:
                    if(!hasToWait)
                        GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "Servants come to take you to the arena again, you know what to expect by now...");
                    else
                        GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "As if nothing has changed, you are unceremoniously thrown into your cell, to wait for the next battle.");
                    break;
                case ArenaState.FightStage3:
                    if(!hasToWait)
                        GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "You hear rumour that the best fighters are given freedom, could it be the chance for you?");
                    else
                        GameTexts.SetVariable("RF_SETTLEMENT_MAIN_TEXT", "This is starting to become a routine you realise, while resting before another match. Regardless, you are too tired to dwell on it");
                    break;
                case ArenaState.Finishing:
                    GameMenu.SwitchToMenu("rf_arena_finish");
                    break;
            }
        }
        public void OnWaitMenuTillEnterTick(MenuCallbackArgs args, CampaignTime dt)
        {
            waitProgress += (float)dt.ToHours;
            if (waitHours.ApproximatelyEqualsTo(0f, 1E-05f))
            {
                CalculateWaitTime();
            }
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(waitProgress / waitHours);
        }

        private void CalculateWaitTime()
        {
            float currentTime = CampaignTime.Now.CurrentHourInDay;
            if (currentTime < arenaBattlesStartTime) waitHours = arenaBattlesStartTime - currentTime;
            else waitHours = 24 - currentTime + arenaBattlesStartTime;
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

        internal void SyncData(ArenaState currentArenaState, string? currentChallengeToSync, bool isWaiting)
        {
            currentState = currentArenaState;
            if (currentChallengeToSync != null)
            {
                ArenaChallenge curChallenge = (from challenge in BuildData.Challenges where challenge.ChallengeName == currentChallengeToSync select challenge).Single();
                currentChallenge = curChallenge;
            }
            hasToWait = isWaiting;
        }
    }
}
