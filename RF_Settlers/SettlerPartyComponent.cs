using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RF_Settlers
{
    /// <summary>
    /// A faction's colonist caravan: civilians travelling from a home town to a
    /// frontier spot where they will pitch a settler camp. Managed by
    /// SettlersCampaignBehavior.
    /// </summary>
    public class SettlerPartyComponent : PartyComponent
    {
        [SaveableField(1)]
        private Kingdom _kingdom;

        [SaveableField(2)]
        private Settlement _originTown;

        [SaveableField(3)]
        private CampaignVec2 _targetPosition;

        public SettlerPartyComponent(Kingdom kingdom, Settlement originTown, CampaignVec2 targetPosition)
        {
            _kingdom = kingdom;
            _originTown = originTown;
            _targetPosition = targetPosition;
        }

        public Kingdom Kingdom => _kingdom;

        public Settlement OriginTown => _originTown;

        public CampaignVec2 TargetPosition => _targetPosition;

        public override Hero PartyOwner => _kingdom?.Leader;

        public override Hero Leader => null;

        public override bool AvoidHostileActions => true;

        public override Settlement HomeSettlement => _originTown;

        public override TextObject Name
        {
            get
            {
                TextObject name = new("{=rf_settler_party_name}{KINGDOM} Settlers");
                name.SetTextVariable("KINGDOM", _kingdom?.Name ?? new TextObject("{=rf_settler_unknown}Wandering"));
                return name;
            }
        }

        public override Banner GetDefaultComponentBanner()
        {
            return _kingdom?.Banner ?? MobileParty?.ActualClan?.Banner;
        }
    }
}
