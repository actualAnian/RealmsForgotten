using System;
using TaleWorlds.CampaignSystem;

namespace RF_warsystem.Logic;

internal readonly struct RFWarStrategicProfile
{
    public RFWarStrategicProfile(
        float offensiveDrive,
        float defensiveDiscipline,
        float siegePreference,
        float raidPreference,
        float homeGuardBias,
        float caution,
        float persistence,
        float coalitionLoyalty,
        float revengeBias,
        float sacredZeal,
        float opportunism,
        float frontierParanoia,
        float siegePatience,
        float deepStrikeBias)
    {
        OffensiveDrive = offensiveDrive;
        DefensiveDiscipline = defensiveDiscipline;
        SiegePreference = siegePreference;
        RaidPreference = raidPreference;
        HomeGuardBias = homeGuardBias;
        Caution = caution;
        Persistence = persistence;
        CoalitionLoyalty = coalitionLoyalty;
        RevengeBias = revengeBias;
        SacredZeal = sacredZeal;
        Opportunism = opportunism;
        FrontierParanoia = frontierParanoia;
        SiegePatience = siegePatience;
        DeepStrikeBias = deepStrikeBias;
    }

    public float OffensiveDrive { get; }
    public float DefensiveDiscipline { get; }
    public float SiegePreference { get; }
    public float RaidPreference { get; }
    public float HomeGuardBias { get; }
    public float Caution { get; }
    public float Persistence { get; }
    public float CoalitionLoyalty { get; }
    public float RevengeBias { get; }
    public float SacredZeal { get; }
    public float Opportunism { get; }
    public float FrontierParanoia { get; }
    public float SiegePatience { get; }
    public float DeepStrikeBias { get; }
}

internal static class RFWarStrategicProfiles
{
    private static readonly RFWarStrategicProfile DefaultProfile = new(
        offensiveDrive: 0.15f,
        defensiveDiscipline: 0.15f,
        siegePreference: 0.1f,
        raidPreference: 0f,
        homeGuardBias: 0.1f,
        caution: 0.1f,
        persistence: 0.1f,
        coalitionLoyalty: 0.1f,
        revengeBias: 0.1f,
        sacredZeal: 0.05f,
        opportunism: 0.1f,
        frontierParanoia: 0.1f,
        siegePatience: 0.1f,
        deepStrikeBias: 0f);

    public static RFWarStrategicProfile Get(Kingdom kingdom)
    {
        string cultureId = kingdom.Culture?.StringId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cultureId))
        {
            return DefaultProfile;
        }

        return cultureId.ToLowerInvariant() switch
        {
            "khuzait" => new RFWarStrategicProfile(0.52f, -0.08f, -0.28f, 0.82f, -0.12f, 0.02f, 0.18f, 0.08f, 0.16f, 0.04f, 0.52f, -0.08f, -0.22f, 0.75f),
            "aserai" => new RFWarStrategicProfile(0.3f, 0.04f, 0.18f, 0.46f, 0.08f, 0.16f, 0.2f, 0.42f, 0.14f, 0.18f, 0.28f, 0.14f, 0.12f, 0.38f),
            "vlandia" => new RFWarStrategicProfile(0.32f, 0.08f, 0.42f, -0.08f, 0.04f, 0.02f, 0.34f, 0.18f, 0.18f, 0.08f, 0.3f, 0.04f, 0.34f, -0.06f),
            "empire" => new RFWarStrategicProfile(0.24f, 0.24f, 0.46f, -0.16f, 0.14f, 0.14f, 0.28f, 0.3f, 0.18f, 0.1f, 0.18f, 0.18f, 0.38f, -0.08f),
            "south_realm" => new RFWarStrategicProfile(0.34f, 0.12f, 0.4f, -0.04f, 0.08f, 0.08f, 0.3f, 0.34f, 0.16f, 0.12f, 0.3f, 0.12f, 0.28f, 0.08f),
            "north_realm" => new RFWarStrategicProfile(0.2f, 0.28f, 0.22f, 0.04f, 0.24f, 0.18f, 0.24f, 0.24f, 0.18f, 0.08f, 0.16f, 0.34f, 0.18f, 0.02f),
            "west_realm" => new RFWarStrategicProfile(0.18f, 0.28f, 0.44f, -0.12f, 0.18f, 0.2f, 0.26f, 0.36f, 0.14f, 0.08f, 0.14f, 0.2f, 0.4f, -0.1f),
            "battania" => new RFWarStrategicProfile(0.04f, 0.48f, -0.14f, 0.22f, 0.38f, 0.28f, 0.12f, 0.12f, 0.08f, 0.04f, 0.12f, 0.42f, -0.08f, 0.32f),
            "sturgia" => new RFWarStrategicProfile(0.18f, 0.38f, 0.14f, 0.08f, 0.32f, 0.18f, 0.24f, 0.14f, 0.32f, 0.08f, 0.08f, 0.38f, 0.16f, 0.06f),
            "dwarf" => new RFWarStrategicProfile(0.04f, 0.58f, 0.18f, -0.24f, 0.5f, 0.34f, 0.24f, 0.42f, 0.12f, 0.12f, 0.04f, 0.46f, 0.5f, -0.18f),
            "grimwatch" => new RFWarStrategicProfile(0.08f, 0.54f, 0.14f, -0.22f, 0.48f, 0.34f, 0.26f, 0.44f, 0.14f, 0.14f, 0.04f, 0.44f, 0.46f, -0.18f),
            "wulf" => new RFWarStrategicProfile(0.52f, -0.08f, 0.16f, 0.28f, -0.08f, -0.06f, 0.42f, 0.04f, 0.42f, 0.18f, 0.34f, 0.04f, 0.04f, 0.12f),
            "urkhai" => new RFWarStrategicProfile(0.62f, -0.12f, 0.12f, 0.42f, -0.12f, -0.12f, 0.38f, 0.04f, 0.52f, 0.28f, 0.38f, -0.02f, -0.08f, 0.3f),
            "giant" => new RFWarStrategicProfile(0.38f, 0.08f, 0.24f, 0.02f, 0.08f, 0.08f, 0.18f, 0.12f, 0.12f, 0.04f, 0.08f, 0.12f, 0.14f, -0.02f),
            "mage" => new RFWarStrategicProfile(0.14f, 0.28f, 0.28f, -0.18f, 0.22f, 0.26f, 0.18f, 0.5f, 0.08f, 0.24f, 0.18f, 0.16f, 0.28f, 0.08f),
            _ => DefaultProfile
        };
    }
}
