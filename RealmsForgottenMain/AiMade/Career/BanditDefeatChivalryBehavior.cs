using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public class BanditDefeatChivalryBehavior : CampaignBehaviorBase
    {
        private int banditPartiesDefeated;

        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent.WinningSide == mapEvent.PlayerSide)
            {
                foreach (var party in mapEvent.DefenderSide.Parties)
                {
                    if (IsBanditParty(party.Party))
                    {
                        banditPartiesDefeated++;
                        AwardChivalryPoints();
                        break;
                    }
                }
            }
        }

        private bool IsBanditParty(PartyBase party)
        {
            if (party == null || party.MobileParty == null || party.MobileParty.PartyComponent == null)
            {
                return false;
            }
            return party.MobileParty.PartyComponent.GetType().Name == "BanditPartyComponent";
        }

        private void AwardChivalryPoints()
        {
            int pointsToAward = 0;

            if (banditPartiesDefeated >= 30)
            {
                pointsToAward = 20;
            }
            else if (banditPartiesDefeated >= 20)
            {
                pointsToAward = 15;
            }
            else if (banditPartiesDefeated >= 10)
            {
                pointsToAward = 10;
            }
            else if (banditPartiesDefeated >= 5)
            {
                pointsToAward = 5;
            }

            if (pointsToAward > 0)
            {
                var careerProgressionBehavior = Campaign.Current.GetCampaignBehavior<CareerProgressionBehavior>();
                if (careerProgressionBehavior.AddChivalryPoints(pointsToAward, CareerType.Knight))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"You have defeated {banditPartiesDefeated} bandit parties and gained {pointsToAward} chivalry points!"));
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("banditPartiesDefeated", ref banditPartiesDefeated);
        }
    }
}
