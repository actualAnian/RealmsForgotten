using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Behaviors;

public sealed class RFWarStrategicMemoryBehavior : CampaignBehaviorBase
{
    private readonly Dictionary<string, float> _pairMomentum = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _pairRivalry = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _pairCommitment = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _homePressure = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _settlementHeat = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _frontMomentum = new(StringComparer.Ordinal);

    internal static RFWarStrategicMemoryBehavior? Instance { get; private set; }

    public RFWarStrategicMemoryBehavior()
    {
        Instance = this;
    }

    public override void RegisterEvents()
    {
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageBeingRaided);
        CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
        string pairMomentumState = string.Empty;
        string pairRivalryState = string.Empty;
        string pairCommitmentState = string.Empty;
        string homePressureState = string.Empty;
        string settlementHeatState = string.Empty;
        string frontMomentumState = string.Empty;

        if (!dataStore.IsLoading)
        {
            pairMomentumState = SerializeDictionary(_pairMomentum);
            pairRivalryState = SerializeDictionary(_pairRivalry);
            pairCommitmentState = SerializeDictionary(_pairCommitment);
            homePressureState = SerializeDictionary(_homePressure);
            settlementHeatState = SerializeDictionary(_settlementHeat);
            frontMomentumState = SerializeDictionary(_frontMomentum);
        }

        dataStore.SyncData("RFWarSystem_PairMomentum", ref pairMomentumState);
        dataStore.SyncData("RFWarSystem_PairRivalry", ref pairRivalryState);
        dataStore.SyncData("RFWarSystem_PairCommitment", ref pairCommitmentState);
        dataStore.SyncData("RFWarSystem_HomePressure", ref homePressureState);
        dataStore.SyncData("RFWarSystem_SettlementHeat", ref settlementHeatState);
        dataStore.SyncData("RFWarSystem_FrontMomentum", ref frontMomentumState);

        if (!dataStore.IsLoading)
        {
            return;
        }

