using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace RealmsForgotten.WorldState.YoungWorld
{
    /// <summary>
    /// Shared stage/cap math for the wall-stage tier gates (recruitment AND
    /// troop upgrades). The cap is the military stage of a KINGDOM, read off
    /// its wall development (majority level 1 → tiers 1-2; majority level 2 →
    /// up to tier 4; two or more level-3 towns → no cap). Author's decision
    /// 2026-07-24: kingdom level governs both recruitment (settlement's realm)
    /// and upgrades (party's realm); factionless parties are never capped.
    /// The IEF ±1 adjustment applies when enabled. Cached per campaign day.
    /// </summary>
    internal static class RFWorldStageCap
    {
        public const int NoCap = 0;
        public const int PalisadeCap = 2; // majority wall level 1: tiers 1-2 only
        public const int KeepCap = 4;     // majority wall level 2: up to tier 4

        private static readonly Dictionary<string, int> KingdomCaps = new(StringComparer.Ordinal);
        private static int _cacheDay = int.MinValue;

        public static bool IsAboveCap(int cap, int tier)
        {
            return cap != NoCap && tier > cap;
        }

        public static int GetKingdomCap(Kingdom? kingdom)
        {
            if (kingdom == null)
            {
                return NoCap;
            }

            RefreshCacheDay();
            string key = string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
            if (KingdomCaps.TryGetValue(key, out int cached))
            {
                return cached;
            }

            int cap = ComputeStageCap(kingdom);
            if (cap != NoCap && RFWorldSettings.IefMilitary)
            {
                int ief = RFFactionEconomyIndex.GetIef(kingdom);
                if (ief >= 70)
                {
                    cap += 1;
                }
                else if (ief <= 40)
                {
                    cap = Math.Max(1, cap - 1);
                }
            }

            KingdomCaps[key] = cap;
            return cap;
        }

        private static void RefreshCacheDay()
        {
            int day = (int)CampaignTime.Now.ToDays;
            if (day != _cacheDay)
            {
                _cacheDay = day;
                KingdomCaps.Clear();
            }
        }

        private static int ComputeStageCap(Kingdom kingdom)
        {
            int townsAtLevel3 = 0;
            int total = 0;
            int[] levelCounts = new int[4]; // wall levels 0..3

            foreach (Town fief in kingdom.Fiefs)
            {
                Settlement? settlement = fief?.Settlement;
                if (settlement == null || settlement.Town == null || !(settlement.IsTown || settlement.IsCastle))
                {
                    continue;
                }

                int wallLevel = fief.GetWallLevel();
                if (wallLevel < 0)
                {
                    wallLevel = 0;
                }
                else if (wallLevel > 3)
                {
                    wallLevel = 3;
                }

                levelCounts[wallLevel]++;
                total++;
                if (settlement.IsTown && wallLevel >= 3)
                {
                    townsAtLevel3++;
                }
            }

            if (townsAtLevel3 >= 2 || total == 0)
            {
                return NoCap;
            }

            int modeLevel = 1;
            int modeCount = -1;
            for (int level = 1; level <= 3; level++)
            {
                if (levelCounts[level] > modeCount)
                {
                    modeCount = levelCounts[level];
                    modeLevel = level;
                }
            }

            return modeLevel switch
            {
                1 => PalisadeCap,
                2 => KeepCap,
                _ => NoCap,
            };
        }
    }
}
