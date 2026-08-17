using RealmsForgotten.Career;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.Module1.Religions
{
    public class RecruitPrisonersMissionBehavior : CampaignBehaviorBase
    {
        private readonly TextObject _missionTitle = new TextObject("Recruit Prisoners for Conversion");
        private readonly TextObject _missionDescription = new TextObject("Capture bandits, recruit them, and bring them to the priest for conversion.");

        private bool _missionAccepted;
        private Settlement _targetSettlement;
        private int _requiredPrisoners;
        private int _convertedPrisoners;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.OnMainPartyPrisonerRecruitedEvent.AddNonSerializedListener(this, OnPrisonerRecruited);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);

            // Listen to the BanditConverted event
            BanditConversionManager.BanditConverted += OnBanditConverted;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_missionAccepted", ref _missionAccepted);
            dataStore.SyncData("_targetSettlement", ref _targetSettlement);
            dataStore.SyncData("_requiredPrisoners", ref _requiredPrisoners);
            dataStore.SyncData("_convertedPrisoners", ref _convertedPrisoners);
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            // Add any initialization logic here
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            // Add any loading logic here
        }

        private void HourlyTick()
        {
            if (!_missionAccepted || _targetSettlement == null)
                return;

            // Check if the player has enough prisoners recruited
            if (MobileParty.MainParty.PrisonRoster.TotalRegulars >= _requiredPrisoners)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Mission Complete",
                    $"You have recruited {_requiredPrisoners} prisoners. Bring them to {_targetSettlement.Name} for conversion.",
                    true,
                    false,
                    "OK",
                    null,
                    null,
                    null
                ));
            }
        }

        private void OnPrisonerRecruited(FlattenedTroopRoster roster)
        {
            if (!_missionAccepted)
                return;

            foreach (var troop in roster.Troops)
            {
                if (troop.IsBandit())
                {
                    _convertedPrisoners++;
                    InformationManager.DisplayMessage(new InformationMessage($"Recruited prisoner: {troop.Name}. Total converted: {_convertedPrisoners}/{_requiredPrisoners}"));

                    if (_convertedPrisoners >= _requiredPrisoners)
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "Mission Complete",
                            "You have recruited enough prisoners. Bring them to the priest for conversion.",
                            true,
                            false,
                            "OK",
                            null,
                            null,
                            null
                        ));
                    }
                }
            }
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (!_missionAccepted || settlement != _targetSettlement || party != MobileParty.MainParty)
                return;

            // Convert the prisoners
            if (MobileParty.MainParty.PrisonRoster.TotalRegulars >= _requiredPrisoners)
            {
                _missionAccepted = false;
                _convertedPrisoners = 0;
                for (int i = 0; i < _requiredPrisoners; i++)
                {
                    MobileParty.MainParty.PrisonRoster.RemoveTroop(MobileParty.MainParty.PrisonRoster.GetCharacterAtIndex(0));
                }
                InformationManager.DisplayMessage(new InformationMessage("Prisoners converted by the priest. Mission complete!"));
                PietyManager.AddPiety(Hero.MainHero, 50, true); // Adjust the piety reward as needed
            }
        }

        private void OnBanditConverted(object sender, BanditConversionEvent e)
        {
            if (!_missionAccepted || e.Hero != Hero.MainHero)
                return;

            _convertedPrisoners += e.BanditCount;
            InformationManager.DisplayMessage(new InformationMessage($"Converted bandits: {e.BanditCount}. Total converted: {_convertedPrisoners}/{_requiredPrisoners}"));

            if (_convertedPrisoners >= _requiredPrisoners)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Mission Complete",
                    "You have recruited enough prisoners. Bring them to the priest for conversion.",
                    true,
                    false,
                    "OK",
                    null,
                    null,
                    null
                ));
            }
        }

        public void StartMission(Settlement targetSettlement, int requiredPrisoners)
        {
            _missionAccepted = true;
            _targetSettlement = targetSettlement;
            _requiredPrisoners = requiredPrisoners;
            _convertedPrisoners = 0;

            InformationManager.DisplayMessage(new InformationMessage($"Mission started: Capture and recruit {requiredPrisoners} bandits, then bring them to {_targetSettlement.Name} for conversion."));
        }
    }

    public static class BanditExtensions
    {
        public static bool IsBandit(this CharacterObject character)
        {
            return character.Occupation == Occupation.Bandit;
        }
    }
}