        DeserializeDictionary(pairMomentumState, _pairMomentum);
        DeserializeDictionary(pairRivalryState, _pairRivalry);
        DeserializeDictionary(pairCommitmentState, _pairCommitment);
        DeserializeDictionary(homePressureState, _homePressure);
        DeserializeDictionary(settlementHeatState, _settlementHeat);
        DeserializeDictionary(frontMomentumState, _frontMomentum);
    }

    internal static float GetMomentum(Kingdom attacker, Kingdom defender)
    {
        return Instance?.GetPairValue(attacker, defender) ?? 0f;
    }

    internal static float GetHomeFrontPressure(Kingdom kingdom)
    {
        return Instance?.GetHomePressureValue(kingdom) ?? 0f;
    }

    internal static float GetRivalry(Kingdom kingdom1, Kingdom kingdom2)
    {
        return Instance?.GetPairRivalryValue(kingdom1, kingdom2) ?? 0f;
    }

    internal static float GetWarCommitment(Kingdom kingdom1, Kingdom kingdom2)
    {
        return Instance?.GetPairCommitmentValue(kingdom1, kingdom2) ?? 0f;
    }

    internal static float GetSettlementHeat(Kingdom kingdom, Settlement settlement)
    {
        return Instance?.GetSettlementHeatValue(kingdom, settlement) ?? 0f;
    }

    internal static float GetFrontMomentum(Kingdom source, Kingdom target, Settlement settlement)
    {
        return Instance?.GetFrontMomentumValue(source, target, settlement) ?? 0f;
    }

    public static void RaiseExternalPairCommitmentFloor(Kingdom kingdom1, Kingdom kingdom2, float floor)
    {
        Instance?.RaisePairCommitmentFloor(kingdom1, kingdom2, floor);
    }

    public static void RaiseExternalPairRivalryFloor(Kingdom kingdom1, Kingdom kingdom2, float floor)
    {
        Instance?.RaisePairRivalryFloor(kingdom1, kingdom2, floor);
    }

    public static void RaiseExternalHomePressureFloor(Kingdom kingdom, float floor)
    {
        Instance?.RaiseHomePressureFloor(kingdom, floor);
    }

    public static void RaiseExternalSettlementHeatFloor(Kingdom kingdom, Settlement settlement, float floor)
    {
        Instance?.RaiseSettlementHeatFloor(kingdom, settlement, floor);
    }

    private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
    {
        if (faction1 is not Kingdom kingdom1 || faction2 is not Kingdom kingdom2)
        {
            return;
        }

        AdjustHomePressure(kingdom1, 0.08f);
        AdjustHomePressure(kingdom2, 0.16f);
        AdjustPairRivalry(kingdom1, kingdom2, 0.22f);
        RaisePairCommitmentFloor(kingdom1, kingdom2, 0.4f);
    }

    private void OnMapEventEnded(MapEvent mapEvent)
    {
        if (mapEvent == null || !mapEvent.HasWinner)
        {
            return;
        }

        MapEventSide winnerSide = mapEvent.Winner;
        MapEventSide loserSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
        if (winnerSide == null || loserSide == null)
        {
            return;
        }

        List<Kingdom> winners = GetKingdomsOnSide(winnerSide);
        List<Kingdom> losers = GetKingdomsOnSide(loserSide);
        if (winners.Count == 0 || losers.Count == 0)
        {
            return;
        }

        float swing = GetBattleSwing(mapEvent);
        foreach (Kingdom winner in winners)
        {
            foreach (Kingdom loser in losers)
            {
                AdjustPairMomentum(winner, loser, 0.65f * swing);
                AdjustPairMomentum(loser, winner, -0.8f * swing);
                AdjustPairRivalry(winner, loser, 0.08f * swing);
                RaisePairCommitmentFloor(winner, loser, 0.45f);

                if (mapEvent.MapEventSettlement != null)
                {
                    AdjustFrontMomentum(winner, loser, mapEvent.MapEventSettlement, 0.7f * swing);
                    AdjustFrontMomentum(loser, winner, mapEvent.MapEventSettlement, -0.85f * swing);
                }
            }

            AdjustHomePressure(winner, -0.12f * swing);
        }

        foreach (Kingdom loser in losers)
        {
            AdjustHomePressure(loser, 0.35f * swing);
            if (mapEvent.MapEventSettlement != null)
            {
                AdjustSettlementHeat(loser, mapEvent.MapEventSettlement, 0.45f * swing);
            }
        }
    }

    private void OnSettlementOwnerChanged(
        Settlement settlement,
        bool openToClaim,
        Hero newOwner,
        Hero oldOwner,
        Hero capturerHero,
        ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        Kingdom? oldKingdom = oldOwner?.Clan?.Kingdom;
        Kingdom? newKingdom = newOwner?.Clan?.Kingdom;
        if (settlement == null || oldKingdom == null || newKingdom == null || oldKingdom == newKingdom)
        {
            return;
        }

        float swing = settlement.IsTown ? 1.45f : settlement.IsCastle ? 1.15f : 0.75f;
        AdjustPairMomentum(newKingdom, oldKingdom, 0.9f * swing);
        AdjustPairMomentum(oldKingdom, newKingdom, -1.1f * swing);
        AdjustPairRivalry(newKingdom, oldKingdom, 0.22f * swing);
        RaisePairCommitmentFloor(newKingdom, oldKingdom, settlement.IsTown ? 0.72f : 0.58f);
        AdjustHomePressure(oldKingdom, 0.7f * swing);
        AdjustHomePressure(newKingdom, -0.18f * swing);
        AdjustSettlementHeat(oldKingdom, settlement, settlement.IsTown ? 1.6f : 1.15f);
        AdjustFrontMomentum(newKingdom, oldKingdom, settlement, 1f * swing);
        AdjustFrontMomentum(oldKingdom, newKingdom, settlement, -1.2f * swing);
    }

    private void OnVillageBeingRaided(Village village)
    {
        Settlement? settlement = village?.Settlement;
        Kingdom? kingdom = settlement?.OwnerClan?.Kingdom;
        if (settlement == null || kingdom == null)
        {
            return;
        }

        if (settlement.LastAttackerParty?.MapFaction is Kingdom raiderKingdom && raiderKingdom != kingdom)
        {
            AdjustPairRivalry(kingdom, raiderKingdom, 0.12f);
            RaisePairCommitmentFloor(kingdom, raiderKingdom, 0.35f);
            AdjustFrontMomentum(kingdom, raiderKingdom, settlement, -0.45f);
            AdjustFrontMomentum(raiderKingdom, kingdom, settlement, 0.32f);
        }

        AdjustHomePressure(kingdom, 0.3f);
        AdjustSettlementHeat(kingdom, settlement, 0.9f);
    }

    private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
    {
        if (faction1 is not Kingdom kingdom1 || faction2 is not Kingdom kingdom2)
        {
            return;
        }

        SoftenPairMomentum(kingdom1, kingdom2);
        SoftenPairMomentum(kingdom2, kingdom1);
        SoftenPairCommitment(kingdom1, kingdom2);
        SoftenPairRivalry(kingdom1, kingdom2);
        AdjustHomePressure(kingdom1, -0.25f);
        AdjustHomePressure(kingdom2, -0.25f);
    }

    private void OnDailyTick()
    {
        DecayDictionary(_pairMomentum, 0.92f);
        DecayDictionary(_pairRivalry, 0.988f);
        DecayDictionary(_pairCommitment, 0.94f);
        DecayDictionary(_homePressure, 0.9f);
        DecayDictionary(_settlementHeat, 0.9f);
        DecayDictionary(_frontMomentum, 0.975f);
        ReinforceActiveWarCommitment();
    }

    private static List<Kingdom> GetKingdomsOnSide(MapEventSide side)
    {
        return side.Parties
            .Select(party => party.Party?.MapFaction)
            .OfType<Kingdom>()
            .Distinct()
            .ToList();
    }

    private static float GetBattleSwing(MapEvent mapEvent)
    {
        if (mapEvent.IsSiegeAssault || mapEvent.IsSiegeAmbush || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut)
        {
            return 1.35f;
        }

        if (mapEvent.IsRaid)
        {
            return 0.75f;
        }

        if (mapEvent.IsNavalMapEvent)
        {
            return 0.9f;
        }

        return 1f;
    }

    private void AdjustPairMomentum(Kingdom source, Kingdom target, float delta)
    {
        string key = GetPairKey(source, target);
        _pairMomentum.TryGetValue(key, out float current);
        _pairMomentum[key] = Clamp(current + delta, -3.5f, 3.5f);
    }

    private void SoftenPairMomentum(Kingdom source, Kingdom target)
    {
        string key = GetPairKey(source, target);
        if (_pairMomentum.TryGetValue(key, out float current))
        {
            _pairMomentum[key] = current * 0.35f;
        }
    }

    private void AdjustPairRivalry(Kingdom kingdom1, Kingdom kingdom2, float delta)
    {
        string key = GetSharedPairKey(kingdom1, kingdom2);
        _pairRivalry.TryGetValue(key, out float current);
        _pairRivalry[key] = Clamp(current + delta, 0f, 4f);
    }

    private void SoftenPairRivalry(Kingdom kingdom1, Kingdom kingdom2)
    {
        string key = GetSharedPairKey(kingdom1, kingdom2);
        if (_pairRivalry.TryGetValue(key, out float current))
        {
            _pairRivalry[key] = current * 0.92f;
        }
    }

    private void RaisePairRivalryFloor(Kingdom kingdom1, Kingdom kingdom2, float floor)
    {
        string key = GetSharedPairKey(kingdom1, kingdom2);
        _pairRivalry.TryGetValue(key, out float current);
        _pairRivalry[key] = Math.Max(current, Clamp(floor, 0f, 4f));
    }

    private void RaisePairCommitmentFloor(Kingdom kingdom1, Kingdom kingdom2, float floor)
    {
        string key = GetSharedPairKey(kingdom1, kingdom2);
        _pairCommitment.TryGetValue(key, out float current);
        _pairCommitment[key] = Math.Max(current, Clamp(floor, 0f, 1.2f));
    }

    private void SoftenPairCommitment(Kingdom kingdom1, Kingdom kingdom2)
    {
        string key = GetSharedPairKey(kingdom1, kingdom2);
        if (_pairCommitment.TryGetValue(key, out float current))
        {
            _pairCommitment[key] = current * 0.18f;
        }
    }

    private void AdjustHomePressure(Kingdom kingdom, float delta)
    {
        string key = GetKingdomKey(kingdom);
        _homePressure.TryGetValue(key, out float current);
        _homePressure[key] = Clamp(current + delta, 0f, 4f);
    }

    private void RaiseHomePressureFloor(Kingdom kingdom, float floor)
    {
        string key = GetKingdomKey(kingdom);
        _homePressure.TryGetValue(key, out float current);
        _homePressure[key] = Math.Max(current, Clamp(floor, 0f, 4f));
    }

    private void AdjustSettlementHeat(Kingdom kingdom, Settlement settlement, float delta)
    {
        string key = GetSettlementKey(kingdom, settlement);
        _settlementHeat.TryGetValue(key, out float current);
        _settlementHeat[key] = Clamp(current + delta, 0f, 4f);
    }

    private void AdjustFrontMomentum(Kingdom source, Kingdom target, Settlement settlement, float delta)
    {
        Settlement? anchor = ResolveFrontAnchor(settlement);
        if (anchor == null)
        {
            return;
        }

        string key = GetFrontKey(source, target, anchor);
        _frontMomentum.TryGetValue(key, out float current);
        _frontMomentum[key] = Clamp(current + delta, -4f, 4f);
    }

    private void RaiseSettlementHeatFloor(Kingdom kingdom, Settlement settlement, float floor)
    {
        string key = GetSettlementKey(kingdom, settlement);
        _settlementHeat.TryGetValue(key, out float current);
        _settlementHeat[key] = Math.Max(current, Clamp(floor, 0f, 4f));
    }

    private float GetPairValue(Kingdom source, Kingdom target)
    {
        _pairMomentum.TryGetValue(GetPairKey(source, target), out float value);
        return value;
    }

    private float GetHomePressureValue(Kingdom kingdom)
    {
        _homePressure.TryGetValue(GetKingdomKey(kingdom), out float value);
        return value;
    }

    private float GetPairRivalryValue(Kingdom kingdom1, Kingdom kingdom2)
    {
        _pairRivalry.TryGetValue(GetSharedPairKey(kingdom1, kingdom2), out float value);
        return value;
    }

    private float GetPairCommitmentValue(Kingdom kingdom1, Kingdom kingdom2)
    {
        _pairCommitment.TryGetValue(GetSharedPairKey(kingdom1, kingdom2), out float value);
        return value;
    }

    private float GetSettlementHeatValue(Kingdom kingdom, Settlement settlement)
    {
        _settlementHeat.TryGetValue(GetSettlementKey(kingdom, settlement), out float value);
        return value;
    }

    private float GetFrontMomentumValue(Kingdom source, Kingdom target, Settlement settlement)
    {
        Settlement? anchor = ResolveFrontAnchor(settlement);
        if (anchor == null)
        {
            return 0f;
        }

        _frontMomentum.TryGetValue(GetFrontKey(source, target, anchor), out float value);
        return value;
    }

    private void ReinforceActiveWarCommitment()
    {
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

                string key = GetSharedPairKey(kingdom, enemy);
                _pairCommitment.TryGetValue(key, out float current);
                _pairCommitment[key] = Clamp(Math.Max(current, 0.28f) + 0.015f, 0f, 1.2f);
            }
        }
    }

    private static void DecayDictionary(Dictionary<string, float> values, float factor)
    {
        if (values.Count == 0)
        {
            return;
        }

        List<string> toRemove = new();
        foreach (KeyValuePair<string, float> pair in values.ToList())
        {
            string key = pair.Key;
            float value = pair.Value;
            float next = value * factor;
            if (Math.Abs(next) < 0.05f)
            {
                toRemove.Add(key);
            }
            else
            {
                values[key] = next;
            }
        }

        foreach (string key in toRemove)
        {
            values.Remove(key);
        }
    }

    private static string GetPairKey(Kingdom source, Kingdom target)
    {
        return $"{GetKingdomKey(source)}->{GetKingdomKey(target)}";
    }

    private static string GetSharedPairKey(Kingdom kingdom1, Kingdom kingdom2)
    {
        string left = GetKingdomKey(kingdom1);
        string right = GetKingdomKey(kingdom2);
        return string.CompareOrdinal(left, right) <= 0 ? $"{left}<->{right}" : $"{right}<->{left}";
    }

    private static string GetSettlementKey(Kingdom kingdom, Settlement settlement)
    {
        return $"{GetKingdomKey(kingdom)}::{settlement.StringId}";
    }

    private static string GetFrontKey(Kingdom source, Kingdom target, Settlement anchor)
    {
        return $"{GetKingdomKey(source)}->{GetKingdomKey(target)}::{anchor.StringId}";
    }

    private static Settlement? ResolveFrontAnchor(Settlement settlement)
    {
        if (settlement.IsFortification)
        {
            return settlement;
        }

        if (settlement.IsVillage)
        {
            return settlement.Village?.Bound ?? settlement;
        }

        return settlement;
    }

    private static string GetKingdomKey(Kingdom kingdom)
    {
        return string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
    }

    private static float Clamp(float value, float min, float max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private static string SerializeDictionary(Dictionary<string, float> values)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(";", values.Select(pair => $"{pair.Key}={pair.Value.ToString("R", CultureInfo.InvariantCulture)}"));
    }

    private static void DeserializeDictionary(string serialized, Dictionary<string, float> target)
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

            string key = entry.Substring(0, separatorIndex);
            string rawValue = entry.Substring(separatorIndex + 1);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (float.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float value))
            {
                target[key] = value;
            }
        }
    }
}
