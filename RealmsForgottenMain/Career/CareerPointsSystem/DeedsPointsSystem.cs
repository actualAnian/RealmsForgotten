using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Career.CareerPointsSystem
{
    public class DeedsPointsSystem : AbstractPointsSystem
    {
        [SaveableField(0)] int deedsPoints;
        private static int pointsForPerk = 50;
        public override void OnMapEventEnded(MapEvent mapEvent)
        {
            PartyBase attackerParty = mapEvent.AttackerSide.LeaderParty;
            PartyBase defenderParty = mapEvent.DefenderSide.LeaderParty;
            if (mapEvent.WinningSide != mapEvent.PlayerSide)
                return;

            if (mapEvent.IsHideoutBattle && mapEvent.IsPlayerMapEvent && mapEvent.WinningSide == mapEvent.PlayerSide)
            {
                AwardDeedsPoints(20);
            }

            if (attackerParty != null && Globals.IsBanditParty(attackerParty) && Globals.IsCaravanParty(defenderParty))
            {
                AwardDeedsPoints(10);
                InformationManager.DisplayMessage(new InformationMessage("You have successfully defended the villagers/caravan and gained chivalry points!"));
            }
        }
        public override void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
        {
            if (details != QuestBase.QuestCompleteDetails.Success) return;
            //if (quest.QuestGiver?.IsNotable == true)
            //{
            if (goodQuestBehaviors.Contains(quest.GetType().Name))
                AwardDeedsPoints(20);
            else if (badQuestBehaviors.Contains(quest.GetType().Name))
                AwardDeedsPoints(-20);
            //}
        }
        private HashSet<string> goodQuestBehaviors = new HashSet<string>
        {
            "RescueDaughterIssueBehavior",
            "EscortMerchantCaravanBehavior",
            "LandlordNeedsGarrisonBehavior",
            "GangLeaderNeedsSpecialWeaponsIssueBehavior",
            "LadysKnightOutIssueBehavior",
            "MerchantArmyOfPoachersIssueBehavior",
            "ExtortionByDesertersIssueBehavior"
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
        };

        public override string Description => new TextObject("{=rf_pointsystem_deeds}You gain deeds points by destroying hideouts, helping caravans, villagers in battle, completing good quests. Every 50 points gives 1 perk point. Your current deeds: ").ToString() + deedsPoints;

        private void AwardDeedsPoints(int points)
        {
            deedsPoints += points;
            int perkPoints = deedsPoints / pointsForPerk;
            deedsPoints -= perkPoints * pointsForPerk;
            AddPoints(perkPoints);
        }
    }
}
