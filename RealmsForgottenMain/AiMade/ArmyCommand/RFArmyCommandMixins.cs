using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.ArmyCommand;

[ViewModelMixin("RefreshValues")]
internal sealed class RFArmyManagementVMMixin : BaseViewModelMixin<ArmyManagementVM>
{
    private bool _isArmySelected;
    private string _armyBehaviorDescription;
    private bool _recommendedPlanEnabled;
    private string _recommendedOrderText;
    private string _recommendedReasonText;
    private bool _strategicPlanEnabled;
    private string _strategicPlanText;
    private string _strategicPlanReasonText;
    private bool _targetSettlementEnabled;
    private string _targetSettlementName;
    private bool _sendInfluenceEnabled;
    private bool _armyBehaviorEnabled;
    private string _armyBehaviorText;

    [DataSourceProperty]
    public bool ACIsValidArmySelected
    {
        get => _isArmySelected;
        set => SetField(ref _isArmySelected, value, nameof(ACIsValidArmySelected));
    }

    [DataSourceProperty]
    public string ArmyBehaviorDescription
    {
        get => _armyBehaviorDescription;
        set => SetField(ref _armyBehaviorDescription, value, nameof(ArmyBehaviorDescription));
    }

    [DataSourceProperty]
    public bool RecommendedPlanEnabled
    {
        get => _recommendedPlanEnabled;
        set => SetField(ref _recommendedPlanEnabled, value, nameof(RecommendedPlanEnabled));
    }

    [DataSourceProperty]
    public string RecommendedOrderText
    {
        get => _recommendedOrderText;
        set => SetField(ref _recommendedOrderText, value, nameof(RecommendedOrderText));
    }

    [DataSourceProperty]
    public string RecommendedReasonText
    {
        get => _recommendedReasonText;
        set => SetField(ref _recommendedReasonText, value, nameof(RecommendedReasonText));
    }

    [DataSourceProperty]
    public bool StrategicPlanEnabled
    {
        get => _strategicPlanEnabled;
        set => SetField(ref _strategicPlanEnabled, value, nameof(StrategicPlanEnabled));
    }

    [DataSourceProperty]
    public string StrategicPlanText
    {
        get => _strategicPlanText;
        set => SetField(ref _strategicPlanText, value, nameof(StrategicPlanText));
    }

    [DataSourceProperty]
    public string StrategicPlanReasonText
    {
        get => _strategicPlanReasonText;
        set => SetField(ref _strategicPlanReasonText, value, nameof(StrategicPlanReasonText));
    }

    [DataSourceProperty]
    public bool TargetSettlementEnabled
    {
        get => _targetSettlementEnabled;
        set => SetField(ref _targetSettlementEnabled, value, nameof(TargetSettlementEnabled));
    }

    [DataSourceProperty]
    public string TargetSettlementName
    {
        get => _targetSettlementName;
        set => SetField(ref _targetSettlementName, value, nameof(TargetSettlementName));
    }

    [DataSourceProperty]
    public bool SendInfluenceEnabled
    {
        get => _sendInfluenceEnabled;
        set => SetField(ref _sendInfluenceEnabled, value, nameof(SendInfluenceEnabled));
    }

    [DataSourceProperty]
    public bool ArmyBehaviorEnabled
    {
        get => _armyBehaviorEnabled;
        set => SetField(ref _armyBehaviorEnabled, value, nameof(ArmyBehaviorEnabled));
    }

    [DataSourceProperty]
    public string ArmyBehaviorText
    {
        get => _armyBehaviorText;
        set => SetField(ref _armyBehaviorText, value, nameof(ArmyBehaviorText));
    }

    public RFArmyManagementVMMixin(ArmyManagementVM vm)
        : base(vm)
    {
        _ = new RFArmyManagementUIContext(vm, this);
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null)
        {
            return;
        }

        context.InfluenceSent = 0;

        MobileParty initialParty = ResolveInitialArmyManagementParty();
        if (initialParty != null)
        {
            context.CurrentMainParty = initialParty;
        }

