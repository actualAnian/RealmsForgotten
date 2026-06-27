using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.ArmyCommand;

[ViewModelMixin("RefreshValues")]
internal sealed class RFClanPartyItemVMMixin : BaseViewModelMixin<ClanPartyItemVM>
{
    private bool _rfPartyCommandsVisible;
    private bool _rfPartyTargetEnabled;
    private bool _rfPartyBehaviorEnabled;
    private bool _rfPartyRecommendedEnabled;
    private bool _rfPartyStrategicEnabled;
    private string _rfPartyCommandsHeader;
    private string _rfPartyTargetText;
    private string _rfPartyBehaviorText;
    private string _rfPartyRecommendedText;
    private string _rfPartyStrategicText;

    [DataSourceProperty]
    public bool RFPartyCommandsVisible
    {
        get => _rfPartyCommandsVisible;
        set => SetField(ref _rfPartyCommandsVisible, value, nameof(RFPartyCommandsVisible));
    }

    [DataSourceProperty]
    public bool RFPartyTargetEnabled
    {
        get => _rfPartyTargetEnabled;
        set => SetField(ref _rfPartyTargetEnabled, value, nameof(RFPartyTargetEnabled));
    }

    [DataSourceProperty]
    public bool RFPartyBehaviorEnabled
    {
        get => _rfPartyBehaviorEnabled;
        set => SetField(ref _rfPartyBehaviorEnabled, value, nameof(RFPartyBehaviorEnabled));
    }

    [DataSourceProperty]
    public bool RFPartyRecommendedEnabled
    {
        get => _rfPartyRecommendedEnabled;
        set => SetField(ref _rfPartyRecommendedEnabled, value, nameof(RFPartyRecommendedEnabled));
    }

    [DataSourceProperty]
    public bool RFPartyStrategicEnabled
    {
        get => _rfPartyStrategicEnabled;
        set => SetField(ref _rfPartyStrategicEnabled, value, nameof(RFPartyStrategicEnabled));
    }

    [DataSourceProperty]
    public string RFPartyCommandsHeader
    {
        get => _rfPartyCommandsHeader;
        set => SetField(ref _rfPartyCommandsHeader, value, nameof(RFPartyCommandsHeader));
    }

    [DataSourceProperty]
    public string RFPartyTargetText
    {
        get => _rfPartyTargetText;
        set => SetField(ref _rfPartyTargetText, value, nameof(RFPartyTargetText));
    }

    [DataSourceProperty]
    public string RFPartyBehaviorText
    {
        get => _rfPartyBehaviorText;
        set => SetField(ref _rfPartyBehaviorText, value, nameof(RFPartyBehaviorText));
    }

    [DataSourceProperty]
    public string RFPartyRecommendedText
    {
        get => _rfPartyRecommendedText;
        set => SetField(ref _rfPartyRecommendedText, value, nameof(RFPartyRecommendedText));
    }

    [DataSourceProperty]
    public string RFPartyStrategicText
    {
        get => _rfPartyStrategicText;
        set => SetField(ref _rfPartyStrategicText, value, nameof(RFPartyStrategicText));
    }

    public RFClanPartyItemVMMixin(ClanPartyItemVM vm)
        : base(vm)
    {
        RefreshState();
    }

    public override void OnRefresh()
    {
        RefreshState();
    }

    [DataSourceMethod]
    public void ExecuteRFSelectPartyTarget()
    {
        MobileParty party = GetManagedParty();
        if (!CanIssueOrders(party))
        {
            return;
        }

        List<InquiryElement> targets = GetAvailableTargets(party);
        if (targets.Count == 0)
        {
            MBInformationManager.AddQuickInformation(new TextObject("{=rf_party_no_target}No valid target is available for this party right now."), 0, null);
            return;
        }

        MBInformationManager.ShowMultiSelectionInquiry(
            new MultiSelectionInquiryData(
                "Party Target",
                "",
                targets,
                true,
                1,
                1,
                "Select",
                "Cancel",
                selected => ConfirmTargetSelection(party, selected),
                _ => { },
                "",
                false),
            false,
            false);
    }

