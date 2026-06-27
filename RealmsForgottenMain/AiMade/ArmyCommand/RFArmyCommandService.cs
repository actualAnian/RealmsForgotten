using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.ArmyCommand;

internal sealed class RFArmyCommandPlan
{
    public Settlement TargetSettlement { get; init; }

    public MobileParty TargetParty { get; init; }

    public Army.ArmyTypes ArmyBehavior { get; init; }

    public string PlanLabel { get; init; } = string.Empty;

    public string OrderText { get; init; } = string.Empty;

    public string ReasonText { get; init; } = string.Empty;
}

internal sealed class RFArmyCommandManualOverride
{
    public RFArmyCommandPlan Plan { get; init; }

    public CampaignTime ExpiresAt { get; init; }
}

internal enum RFArmyCommanderProfile
{
    Balanced,
    Aggressive,
    Methodical,
    Cautious,
    Raider
}

internal enum RFArmyCommandPreset
{
    HoldReserve,
    FrontierDefense,
    RelieveSiege,
    GatherForCampaign,
    RaidEnemySupport,
    BesiegeStronghold,
    InterceptHostileArmy
}

internal static class RFArmyCommandService
{
    private const float ManualOverrideDurationHours = 72f;
    private static readonly Dictionary<string, RFArmyCommandManualOverride> ManualOverrides = new();

    public static IReadOnlyList<RFArmyCommandPlan> GetSelectablePlans(MobileParty leaderParty)
    {
        List<RFArmyCommandPlan> plans = new();
        RFArmyCommandPlan recommended = GetRecommendedPlan(leaderParty);
        AddPlanUnique(plans, recommended);

        if (leaderParty?.MapFaction is not Kingdom kingdom)
        {
            return plans;
        }

        List<Kingdom> enemyKingdoms = kingdom.FactionsAtWarWith?.OfType<Kingdom>().Where(candidate => candidate != null).ToList() ?? new List<Kingdom>();
        Kingdom enemy = RFArmyCommandHelpers.GetStrategicPrimaryEnemy(kingdom)
            ?? enemyKingdoms.FirstOrDefault();
        if (enemy == null)
        {
            return plans;
        }

        Settlement objective = RFArmyCommandHelpers.GetStrategicObjectiveSettlement(kingdom, enemy);
        Settlement frontline = RFArmyCommandHelpers.GetStrategicFrontlineSettlement(kingdom, enemy);
        Settlement theater = RFArmyCommandHelpers.GetStrategicTheaterSettlement(kingdom, enemy);
        Settlement threatenedHomeland = FindThreatenedHomelandSettlement(kingdom, enemy);
        Settlement rallyPoint = FindFriendlyRallySettlement(leaderParty, kingdom, threatenedHomeland ?? frontline ?? theater ?? objective);
        MobileParty hostileFieldThreat = FindPriorityEnemyParty(kingdom, enemy, leaderParty, threatenedHomeland ?? frontline ?? theater ?? objective);

        List<Settlement> kingdomSettlements = kingdom.Fiefs?.Select(fief => fief?.Settlement).Where(settlement => settlement != null).ToList() ?? new List<Settlement>();
        Settlement frontierDefenseTarget = FirstMatchingSettlement(
                new[] { threatenedHomeland, frontline, theater, objective },
                settlement => settlement?.MapFaction == kingdom)
            ?? kingdomSettlements.FirstOrDefault();

        Settlement raidTarget = FirstMatchingSettlement(
            new[] { objective, frontline, theater },
            settlement => settlement?.MapFaction == enemy && settlement.IsVillage);

        Settlement siegeTarget = FirstMatchingSettlement(
            new[] { objective, frontline, theater },
            settlement => settlement?.MapFaction == enemy && !settlement.IsVillage);

        if (frontierDefenseTarget != null)
        {
            AddPlanUnique(plans, BuildPlan(
                frontierDefenseTarget,
                null,
                Army.ArmyTypes.Defender,
                "Frontier Defense",
                $"Frontier Defense -> {frontierDefenseTarget.Name}",
                "Hold the most exposed friendly approach and block enemy advances."));
        }

        if (rallyPoint != null)
        {
            AddPlanUnique(plans, BuildPlan(
                rallyPoint,
                null,
                Army.ArmyTypes.Defender,
                "Gather For Campaign",
                $"Gather For Campaign -> {rallyPoint.Name}",
                "Assemble the army at a safe rally point before pushing deeper."));

            AddPlanUnique(plans, BuildPlan(
                rallyPoint,
                null,
                Army.ArmyTypes.Patrolling,
                "Hold Reserve",
                $"Hold Reserve -> {rallyPoint.Name}",
                "Keep the army in reserve near a safe mustering ground."));
        }

        if (hostileFieldThreat != null)
        {
            AddPlanUnique(plans, BuildPlan(
                null,
                hostileFieldThreat,
                Army.ArmyTypes.Defender,
                "Intercept Hostile Army",
                $"Intercept Hostile Army -> {hostileFieldThreat.Name}",
                "A hostile field force is exposed and can be intercepted before it reaches a stronger position."));
        }

        if (raidTarget != null)
        {
            AddPlanUnique(plans, BuildPlan(
                raidTarget,
                null,
                Army.ArmyTypes.Raider,
                "Raid Enemy Support",
                $"Raid Enemy Support -> {raidTarget.Name}",
                "Strike an enemy village to pressure supplies and draw out defenders."));
        }

        if (siegeTarget != null)
        {
            AddPlanUnique(plans, BuildPlan(
                siegeTarget,
                null,
                Army.ArmyTypes.Besieger,
                "Besiege Stronghold",
                $"Besiege Stronghold -> {siegeTarget.Name}",
                "Commit the army against a fortified enemy objective."));
        }

        return plans;
    }

