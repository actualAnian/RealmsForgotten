using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace RF_Settlers
{
    /// <summary>
    /// Picks a deliberate VillageType for a new settler village based on the
    /// TERRAIN at the founding position (fishing near water, mines in the
    /// mountains, date farms in the desert...), instead of inheriting whatever
    /// random donor village was cloned. All ids are vanilla types actually used
    /// by RF_Map's own villages; every candidate is validated against the
    /// object manager at runtime, so a missing type can never produce a broken
    /// village — the caller falls back to the donor's type.
    /// </summary>
    public static class SettlerVillageTypePicker
    {
        // Weighted by repetition. Ids verified against RF_Map settlements.xml.
        private static readonly Dictionary<TerrainType, string[]> CandidatesByTerrain = new()
        {
            [TerrainType.Plain] = new[] { "wheat_farm", "wheat_farm", "wheat_farm", "cattle_farm", "cattle_farm", "sheep_farm", "europe_horse_ranch", "flax_plant" },
            [TerrainType.Steppe] = new[] { "steppe_horse_ranch", "steppe_horse_ranch", "sheep_farm", "sheep_farm", "cattle_farm" },
            [TerrainType.Desert] = new[] { "date_farm", "date_farm", "desert_horse_ranch", "salt_mine" },
            [TerrainType.Dune] = new[] { "date_farm", "desert_horse_ranch", "salt_mine" },
            [TerrainType.Forest] = new[] { "lumberjack", "lumberjack", "swine_farm", "trapper" },
            [TerrainType.Snow] = new[] { "trapper", "trapper", "lumberjack", "sturgian_horse_ranch" },
            [TerrainType.Mountain] = new[] { "iron_mine", "iron_mine", "clay_mine", "silver_mine", "salt_mine" },
            [TerrainType.Canyon] = new[] { "iron_mine", "clay_mine", "silver_mine" },
            [TerrainType.Swamp] = new[] { "flax_plant", "swine_farm" },
        };

        private static readonly string[] DefaultCandidates = { "wheat_farm", "cattle_farm", "sheep_farm" };

        private const string FishermanId = "fisherman";
        private const float WaterProbeRadius = 4f;

        /// <summary>
        /// Returns a valid VillageType stringId for the position, or null if
        /// none of the candidates resolve (caller keeps the donor's type).
        /// </summary>
        public static string Pick(Vec2 position)
        {
            try
            {
                // Coastal/lakeside spots become fishing villages most of the time.
                if (IsNearWater(position) && Resolve(FishermanId) != null && MBRandom.RandomFloat < 0.75f)
                {
                    return FishermanId;
                }

                TerrainType terrain = GetTerrain(position);
                string[] pool = CandidatesByTerrain.TryGetValue(terrain, out string[] candidates)
                    ? candidates
                    : DefaultCandidates;

                // Random weighted pick, skipping ids that don't exist in this setup.
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    string id = pool[MBRandom.RandomInt(pool.Length)];
                    if (Resolve(id) != null)
                    {
                        return id;
                    }
                }

                foreach (string id in DefaultCandidates)
                {
                    if (Resolve(id) != null)
                    {
                        return id;
                    }
                }
            }
            catch (Exception exception)
            {
                SettlersLog.Write($"SettlerVillageTypePicker.Pick failed: {exception.Message}");
            }

            return null;
        }

        public static VillageType Resolve(string villageTypeId)
        {
            if (string.IsNullOrEmpty(villageTypeId))
            {
                return null;
            }

            return MBObjectManager.Instance?.GetObject<VillageType>(villageTypeId);
        }

        private static TerrainType GetTerrain(Vec2 position)
        {
            var wrapper = Campaign.Current?.MapSceneWrapper;
            if (wrapper == null)
            {
                return TerrainType.Plain;
            }

            var face = wrapper.GetFaceIndex(new CampaignVec2(position, isOnLand: true));
            return face.IsValid() ? wrapper.GetFaceTerrainType(face) : TerrainType.Plain;
        }

        private static bool IsNearWater(Vec2 position)
        {
            var wrapper = Campaign.Current?.MapSceneWrapper;
            if (wrapper == null)
            {
                return false;
            }

            for (int i = 0; i < 8; i++)
            {
                float angle = i * (MathF.PI * 2f / 8f);
                Vec2 probe = position + new Vec2(MathF.Cos(angle), MathF.Sin(angle)) * WaterProbeRadius;
                var face = wrapper.GetFaceIndex(new CampaignVec2(probe, isOnLand: false));
                if (!face.IsValid())
                {
                    continue;
                }
                TerrainType terrain = wrapper.GetFaceTerrainType(face);
                if (terrain == TerrainType.Water || terrain == TerrainType.Lake
                    || terrain == TerrainType.River || terrain == TerrainType.CoastalSea
                    || terrain == TerrainType.OpenSea || terrain == TerrainType.Fording)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
