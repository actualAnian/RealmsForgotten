using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(RebellionsCampaignBehavior), "InitializeIconIdAndFrequencies")]
    public class Patch_RebellionsCampaignBehavior
    {
        static void Postfix(RebellionsCampaignBehavior __instance)
        {
            if (__instance != null)
            {
                // Ensure rebel icons are initialized to avoid null references
                var rebelIconIds = __instance.GetType().GetField("_rebelIconIds")?.GetValue(__instance) as List<string>;
                if (rebelIconIds == null || rebelIconIds.Count == 0)
                {
                    __instance.GetType().GetField("_rebelIconIds")?.SetValue(__instance, new List<string> { "default_rebel_icon" });
                    InformationManager.DisplayMessage(new InformationMessage("Default rebel icons assigned."));
                }
            }
        }
    }
}