    public static RFArmyCommandPlan GetRecommendedPlan(MobileParty leaderParty)
    {
        if (leaderParty?.MapFaction is not Kingdom kingdom)
        {
            Settlement fallbackSettlement = RFArmyCommandHelpers.GetDefaultTargetSettlement(leaderParty);
            Army.ArmyTypes fallbackBehavior = RFArmyCommandHelpers.GetDefaultArmyBehavior(fallbackSettlement);
            return BuildPlan(
                fallbackSettlement,
                null,
                fallbackBehavior,
                "Fallback Command",
                "Fallback Command -> Default target",
                "No war-system signal was available.");
        }

        List<Kingdom> enemyKingdoms = kingdom.FactionsAtWarWith?.OfType<Kingdom>().Where(candidate => candidate != null).ToList() ?? new List<Kingdom>();
        Kingdom enemy = RFArmyCommandHelpers.GetStrategicPrimaryEnemy(kingdom)
            ?? enemyKingdoms.FirstOrDefault();
        if (enemy == null)
        {
            Settlement rallyPoint = RFArmyCommandHelpers.GetDefaultTargetSettlement(leaderParty);
            return BuildPlan(
                rallyPoint,
                null,
                Army.ArmyTypes.Defender,
                "Frontier Defense",
                "Frontier Defense -> Hold nearby lands",
                "The kingdom has no active enemy focus right now.");
        }

        bool homelandDefense = RFArmyCommandHelpers.IsHomelandDefense(kingdom, enemy);
        string phase = RFArmyCommandHelpers.GetStrategicCampaignPhaseName(kingdom, enemy);
        string focusMode = RFArmyCommandHelpers.GetStrategicFocusModeName(kingdom, enemy);
        string operationalState = RFArmyCommandHelpers.GetStrategicOperationalStateName(kingdom, enemy);
        float homePressure = RFArmyCommandHelpers.GetStrategicHomeFrontPressure(kingdom);
        float coalitionRoleFactor = RFArmyCommandHelpers.GetStrategicCoalitionRoleFactor(kingdom, enemy);
        float coalitionPullFactor = RFArmyCommandHelpers.GetStrategicCoalitionPullFactor(kingdom, enemy);
        float decisivePressure = RFArmyCommandHelpers.GetStrategicDecisivePressure(kingdom, enemy);
        float unresolvedFrontPressure = RFArmyCommandHelpers.GetStrategicUnresolvedFrontPressure(kingdom, enemy);
        Settlement objective = RFArmyCommandHelpers.GetStrategicObjectiveSettlement(kingdom, enemy);
        Settlement frontline = RFArmyCommandHelpers.GetStrategicFrontlineSettlement(kingdom, enemy);
        Settlement theater = RFArmyCommandHelpers.GetStrategicTheaterSettlement(kingdom, enemy);
        Settlement threatenedHomeland = FindThreatenedHomelandSettlement(kingdom, enemy);
        bool regroupRecommended = ShouldRegroup(leaderParty, operationalState);
        RFArmyCommanderProfile commanderProfile = GetCommanderProfile(leaderParty, threatenedHomeland != null || homelandDefense);
        MobileParty hostileFieldThreat = FindPriorityEnemyParty(kingdom, enemy, leaderParty, threatenedHomeland ?? frontline ?? theater);

        Settlement target = regroupRecommended
            ? FindFriendlyRallySettlement(leaderParty, kingdom, threatenedHomeland ?? frontline ?? theater)
            : SelectRecommendedTarget(kingdom, enemy, homelandDefense, phase, objective, frontline, theater, threatenedHomeland, hostileFieldThreat, leaderParty, commanderProfile)
            ?? RFArmyCommandHelpers.GetDefaultTargetSettlement(leaderParty);
        Army.ArmyTypes behavior = SelectRecommendedBehavior(kingdom, enemy, homelandDefense, phase, operationalState, target, regroupRecommended, decisivePressure, hostileFieldThreat, commanderProfile);
        RFArmyCommandPreset preset = SelectRecommendedPreset(kingdom, homelandDefense, regroupRecommended, target, threatenedHomeland, hostileFieldThreat, behavior);
        MobileParty targetParty = preset == RFArmyCommandPreset.InterceptHostileArmy ? hostileFieldThreat : null;

        string planLabel = GetPresetLabel(preset);
        string orderText = targetParty != null
            ? $"{planLabel} -> {targetParty.Name}"
            : target == null
            ? $"{planLabel} -> {RFArmyCommandHelpers.GetArmyBehaviorName(behavior)}"
            : $"{planLabel} -> {target.Name}";
        string reasonText = BuildReasonText(
            planLabel,
            focusMode,
            phase,
            operationalState,
            objective,
            frontline,
            theater,
            homelandDefense,
            homePressure,
            coalitionRoleFactor,
            coalitionPullFactor,
            decisivePressure,
            unresolvedFrontPressure,
            leaderParty,
            regroupRecommended,
            hostileFieldThreat,
            commanderProfile);

        return BuildPlan(target, targetParty, behavior, planLabel, orderText, reasonText);
    }

    public static RFArmyCommandPlan NormalizePlan(MobileParty leaderParty, Settlement selectedTarget, Army.ArmyTypes selectedBehavior)
    {
        RFArmyCommandPlan recommended = GetRecommendedPlan(leaderParty);
        if (selectedBehavior == Army.ArmyTypes.Patrolling && selectedTarget == null)
        {
            return BuildPlan(null, null, selectedBehavior, "Manual Patrol", RFArmyCommandHelpers.GetArmyBehaviorName(selectedBehavior), recommended.ReasonText);
        }

        if (selectedTarget == null)
        {
            return recommended;
        }

        if (selectedBehavior == Army.ArmyTypes.Raider && !selectedTarget.IsVillage)
        {
            return recommended;
        }

        if (selectedBehavior == Army.ArmyTypes.Besieger && selectedTarget.IsVillage)
        {
            return recommended;
        }

        if (selectedBehavior == Army.ArmyTypes.Defender && selectedTarget.MapFaction != leaderParty?.MapFaction)
        {
            return recommended;
        }

        string orderText = $"Manual Order -> {selectedTarget.Name}";
        return BuildPlan(selectedTarget, null, selectedBehavior, "Manual Order", orderText, recommended.ReasonText);
    }

    public static RFArmyCommandPlan NormalizePlan(MobileParty leaderParty, RFArmyCommandPlan requestedPlan)
    {
        if (requestedPlan == null)
        {
            return GetRecommendedPlan(leaderParty);
        }

        if (requestedPlan.TargetParty != null
            && requestedPlan.TargetParty.IsActive
            && requestedPlan.TargetParty.MapFaction != null
            && requestedPlan.TargetParty.MapFaction != leaderParty?.MapFaction)
        {
            return requestedPlan;
        }

        return NormalizePlan(leaderParty, requestedPlan.TargetSettlement, requestedPlan.ArmyBehavior);
    }

    public static MobileParty GetRecommendedHostilePartyTarget(MobileParty leaderParty)
    {
        return GetHostilePartyTargets(leaderParty, 1).FirstOrDefault();
    }

