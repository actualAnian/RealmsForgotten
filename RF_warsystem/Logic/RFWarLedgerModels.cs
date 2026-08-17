using System.Collections.Generic;
using System.Linq;

namespace RF_warsystem.Logic;

public enum RFWarMotive
{
    Unknown = 0,
    RecoveredConflict = 1,
    DefensiveWar = 2,
    Reconquest = 3,
    HolyWar = 4,
    PunitiveWar = 5,
    Expansion = 6,
    Containment = 7,
    GrandDesign = 8,
    StrategicIntrigue = 9,
    MercenaryContract = 10,
    AlignmentWar = 11,
    CollectiveDefense = 12,
    EnduringRivalry = 13
}

public enum RFWarLedgerEventType
{
    WarDeclared = 0,
    RecoveredActiveWar = 1,
    BattleWon = 2,
    MajorBattleWon = 3,
    TownCaptured = 4,
    CastleCaptured = 5,
    TownRetaken = 6,
    CastleRetaken = 7,
    VillageRaided = 8,
    NobleCaptured = 9,
    RulerCaptured = 10,
    PeaceMade = 11,
    ObjectiveSelected = 12
}

public sealed class RFWarLedgerEvent
{
    public RFWarLedgerEventType Type { get; internal set; }
    public float Day { get; internal set; }
    public float Delta { get; internal set; }
    public float ScoreAfter { get; internal set; }
    public string ActorKingdomId { get; internal set; } = string.Empty;
    public string TargetKingdomId { get; internal set; } = string.Empty;
    public string SettlementId { get; internal set; } = string.Empty;
    public string HeroId { get; internal set; } = string.Empty;
    public string Detail { get; internal set; } = string.Empty;
}

public sealed class RFWarLedgerRecord
{
    internal readonly Dictionary<string, string> InitialOwnerBySettlementId = new();
    internal readonly List<RFWarLedgerEvent> MutableEvents = new();

    public string Id { get; internal set; } = string.Empty;
    public string SideAKingdomId { get; internal set; } = string.Empty;
    public string SideBKingdomId { get; internal set; } = string.Empty;
    public string InitiatorKingdomId { get; internal set; } = string.Empty;
    public RFWarMotive Motive { get; internal set; }
    public float StartedAtDay { get; internal set; }
    public float EndedAtDay { get; internal set; } = -1f;
    public bool IsActive { get; internal set; }
    public float Score { get; internal set; }
    public IReadOnlyList<RFWarLedgerEvent> Events => MutableEvents;

    public float GetComponentScoreFor(string kingdomId, params RFWarLedgerEventType[] eventTypes)
    {
        if (eventTypes == null || eventTypes.Length == 0)
        {
            return 0f;
        }

        float sideAScore = MutableEvents
            .Where(ledgerEvent => eventTypes.Contains(ledgerEvent.Type))
            .Sum(ledgerEvent => ledgerEvent.Delta);
        return kingdomId == SideAKingdomId
            ? sideAScore
            : kingdomId == SideBKingdomId ? -sideAScore : 0f;
    }

    public float GetScoreFor(string kingdomId)
    {
        if (kingdomId == SideAKingdomId)
        {
            return Score;
        }

        if (kingdomId == SideBKingdomId)
        {
            return -Score;
        }

        return 0f;
    }
}
