using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.WorldState.Refugees
{
    /// <summary>
    /// Wandering band of villagers displaced by a completed village raid.
    /// Neutral to everyone (AvoidHostileActions), hunted by bandits like any
    /// civilian party. Spawned and managed by RefugeeCampaignBehavior.
    /// </summary>
    public class RefugeePartyComponent : PartyComponent
    {
        [SaveableField(1)]
        private Settlement _homeVillage;

        [SaveableField(2)]
        private CampaignTime _createdTime;

        public RefugeePartyComponent(Settlement homeVillage)
        {
            _homeVillage = homeVillage;
            _createdTime = CampaignTime.Now;
        }

        public Settlement HomeVillage => _homeVillage;

        public CampaignTime CreatedTime => _createdTime;

        public override Hero PartyOwner => null;

        public override Hero Leader => null;

        public override bool AvoidHostileActions => true;

        public override Settlement HomeSettlement => _homeVillage;

        public override TextObject Name
        {
            get
            {
                TextObject name = new("{=rf_refugee_party_name}Refugees of {VILLAGE}");
                name.SetTextVariable("VILLAGE", _homeVillage?.Name ?? new TextObject("{=rf_refugee_unknown_home}a razed village"));
                return name;
            }
        }

        public override Banner GetDefaultComponentBanner()
        {
            return MobileParty?.ActualClan?.Banner;
        }
    }
}
