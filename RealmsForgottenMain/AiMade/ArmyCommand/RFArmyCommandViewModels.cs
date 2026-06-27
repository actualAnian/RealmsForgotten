using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.ArmyCommand;

internal sealed class RFSelectableArmyItemPropertyVM : ViewModel
{
    private readonly Func<RFArmyLineUIContext, bool> _updateIsWarning;
    private readonly Func<RFArmyLineUIContext, string> _updateValue;
    private readonly Func<RFArmyLineUIContext, int> _updateDailyChange;
    private readonly Func<RFArmyLineUIContext, BasicTooltipViewModel> _updateHint;

    private string _spritePath;
    private bool _isWarning;
    private string _value;
    private BasicTooltipViewModel _hint;
    private int _changeAmount;

    public Action ActivateParentHovered { get; private set; }
    public Action DeactivateParentHovered { get; private set; }
    public Action ExecuteParentClickFunction { get; private set; }

    [DataSourceProperty]
    public string SpritePath
    {
        get => _spritePath;
        set => SetField(ref _spritePath, value, nameof(SpritePath));
    }

    [DataSourceProperty]
    public bool IsWarning
    {
        get => _isWarning;
        set => SetField(ref _isWarning, value, nameof(IsWarning));
    }

    [DataSourceProperty]
    public string Value
    {
        get => _value;
        set => SetField(ref _value, value, nameof(Value));
    }

    [DataSourceProperty]
    public int ChangeAmount
    {
        get => _changeAmount;
        set => SetField(ref _changeAmount, value, nameof(ChangeAmount));
    }

    public BasicTooltipViewModel Hint
    {
        get => _hint;
        set => SetField(ref _hint, value, nameof(Hint));
    }

    public RFSelectableArmyItemPropertyVM(
        string spritePath,
        Func<RFArmyLineUIContext, bool> updateIsWarning,
        Func<RFArmyLineUIContext, string> updateValue,
        Func<RFArmyLineUIContext, int> updateDailyChange,
        Func<RFArmyLineUIContext, BasicTooltipViewModel> updateHint)
    {
        _updateIsWarning = updateIsWarning;
        _updateValue = updateValue;
        _updateDailyChange = updateDailyChange;
        _updateHint = updateHint;
        SpritePath = spritePath;
        RefreshValues();
    }

    public void ExecuteBeginHint()
    {
        ActivateParentHovered?.Invoke();
        Hint?.ExecuteBeginHint();
    }

    public void ExecuteEndHint()
    {
        DeactivateParentHovered?.Invoke();
        Hint?.ExecuteEndHint();
    }

    public void ExecuteClickFunction()
    {
        ExecuteParentClickFunction?.Invoke();
    }

    public void UpdateValues(RFArmyLineUIContext context)
    {
        Value = _updateValue(context);
        IsWarning = _updateIsWarning(context);
        ChangeAmount = _updateDailyChange(context);
        Hint = _updateHint(context);
        ActivateParentHovered = context.GetLineVm().ExecuteBeginHover;
        DeactivateParentHovered = context.GetLineVm().ExecuteEndHover;
        ExecuteParentClickFunction = context.GetLineVm().ExecuteClickFunction;
        RefreshValues();
    }
}

internal sealed class RFSelectableArmyPropertiesRow : ViewModel
{
    private MBBindingList<RFSelectableArmyItemPropertyVM> _armyInfosRow;

    [DataSourceProperty]
    public MBBindingList<RFSelectableArmyItemPropertyVM> ArmyInfosRow
    {
        get => _armyInfosRow;
        set => SetField(ref _armyInfosRow, value, nameof(ArmyInfosRow));
    }

    public RFSelectableArmyPropertiesRow(MBBindingList<RFSelectableArmyItemPropertyVM> armyInfosRow)
    {
        ArmyInfosRow = armyInfosRow;
        RefreshValues();
    }

    public void UpdateValues(RFArmyLineUIContext context)
    {
        foreach (RFSelectableArmyItemPropertyVM item in ArmyInfosRow)
        {
            item.UpdateValues(context);
        }

        RefreshValues();
    }
}

internal sealed class RFSelectableArmyLineVM : ViewModel
{
    private readonly Func<RFArmyLineUIContext, MobileParty> _updateLeaderParty;
    private MobileParty _leaderParty;
    private CharacterImageIdentifierVM _leaderVisual;
    private MBBindingList<RFSelectableArmyPropertiesRow> _armyInfoRows;
    private bool _forceHovered;
    private bool _isSelected;

    [DataSourceProperty]
    public MobileParty LeaderParty
    {
        get => _leaderParty;
        set => SetField(ref _leaderParty, value, nameof(LeaderParty));
    }

    [DataSourceProperty]
    public CharacterImageIdentifierVM LeaderVisual
    {
        get => _leaderVisual;
        set => SetField(ref _leaderVisual, value, nameof(LeaderVisual));
    }

    [DataSourceProperty]
    public MBBindingList<RFSelectableArmyPropertiesRow> ArmyInfoRows
    {
        get => _armyInfoRows;
        set => SetField(ref _armyInfoRows, value, nameof(ArmyInfoRows));
    }