        RFArmyManagementVMPatches.EnsureInitialized(vm);
    }

    public override void OnRefresh()
    {
        RFArmyManagementVMPatches.EnsureInitialized(ViewModel);
        RefreshUiState();
    }

    public void UpdateContextOnFirstPartyAdded(MobileParty newMainParty)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null)
        {
            return;
        }

        Army mainPartyArmy = newMainParty?.Army;
        if (mainPartyArmy != null && RFArmyCommandHelpers.IsSameParty(mainPartyArmy.LeaderParty, newMainParty))
        {
            context.MainPartyHasArmy = true;
            context.ArmyBehavior = mainPartyArmy.ArmyType;
            context.TargetSettlement = RFArmyCommandHelpers.ResolveArmyTargetSettlement(newMainParty);
        }
        else
        {
            context.MainPartyHasArmy = false;
            context.TargetSettlement = RFArmyCommandHelpers.GetDefaultTargetSettlement(newMainParty);
            context.ArmyBehavior = RFArmyCommandHelpers.GetDefaultArmyBehavior(context.TargetSettlement);
        }

        context.PendingPlan = null;
        UpdateWidgets();
    }

    public void UpdateContextOnLeaderPartyRemoved()
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null)
        {
            return;
        }

        context.MainPartyHasArmy = false;
        context.TargetSettlement = null;
        context.PendingPlan = null;
        ACIsValidArmySelected = false;
        UpdateWidgets();
    }

    public void UpdateWidgets()
    {
        try
        {
            RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
            if (context == null || !context.MovieIsLoaded)
            {
                return;
            }

            ACIsValidArmySelected = context.CurrentMainParty != null;

            MobileParty selectedParty = context.CurrentMainParty;
            if (selectedParty == null)
            {
                ArmyBehaviorDescription = " ";
                RecommendedPlanEnabled = false;
                RecommendedOrderText = string.Empty;
                RecommendedReasonText = string.Empty;
                StrategicPlanEnabled = false;
                StrategicPlanText = string.Empty;
                StrategicPlanReasonText = string.Empty;
                TargetSettlementEnabled = false;
                TargetSettlementName = string.Empty;
                ArmyBehaviorEnabled = false;
                ArmyBehaviorText = string.Empty;
                SendInfluenceEnabled = false;
                return;
            }

            if (!context.MainPartyHasArmy)
            {
                ApplyRecommendation(selectedParty, setDefaults: context.TargetSettlement == null);
                ArmyBehaviorDescription = "Army Commands";
                TargetSettlementEnabled = true;
                TargetSettlementName = RFArmyCommandHelpers.TruncateText(GetDisplayedTargetName(context, selectedParty), 30);
                ArmyBehaviorEnabled = true;
                ArmyBehaviorText = RFArmyCommandHelpers.TruncateText(RFArmyCommandHelpers.GetArmyBehaviorName(context.ArmyBehavior), 24);
                SendInfluenceEnabled = false;
                return;
            }

            Army selectedArmy = selectedParty.Army;
            if (selectedArmy == null || !RFArmyCommandHelpers.IsSameParty(selectedArmy.LeaderParty, selectedParty))
            {
                context.MainPartyHasArmy = false;
                ApplyRecommendation(selectedParty, setDefaults: true);
                ArmyBehaviorDescription = "Army Commands";
                TargetSettlementEnabled = true;
                TargetSettlementName = RFArmyCommandHelpers.TruncateText(GetDisplayedTargetName(context, selectedParty), 30);
                ArmyBehaviorEnabled = true;
                ArmyBehaviorText = RFArmyCommandHelpers.TruncateText(RFArmyCommandHelpers.GetArmyBehaviorName(context.ArmyBehavior), 24);
                SendInfluenceEnabled = false;
                return;
            }

            ApplyRecommendation(selectedParty, setDefaults: false);
            ArmyBehaviorDescription = RFArmyCommandHelpers.TruncateText(
                RFArmyCommandHelpers.GetArmyCommandHeaderText(selectedArmy),
                42);
            TargetSettlementName = RFArmyCommandHelpers.TruncateText(GetDisplayedTargetName(context, selectedParty), 30);
            ArmyBehaviorText = RFArmyCommandHelpers.TruncateText(RFArmyCommandHelpers.GetArmyBehaviorName(context.ArmyBehavior), 24);

            bool canIssueOrders = RFArmyCommandHelpers.IsArmyAvailableForOrders(selectedArmy);
            ArmyBehaviorEnabled = canIssueOrders;
            TargetSettlementEnabled = canIssueOrders;
            SendInfluenceEnabled = selectedParty.ActualClan != null && selectedParty.ActualClan != Clan.PlayerClan;
        }
        catch (Exception ex)
        {
            RFLogger.Log($"[RFArmyCommand] UpdateWidgets failed: {ex}");
        }
    }

    [DataSourceMethod]
    public void ExecuteUseRecommendedPlan()
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        MobileParty selectedParty = context?.CurrentMainParty;
        if (context == null || selectedParty == null)
        {
            return;
        }

        ApplyRecommendation(selectedParty, setDefaults: true);
        RFLogger.Log($"[RFArmyCommand] Player applied recommended plan. leader={selectedParty.StringId} order={context.PendingPlan?.OrderText ?? "none"}");
        UpdateWidgets();
    }

    [DataSourceMethod]
    public void ExecuteSelectStrategicPlan()
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        MobileParty selectedParty = context?.CurrentMainParty;
        if (context == null || selectedParty == null)
        {
            return;
        }

        List<InquiryElement> plans = RFArmyCommandService.GetSelectablePlans(selectedParty)
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
                ConfirmStrategicPlanSelection,
                _ => { },
                "",
                false),
            false,
            false);
    }

    [DataSourceMethod]
    public void ExecuteSendInfluence()
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null)
        {
            return;
        }

        MobileParty selectedParty = context.CurrentMainParty;
        if (selectedParty?.ActualClan == null || selectedParty.ActualClan == Clan.PlayerClan)
        {
            return;
        }

        if (Clan.PlayerClan.Influence < 50f)
        {
            MBInformationManager.AddQuickInformation(new TextObject("{=rf_army_need_influence}You need 50 influence."), 0, null);
            return;
        }

        RFArmyCommandActions.TransferInfluence(Clan.PlayerClan, selectedParty.ActualClan, 50);
        context.InfluenceSent += 50;
        context.CurrentArmyManagementVM?.RefreshValues();
        RFArmyOverlayUIContext.Instance?.CurrentArmyOverlayVMMixIn?.UpdateLeftArmyOverlay();
    }

    [DataSourceMethod]
    public void ExecuteSelectTargetSettlement()
    {
        List<InquiryElement> settlements = GetAvailableSettlements();
        if (settlements.Count == 0)
        {
            MBInformationManager.AddQuickInformation(new TextObject("{=rf_army_no_target}No valid settlement target right now."), 0, null);
            return;
        }

        MBInformationManager.ShowMultiSelectionInquiry(
            new MultiSelectionInquiryData(
                "Army Target",
                "",
                settlements,
                true,
                1,
                1,
                "Select",
                "Cancel",
                ConfirmTargetSettlementSelection,
                _ => { },
                "",
                false),
            false,
            false);
    }

    [DataSourceMethod]
    public void ExecuteSelectArmyBehavior()
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null)
        {
            return;
        }

        List<InquiryElement> behaviors = RFArmyCommandHelpers.GetAvailableArmyBehaviors(context.TargetSettlement)
            .Select(behavior => new InquiryElement(behavior, RFArmyCommandHelpers.GetArmyBehaviorName(behavior), null))
            .ToList();

        if (behaviors.Count == 0)
        {
            return;
        }

        MBInformationManager.ShowMultiSelectionInquiry(
            new MultiSelectionInquiryData(
                "Army Behavior",
                "",
                behaviors,
                true,
                1,
                1,
                "Select",
                "Cancel",
                ConfirmArmyBehaviorSelection,
                _ => { },
                "",
                false),
            false,
            false);
    }

    private void RefreshUiState()
    {
        UpdateWidgets();
    }

    private void ApplyRecommendation(MobileParty selectedParty, bool setDefaults)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null || selectedParty == null)
        {
            RecommendedPlanEnabled = false;
            RecommendedOrderText = string.Empty;
            RecommendedReasonText = string.Empty;
            return;
        }

        RFArmyCommandPlan plan = RFArmyCommandService.GetRecommendedPlan(selectedParty);
        string manualOrderText = GetCurrentManualOrderText(context, selectedParty);
        RecommendedPlanEnabled = plan != null;
        if (!string.IsNullOrWhiteSpace(manualOrderText))
        {
            RecommendedPlanEnabled = false;
        }

        RecommendedOrderText = RFArmyCommandHelpers.TruncateText(plan?.OrderText ?? string.Empty, 34);
        RecommendedReasonText = plan?.ReasonText ?? string.Empty;
        IReadOnlyList<RFArmyCommandPlan> selectablePlans = RFArmyCommandService.GetSelectablePlans(selectedParty);
        StrategicPlanEnabled = selectablePlans.Count > 0;
        StrategicPlanText = RFArmyCommandHelpers.TruncateText(
            context.PendingPlan?.OrderText
                ?? manualOrderText
                ?? selectablePlans.FirstOrDefault()?.OrderText
                ?? string.Empty,
            30);
        StrategicPlanReasonText = context.PendingPlan?.ReasonText
            ?? selectablePlans.FirstOrDefault()?.ReasonText
            ?? string.Empty;
        if (setDefaults && plan != null)
        {
            context.PendingPlan = plan;
            context.TargetSettlement = plan.TargetSettlement;
            context.ArmyBehavior = plan.ArmyBehavior;
        }
    }

    private static MobileParty ResolveInitialArmyManagementParty()
    {
        if (!Hero.MainHero.IsKingdomLeader)
        {
            return Hero.MainHero.PartyBelongedTo;
        }

        return RFArmyCommandHelpers.GetPreferredSelectedArmy(RFArmyOverlayUIContext.Instance?.SelectedArmy)?.LeaderParty
            ?? Hero.MainHero.PartyBelongedTo;
    }

    private static string GetDisplayedTargetName(RFArmyManagementUIContext context, MobileParty selectedParty)
    {
        MobileParty targetParty = context?.PendingPlan?.TargetParty;
        if (targetParty != null
            && targetParty.IsActive
            && targetParty.MapFaction != null
            && targetParty.MapFaction != selectedParty?.MapFaction)
        {
            return $"Intercept {targetParty.Name}";
        }

        if (context?.TargetSettlement != null)
        {
            return RFArmyCommandHelpers.GetTargetSettlementName(context.TargetSettlement);
        }

        Army selectedPartyArmy = selectedParty?.Army;
        if (selectedPartyArmy != null
            && RFArmyCommandHelpers.IsSameParty(selectedPartyArmy.LeaderParty, selectedParty))
        {
            targetParty = selectedParty.TargetParty;
            if (targetParty != null
                && targetParty.IsActive
                && targetParty.MapFaction != null
                && targetParty.MapFaction != selectedParty.MapFaction)
            {
                return $"Intercept {targetParty.Name}";
            }
        }

        return RFArmyCommandHelpers.GetTargetSettlementName(context?.TargetSettlement);
    }

    private static List<InquiryElement> GetAvailableSettlements()
    {
        List<InquiryElement> list = new();
        Kingdom kingdom = Hero.MainHero.Clan?.Kingdom;
        if (kingdom == null)
        {
            return list;
        }

        if (RFArmyManagementUIContext.Instance?.MainPartyHasArmy == true)
        {
            list.Add(new InquiryElement("none", "No target (Patrol)", null));
        }

        MobileParty selectedParty = RFArmyManagementUIContext.Instance?.CurrentMainParty ?? MobileParty.MainParty;
        RFArmyCommandPlan recommendedPlan = RFArmyCommandService.GetRecommendedPlan(selectedParty);
        Settlement recommendedTarget = recommendedPlan?.TargetSettlement;
        MobileParty recommendedTargetParty = recommendedPlan?.TargetParty ?? RFArmyCommandService.GetRecommendedHostilePartyTarget(selectedParty);
        IReadOnlyList<MobileParty> hostileTargets = RFArmyCommandService.GetHostilePartyTargets(selectedParty);
        Vec2 anchor = selectedParty?.GetPosition2D ?? MobileParty.MainParty?.GetPosition2D ?? Vec2.Zero;
        HashSet<string> seenSettlementIds = new(StringComparer.Ordinal);
        HashSet<string> seenPartyIds = new(StringComparer.Ordinal);
        List<Settlement> friendlySettlements = new();
        List<Settlement> enemySettlements = new();

        foreach (Settlement settlement in kingdom.Settlements)
        {
            if (settlement.IsCastle || settlement.IsTown || settlement.IsVillage)
            {
                friendlySettlements.Add(settlement);
            }
        }

        foreach (IFaction faction in kingdom.FactionsAtWarWith)
        {
            if (!faction.IsKingdomFaction)
            {
                continue;
            }

            foreach (Settlement settlement in faction.Settlements)
            {
                if (settlement.IsVillage || settlement.IsCastle || settlement.IsTown)
                {
                    enemySettlements.Add(settlement);
                }
            }
        }

        foreach (MobileParty hostileTarget in hostileTargets)
        {
            AddHostilePartyOption(
                list,
                seenPartyIds,
                hostileTarget,
                appendRecommended: recommendedTargetParty != null && RFArmyCommandHelpers.IsSameParty(hostileTarget, recommendedTargetParty));
        }
        AddSettlementOption(list, seenSettlementIds, recommendedTarget, appendRecommended: true);

        foreach (Settlement settlement in friendlySettlements
                     .Where(settlement => settlement != null)
                     .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(anchor))
                     .ThenBy(settlement => settlement.Name?.ToString(), StringComparer.Ordinal))
        {
            AddSettlementOption(list, seenSettlementIds, settlement, appendRecommended: false);
        }

        foreach (Settlement settlement in enemySettlements
                     .Where(settlement => settlement != null)
                     .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(anchor))
                     .ThenBy(settlement => settlement.Name?.ToString(), StringComparer.Ordinal))
        {
            AddSettlementOption(list, seenSettlementIds, settlement, appendRecommended: settlement == recommendedTarget);
        }

        return list;
    }

    private static void AddHostilePartyOption(List<InquiryElement> list, HashSet<string> seenPartyIds, MobileParty hostileParty, bool appendRecommended)
    {
        if (hostileParty == null || !hostileParty.IsActive || string.IsNullOrWhiteSpace(hostileParty.StringId) || !seenPartyIds.Add(hostileParty.StringId))
        {
            return;
        }

        string label = $"Intercept {hostileParty.Name}";
        if (appendRecommended)
        {
            label += " [Recommended]";
        }

        list.Add(new InquiryElement(hostileParty, label, null));
    }

    private static void AddSettlementOption(List<InquiryElement> list, HashSet<string> seenSettlementIds, Settlement settlement, bool appendRecommended)
    {
        if (settlement == null || string.IsNullOrWhiteSpace(settlement.StringId) || !seenSettlementIds.Add(settlement.StringId))
        {
            return;
        }

        string label = RFArmyCommandHelpers.GetSettlementCommandLabel(settlement);
        if (appendRecommended)
        {
            label += " [Recommended]";
        }

        list.Add(new InquiryElement(settlement, label, null));
    }

    private static string GetCurrentManualOrderText(RFArmyManagementUIContext context, MobileParty selectedParty)
    {
        if (context == null)
        {
            return null;
        }

        MobileParty targetParty = context.PendingPlan?.TargetParty;
        if (targetParty != null
            && targetParty.IsActive
            && targetParty.MapFaction != null
            && targetParty.MapFaction != selectedParty?.MapFaction)
        {
            return $"Intercept {targetParty.Name}";
        }

        if (context.TargetSettlement == null && context.ArmyBehavior == Army.ArmyTypes.Patrolling)
        {
            return "Manual Patrol";
        }

        if (context.TargetSettlement != null)
        {
            return RFArmyCommandHelpers.GetSettlementCommandLabel(context.TargetSettlement);
        }

        return null;
    }

    private void ConfirmTargetSettlementSelection(List<InquiryElement> selected)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null)
        {
            return;
        }

        if (selected == null || selected.Count == 0)
        {
            return;
        }

        if (selected[0].Identifier as string == "none")
        {
            context.PendingPlan = null;
            context.TargetSettlement = null;
            context.ArmyBehavior = Army.ArmyTypes.Patrolling;
            UpdateWidgets();
            return;
        }

        if (selected[0].Identifier is MobileParty hostileParty)
        {
            context.PendingPlan = RFArmyCommandService.CreateManualInterceptPlan(context.CurrentMainParty, hostileParty);
            context.TargetSettlement = null;
            context.ArmyBehavior = context.PendingPlan?.ArmyBehavior ?? Army.ArmyTypes.Defender;
            RFLogger.Log($"[RFArmyCommand] Player selected hostile target. leader={context.CurrentMainParty?.StringId ?? "none"} targetParty={hostileParty.StringId} behavior={context.ArmyBehavior}");
            UpdateWidgets();
            return;
        }

        Settlement settlement = selected[0].Identifier as Settlement;
        if (settlement == null)
        {
            return;
        }

        context.PendingPlan = null;
        context.TargetSettlement = settlement;
        context.ArmyBehavior = RFArmyCommandHelpers.GetDefaultArmyBehavior(settlement);
        RFLogger.Log($"[RFArmyCommand] Player selected settlement target. leader={context.CurrentMainParty?.StringId ?? "none"} target={settlement.StringId} behavior={context.ArmyBehavior}");
        UpdateWidgets();
    }

    private void ConfirmArmyBehaviorSelection(List<InquiryElement> selected)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null)
        {
            return;
        }

        if (selected == null || selected.Count == 0)
        {
            return;
        }

        if (selected[0].Identifier is not Army.ArmyTypes armyBehavior)
        {
            return;
        }

        context.PendingPlan = null;
        context.ArmyBehavior = armyBehavior;
        if (armyBehavior == Army.ArmyTypes.Patrolling)
        {
            context.TargetSettlement = null;
        }

        RFLogger.Log($"[RFArmyCommand] Player selected army behavior. leader={context.CurrentMainParty?.StringId ?? "none"} behavior={armyBehavior}");
        UpdateWidgets();
    }

    private void ConfirmStrategicPlanSelection(List<InquiryElement> selected)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null || selected == null || selected.Count == 0)
        {
            return;
        }

        if (selected[0].Identifier is not RFArmyCommandPlan selectedPlan)
        {
            return;
        }

        RFArmyCommandPlan normalizedPlan = RFArmyCommandService.NormalizePlan(context.CurrentMainParty, selectedPlan);
        context.PendingPlan = normalizedPlan;
        context.TargetSettlement = normalizedPlan?.TargetSettlement;
        context.ArmyBehavior = normalizedPlan?.ArmyBehavior ?? context.ArmyBehavior;
        RFLogger.Log($"[RFArmyCommand] Player selected strategic plan. leader={context.CurrentMainParty?.StringId ?? "none"} plan={normalizedPlan?.PlanLabel ?? "none"} order={normalizedPlan?.OrderText ?? "none"}");
        UpdateWidgets();
    }
}