    [DataSourceMethod]
    public void ExecuteRFSelectPartyBehavior()
    {
        MobileParty party = GetManagedParty();
        if (!CanIssueOrders(party))
        {
            return;
        }

        if (GetCurrentHostilePartyTarget(party) != null)
        {
            return;
        }

        Settlement currentTarget = GetCurrentSettlementTarget(party);
        List<InquiryElement> behaviors = RFArmyCommandHelpers.GetAvailableArmyBehaviors(currentTarget)
            .Select(behavior => new InquiryElement(behavior, RFArmyCommandHelpers.GetArmyBehaviorName(behavior), null))
            .ToList();
        if (behaviors.Count == 0)
        {
            return;
        }

        MBInformationManager.ShowMultiSelectionInquiry(
            new MultiSelectionInquiryData(
                "Party Behavior",
                "",
                behaviors,
                true,
                1,
                1,
                "Select",
                "Cancel",
                selected => ConfirmBehaviorSelection(party, selected),
                _ => { },
                "",
                false),
            false,
            false);
    }

    [DataSourceMethod]
    public void ExecuteRFUseRecommendedPartyPlan()
    {
        MobileParty party = GetManagedParty();
        if (!CanIssueOrders(party))
        {
            return;
        }

        RFArmyCommandPlan plan = RFArmyCommandService.GetRecommendedPlan(party);
        ApplyPartyPlan(party, plan, "recommended");
    }

    [DataSourceMethod]
    public void ExecuteRFSelectPartyStrategicPlan()
    {
        MobileParty party = GetManagedParty();
        if (!CanIssueOrders(party))
        {
            return;
        }

        List<InquiryElement> plans = RFArmyCommandService.GetSelectablePlans(party)
            .Select(plan => new InquiryElement(plan, plan.OrderText, null))
            .ToList();
        if (plans.Count == 0)
        {
            return;
        }

        MBInformationManager.ShowMultiSelectionInquiry(
            new MultiSelectionInquiryData(
                "Strategic Plan",
                "",
                plans,
                true,
                1,
                1,
                "Select",
                "Cancel",
                selected => ConfirmStrategicPlanSelection(party, selected),
                _ => { },
                "",
                false),
            false,
            false);
    }

    private void RefreshState()
    {
        MobileParty party = ViewModel?.Party?.MobileParty;
        bool visible = IsSupportedClanParty(party);
        RFPartyCommandsVisible = visible;
        if (!visible)
        {
            RFPartyCommandsHeader = string.Empty;
            RFPartyTargetText = string.Empty;
            RFPartyBehaviorText = string.Empty;
            RFPartyRecommendedText = string.Empty;
            RFPartyStrategicText = string.Empty;
            RFPartyTargetEnabled = false;
            RFPartyBehaviorEnabled = false;
            RFPartyRecommendedEnabled = false;
            RFPartyStrategicEnabled = false;
            return;
        }

        RFPartyCommandsHeader = GetHeaderText(party);
        RFPartyTargetText = RFArmyCommandHelpers.TruncateText(GetTargetText(party), 34);
        RFPartyBehaviorText = RFArmyCommandHelpers.TruncateText(GetBehaviorText(party), 24);

        RFArmyCommandPlan recommendedPlan = RFArmyCommandService.GetRecommendedPlan(party);
        RFPartyRecommendedText = RFArmyCommandHelpers.TruncateText(recommendedPlan?.OrderText ?? "Recommended Plan", 34);

        IReadOnlyList<RFArmyCommandPlan> strategicPlans = RFArmyCommandService.GetSelectablePlans(party);
        RFArmyCommandPlan currentOverride = GetCurrentOverride(party);
        RFPartyStrategicText = RFArmyCommandHelpers.TruncateText(
            currentOverride?.OrderText
                ?? strategicPlans.FirstOrDefault()?.OrderText
                ?? "Strategic Plan",
            34);

        bool canIssue = CanIssueOrders(party);
        bool hasHostilePartyTarget = GetCurrentHostilePartyTarget(party) != null;
        RFPartyTargetEnabled = canIssue;
        RFPartyBehaviorEnabled = canIssue && !hasHostilePartyTarget;
        RFPartyRecommendedEnabled = canIssue && recommendedPlan != null;
        RFPartyStrategicEnabled = canIssue && strategicPlans.Count > 0;
    }

    private static bool IsSupportedClanParty(MobileParty party)
    {
        return party != null
            && party.IsActive
            && party.IsLordParty
            && !party.IsMainParty
            && party.ActualClan == Clan.PlayerClan;
    }

    private static bool CanIssueOrders(MobileParty party)
    {
        return IsSupportedClanParty(party)
            && party.Army == null
            && !RFArmyCommandHelpers.IsPartyBusy(party);
    }

    private MobileParty GetManagedParty()
    {
        MobileParty party = ViewModel?.Party?.MobileParty;
        return IsSupportedClanParty(party) ? party : null;
    }

