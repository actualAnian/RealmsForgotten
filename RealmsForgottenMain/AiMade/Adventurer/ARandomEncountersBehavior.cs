using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;



namespace RealmsForgotten.AiMade.Adventurer
{
    public class ARandomEncountersBehavior : CampaignBehaviorBase
    {
        private readonly Random _rng = new Random();

        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != MobileParty.MainParty) return;
            if (!settlement.IsTown) return;
            if (_rng.NextDouble() > 0.2) return; // 20% chance

            Hero localLord = settlement.HeroesWithoutParty
                .FirstOrDefault(h => h.IsLord && h != Hero.MainHero);

            if (localLord != null)
            {
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Hostile Encounter",
                        $"You run into {localLord.Name} in the streets. He seems aggressive.",
                        true, true,
                        "Confront Him",
                        "Walk Away",
                        () => StartDuel(localLord),
                        null
                    )
                );
            }
        }

        private void StartDuel(Hero opponent)
        {
            if (opponent == null || opponent.CharacterObject == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Could not start duel: opponent is invalid."));
                return;
            }

            InformationManager.DisplayMessage(new InformationMessage("⚔ Duel initiated with " + opponent.Name));

            MissionState.OpenNew(
                "DuelMission",
                new MissionInitializerRecord("arena_duel"),
                mission => new MissionBehavior[]
                {
                    new MissionOptionsComponent(),
                    new SimpleDuelMissionLogic(opponent)
                }
            );
        }
    }
}