using HarmonyLib;
using RealmsForgotten.AiMade;
using System;

namespace RealmsForgotten.WarSailsPatches
{
    internal static class OptionalNavalStartupPatchBootstrap
    {
        internal static void Apply(Harmony harmony)
        {
            TryApply("InitializePirateSpawnPointsPatch", () => InitializePirateSpawnPointsPatch.TryApply(harmony));
            TryApply("NavalDLCBanditDensityModel_IsPositionInsideNavalSafeZone_Patch", () => NavalDLCBanditDensityModel_IsPositionInsideNavalSafeZone_Patch.TryApply(harmony));
            TryApply("NavalDLCMapDistanceModel_GetDistance_GuardPatch", () => NavalDLCMapDistanceModel_GetDistance_GuardPatch.TryApply(harmony));
            TryApply("NavalDLCMapDistanceModel_GetPortToGateDistanceForSettlement_GuardPatch", () => NavalDLCMapDistanceModel_GetPortToGateDistanceForSettlement_GuardPatch.TryApply(harmony));
        }

        private static void TryApply(string patchName, Func<bool> apply)
        {
            try
            {
                bool applied = apply();
                RFLogger.Log(applied
                    ? $"[Lifecycle] Optional naval patch applied | {patchName}"
                    : $"[Lifecycle] Optional naval patch skipped | {patchName}");
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[Lifecycle] Optional naval patch failed | {patchName} | error={ex}");
            }
        }
    }
}