    private static string GetHeaderText(MobileParty party)
    {
        if (party == null)
        {
            return string.Empty;
        }

        if (party.Army != null)
        {
            return "Party committed to army";
        }

        if (RFArmyCommandHelpers.IsPartyBusy(party))
        {
            return "Party busy";
        }

        return "Party ready for orders";
    }

    private static string GetTargetText(MobileParty party)
    {
        RFArmyCommandPlan manualOverride = GetCurrentOverride(party);
        if (manualOverride?.TargetParty != null
            && manualOverride.TargetParty.IsActive
            && manualOverride.TargetParty.MapFaction != null
            && manualOverride.TargetParty.MapFaction != party?.MapFaction)
        {
            return $"Intercept {manualOverride.TargetParty.Name}";
        }

        if (manualOverride?.TargetSettlement != null)
        {
            return RFArmyCommandHelpers.GetSettlementCommandLabel(manualOverride.TargetSettlement);
        }

        MobileParty hostileParty = GetCurrentHostilePartyTarget(party);
        if (hostileParty != null)
        {
            return $"Intercept {hostileParty.Name}";
        }

        Settlement settlement = GetCurrentSettlementTarget(party);
        if (settlement != null)
        {
            return RFArmyCommandHelpers.GetSettlementCommandLabel(settlement);
        }

        return "No target";
    }

    private static string GetBehaviorText(MobileParty party)
    {
        RFArmyCommandPlan manualOverride = GetCurrentOverride(party);
        if (manualOverride != null)
        {
            return manualOverride.TargetParty != null
                ? "Intercept"
                : RFArmyCommandHelpers.GetArmyBehaviorName(manualOverride.ArmyBehavior);
        }

        if (GetCurrentHostilePartyTarget(party) != null)
        {
            return "Intercept";
        }

        Settlement settlement = GetCurrentSettlementTarget(party);
        Army.ArmyTypes behavior = settlement == null
            ? Army.ArmyTypes.Patrolling
            : RFArmyCommandHelpers.GetDefaultArmyBehavior(settlement);
        return RFArmyCommandHelpers.GetArmyBehaviorName(behavior);
    }

    private static RFArmyCommandPlan GetCurrentOverride(MobileParty party)
    {
        return RFArmyCommandService.TryGetManualOverride(party, out RFArmyCommandPlan plan) ? plan : null;
    }

    private static Settlement GetCurrentSettlementTarget(MobileParty party)
    {
        return party?.TargetSettlement
            ?? party?.LeaderHero?.HomeSettlement;
    }

    private static MobileParty GetCurrentHostilePartyTarget(MobileParty party)
    {
        MobileParty targetParty = party?.TargetParty;
        if (targetParty != null
            && targetParty.IsActive
            && targetParty.MapFaction != null
            && targetParty.MapFaction != party?.MapFaction)
        {
            return targetParty;
        }

        return null;
    }

