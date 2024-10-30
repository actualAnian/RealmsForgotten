using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(FactionDiscontinuationCampaignBehavior), "CanClanBeDiscontinued")]
    internal class ClanRemovalPatches
    {
        private static void Postfix(ref bool __result)
        {
            __result = false;
        }
    }
    [HarmonyPatch(typeof(FactionDiscontinuationCampaignBehavior), "AddIndependentClan")]
    public class AddIndependentClanPatch
    {
        private static AccessTools.FieldRef<FactionDiscontinuationCampaignBehavior, Dictionary<Clan, CampaignTime>> _independentClansRef = AccessTools.FieldRefAccess<FactionDiscontinuationCampaignBehavior, Dictionary<Clan, CampaignTime>>("_independentClans");

        private static bool Prefix(FactionDiscontinuationCampaignBehavior __instance, Clan clan)
        {
            if (_independentClansRef(__instance).ContainsKey(clan) || clan.IsClanTypeMercenary || clan.IsEliminated || clan.IsRebelClan || clan == Clan.PlayerClan || clan.IsMinorFaction || clan == null || clan.Kingdom != null || !clan.IsNoble)
                return false;

            _independentClansRef(__instance).Add(clan, CampaignTime.WeeksFromNow(0.0f));
            return false;
        }
    }

    [HarmonyPatch(typeof(FactionDiscontinuationCampaignBehavior), "DiscontinueClan")]
    public class DiscontinueClanPatch
    {
        private static AccessTools.FieldRef<FactionDiscontinuationCampaignBehavior, Dictionary<Clan, CampaignTime>> _independentClansRef = AccessTools.FieldRefAccess<FactionDiscontinuationCampaignBehavior, Dictionary<Clan, CampaignTime>>("_independentClans");

        private static bool Prefix(FactionDiscontinuationCampaignBehavior __instance, Clan clan)
        {
            ClanRemovalLogic.JoinNewKingdom(clan);
            if (clan.Kingdom != null)
            {
                _independentClansRef(__instance).Remove(clan);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(FactionDiscontinuationCampaignBehavior), "DailyTickClan")]
    public class DailyTickClanPatch
    {
        private static AccessTools.FieldRef<FactionDiscontinuationCampaignBehavior, Dictionary<Clan, CampaignTime>> _independentClansRef = AccessTools.FieldRefAccess<FactionDiscontinuationCampaignBehavior, Dictionary<Clan, CampaignTime>>("_independentClans");

        private static void Postfix(FactionDiscontinuationCampaignBehavior __instance, Clan clan)
        {
            int num;
            if (_independentClansRef(__instance).ContainsKey(clan))
            {
                CampaignTime campaignTime = _independentClansRef(__instance)[clan];
                num = !campaignTime.IsPast ? 1 : 0;
            }
            else
            {
                num = 1;
            }

            if (num != 0)
                return;

            ClanRemovalLogic.JoinNewKingdom(clan);
            if (clan.Kingdom != null)
            {
                _independentClansRef(__instance).Remove(clan);
            }
        }
    }

    public static class ClanRemovalLogic
    {
        public static void JoinNewKingdom(Clan clan)
        {
            Kingdom kingdomToJoin = GetKingdomToJoin(clan);
            if (kingdomToJoin == null)
                return;
            clan.Kingdom = kingdomToJoin;
            InformationManager.DisplayMessage(new InformationMessage(clan.GetName()?.ToString() + " has pledged their banners to " + kingdomToJoin.InformalName?.ToString() + "."));
        }

        public static Kingdom GetKingdomToJoin(Clan clan)
        {
            List<Kingdom> kingdoms1 = new List<Kingdom>();
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (!kingdom.IsEliminated && !kingdom.IsAtWarWith(clan) && kingdom.Settlements.Count > 1 && kingdom.RulingClan != Clan.PlayerClan)
                    kingdoms1.Add(kingdom);
            }
            List<Kingdom> kingdoms2 = new List<Kingdom>();
            foreach (Kingdom kingdom in kingdoms1)
            {
                if (kingdom.Culture == clan.Culture)
                    kingdoms2.Add(kingdom);
            }
            List<Kingdom> kingdoms3 = new List<Kingdom>();
            foreach (Kingdom kingdom in kingdoms1)
            {
                if (clan.Leader.GetRelation(kingdom.Leader) > -10)
                    kingdoms3.Add(kingdom);
            }
            List<Kingdom> kingdoms4 = new List<Kingdom>();
            foreach (Kingdom kingdom in kingdoms2)
            {
                if (clan.Leader.GetRelation(kingdom.Leader) > -10)
                    kingdoms4.Add(kingdom);
            }
            List<Kingdom> kingdomList = new List<Kingdom>();
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (!kingdom.IsEliminated)
                    kingdomList.Add(kingdom);
            }
            Kingdom kingdomToJoin = null;
            if (kingdoms4.Count != 0)
                kingdomToJoin = GetWeakestKingdomInList(kingdoms4);
            if (kingdomToJoin == null && kingdoms3.Count != 0)
                kingdomToJoin = GetWeakestKingdomInList(kingdoms3);
            if (kingdomToJoin == null && kingdoms2.Count != 0)
                kingdomToJoin = GetWeakestKingdomInList(kingdoms2);
            if (kingdomToJoin == null)
                kingdomToJoin = GetWeakestKingdomInList(kingdoms1);
            if (kingdomToJoin == null && kingdomList.Count == 1 && clan.Settlements.Count == 0)
            {
                foreach (Kingdom kingdom in kingdomList)
                    kingdomToJoin = kingdom;
            }
            return kingdomToJoin;
        }

        private static Kingdom GetWeakestKingdomInList(List<Kingdom> kingdoms)
        {
            if (kingdoms == null || kingdoms.Count == 0)
                return null;
            float num = float.MaxValue;
            Kingdom weakestKingdomInList = null;
            foreach (Kingdom kingdom in kingdoms)
            {
                if (kingdom.TotalStrength < num)
                {
                    num = kingdom.TotalStrength;
                    weakestKingdomInList = kingdom;
                }
            }
            return weakestKingdomInList;
        }
    }
}
