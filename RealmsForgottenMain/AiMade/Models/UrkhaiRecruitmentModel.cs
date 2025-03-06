using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace RealmsForgotten.AiMade.Models
{
    [HarmonyPatch(typeof(RecruitmentCampaignBehavior))]
    public class UrkhaiRecruitmentPatch
    {
        // Reduce Recruitment Costs for Urkhai
        [HarmonyPatch("ApplyInternal")]
        [HarmonyPrefix]
        private static bool AdjustRecruitmentCost(
            MobileParty side1Party,
            Settlement settlement,
            Hero individual,
            CharacterObject troop,
            int number,
            int bitCode,
            RecruitmentCampaignBehavior.RecruitingDetail detail)
        {
            // 🚨 Fix: Ensure all required objects exist
            if (individual == null || troop == null || side1Party == null)
            {
                return true; // Skip execution to prevent crash
            }

            if (individual.Culture != null && individual.Culture.StringId == "urkhai")
            {
                int reducedCost = (int)Math.Floor(number * troop.Tier * 0.75f); // 25% cheaper
                if (individual.Gold >= reducedCost) // Ensure they have enough gold
                {
                    GiveGoldAction.ApplyBetweenCharacters(individual, null, -reducedCost);
                }
            }

            return true; // Continue with normal execution
        }

        // Ensure Urkhai Lords Recruit More Troops
        [HarmonyPatch("UpdateVolunteersOfNotablesInSettlement")]
        [HarmonyPostfix]
        private static void IncreaseRecruitment(Settlement settlement)
        {
            if (settlement == null || settlement.Notables == null) return; // 🚨 Fix: Prevent null errors

            foreach (Hero notable in settlement.Notables)
            {
                if (notable?.Culture != null && notable.Culture.StringId == "urkhai" && notable.CanHaveRecruits)
                {
                    // 🚨 Fix: Ensure volunteer slots are available before modifying them
                    if (notable.VolunteerTypes == null) continue;

                    for (int i = 0; i < notable.VolunteerTypes.Length; i++)
                    {
                        if (notable.VolunteerTypes[i] == null)
                        {
                            notable.VolunteerTypes[i] = Campaign.Current.Models.VolunteerModel.GetBasicVolunteer(notable);
                        }
                    }
                }
            }
        }
    }
}