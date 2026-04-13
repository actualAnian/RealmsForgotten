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
