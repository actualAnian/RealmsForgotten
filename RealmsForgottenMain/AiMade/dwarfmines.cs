using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

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

        [SaveableField(3)]
        public Dictionary<string, int> AssignedPrisonerTypes = new();

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
        private Dictionary<string, int> _mineOreStorage = new();
        private Dictionary<string, float> _mineHealth = new();

        private const int MineBuildDuration = 5;
        private const int PrisonerEfficiencyFactor = 8;
        private const int OreProductionBaseRate = 2;
        private const float HealthDecayPerOreUnit = 0.1f;
        private const float MinOreProductionFactor = 0.25f;
        private const int OreExhaustionLimit = 1000;

        public static MineBehavior? Instance { get; private set; }

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
            foreach (var kv in _mineCompletionDates)
            {
                if (CampaignTime.Now >= kv.Value && AllowedDwarvenSettlements.Contains(kv.Key.StringId))
                {
                    TextObject textObject = new TextObject("{=mine_build_complete}Your mine at {SETTLEMENT} has been completed.");
                    textObject.SetTextVariable("SETTLEMENT", kv.Key.Name);

                    PrisonerData[kv.Key.StringId] = new TownPrisonerData(0, MineDestinationTypes.Clan);
                    _mineHealth[kv.Key.StringId] = 100f;
                    _mineOreStorage[kv.Key.StringId] = 0;

                    toRemove.Add(kv.Key);

                    InformationManager.ShowInquiry(new InquiryData(
                        new TextObject("{=event}Event").ToString(),
                        textObject.ToString(),
                        true, false,
                        GameTexts.FindText("str_done").ToString(),
                        null, null, null), true);
                }
            }

            foreach (var settlement in toRemove)
                _mineCompletionDates.Remove(settlement);
        }

        private void WeeklyTick()
        {
            int totalEscaped = 0;

            foreach (string settlementId in PrisonerData.Keys.ToList())
            {
                TownPrisonerData data = PrisonerData[settlementId];
                int prisoners = data.PrisonerAmount;

                if (MBRandom.RandomFloat > 0.1f)
                {
                    float factor = MBRandom.RandomFloat;
                    int escaped = (int)(0.1f * prisoners * factor);
                    PrisonerData[settlementId].PrisonerAmount -= escaped;
                    totalEscaped += escaped;
                }

                if (!_mineHealth.ContainsKey(settlementId))
                    _mineHealth[settlementId] = 100f;

                if (!_mineOreStorage.ContainsKey(settlementId))
                    _mineOreStorage[settlementId] = 0;

                float health = _mineHealth[settlementId];
                float healthFactor = MathF.Max(health / 100f, MinOreProductionFactor);
                int oreProduced = (int)(prisoners * OreProductionBaseRate * healthFactor * MBRandom.RandomFloat);

                if (health > 0 && _mineOreStorage[settlementId] < OreExhaustionLimit)
                {
                    _mineOreStorage[settlementId] += oreProduced;
                    _mineHealth[settlementId] = MathF.Max(0f, health - oreProduced * HealthDecayPerOreUnit);

                    InformationManager.DisplayMessage(new InformationMessage($"Mine at {settlementId} produced {oreProduced} ore. Health: {(int)_mineHealth[settlementId]}%"));

                    ItemObject kardrathium = MBObjectManager.Instance.GetObject<ItemObject>("kardrathium");
                    Settlement? settlement = Settlement.All.FirstOrDefault(s => s.StringId == settlementId);

                    if (kardrathium != null && settlement != null)
                    {
                        settlement.ItemRoster.AddToCounts(kardrathium, oreProduced);
                    }

                    if (data.DestinationType == MineDestinationTypes.TownProsperity && settlement != null && settlement.Town != null)
                    {
                        float prosperityGain = oreProduced * 0.2f;
                        settlement.Town.Prosperity += prosperityGain;
                        InformationManager.DisplayMessage(new InformationMessage($"{settlement.Name} gained +{(int)prosperityGain} prosperity from the mine."));
                    }
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Mine at {settlementId} is exhausted or too damaged to produce."));
                }
            }

            if (totalEscaped > 0)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    new TextObject("{=mine_prisoner_escape}Escapes").ToString(),
                    new TextObject("{=mine_prisoner_escape_description}{AMOUNT} prisoners escaped from your mine this week.")
                        .SetTextVariable("AMOUNT", totalEscaped).ToString(),
                    true, false, "Done", "", null, null), true);
            }
        }


        private void SelectPrisonerQuantities(List<InquiryElement> selected, string townId)
        {
            if (selected.Count == 0) return;

            var selectedTroops = selected.Select(e => (TroopRosterElement)e.Identifier).Where(t => t.Number > 0).ToList();

            void AssignNext(int index)
            {
                if (index >= selectedTroops.Count)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Prisoners assigned to the mine."));
                    return;
                }

                var troop = selectedTroops[index];
                string troopName = troop.Character.Name.ToString();
                int maxAvailable = troop.Number;

                InformationManager.ShowTextInquiry(new TextInquiryData(
                    $"Assign Prisoners: {troopName}",
                    $"You have {maxAvailable} prisoners of this type. How many do you want to assign?",
                    true, true,
                    "Assign", "Cancel",
                    input =>
                    {
                        if (int.TryParse(input, out int assignCount))
                        {
                            assignCount = Math.Min(assignCount, maxAvailable);
                            if (assignCount > 0)
                            {
                                MobileParty.MainParty.PrisonRoster.RemoveTroop(troop.Character, assignCount);

                                if (!PrisonerData[townId].AssignedPrisonerTypes.ContainsKey(troop.Character.StringId))
                                    PrisonerData[townId].AssignedPrisonerTypes[troop.Character.StringId] = 0;

                                PrisonerData[townId].AssignedPrisonerTypes[troop.Character.StringId] += assignCount;
                                PrisonerData[townId].PrisonerAmount += assignCount;

                                InformationManager.DisplayMessage(new InformationMessage($"Assigned {assignCount}x {troopName} to the mine."));
                            }
                            AssignNext(index + 1);
                        }
                        else
                        {
                            InformationManager.DisplayMessage(new InformationMessage("Invalid input. Try again."));
                            AssignNext(index);
                        }
                    }, null));
            }

            AssignNext(0);
        }

        private void SessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "town_build_mine",
                "{=mine_build_option}Build mine ({COST}{GOLD_ICON})", args =>
                {
                    if (!AllowedDwarvenSettlements.Contains(Settlement.CurrentSettlement.StringId))
                        return false;

                    float cost = Settlement.CurrentSettlement.Town.Prosperity * PrisonerEfficiencyFactor;
                    MBTextManager.SetTextVariable("COST", string.Format("{0:n0}", cost));
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

                    bool isDwarfCulture = Hero.MainHero.Culture != null
                        && Hero.MainHero.Culture.StringId == "dwarf";
                    bool hasEnoughGold = Hero.MainHero.Gold >= cost;
                    bool notAlreadyBuilding = !_mineCompletionDates.ContainsKey(Settlement.CurrentSettlement);
                    bool noActiveMine = !PrisonerData.ContainsKey(Settlement.CurrentSettlement.StringId)
                                        || (_mineHealth.TryGetValue(Settlement.CurrentSettlement.StringId, out float health)
                                            && health <= 0f)
                                        || (_mineOreStorage.TryGetValue(Settlement.CurrentSettlement.StringId, out int ore)
                                            && ore >= OreExhaustionLimit);

                    args.IsEnabled = hasEnoughGold && isDwarfCulture && notAlreadyBuilding && noActiveMine;

                    if (!isDwarfCulture)
                        args.Tooltip = new TextObject("{=mine_wrong_culture}Only dwarves know the secrets of deep mining.");
                    else if (!hasEnoughGold)
                        args.Tooltip = new TextObject("{=mine_not_enough_gold}You don't have enough money.");

                    return true;
                },
                args =>
                {
                    float cost = Settlement.CurrentSettlement.Town.Prosperity * PrisonerEfficiencyFactor;
                    _mineCompletionDates[Settlement.CurrentSettlement] = CampaignTime.DaysFromNow(MineBuildDuration);
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -(int)cost);
                    args.MenuContext.Refresh();
                }, false, 4);

            starter.AddGameMenuOption("town", "repair_mine",
                "{=mine_repair_option}Repair Mine ({COST}{GOLD_ICON})", args =>
                {
                    if (!PrisonerData.ContainsKey(Settlement.CurrentSettlement.StringId))
                        return false;

                    float health = _mineHealth.ContainsKey(Settlement.CurrentSettlement.StringId)
                                   ? _mineHealth[Settlement.CurrentSettlement.StringId]
                                   : 0f;

                    args.IsEnabled = health < 100f;
                    float cost = (100f - health) * 5;
                    MBTextManager.SetTextVariable("COST", string.Format("{0:n0}", cost));
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    return true;
                },
                args =>
                {
                    float health = _mineHealth.ContainsKey(Settlement.CurrentSettlement.StringId)
                                   ? _mineHealth[Settlement.CurrentSettlement.StringId]
                                   : 0f;

                    float cost = (100f - health) * 5;

                    if (Hero.MainHero.Gold >= cost)
                    {
                        GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -(int)cost);
                        _mineHealth[Settlement.CurrentSettlement.StringId] = 100f;
                        InformationManager.DisplayMessage(new InformationMessage("Mine fully repaired."));
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Not enough gold to repair the mine."));
                    }

                    args.MenuContext.Refresh();
                }, false, 5);

            starter.AddGameMenuOption("town", "assign_prisoners_to_mine",
                "{=mine_assign_prisoners}Assign Prisoners to Mine", args =>
                {
                    string townId = Settlement.CurrentSettlement.StringId;
                    bool hasMine = PrisonerData.ContainsKey(townId);
                    bool notExhausted = !_mineHealth.TryGetValue(townId, out float health) || health > 0f;
                    bool notFull = !_mineOreStorage.TryGetValue(townId, out int ore) || ore < OreExhaustionLimit;
                    return hasMine && notExhausted && notFull;
                },
                args =>
                {
                    var prisonerRoster = MobileParty.MainParty.PrisonRoster;
                    var townId = Settlement.CurrentSettlement.StringId;
                    var selectedTroops = prisonerRoster.GetTroopRoster()
                        .Where(t => t.Number > 0)
                        .ToList();

                    if (!selectedTroops.Any())
                    {
                        InformationManager.DisplayMessage(new InformationMessage("You have no assignable prisoners."));
                        return;
                    }

                    void AssignNext(int index)
                    {
                        if (index >= selectedTroops.Count)
                        {
                            InformationManager.DisplayMessage(new InformationMessage("Finished assigning prisoners to the mine."));
                            return;
                        }

                        var troop = selectedTroops[index];
                        string troopName = troop.Character.Name.ToString();
                        int maxAvailable = troop.Number;

                        InformationManager.ShowTextInquiry(new TextInquiryData(
                            $"Assign Prisoners: {troopName}",
                            $"You have {maxAvailable} of this type. Enter how many to assign (0 to skip):",
                            true, true,
                            "Assign", "Cancel",
                            input =>
                            {
                                if (int.TryParse(input, out int assignCount))
                                {
                                    assignCount = Math.Max(0, Math.Min(assignCount, maxAvailable));
                                    if (assignCount > 0)
                                    {
                                        MobileParty.MainParty.PrisonRoster.RemoveTroop(troop.Character, assignCount);
                                        if (!PrisonerData[townId].AssignedPrisonerTypes.ContainsKey(troop.Character.StringId))
                                            PrisonerData[townId].AssignedPrisonerTypes[troop.Character.StringId] = 0;

                                        PrisonerData[townId].AssignedPrisonerTypes[troop.Character.StringId] += assignCount;
                                        PrisonerData[townId].PrisonerAmount += assignCount;

                                        InformationManager.DisplayMessage(new InformationMessage($"Assigned {assignCount} {troopName}."));
                                    }
                                    AssignNext(index + 1);
                                }
                                else
                                {
                                    InformationManager.DisplayMessage(new InformationMessage("Invalid input. Try again."));
                                    AssignNext(index);
                                }
                            },
                            null
                        ));
                    }

                    AssignNext(0);
                }, false, 7);
        }



        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("prisonerData", ref PrisonerData);
            dataStore.SyncData("_mineCompletionDates", ref _mineCompletionDates);
            dataStore.SyncData("_mineOreStorage", ref _mineOreStorage);
            dataStore.SyncData("_mineHealth", ref _mineHealth);

            if (dataStore.IsLoading)
            {
                PrisonerData ??= new();
                _mineHealth ??= new();
                _mineOreStorage ??= new();
            }
        }
    }
}


