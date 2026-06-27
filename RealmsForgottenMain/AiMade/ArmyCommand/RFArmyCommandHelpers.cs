using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;

namespace RealmsForgotten.AiMade.ArmyCommand;

internal static class RFArmyCommandHelpers
{

    internal sealed class MobilePartyIdentityComparer : IEqualityComparer<MobileParty>
    {
        public bool Equals(MobileParty x, MobileParty y)
        {
            return IsSameParty(x, y);
        }

        public int GetHashCode(MobileParty obj)
        {
            if (obj == null)
            {
                return 0;
            }

            if (!string.IsNullOrEmpty(obj.StringId))
            {
                return StringComparer.Ordinal.GetHashCode(obj.StringId);
            }

            if (!string.IsNullOrEmpty(obj.LeaderHero?.StringId))
            {
                return StringComparer.Ordinal.GetHashCode(obj.LeaderHero.StringId);
            }

            return obj.GetHashCode();
        }
    }

    private static readonly MethodInfo GetPrimaryEnemyMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarCampaignDirectorBehavior"), "GetPrimaryEnemy");

    private static readonly MethodInfo GetObjectiveSettlementMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarObjectiveChainBehavior"), "GetObjectiveSettlement");

    private static readonly MethodInfo GetFrontlineAnchorMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarFrontlineBehavior"), "GetAnchorSettlement");

    private static readonly MethodInfo GetTheaterAnchorMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarTheaterBehavior"), "GetAnchorSettlement");

    private static readonly MethodInfo GetTheaterFocusModeMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarTheaterBehavior"), "GetFocusMode");

    private static readonly MethodInfo GetCampaignPhaseMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarCampaignPhaseBehavior"), "GetPhase");

    private static readonly MethodInfo GetOperationalStateMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarOperationalRhythmBehavior"), "GetState");

    private static readonly MethodInfo GetHomeFrontPressureMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarStrategicMemoryBehavior"), "GetHomeFrontPressure");

    private static readonly MethodInfo GetCoalitionRoleFactorMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarCoalitionRoleBehavior"), "GetCampaignRoleFactor");

    private static readonly MethodInfo GetCoalitionPullFactorMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarCampaignDirectorBehavior"), "GetCoalitionPullFactor");

    private static readonly MethodInfo GetDecisiveCampaignPressureMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarCampaignDirectorBehavior"), "GetDecisiveCampaignPressure");

    private static readonly MethodInfo GetUnresolvedFrontPressureMethod =
        AccessTools.Method(AccessTools.TypeByName("RF_warsystem.Behaviors.RFWarCampaignDirectorBehavior"), "GetUnresolvedFrontPressure");

    public static IEqualityComparer<MobileParty> PartyIdentityComparer { get; } = new MobilePartyIdentityComparer();

    public static bool IsArmyInPlayerKingdom(Army army)
    {
        return army?.Kingdom != null && army.Kingdom == Clan.PlayerClan?.Kingdom;
    }

    public static bool IsValidArmyForOverlay(Army army)
    {
        return IsArmyInPlayerKingdom(army)
            && army?.LeaderParty != null
            && army.Parties != null
            && army.Parties.Count > 0;
    }

    public static Army GetPlayerOverlayArmy()
    {
        Army mainPartyArmy = MobileParty.MainParty?.Army;
        return IsValidArmyForOverlay(mainPartyArmy) ? mainPartyArmy : null;
    }

    public static Army ResolveSelectedArmy(Army army = null)
    {
        Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
        if (playerKingdom == null)
        {
            return null;
        }

        Army resolvedArmy = playerKingdom.Armies?.FirstOrDefault(candidate => IsSameArmy(candidate, army));
        if (resolvedArmy != null)
        {
            return resolvedArmy;
        }

        Army mainPartyArmy = MobileParty.MainParty?.Army;
        if (IsArmyInPlayerKingdom(mainPartyArmy))
        {
            return mainPartyArmy;
        }

        return playerKingdom.Armies?.FirstOrDefault();
    }

    public static Army GetPreferredSelectedArmy(Army preferredArmy = null)
    {
        Army selectedArmy = ResolveSelectedArmy(preferredArmy);
        if (selectedArmy != null)
        {
            return selectedArmy;
        }

        Army mainPartyArmy = MobileParty.MainParty?.Army;
        if (IsArmyInPlayerKingdom(mainPartyArmy))
        {
            return mainPartyArmy;
        }

        return Clan.PlayerClan?.Kingdom?.Armies?.FirstOrDefault();
    }

