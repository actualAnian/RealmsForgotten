using System;
using HarmonyLib;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace RF_Settlers.Patches
{
    /// <summary>
    /// Stationary camp-style parties (settler camps, resource zones) are
    /// MobileParties, so vanilla would draw them as walking figures. This
    /// prefix reroutes their map icon:
    /// - components exposing MapIconMeshName INSTANTIATE that village-type
    ///   PREFAB (iron_mine, silver_mine, lumberjack, clay_mine — the same
    ///   prefabs the settlement visual assembles from child parts) plus the
    ///   owner's banner flag;
    /// - the rest get the base game's siege-camp TENT (banner included).
    /// Applied LATE via RF_Settlers.RunManualPatches — never at module load
    /// (the folded-character lesson).
    /// </summary>
    public static class SettlerCampVisualPatch
    {
        public static bool Prefix(
            MobilePartyVisual __instance,
            PartyBase party,
            ref bool clearBannerComponentCache,
            ref bool clearBannerEntityCache)
        {
            if (party?.MobileParty?.PartyComponent is not IRFStationaryCampParty camp)
            {
                return true;
            }

            try
            {
                string? prefabName = camp.MapIconMeshName;
                if (!string.IsNullOrEmpty(prefabName)
                    && TryBuildIcon(__instance.StrategicEntity, party, prefabName!))
                {
                    return false;
                }

                __instance.AddTentEntityForParty(__instance.StrategicEntity, party, ref clearBannerComponentCache);
                return false;
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_Settlers] Camp icon visual failed, falling back to default icon: {exception}");
                return true;
            }
        }

        /// <summary>Instantiates the village-type PREFAB (which assembles its
        /// child part meshes, exactly like the icons seen inside towns) and
        /// plants the owner clan's banner on top. Returns false (→ tent) if the
        /// prefab can't be instantiated.</summary>
        private static bool TryBuildIcon(GameEntity strategicEntity, PartyBase party, string prefabName)
        {
            GameEntity? entity = GameEntity.Instantiate(strategicEntity.Scene, prefabName, true, true, "");
            if (entity == null)
            {
                Debug.Print($"[RF_Settlers] Map icon prefab '{prefabName}' not instantiable; using the tent instead.");
                return false;
            }

            // DON'T override the frame: the author's prefab carries its own
            // tuned transform (scale + local origin 0). AddChild(..., false)
            // uses that as the LOCAL frame → the icon sits at the party position
            // at the size the author set in RF_Map/Prefabs/mine_icons.xml.
            TryPlantOwnerBanner(entity, party);

            strategicEntity.AddChild(entity, false);
            entity.SetVisibilityExcludeParents(true);
            return true;
        }

        private static void TryPlantOwnerBanner(GameEntity entity, PartyBase party)
        {
            try
            {
                Banner? banner = party.MobileParty?.ActualClan?.Banner
                                 ?? party.MobileParty?.PartyComponent?.GetDefaultComponentBanner();
                if (banner == null)
                {
                    return;
                }

                MetaMesh? bannerMesh = MobilePartyVisual.GetBannerOfCharacter(banner, "campaign_flag");
                if (bannerMesh == null)
                {
                    return;
                }

                MatrixFrame frame = MatrixFrame.Identity;
                frame.origin.z += 0.15f;
                frame.rotation.RotateAboutUp(MathF.PI / 2f);
                frame.rotation.ApplyScaleLocal(0.4f);
                bannerMesh.Frame = frame;
                entity.AddMultiMesh(bannerMesh, true);
            }
            catch (Exception exception)
            {
                // An icon without a flag is acceptable; a crashed map visual is not.
                Debug.Print($"[RF_Settlers] Owner banner on camp icon failed (harmless): {exception.Message}");
            }
        }
    }
}
