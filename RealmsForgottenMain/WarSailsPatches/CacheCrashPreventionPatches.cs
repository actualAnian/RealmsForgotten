using HarmonyLib;
using NavalDLC.GameComponents;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

// Crash prevention for NavalDLCBanditDensityModel.IsPositionInsideNavalSafeZone
// GetClosestEntranceToFace returns null Item1 on custom map faces with no reachable naval settlement,
// causing NullReferenceException both in GetDistance(item, ...) and item.IsVillage (1.4beta).
// Called from NavalDLCMobilePartyAIModel.ShouldConsiderAttacking during every AI tick.
// NOTE: This patch is applied manually from SubModule.RunWarSailsPatches
public class NavalDLCBanditDensityModel_IsPositionInsideNavalSafeZone_Patch
{
    public static bool Prefix(CampaignVec2 position, ref bool __result)
    {
        try
        {
            __result = false;

            if (!position.IsValid() || position.IsOnLand)
                return false;

            Settlement item = Campaign.Current.Models.MapDistanceModel
                .GetClosestEntranceToFace(position.Face, MobileParty.NavigationType.Naval)
                .Item1;

            if (item == null)
            {
                InformationManager.DisplayMessage(new($"[NavalDLC SafeZone] GetClosestEntranceToFace returned NULL for position ({position.X}, {position.Y}) - no reachable naval settlement"));
                return false; // skip original, __result = false
            }

            // item is valid — let original method run safely
            return true;
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new($"[NavalDLC SafeZone] Unexpected error in prefix: {ex.Message}"));
            __result = false;
            return false;
        }
    }
}