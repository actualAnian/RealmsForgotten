namespace RealmsForgotten.HuntableHerds
{
    /// <summary>
    /// Central tuning knobs for the hunting system. Everything a designer is likely to want to
    /// change lives here (or in hunting_herds.xml, for per-herd values).
    /// </summary>
    public class Settings
    {
        private static Settings? _instance;
        public static Settings Instance
        {
            get
            {
                _instance ??= new Settings();
                return _instance;
            }
        }

        // ---------------------------------------------------------------------------------------
        // CAMPAIGN MAP: how often a herd is spotted
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Chance, rolled once per day while the main party is outside a settlement, of spotting a
        /// herd. 0f disables hunting entirely. Effective cadence is this chance ANDed with
        /// <see cref="MinDaysBetweenHerdSpottings"/>, so 0.2f + 4 days averages roughly one
        /// notification every 9 days.
        /// </summary>
        public float DailyChanceOfSpottingHerd { get; } = 0.2f;

        /// <summary>Hard floor, in whole days, between two herd-spotting notifications.</summary>
        public float MinDaysBetweenHerdSpottings { get; } = 4f;

        /// <summary>Extra days added on top of <see cref="MinDaysBetweenHerdSpottings"/> once the player actually accepts a hunt.</summary>
        public float ExtraCooldownDaysAfterHunt { get; } = 3f;

        /// <summary>
        /// Opt-in flavour filter. When true, only herds whose &lt;terrain&gt; list contains the terrain
        /// the main party is standing on can be spotted (herds with no &lt;terrain&gt; entries match
        /// everywhere, and if nothing matches at all the pick falls back to the full pool).
        /// <para>
        /// Default is false on purpose: hunting_herds.xml does not yet cover every terrain type, so
        /// turning it on narrows the pool hard in deserts/snow. Flip it to true once the
        /// &lt;terrain&gt; lists cover the map.
        /// </para>
        /// </summary>
        public bool FilterHerdsByTerrain { get; } = true;

        // ---------------------------------------------------------------------------------------
        // MISSION: looting
        // ---------------------------------------------------------------------------------------

        /// <summary>Player must be crouched (default Z) to field-dress a carcass.</summary>
        public bool CrouchNeededEnabled { get; } = true;

        /// <summary>Radius, in meters, of the legacy "press Q to skin everything nearby" convenience sweep.</summary>
        public float AreaLootRadius { get; } = 10f;

        // ---------------------------------------------------------------------------------------
        // MISSION: spawning
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Safety cap on how many herd animals may be alive at once, regardless of what
        /// &lt;totalAmountInHerd&gt; says. Some XML entries ask for 100-1000 animals, which would
        /// stall the mission; this clamps them.
        /// </summary>
        public int MaxAliveAnimalsPerHunt { get; } = 40;

        // ---------------------------------------------------------------------------------------
        // MISSION: aggressive animal behaviour
        // ---------------------------------------------------------------------------------------

        /// <summary>How long (seconds) an animal keeps chasing after it loses sight of the player.</summary>
        public float AggroDurationSeconds { get; } = 15f;

        /// <summary>Field-of-view half-angle (radians) used when an animal checks whether it can see the player. ~2.2 rad is a wide but not omniscient cone.</summary>
        public float SightConeHalfAngle { get; } = 2.2f;

        /// <summary>Extra meters beyond &lt;hitboxRange&gt; at which an animal starts its wind-up.</summary>
        public float TelegraphExtraRange { get; } = 2.5f;

        /// <summary>Duration (seconds) of the visible/audible wind-up before the blow lands.</summary>
        public float TelegraphDurationSeconds { get; } = 0.65f;

        /// <summary>Speed multiplier applied while charging in during the wind-up.</summary>
        public float ChargeSpeedMultiplier { get; } = 1.3f;

        /// <summary>Duration (seconds) of the short backwards recoil after a hit. Keep it small: this is a step back, not a retreat.</summary>
        public float RecoilDurationSeconds { get; } = 1.1f;

        /// <summary>Minimum distance (meters) of the post-hit recoil.</summary>
        public float RecoilMinDistance { get; } = 3f;

        /// <summary>Maximum distance (meters) of the post-hit recoil.</summary>
        public float RecoilMaxDistance { get; } = 8f;

        /// <summary>Seconds an animal must wait between two blows.</summary>
        public float AttackCooldownSeconds { get; } = 2.2f;

        /// <summary>Seconds between AI re-targets while chasing. Lower = tighter pursuit, higher = cheaper.</summary>
        public float PursueRepathInterval { get; } = 0.35f;

        /// <summary>When an aggressive animal is hurt it becomes aggroed even if it never saw the player (so bow shots provoke a charge).</summary>
        public bool AggressiveAnimalsRetaliateWhenShot { get; } = true;
    }
}
