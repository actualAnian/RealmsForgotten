using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// QuestBase.IsSpecialQuest is not marked virtual in the 1.3.0 binary,
    /// so we cannot override it in AIDialogQuest. Without IsSpecialQuest = true,
    /// QuestManager.OnGameLoaded treats our quest as a broken issue-quest,
    /// calls CompleteQuestWithCancel(), and fires Debug.FailedAssert — crashing
    /// the save reload.
    ///
    /// This postfix forces the property to return true whenever the instance
    /// is an AIDialogQuest, making QuestManager call
    /// InitializeQuestOnLoadWithQuestManager() instead.
    /// </summary>
    [HarmonyPatch(typeof(QuestBase), "get_IsSpecialQuest")]
    internal static class AIDialogQuestIsSpecialPatch
    {
        [HarmonyPostfix]
        private static void Postfix(QuestBase __instance, ref bool __result)
        {
            if (__instance is AIDialogQuest)
                __result = true;
        }
    }
}
