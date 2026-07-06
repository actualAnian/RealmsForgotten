using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Career.CareerPointsSystem
{
    public class DeedsPointsSystem : AbstractPointsSystem
    {
        [SaveableField(0)] int deedsPoints;
        [SaveableField(1)] int spentDeedsPoints = 0;
        private static readonly int pointsForPerk = 50;
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

            bool defendedCiviliansFromBandits = attackerParty != null && defenderParty != null && Globals.IsBanditParty(attackerParty) && Globals.IsCaravanParty(defenderParty);
            if (defendedCiviliansFromBandits)
            {
                AwardDeedsPoints(10);
                InformationManager.DisplayMessage(new InformationMessage("You have successfully defended the villagers/caravan and gained deeds points!"));
            }
            else if (!mapEvent.IsHideoutBattle && mapEvent.IsPlayerMapEvent && PlayerWonAgainstBandits(mapEvent))
            {
                AwardDeedsPoints(5);
                InformationManager.DisplayMessage(new InformationMessage("You defeated a bandit party and gained deeds points!"));
            }
        }

        private bool PlayerWonAgainstBandits(MapEvent mapEvent)
        {
            MapEventSide enemySide = mapEvent.PlayerSide == BattleSideEnum.Attacker ? mapEvent.DefenderSide : mapEvent.AttackerSide;
            return enemySide.Parties.Any(party => Globals.IsBanditParty(party.Party));
        }
        public int AllDeedsPoints()
        {
            return deedsPoints + spentDeedsPoints;
        }
        public override void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
        {
            if (quest == null) return;
            if (details != QuestBase.QuestCompleteDetails.Success) return;
            foreach (string questBehavior in goodQuestBehaviors)
            {
                if (quest.Title.Value.Contains(questBehavior))
                {
                    AwardDeedsPoints(20);
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=rf_career_deeds_gained}You gain 20 deeds points!").ToString(), new Color(0, 255, 0)));
                    return;
                }
            }
            foreach (string questBehavior in badQuestBehaviors)
            {
                if (quest.Title.Value.Contains(questBehavior))
                {
                    AwardDeedsPoints(-20);
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=rf_career_deeds_lost}You lose 20 deeds points for your disgusting actions!").ToString(), new Color(255, 0, 0)));
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

        public override string Description => new TextObject("{=rf_pointsystem_deeds}You gain deeds points by defeating bandit parties, destroying hideouts, helping caravans, villagers in battle, completing good quests, you also lose points through completing bad quests. Every 50 points gives 1 perk point. Your current deeds: ").ToString() + deedsPoints;

        private void AwardDeedsPoints(int points)
        {
            if (pointsForPerk <= 0) return;

            deedsPoints += points;
            int perkPoints = deedsPoints / pointsForPerk;

            if (perkPoints != 0)
            {
                deedsPoints -= perkPoints * pointsForPerk;
                spentDeedsPoints += perkPoints * pointsForPerk;
                AddPoints(perkPoints);
            }
        }
    }
}
