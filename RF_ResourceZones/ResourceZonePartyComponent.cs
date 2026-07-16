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

        /// <summary>Map-icon PREFAB name per resource — the author's custom
        /// prefabs in RF_Map/Prefabs/mine_icons.xml (built from the vanilla
        /// mi_*_mine map-icon meshes), instantiated via GameEntity.Instantiate.
        /// Null → tent fallback.</summary>
        string? IRFStationaryCampParty.MapIconMeshName
        {
            get
            {
                // A built fortification overrides the mine icon with a
                // stronghold prefab (the visual patch falls back to the tent if
                // the prefab can't be instantiated).
                if (ResourceZonesCampaignBehavior.Instance?.GetRecord(_zoneId)?.HasFortification == true)
                {
                    return "battania_castle_keep";
                }

                return (ResourceZoneType)_zoneType switch
                {
                    ResourceZoneType.Iron => "mine_icon_iron",
                    ResourceZoneType.Karthradium => "mine_icon_iron",
                    ResourceZoneType.Silver => "map_icons_production_gold",
                    ResourceZoneType.Gold => "map_icons_production_gold",
                    ResourceZoneType.Wood => "mine_icon_wood",
                    ResourceZoneType.Charcoal => "mine_icon_charcoal",
                    ResourceZoneType.Salt => "mine_icon_saltmine",
                    ResourceZoneType.Clay => "mine_icons_clay",
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
