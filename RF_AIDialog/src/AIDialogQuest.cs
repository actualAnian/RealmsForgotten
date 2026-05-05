using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RF_AIDialog
{
    /// <summary>
    /// Thin QuestBase wrapper that surfaces an NPC pending request in the
    /// native Bannerlord quest log. NPCContext.PendingRequest is ground truth.
    ///
    /// Persistence strategy:
    ///   AIDialogQuest is NOT registered in any SaveableTypeDefiner.
    ///   Registering a QuestBase subclass without IssueBase crashes on load in 1.3.0.
    ///
    ///   Instead, AIDialogBehavior.OnBeforeSave() removes all AIDialogQuest instances
    ///   from QuestManager._quests right before the campaign is written to disk.
    ///   AIDialogBehavior.OnSaveOver() restores them immediately after.
    ///   As a result, AIDialogQuest never appears in save data and the save system
    ///   never encounters an unknown type.
    ///
    ///   On load, ReconstructQuestsFromNPCContexts (OnGameLoadFinishedEvent) recreates
    ///   the quest entries from NPCContext.PendingRequest (persisted via JSON).
    /// </summary>
    public class AIDialogQuest : QuestBase
    {
        private static readonly Dictionary<string, AIDialogQuest> _activeByNpcId
            = new Dictionary<string, AIDialogQuest>();

        // Not persisted — quest is reconstructed from NPCContext on every load.
        private string _npcStringId  = "";
        private string _description  = "";

        public AIDialogQuest(
            string questId,
            Hero   questGiver,
            string description,
            int    durationDays)
            : base(questId, questGiver, CampaignTime.Now + CampaignTime.Days(durationDays), 0)
        {
            _npcStringId = questGiver.StringId;
            _description = Truncate(description, 400);
        }

        public override string SpecialQuestType => "RfAIDialog";

        // IsSpecialQuest is not virtual in 1.3.0 binary — handled by AIDialogQuestPatch.

        public override TextObject Title
            => new TextObject("{=!}" + (QuestGiver?.Name?.ToString() ?? "NPC") + " — pending request");

        public override bool IsRemainingTimeHidden => false;

        protected override void SetDialogs() { }

        protected override void OnStartQuest()
        {
            string id = !string.IsNullOrWhiteSpace(_npcStringId)
                ? _npcStringId
                : QuestGiver?.StringId ?? "";
            RFAIDebug.Log($"AIDialogQuest.OnStartQuest: npc={id}");
            if (!string.IsNullOrWhiteSpace(id))
                _activeByNpcId[id] = this;
            AddLog(new TextObject("{=!}" + _description));
        }

        protected override void InitializeQuestOnGameLoad()
        {
            // Not called in practice: quest is never serialized.
            // If somehow reached, derive id from QuestGiver (which base class does persist).
            string id = !string.IsNullOrWhiteSpace(_npcStringId)
                ? _npcStringId
                : QuestGiver?.StringId ?? "";
            if (!string.IsNullOrWhiteSpace(id))
                _activeByNpcId[id] = this;
        }

        protected override void OnTimedOut()
        {
            try
            {
                if (QuestGiver != null)
                {
                    var ctx = NPCContextStore.Instance?.GetOrCreate(QuestGiver);
                    if (ctx != null && ctx.HasPendingRequest)
                    {
                        ctx.PendingRequest = null;
                        NPCContextStore.Instance!.MarkDirty(ctx);
                    }
                }
            }
            catch { }

            AddLog(new TextObject("{=!}Time ran out. The matter was left unresolved."));
            _activeByNpcId.Remove(_npcStringId);
        }

        /// <summary>
        /// Appends a single-line objective update to the quest log.
        /// Called by QuestAtomEngine as atoms are completed.
        /// </summary>
        public void AddObjectiveLog(string message)
        {
            try { AddLog(new TextObject("{=!}" + message)); }
            catch { }
        }

        public void MarkFulfilled()
        {
            AddLog(new TextObject("{=!}The matter has been settled."));
            CompleteQuestWithSuccess();
            _activeByNpcId.Remove(_npcStringId);
        }

        public void Cancel()
        {
            CompleteQuestWithFail();
            _activeByNpcId.Remove(_npcStringId);
        }

        public static AIDialogQuest? ForNpc(string npcStringId)
        {
            if (string.IsNullOrWhiteSpace(npcStringId)) return null;
            return _activeByNpcId.TryGetValue(npcStringId, out var q) ? q : null;
        }

        /// <summary>
        /// Clears all entries from the static lookup. Must be called before
        /// reconstructing quests from NPCContext after a game load — the dictionary
        /// persists across load boundaries and stale entries would block recreation.
        /// </summary>
        internal static void ClearAll() => _activeByNpcId.Clear();

        private static string Truncate(string s, int max)
            => s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
