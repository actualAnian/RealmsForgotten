using HarmonyLib;

namespace RF_Enlistment;

internal static class RFEnlistmentBootstrap
{
    private static bool _initialized;
    private static readonly Harmony Harmony = new("RealmsForgotten.RF_Enlistment");

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        RFEnlistmentDebug.Clear();
        RFEnlistmentDebug.Log("RF_Enlistment bootstrap initialized.");
        Harmony.PatchAll(typeof(RFEnlistmentBootstrap).Assembly);
    }
}
