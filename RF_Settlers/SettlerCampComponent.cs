using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RF_Settlers
{
    /// <summary>
    /// A stationary settler camp: the tent phase of a future village. Grows in
    /// population over time; once mature it is ready to be converted into a
    /// real settlement (phase C — Player Settlement XML-injection technique).
    /// Defended by its villager roster in a plain field battle when attacked.
    /// </summary>
    public class SettlerCampComponent : PartyComponent
    {
        [SaveableField(1)]
        private Kingdom _kingdom;

        [SaveableField(2)]
        private Settlement _originTown;

        [SaveableField(3)]
        private float _population;

        [SaveableField(4)]
        private CampaignTime _foundedTime;

        [SaveableField(5)]
        private bool _matureAnnounced;

        public SettlerCampComponent(Kingdom kingdom, Settlement originTown, float initialPopulation)
        {
            _kingdom = kingdom;
            _originTown = originTown;
            _population = initialPopulation;
            _foundedTime = CampaignTime.Now;
        }

        public Kingdom Kingdom => _kingdom;

        public Settlement OriginTown => _originTown;

        public CampaignTime FoundedTime => _foundedTime;

        public float Population
        {
            get => _population;
            set => _population = value;
        }

        public bool MatureAnnounced
        {
            get => _matureAnnounced;
            set => _matureAnnounced = value;
        }

        public override Hero PartyOwner => _kingdom?.Leader;

        public override Hero Leader => null;

        public override Settlement HomeSettlement => _originTown;

        public override TextObject Name
        {
            get
            {
                TextObject name = new("{=rf_settler_camp_name}{CULTURE} Settler Camp");
                name.SetTextVariable("CULTURE", _kingdom?.Culture?.Name ?? new TextObject("{=rf_settler_unknown}Wandering"));
                return name;
            }
        }

        public override Banner GetDefaultComponentBanner()
        {
            return _kingdom?.Banner ?? MobileParty?.ActualClan?.Banner;
        }
    }
}
