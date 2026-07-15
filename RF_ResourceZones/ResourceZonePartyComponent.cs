using RF_Settlers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RF_ResourceZones
{
    /// <summary>
    /// The stationary party representing a resource zone on the map: its
    /// MemberRoster is the garrison, its ItemRoster the production stockpile.
    /// Ownership, tier and stored gold live in the ResourceZoneRecord held by
    /// ResourceZonesCampaignBehavior; the component only carries identity, so a
    /// destroyed party can always be rebuilt from the record.
    /// Implements IRFStationaryCampParty: tent map icon with the owner's banner
    /// and a game-menu encounter, both provided by RF_Settlers' patches.
    /// </summary>
    public class ResourceZonePartyComponent : PartyComponent, IRFStationaryCampParty
    {
        [SaveableField(1)]
        private string _zoneId = string.Empty;

        [SaveableField(2)]
        private string _displayName = string.Empty;

        [SaveableField(3)]
        private int _zoneType;

        public ResourceZonePartyComponent(string zoneId, string displayName, ResourceZoneType type)
        {
            _zoneId = zoneId;
            _displayName = displayName;
            _zoneType = (int)type;
        }

        public string ZoneId => _zoneId;

        public ResourceZoneType ZoneType => (ResourceZoneType)_zoneType;

        string IRFStationaryCampParty.EncounterMenuId => "rf_resource_zone";

        /// <summary>Vanilla village-type map icon per resource: iron/silver
        /// mine diggings, lumberjack camp... resolved lazily (VillageTypes only
        /// exist once the campaign is up). Gold reuses the silver-mine icon;
        /// charcoal burners use the clay-pit kilns. Null falls back to the tent.</summary>
        string? IRFStationaryCampParty.MapIconMeshName
        {
            get
            {
                if (Campaign.Current == null)
                {
                    return null;
                }

                return (ResourceZoneType)_zoneType switch
                {
                    ResourceZoneType.Iron => DefaultVillageTypes.IronMine?.MeshName,
                    ResourceZoneType.Karthradium => DefaultVillageTypes.IronMine?.MeshName,
                    ResourceZoneType.Silver => DefaultVillageTypes.SilverMine?.MeshName,
                    ResourceZoneType.Gold => DefaultVillageTypes.SilverMine?.MeshName,
                    ResourceZoneType.Wood => DefaultVillageTypes.Lumberjack?.MeshName,
                    ResourceZoneType.Charcoal => DefaultVillageTypes.ClayMine?.MeshName,
                    _ => null,
                };
            }
        }

        public Clan? OwnerClan =>
            ResourceZonesCampaignBehavior.Instance?.GetRecord(_zoneId)?.OwnerClan;

        public override Hero? PartyOwner => OwnerClan?.Leader;

        public override Hero? Leader => null;

        public override Settlement? HomeSettlement => OwnerClan?.HomeSettlement;

        public override TextObject Name
        {
            get
            {
                TextObject name = new("{=rf_zone_party_name}{ZONE_NAME}");
                name.SetTextVariable("ZONE_NAME", string.IsNullOrEmpty(_displayName) ? _zoneId : _displayName);
                return name;
            }
        }

        public override Banner? GetDefaultComponentBanner()
        {
            return OwnerClan?.Banner ?? MobileParty?.ActualClan?.Banner;
        }
    }
}
