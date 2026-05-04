using TaleWorlds.SaveSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// Registers RF_AIDialog types with the Bannerlord save system.
    /// Base ID 456789012 -- does not overlap with any other RF or base-game definer.
    ///
    /// Without this, AIDialogQuest sits in QuestManager._quests as an unregistered
    /// type and the save system crashes when writing the campaign file.
    ///
    /// Load path:
    ///   1. Save system deserializes AIDialogQuest (StringId, QuestGiver, QuestDueTime,
    ///      _npcStringId all restored from the save file).
    ///   2. QuestManager.PreAfterLoad removes null entries -- AIDialogQuest is NOT null,
    ///      so it survives.
    ///   3. QuestManager.OnGameLoaded sees IsSpecialQuest == true (driven by
    ///      SpecialQuestType != null, reinforced by AIDialogQuestIsSpecialPatch) and
    ///      calls InitializeQuestOnLoadWithQuestManager() -> InitializeQuestOnGameLoad(),
    ///      which repopulates the static _activeByNpcId lookup.
    ///   4. AIDialogBehavior.ReconstructQuestsFromNPCContexts fires after OnGameLoaded;
    ///      it checks ForNpc() / IsOngoing and skips re-creation for quests that
    ///      survived intact.
    /// </summary>
    internal class RF_AIDialogSaveDefiner : SaveableTypeDefiner
    {
        public RF_AIDialogSaveDefiner() : base(456789012) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(AIDialogQuest), 1);
        }
    }
}