[ViewModelMixin("RefreshValues")]
internal sealed class RFArmyMenuOverlayVMMixin : BaseViewModelMixin<ArmyMenuOverlayVM>
{
    private RFArmyOverlayArmyListVM _armyOverlayArmiesList;
    private BasicTooltipViewModel _armiesHint;
    private BasicTooltipViewModel _partiesHint;
    private string _armiesCount;
    private string _menCount;
    private string _partiesCount;
    private bool _selectedArmyVisible;
    private string _selectedArmyName;
    private string _selectedArmyOrder;
    private string _selectedArmyActionText;

    [DataSourceProperty]
    public bool ACSelectedArmyVisible
    {
        get => _selectedArmyVisible;
        set => SetField(ref _selectedArmyVisible, value, nameof(ACSelectedArmyVisible));
    }

    [DataSourceProperty]
    public string ACSelectedArmyName
    {
        get => _selectedArmyName;
        set => SetField(ref _selectedArmyName, value, nameof(ACSelectedArmyName));
    }

    [DataSourceProperty]
    public string ACSelectedArmyOrder
    {
        get => _selectedArmyOrder;
        set => SetField(ref _selectedArmyOrder, value, nameof(ACSelectedArmyOrder));
    }

    [DataSourceProperty]
    public string ACSelectedArmyActionText
    {
        get => _selectedArmyActionText;
        set => SetField(ref _selectedArmyActionText, value, nameof(ACSelectedArmyActionText));
    }

