namespace RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;

public enum IntriguePactGoal
{
    UndermineRuler = 0,
    BreakAwayFromKingdom = 1,
    SupportFutureClaimant = 2,
    PrepareProtectedVassalage = 3
}

public enum IntrigueOperationType
{
    RumorCampaign = 0,
    SponsorDissidence = 1,
    PrepareBreakaway = 2
}

public enum IntrigueOperationStatus
{
    Pending = 0,
    Resolved = 1,
    Exposed = 2,
    Cancelled = 3
}

public enum IntrigueAllianceObjective
{
    BackClaimant = 0,
    BackBreakaway = 1,
    ForeignIntervention = 2
}

public enum IntrigueAllianceRewardType
{
    None = 0,
    PromisedSettlement = 1
}

public enum KingdomObjectiveType
{
    None = 0,
    CrushBattanianResistance = 1,
    NobleWealthSupremacy = 2,
    PreserveBattanianHomelands = 3,
    UniteAseraiRealms = 4,
    ClaimImperialLegitimacy = 5,
    ForgeBorderEmpire = 6,
    ArcaneFrontier = 7,
    SecureMountainHolds = 8,
    DefileMountainHolds = 9,
    MartialGlory = 10,
    UnbreakableRealm = 11
}
