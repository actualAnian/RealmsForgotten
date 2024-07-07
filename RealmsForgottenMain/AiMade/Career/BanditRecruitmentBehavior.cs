using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public class BanditRecruitmentBehavior : CampaignBehaviorBase
    {
        private int banditsRecruited;

        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent.IsPlayerMapEvent && mapEvent.WinningSide == PartyBase.MainParty.Side)
            {
                RecruitBanditPrisoners();
            }
        }

        private void RecruitBanditPrisoners()
        {
            var playerParty = MobileParty.MainParty;
            int banditCount = 0;

            foreach (var prisoner in playerParty.PrisonRoster.GetTroopRoster())
            {
                if (prisoner.Character.IsBandit())
                {
                    playerParty.MemberRoster.AddToCounts(prisoner.Character, prisoner.Number);
                    playerParty.PrisonRoster.RemoveTroop(prisoner.Character, prisoner.Number);
                    banditCount += prisoner.Number;
                }
            }

            if (banditCount > 0)
            {
                BanditConversionManager.OnBanditConverted(Hero.MainHero, banditCount);
                InformationManager.DisplayMessage(new InformationMessage($"{banditCount} bandits have been recruited into your party."));
                banditsRecruited += banditCount;
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("banditsRecruited", ref banditsRecruited);
        }
    }
}