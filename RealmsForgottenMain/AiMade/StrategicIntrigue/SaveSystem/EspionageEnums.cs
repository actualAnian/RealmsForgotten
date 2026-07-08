namespace RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;

public enum EspionageOperationType
{
    ClanInfiltration = 0,
    CourtListening = 1
}

public enum EspionageOperationStatus
{
    Pending = 0,
    Succeeded = 1,
    Partial = 2,
    Exposed = 3,
    Failed = 4
}

public enum EspionageReportConfidence
{
    Low = 0,
    Medium = 1,
    High = 2
}
