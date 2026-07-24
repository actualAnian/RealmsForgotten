namespace RF_BattleAI;

/// <summary>
/// Log switches for the battle AI diagnostics, pushed from RealmsForgotten's
/// RF Diagnostics MCM page (RFLogSwitchboard.PushToModules). All default OFF,
/// so a player who never opens the diagnostics page writes nothing to disk.
/// </summary>
public static class BattleAILogSwitches
{
    public static bool RuntimeTrace;
    public static bool AdaptiveMemory;
    public static bool Tactics;
    public static bool BanditTrap;
}