    public static bool IsSameParty(MobileParty first, MobileParty second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (first == null || second == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(first.StringId) && string.Equals(first.StringId, second.StringId, StringComparison.Ordinal))
        {
            return true;
        }

        return first.LeaderHero != null
            && second.LeaderHero != null
            && string.Equals(first.LeaderHero.StringId, second.LeaderHero.StringId, StringComparison.Ordinal);
    }

    public static bool IsSameHero(Hero first, Hero second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (first == null || second == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(first.StringId) && string.Equals(first.StringId, second.StringId, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    public static bool IsSameArmy(Army first, Army second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (first == null || second == null)
        {
            return false;
        }

        return IsSameParty(first.LeaderParty, second.LeaderParty);
    }

    public static bool IsArmyAvailableForOrders(Army army)
    {
        if (army?.LeaderParty == null)
        {
            return false;
        }

        MobileParty leaderParty = army.LeaderParty;
        if (leaderParty.MapEvent != null || leaderParty.SiegeEvent != null)
        {
            return false;
        }

        if (leaderParty.CurrentSettlement != null && leaderParty.CurrentSettlement.IsUnderSiege)
        {
            return false;
        }

        return PlayerSiege.PlayerSiegeEvent == null;
    }

    public static bool IsPlayerBusy()
    {
        if (PlayerEncounter.Current != null && !IsSettlementOk(PlayerEncounter.EncounterSettlement))
        {
            return true;
        }

        if (MapEvent.PlayerMapEvent != null || CampaignMission.Current != null || PlayerSiege.PlayerSiegeEvent != null)
        {
            return true;
        }

        return IsPartyBusy(MobileParty.MainParty);
    }

    public static bool IsPartyBusy(MobileParty party)
    {
        if (party == null || party.LeaderHero == null)
        {
            return true;
        }

        if (party.LeaderHero.IsPrisoner || party.MapEvent != null || party.SiegeEvent != null || party.IsDisbanding)
        {
            return true;
        }

        if (party.CurrentSettlement != null && !IsSettlementOk(party.CurrentSettlement))
        {
            return true;
        }

        IDisbandPartyCampaignBehavior campaignBehavior = Campaign.Current?.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
        return campaignBehavior != null && campaignBehavior.IsPartyWaitingForDisband(party);
    }

    public static bool IsSettlementOk(Settlement settlement)
    {
        if (settlement == null)
        {
            return false;
        }

        if (settlement.IsVillage)
        {
            return settlement.Village.VillageState != Village.VillageStates.Looted;
        }

        return !settlement.IsUnderSiege;
    }

    public static bool ShouldShowArmyOverlayForPlayer()
    {
        return Campaign.Current != null
            && Hero.MainHero != null
            && Clan.PlayerClan?.Kingdom != null
            && !Clan.PlayerClan.IsUnderMercenaryService
            && MobileParty.MainParty?.IsActive == true
            && GetPlayerOverlayArmy() != null;
    }

    public static string GetArmyBehaviorName(Army.ArmyTypes armyBehavior)
    {
        return armyBehavior switch
        {
            Army.ArmyTypes.Defender => "Defend",
            Army.ArmyTypes.Besieger => "Besiege",
            Army.ArmyTypes.Raider => "Raid",
            Army.ArmyTypes.Patrolling => "Patrol",
            _ => armyBehavior.ToString()
        };
    }

    public static bool IsFriendlySettlement(Settlement settlement)
    {
        return settlement?.MapFaction == Clan.PlayerClan?.Kingdom;
    }

    public static bool IsEnemySettlement(Settlement settlement)
    {
        Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
        return settlement?.MapFaction != null
            && settlement.MapFaction != playerKingdom
            && playerKingdom?.FactionsAtWarWith.Contains(settlement.MapFaction) == true;
    }

    public static IEnumerable<Army.ArmyTypes> GetAvailableArmyBehaviors(Settlement targetSettlement)
    {
        if (targetSettlement == null)
        {
            yield return Army.ArmyTypes.Patrolling;
            yield break;
        }

        if (IsEnemySettlement(targetSettlement))
        {
            if (targetSettlement.IsVillage)
            {
                yield return Army.ArmyTypes.Raider;
            }
            else
            {
                yield return Army.ArmyTypes.Besieger;
            }

            yield return Army.ArmyTypes.Patrolling;
            yield break;
        }

        yield return Army.ArmyTypes.Defender;
        yield return Army.ArmyTypes.Patrolling;
    }

    public static Army.ArmyTypes GetDefaultArmyBehavior(Settlement targetSettlement)
    {
        if (targetSettlement == null)
        {
            return Army.ArmyTypes.Patrolling;
        }

        return IsEnemySettlement(targetSettlement)
            ? (targetSettlement.IsVillage ? Army.ArmyTypes.Raider : Army.ArmyTypes.Besieger)
            : Army.ArmyTypes.Defender;
    }

    public static Settlement GetDefaultTargetSettlement(MobileParty party)
    {
        if (party?.MapFaction is Kingdom kingdom)
        {
            Kingdom enemy = GetStrategicPrimaryEnemy(kingdom) ?? kingdom.FactionsAtWarWith.OfType<Kingdom>().FirstOrDefault();
            if (enemy != null)
            {
                bool homelandDefense = IsHomelandDefense(kingdom, enemy);
                Settlement strategicTarget = GetStrategicDefaultTarget(kingdom, enemy, homelandDefense);
                if (strategicTarget != null)
                {
                    return strategicTarget;
                }
            }
        }

        return party?.CurrentSettlement
            ?? party?.TargetSettlement
            ?? party?.LeaderHero?.HomeSettlement
            ?? Hero.MainHero.HomeSettlement;
    }

    public static Settlement ResolveArmyTargetSettlement(MobileParty leaderParty)
    {
        if (leaderParty == null)
        {
            return null;
        }

        if (leaderParty.IsMainParty)
        {
            return leaderParty.TargetSettlement
                ?? leaderParty.Army?.AiBehaviorObject as Settlement;
        }

        return leaderParty.Army?.AiBehaviorObject as Settlement
            ?? leaderParty.TargetSettlement;
    }

    public static string GetTargetSettlementName(Settlement settlement)
    {
        return settlement?.Name?.ToString() ?? "No target";
    }

    public static string GetArmyCommandSummaryText(Army army)
    {
        if (army == null)
        {
            return string.Empty;
        }

        MobileParty targetParty = army.LeaderParty?.TargetParty;
        Settlement targetSettlement = ResolveArmyTargetSettlement(army.LeaderParty);
        string behavior = GetArmyBehaviorName(army.ArmyType);
        string summary = targetParty != null
            ? $"{behavior} -> {targetParty.Name}"
            : targetSettlement != null
            ? $"{behavior} -> {targetSettlement.Name}"
            : behavior;

        return TruncateText(summary, 40);
    }

    public static string GetArmyCommandHeaderText(Army army)
    {
        if (army?.LeaderParty == null)
        {
            return "Army Commands";
        }

        MobileParty leaderParty = army.LeaderParty;
        if (leaderParty.MapEvent != null)
        {
            return "Army busy: in battle";
        }

        if (leaderParty.SiegeEvent != null)
        {
            return "Army busy: siege underway";
        }

        if (leaderParty.CurrentSettlement != null && leaderParty.CurrentSettlement.IsUnderSiege)
        {
            return "Army busy: besieged inside settlement";
        }

        List<MobileParty> allParties = GetAllPartiesFromArmy(army).ToList();
        List<MobileParty> joiningToday = GetPartiesJoiningToday(army, allParties).ToList();
        float currentFood = GetCurrentArmyFood(army);
        float dailyFood = GetTotalArmyFoodChange(army, joiningToday);

        if (army.Cohesion < 35f)
        {
            return "Army strained: low cohesion";
        }

        if (currentFood < 25f)
        {
            return "Army strained: low supplies";
        }

        if (dailyFood < -5f)
        {
            return "Army strained: food drain too high";
        }

        if (leaderParty.TargetParty != null)
        {
            return "Army ready: intercepting threat";
        }

        return "Army ready for orders";
    }

    public static string GetSettlementCommandLabel(Settlement settlement)
    {
        if (settlement == null)
        {
            return "No target";
        }

        if (IsEnemySettlement(settlement))
        {
            return settlement.IsVillage
                ? $"{settlement.Name} (Raid)"
                : $"{settlement.Name} (Besiege)";
        }

        return $"{settlement.Name} (Defend)";
    }

    public static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || maxLength <= 0 || text.Length <= maxLength)
        {
            return text ?? string.Empty;
        }

        return text.Substring(0, maxLength - 3).TrimEnd() + "...";
    }

    internal static Kingdom GetStrategicPrimaryEnemy(Kingdom kingdom)
    {
        return InvokeStatic<Kingdom>(GetPrimaryEnemyMethod, kingdom);
    }

    internal static Settlement GetStrategicObjectiveSettlement(Kingdom kingdom, Kingdom enemy)
    {
        return InvokeStatic<Settlement>(GetObjectiveSettlementMethod, kingdom, enemy);
    }

    internal static Settlement GetStrategicFrontlineSettlement(Kingdom kingdom, Kingdom enemy)
    {
        return InvokeStatic<Settlement>(GetFrontlineAnchorMethod, kingdom, enemy);
    }

    internal static Settlement GetStrategicTheaterSettlement(Kingdom kingdom, Kingdom enemy)
    {
        return InvokeStatic<Settlement>(GetTheaterAnchorMethod, kingdom, enemy);
    }

    private static Settlement GetStrategicDefaultTarget(Kingdom kingdom, Kingdom enemy, bool homelandDefense)
    {
        Settlement objective = GetStrategicObjectiveSettlement(kingdom, enemy);
        Settlement frontline = GetStrategicFrontlineSettlement(kingdom, enemy);
        Settlement theater = GetStrategicTheaterSettlement(kingdom, enemy);

        if (homelandDefense)
        {
            return new[] { objective, frontline, theater }
                .FirstOrDefault(settlement => settlement?.MapFaction == kingdom);
        }

        return new[] { objective, frontline, theater }
            .FirstOrDefault(settlement => settlement?.MapFaction == enemy);
    }

    internal static bool IsHomelandDefense(Kingdom kingdom, Kingdom enemy)
    {
        object focusMode = InvokeStatic<object>(GetTheaterFocusModeMethod, kingdom, enemy);
        return string.Equals(focusMode?.ToString(), "HomelandDefense", StringComparison.Ordinal);
    }

    internal static string GetStrategicFocusModeName(Kingdom kingdom, Kingdom enemy)
    {
        return InvokeStatic<object>(GetTheaterFocusModeMethod, kingdom, enemy)?.ToString() ?? string.Empty;
    }

    internal static string GetStrategicCampaignPhaseName(Kingdom kingdom, Kingdom enemy)
    {
        return InvokeStatic<object>(GetCampaignPhaseMethod, kingdom, enemy)?.ToString() ?? string.Empty;
    }

    internal static string GetStrategicOperationalStateName(Kingdom kingdom, Kingdom enemy)
    {
        return InvokeStatic<object>(GetOperationalStateMethod, kingdom, enemy)?.ToString() ?? string.Empty;
    }

    internal static float GetStrategicHomeFrontPressure(Kingdom kingdom)
    {
        return InvokeStaticFloat(GetHomeFrontPressureMethod, kingdom);
    }

    internal static float GetStrategicCoalitionRoleFactor(Kingdom kingdom, Kingdom enemy)
    {
        return InvokeStaticFloat(GetCoalitionRoleFactorMethod, kingdom, enemy);
    }

    internal static float GetStrategicCoalitionPullFactor(Kingdom kingdom, Kingdom enemy)
    {
        return InvokeStaticFloat(GetCoalitionPullFactorMethod, kingdom, enemy);
    }

    internal static float GetStrategicDecisivePressure(Kingdom kingdom, Kingdom enemy)
    {
        return InvokeStaticFloat(GetDecisiveCampaignPressureMethod, kingdom, enemy);
    }

    internal static float GetStrategicUnresolvedFrontPressure(Kingdom kingdom, Kingdom enemy)
    {
        return InvokeStaticFloat(GetUnresolvedFrontPressureMethod, kingdom, enemy);
    }

    private static T InvokeStatic<T>(MethodInfo method, params object[] args) where T : class
    {
        if (method == null)
        {
            return null;
        }

        try
        {
            return method.Invoke(null, args) as T;
        }
        catch
        {
            return null;
        }
    }

    private static float InvokeStaticFloat(MethodInfo method, params object[] args)
    {
        if (method == null)
        {
            return 0f;
        }

        try
        {
            object value = method.Invoke(null, args);
            return value is float floatValue ? floatValue : 0f;
        }
        catch
        {
            return 0f;
        }
    }

    public static float GetDaysDistance(MobileParty calledParty, MobileParty mainParty, float calledPartySpeed)
    {
        if (calledParty == null || mainParty == null || calledPartySpeed <= 0f)
        {
            return float.MaxValue;
        }

        float distance = Helpers.DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(calledParty, mainParty, calledParty.NavigationCapability) / calledPartySpeed;
        return distance / CampaignTime.HoursInDay;
    }

    public static IEnumerable<MobileParty> GetAllPartiesFromArmy(Army army)
    {
        if (army?.Kingdom?.AllParties == null)
        {
            return Enumerable.Empty<MobileParty>();
        }

        return army.Kingdom.AllParties
            .Where(party => IsSameArmy(party?.Army, army))
            .ToList();
    }

    public static IEnumerable<MobileParty> GetPartiesJoiningToday(Army army, IEnumerable<MobileParty> allParties)
    {
        if (army?.LeaderParty == null || allParties == null)
        {
            return Enumerable.Empty<MobileParty>();
        }

        return allParties
            .Where(party => !IsSameParty(party, army.LeaderParty) && party.AttachedTo == null && GetDaysDistance(party, army.LeaderParty, party.Speed) < 1f)
            .ToList();
    }

    public static int GetAttachedPartiesCount(Army army)
    {
        return army?.LeaderParty?.AttachedParties?.Count + 1 ?? 0;
    }

    public static int GetTotalAssignedPartiesCount(IEnumerable<MobileParty> allParties)
    {
        return allParties.Count();
    }

    public static int GetPartiesWithinADayDistanceCount(IEnumerable<MobileParty> partiesJoiningToday)
    {
        return partiesJoiningToday.Count();
    }

    public static int GetMenCount(Army army)
    {
        return army?.TotalManCount ?? 0;
    }

    public static int GetPotentialMenCount(IEnumerable<MobileParty> allParties)
    {
        return allParties?.Sum(party => party?.Party?.NumberOfAllMembers ?? 0) ?? 0;
    }

    public static int GetMenJoiningToday(IEnumerable<MobileParty> partiesJoiningToday)
    {
        return partiesJoiningToday?.Sum(party => party?.Party?.NumberOfAllMembers ?? 0) ?? 0;
    }

    public static float GetCurrentArmyFood(Army army)
    {
        MobileParty leaderParty = army?.LeaderParty;
        if (leaderParty == null)
        {
            return 0f;
        }

        List<MobileParty> attachedParties = leaderParty.AttachedParties?.Where(party => party != null).ToList() ?? new List<MobileParty>();
        return leaderParty.Food + attachedParties.Sum(party => party?.Food ?? 0f);
    }

    public static float GetTotalArmyFoodChange(Army army, IEnumerable<MobileParty> partiesJoiningToday)
    {
        MobileParty leaderParty = army?.LeaderParty;
        if (leaderParty == null)
        {
            return 0f;
        }

        List<MobileParty> attachedParties = leaderParty.AttachedParties?.Where(party => party != null).ToList() ?? new List<MobileParty>();
        return leaderParty.FoodChange
            + attachedParties.Sum(party => party?.FoodChange ?? 0f)
            + (partiesJoiningToday?.Sum(party => (party?.Food ?? 0f) + (party?.FoodChange ?? 0f)) ?? 0f);
    }

    public static float GetTotalArmyFood(IEnumerable<MobileParty> allParties)
    {
        return allParties?.Sum(party => party?.Food ?? 0f) ?? 0f;
    }

    public static float GetCurrentArmyInfluence(Army army)
    {
        return army?.LeaderParty?.LeaderHero?.Clan?.Influence ?? 0f;
    }

    public static float GetDailyArmyInfluenceChange(Army army)
    {
        return army?.LeaderParty?.LeaderHero?.Clan?.InfluenceChangeExplained.ResultNumber ?? 0f;
    }

    public static float GetCurrentCohesion(Army army)
    {
        return army?.Cohesion ?? 0f;
    }

    public static float GetDailyCohesionChange(Army army)
    {
        return army?.DailyCohesionChange ?? 0f;
    }

    public static int GetLostCohesionCostValue(Army army)
    {
        ArmyManagementCalculationModel calculationModel = Campaign.Current?.Models?.ArmyManagementCalculationModel;
        if (army == null || calculationModel == null)
        {
            return 0;
        }

        return calculationModel.CalculateTotalInfluenceCost(army, 100f - army.Cohesion);
    }

    public static int GetSendItemInfluenceCost(Army army)
    {
        return GetLostCohesionCostValue(army);
    }

    public static int GetDisbandInfluenceCost(Army army)
    {
        return army?.LeaderParty == null ? 0 : 50;
    }
}