    public static IReadOnlyList<MobileParty> GetHostilePartyTargets(MobileParty leaderParty, int maxCount = 5)
    {
        if (leaderParty?.MapFaction is not Kingdom kingdom)
        {
            return Array.Empty<MobileParty>();
        }

        List<Kingdom> enemyKingdoms = kingdom.FactionsAtWarWith?.OfType<Kingdom>().Where(candidate => candidate != null).ToList() ?? new List<Kingdom>();
        Kingdom enemy = RFArmyCommandHelpers.GetStrategicPrimaryEnemy(kingdom)
            ?? enemyKingdoms.FirstOrDefault();
        if (enemy == null)
        {
            return Array.Empty<MobileParty>();
        }

        Settlement objective = RFArmyCommandHelpers.GetStrategicObjectiveSettlement(kingdom, enemy);
        Settlement frontline = RFArmyCommandHelpers.GetStrategicFrontlineSettlement(kingdom, enemy);
        Settlement theater = RFArmyCommandHelpers.GetStrategicTheaterSettlement(kingdom, enemy);
        Settlement threatenedHomeland = FindThreatenedHomelandSettlement(kingdom, enemy);
        Settlement anchor = threatenedHomeland ?? frontline ?? theater ?? objective;
        Vec2 focusPoint = anchor?.GetPosition2D
            ?? leaderParty.GetPosition2D;
        List<MobileParty> mobileParties = MobileParty.All?.Where(party => party != null).ToList() ?? new List<MobileParty>();

        return mobileParties
            .Where(party =>
                party?.LeaderHero != null
                && party.MapFaction == enemy
                && !party.IsCaravan
                && !party.IsVillager
                && !party.IsMilitia
                && !party.IsBandit
                && party.Party != null
                && party.IsActive)
            .OrderByDescending(party => ScoreEnemyFieldThreat(kingdom, party, focusPoint))
            .Take(Math.Max(1, maxCount))
            .ToList();
    }

    public static RFArmyCommandPlan CreateManualInterceptPlan(MobileParty leaderParty, MobileParty hostileParty)
    {
        if (leaderParty == null || hostileParty == null || !hostileParty.IsActive)
        {
            return GetRecommendedPlan(leaderParty);
        }

        RFArmyCommandPlan recommended = GetRecommendedPlan(leaderParty);
        Army.ArmyTypes behavior = recommended?.TargetParty != null && RFArmyCommandHelpers.IsSameParty(recommended.TargetParty, hostileParty)
            ? recommended.ArmyBehavior
            : Army.ArmyTypes.Defender;

        return BuildPlan(
            null,
            hostileParty,
            behavior,
            "Manual Intercept",
            $"Manual Intercept -> {hostileParty.Name}",
            recommended?.ReasonText ?? "A hostile field force was selected as the army target.");
    }

    public static Army CreateOrUpdateArmy(MobileParty leaderParty, IEnumerable<MobileParty> partiesToJoin, RFArmyCommandPlan plan)
    {
        if (leaderParty?.LeaderHero == null)
        {
            return leaderParty?.Army;
        }

        Kingdom leaderKingdom = leaderParty.MapFaction as Kingdom;
        if (leaderKingdom == null || leaderKingdom != Clan.PlayerClan?.Kingdom)
        {
            return leaderParty.Army;
        }

        List<MobileParty> joinParties = partiesToJoin?
            .Where(party => party != null && !RFArmyCommandHelpers.IsSameParty(party, leaderParty))
            .Distinct(RFArmyCommandHelpers.PartyIdentityComparer)
            .ToList() ?? new List<MobileParty>();
        plan = NormalizePlan(leaderParty, plan);
        if (leaderParty.Army == null)
        {
            Settlement target = plan.TargetSettlement ?? RFArmyCommandHelpers.GetDefaultTargetSettlement(leaderParty);
            if (target == null)
            {
                RFLogger.Log($"[RFArmyCommand] CreateOrUpdateArmy aborted because no valid target settlement could be resolved. leader={leaderParty.StringId}");
                return null;
            }

            try
            {
                leaderKingdom.CreateArmy(
                    leaderParty.LeaderHero,
                    target,
                    plan.ArmyBehavior,
                    new MBReadOnlyList<MobileParty>(joinParties));
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[RFArmyCommand] CreateOrUpdateArmy failed while creating army. leader={leaderParty.StringId} target={target.StringId} behavior={plan.ArmyBehavior} error={ex}");
                return leaderParty.Army;
            }
        }

        Army army = leaderParty.Army;
        if (army == null)
        {
            return null;
        }

        foreach (MobileParty party in joinParties)
        {
            if (!RFArmyCommandHelpers.IsSameArmy(party.Army, army))
            {
                party.Army = army;
            }
        }

        if (leaderParty.IsMainParty)
        {
            RFLogger.Log($"[RFArmyCommand] CreateOrUpdateArmy player-led army membership updated without auto-ordering. leader={leaderParty.StringId} joiners={joinParties.Count}");
            return army;
        }

        RFLogger.Log($"[RFArmyCommand] CreateOrUpdateArmy leader={leaderParty.StringId} joiners={joinParties.Count} preset={plan.PlanLabel} behavior={plan.ArmyBehavior} target={plan.TargetSettlement?.StringId ?? "none"} targetParty={plan.TargetParty?.StringId ?? "none"}");
        ApplyPlan(army, plan, registerPlayerOverride: RFArmyCommandHelpers.IsArmyInPlayerKingdom(army));
        return army;
    }

    public static void ApplyPlan(Army army, RFArmyCommandPlan plan, bool emitLog = true, bool registerPlayerOverride = false)
    {
        if (army == null || plan == null)
        {
            return;
        }

        if (registerPlayerOverride)
        {
            RegisterManualOverride(army, plan);
        }

        army.ArmyType = plan.ArmyBehavior;
        if (!RFArmyCommandHelpers.IsArmyAvailableForOrders(army))
        {
            if (emitLog)
            {
                RFLogger.Log($"[RFArmyCommand] ApplyPlan skipped because army is unavailable. leader={army.LeaderParty?.StringId ?? "none"} preset={plan.PlanLabel} behavior={plan.ArmyBehavior} target={plan.TargetSettlement?.StringId ?? "none"}");
            }
            return;
        }

        MobileParty leaderParty = army.LeaderParty;
        if (plan.TargetParty != null && plan.TargetParty.IsActive && leaderParty != null && plan.TargetParty.MapFaction != leaderParty.MapFaction)
        {
            if (emitLog)
            {
                RFLogger.Log($"[RFArmyCommand] ApplyPlan intercept leader={leaderParty.StringId ?? "none"} preset={plan.PlanLabel} behavior={plan.ArmyBehavior} targetParty={plan.TargetParty.StringId}");
            }

            leaderParty.SetMoveEngageParty(plan.TargetParty, MobileParty.NavigationType.Default);
            return;
        }

        if (plan.TargetSettlement != null)
        {
            if (emitLog)
            {
                RFLogger.Log($"[RFArmyCommand] ApplyPlan gather leader={army.LeaderParty?.StringId ?? "none"} preset={plan.PlanLabel} behavior={plan.ArmyBehavior} target={plan.TargetSettlement.StringId}");
            }
            army.Gather(plan.TargetSettlement, null);
            return;
        }

        if (emitLog)
        {
            RFLogger.Log($"[RFArmyCommand] ApplyPlan finish-objective leader={army.LeaderParty?.StringId ?? "none"} preset={plan.PlanLabel} behavior={plan.ArmyBehavior}");
        }
        army.FinishArmyObjective();
    }

