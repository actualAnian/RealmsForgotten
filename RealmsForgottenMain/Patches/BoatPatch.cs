using HarmonyLib;
using SandBox;
using SandBox.View.Map;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace RealmsForgotten.Patches;

[HarmonyPatch]
public class Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapScreen), "StepSounds")]
    private static bool Prefix1(MobileParty party) => Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace) != TerrainType.Water;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapScene), "DisableUnwalkableNavigationMeshes")]
    public static void Postfix1(MapScene __instance) => __instance.Scene.SetAbilityOfFacesWithId(MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.Water), true);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapScene), "GetHeightAtPoint")]
    public static void Postfix2(MapScene __instance, ref float height) => height = MathF.Max(height, __instance.Scene.GetWaterLevel());

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PartyVisual), "TickMobilePartyVisual")]
    private static void Postfix3(PartyVisual __instance)
    {
        GameEntity strategicEntity = __instance.StrategicEntity;

        if (Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(__instance.Position) == TerrainType.Water && strategicEntity.ChildCount == 0)
        {
            MatrixFrame identity = MatrixFrame.Identity;
            GameEntity gameEntity = GameEntity.CreateEmpty(strategicEntity.Scene, true);
            string metaMeshName = "boat_sail_on"/* (default mesh) */, bannerKey = __instance.PartyBase.LeaderHero?.ClanBanner?.Serialize(), bannerMeshName = "campaign_flag";

            identity.rotation.ApplyScaleLocal(0.25f);// You can change the scale of the mesh.
            gameEntity.SetFrame(ref identity);
            gameEntity.AddMultiMesh(MetaMesh.GetCopy(metaMeshName, true, false), true);

            if (!string.IsNullOrEmpty(bannerKey))
            {
                try
                {
                    gameEntity.AddMultiMesh((MetaMesh)AccessTools.Method(typeof(PartyVisual), "GetBannerOfCharacter").Invoke(null, new object[] { new Banner(bannerKey), bannerMeshName }), true);
                }
                catch (Exception)
                {
                    InformationManager.DisplayMessage(new InformationMessage(MethodBase.GetCurrentMethod().DeclaringType.FullName + "." + MethodBase.GetCurrentMethod().Name + ": Error adding banner to ship visual of " + __instance.PartyBase.Name + "!"));
                }
            }

            strategicEntity.AddChild(gameEntity, false);

            __instance.HumanAgentVisuals?.Reset();
            __instance.MountAgentVisuals?.Reset();
            __instance.CaravanMountAgentVisuals?.Reset();

            AccessTools.Property(typeof(PartyVisual), "HumanAgentVisuals").SetValue(__instance, null);
            AccessTools.Property(typeof(PartyVisual), "MountAgentVisuals").SetValue(__instance, null);
            AccessTools.Property(typeof(PartyVisual), "CaravanMountAgentVisuals").SetValue(__instance, null);
        }
        else if (Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(__instance.Position) != TerrainType.Water && strategicEntity.ChildCount > 0)
        {
            __instance.PartyBase.SetVisualAsDirty();
        }
    }
}
