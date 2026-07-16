using System;
using System.Collections.Generic;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace RF_ResourceZones
{
    /// <summary>
    /// Renders the burned/derelict icon for a plundered mine while it lies
    /// abandoned — a standalone GameEntity on the campaign map (NOT a party, so
    /// it can't be auto-destroyed the way a 0-troop party would be). This is the
    /// same idiom a raided village uses: a persistent map entity whose mesh
    /// shows the "_burned" state. Runtime-only (never saved); rebuilt on load
    /// from the record's IdleUntilDay.
    /// </summary>
    public static class ResourceZoneIdleVisuals
    {
        private static readonly Dictionary<string, GameEntity> Entities =
            new(StringComparer.Ordinal);

        private static Scene? MapScene
        {
            get
            {
                try { return ((MapScene)Campaign.Current.MapSceneWrapper).Scene; }
                catch { return null; }
            }
        }

        /// <summary>Shows the derelict icon at the zone position. Idempotent.</summary>
        public static void Show(string zoneId, ResourceZoneType type, Vec2 position)
        {
            if (string.IsNullOrEmpty(zoneId) || Entities.ContainsKey(zoneId))
            {
                return;
            }

            string? prefab = ResourceZoneRules.BurnedPrefab(type);
            Scene? scene = MapScene;
            if (string.IsNullOrEmpty(prefab) || scene == null)
            {
                return;
            }

            try
            {
                GameEntity? entity = GameEntity.Instantiate(scene, prefab, true, true, "");
                if (entity == null)
                {
                    return;
                }

                // Preserve the author's prefab transform (its tuned scale lives
                // in the frame's rotation matrix) — only move it to the world
                // position on the ground.
                MatrixFrame frame = entity.GetFrame();
                Vec3 world = new(position.x, position.y, 0f);
                world.z = scene.GetGroundHeightAtPosition(world);
                frame.origin = world;
                entity.SetFrame(ref frame, true);
                entity.SetVisibilityExcludeParents(true);

                Entities[zoneId] = entity;
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_ResourceZones] Idle icon for '{zoneId}' failed (harmless): {exception.Message}");
            }
        }

        /// <summary>Removes the derelict icon (brigands re-occupied, or cleanup).</summary>
        public static void Hide(string zoneId)
        {
            if (Entities.TryGetValue(zoneId, out GameEntity? entity))
            {
                Entities.Remove(zoneId);
                try { entity?.Remove(0); } catch { }
            }
        }

        /// <summary>Drops all references (session teardown) — the scene owns the
        /// entities and disposes them, so we only clear the map here.</summary>
        public static void ClearAll()
        {
            foreach (GameEntity entity in Entities.Values)
            {
                try { entity?.Remove(0); } catch { }
            }
            Entities.Clear();
        }
    }
}
