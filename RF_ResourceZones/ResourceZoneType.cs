using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace RF_ResourceZones
{
    public enum ResourceZoneType
    {
        Gold = 0,
        Iron = 1,
        Wood = 2,
        Charcoal = 3,
        Silver = 4,
        /// <summary>The dwarves' sacred metal (item "kardrathium", registered
        /// at runtime by RFSmithing). Mined only in the dwarf homelands.</summary>
        Karthradium = 5,
        Salt = 6,
        Clay = 7,
    }

    /// <summary>
    /// Central balance table for the zones. All economy knobs live here so
    /// tuning never requires touching the behavior.
    /// </summary>
    public static class ResourceZoneRules
    {
        public const int MaxTier = 3;

        // Garrison caps per tier — the zone's MemberRoster is its garrison.
        // A built fortification adds a flat +50 (walls to man).
        public static int GarrisonCap(int tier, bool hasFortification = false)
        {
            int baseCap = tier switch { 1 => 25, 2 => 45, _ => 80 };
            return hasFortification ? baseCap + 50 : baseCap;
        }

        // ── Fortification construction (player-owned zones) ──────────────────
        public const int FortificationGoldCost = 25000;
        public const int FortificationWoodCost = 120;
        public const int FortificationToolsCost = 30;
        public const int FortificationBuildDays = 12;

        // Fresh garrison strength for a newly spawned/captured zone.
        public const int InitialBanditGarrison = 18;
        public const int PostCaptureGarrison = 6;

        // AI/bandit owners slowly refill their garrison (men per day).
        public const int GarrisonRegenPerDay = 2;

        // Production per day, in ITEM UNITS, per tier (Gold produces denars).
        public static int UnitsPerDay(ResourceZoneType type, int tier)
        {
            int baseUnits = type switch
            {
                ResourceZoneType.Gold => 120,     // denars/day, tier 1
                ResourceZoneType.Iron => 6,
                ResourceZoneType.Wood => 8,
                ResourceZoneType.Charcoal => 7,
                ResourceZoneType.Silver => 3,
                ResourceZoneType.Karthradium => 2, // rare — but each load is worth ~300
                ResourceZoneType.Salt => 5,
                ResourceZoneType.Clay => 8,
                _ => 0,
            };
            return baseUnits * tier;
        }

        // Days a plundered deposit lies abandoned before brigands re-occupy it
        // (like a raided village's recovery window).
        public const int PlunderedIdleDays = 10;

        // A caravan departs when the stockpile reaches this many units.
        public static int CaravanLoad(int tier) => 25 * tier;

        // Minimum days between caravans, so tier-3 zones don't flood the road.
        public const float CaravanCooldownDays = 2f;

        // Upgrade costs: denars + hardwood units, paid at the zone menu.
        public static int UpgradeGoldCost(int currentTier) => currentTier == 1 ? 8000 : 20000;
        public static int UpgradeWoodCost(int currentTier) => currentTier == 1 ? 40 : 100;

        // ── Deposit richness (rolled once per zone, saved) ───────────────────
        // 1 = poor vein, 2 = steady vein, 3 = rich vein. Richness scales the
        // daily yield AND the reserve, and shortens the recovery downtime —
        // rich mines are strictly more worth fighting over.

        public static int RollRichness()
        {
            float roll = MBRandom.RandomFloat;
            if (roll < 0.30f) return 1;
            if (roll < 0.75f) return 2;
            return 3;
        }

        public static float RichnessYieldMultiplier(int richness) =>
            richness switch { 1 => 0.6f, 3 => 1.6f, _ => 1f };

        /// <summary>Reserve capacity in units (denars for gold): how much the
        /// deposit holds before it is exhausted. Measured in days of tier-1
        /// work — higher tiers dig the same vein FASTER, so upgrading a poor
        /// mine burns it out sooner (a real decision).</summary>
        public static int ReserveCapacity(ResourceZoneType type, int richness)
        {
            int daysOfWork = richness switch { 1 => 20, 3 => 55, _ => 35 };
            float yield = UnitsPerDay(type, 1) * RichnessYieldMultiplier(richness);
            return Math.Max(1, (int)(yield * daysOfWork));
        }

        /// <summary>Downtime (days) before an exhausted deposit reopens.</summary>
        public static int RecoveryDays(int richness) =>
            richness switch { 1 => 12, 3 => 5, _ => 8 };

        public static string RichnessLabel(int richness) =>
            richness switch { 1 => "poor vein", 3 => "rich vein", _ => "steady vein" };

        /// <summary>Derelict map prefab shown while a plundered deposit lies
        /// abandoned — the author's single "mine_icon_burned" prefab
        /// (RF_Map/Prefabs/mine_icons.xml, burned_wood_a mesh).</summary>
        public static string? BurnedPrefab(ResourceZoneType type)
        {
            return "mine_icon_burned";
        }

        /// <summary>Trade item produced by the type; null for Gold (direct denars)
        /// and for items missing from the current game data (logged, skipped).</summary>
        public static ItemObject? ProducedItem(ResourceZoneType type)
        {
            switch (type)
            {
                case ResourceZoneType.Iron:
                    return DefaultItems.IronOre;
                case ResourceZoneType.Wood:
                    return DefaultItems.HardWood;
                case ResourceZoneType.Charcoal:
                    return DefaultItems.Charcoal;
                case ResourceZoneType.Silver:
                    // Not part of DefaultItems; resolve by id with a safe miss.
                    return MBObjectManager.Instance?.GetObject<ItemObject>("silver");
                case ResourceZoneType.Karthradium:
                    // Registered at runtime by RFSmithing (RFItems.RegisterAll);
                    // note the item id spelling differs from the type name.
                    return MBObjectManager.Instance?.GetObject<ItemObject>("kardrathium");
                case ResourceZoneType.Salt:
                    return MBObjectManager.Instance?.GetObject<ItemObject>("salt");
                case ResourceZoneType.Clay:
                    return MBObjectManager.Instance?.GetObject<ItemObject>("clay");
                default:
                    return null;
            }
        }
    }
}
