using RealmsForgotten.RFCustomSettlements;
using System;
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
using RFCustomSettlements;

namespace RealmsForgotten.RFCustomSettlements
{
    public class ExploreSettlementStateHandler : ISettlementStateHandler
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
                    if (!CanEnter())
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
            args.MenuContext.SetBackgroundMeshName(currentSettlement.WaitMeshName);
            GameTexts.SetVariable("RF_SETTLEMENT_WAIT_TEXT", "As the scouts tell you of the best approach time, you prepare for whatever danger might be inside");
        }
    }
}
