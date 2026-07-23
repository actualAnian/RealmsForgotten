using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RF_ResourceZones
{
    /// <summary>
    /// A small caravan hauling one zone's production batch to its bound town.
    /// Attackable on the road — losing it loses the goods, which IS the
    /// economic-warfare gameplay. On arrival the goods are sold into the town
    /// market and the revenue goes to the zone's owner clan.
    /// </summary>
    // Extends CaravanPartyComponent (protected ctor) instead of raw
    // PartyComponent so the engine treats it as a real caravan: IsCaravan is
    // computed as "_partyComponent is CaravanPartyComponent" (map icon = laden
    // caravan mule, not a foot troop), and every vanilla site that dereferences
    // party.CaravanPartyComponent gets a non-null instance.
    public class ResourceCaravanPartyComponent : CaravanPartyComponent, RF_Settlers.IRFSelfDrivenParty
    {
        [SaveableField(1)]
        private string _zoneId = string.Empty;

        [SaveableField(2)]
        private Settlement? _targetTown;

        [SaveableField(3)]
        private string _zoneName = string.Empty;

        public ResourceCaravanPartyComponent(string zoneId, string zoneName, Settlement targetTown)
            : base(targetTown, null, null, isElite: false, null)
        {
            _zoneId = zoneId;
            _zoneName = zoneName;
            _targetTown = targetTown;
        }

        public string ZoneId => _zoneId;

        public Settlement? TargetTown => _targetTown;

        public Clan? OwnerClan =>
            ResourceZonesCampaignBehavior.Instance?.GetRecord(_zoneId)?.OwnerClan;

        public override Hero? PartyOwner => OwnerClan?.Leader;

        public override Hero? Leader => null;

        public override Settlement? HomeSettlement => _targetTown;

        public override TextObject Name
        {
            get
            {
                TextObject name = new("{=rf_zone_caravan_name}{ZONE_NAME} Caravan");
                name.SetTextVariable("ZONE_NAME", string.IsNullOrEmpty(_zoneName) ? _zoneId : _zoneName);
                return name;
            }
        }

        public override Banner? GetDefaultComponentBanner()
        {
            return OwnerClan?.Banner ?? MobileParty?.ActualClan?.Banner;
        }

        // Zone caravans have no owner Hero (Owner is always null): the vanilla
        // CaravanPartyComponent lifecycle dereferences Owner unguarded —
        // OnInitialize does Owner.OwnedCaravans.Add(this) (NRE on every save
        // load), OnFinalize the matching Remove, and OnMobilePartySetOnCreation
        // reads Owner.Clan. Re-implement the ownerless halves ourselves.
        protected override void OnInitialize()
        {
            if (Owner != null)
            {
                base.OnInitialize();
            }
        }

        protected override void OnFinalize()
        {
            if (Owner != null)
            {
                base.OnFinalize();
            }
        }

        protected override void OnMobilePartySetOnCreation()
        {
            if (Owner != null)
            {
                base.OnMobilePartySetOnCreation();
                return;
            }
            MobileParty.Aggressiveness = 0f;
            MobileParty.ActualClan = OwnerClan;
            MobileParty.Party.SetVisualAsDirty();
        }
    }
}