    public static void ApplyPlan(MobileParty leaderParty, RFArmyCommandPlan plan, bool emitLog = true, bool registerPlayerOverride = false)
    {
        if (leaderParty == null || plan == null)
        {
            return;
        }

        if (registerPlayerOverride)
        {
            RegisterManualOverride(leaderParty, plan);
        }

        if (RFArmyCommandHelpers.IsPartyBusy(leaderParty))
        {
            if (emitLog)
            {
                RFLogger.Log($"[RFArmyCommand] ApplyPlan skipped because party is unavailable. leader={leaderParty.StringId ?? "none"} preset={plan.PlanLabel} behavior={plan.ArmyBehavior} target={plan.TargetSettlement?.StringId ?? "none"}");
            }
            return;
        }

        if (plan.TargetParty != null && plan.TargetParty.IsActive && plan.TargetParty.MapFaction != null && plan.TargetParty.MapFaction != leaderParty.MapFaction)
        {
            if (emitLog)
            {
                RFLogger.Log($"[RFArmyCommand] ApplyPlan direct-intercept leader={leaderParty.StringId ?? "none"} preset={plan.PlanLabel} behavior={plan.ArmyBehavior} targetParty={plan.TargetParty.StringId}");
            }

            leaderParty.SetMoveEngageParty(plan.TargetParty, MobileParty.NavigationType.Default);
            return;
        }

        Settlement targetSettlement = plan.TargetSettlement ?? RFArmyCommandHelpers.GetDefaultTargetSettlement(leaderParty);
        if (targetSettlement != null)
        {
            if (emitLog)
            {
                RFLogger.Log($"[RFArmyCommand] ApplyPlan direct-move leader={leaderParty.StringId ?? "none"} preset={plan.PlanLabel} behavior={plan.ArmyBehavior} target={targetSettlement.StringId}");
            }

            if (plan.ArmyBehavior == Army.ArmyTypes.Patrolling)
            {
                leaderParty.SetMovePatrolAroundSettlement(targetSettlement, MobileParty.NavigationType.Default, false);
            }
            else
            {
                leaderParty.SetMoveGoToSettlement(targetSettlement, MobileParty.NavigationType.Default, targetSettlement.HasPort);
            }
        }
    }

    public static bool TryGetManualOverride(Army army, out RFArmyCommandPlan plan)
    {
        plan = null;
        string leaderId = army?.LeaderParty?.StringId;
        if (string.IsNullOrWhiteSpace(leaderId))
        {
            return false;
        }

        if (!ManualOverrides.TryGetValue(leaderId, out RFArmyCommandManualOverride manualOverride) || manualOverride?.Plan == null)
        {
            return false;
        }

        if (manualOverride.ExpiresAt.IsPast)
        {
            ManualOverrides.Remove(leaderId);
            RFLogger.Log($"[RFArmyCommand] Manual override expired. leader={leaderId}");
            return false;
        }

        if (!IsManualOverrideStillValid(army, manualOverride.Plan))
        {
            ManualOverrides.Remove(leaderId);
            RFLogger.Log($"[RFArmyCommand] Manual override cleared because target became invalid. leader={leaderId}");
            return false;
        }

        plan = manualOverride.Plan;
        return true;
    }

    public static bool TryGetManualOverride(MobileParty leaderParty, out RFArmyCommandPlan plan)
    {
        plan = null;
        string leaderId = leaderParty?.StringId;
        if (string.IsNullOrWhiteSpace(leaderId))
        {
            return false;
        }

        if (!ManualOverrides.TryGetValue(leaderId, out RFArmyCommandManualOverride manualOverride) || manualOverride?.Plan == null)
        {
            return false;
        }

        if (manualOverride.ExpiresAt.IsPast)
        {
            ManualOverrides.Remove(leaderId);
            RFLogger.Log($"[RFArmyCommand] Manual override expired. leader={leaderId}");
            return false;
        }

        if (!IsManualOverrideStillValid(leaderParty, manualOverride.Plan))
        {
            ManualOverrides.Remove(leaderId);
            RFLogger.Log($"[RFArmyCommand] Manual override cleared because target became invalid. leader={leaderId}");
            return false;
        }

        plan = manualOverride.Plan;
        return true;
    }

    public static List<string> ExportManualOverrides()
    {
        List<string> serialized = new();
        foreach (KeyValuePair<string, RFArmyCommandManualOverride> pair in ManualOverrides)
        {
            string leaderId = pair.Key;
            RFArmyCommandManualOverride manualOverride = pair.Value;
            if (string.IsNullOrWhiteSpace(leaderId) || manualOverride?.Plan == null)
            {
                continue;
            }

            float remainingHours = (float)(manualOverride.ExpiresAt - CampaignTime.Now).ToHours;
            if (remainingHours <= 0f)
            {
                continue;
            }

            serialized.Add(string.Join("\t", new[]
            {
                EncodeValue(leaderId),
                remainingHours.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ((int)manualOverride.Plan.ArmyBehavior).ToString(),
                EncodeValue(manualOverride.Plan.PlanLabel),
                EncodeValue(manualOverride.Plan.OrderText),
                EncodeValue(manualOverride.Plan.ReasonText),
                EncodeValue(manualOverride.Plan.TargetSettlement?.StringId),
                EncodeValue(manualOverride.Plan.TargetParty?.StringId)
            }));
        }

        return serialized;
    }

    public static void ImportManualOverrides(IEnumerable<string> serializedOverrides)
    {
        ManualOverrides.Clear();
        if (serializedOverrides == null)
        {
            return;
        }

        foreach (string serialized in serializedOverrides)
        {
            if (TryDeserializeManualOverride(serialized, out string leaderId, out RFArmyCommandManualOverride manualOverride))
            {
                ManualOverrides[leaderId] = manualOverride;
            }
        }
    }

    public static void ClearManualOverride(Army army)
    {
        string leaderId = army?.LeaderParty?.StringId;
        if (string.IsNullOrWhiteSpace(leaderId))
        {
            return;
        }

        ManualOverrides.Remove(leaderId);
    }

    public static void ClearManualOverride(MobileParty leaderParty)
    {
        string leaderId = leaderParty?.StringId;
        if (string.IsNullOrWhiteSpace(leaderId))
        {
            return;
        }

        ManualOverrides.Remove(leaderId);
    }

    public static void RemovePartiesFromArmy(Army army, IEnumerable<MobileParty> partiesToRemove)
    {
        if (army == null || partiesToRemove == null)
        {
            return;
        }

        List<MobileParty> attachedParties = army.Parties?.Where(attachedParty => attachedParty != null).ToList() ?? new List<MobileParty>();
        foreach (MobileParty party in partiesToRemove)
        {
            if (party != null && attachedParties.Any(attachedParty => RFArmyCommandHelpers.IsSameParty(attachedParty, party)))
            {
                party.Army = null;
            }
        }

        RFLogger.Log($"[RFArmyCommand] RemovePartiesFromArmy leader={army.LeaderParty?.StringId ?? "none"}");
    }

