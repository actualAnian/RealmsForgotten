using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Conversation;

namespace RF_AIDialog
{
    [HarmonyPatch]
    internal static class RFAudioConversationPatches
    {
        [HarmonyPatch(typeof(ConversationManager), "EndConversation")]
        private static class Patch_EndConversation
        {
            private static void Postfix()
            {
                RFAudioService.StopAll("ConversationManager.EndConversation");
            }
        }

        [HarmonyPatch]
        private static class Patch_OnConversationDeactivate
        {
            private static MethodBase? TargetMethod()
            {
                return AccessTools.Method(typeof(ConversationManager), "OnConversationDeactivate");
            }

            private static void Postfix()
            {
                RFAudioService.StopAll("ConversationManager.OnConversationDeactivate");
            }
        }

        [HarmonyPatch]
        private static class Patch_Clear
        {
            private static MethodBase? TargetMethod()
            {
                return AccessTools.Method(typeof(ConversationManager), "Clear");
            }

            private static void Postfix()
            {
                RFAudioService.StopAll("ConversationManager.Clear");
            }
        }
    }
}