    [DataSourceProperty]
    public BasicTooltipViewModel ACArmiesHint
    {
        get => _armiesHint;
        set => SetField(ref _armiesHint, value, nameof(ACArmiesHint));
    }

    [DataSourceProperty]
    public BasicTooltipViewModel ACPartiesHint
    {
        get => _partiesHint;
        set => SetField(ref _partiesHint, value, nameof(ACPartiesHint));
    }

    [DataSourceProperty]
    public string ACArmiesCount
    {
        get => _armiesCount;
        set => SetField(ref _armiesCount, value, nameof(ACArmiesCount));
    }

    [DataSourceProperty]
    public string ACPartiesCount
    {
        get => _partiesCount;
        set => SetField(ref _partiesCount, value, nameof(ACPartiesCount));
    }

    [DataSourceProperty]
    public string ACManCount
    {
        get => _menCount;
        set => SetField(ref _menCount, value, nameof(ACManCount));
    }

    [DataSourceProperty]
    public RFArmyOverlayArmyListVM ArmyOverlayArmiesList
    {
        get => _armyOverlayArmiesList;
        set => SetField(ref _armyOverlayArmiesList, value, nameof(ArmyOverlayArmiesList));
    }

    public RFArmyMenuOverlayVMMixin(ArmyMenuOverlayVM vm)
        : base(vm)
    {
        _ = new RFArmyOverlayUIContext(vm, this);
        vm.IsInitializationOver = false;
        try
        {
            ArmyOverlayArmiesList = new RFArmyOverlayArmyListVM(new MBBindingList<RFSelectableArmyLineVM>());
            if (RFArmyOverlayUIContext.Instance.SelectedArmy != null)
            {
                Hero selectedLeader = RFArmyOverlayUIContext.Instance.SelectedArmy.LeaderParty?.LeaderHero;
                if (selectedLeader != null)
                {
                    RFArmyOverlayUIContext.Instance.SelectedArmy = Hero.Find(selectedLeader.StringId)?.PartyBelongedTo?.Army;
                }
            }

            RFArmyOverlayUIContext.Instance.SelectedArmy = RFArmyCommandHelpers.GetPreferredSelectedArmy(RFArmyOverlayUIContext.Instance.SelectedArmy);

            RenewLeftArmyOverlay(null, false);
            CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
            UpdateTopWidgets();
        }
        finally
        {
            vm.IsInitializationOver = true;
        }
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, UpdateLeftArmyOverlay);
    }

    public override void OnRefresh()
    {
        UpdateLineSelection();
        UpdateSelectedArmySummary();
    }

    public override void OnFinalize()
    {
        CampaignEvents.HourlyTickEvent.ClearListeners(this);
    }

    public void OnArmyDisband(Army army)
    {
        RFArmyOverlayUIContext overlayContext = RFArmyOverlayUIContext.Instance;
        if (army?.LeaderParty?.ActualClan?.Kingdom != Clan.PlayerClan?.Kingdom || overlayContext?.CurrentArmyOverlayVM == null)
        {
            return;
        }

        overlayContext.CurrentArmyOverlayVM.IsInitializationOver = false;
        try
        {
            if (RFArmyCommandHelpers.IsSameArmy(army, overlayContext.SelectedArmy))
            {
                overlayContext.SelectedArmy = RFArmyCommandHelpers.GetPreferredSelectedArmy();
            }

            RenewLeftArmyOverlay(new List<Army> { army });
            UpdateTopWidgets();
        }
        finally
        {
            overlayContext.CurrentArmyOverlayVM.IsInitializationOver = true;
        }
    }

    public void OnArmyGathered(Army army)
    {
        RFArmyOverlayUIContext overlayContext = RFArmyOverlayUIContext.Instance;
        if (army?.Kingdom != Clan.PlayerClan?.Kingdom || overlayContext?.CurrentArmyOverlayVM == null)
        {
            return;
        }

        overlayContext.CurrentArmyOverlayVM.IsInitializationOver = false;
        try
        {
            RenewLeftArmyOverlay();
            UpdateTopWidgets();
        }
        finally
        {
            overlayContext.CurrentArmyOverlayVM.IsInitializationOver = true;
        }
    }

    public RFArmyLineUIContext UpdateLineContext(RFArmyLineUIContext context, Army army)
    {
        if (army?.LeaderParty == null)
        {
            context.LeaderParty = null;
            context.AttachedPartiesCount = 0;
            context.TotalAssignedPartiesCount = 0;
            context.PartiesWithinADayDistanceCount = 0;
            context.CurrentArmyFood = 0f;
            context.TotalArmyFood = 0f;
            context.TotalArmyFoodChange = 0f;
            context.CurrentArmyInfluence = 0f;
            context.DailyArmyInfluenceChange = 0f;
            context.CurrentCohesion = 0f;
            context.DailyCohesionChange = 0f;
            context.MenCount = 0;
            context.PotentialMenCount = 0;
            context.MenJoiningToday = 0;
            context.LostCohesionCostValue = 0;
            context.SendItemInfluenceCost = 0;
            context.DisbandInfluenceCost = 0;
            return context;
        }

        List<MobileParty> allParties = context.AllPartiesFromArmy;
        List<MobileParty> joiningToday = context.PartiesJoiningToday;
        context.LeaderParty = army.LeaderParty;
        context.AttachedPartiesCount = RFArmyCommandHelpers.GetAttachedPartiesCount(army);
        context.TotalAssignedPartiesCount = RFArmyCommandHelpers.GetTotalAssignedPartiesCount(allParties);
        context.PartiesWithinADayDistanceCount = RFArmyCommandHelpers.GetPartiesWithinADayDistanceCount(joiningToday);
        context.CurrentArmyFood = RFArmyCommandHelpers.GetCurrentArmyFood(army);
        context.TotalArmyFood = RFArmyCommandHelpers.GetTotalArmyFood(allParties);
        context.TotalArmyFoodChange = RFArmyCommandHelpers.GetTotalArmyFoodChange(army, joiningToday);
        context.CurrentArmyInfluence = RFArmyCommandHelpers.GetCurrentArmyInfluence(army);
        context.DailyArmyInfluenceChange = RFArmyCommandHelpers.GetDailyArmyInfluenceChange(army);
        context.CurrentCohesion = RFArmyCommandHelpers.GetCurrentCohesion(army);
        context.DailyCohesionChange = RFArmyCommandHelpers.GetDailyCohesionChange(army);
        context.MenCount = RFArmyCommandHelpers.GetMenCount(army);
        context.PotentialMenCount = RFArmyCommandHelpers.GetPotentialMenCount(allParties);
        context.MenJoiningToday = RFArmyCommandHelpers.GetMenJoiningToday(joiningToday);
        context.LostCohesionCostValue = RFArmyCommandHelpers.GetLostCohesionCostValue(army);
        context.SendItemInfluenceCost = RFArmyCommandHelpers.GetSendItemInfluenceCost(army);
        context.DisbandInfluenceCost = RFArmyCommandHelpers.GetDisbandInfluenceCost(army);
        return context;
    }

    public void RenewLeftArmyOverlay(List<Army> excludedArmies = null, bool refreshRightOverlay = true)
    {
        RFArmyOverlayUIContext overlayContext = RFArmyOverlayUIContext.Instance;
        if (Clan.PlayerClan?.Kingdom == null || overlayContext == null)
        {
            return;
        }

        ArmyOverlayArmiesList.ClearLines();
        overlayContext.ArmiesCount = 0;
        overlayContext.MenInArmiesCount = 0;
        overlayContext.MenInKingdomCount = 0;
        overlayContext.PartiesInArmiesCount = 0;
        overlayContext.PartiesInKingdomCount = 0;

        List<Army> kingdomArmies = Clan.PlayerClan.Kingdom.Armies?.Where(army => army != null).ToList() ?? new List<Army>();
        foreach (Army army in kingdomArmies)
        {
            if (army?.LeaderParty != null && (excludedArmies == null || !excludedArmies.Any(excludedArmy => RFArmyCommandHelpers.IsSameArmy(excludedArmy, army))))
            {
                ArmyOverlayArmiesList.AddLine(RFArmyLineWidgetBuilders.BuildArmyLine());
            }
        }

        ArmyOverlayArmiesList.UpdateValues();
        if (refreshRightOverlay)
        {
            RFArmyCommandMapScreenPatch.RefreshArmyOverlay();
            CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
        }

        UpdateLineSelection();
    }

    public void UpdateLeftArmyOverlay()
    {
        RFArmyOverlayUIContext overlayContext = RFArmyOverlayUIContext.Instance;
        if (overlayContext?.CurrentArmyOverlayVM == null)
        {
            return;
        }

        overlayContext.CurrentArmyOverlayVM.IsInitializationOver = false;
        try
        {
            overlayContext.ArmiesCount = 0;
            overlayContext.MenInArmiesCount = 0;
            overlayContext.MenInKingdomCount = 0;
            overlayContext.PartiesInArmiesCount = 0;
            overlayContext.PartiesInKingdomCount = 0;
            ArmyOverlayArmiesList.UpdateValues();
            UpdateTopWidgets();
        }
        finally
        {
            overlayContext.CurrentArmyOverlayVM.IsInitializationOver = true;
        }
    }

    public void UpdateLineSelection()
    {
        ArmyOverlayArmiesList.UpdateSelection();
    }

    public void UpdateSelectedArmySummary()
    {
        RFArmyOverlayUIContext overlayContext = RFArmyOverlayUIContext.Instance;
        Army selectedArmy = RFArmyCommandHelpers.GetPreferredSelectedArmy(overlayContext?.SelectedArmy);
        if (selectedArmy == null)
        {
            bool canCreateArmy = Clan.PlayerClan?.Kingdom != null
                && !Clan.PlayerClan.IsUnderMercenaryService
                && MobileParty.MainParty?.LeaderHero != null;
            ACSelectedArmyVisible = canCreateArmy;
            ACSelectedArmyName = canCreateArmy ? "Create Army" : string.Empty;
            ACSelectedArmyOrder = canCreateArmy ? "No active armies in your kingdom." : string.Empty;
            ACSelectedArmyActionText = canCreateArmy ? "Create" : string.Empty;
            return;
        }

        if (overlayContext != null)
        {
            overlayContext.SelectedArmy = selectedArmy;
        }
        Hero leader = selectedArmy.LeaderParty?.LeaderHero;
        string orderText;
        try
        {
            orderText = selectedArmy.GetLongTermBehaviorText(false)?.ToString();
        }
        catch (Exception ex)
        {
            RFLogger.Log($"[RFArmyCommand] UpdateSelectedArmySummary long-term behavior text failed: {ex}");
            orderText = null;
        }

        if (string.IsNullOrWhiteSpace(orderText))
        {
            orderText = RFArmyCommandHelpers.GetArmyCommandSummaryText(selectedArmy);
        }
        else
        {
            orderText = RFArmyCommandHelpers.TruncateText(orderText, 40);
        }

        ACSelectedArmyVisible = true;
        ACSelectedArmyName = leader != null ? leader.Name.ToString() : "Unnamed army";
        ACSelectedArmyOrder = orderText;
        ACSelectedArmyActionText = "Manage";
    }

    public void ExecuteOpenSelectedArmyManagement()
    {
        RFArmyOverlayUIContext overlayContext = RFArmyOverlayUIContext.Instance;
        if (overlayContext?.CurrentArmyOverlayVM == null || Clan.PlayerClan?.Kingdom == null)
        {
            return;
        }

        overlayContext.CurrentArmyOverlayVM.ExecuteOpenArmyManagement();
    }

    private void UpdateTopWidgets()
    {
        RFArmyOverlayUIContext overlayContext = RFArmyOverlayUIContext.Instance;
        if (overlayContext == null)
        {
            ACArmiesHint = BuildOverlayHint("Kingdom armies", "Active armies in your kingdom.");
            ACPartiesHint = BuildOverlayHint("Army parties", "Parties assigned to armies / total kingdom parties.");
            ACArmiesCount = "0";
            ACManCount = "0/0";
            ACPartiesCount = "0/0";
            UpdateSelectedArmySummary();
            return;
        }

        ACArmiesHint = BuildOverlayHint("Kingdom armies", $"{overlayContext.ArmiesCount} active armies in your kingdom.");
        ACPartiesHint = BuildOverlayHint("Army parties", $"{overlayContext.PartiesInArmiesCount} parties are assigned to armies out of {overlayContext.PartiesInKingdomCount} kingdom parties.");
        ACArmiesCount = overlayContext.ArmiesCount.ToString();
        ACManCount = $"{overlayContext.MenInArmiesCount}/{overlayContext.MenInKingdomCount}";
        ACPartiesCount = $"{overlayContext.PartiesInArmiesCount}/{overlayContext.PartiesInKingdomCount}";
        UpdateSelectedArmySummary();
    }

    private static BasicTooltipViewModel BuildOverlayHint(string title, string description)
    {
        return new BasicTooltipViewModel(() => new List<TooltipProperty>
        {
            new TooltipProperty(title, description, 0, false, TooltipProperty.TooltipPropertyFlags.None)
        });
    }
}
