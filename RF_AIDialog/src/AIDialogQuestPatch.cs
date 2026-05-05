using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

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

    /// <summary>
    /// CampaignBehaviorManager.OnBeforeSave() fires BEFORE AIDialogBehavior's own
    /// OnBeforeSave listener, because CampaignBehaviorManager registers its listener
    /// in its constructor while our behavior registers in RegisterEvents().
    ///
    /// When CampaignBehaviorManager.OnBeforeSave() runs it calls SyncData on every
    /// behavior, including ViewDataTrackerCampaignBehavior, which stores
    /// _questSelection (a QuestBase reference) into BehaviorSaveData._records.
    /// If _questSelection == AIDialogQuest, CollectObjects finds the unregistered
    /// type and the save fails with "Could not find type definition".
    ///
    /// This prefix runs our hide logic BEFORE behavior data is stored, so
    /// _questSelection is cleared (and later restored) around the save window.
    /// </summary>
    [HarmonyPatch(typeof(CampaignBehaviorManager), "OnBeforeSave")]
    internal static class AIDialogPreSavePatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            try { AIDialogBehavior.Instance?.PrehideBeforeBehaviorSave(); }
            catch { }
        }
    }
}
