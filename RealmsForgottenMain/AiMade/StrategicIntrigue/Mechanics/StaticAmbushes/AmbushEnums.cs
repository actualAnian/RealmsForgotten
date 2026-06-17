using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.StaticAmbushes;

public enum AmbushZoneShape
{
    Box = 0,
    Circle = 1
}

public enum AmbushConcealmentType
{
    Forest = 0,
    HillReverseSlope = 1,
    Brush = 2,
    Mixed = 3
}

public enum AmbushTriggerMode
{
    Manual = 0,
    EnemyEntersZone = 1,
    EnemyNearZone = 2,
    TimedAutoReveal = 3
}

public enum AmbushRevealState
{
    Ready = 0,
    Compromised = 1,
    Triggered = 2,
    Spent = 3
}

public enum AmbushAllowedSide
{
    Attacker = 0,
    Defender = 1,
    Both = 2
}
