using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RF_ResourceZones
{
    /// <summary>
    /// Authoritative saved state of one zone. The map party is a projection of
    /// this record and can always be rebuilt from it (settlers' record
    /// philosophy). Manifest data (position, name, type) is NOT duplicated
    /// here — it is re-read from content each session, so slot positions can be
    /// re-authored without breaking saves.
    /// </summary>
    public class ResourceZoneRecord
    {
        [SaveableField(1)]
        public string ZoneId = string.Empty;

        [SaveableField(2)]
        public Clan? OwnerClan;

        [SaveableField(3)]
        public int Tier = 1;

        [SaveableField(4)]
        public int StoredGold;

        [SaveableField(5)]
        public float LastCaravanDay = -1000f;

        [SaveableField(6)]
        public string PartyId = string.Empty;

        [SaveableField(7)]
        public string ActiveCaravanPartyId = string.Empty;

        /// <summary>Days of uninterrupted AI ownership, counting toward the
        /// owner's next tier upgrade (ResourceZoneAmbitionBehavior). Resets on
        /// capture and for player/bandit owners.</summary>
        [SaveableField(8)]
        public int UpgradeProgressDays;

        /// <summary>Deposit richness (1 poor / 2 steady / 3 rich). 0 = not yet
        /// rolled — rolled lazily so zones from older saves get one too.</summary>
        [SaveableField(9)]
        public int Richness;

        /// <summary>Units left in the deposit before it is exhausted.</summary>
        [SaveableField(10)]
        public int ReserveUnits;

        /// <summary>Campaign day when an exhausted deposit reopens (0 = producing).</summary>
        [SaveableField(11)]
        public float ExhaustedUntilDay;
    }
}
