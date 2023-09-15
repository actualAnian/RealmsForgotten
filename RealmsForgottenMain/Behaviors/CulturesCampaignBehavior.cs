using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Behaviors
{
    internal class CulturesCampaignBehavior : CampaignBehaviorBase
    {
        private readonly string sturgiaId = "sturgia";
        private readonly string khuzaitId = "khuzait";

        private List<string> partiesInForests = new();
        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, SturgianBonus);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, KhuzaitBonus);

        }
        private void KhuzaitBonus(Settlement settlement)
        {
            if ((settlement.IsTown || settlement.IsCastle) && settlement.Owner.Culture.StringId == khuzaitId && settlement.Party.PrisonRoster.TotalRegulars > 0 && settlement.MilitiaPartyComponent != null)
            {
                foreach (FlattenedTroopRosterElement troopRosterElement in settlement.Party.PrisonRoster.ToFlattenedRoster())
                {
                    if (MBRandom.RandomFloat < 0.15f)
                    {
                        settlement.Party.PrisonRoster.RemoveTroop(troopRosterElement.Troop);
                        settlement.MilitiaPartyComponent.Party.AddElementToMemberRoster(troopRosterElement.Troop, 1);
                    }
                }
            }
        }
        private void SturgianBonus(MapEvent mapEvent)
        {

            if (mapEvent.HasWinner && mapEvent.Winner.LeaderParty.Culture.StringId == sturgiaId && SubModule.undeadRespawnConfig?.Count > 0)
            {



                List<MapEventParty> parties = mapEvent.Winner.Parties;
                foreach (MapEventParty party in parties)
                {
                    //Calculation to take a number between 0.15 and 0.5 based on the level of the party owner
                    float probability = 0.15f + (0.50f - 0.15f) * (party.Party.Owner.Level - 0f) / (63 - 0f);
                    if (MBRandom.RandomFloat > probability)
                        continue;

                    if (party.Party.Owner.Culture.StringId != sturgiaId)
                        continue;

                    int wounded = party.HealthyManCountAtStart - party.Party.NumberOfHealthyMembers;

                    if (wounded <= 0)
                        continue;

                    int recovered = (int)(15f / 100f * wounded);

                    if (recovered > 0)
                    {
                        for (int i = 0; i < recovered; i++)
                        {
                            float random = MBRandom.RandomFloat;
                            string characterId = SubModule.undeadRespawnConfig.RandomElementByWeight(x => x.Value);
                            CharacterObject characterObject = CharacterObject.Find(characterId);


                            party.Party.AddElementToMemberRoster(characterObject, 1);
                        }
                        if (party.Party == PartyBase.MainParty)
                        {
                            var textObject = new TextObject("{=sf4yHsh3JKw}{AMOUNT} undead soldiers rejoined your army");
                            textObject.SetTextVariable("AMOUNT", recovered);
                            MBInformationManager.AddQuickInformation(textObject);
                        }
                    }
                }

            }
        }
        public override void SyncData(IDataStore dataStore)
        {

        }
    }
}
