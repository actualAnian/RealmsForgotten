using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;

namespace RealmsForgotten.AiMade
{
    public enum MineDestinationTypes
    {
        Clan,
        TownProsperity,
        Construction,
        WarEffort
    }

    internal class TownPrisonerData
    {
        [SaveableField(1)]
        public int PrisonerAmount;
        [SaveableField(2)]
        public MineDestinationTypes DestinationType;

        public TownPrisonerData(int prisoners, MineDestinationTypes destination)
        {
            PrisonerAmount = prisoners;
            DestinationType = destination;
        }
    }

    internal class MineBehavior : CampaignBehaviorBase
    {
        public Dictionary<string, TownPrisonerData> PrisonerData = new();
        private Dictionary<Settlement, CampaignTime> _mineCompletionDates = new();
        private const int MineBuildDuration = 5;
        public static MineBehavior? Instance { get; private set; }
        private const int PrisonerEfficiencyFactor = 8;

        private static readonly HashSet<string> AllowedDwarvenSettlements = new()
        {
            "town_dwarf_1",
            "town_dwarf_2",
            "town_dwarf_3",
            "town_dwarf_4",
            "town_dwarf_5",
            "town_dwarf_6",
            "town_dwarf_7"
        };

        private readonly Dictionary<MineDestinationTypes, TextObject> _destinationNames = new()
        {
            { MineDestinationTypes.Clan, new TextObject("{=mine_clan}Dwarven Clan Wealth") },
            { MineDestinationTypes.TownProsperity, new TextObject("{=mine_prosperity}Town Prosperity") },
            { MineDestinationTypes.Construction, new TextObject("{=mine_construction}Mine Expansion") },
            { MineDestinationTypes.WarEffort, new TextObject("{=mine_war_effort}War Effort Contribution") }
        };

        public MineBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, SessionLaunched);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnDailyTick()
        {
            List<Settlement> toRemove = new();
            foreach (var keyValue in _mineCompletionDates)
            {
                if (CampaignTime.Now >= keyValue.Value && AllowedDwarvenSettlements.Contains(keyValue.Key.StringId))
                {
                    TextObject textObject = new TextObject("{=mine_build_complete}Your mine at {SETTLEMENT} has been completed.");
                    textObject.SetTextVariable("SETTLEMENT", keyValue.Key.Name);

                    PrisonerData.Add(keyValue.Key.StringId, new TownPrisonerData(0, MineDestinationTypes.Clan));
                    toRemove.Add(keyValue.Key);

                    InformationManager.ShowInquiry(new InquiryData(new TextObject("{=event}Event").ToString(), textObject.ToString(), true, false,
                        GameTexts.FindText("str_done").ToString(), null, null, null), true);
                }
            }

            foreach (var element in toRemove)
                _mineCompletionDates.Remove(element);
        }

        private void WeeklyTick()
        {
            int escapedPrisoners = 0;
            foreach (string settlementId in PrisonerData.Keys)
            {
                if (MBRandom.RandomFloat > 0.1f)
                {
                    float factor = MBRandom.RandomFloat;
                    escapedPrisoners += (int)(0.1f * PrisonerData[settlementId].PrisonerAmount * factor);
                    PrisonerData[settlementId].PrisonerAmount -= escapedPrisoners;
                }
            }

            if (escapedPrisoners > 0)
            {
                InformationManager.ShowInquiry(new InquiryData(new TextObject("{=mine_prisoner_escape}Escapes").ToString(),
                    new TextObject("{=mine_prisoner_escape_description}{AMOUNT} prisoners escaped from your mine this week.").SetTextVariable("AMOUNT", escapedPrisoners).ToString(),
                    true, false, "Done", "", null, null), true);
            }
        }

        private void SessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption("town", "town_build_mine",
                "{=mine_build_option}Build mine ({COST}{GOLD_ICON})", args =>
                {
                    if (!AllowedDwarvenSettlements.Contains(Settlement.CurrentSettlement.StringId))
                        return false;

                    float totalCost = Settlement.CurrentSettlement.Town.Prosperity * PrisonerEfficiencyFactor;
                    MBTextManager.SetTextVariable("COST", string.Format("{0:n0}", totalCost));
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    if (Hero.MainHero.Gold < totalCost)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=mine_not_enough_gold}You don't have enough money.");
                    }

                    return !_mineCompletionDates.ContainsKey(Settlement.CurrentSettlement);
                }, args =>
                {
                    _mineCompletionDates.Add(Settlement.CurrentSettlement, CampaignTime.DaysFromNow(MineBuildDuration));
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -(int)(Settlement.CurrentSettlement.Town.Prosperity * PrisonerEfficiencyFactor));
                    args.MenuContext.Refresh();
                }, false, 4);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("prisonerData", ref PrisonerData);
            dataStore.SyncData("_mineCompletionDates", ref _mineCompletionDates);

            if (dataStore.IsLoading && PrisonerData == null)
                PrisonerData = new();
        }
    }
}
