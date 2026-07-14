using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RF_warsystem.Diagnostics;
using RF_warsystem.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Behaviors;

public sealed class RFWarOperationalRhythmBehavior : CampaignBehaviorBase
{
    private readonly Dictionary<string, string> _stateByPair = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _holdUntilByPair = new(StringComparer.Ordinal);

    internal static RFWarOperationalRhythmBehavior? Instance { get; private set; }

    public RFWarOperationalRhythmBehavior()
    {
        Instance = this;
    }

    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
    }

    public override void SyncData(IDataStore dataStore)
    {
        string stateData = string.Empty;
        string holdData = string.Empty;

        if (!dataStore.IsLoading)
        {
            stateData = SerializeStringDictionary(_stateByPair);
            holdData = SerializeFloatDictionary(_holdUntilByPair);
        }

        dataStore.SyncData("RFWarSystem_OperationalStates", ref stateData);
        dataStore.SyncData("RFWarSystem_OperationalStateHoldUntil", ref holdData);

        if (!dataStore.IsLoading)
        {
            return;
        }

        DeserializeStringDictionary(stateData, _stateByPair);
        DeserializeFloatDictionary(holdData, _holdUntilByPair);
    }

    internal static RFWarOperationalState GetState(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentState(kingdom, enemy) ?? RFWarOperationalState.Advance;
    }

    internal static float GetTargetFactor(Kingdom kingdom, Kingdom enemy, TaleWorlds.CampaignSystem.Army.ArmyTypes missionType, TaleWorlds.CampaignSystem.Settlements.Settlement targetSettlement)
    {
        return Instance?.GetOperationalTargetFactor(kingdom, enemy, missionType, targetSettlement) ?? 0f;
    }

    internal static float GetPeaceFactor(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetOperationalPeaceFactor(kingdom, enemy) ?? 0f;
    }

    private void OnDailyTick()
    {
        PruneInactivePairs();

        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (kingdom == null || kingdom.IsEliminated)
            {
                continue;
            }

            foreach (Kingdom enemy in kingdom.FactionsAtWarWith.OfType<Kingdom>())
            {
                if (enemy == null || enemy.IsEliminated)
                {
                    continue;
                }

                UpdateState(kingdom, enemy);
            }
        }
    }

    private void OnMakePeace(IFaction faction1, IFaction faction2, TaleWorlds.CampaignSystem.Actions.MakePeaceAction.MakePeaceDetail detail)
    {
        if (faction1 is not Kingdom kingdom1 || faction2 is not Kingdom kingdom2)
        {
            return;
        }

        RemovePair(kingdom1, kingdom2);
        RemovePair(kingdom2, kingdom1);
    }

    private void OnWarDeclared(IFaction faction1, IFaction faction2, TaleWorlds.CampaignSystem.Actions.DeclareWarAction.DeclareWarDetail detail)
    {
        if (faction1 is not Kingdom kingdom1 || faction2 is not Kingdom kingdom2)
        {
            return;
        }

        ForceState(kingdom1, kingdom2, EvaluateDesiredState(kingdom1, kingdom2), "war_declared");
        ForceState(kingdom2, kingdom1, EvaluateDesiredState(kingdom2, kingdom1), "war_declared");
    }

    private void UpdateState(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        RFWarOperationalState desired = EvaluateDesiredState(kingdom, enemy);
        RFWarOperationalState current = GetCurrentState(kingdom, enemy);
        float now = GetCurrentDay();

        if (current == desired)
        {
            if (!_holdUntilByPair.ContainsKey(key))
            {
                _holdUntilByPair[key] = now + GetHoldDuration(kingdom, enemy, desired);
            }

            return;
        }

        if (!_holdUntilByPair.TryGetValue(key, out float holdUntil) || now >= holdUntil || CanBreakHoldEarly(kingdom, enemy, current, desired))
        {
            ForceState(kingdom, enemy, desired, "daily");
        }
    }

    private void ForceState(Kingdom kingdom, Kingdom enemy, RFWarOperationalState state, string source)
    {
        string key = GetPairKey(kingdom, enemy);
        RFWarOperationalState previous = GetCurrentState(kingdom, enemy);
        _stateByPair[key] = state.ToString();
        _holdUntilByPair[key] = GetCurrentDay() + GetHoldDuration(kingdom, enemy, state);
        if (previous != state)
        {
            RFWarSystemTraceLog.OperationalStateChanged(
                kingdom,
                enemy,
                previous.ToString(),
                state.ToString(),
                source,
                ("strength", kingdom.CurrentTotalStrength / Math.Max(1f, enemy.CurrentTotalStrength)),
                ("readiness", GetReadiness(kingdom)),
                ("momentum", RFWarStrategicMemoryBehavior.GetMomentum(kingdom, enemy)),
                ("homeThreat", GetHomeThreatRatio(kingdom, enemy)),
                ("homePressure", Math.Min(1f, RFWarStrategicMemoryBehavior.GetHomeFrontPressure(kingdom) / 2.25f)));
        }
    }

    private RFWarOperationalState GetCurrentState(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        if (_stateByPair.TryGetValue(key, out string? rawState) &&
            Enum.TryParse(rawState, ignoreCase: true, out RFWarOperationalState state))
        {
            return state;
        }

        return RFWarOperationalState.Advance;
    }

    private RFWarOperationalState EvaluateDesiredState(Kingdom kingdom, Kingdom enemy)
    {
        float strengthRatio = kingdom.CurrentTotalStrength / Math.Max(1f, enemy.CurrentTotalStrength);
        float readiness = GetReadiness(kingdom);
        float momentum = RFWarStrategicMemoryBehavior.GetMomentum(kingdom, enemy);
        float homeThreat = GetHomeThreatRatio(kingdom, enemy);
        float homePressure = Math.Min(1f, RFWarStrategicMemoryBehavior.GetHomeFrontPressure(kingdom) / 2.25f);
        float focus = Math.Max(0f, RFWarCampaignDirectorBehavior.GetEnemyFocusFactor(kingdom, enemy));
        float campaignLock = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(kingdom, enemy));
        bool offensivePush = RFWarTheaterBehavior.GetFocusMode(kingdom, enemy) == RFWarTheaterFocusMode.OffensivePush;
        RFWarCoalitionRole coalitionRole = RFWarCoalitionRoleBehavior.GetRole(kingdom, enemy);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);
        float objectiveCommitment = Math.Max(0f, RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy));
        float decisivePressure = Math.Max(0f, RFWarCampaignDirectorBehavior.GetDecisiveCampaignPressure(kingdom, enemy));

        if (homeThreat >= 0.45f || homePressure >= 0.7f)
        {
            return RFWarOperationalState.Defend;
        }

        if (phase == RFWarCampaignPhase.Stabilize)
        {
            return homeThreat >= 0.15f || homePressure >= 0.28f
                ? RFWarOperationalState.Defend
                : RFWarOperationalState.Regroup;
        }

        if (coalitionRole == RFWarCoalitionRole.BorderShield)
        {
            return homeThreat >= 0.15f || readiness < 0.42f
                ? RFWarOperationalState.Defend
                : RFWarOperationalState.Muster;
        }

        if (IsBesiegingEnemyFortification(kingdom, enemy))
        {
            return RFWarOperationalState.Besiege;
        }

        if (phase == RFWarCampaignPhase.BreakFront)
        {
            if (readiness < 0.42f)
            {
                return RFWarOperationalState.Muster;
            }

            return strengthRatio >= 0.92f && offensivePush
                ? RFWarOperationalState.Advance
                : RFWarOperationalState.Regroup;
        }

        if (phase == RFWarCampaignPhase.StripSupport)
        {
            if (coalitionRole == RFWarCoalitionRole.Raider && readiness >= 0.42f)
            {
                return momentum >= 0.1f ? RFWarOperationalState.Exploit : RFWarOperationalState.Advance;
            }

            return readiness >= 0.45f ? RFWarOperationalState.Advance : RFWarOperationalState.Muster;
        }

        if (phase == RFWarCampaignPhase.PressCastle || phase == RFWarCampaignPhase.PressTown)
        {
            if (readiness < 0.46f)
            {
                return RFWarOperationalState.Muster;
            }

            if ((strengthRatio < 0.88f && momentum < 0.05f)
                || (campaignLock >= 0.62f && objectiveCommitment >= 0.5f && (strengthRatio < 0.94f || momentum < -0.08f)))
            {
                return RFWarOperationalState.Regroup;
            }

            return IsBesiegingEnemyFortification(kingdom, enemy)
                ? RFWarOperationalState.Besiege
                : RFWarOperationalState.Advance;
        }

        if (phase == RFWarCampaignPhase.DeepStrike)
        {
            if (readiness < 0.44f)
            {
                return RFWarOperationalState.Muster;
            }

            return momentum >= 0.05f && offensivePush
                ? RFWarOperationalState.Exploit
                : RFWarOperationalState.Advance;
        }

        if (strengthRatio < 0.9f && (momentum <= -0.35f || readiness < 0.45f))
        {
            return RFWarOperationalState.Regroup;
        }

        if (campaignLock >= 0.68f
            && objectiveCommitment >= 0.52f
            && decisivePressure >= 0.28f
            && (readiness < 0.5f || strengthRatio < 0.94f || momentum < -0.1f))
        {
            return RFWarOperationalState.Regroup;
        }

        if (readiness < 0.4f)
        {
            return RFWarOperationalState.Muster;
        }

        if (coalitionRole == RFWarCoalitionRole.Reserve && (readiness < 0.58f || strengthRatio < 1f))
        {
            return RFWarOperationalState.Regroup;
        }

        if (coalitionRole == RFWarCoalitionRole.SiegeFinisher && readiness >= 0.48f && strengthRatio >= 0.95f)
        {
            return IsBesiegingEnemyFortification(kingdom, enemy) ? RFWarOperationalState.Besiege : RFWarOperationalState.Advance;
        }

        if (coalitionRole == RFWarCoalitionRole.Raider && offensivePush && readiness >= 0.45f)
        {
            return momentum >= 0.15f ? RFWarOperationalState.Exploit : RFWarOperationalState.Advance;
        }

        if (strengthRatio >= 1.08f && momentum >= 0.45f && offensivePush)
        {
            return RFWarOperationalState.Exploit;
        }

        if (offensivePush && focus >= 0.55f && readiness >= 0.48f && strengthRatio >= 0.96f)
        {
            return RFWarOperationalState.Advance;
        }

        return RFWarOperationalState.Advance;
    }

    private bool CanBreakHoldEarly(Kingdom kingdom, Kingdom enemy, RFWarOperationalState current, RFWarOperationalState desired)
    {
        float strengthRatio = kingdom.CurrentTotalStrength / Math.Max(1f, enemy.CurrentTotalStrength);
        float readiness = GetReadiness(kingdom);
        float momentum = RFWarStrategicMemoryBehavior.GetMomentum(kingdom, enemy);
        float homeThreat = GetHomeThreatRatio(kingdom, enemy);
        float homePressure = Math.Min(1f, RFWarStrategicMemoryBehavior.GetHomeFrontPressure(kingdom) / 2.25f);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);

        if (desired == RFWarOperationalState.Defend && (homeThreat >= 0.62f || homePressure >= 0.82f))
        {
            return true;
        }

        if (desired == RFWarOperationalState.Regroup && (strengthRatio < 0.75f || readiness < 0.3f || momentum < -0.9f))
        {
            return true;
        }

        if (phase == RFWarCampaignPhase.Stabilize && desired == RFWarOperationalState.Defend)
        {
            return true;
        }

        if ((phase == RFWarCampaignPhase.PressCastle || phase == RFWarCampaignPhase.PressTown) && desired == RFWarOperationalState.Besiege)
        {
            return true;
        }

        if (phase == RFWarCampaignPhase.DeepStrike && desired == RFWarOperationalState.Exploit && readiness >= 0.44f)
        {
            return true;
        }

        return current switch
        {
            RFWarOperationalState.Besiege => !IsBesiegingEnemyFortification(kingdom, enemy) || homeThreat >= 0.7f || readiness < 0.28f || strengthRatio < 0.72f,
            RFWarOperationalState.Defend => desired == RFWarOperationalState.Besiege && IsBesiegingEnemyFortification(kingdom, enemy) || (homeThreat <= 0.05f && homePressure <= 0.22f && desired == RFWarOperationalState.Exploit),
            RFWarOperationalState.Regroup => desired == RFWarOperationalState.Defend || readiness >= 0.58f && strengthRatio >= 0.96f && momentum >= 0.08f,
            RFWarOperationalState.Muster => desired == RFWarOperationalState.Defend || readiness >= 0.5f || IsBesiegingEnemyFortification(kingdom, enemy),
            RFWarOperationalState.Exploit => desired == RFWarOperationalState.Defend || strengthRatio < 1f || momentum < -0.05f || readiness < 0.42f,
            RFWarOperationalState.Advance => desired == RFWarOperationalState.Defend && homeThreat >= 0.6f || desired == RFWarOperationalState.Besiege && IsBesiegingEnemyFortification(kingdom, enemy),
            _ => false
        };
    }

    private float GetOperationalTargetFactor(Kingdom kingdom, Kingdom enemy, TaleWorlds.CampaignSystem.Army.ArmyTypes missionType, TaleWorlds.CampaignSystem.Settlements.Settlement targetSettlement)
    {
        RFWarOperationalState state = GetCurrentState(kingdom, enemy);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);
        bool isVillage = targetSettlement.IsVillage;
        bool isFortification = targetSettlement.IsFortification;
        bool isFrontier = IsFrontierTarget(kingdom, targetSettlement);
        bool sameObjectiveCluster = IsCurrentObjectiveCluster(kingdom, enemy, targetSettlement);
        float softness = GetSettlementSoftness(targetSettlement);

        if (missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Defender)
        {
            return state switch
            {
                RFWarOperationalState.Defend => isFrontier ? 0.85f : 0.55f,
                RFWarOperationalState.Regroup => isFrontier ? 0.45f : 0.25f,
                RFWarOperationalState.Muster => 0.2f,
                _ => 0f
            };
        }

        return state switch
        {
            RFWarOperationalState.Muster => (isFrontier && isFortification ? 0.12f : isVillage ? -0.5f : -0.28f) + GetPhaseOperationalBias(phase, targetSettlement),
            RFWarOperationalState.Advance => (isFrontier && isFortification ? 0.32f : isVillage ? -0.18f : 0.12f) + (sameObjectiveCluster ? 0.12f : 0f) + GetPhaseOperationalBias(phase, targetSettlement),
            RFWarOperationalState.Besiege => (isFortification ? 0.75f : -0.45f) + GetPhaseOperationalBias(phase, targetSettlement),
            RFWarOperationalState.Defend => -0.65f,
            RFWarOperationalState.Regroup => (isFrontier ? -0.2f : isVillage ? -0.6f : -0.42f) + (phase == RFWarCampaignPhase.Stabilize ? 0.1f : 0f),
            RFWarOperationalState.Exploit => (isVillage ? 0.35f : softness * 0.45f + (isFortification ? 0.08f : 0.18f)) + (sameObjectiveCluster ? 0.18f : 0f) + GetPhaseOperationalBias(phase, targetSettlement),
            _ => 0f
        };
    }

    private float GetOperationalPeaceFactor(Kingdom kingdom, Kingdom enemy)
    {
        return GetCurrentState(kingdom, enemy) switch
        {
            RFWarOperationalState.Regroup => 0.22f,
            RFWarOperationalState.Defend => 0.14f,
            RFWarOperationalState.Besiege => -0.18f,
            RFWarOperationalState.Exploit => -0.16f,
            _ => 0f
        };
    }

    private void PruneInactivePairs()
    {
        if (_stateByPair.Count == 0)
        {
            return;
        }

        List<string> toRemove = new();
        foreach (KeyValuePair<string, string> pair in _stateByPair)
        {
            if (!IsPairStillActive(pair.Key))
            {
                toRemove.Add(pair.Key);
            }
        }

        foreach (string key in toRemove)
        {
            _stateByPair.Remove(key);
            _holdUntilByPair.Remove(key);
        }
    }

    private static bool IsPairStillActive(string key)
    {
        string[] split = key.Split(new[] { "->" }, StringSplitOptions.None);
        if (split.Length != 2)
        {
            return false;
        }

        Kingdom? left = Kingdom.All.FirstOrDefault(kingdom => GetKingdomKey(kingdom) == split[0]);
        Kingdom? right = Kingdom.All.FirstOrDefault(kingdom => GetKingdomKey(kingdom) == split[1]);
        return left != null && right != null && !left.IsEliminated && !right.IsEliminated && left.IsAtWarWith(right);
    }

    private void RemovePair(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        _stateByPair.Remove(key);
        _holdUntilByPair.Remove(key);
    }

    private static float GetHoldDuration(Kingdom kingdom, Kingdom enemy, RFWarOperationalState state)
    {
        float readiness = GetReadiness(kingdom);
        float homeThreat = GetHomeThreatRatio(kingdom, enemy);
        float momentum = RFWarStrategicMemoryBehavior.GetMomentum(kingdom, enemy);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);
        float campaignLock = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(kingdom, enemy));
        float objectiveCommitment = Math.Max(0f, RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy));

        float duration = state switch
        {
            RFWarOperationalState.Muster => readiness < 0.32f ? 4f : 3.2f,
            RFWarOperationalState.Advance => 3.1f,
            RFWarOperationalState.Besiege => homeThreat >= 0.3f ? 3.2f : 4.6f,
            RFWarOperationalState.Defend => homeThreat >= 0.45f ? 3.8f : 3f,
            RFWarOperationalState.Regroup => momentum < -0.65f ? 4.4f : 3.5f,
            RFWarOperationalState.Exploit => momentum >= 0.8f ? 2.8f : 2.1f,
            _ => 2f
        };

        if ((phase == RFWarCampaignPhase.PressCastle || phase == RFWarCampaignPhase.PressTown) && state == RFWarOperationalState.Besiege)
        {
            duration += 0.6f;
        }
        else if (phase == RFWarCampaignPhase.Stabilize && (state == RFWarOperationalState.Defend || state == RFWarOperationalState.Regroup))
        {
            duration += 0.4f;
        }
        else if (phase == RFWarCampaignPhase.DeepStrike && state == RFWarOperationalState.Exploit)
        {
            duration += 0.25f;
        }

        if (state == RFWarOperationalState.Regroup)
        {
            duration += campaignLock * 0.55f;
            duration += objectiveCommitment * 0.45f;
        }

        return duration;
    }

    private static float GetPhaseOperationalBias(RFWarCampaignPhase phase, Settlement targetSettlement)
    {
        return phase switch
        {
            RFWarCampaignPhase.BreakFront => targetSettlement.IsFortification ? 0.08f : targetSettlement.IsVillage ? -0.08f : 0f,
            RFWarCampaignPhase.StripSupport => targetSettlement.IsVillage ? 0.12f : targetSettlement.IsFortification ? -0.08f : 0f,
            RFWarCampaignPhase.PressCastle => targetSettlement.IsCastle ? 0.14f : targetSettlement.IsVillage ? -0.08f : 0f,
            RFWarCampaignPhase.PressTown => targetSettlement.IsTown ? 0.14f : targetSettlement.IsVillage ? -0.06f : 0f,
            RFWarCampaignPhase.DeepStrike => !targetSettlement.IsFortification ? 0.08f : -0.1f,
            _ => 0f
        };
    }

    private static float GetCurrentDay()
    {
        return (float)CampaignTime.Now.ToDays;
    }

    private static float GetReadiness(Kingdom kingdom)
    {
        List<MobileParty> parties = kingdom.WarPartyComponents
            .Select(component => component.MobileParty)
            .Where(party => party != null && party.IsActive && party.LeaderHero != null && !party.IsCaravan && !party.IsMilitia)
            .ToList();

        if (parties.Count == 0)
        {
            return 0f;
        }

        float averageFood = parties.Average(party => Math.Min(18f, Math.Max(0f, party.GetNumDaysForFoodToLast()))) / 18f;
        float averageSize = parties.Average(party => Math.Min(1f, Math.Max(0f, party.PartySizeRatio)));
        return (averageFood * 0.55f) + (averageSize * 0.45f);
    }

    private static float GetHomeThreatRatio(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.Fiefs.Any())
        {
            return 0f;
        }

        int threatened = 0;
        foreach (Town town in kingdom.Fiefs)
        {
            if (town?.Settlement == null)
            {
                continue;
            }

            if (town.Settlement.IsUnderSiege && town.Settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction == enemy)
            {
                threatened++;
                continue;
            }

            MobileParty? attacker = town.Settlement.LastAttackerParty;
            if (attacker != null && attacker.IsActive && attacker.MapFaction == enemy)
            {
                threatened++;
            }
        }

        return threatened / (float)Math.Max(1, kingdom.Fiefs.Count);
    }

    private static bool IsBesiegingEnemyFortification(Kingdom kingdom, Kingdom enemy)
    {
        foreach (Town town in enemy.Fiefs)
        {
            if (town?.Settlement == null || !town.Settlement.IsFortification || !town.Settlement.IsUnderSiege)
            {
                continue;
            }

            if (town.Settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction == kingdom)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFrontierTarget(Kingdom kingdom, TaleWorlds.CampaignSystem.Settlements.Settlement targetSettlement)
    {
        foreach (Town town in kingdom.Fiefs)
        {
            if (town?.Settlement == null)
            {
                continue;
            }

            if (town.Settlement.GatePosition.DistanceSquared(targetSettlement.GatePosition) <= 19600f)
            {
                return true;
            }
        }

        return false;
    }

    private static float GetSettlementSoftness(TaleWorlds.CampaignSystem.Settlements.Settlement settlement)
    {
        int garrison = settlement.Town?.GarrisonParty?.Party.NumberOfHealthyMembers ?? 0;
        int militia = (int)settlement.Militia;
        int defenders = garrison + militia;
        int threshold = settlement.IsTown ? 220 : settlement.IsCastle ? 140 : 60;
        if (defenders >= threshold)
        {
            return 0f;
        }

        return Math.Max(0f, Math.Min(1f, (threshold - defenders) / (float)threshold));
    }

    private static bool IsCurrentObjectiveCluster(Kingdom kingdom, Kingdom enemy, Settlement targetSettlement)
    {
        Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);
        if (objective == null)
        {
            return false;
        }

        return objective == targetSettlement || objective.GatePosition.DistanceSquared(targetSettlement.GatePosition) <= 32400f;
    }

    private static string GetPairKey(Kingdom kingdom, Kingdom enemy)
    {
        return $"{GetKingdomKey(kingdom)}->{GetKingdomKey(enemy)}";
    }

    private static string GetKingdomKey(Kingdom kingdom)
    {
        return string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
    }

    private static string SerializeStringDictionary(Dictionary<string, string> values)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(";", values.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static string SerializeFloatDictionary(Dictionary<string, float> values)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(";", values.Select(pair => $"{pair.Key}={pair.Value.ToString("R", CultureInfo.InvariantCulture)}"));
    }

    private static void DeserializeStringDictionary(string serialized, Dictionary<string, string> target)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return;
        }

        foreach (string entry in serialized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = entry.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= entry.Length - 1)
            {
                continue;
            }

            target[entry.Substring(0, separatorIndex)] = entry.Substring(separatorIndex + 1);
        }
    }

    private static void DeserializeFloatDictionary(string serialized, Dictionary<string, float> target)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return;
        }

        foreach (string entry in serialized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = entry.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= entry.Length - 1)
            {
                continue;
            }

            if (float.TryParse(entry.Substring(separatorIndex + 1), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float value))
            {
                target[entry.Substring(0, separatorIndex)] = value;
            }
        }
    }
}
