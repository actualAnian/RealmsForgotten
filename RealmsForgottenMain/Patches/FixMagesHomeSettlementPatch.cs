using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.Patches;

[HarmonyPatch(typeof(Clan), "FindSettlementScoreForBeingHomeSettlement")]
public static class FixMagesHomeSettlementPatch
{
    public static void Postfix(Settlement settlement, Clan __instance, ref float __result)
    {
        if (__instance.Culture?.StringId == "mage" && settlement.Culture?.StringId == "mage")
            __result = 999999f;
    }
}