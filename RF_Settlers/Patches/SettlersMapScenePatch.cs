using System;
using System.Collections.Generic;
using HarmonyLib;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace RF_Settlers.Patches
{
    /// <summary>
    /// Map visual for runtime-created villages. When a settlement has no
    /// hand-placed entity in the map scene, vanilla calls
    /// MapScene.AddNewEntityToMapScene(settlementId, position) and tries to
    /// instantiate a PREFAB named after the settlement id — which cannot exist
    /// for our dynamic ids. This prefix redirects registered settler-village
    /// ids to a real map-icon prefab (culture variant first, generic fallback)
    /// and snaps it to the terrain, exactly like the Player Settlement mod.
    /// </summary>
    [HarmonyPatch(typeof(MapScene), nameof(MapScene.AddNewEntityToMapScene))]
    public static class SettlersMapScenePatch
    {
        private const string GenericPrefab = "rf_settler_village_icon";

        private static readonly Dictionary<string, string> PrefabByEntityId = new();

        /// <summary>Called when a record is created and again on every early
        /// load, so the mapping exists before the map scene initializes.</summary>
        public static void RegisterVillagePrefab(string entityId, string prefabId)
        {
            if (!string.IsNullOrEmpty(entityId))
            {
                PrefabByEntityId[entityId] = string.IsNullOrEmpty(prefabId) ? GenericPrefab : prefabId;
            }
        }

        private static bool Prefix(Scene ____scene, string entityId, CampaignVec2 position)
        {
            if (entityId == null || !PrefabByEntityId.TryGetValue(entityId, out string prefabId))
            {
                return true;
            }

            try
            {
                GameEntity entity = GameEntity.Instantiate(____scene, prefabId, true, true, "");
                if (entity == null && prefabId != GenericPrefab)
                {
                    entity = GameEntity.Instantiate(____scene, GenericPrefab, true, true, "");
                }

                if (entity == null)
                {
                    // No prefab available — let vanilla try (and fail visibly),
                    // so the problem shows up instead of hiding.
                    return true;
                }

                entity.Name = entityId;
                Vec3 worldPosition = position.AsVec3();
                worldPosition.z = ____scene.GetGroundHeightAtPosition(worldPosition);
                entity.SetLocalPosition(worldPosition);
                return false;
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_Settlers] Map icon instantiation failed for {entityId}: {exception}");
                return true;
            }
        }
    }
}