    public static void DisbandArmy(MobileParty leaderParty)
    {
        if (leaderParty == null)
        {
            return;
        }

        Army army = leaderParty.Army;
        if (army == null)
        {
            return;
        }

        ClearManualOverride(army);
        List<MobileParty> armyParties = army.Parties?.Where(party => party != null).ToList() ?? new List<MobileParty>();
        foreach (MobileParty party in armyParties)
        {
            party.Army = null;
        }

        RFLogger.Log(
            leaderParty.IsMainParty
                ? $"[RFArmyCommand] DisbandArmy released player-led army directly. parties={armyParties.Count}"
                : $"[RFArmyCommand] DisbandArmy released selected army directly. leader={leaderParty.StringId} parties={armyParties.Count}");
    }

    private static void RegisterManualOverride(Army army, RFArmyCommandPlan plan)
    {
        string leaderId = army?.LeaderParty?.StringId;
        if (string.IsNullOrWhiteSpace(leaderId) || plan == null)
        {
            return;
        }

        RegisterManualOverride(leaderId, plan);
    }

    private static void RegisterManualOverride(MobileParty leaderParty, RFArmyCommandPlan plan)
    {
        string leaderId = leaderParty?.StringId;
        if (string.IsNullOrWhiteSpace(leaderId) || plan == null)
        {
            return;
        }

        RegisterManualOverride(leaderId, plan);
    }

    private static void RegisterManualOverride(string leaderId, RFArmyCommandPlan plan)
    {
        ManualOverrides[leaderId] = new RFArmyCommandManualOverride
        {
            Plan = plan,
            ExpiresAt = CampaignTime.HoursFromNow(ManualOverrideDurationHours)
        };

        RFLogger.Log(
            $"[RFArmyCommand] Manual override registered. leader={leaderId} preset={plan.PlanLabel} behavior={plan.ArmyBehavior} target={plan.TargetSettlement?.StringId ?? "none"} targetParty={plan.TargetParty?.StringId ?? "none"} expiresInHours={ManualOverrideDurationHours}");
    }

    private static bool IsManualOverrideStillValid(Army army, RFArmyCommandPlan plan)
    {
        MobileParty leaderParty = army?.LeaderParty;
        return IsManualOverrideStillValid(leaderParty, plan);
    }

    private static bool IsManualOverrideStillValid(MobileParty leaderParty, RFArmyCommandPlan plan)
    {
        if (leaderParty?.MapFaction == null || plan == null)
        {
            return false;
        }

        if (plan.TargetParty != null)
        {
            return plan.TargetParty.IsActive
                && plan.TargetParty.MapFaction != null
                && plan.TargetParty.MapFaction != leaderParty.MapFaction;
        }

        if (plan.TargetSettlement == null)
        {
            return true;
        }

        return plan.ArmyBehavior switch
        {
            Army.ArmyTypes.Raider or Army.ArmyTypes.Besieger => plan.TargetSettlement.MapFaction != null && plan.TargetSettlement.MapFaction != leaderParty.MapFaction,
            Army.ArmyTypes.Defender => plan.TargetSettlement.MapFaction == leaderParty.MapFaction,
            _ => true
        };
    }

    private static bool TryDeserializeManualOverride(string serialized, out string leaderId, out RFArmyCommandManualOverride manualOverride)
    {
        leaderId = null;
        manualOverride = null;
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return false;
        }

        string[] parts = serialized.Split('\t');
        if (parts.Length != 8)
        {
            return false;
        }

