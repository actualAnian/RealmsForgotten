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

            // Added null checks for safety
            if (attackerParty != null && defenderParty != null && Globals.IsBanditParty(attackerParty) && Globals.IsCaravanParty(defenderParty))
            {
                AwardDeedsPoints(10);
                InformationManager.DisplayMessage(new InformationMessage("You have successfully defended the villagers/caravan and gained chivalry points!"));
            }
        }
        public int AllDeedsPoints()
        {
            return deedsPoints + spentDeedsPoints;
        }
        public override void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
        {
            if (quest == null)
            {
                Debug.Print($"WARNING: DeedsPointsSystem.OnQuestCompleted received a null quest.");
                return;
            }

            if (details != QuestBase.QuestCompleteDetails.Success) return;

            try // Add try-catch around the logic just in case
            {
                string questTypeName = quest.GetType().Name;
                Debug.Print($"DeedsPointsSystem: Quest '{questTypeName}' completed successfully."); // Log which quest completed

                if (goodQuestBehaviors.Contains(questTypeName))
                {
                    Debug.Print($"DeedsPointsSystem: Quest '{questTypeName}' is GOOD. Awarding points.");
                    AwardDeedsPoints(20);
                }
                else if (badQuestBehaviors.Contains(questTypeName))
                {
                    Debug.Print($"DeedsPointsSystem: Quest '{questTypeName}' is BAD. Awarding points.");
                    AwardDeedsPoints(-20);
                }
                else
                {
                    Debug.Print($"DeedsPointsSystem: Quest '{questTypeName}' not found in good/bad lists.");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"ERROR in DeedsPointsSystem.OnQuestCompleted for quest '{quest?.GetType()?.Name ?? "NULL"}': {ex.Message}\n{ex.StackTrace}");
                // Optionally show an in-game message too
                InformationManager.DisplayMessage(new InformationMessage($"Crash Prevented in DeedsPointsSystem: {ex.Message}", Colors.Red));
            }
        }
        private readonly HashSet<string> goodQuestBehaviors = new()
        {
            "RescueDaughterIssueBehavior",
            "EscortMerchantCaravanBehavior",
            "LandlordNeedsGarrisonBehavior",
            "GangLeaderNeedsSpecialWeaponsIssueBehavior", // Note: This appears in both lists? Intentional?
            "LadysKnightOutIssueBehavior",
            "MerchantArmyOfPoachersIssueBehavior",
            "ExtortionByDesertersIssueBehavior",
            "RescueUliahQuest"
        };

        private readonly HashSet<string> badQuestBehaviors = new()
        {
            "GangLeaderNeedsRecruitsBehavior",
            "LandlordNeedsManualLaborersBehavior",
            "GangLeaderNeedsSpecialWeaponsIssueBehavior", // Note: This appears in both lists? Intentional?
            "GangLeaderNeedsToOffloadStolenGoodsIssueBehavior",
             "GangLeaderNeedsWeaponsIssueQuestBehavior",
             "SmugglersIssueBehavior",
             "RaidVillageQuestTask" // Assuming this is a quest type name
        };

        public override string Description => new TextObject("{=rf_pointsystem_deeds}You gain deeds points by destroying hideouts, helping caravans, villagers in battle, completing good quests. Every 50 points gives 1 perk point. Your current deeds: ").ToString() + deedsPoints;

        private void AwardDeedsPoints(int points)
        {
            // --- Add Debugging HERE ---
            Debug.Print($"DeedsPointsSystem: Attempting to award {points} deeds points.");
            try
            {
                deedsPoints += points;
                int perkPoints = 0; // Default to 0

                // Prevent division by zero
                if (pointsForPerk != 0)
                {
                    perkPoints = deedsPoints / pointsForPerk;
                    if (perkPoints != 0) // Only adjust if perk points were actually earned/lost
                    {
                        deedsPoints -= perkPoints * pointsForPerk;
                        spentDeedsPoints += perkPoints * pointsForPerk;
                    }
                }
                else
                {
                    Debug.Print("ERROR: pointsForPerk is zero in DeedsPointsSystem.AwardDeedsPoints");
                    return; // Don't proceed if config is broken
                }

                Debug.Print($"DeedsPointsSystem: Calculated {perkPoints} perk points to add.");

                // --- Check before calling base method ---
                if (Hero.MainHero == null)
                {
                    Debug.Print("ERROR: Hero.MainHero is NULL before calling AddPoints!");
                    // Consider throwing a more specific exception or handling this case
                    return; // Prevent calling AddPoints if MainHero is null
                }
                if (Hero.MainHero.HeroDeveloper == null)
                {
                    Debug.Print("ERROR: Hero.MainHero.HeroDeveloper is NULL before calling AddPoints!");
                    // Consider throwing or handling
                    return;
                }

                // --- Call the base method ---
                Debug.Print($"DeedsPointsSystem: Calling base.AddPoints({perkPoints}).");
                AddPoints(perkPoints); // This calls the method in AbstractPointsSystem
                Debug.Print($"DeedsPointsSystem: Successfully returned from base.AddPoints({perkPoints}).");

            }
            catch (NullReferenceException nre)
            {
                Debug.Print($"NullReferenceException INSIDE AwardDeedsPoints or base.AddPoints!: {nre.Message}\n{nre.StackTrace}");
                InformationManager.DisplayMessage(new InformationMessage($"Crash Prevented in AwardDeedsPoints: {nre.Message}", Colors.Red));
            }
            catch (Exception ex)
            {
                Debug.Print($"Exception INSIDE AwardDeedsPoints or base.AddPoints!: {ex.Message}\n{ex.StackTrace}");
                InformationManager.DisplayMessage(new InformationMessage($"Crash Prevented in AwardDeedsPoints: {ex.Message}", Colors.Red));
            }
        }
    }
}