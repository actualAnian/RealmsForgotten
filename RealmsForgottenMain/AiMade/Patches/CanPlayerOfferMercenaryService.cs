using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch]
    public static class Patch_FactionHelper_MercenaryCrashFix
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method("Helpers.FactionHelper:CanPlayerOfferMercenaryService");
        }

        static bool Prefix(Kingdom offerKingdom, ref List<IFaction> playerWars, ref List<IFaction> warsOfFactionToJoin, ref bool __result)
        {
            playerWars = new List<IFaction>();
            warsOfFactionToJoin = new List<IFaction>();

            if (offerKingdom == null || offerKingdom.Leader == null || Clan.PlayerClan?.MapFaction == null)
            {
                __result = false;
                return false; // Skip original method
            }

            try
            {
                float threshold = Campaign.Current.Models.DiplomacyModel.GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(offerKingdom);

                foreach (Kingdom kingdom in Kingdom.All)
                {
                    if (Clan.PlayerClan.MapFaction.IsAtWarWith(kingdom) && kingdom.CurrentTotalStrength > threshold)
                    {
                        playerWars.Add(kingdom);
                    }
                }

                foreach (Kingdom kingdom in Kingdom.All)
                {
                    if (offerKingdom.IsAtWarWith(kingdom))
                    {
                        warsOfFactionToJoin.Add(kingdom);
                    }
                }

                bool canJoin = Clan.PlayerClan.Kingdom == null &&
                               !Clan.PlayerClan.IsAtWarWith(offerKingdom) &&
                               Clan.PlayerClan.Tier >= Campaign.Current.Models.ClanTierModel.MercenaryEligibleTier &&
                               offerKingdom.Leader.GetRelationWithPlayer() >= Campaign.Current.Models.DiplomacyModel.MinimumRelationWithConversationCharacterToJoinKingdom &&
                               warsOfFactionToJoin.Intersect(playerWars).Count() == playerWars.Count &&
                               Clan.PlayerClan.Settlements.Count == 0;

                __result = canJoin;
                return false;
            }
            catch
            {
                __result = false;
                return false;
            }
        }
    }
}