        leaderId = DecodeValue(parts[0]);
        if (string.IsNullOrWhiteSpace(leaderId)
            || !float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float remainingHours)
            || !int.TryParse(parts[2], out int armyBehaviorValue)
            || !Enum.IsDefined(typeof(Army.ArmyTypes), armyBehaviorValue))
        {
            return false;
        }

        string resolvedLeaderId = leaderId;
        List<MobileParty> mobileParties = MobileParty.All?.Where(party => party != null).ToList() ?? new List<MobileParty>();
        List<Settlement> settlements = Settlement.All?.Where(settlement => settlement != null).ToList() ?? new List<Settlement>();
        MobileParty leaderParty = mobileParties.FirstOrDefault(party => party?.StringId == resolvedLeaderId);
        if (leaderParty == null)
        {
            return false;
        }

        string targetSettlementId = DecodeValue(parts[6]);
        string targetPartyId = DecodeValue(parts[7]);
        Settlement targetSettlement = string.IsNullOrWhiteSpace(targetSettlementId)
            ? null
            : settlements.FirstOrDefault(settlement => settlement?.StringId == targetSettlementId);
        MobileParty targetParty = string.IsNullOrWhiteSpace(targetPartyId)
            ? null
            : mobileParties.FirstOrDefault(party => party?.StringId == targetPartyId);
        if ((!string.IsNullOrWhiteSpace(targetSettlementId) && targetSettlement == null)
            || (!string.IsNullOrWhiteSpace(targetPartyId) && targetParty == null))
        {
            return false;
        }

        RFArmyCommandPlan plan = new()
        {
            ArmyBehavior = (Army.ArmyTypes)armyBehaviorValue,
            PlanLabel = DecodeValue(parts[3]) ?? string.Empty,
            OrderText = DecodeValue(parts[4]) ?? string.Empty,
            ReasonText = DecodeValue(parts[5]) ?? string.Empty,
            TargetSettlement = targetSettlement,
            TargetParty = targetParty
        };

        manualOverride = new RFArmyCommandManualOverride
        {
            Plan = plan,
            ExpiresAt = CampaignTime.HoursFromNow(Math.Max(remainingHours, 0.1f))
        };
        return true;
    }

    private static string EncodeValue(string value)
    {
        return Uri.EscapeDataString(value ?? string.Empty);
    }

    private static string DecodeValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : Uri.UnescapeDataString(value);
    }

    private static RFArmyCommandPlan BuildPlan(Settlement target, MobileParty targetParty, Army.ArmyTypes behavior, string planLabel, string orderText, string reasonText)
    {
        return new RFArmyCommandPlan
        {
            TargetSettlement = target,
            TargetParty = targetParty,
            ArmyBehavior = behavior,
            PlanLabel = planLabel,
            OrderText = orderText,
            ReasonText = reasonText
        };
    }

    private static void AddPlanUnique(List<RFArmyCommandPlan> plans, RFArmyCommandPlan plan)
    {
        if (plan == null)
        {
            return;
        }

        bool exists = plans.Any(existing =>
            existing.ArmyBehavior == plan.ArmyBehavior
            && string.Equals(existing.TargetSettlement?.StringId, plan.TargetSettlement?.StringId, StringComparison.Ordinal)
            && string.Equals(existing.TargetParty?.StringId, plan.TargetParty?.StringId, StringComparison.Ordinal));
        if (!exists)
        {
            plans.Add(plan);
        }
    }

    private static Army.ArmyTypes SelectRecommendedBehavior(Kingdom kingdom, Kingdom enemy, bool homelandDefense, string phase, string operationalState, Settlement target, bool regroupRecommended, float decisivePressure, MobileParty hostileFieldThreat, RFArmyCommanderProfile commanderProfile)
    {
        if (commanderProfile == RFArmyCommanderProfile.Cautious && target?.MapFaction == enemy && target.IsVillage)
        {
            return Army.ArmyTypes.Patrolling;
        }

        if (hostileFieldThreat != null && target?.MapFaction == kingdom)
        {
            return Army.ArmyTypes.Defender;
        }

        if (regroupRecommended || homelandDefense || operationalState == "Defend")
        {
            return Army.ArmyTypes.Defender;
        }

        if (target == null)
        {
            return Army.ArmyTypes.Patrolling;
        }

        if (operationalState == "Muster")
        {
            return target.MapFaction == kingdom ? Army.ArmyTypes.Defender : Army.ArmyTypes.Patrolling;
        }

        if (target.MapFaction == enemy)
        {
            if (commanderProfile == RFArmyCommanderProfile.Raider && target.IsVillage)
            {
                return Army.ArmyTypes.Raider;
            }

            if (operationalState == "Exploit" && target.IsVillage)
            {
                return Army.ArmyTypes.Raider;
            }

            if (commanderProfile == RFArmyCommanderProfile.Methodical && !target.IsVillage && decisivePressure < 0.7f)
            {
                return Army.ArmyTypes.Defender;
            }

            if (operationalState == "Besiege" || decisivePressure >= 0.55f || !target.IsVillage)
            {
                return target.IsVillage ? Army.ArmyTypes.Raider : Army.ArmyTypes.Besieger;
            }

            if (target.IsVillage)
            {
                return Army.ArmyTypes.Raider;
            }

            return Army.ArmyTypes.Besieger;
        }

        return phase == "Stabilize"
            ? Army.ArmyTypes.Defender
            : RFArmyCommandHelpers.GetDefaultArmyBehavior(target);
    }

    private static Settlement SelectRecommendedTarget(Kingdom kingdom, Kingdom enemy, bool homelandDefense, string phase, Settlement objective, Settlement frontline, Settlement theater, Settlement threatenedHomeland, MobileParty hostileFieldThreat, MobileParty leaderParty, RFArmyCommanderProfile commanderProfile)
    {
        if (threatenedHomeland != null)
        {
            return threatenedHomeland;
        }

        if (homelandDefense)
        {
            List<Settlement> kingdomSettlements = kingdom.Fiefs?.Select(fief => fief?.Settlement).Where(settlement => settlement != null).ToList() ?? new List<Settlement>();
            return FirstMatchingSettlement(new[] { objective, frontline, theater }, settlement => settlement?.MapFaction == kingdom)
                ?? kingdomSettlements.FirstOrDefault(settlement => settlement.IsUnderSiege)
                ?? kingdomSettlements.FirstOrDefault();
        }

        if (hostileFieldThreat != null)
        {
            Settlement threatAnchor = FindFriendlyThreatAnchor(kingdom, hostileFieldThreat, frontline ?? theater ?? objective);
            if (threatAnchor != null)
            {
                return threatAnchor;
            }
        }

        if (phase == "StripSupport")
        {
            Settlement village = FindBoundVillageTarget(enemy, objective)
                ?? FindBoundVillageTarget(enemy, frontline)
                ?? FindBoundVillageTarget(enemy, theater);
            if (village != null)
            {
                return village;
            }
        }

        if (commanderProfile == RFArmyCommanderProfile.Raider)
        {
            Settlement villageTarget = FindBoundVillageTarget(enemy, objective)
                ?? FindBoundVillageTarget(enemy, frontline)
                ?? FindBoundVillageTarget(enemy, theater);
            if (villageTarget != null)
            {
                return villageTarget;
            }
        }

        if (commanderProfile == RFArmyCommanderProfile.Cautious)
        {
            Settlement safeFront = FirstMatchingSettlement(new[] { frontline, objective, theater }, settlement => settlement?.MapFaction == kingdom && settlement.IsFortification);
            if (safeFront != null)
            {
                return safeFront;
            }
        }

        return BuildOffensiveTargetCandidates(enemy, objective, frontline, theater)
            .Distinct()
            .OrderByDescending(settlement => ScoreSettlementTarget(kingdom, enemy, settlement, objective, frontline, theater, leaderParty, commanderProfile, phase))
            .ThenBy(settlement => settlement.GetPosition2D.DistanceSquared((leaderParty?.GetPosition2D) ?? (MobileParty.MainParty?.GetPosition2D) ?? Vec2.Zero))
            .FirstOrDefault();
    }

    private static RFArmyCommandPreset SelectRecommendedPreset(Kingdom kingdom, bool homelandDefense, bool regroupRecommended, Settlement target, Settlement threatenedHomeland, MobileParty hostileFieldThreat, Army.ArmyTypes behavior)
    {
        if (regroupRecommended)
        {
            return RFArmyCommandPreset.GatherForCampaign;
        }

        if (threatenedHomeland != null)
        {
            return RFArmyCommandPreset.RelieveSiege;
        }

        if (hostileFieldThreat != null && target?.MapFaction == kingdom)
        {
            return RFArmyCommandPreset.InterceptHostileArmy;
        }

        if (homelandDefense || target?.MapFaction == kingdom)
        {
            return RFArmyCommandPreset.FrontierDefense;
        }

        return behavior switch
        {
            Army.ArmyTypes.Raider => RFArmyCommandPreset.RaidEnemySupport,
            Army.ArmyTypes.Besieger => RFArmyCommandPreset.BesiegeStronghold,
            _ => RFArmyCommandPreset.HoldReserve
        };
    }

    private static Settlement FirstMatchingSettlement(IEnumerable<Settlement> settlements, System.Func<Settlement, bool> predicate)
    {
        return settlements?.FirstOrDefault(settlement => settlement != null && predicate(settlement));
    }

    private static IEnumerable<Settlement> BuildOffensiveTargetCandidates(Kingdom enemy, Settlement objective, Settlement frontline, Settlement theater)
    {
        if (objective != null)
        {
            yield return objective;
        }

        if (frontline != null)
        {
            yield return frontline;
        }

        if (theater != null)
        {
            yield return theater;
        }

        Settlement frontlineVillage = FindBoundVillageTarget(enemy, frontline);
        if (frontlineVillage != null)
        {
            yield return frontlineVillage;
        }

        Settlement theaterVillage = FindBoundVillageTarget(enemy, theater);
        if (theaterVillage != null)
        {
            yield return theaterVillage;
        }

        List<Settlement> enemySettlements = enemy?.Fiefs?.Select(fief => fief?.Settlement).Where(settlement => settlement != null).ToList() ?? new List<Settlement>();
        foreach (Settlement settlement in enemySettlements)
        {
            yield return settlement;
        }
    }

    private static float ScoreSettlementTarget(Kingdom kingdom, Kingdom enemy, Settlement candidate, Settlement objective, Settlement frontline, Settlement theater, MobileParty leaderParty, RFArmyCommanderProfile commanderProfile, string phase)
    {
        if (candidate == null)
        {
            return float.MinValue;
        }

        float score = 0f;
        if (candidate == objective)
        {
            score += 120f;
        }

        if (candidate == frontline)
        {
            score += 90f;
        }

        if (candidate == theater)
        {
            score += 70f;
        }

        if (candidate.MapFaction == enemy)
        {
            score += candidate.IsVillage ? 30f : 55f;
        }

        if (phase == "StripSupport" && candidate.IsVillage)
        {
            score += 35f;
        }

        score += commanderProfile switch
        {
            RFArmyCommanderProfile.Raider when candidate.IsVillage => 35f,
            RFArmyCommanderProfile.Methodical when candidate.IsFortification => 25f,
            RFArmyCommanderProfile.Cautious when candidate.MapFaction == kingdom && candidate.IsFortification => 30f,
            RFArmyCommanderProfile.Cautious when candidate.MapFaction == enemy && candidate.IsVillage => -40f,
            RFArmyCommanderProfile.Aggressive when candidate.MapFaction == enemy => 20f,
            _ => 0f
        };

        Vec2 origin = leaderParty?.GetPosition2D
            ?? MobileParty.MainParty?.GetPosition2D
            ?? Vec2.Zero;
        float distance = candidate.GetPosition2D.Distance(origin);
        score -= distance * 0.35f;

        return score;
    }

    private static Settlement FindBoundVillageTarget(Kingdom enemy, Settlement anchor)
    {
        if (anchor == null)
        {
            return null;
        }

        if (anchor.IsVillage && anchor.MapFaction == enemy)
        {
            return anchor;
        }

        List<Settlement> settlements = Settlement.All?.Where(settlement => settlement != null).ToList() ?? new List<Settlement>();
        return settlements.FirstOrDefault(settlement =>
            settlement.IsVillage
            && settlement.MapFaction == enemy
            && settlement.Village?.Bound == anchor);
    }

    private static Settlement FindThreatenedHomelandSettlement(Kingdom kingdom, Kingdom enemy)
    {
        List<Settlement> kingdomSettlements = kingdom?.Fiefs?.Select(fief => fief?.Settlement).Where(settlement => settlement != null).ToList() ?? new List<Settlement>();
        return kingdomSettlements.FirstOrDefault(settlement =>
            settlement?.IsUnderSiege == true
            && settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction == enemy);
    }

    private static MobileParty FindPriorityEnemyParty(Kingdom kingdom, Kingdom enemy, MobileParty leaderParty, Settlement anchor)
    {
        return GetHostilePartyTargets(leaderParty, 1).FirstOrDefault();
    }

    private static float ScoreEnemyFieldThreat(Kingdom kingdom, MobileParty enemyParty, Vec2 focusPoint)
    {
        if (enemyParty == null)
        {
            return float.MinValue;
        }

        float score = 0f;
        if (enemyParty.Army != null)
        {
            score += 120f;
        }

        if (enemyParty.BesiegedSettlement?.MapFaction == kingdom)
        {
            score += 220f;
        }

        if (enemyParty.TargetSettlement?.MapFaction == kingdom)
        {
            score += enemyParty.TargetSettlement.IsVillage ? 110f : 150f;
        }

        if (enemyParty.CurrentSettlement?.MapFaction == kingdom)
        {
            score += enemyParty.CurrentSettlement.IsVillage ? 90f : 130f;
        }

        List<Settlement> kingdomSettlements = kingdom?.Fiefs?.Select(fief => fief?.Settlement).Where(settlement => settlement != null).ToList() ?? new List<Settlement>();
        Settlement nearestFriendly = kingdomSettlements
            .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(enemyParty.GetPosition2D))
            .FirstOrDefault();
        if (nearestFriendly != null)
        {
            float nearbyDistance = nearestFriendly.GetPosition2D.Distance(enemyParty.GetPosition2D);
            score += MathF.Max(0f, 80f - nearbyDistance * 0.35f);
        }

        score += enemyParty.Party.NumberOfAllMembers * 0.2f;
        score -= enemyParty.GetPosition2D.Distance(focusPoint) * 0.3f;
        return score;
    }

    private static Settlement FindFriendlyThreatAnchor(Kingdom kingdom, MobileParty hostileParty, Settlement fallback)
    {
        if (hostileParty == null)
        {
            return fallback;
        }

        List<Settlement> kingdomSettlements = kingdom?.Fiefs?.Select(fief => fief?.Settlement).Where(settlement => settlement != null).ToList() ?? new List<Settlement>();
        Settlement closestFriendly = kingdomSettlements
            .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(hostileParty.GetPosition2D))
            .FirstOrDefault();

        return closestFriendly ?? fallback;
    }

    private static Settlement FindFriendlyRallySettlement(MobileParty leaderParty, Kingdom kingdom, Settlement preferredAnchor)
    {
        if (RFArmyCommandHelpers.IsFriendlySettlement(leaderParty?.CurrentSettlement))
        {
            return leaderParty.CurrentSettlement;
        }

        if (RFArmyCommandHelpers.IsFriendlySettlement(leaderParty?.TargetSettlement))
        {
            return leaderParty.TargetSettlement;
        }

        if (RFArmyCommandHelpers.IsFriendlySettlement(preferredAnchor))
        {
            return preferredAnchor;
        }

        if (RFArmyCommandHelpers.IsFriendlySettlement(leaderParty?.LeaderHero?.HomeSettlement))
        {
            return leaderParty.LeaderHero.HomeSettlement;
        }

        List<Settlement> kingdomSettlements = kingdom?.Fiefs?.Select(fief => fief?.Settlement).Where(settlement => settlement != null).ToList() ?? new List<Settlement>();
        return kingdomSettlements.FirstOrDefault(settlement => settlement.IsFortification && !settlement.IsUnderSiege)
            ?? kingdomSettlements.FirstOrDefault();
    }

    private static bool ShouldRegroup(MobileParty leaderParty, string operationalState)
    {
        if (operationalState == "Regroup" || operationalState == "Muster")
        {
            return true;
        }

        Army army = leaderParty?.Army;
        if (army?.LeaderParty == null)
        {
            return false;
        }

        if (army.Cohesion < 35f)
        {
            return true;
        }

        List<MobileParty> allParties = RFArmyCommandHelpers.GetAllPartiesFromArmy(army).ToList();
        List<MobileParty> joiningToday = RFArmyCommandHelpers.GetPartiesJoiningToday(army, allParties).ToList();
        return RFArmyCommandHelpers.GetCurrentArmyFood(army) < 25f
            || RFArmyCommandHelpers.GetTotalArmyFoodChange(army, joiningToday) < -5f;
    }

    private static RFArmyCommanderProfile GetCommanderProfile(MobileParty leaderParty, bool homelandEmergency)
    {
        Hero leader = leaderParty?.LeaderHero;
        if (leader == null)
        {
            return RFArmyCommanderProfile.Balanced;
        }

        int tactics = leader.GetSkillValue(DefaultSkills.Tactics);
        int scouting = leader.GetSkillValue(DefaultSkills.Scouting);
        int riding = leader.GetSkillValue(DefaultSkills.Riding);
        int valor = leader.GetTraitLevel(DefaultTraits.Valor);
        int calculating = leader.GetTraitLevel(DefaultTraits.Calculating);
        int mercy = leader.GetTraitLevel(DefaultTraits.Mercy);

        if (!homelandEmergency && scouting >= 120 && riding >= 100 && (valor > 0 || tactics >= 140))
        {
            return RFArmyCommanderProfile.Raider;
        }

        if (homelandEmergency && (mercy > 0 || calculating > 0))
        {
            return RFArmyCommanderProfile.Cautious;
        }

        if (calculating > valor || (calculating > 0 && tactics >= 120))
        {
            return RFArmyCommanderProfile.Methodical;
        }

        if (valor > calculating || tactics >= 160)
        {
            return RFArmyCommanderProfile.Aggressive;
        }

        if (mercy > 0)
        {
            return RFArmyCommanderProfile.Cautious;
        }

        return RFArmyCommanderProfile.Balanced;
    }

    private static string GetPresetLabel(RFArmyCommandPreset preset)
    {
        return preset switch
        {
            RFArmyCommandPreset.RelieveSiege => "Relieve Siege",
            RFArmyCommandPreset.FrontierDefense => "Frontier Defense",
            RFArmyCommandPreset.GatherForCampaign => "Gather for Campaign",
            RFArmyCommandPreset.RaidEnemySupport => "Raid Enemy Support",
            RFArmyCommandPreset.BesiegeStronghold => "Besiege Stronghold",
            RFArmyCommandPreset.InterceptHostileArmy => "Intercept Hostile Army",
            _ => "Hold Reserve"
        };
    }

    private static string GetCommanderProfileLabel(RFArmyCommanderProfile commanderProfile)
    {
        return commanderProfile switch
        {
            RFArmyCommanderProfile.Aggressive => "Aggressive commander",
            RFArmyCommanderProfile.Methodical => "Methodical commander",
            RFArmyCommanderProfile.Cautious => "Cautious commander",
            RFArmyCommanderProfile.Raider => "Raiding-minded commander",
            _ => "Balanced commander"
        };
    }

    private static string BuildReasonText(
        string planLabel,
        string focusMode,
        string phase,
        string operationalState,
        Settlement objective,
        Settlement frontline,
        Settlement theater,
        bool homelandDefense,
        float homePressure,
        float coalitionRoleFactor,
        float coalitionPullFactor,
        float decisivePressure,
        float unresolvedFrontPressure,
        MobileParty leaderParty,
        bool regroupRecommended,
        MobileParty hostileFieldThreat,
        RFArmyCommanderProfile commanderProfile)
    {
        List<string> parts = new();

        AddReasonLabel(parts, planLabel);
        AddReasonLabel(parts, GetCommanderProfileLabel(commanderProfile));

        if (homelandDefense)
        {
            AddReasonLabel(parts, "Homeland defense priority");
        }
        else if (!string.IsNullOrWhiteSpace(focusMode) && !string.Equals(focusMode, "None"))
        {
            AddReasonLabel(parts, $"Focus: {focusMode}");
        }

        if (regroupRecommended)
        {
            AddReasonLabel(parts, "Safe rally point recommended");
        }
        else if (objective != null)
        {
            AddReasonLabel(parts, $"Objective: {objective.Name}");
        }
        else if (frontline != null)
        {
            AddReasonLabel(parts, $"Front: {frontline.Name}");
        }
        else if (theater != null)
        {
            AddReasonLabel(parts, $"Theater: {theater.Name}");
        }

        if (hostileFieldThreat != null)
        {
            AddReasonLabel(parts, $"Threat nearby: {hostileFieldThreat.Name}");
        }

        if (!string.IsNullOrWhiteSpace(phase) && phase != "None")
        {
            AddReasonLabel(parts, $"Phase: {phase}");
        }

        if (!string.IsNullOrWhiteSpace(operationalState))
        {
            AddReasonLabel(parts, $"Posture: {operationalState}");
        }

        if (homePressure >= 1.2f)
        {
            AddReasonLabel(parts, "Front under pressure");
        }

        if (unresolvedFrontPressure >= 0.55f)
        {
            AddReasonLabel(parts, "Enemy line still unstable");
        }

        if (decisivePressure >= 0.55f)
        {
            AddReasonLabel(parts, "Good window for a decisive push");
        }

        if (coalitionPullFactor >= 0.45f)
        {
            AddReasonLabel(parts, "Coalition pressure is shaping this front");
        }

        if (coalitionRoleFactor >= 0.45f)
        {
            AddReasonLabel(parts, "Kingdom role favors active campaigning");
        }

        AppendArmyConditionLabels(parts, leaderParty?.Army);

        return string.Join(" | ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Take(5));
    }

    private static void AppendArmyConditionLabels(List<string> parts, Army army)
    {
        if (army?.LeaderParty == null)
        {
            return;
        }

        if (army.Cohesion < 35f)
        {
            AddReasonLabel(parts, "Cohesion too low for a deep push");
        }

        List<MobileParty> allParties = RFArmyCommandHelpers.GetAllPartiesFromArmy(army).ToList();
        List<MobileParty> joiningToday = RFArmyCommandHelpers.GetPartiesJoiningToday(army, allParties).ToList();
        float currentFood = RFArmyCommandHelpers.GetCurrentArmyFood(army);
        float dailyFood = RFArmyCommandHelpers.GetTotalArmyFoodChange(army, joiningToday);

        if (currentFood < 25f)
        {
            AddReasonLabel(parts, "Army food is running low");
        }

        if (dailyFood < -5f)
        {
            AddReasonLabel(parts, "Army food drain is too high");
        }
    }

    private static void AddReasonLabel(List<string> parts, string label)
    {
        if (string.IsNullOrWhiteSpace(label) || parts.Contains(label))
        {
            return;
        }

        parts.Add(label);
    }
}