    private static List<InquiryElement> GetAvailableTargets(MobileParty party)
    {
        List<InquiryElement> targets = new();
        Kingdom kingdom = party?.MapFaction as Kingdom;
        if (kingdom == null)
        {
            return targets;
        }

        targets.Add(new InquiryElement("none", "No target (Patrol)", null));

        RFArmyCommandPlan recommendedPlan = RFArmyCommandService.GetRecommendedPlan(party);
        Settlement recommendedSettlement = recommendedPlan?.TargetSettlement;
        MobileParty recommendedHostileParty = recommendedPlan?.TargetParty ?? RFArmyCommandService.GetRecommendedHostilePartyTarget(party);
        IReadOnlyList<MobileParty> hostileTargets = RFArmyCommandService.GetHostilePartyTargets(party);
        Vec2 anchor = party.GetPosition2D;
        HashSet<string> seenSettlementIds = new(StringComparer.Ordinal);
        HashSet<string> seenPartyIds = new(StringComparer.Ordinal);

        foreach (MobileParty hostileTarget in hostileTargets)
        {
            AddHostilePartyOption(
                targets,
                seenPartyIds,
                hostileTarget,
                recommendedHostileParty != null && RFArmyCommandHelpers.IsSameParty(hostileTarget, recommendedHostileParty));
        }

        AddSettlementOption(targets, seenSettlementIds, recommendedSettlement, true);

        List<Settlement> friendlySettlements = kingdom.Settlements?
            .Where(settlement => settlement != null && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage))
            .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(anchor))
            .ThenBy(settlement => settlement.Name?.ToString(), StringComparer.Ordinal)
            .ToList() ?? new List<Settlement>();

        foreach (Settlement settlement in friendlySettlements)
        {
            AddSettlementOption(targets, seenSettlementIds, settlement, settlement == recommendedSettlement);
        }

        List<Settlement> enemySettlements = kingdom.FactionsAtWarWith?
            .Where(faction => faction?.IsKingdomFaction == true)
            .SelectMany(faction => faction.Settlements)
            .Where(settlement => settlement != null && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage))
            .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(anchor))
            .ThenBy(settlement => settlement.Name?.ToString(), StringComparer.Ordinal)
            .ToList() ?? new List<Settlement>();

        foreach (Settlement settlement in enemySettlements)
        {
            AddSettlementOption(targets, seenSettlementIds, settlement, settlement == recommendedSettlement);
        }

        return targets;
    }

    private static void AddHostilePartyOption(List<InquiryElement> targets, HashSet<string> seenPartyIds, MobileParty hostileParty, bool recommended)
    {
        if (hostileParty == null || !hostileParty.IsActive || string.IsNullOrWhiteSpace(hostileParty.StringId) || !seenPartyIds.Add(hostileParty.StringId))
        {
            return;
        }

        string label = $"Intercept {hostileParty.Name}";
        if (recommended)
        {
            label += " [Recommended]";
        }

        targets.Add(new InquiryElement(hostileParty, label, null));
    }

    private static void AddSettlementOption(List<InquiryElement> targets, HashSet<string> seenSettlementIds, Settlement settlement, bool recommended)
    {
        if (settlement == null || string.IsNullOrWhiteSpace(settlement.StringId) || !seenSettlementIds.Add(settlement.StringId))
        {
            return;
        }

        string label = RFArmyCommandHelpers.GetSettlementCommandLabel(settlement);
        if (recommended)
        {
            label += " [Recommended]";
        }

        targets.Add(new InquiryElement(settlement, label, null));
    }

    private void ConfirmTargetSelection(MobileParty party, List<InquiryElement> selected)
    {
        if (party == null || selected == null || selected.Count == 0)
        {
            return;
        }

        object identifier = selected[0].Identifier;
        if (identifier as string == "none")
        {
            RFArmyCommandPlan patrolPlan = RFArmyCommandService.NormalizePlan(party, null, Army.ArmyTypes.Patrolling);
            ApplyPartyPlan(party, patrolPlan, "manual-patrol");
            return;
        }

        if (identifier is MobileParty hostileParty)
        {
            ApplyPartyPlan(party, RFArmyCommandService.CreateManualInterceptPlan(party, hostileParty), "manual-intercept");
            return;
        }

        if (identifier is Settlement settlement)
        {
            RFArmyCommandPlan plan = RFArmyCommandService.NormalizePlan(party, settlement, RFArmyCommandHelpers.GetDefaultArmyBehavior(settlement));
            ApplyPartyPlan(party, plan, "manual-settlement");
        }
    }

    private void ConfirmBehaviorSelection(MobileParty party, List<InquiryElement> selected)
    {
        if (party == null || selected == null || selected.Count == 0 || selected[0].Identifier is not Army.ArmyTypes behavior)
        {
            return;
        }

        Settlement currentTarget = GetCurrentSettlementTarget(party);
        RFArmyCommandPlan plan = RFArmyCommandService.NormalizePlan(party, currentTarget, behavior);
        ApplyPartyPlan(party, plan, "manual-behavior");
    }

    private void ConfirmStrategicPlanSelection(MobileParty party, List<InquiryElement> selected)
    {
        if (party == null || selected == null || selected.Count == 0 || selected[0].Identifier is not RFArmyCommandPlan selectedPlan)
        {
            return;
        }

        ApplyPartyPlan(party, RFArmyCommandService.NormalizePlan(party, selectedPlan), "strategic");
    }

    private void ApplyPartyPlan(MobileParty party, RFArmyCommandPlan plan, string source)
    {
        if (party == null || plan == null)
        {
            return;
        }

        RFArmyCommandService.ApplyPlan(party, plan, registerPlayerOverride: true);
        RFLogger.Log($"[RFArmyCommand] Clan party order applied. leader={party.StringId} source={source} preset={plan.PlanLabel} behavior={plan.ArmyBehavior} target={plan.TargetSettlement?.StringId ?? "none"} targetParty={plan.TargetParty?.StringId ?? "none"}");
        RefreshState();
        ViewModel?.RefreshValues();
    }
}
