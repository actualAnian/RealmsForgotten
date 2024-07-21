using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace RealmsForgotten.AiMade;

public class HarmonyPatches
{
    public static void ApplyPatches()
    {
        var harmony = new Harmony("com.realmsforgotten.extensions"); // Only one instance

        var original = AccessTools.Method(typeof(VassalAndMercenaryOfferCampaignBehavior), "MercenaryKingdomSelectionConditionsHold");
        var postfix = new HarmonyMethod(typeof(VassalAndMercenaryOfferCampaignBehaviorPatch), "MercenaryKingdomSelectionConditionsHoldPostfix");

        harmony.Patch(original, postfix: postfix);
    }
}