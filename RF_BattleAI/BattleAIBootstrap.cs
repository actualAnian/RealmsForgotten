using System.Reflection;
using HarmonyLib;

namespace RF_BattleAI;

public static class BattleAIBootstrap
{
    private static readonly Harmony Harmony = new("RealmsForgotten.BattleAI");

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        Harmony.PatchAll(Assembly.GetExecutingAssembly());
        _initialized = true;
    }
}
