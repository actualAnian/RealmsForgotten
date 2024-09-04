using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.Patches;

[HarmonyPatch(typeof(Clan), "FindSettlementScoreForBeingHomeSettlement")]
public static class FixMagesHomeSettlementPatch
{
    public static void Postfix(Settlement settlement, ref float __result)
    {
        if (settlement.Culture?.StringId == "mage")
            __result = 999999f;
    }
}