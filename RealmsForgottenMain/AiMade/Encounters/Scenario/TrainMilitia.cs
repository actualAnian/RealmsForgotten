using RealmsForgotten.AiMade.Encounters.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Encounters.Scenario
{
    public class TrainMilitia : IEncounterScenario
    {
        public string Name => "Train Local Militia";

        public bool IsPossible(Settlement settlement)
        {
            return settlement.IsTown &&
                   settlement.Notables.Any() &&
                   !settlement.IsUnderSiege &&
                   Hero.MainHero.Clan.Tier >= 2;
        }

        public void OnStartEncounter(Settlement settlement)
        {
            InformationManager.ShowInquiry(
                new InquiryData(
                    "Militia Request",
                    $"A town notable asks if you'll help train the local militia.",
                    true, true,
                    "Help Them Train",
                    "Decline",
                    () => StartMilitiaMission(settlement),
                    null
                )
            );
        }

        private void StartMilitiaMission(Settlement settlement)
        {
            InformationManager.DisplayMessage(new InformationMessage("🛡 Training militia in " + settlement.Name));
            // Replace "arena" with a custom scene later
            MissionState.OpenNew(
                "TrainMilitia",
                new MissionInitializerRecord("arena"),
                (mission) => new MissionBehavior[] {
                    new RealmsForgotten.AiMade.Adventurer.TrainMilitiaForVillage(settlement)
                }
            );
        }
    }
}
