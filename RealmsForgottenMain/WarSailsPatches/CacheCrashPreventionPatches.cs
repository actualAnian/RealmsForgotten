using HarmonyLib;
using NavalDLC.GameComponents;
using RealmsForgotten.AiMade;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RealmsForgotten.WarSailsPatches
{
    public class NavalDLCBanditDensityModel_IsPositionInsideNavalSafeZone_Patch
    {
        public static bool TryApply(Harmony harmony)
        {
            var target = AccessTools.Method(
                typeof(NavalDLCBanditDensityModel),
                nameof(NavalDLCBanditDensityModel.IsPositionInsideNavalSafeZone),
                new[] { typeof(CampaignVec2) });

            if (target == null)
            {
                RFLogger.Log("[Lifecycle] Optional naval patch skipped | IsPositionInsideNavalSafeZone target not found.");
                return false;
            }

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(NavalDLCBanditDensityModel_IsPositionInsideNavalSafeZone_Patch), nameof(Prefix)));
            return true;
        }

        public static bool Prefix(CampaignVec2 position, ref bool __result)
        {
            try
            {
                __result = false;

                if (!position.IsValid() || position.IsOnLand || Campaign.Current?.Models?.MapDistanceModel == null)
                    return false;

                Settlement item = Campaign.Current.Models.MapDistanceModel
                    .GetClosestEntranceToFace(position.Face, MobileParty.NavigationType.Naval)
                    .Item1;

                if (item == null)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[Lifecycle] Optional naval patch failed inside IsPositionInsideNavalSafeZone prefix. Falling back to safe false. error={ex}");
                __result = false;
                return false;
            }
        }
    }
}
