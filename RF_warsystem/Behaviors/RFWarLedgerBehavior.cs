using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RF_warsystem.Diagnostics;
using RF_warsystem.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Behaviors;

public sealed class RFWarLedgerBehavior : CampaignBehaviorBase
{
    private const int MaxCompletedWars = 24;
    private const int MaxEventsPerWar = 80;
    private const int MaxSerializedChunkLength = 24000;
    private const float MaximumRaidScorePerSide = 15f;

    private readonly List<RFWarLedgerRecord> _records = new();
    private readonly Dictionary<string, RFWarLedgerRecord> _activeByPair = new(StringComparer.Ordinal);

    public static RFWarLedgerBehavior? Instance { get; private set; }

    public RFWarLedgerBehavior()
    {
        Instance = this;
    }

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
        CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
        List<string> serializedChunks = new();
        if (dataStore.IsSaving)
        {
            serializedChunks = SplitSerializedLedger(Serialize());
        }

        dataStore.SyncData("RFWarSystem_WarLedger_v2", ref serializedChunks);

        if (dataStore.IsLoading)
        {
            if (serializedChunks != null && serializedChunks.Count > 0)
            {
                Deserialize(string.Concat(serializedChunks));
                return;
            }

            string legacySerialized = string.Empty;
            dataStore.SyncData("RFWarSystem_WarLedger_v1", ref legacySerialized);
            Deserialize(legacySerialized);
        }
    }

    public static IReadOnlyList<RFWarLedgerRecord> GetTrackedWars()
    {
        if (Instance == null)
        {
            return Array.Empty<RFWarLedgerRecord>();
        }

        return Instance._records.ToList();
    }

    public static RFWarLedgerRecord? GetActiveWar(Kingdom first, Kingdom second)
    {
        return Instance?.FindActiveWar(first, second);
    }

    public static float GetScoreFor(Kingdom viewingKingdom, Kingdom opposingKingdom)
    {
        RFWarLedgerRecord? record = GetActiveWar(viewingKingdom, opposingKingdom);
        return record?.GetScoreFor(GetKingdomId(viewingKingdom)) ?? 0f;
    }

    internal static void RecordObjectiveSelection(
        Kingdom kingdom,
        Kingdom enemy,
        Settlement objective,
        string source)
    {
        Instance?.RecordObjectiveSelectionInternal(kingdom, enemy, objective, source);
    }

    public static string BuildDebugReport(Kingdom first, Kingdom second)
    {
        RFWarLedgerRecord? record = GetActiveWar(first, second)
            ?? Instance?._records.LastOrDefault(candidate => IsSamePair(candidate, first, second));

        if (record == null)
        {
            return "No war ledger record exists for this pair.";
        }

        StringBuilder builder = new();
        builder.Append("War ").Append(record.SideAKingdomId).Append(" vs ").Append(record.SideBKingdomId)
            .Append(" | active=").Append(record.IsActive)
            .Append(" | initiator=").Append(record.InitiatorKingdomId)
            .Append(" | motive=").Append(record.Motive)
            .Append(" | score(A)=").Append(record.Score.ToString("0.0", CultureInfo.InvariantCulture));

        builder.Append(" | occupation(A)=").Append(record.GetComponentScoreFor(
                record.SideAKingdomId,
                RFWarLedgerEventType.TownCaptured,
                RFWarLedgerEventType.CastleCaptured,
                RFWarLedgerEventType.TownRetaken,
                RFWarLedgerEventType.CastleRetaken).ToString("0.0", CultureInfo.InvariantCulture))
            .Append(" | battles(A)=").Append(record.GetComponentScoreFor(
                record.SideAKingdomId,
                RFWarLedgerEventType.BattleWon,
                RFWarLedgerEventType.MajorBattleWon).ToString("0.0", CultureInfo.InvariantCulture));

        if (record.IsActive)
        {
            builder.Append(" | exhaustion(").Append(GetKingdomId(first)).Append(")=")
                .Append((RFWarStrategicAssessment.GetWarExhaustion(first, second) * 100f).ToString("0", CultureInfo.InvariantCulture)).Append('%')
                .Append(" | warWill=")
                .Append((RFWarStrategicAssessment.GetWarWill(first, second) * 100f).ToString("0", CultureInfo.InvariantCulture)).Append('%')
                .Append(" | peacePressure=")
                .Append((RFWarStrategicAssessment.GetPeacePressure(first, second) * 100f).ToString("0", CultureInfo.InvariantCulture)).Append('%')
                .Append(" | finishPressure=")
                .Append((RFWarStrategicAssessment.GetFinishPressure(first, second) * 100f).ToString("0", CultureInfo.InvariantCulture)).Append('%');
        }

        foreach (RFWarLedgerEvent ledgerEvent in record.Events.Skip(Math.Max(0, record.Events.Count - 10)))
        {
            builder.AppendLine();
            builder.Append(ledgerEvent.Day.ToString("0.0", CultureInfo.InvariantCulture)).Append(" ")
                .Append(ledgerEvent.Type).Append(" ")
                .Append(ledgerEvent.ActorKingdomId).Append(" -> ").Append(ledgerEvent.TargetKingdomId)
                .Append(" delta=").Append(ledgerEvent.Delta.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture))
                .Append(" score=").Append(ledgerEvent.ScoreAfter.ToString("0.0", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private void RecordObjectiveSelectionInternal(
        Kingdom kingdom,
        Kingdom enemy,
        Settlement objective,
        string source)
    {
        if (kingdom == null || enemy == null || objective == null)
        {
            return;
        }

        RFWarLedgerRecord? record = FindActiveWar(kingdom, enemy);
        if (record == null)
        {
            return;
        }

        string kingdomId = GetKingdomId(kingdom);
        string enemyId = GetKingdomId(enemy);
        bool duplicate = record.Events.Any(ledgerEvent =>
            ledgerEvent.Type == RFWarLedgerEventType.ObjectiveSelected
            && ledgerEvent.ActorKingdomId == kingdomId
            && ledgerEvent.TargetKingdomId == enemyId
            && ledgerEvent.SettlementId == objective.StringId
            && GetCurrentDay() - ledgerEvent.Day < 3f);
        if (!duplicate)
        {
            AddEvent(
                record,
                RFWarLedgerEventType.ObjectiveSelected,
                0f,
                kingdomId,
                enemyId,
                objective.StringId,
                detail: source);
        }
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        ReconcileActiveWars();
    }

    private void OnDailyTick()
    {
        ReconcileActiveWars();
        PruneCompletedWars();
    }

    private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
    {
        if (faction1 is Kingdom first && faction2 is Kingdom second)
        {
            StartWar(first, second, RFWarLedgerEventType.WarDeclared, detail.ToString());
        }
    }

    private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
    {
        if (faction1 is not Kingdom first || faction2 is not Kingdom second)
        {
            return;
        }

        RFWarLedgerRecord? record = FindActiveWar(first, second);
        if (record != null)
        {
            CloseWar(record, first, second, detail.ToString());
        }
    }

    private void OnMapEventEnded(MapEvent mapEvent)
    {
        if (mapEvent == null || !mapEvent.HasWinner || mapEvent.IsRaid)
        {
            return;
        }

        MapEventSide? winnerSide = mapEvent.Winner;
        MapEventSide? loserSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
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

        int losingCasualties = CountCasualties(loserSide);
        int startingStrength = CountStartingHealthyTroops(winnerSide) + CountStartingHealthyTroops(loserSide);
        bool majorBattle = startingStrength >= 300;
        float magnitude = Math.Max(1f, Math.Min(12f, losingCasualties / 60f));
        if (majorBattle)
        {
            magnitude = Math.Min(15f, magnitude + 3f);
        }

        foreach (Kingdom winner in winners)
        {
            foreach (Kingdom loser in losers)
            {
                RFWarLedgerRecord? record = FindActiveWar(winner, loser);
                if (record == null)
                {
                    continue;
                }

                AddScoredEvent(
                    record,
                    majorBattle ? RFWarLedgerEventType.MajorBattleWon : RFWarLedgerEventType.BattleWon,
                    winner,
                    loser,
                    magnitude,
                    detail: $"losing_casualties={losingCasualties};starting_strength={startingStrength}");
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
        if (settlement == null || (!settlement.IsTown && !settlement.IsCastle))
        {
            return;
        }

        Kingdom? previousKingdom = oldOwner?.Clan?.Kingdom;
        Kingdom? newKingdom = newOwner?.Clan?.Kingdom;
        if (previousKingdom == null || newKingdom == null || previousKingdom == newKingdom)
        {
            return;
        }

        RFWarLedgerRecord? record = FindActiveWar(previousKingdom, newKingdom);
        if (record == null)
        {
            return;
        }

        string newKingdomId = GetKingdomId(newKingdom);
        bool retaken = record.InitialOwnerBySettlementId.TryGetValue(settlement.StringId, out string? initialOwnerId)
            && initialOwnerId == newKingdomId;
        RFWarLedgerEventType eventType = settlement.IsTown
            ? retaken ? RFWarLedgerEventType.TownRetaken : RFWarLedgerEventType.TownCaptured
            : retaken ? RFWarLedgerEventType.CastleRetaken : RFWarLedgerEventType.CastleCaptured;
        float magnitude = settlement.IsTown ? 18f : 12f;

        AddScoredEvent(
            record,
            eventType,
            newKingdom,
            previousKingdom,
            magnitude,
            settlement.StringId,
            capturerHero?.StringId ?? string.Empty,
            detail.ToString());
    }

    private void OnVillageLooted(Village village)
    {
        if (village == null)
        {
            return;
        }

        Settlement? settlement = village.Settlement;
        Kingdom? victim = settlement?.OwnerClan?.Kingdom;
        Kingdom? raider = settlement?.LastAttackerParty?.MapFaction as Kingdom;
        if (settlement == null || victim == null || raider == null || victim == raider)
        {
            return;
        }

        RFWarLedgerRecord? record = FindActiveWar(raider, victim);
        if (record == null)
        {
            return;
        }

        string raiderId = GetKingdomId(raider);
        float alreadyApplied = record.Events
            .Where(ledgerEvent => ledgerEvent.Type == RFWarLedgerEventType.VillageRaided && ledgerEvent.ActorKingdomId == raiderId)
            .Sum(ledgerEvent => Math.Abs(ledgerEvent.Delta));
        float remaining = MaximumRaidScorePerSide - alreadyApplied;
        if (remaining <= 0f)
        {
            return;
        }

        AddScoredEvent(
            record,
            RFWarLedgerEventType.VillageRaided,
            raider,
            victim,
            Math.Min(2f, remaining),
            settlement.StringId,
            detail: village.Name?.ToString() ?? settlement.StringId);
    }

    private void OnHeroPrisonerTaken(PartyBase captor, Hero prisoner)
    {
        if (prisoner == null)
        {
            return;
        }

        Kingdom? captorKingdom = captor?.MapFaction as Kingdom;
        Kingdom? prisonerKingdom = prisoner.Clan?.Kingdom;
        if (captorKingdom == null || prisonerKingdom == null || captorKingdom == prisonerKingdom)
        {
            return;
        }

        RFWarLedgerRecord? record = FindActiveWar(captorKingdom, prisonerKingdom);
        if (record == null)
        {
            return;
        }

        string heroId = prisoner.StringId ?? string.Empty;
        float currentDay = GetCurrentDay();
        bool recentlyCounted = record.Events.Any(ledgerEvent =>
            ledgerEvent.HeroId == heroId
            && (ledgerEvent.Type == RFWarLedgerEventType.NobleCaptured || ledgerEvent.Type == RFWarLedgerEventType.RulerCaptured)
            && currentDay - ledgerEvent.Day < 7f);
        if (recentlyCounted)
        {
            return;
        }

        bool isRuler = prisonerKingdom.RulingClan?.Leader == prisoner;
        AddScoredEvent(
            record,
            isRuler ? RFWarLedgerEventType.RulerCaptured : RFWarLedgerEventType.NobleCaptured,
            captorKingdom,
            prisonerKingdom,
            isRuler ? 6f : 2f,
            heroId: heroId,
            detail: prisoner.Name?.ToString() ?? heroId);
    }

    private RFWarLedgerRecord StartWar(
        Kingdom first,
        Kingdom second,
        RFWarLedgerEventType openingEvent,
        string detail)
    {
        RFWarLedgerRecord? existing = FindActiveWar(first, second);
        if (existing != null)
        {
            return existing;
        }

        string firstId = GetKingdomId(first);
        string secondId = GetKingdomId(second);
        string sideAId = string.CompareOrdinal(firstId, secondId) <= 0 ? firstId : secondId;
        string sideBId = sideAId == firstId ? secondId : firstId;
        float day = GetCurrentDay();
        string baseId = $"{sideAId}<->{sideBId}@{day.ToString("0.###", CultureInfo.InvariantCulture)}";
        string id = baseId;
        int suffix = 2;
        while (_records.Any(record => record.Id == id))
        {
            id = $"{baseId}#{suffix++}";
        }

        RFWarLedgerRecord record = new()
        {
            Id = id,
            SideAKingdomId = sideAId,
            SideBKingdomId = sideBId,
            InitiatorKingdomId = openingEvent == RFWarLedgerEventType.RecoveredActiveWar ? string.Empty : firstId,
            Motive = DetermineWarMotive(first, second, openingEvent),
            StartedAtDay = day,
            EndedAtDay = -1f,
            IsActive = true,
            Score = 0f
        };

        SnapshotInitialOwners(record, first, second);
        _records.Add(record);
        _activeByPair[GetPairKey(first, second)] = record;
        AddEvent(record, openingEvent, 0f, firstId, secondId, detail: detail);
        PruneCompletedWars();
        return record;
    }

    private void CloseWar(RFWarLedgerRecord record, Kingdom first, Kingdom second, string detail)
    {
        AddEvent(record, RFWarLedgerEventType.PeaceMade, 0f, GetKingdomId(first), GetKingdomId(second), detail: detail);
        record.IsActive = false;
        record.EndedAtDay = GetCurrentDay();
        _activeByPair.Remove(GetPairKey(first, second));
        PruneCompletedWars();
    }

    private void AddScoredEvent(
        RFWarLedgerRecord record,
        RFWarLedgerEventType eventType,
        Kingdom actor,
        Kingdom target,
        float magnitude,
        string settlementId = "",
        string heroId = "",
        string detail = "")
    {
        string actorId = GetKingdomId(actor);
        float delta = actorId == record.SideAKingdomId ? magnitude : -magnitude;
        AddEvent(record, eventType, delta, actorId, GetKingdomId(target), settlementId, heroId, detail);
    }

    private void AddEvent(
        RFWarLedgerRecord record,
        RFWarLedgerEventType eventType,
        float delta,
        string actorKingdomId,
        string targetKingdomId,
        string settlementId = "",
        string heroId = "",
        string detail = "")
    {
        record.Score = Math.Max(-100f, Math.Min(100f, record.Score + delta));
        RFWarLedgerEvent ledgerEvent = new()
        {
            Type = eventType,
            Day = GetCurrentDay(),
            Delta = delta,
            ScoreAfter = record.Score,
            ActorKingdomId = actorKingdomId ?? string.Empty,
            TargetKingdomId = targetKingdomId ?? string.Empty,
            SettlementId = settlementId ?? string.Empty,
            HeroId = heroId ?? string.Empty,
            Detail = detail ?? string.Empty
        };

        record.MutableEvents.Add(ledgerEvent);
        if (record.MutableEvents.Count > MaxEventsPerWar)
        {
            record.MutableEvents.RemoveAt(0);
        }

        RFWarSystemTraceLog.Write(
            $"ledger war={record.Id} event={eventType} actor={actorKingdomId} target={targetKingdomId} "
            + $"delta={delta:0.00} scoreA={record.Score:0.00} settlement={settlementId} hero={heroId}");
    }

    private void ReconcileActiveWars()
    {
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (!IsValidKingdom(kingdom))
            {
                continue;
            }

            foreach (Kingdom enemy in kingdom.FactionsAtWarWith.OfType<Kingdom>())
            {
                if (!IsValidKingdom(enemy) || string.CompareOrdinal(GetKingdomId(kingdom), GetKingdomId(enemy)) >= 0)
                {
                    continue;
                }

                if (FindActiveWar(kingdom, enemy) == null)
                {
                    StartWar(kingdom, enemy, RFWarLedgerEventType.RecoveredActiveWar, "recovered_active_war");
                }
            }
        }

        foreach (RFWarLedgerRecord record in _records.Where(candidate => candidate.IsActive).ToList())
        {
            Kingdom? first = ResolveKingdom(record.SideAKingdomId);
            Kingdom? second = ResolveKingdom(record.SideBKingdomId);
            if (first == null || second == null || !first.IsAtWarWith(second))
            {
                if (first != null && second != null)
                {
                    CloseWar(record, first, second, "reconciled_inactive_war");
                }
                else
                {
                    record.IsActive = false;
                    record.EndedAtDay = GetCurrentDay();
                    _activeByPair.Remove(GetPairKey(record.SideAKingdomId, record.SideBKingdomId));
                }
            }
        }
    }

    private RFWarLedgerRecord? FindActiveWar(Kingdom first, Kingdom second)
    {
        _activeByPair.TryGetValue(GetPairKey(first, second), out RFWarLedgerRecord? record);
        return record;
    }

    private static bool IsSamePair(RFWarLedgerRecord record, Kingdom first, Kingdom second)
    {
        string key = GetPairKey(first, second);
        return key == GetPairKey(record.SideAKingdomId, record.SideBKingdomId);
    }

    private static void SnapshotInitialOwners(RFWarLedgerRecord record, Kingdom first, Kingdom second)
    {
        foreach (Settlement settlement in first.Settlements.Concat(second.Settlements).Distinct())
        {
            if (settlement == null || (!settlement.IsTown && !settlement.IsCastle))
            {
                continue;
            }

            Kingdom? owner = settlement.OwnerClan?.Kingdom;
            if (owner != null)
            {
                record.InitialOwnerBySettlementId[settlement.StringId] = GetKingdomId(owner);
            }
        }
    }

    private void PruneCompletedWars()
    {
        List<RFWarLedgerRecord> completed = _records
            .Where(record => !record.IsActive)
            .OrderByDescending(record => record.EndedAtDay)
            .ToList();
        if (completed.Count <= MaxCompletedWars)
        {
            return;
        }

        foreach (RFWarLedgerRecord record in completed.Skip(MaxCompletedWars))
        {
            _records.Remove(record);
        }
    }

    private string Serialize()
    {
        StringBuilder builder = new("V2");
        foreach (RFWarLedgerRecord record in _records)
        {
            string snapshot = string.Join(";", record.InitialOwnerBySettlementId.Select(pair => $"{pair.Key}={pair.Value}"));
            builder.AppendLine();
            builder.Append("R|").Append(Encode(record.Id))
                .Append('|').Append(Encode(record.SideAKingdomId))
                .Append('|').Append(Encode(record.SideBKingdomId))
                .Append('|').Append(record.StartedAtDay.ToString("R", CultureInfo.InvariantCulture))
                .Append('|').Append(record.EndedAtDay.ToString("R", CultureInfo.InvariantCulture))
                .Append('|').Append(record.IsActive ? "1" : "0")
                .Append('|').Append(record.Score.ToString("R", CultureInfo.InvariantCulture))
                .Append('|').Append(Encode(snapshot))
                .Append('|').Append(Encode(record.InitiatorKingdomId))
                .Append('|').Append(((int)record.Motive).ToString(CultureInfo.InvariantCulture));

            foreach (RFWarLedgerEvent ledgerEvent in record.Events)
            {
                builder.AppendLine();
                builder.Append("E|").Append(Encode(record.Id))
                    .Append('|').Append((int)ledgerEvent.Type)
                    .Append('|').Append(ledgerEvent.Day.ToString("R", CultureInfo.InvariantCulture))
                    .Append('|').Append(ledgerEvent.Delta.ToString("R", CultureInfo.InvariantCulture))
                    .Append('|').Append(ledgerEvent.ScoreAfter.ToString("R", CultureInfo.InvariantCulture))
                    .Append('|').Append(Encode(ledgerEvent.ActorKingdomId))
                    .Append('|').Append(Encode(ledgerEvent.TargetKingdomId))
                    .Append('|').Append(Encode(ledgerEvent.SettlementId))
                    .Append('|').Append(Encode(ledgerEvent.HeroId))
                    .Append('|').Append(Encode(ledgerEvent.Detail));
            }
        }

        return builder.ToString();
    }

    private static List<string> SplitSerializedLedger(string serialized)
    {
        List<string> chunks = new();
        for (int offset = 0; offset < serialized.Length; offset += MaxSerializedChunkLength)
        {
            int length = Math.Min(MaxSerializedChunkLength, serialized.Length - offset);
            chunks.Add(serialized.Substring(offset, length));
        }

        if (chunks.Count == 0)
        {
            chunks.Add(string.Empty);
        }

        return chunks;
    }

    private void Deserialize(string serialized)
    {
        _records.Clear();
        _activeByPair.Clear();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return;
        }

        string[] lines = serialized.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, RFWarLedgerRecord> recordsById = new(StringComparer.Ordinal);

        foreach (string line in lines.Where(line => line.StartsWith("R|", StringComparison.Ordinal)))
        {
            string[] fields = line.Split('|');
            if (fields.Length < 9
                || !TryParseFloat(fields[4], out float startedAt)
                || !TryParseFloat(fields[5], out float endedAt)
                || !TryParseFloat(fields[7], out float score))
            {
                continue;
            }

            RFWarLedgerRecord record = new()
            {
                Id = Decode(fields[1]),
                SideAKingdomId = Decode(fields[2]),
                SideBKingdomId = Decode(fields[3]),
                StartedAtDay = startedAt,
                EndedAtDay = endedAt,
                IsActive = fields[6] == "1",
                Score = Math.Max(-100f, Math.Min(100f, score)),
                InitiatorKingdomId = fields.Length >= 10 ? Decode(fields[9]) : string.Empty,
                Motive = fields.Length >= 11
                    && int.TryParse(fields[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawMotive)
                    && Enum.IsDefined(typeof(RFWarMotive), rawMotive)
                        ? (RFWarMotive)rawMotive
                        : RFWarMotive.RecoveredConflict
            };

            DeserializeSnapshot(Decode(fields[8]), record.InitialOwnerBySettlementId);
            if (string.IsNullOrWhiteSpace(record.Id))
            {
                continue;
            }

            _records.Add(record);
            recordsById[record.Id] = record;
        }

        foreach (string line in lines.Where(line => line.StartsWith("E|", StringComparison.Ordinal)))
        {
            string[] fields = line.Split('|');
            if (fields.Length < 11
                || !recordsById.TryGetValue(Decode(fields[1]), out RFWarLedgerRecord? record)
                || !int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawType)
                || !TryParseFloat(fields[3], out float day)
                || !TryParseFloat(fields[4], out float delta)
                || !TryParseFloat(fields[5], out float scoreAfter))
            {
                continue;
            }

            record.MutableEvents.Add(new RFWarLedgerEvent
            {
                Type = Enum.IsDefined(typeof(RFWarLedgerEventType), rawType)
                    ? (RFWarLedgerEventType)rawType
                    : RFWarLedgerEventType.RecoveredActiveWar,
                Day = day,
                Delta = delta,
                ScoreAfter = scoreAfter,
                ActorKingdomId = Decode(fields[6]),
                TargetKingdomId = Decode(fields[7]),
                SettlementId = Decode(fields[8]),
                HeroId = Decode(fields[9]),
                Detail = Decode(fields[10])
            });
        }

        foreach (RFWarLedgerRecord record in _records.Where(record => record.IsActive))
        {
            _activeByPair[GetPairKey(record.SideAKingdomId, record.SideBKingdomId)] = record;
        }

        PruneCompletedWars();
    }

    private static void DeserializeSnapshot(string serialized, Dictionary<string, string> target)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return;
        }

        foreach (string entry in serialized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = entry.IndexOf('=');
            if (separator > 0 && separator < entry.Length - 1)
            {
                target[entry.Substring(0, separator)] = entry.Substring(separator + 1);
            }
        }
    }

    private static RFWarMotive DetermineWarMotive(
        Kingdom attacker,
        Kingdom defender,
        RFWarLedgerEventType openingEvent)
    {
        if (openingEvent == RFWarLedgerEventType.RecoveredActiveWar)
        {
            return RFWarMotive.RecoveredConflict;
        }

        RFWarSpecialRequestType? specialType = RFWarSpecialAuthorityBehavior.GetPendingWarType(attacker, defender);
        if (specialType.HasValue)
        {
            return specialType.Value switch
            {
                RFWarSpecialRequestType.HolyWar => RFWarMotive.HolyWar,
                RFWarSpecialRequestType.CollectiveDefense => RFWarMotive.CollectiveDefense,
                RFWarSpecialRequestType.AlignmentWar => RFWarMotive.AlignmentWar,
                RFWarSpecialRequestType.MercenaryContract => RFWarMotive.MercenaryContract,
                RFWarSpecialRequestType.StrategicIntrigue => RFWarMotive.StrategicIntrigue,
                RFWarSpecialRequestType.EnduringRivalry => RFWarMotive.EnduringRivalry,
                RFWarSpecialRequestType.GrandDesign => RFWarMotive.GrandDesign,
                _ => RFWarMotive.Unknown
            };
        }

        return RFWarStrategicIntent.GetWarObjective(attacker, defender) switch
        {
            RFWarObjectiveType.SurvivalDefense => RFWarMotive.DefensiveWar,
            RFWarObjectiveType.Reconquest => RFWarMotive.Reconquest,
            RFWarObjectiveType.HolyWar => RFWarMotive.HolyWar,
            RFWarObjectiveType.PunitiveRaid => RFWarMotive.PunitiveWar,
            RFWarObjectiveType.Expansion => RFWarMotive.Expansion,
            RFWarObjectiveType.Containment => RFWarMotive.Containment,
            _ => RFWarMotive.Unknown
        };
    }

    private static string Encode(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static string Decode(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

    private static bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    private static List<Kingdom> GetKingdomsOnSide(MapEventSide side)
    {
        return side.Parties
            .Select(party => party?.Party?.MapFaction)
            .OfType<Kingdom>()
            .Where(IsValidKingdom)
            .Distinct()
            .ToList();
    }

    private static int CountCasualties(MapEventSide side)
    {
        return side.Parties.Sum(party =>
        {
            int startingHealthy = Math.Max(0, party?.HealthyManCountAtStart ?? 0);
            int remainingHealthy = Math.Max(0, party?.Party?.NumberOfHealthyMembers ?? 0);
            return Math.Max(0, startingHealthy - remainingHealthy);
        });
    }

    private static int CountStartingHealthyTroops(MapEventSide side)
    {
        return side.Parties.Sum(party => Math.Max(0, party?.HealthyManCountAtStart ?? 0));
    }

    private static Kingdom? ResolveKingdom(string kingdomId)
    {
        return Kingdom.All.FirstOrDefault(kingdom => GetKingdomId(kingdom) == kingdomId);
    }

    private static bool IsValidKingdom(Kingdom kingdom)
    {
        return kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(GetKingdomId(kingdom));
    }

    private static string GetPairKey(Kingdom first, Kingdom second)
    {
        return GetPairKey(GetKingdomId(first), GetKingdomId(second));
    }

    private static string GetPairKey(string firstId, string secondId)
    {
        return string.CompareOrdinal(firstId, secondId) <= 0
            ? $"{firstId}<->{secondId}"
            : $"{secondId}<->{firstId}";
    }

    private static string GetKingdomId(Kingdom kingdom)
    {
        if (kingdom == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
    }

    private static float GetCurrentDay()
    {
        return (float)CampaignTime.Now.ToDays;
    }
}
