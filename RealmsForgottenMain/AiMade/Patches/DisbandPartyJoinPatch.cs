using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(DisbandPartyCampaignBehavior), "disbanding_leaderless_party_join_main_party_answer_on_consequence")]
    public static class DisbandPartyJoinPatch
    {
        public static bool Prefix()
        {
            if (MobileParty.ConversationParty == null)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage("[RF Patch] Skipped leaderless join — ConversationParty was null.", Colors.Red));
                return false; // impede execução do original
            }

            return true; // deixa o método original rodar normalmente
        }
    }
}
