using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.AiMade.ArmyCommand;

public sealed class RFArmyCommandCampaignBehavior : CampaignBehaviorBase
{
    private const float ReplanCooldownHours = 8f;
    private readonly Dictionary<string, CampaignTime> _lastPlanUpdate = new();
    private readonly Dictionary<string, string> _lastPlanSignature = new();
    private List<string> _serializedManualOverrides = new();

    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
        CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
    }

    public override void SyncData(IDataStore dataStore)
    {
        if (dataStore.IsSaving)
        {
            _serializedManualOverrides = RFArmyCommandService.ExportManualOverrides();
        }

        dataStore.SyncData("_serializedManualOverrides", ref _serializedManualOverrides);

        if (dataStore.IsLoading)
        {
            RFArmyCommandService.ImportManualOverrides(_serializedManualOverrides);
        }
    }

    private void OnHourlyTickParty(MobileParty party)
    {
        ApplyManualClanPartyOverride(party);

        if (!ShouldManageArmy(party))
        {
            return;
        }

        string partyId = party.StringId ?? string.Empty;
        if (_lastPlanUpdate.TryGetValue(partyId, out CampaignTime lastUpdate)
            && (CampaignTime.Now - lastUpdate).ToHours < ReplanCooldownHours)
        {
            return;
        }

        Army army = party.Army;
        bool hasManualOverride = RFArmyCommandService.TryGetManualOverride(army, out RFArmyCommandPlan manualPlan);
        RFArmyCommandPlan plan = hasManualOverride
            ? manualPlan
            : RFArmyCommandService.GetRecommendedPlan(party);
        if (plan == null || IsPlanAlreadyApplied(party, plan))
        {
            _lastPlanSignature[partyId] = GetPlanSignature(plan);
            _lastPlanUpdate[partyId] = CampaignTime.Now;
            return;
        }

        string planSignature = GetPlanSignature(plan);
        if (!_lastPlanSignature.TryGetValue(partyId, out string lastSignature)
            || !string.Equals(lastSignature, planSignature))
        {
            RFLogger.Log(
                $"[RFArmyCommand] Campaign AI plan leader={party.StringId} kingdom={party.MapFaction?.StringId ?? "none"} source={(ReferenceEquals(plan, manualPlan) ? "manual" : "recommended")} mode=army preset={plan.PlanLabel} behavior={plan.ArmyBehavior} target={plan.TargetSettlement?.StringId ?? "none"} targetParty={plan.TargetParty?.StringId ?? "none"} reason={plan.ReasonText}");
            _lastPlanSignature[partyId] = planSignature;
        }

        RFArmyCommandService.ApplyPlan(army, plan, emitLog: false);
        _lastPlanUpdate[partyId] = CampaignTime.Now;
    }

    private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
    {
        if (mobileParty?.StringId == null)
        {
            return;
        }

        _lastPlanUpdate.Remove(mobileParty.StringId);
        _lastPlanSignature.Remove(mobileParty.StringId);
    }

    private static bool ShouldManageArmy(MobileParty party)
    {
        Army army = party?.Army;
        if (party == null
            || !party.IsLordParty
            || party.IsMainParty
            || party.LeaderHero == null
            || party.MapFaction is not Kingdom kingdom
            || army == null
            || !RFArmyCommandHelpers.IsSameParty(army.LeaderParty, party)
            || !RFArmyCommandHelpers.IsArmyAvailableForOrders(army))
        {
            return false;
        }

        return true;
    }

    private static void ApplyManualClanPartyOverride(MobileParty party)
    {
        if (party == null
            || !party.IsLordParty
            || party.IsMainParty
            || party.ActualClan != Clan.PlayerClan
            || party.Army != null
            || RFArmyCommandHelpers.IsPartyBusy(party))
        {
            return;
        }

        if (!RFArmyCommandService.TryGetManualOverride(party, out RFArmyCommandPlan plan) || plan == null)
        {
            return;
        }

        RFArmyCommandService.ApplyPlan(party, plan, emitLog: false);
    }

    private static string GetPlanSignature(RFArmyCommandPlan plan)
    {
        if (plan == null)
        {
            return "none";
        }

        return string.Join(
            "|",
            plan.PlanLabel ?? string.Empty,
            plan.ArmyBehavior.ToString(),
            plan.TargetSettlement?.StringId ?? string.Empty,
            plan.TargetParty?.StringId ?? string.Empty);
    }

    private static bool IsPlanAlreadyApplied(MobileParty leaderParty, RFArmyCommandPlan plan)
    {
        if (leaderParty == null || plan == null)
        {
            return true;
        }

        Army army = leaderParty.Army;
        Settlement currentTarget = army != null
            ? RFArmyCommandHelpers.ResolveArmyTargetSettlement(leaderParty)
            : leaderParty.TargetSettlement;
        bool sameSettlementTarget = (currentTarget == null && plan.TargetSettlement == null)
            || currentTarget?.StringId == plan.TargetSettlement?.StringId;
        bool samePartyTarget = (leaderParty?.TargetParty == null && plan.TargetParty == null)
            || leaderParty?.TargetParty?.StringId == plan.TargetParty?.StringId;
        bool sameBehavior = army == null || army.ArmyType == plan.ArmyBehavior;

        return sameSettlementTarget && samePartyTarget && sameBehavior;
    }
}