    [DataSourceProperty]
    public bool ForceHovered
    {
        get => _forceHovered;
        set => SetField(ref _forceHovered, value, nameof(ForceHovered));
    }

    [DataSourceProperty]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value, nameof(IsSelected));
    }

    public RFSelectableArmyLineVM(Func<RFArmyLineUIContext, MobileParty> updateLeaderParty, MBBindingList<RFSelectableArmyPropertiesRow> armyInfoRows)
    {
        _updateLeaderParty = updateLeaderParty;
        ArmyInfoRows = armyInfoRows;
        RefreshValues();
    }

    public void ExecuteClickFunction()
    {
        if (RFArmyOverlayUIContext.Instance != null)
        {
            RFArmyOverlayUIContext.Instance.SelectedArmy = RFArmyCommandHelpers.ResolveSelectedArmy(LeaderParty?.Army);
        }

        CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
    }

    public void ExecuteBeginHover()
    {
        ForceHovered = true;
    }

    public void ExecuteEndHover()
    {
        if (!IsSelected)
        {
            ForceHovered = false;
        }
    }

    public void UpdateValues(RFArmyLineUIContext context)
    {
        MobileParty party = _updateLeaderParty(context);
        if (!ReferenceEquals(party, LeaderParty))
        {
            LeaderParty = party;
            if (LeaderParty?.LeaderHero != null)
            {
                try
                {
                    CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(LeaderParty.LeaderHero.CharacterObject, false);
                    LeaderVisual = new CharacterImageIdentifierVM(characterCode);
                }
                catch
                {
                    LeaderVisual = null;
                }
            }
            else
            {
                LeaderVisual = null;
            }
        }

        context.RegisterLineVm(this);
        foreach (RFSelectableArmyPropertiesRow row in ArmyInfoRows)
        {
            row.UpdateValues(context);
        }

        RefreshValues();
    }

    public void UpdateSelection()
    {
        if (RFArmyCommandHelpers.IsSameParty(LeaderParty, RFArmyOverlayUIContext.Instance?.SelectedArmy?.LeaderParty))
        {
            IsSelected = true;
            ExecuteBeginHover();
        }
        else
        {
            IsSelected = false;
            ExecuteEndHover();
        }
    }
}

internal sealed class RFArmyOverlayArmyListVM : ViewModel
{
    private MBBindingList<RFSelectableArmyLineVM> _armiesList;

    [DataSourceProperty]
    public MBBindingList<RFSelectableArmyLineVM> ArmiesList
    {
        get => _armiesList;
        set => SetField(ref _armiesList, value, nameof(ArmiesList));
    }

    public RFArmyOverlayArmyListVM(MBBindingList<RFSelectableArmyLineVM> armiesList)
    {
        ArmiesList = armiesList;
    }

    public void UpdateValues()
    {
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        RFArmyOverlayUIContext overlayContext = RFArmyOverlayUIContext.Instance;
        if (kingdom == null || overlayContext?.CurrentArmyOverlayVMMixIn == null)
        {
            return;
        }

        List<Army> kingdomArmies = kingdom.Armies?.Where(army => army != null).ToList() ?? new List<Army>();
        List<MobileParty> kingdomParties = kingdom.AllParties?.Where(party => party != null).ToList() ?? new List<MobileParty>();
        List<Army> orderedArmies = new();
        Dictionary<string, RFArmyLineUIContext> armyContexts = new();
        foreach (Army army in kingdomArmies)
        {
            if (army?.Kingdom == null || army.LeaderParty == null)
            {
                continue;
            }

            string armyKey = GetArmyKey(army);
            if (string.IsNullOrEmpty(armyKey))
            {
                continue;
            }

            orderedArmies.Add(army);
            armyContexts[armyKey] = new RFArmyLineUIContext();
            overlayContext.ArmiesCount++;
        }

        if (kingdomParties.Count == 0)
        {
            return;
        }

        foreach (MobileParty party in kingdomParties)
        {
            if (party == null || party.IsCaravan || party.LeaderHero == null || party.Party == null)
            {
                continue;
            }

            Army partyArmy = party.Army;
            string partyArmyKey = GetArmyKey(partyArmy);
            if (!string.IsNullOrEmpty(partyArmyKey) && partyArmy?.Kingdom != null && armyContexts.TryGetValue(partyArmyKey, out RFArmyLineUIContext context))
            {
                context.AllPartiesFromArmy.Add(party);
                if (!RFArmyCommandHelpers.IsSameParty(party, partyArmy.LeaderParty) && party.AttachedTo == null && RFArmyCommandHelpers.GetDaysDistance(party, partyArmy.LeaderParty, party.Speed) < 1f)
                {
                    context.PartiesJoiningToday.Add(party);
                }

                overlayContext.PartiesInArmiesCount++;
                overlayContext.MenInArmiesCount += party.Party.MemberRoster.TotalManCount;
            }

            overlayContext.PartiesInKingdomCount++;
            overlayContext.MenInKingdomCount += party.Party.MemberRoster.TotalManCount;
        }

        int index = 0;
        foreach (Army army in orderedArmies)
        {
            string armyKey = GetArmyKey(army);
            if (string.IsNullOrEmpty(armyKey) || !armyContexts.TryGetValue(armyKey, out RFArmyLineUIContext context))
            {
                continue;
            }

            overlayContext.CurrentArmyOverlayVMMixIn.UpdateLineContext(context, army);
            if (context.LeaderParty != null)
            {
                ArmiesList.ElementAtOrDefault(index)?.UpdateValues(context);
                index++;
            }
        }
    }

