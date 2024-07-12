using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public class QuestCompletionBehavior : CampaignBehaviorBase
    {
        private HashSet<string> goodQuestBehaviors = new HashSet<string>
        {
            "RescueDaughterIssueBehavior",
            "EscortMerchantCaravanBehavior",
            "LandlordNeedsGarrisonBehavior",
            "GangLeaderNeedsSpecialWeaponsIssueBehavior",
            "LadysKnightOutIssueBehavior",
            "MerchantArmyOfPoachersIssueBehavior",
            "ExtortionByDesertersIssueBehavior"

            // Add other behavior class names that are considered good
        };

        private HashSet<string> badQuestBehaviors = new HashSet<string>
        {
            "GangLeaderNeedsRecruitsBehavior",
            "LandlordNeedsManualLaborersBehavior",
            "GangLeaderNeedsSpecialWeaponsIssueBehavior",
            "GangLeaderNeedsToOffloadStolenGoodsIssueBehavior",
             "GangLeaderNeedsWeaponsIssueQuestBehavior",
             "SmugglersIssueBehavior",
             "RaidVillageQuestTask"
            // Add other behavior class names that are considered bad
        };

        public override void RegisterEvents()
        {
            CustomCampaignEvents.OnQuestCompleted += OnQuestCompleted;
        }

        private void OnQuestCompleted(QuestBase quest)
        {
            if (quest.QuestGiver?.IsNotable == true)
            {
                if (IsGoodQuest(quest))
                {
                    AwardChivalryPoints(quest);
                }
                else if (IsBadQuest(quest))
                {
                    DeductChivalryPoints(quest);
                }
            }
        }

        private bool IsGoodQuest(QuestBase quest)
        {
            return goodQuestBehaviors.Contains(quest.GetType().Name);
        }

        private bool IsBadQuest(QuestBase quest)
        {
            return badQuestBehaviors.Contains(quest.GetType().Name);
        }

        private void AwardChivalryPoints(QuestBase quest)
        {
            int pointsToAward = 5; // Award 5 chivalry points for good quests
            var careerProgressionBehavior = Campaign.Current.GetCampaignBehavior<CareerProgressionBehavior>();
            careerProgressionBehavior.AddChivalryPoints(pointsToAward, CareerType.Knight); // Specify the career ID
            InformationManager.DisplayMessage(new InformationMessage($"You have completed a good quest and gained {pointsToAward} chivalry points!"));
        }

        private void DeductChivalryPoints(QuestBase quest)
        {
            int pointsToDeduct = 5; // Deduct 5 chivalry points for bad quests
            var careerProgressionBehavior = Campaign.Current.GetCampaignBehavior<CareerProgressionBehavior>();
            careerProgressionBehavior.AddChivalryPoints(-pointsToDeduct, CareerType.Knight); // Specify the career ID
            InformationManager.DisplayMessage(new InformationMessage($"You have completed a bad quest and lost {pointsToDeduct} chivalry points!"));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("goodQuestBehaviors", ref goodQuestBehaviors);
            dataStore.SyncData("badQuestBehaviors", ref badQuestBehaviors);
        }
    }
}
