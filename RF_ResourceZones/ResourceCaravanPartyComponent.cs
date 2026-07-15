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
    public class ResourceCaravanPartyComponent : PartyComponent, RF_Settlers.IRFSelfDrivenParty
    {
        [SaveableField(1)]
        private string _zoneId = string.Empty;

        [SaveableField(2)]
        private Settlement? _targetTown;

        [SaveableField(3)]
        private string _zoneName = string.Empty;

        public ResourceCaravanPartyComponent(string zoneId, string zoneName, Settlement targetTown)
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
    }
}