    private static string GetArmyKey(Army army)
    {
        MobileParty leaderParty = army?.LeaderParty;
        if (leaderParty == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(leaderParty.StringId))
        {
            return leaderParty.StringId;
        }

        return leaderParty.LeaderHero?.StringId;
    }

    public void UpdateSelection()
    {
        foreach (RFSelectableArmyLineVM line in ArmiesList)
        {
            line?.UpdateSelection();
        }
    }

    public void AddLine(RFSelectableArmyLineVM newLine)
    {
        ArmiesList.Add(newLine);
    }

    public void ClearLines()
    {
        ArmiesList.Clear();
    }

    public int GetArmiesCount()
    {
        return ArmiesList.Count;
    }
}

internal static class RFArmyLineWidgetBuilders
{
    private static BasicTooltipViewModel SafeArmyTooltip(Func<List<TooltipProperty>> tooltipFactory)
    {
        return new BasicTooltipViewModel(() =>
        {
            try
            {
                return tooltipFactory?.Invoke() ?? new List<TooltipProperty>();
            }
            catch
            {
                return new List<TooltipProperty>();
            }
        });
    }

    public static RFSelectableArmyLineVM BuildArmyLine()
    {
        RFSelectableArmyItemPropertyVM parties = new(
            "MapBar\\mapbar_icon2",
            _ => false,
            c => $"{c.AttachedPartiesCount}/{c.TotalAssignedPartiesCount}",
            c => c.PartiesWithinADayDistanceCount,
            _ => null);

        RFSelectableArmyItemPropertyVM men = new(
            "General\\Icons\\Party@2x",
            _ => false,
            c => $"{c.MenCount}/{c.PotentialMenCount}",
            c => c.MenJoiningToday,
            c => c.LeaderParty?.Army == null ? null : SafeArmyTooltip(() => CampaignUIHelper.GetArmyManCountTooltip(c.LeaderParty.Army)));

        RFSelectableArmyItemPropertyVM food = new(
            "General\\Icons\\Food@2x",
            c => c.CurrentArmyFood + c.TotalArmyFoodChange < 0f,
            c => $"{c.CurrentArmyFood:F0}/{c.TotalArmyFood:F0}",
            c => (int)c.TotalArmyFoodChange,
            c => c.LeaderParty?.Army == null ? null : SafeArmyTooltip(() => CampaignUIHelper.GetArmyFoodTooltip(c.LeaderParty.Army)));

        RFSelectableArmyItemPropertyVM influence = new(
            "General\\Icons\\Influence@2x",
            c => c.CurrentArmyInfluence + c.DailyArmyInfluenceChange < 0f,
            c => $"{c.CurrentArmyInfluence:F0}",
            c => (int)c.DailyArmyInfluenceChange,
            c => c.LeaderParty?.ActualClan == null ? null : SafeArmyTooltip(() => CampaignUIHelper.GetInfluenceTooltip(c.LeaderParty.ActualClan)));

        RFSelectableArmyItemPropertyVM cohesion = new(
            "General\\Icons\\Prosperity",
            c => c.CurrentCohesion + c.DailyCohesionChange < 0f,
            c => $"{c.CurrentCohesion:F0}",
            c => (int)c.DailyCohesionChange,
            c => c.LeaderParty?.Army == null ? null : SafeArmyTooltip(() => CampaignUIHelper.GetArmyCohesionTooltip(c.LeaderParty.Army)));

        RFSelectableArmyItemPropertyVM restoreCost = new(
            "General\\Icons\\Influence@2x",
            c => c.CurrentArmyInfluence - c.LostCohesionCostValue < 0f,
            c => $"{c.LostCohesionCostValue}",
            _ => 0,
            _ => null);

        MBBindingList<RFSelectableArmyItemPropertyVM> row1Items = new();
        row1Items.Add(parties);
        row1Items.Add(men);
        row1Items.Add(food);

        MBBindingList<RFSelectableArmyItemPropertyVM> row2Items = new();
        row2Items.Add(influence);
        row2Items.Add(cohesion);
        row2Items.Add(restoreCost);

        MBBindingList<RFSelectableArmyPropertiesRow> rows = new();
        rows.Add(new RFSelectableArmyPropertiesRow(row1Items));
        rows.Add(new RFSelectableArmyPropertiesRow(row2Items));

        return new RFSelectableArmyLineVM(c => c.LeaderParty, rows);
    }
}
