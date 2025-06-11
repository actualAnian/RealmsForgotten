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
    class VirtuePointsSystem : AbstractPointsSystem
    {
        [SaveableField(0)] int virtuePoints;
        [SaveableField(1)] int spentVirtuePoints;
        private static readonly int pointsForPerk = 50;

        public override void OnMapEventEnded(MapEvent mapEvent)
        {
            PartyBase attackerParty = mapEvent.AttackerSide.LeaderParty;
            PartyBase defenderParty = mapEvent.DefenderSide.LeaderParty;

            if (mapEvent.WinningSide != mapEvent.PlayerSide)
                return;

            if (mapEvent.IsHideoutBattle && mapEvent.IsPlayerMapEvent && mapEvent.WinningSide == mapEvent.PlayerSide)
            {
                AwardVirtuePoints(20);
            }

            if (mapEvent.IsFieldBattle
                && mapEvent.IsPlayerMapEvent
                && (Globals.IsBanditParty(attackerParty) || Globals.IsBanditParty(defenderParty))
                && mapEvent.WinningSide == mapEvent.PlayerSide)
            {
                AwardVirtuePoints(20);
            }
            
            if (attackerParty != null && Globals.IsBanditParty(attackerParty) && Globals.IsCaravanParty(defenderParty))
            {
                AwardVirtuePoints(10);
                InformationManager.DisplayMessage(new InformationMessage("You have successfully defended the villagers/caravan and gained virtue points!"));
            }

            base.OnMapEventEnded(mapEvent);
        }

        public override void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
        {
            if (quest == null) return;
            if (details != QuestBase.QuestCompleteDetails.Success) return;
            foreach (string questBehavior in goodQuestBehaviors)
            {
                if (quest.Title.Value.Contains(questBehavior))
                {
                    AwardVirtuePoints(20);
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=rf_career_virtue_gained}You gain 20 virtue points!").ToString(), new Color(0, 255, 0)));
                    return;
                }
            }
            foreach (string questBehavior in badQuestBehaviors)
            {
                if (quest.Title.Value.Contains(questBehavior))
                {
                    AwardVirtuePoints(-20);
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=rf_career_virtue_lost}You lose 20 virtue points for your disgusting actions!").ToString(), new Color(255, 0, 0)));
                    return;
                }
            }
        }

        static readonly List<string> goodQuestBehaviors = new()
        {
            "daughter found", // NotableWantsDaughterFoundIssueBehavior
            "Escort Merchant Caravan", // EscortMerchantCaravanIssueBehavior
            "Train troops for ", // LandlordTrainingForRetainersIssueBehavior
            "The Art of The Trade", // LandLordTheArtOfTheTradeIssueBehavior
            "Lady's Knight Out", //LadysKnightOutIssueBehavior
            "Army of Poachers", //MerchantArmyOfPoachersIssueBehavior
            "Needs Help With Brigands", //MerchantNeedsHelpWithOutlawsIssueQuestBehavior
            "Caravan Ambush", //CaravanAmbushIssueBehavior
            "Needs Grain Seeds", //HeadmanNeedsGrainIssueBehavior
            "Extortion by Deserters ", //ExtortionByDesertersIssueBehavior
            "Smugglers of ", //SmugglersIssueBehavior
            "Bandit Base Near", //NearbyBanditBaseIssueBehavior
            "Needs Tools", //VillageNeedsToolsIssueBehavior
        };

        static readonly List<string> badQuestBehaviors = new()
        {
            "Gang Needs Recruits", //GangLeaderNeedsRecruitsBehavior
            "Landlord needs access to", //LandlordNeedsAccessToVillageCommonsIssueBehavior
            "Landowner Needs Manual Laborers", //LandLordNeedsManualLaborersIssueBehavior
            "Special Weapon Order", //"GangLeaderNeedsSpecialWeaponsIssueBehavior",
            "Purchase stolen goods from ", //GangLeaderNeedsToOffloadStolenGoodsIssueBehavior
            "Gang leader needs weapons" , //GangLeaderNeedsWeaponsIssueBehavior
            "Gang Needs Recruits", //GangLeaderNeedsRecruitsIssueBehavior
             "Raid an Enemy Territory", //RaidAnEnemyTerritoryIssueBehavior
             "Snare The Wealthy", //SnareTheWealthyIssueBehavior
        };

        public override string Description => throw new NotImplementedException();

        private void AwardVirtuePoints(int points)
        {
            virtuePoints += points;
            int perkPoints = virtuePoints / pointsForPerk;
            virtuePoints -= perkPoints * pointsForPerk;
            spentVirtuePoints += perkPoints * pointsForPerk;
            AddPoints(perkPoints);
        }
    }
}
