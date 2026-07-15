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
    /// - components exposing MapIconMeshName render that STATIC mesh (a vanilla
    ///   village-type mesh: iron_mine, silver_mine, lumberjack, clay_mine...)
    ///   plus the owner's banner flag;
    /// - the rest get the base game's siege-camp TENT (banner included).
    /// The entity is built exactly like vanilla AddTentEntityForParty (correct
    /// CreateEmpty/AddChild signatures + SetVisibilityExcludeParents), only the
    /// mesh name swaps. Applied LATE via RF_Settlers.RunManualPatches — never at
    /// module load (the folded-character lesson).
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
                string? meshName = camp.MapIconMeshName;
                if (!string.IsNullOrEmpty(meshName)
                    && TryBuildIcon(__instance.StrategicEntity, party, meshName!))
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

        /// <summary>Builds the map icon entity the same way vanilla builds the
        /// siege-camp tent, swapping in <paramref name="meshName"/> and planting
        /// the owner clan's banner on top. Returns false (→ tent fallback) if the
        /// mesh can't be loaded.</summary>
        private static bool TryBuildIcon(GameEntity strategicEntity, PartyBase party, string meshName)
        {
            MetaMesh? iconMesh = MetaMesh.GetCopy(meshName, true, false);
            if (iconMesh == null)
            {
                Debug.Print($"[RF_Settlers] Map icon mesh '{meshName}' not loadable; using the tent instead.");
                return false;
            }

            GameEntity entity = GameEntity.CreateEmpty(strategicEntity.Scene, true, true, true);
            entity.AddMultiMesh(iconMesh, true);

            MatrixFrame frame = MatrixFrame.Identity;
            frame.rotation.ApplyScaleLocal(1.0f);
            entity.SetFrame(ref frame, true);

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
