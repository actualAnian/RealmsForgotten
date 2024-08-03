using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public class BanditRecruitmentBehavior : CampaignBehaviorBase
    {
        private int banditsRecruited;

        // Define the set of bandits that should not be converted
        private static readonly HashSet<string> NonConvertibleBandits = new HashSet<string>
        {
            "sea_raiders_bandit",
            "sea_raiders_raider",
            "sea_raiders_chief",
            "sea_raiders_boss",
            "cs_nelrog_bandits_bandit",
            "cs_nelrog_bandits_raider",
            "cs_nelrog_bandits_chief",
            "cs_nelrog_bandits_boss",
             "cs_devils_bandits_bandit",
             "cs_devils_bandits_raider",
             "cs_devils_bandits_chief",
             "cs_devils_bandits_boss",

        };

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
                if (IsConvertibleBandit(prisoner.Character))
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

        // Method to check if a bandit is convertible
        private bool IsConvertibleBandit(CharacterObject character)
        {
            return character.IsBandit() && !NonConvertibleBandits.Contains(character.StringId);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("banditsRecruited", ref banditsRecruited);
        }
    }
}
