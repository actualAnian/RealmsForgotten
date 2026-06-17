using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// Thin QuestBase wrapper that surfaces an NPC pending request in the
    /// native Bannerlord quest log. NPCContext.PendingRequest is ground truth.
    ///
    /// Persistence strategy:
    ///   - RF_AIDialogSaveDefiner registers this type (base 456789012, class ID 1).
    ///   - SpecialQuestType = "RfAIDialog" keeps QuestManager.OnGameLoaded from
    ///     cancelling the quest on reload (same pattern as all other RF quests).
    ///   - InitializeQuestOnGameLoad() repopulates the static _activeByNpcId lookup.
    ///   - AIDialogBehavior.ReconstructQuestsFromNPCContexts() acts as a fallback:
    ///     if the quest somehow fails to survive load, NPCContext.PendingRequest
    ///     (persisted via JSON) lets us recreate it transparently.
    /// </summary>
    public class AIDialogQuest : QuestBase
    {
        private static readonly Dictionary<string, AIDialogQuest> _activeByNpcId
            = new Dictionary<string, AIDialogQuest>();

        // Persisted so InitializeQuestOnGameLoad can repopulate _activeByNpcId on load.
        [SaveableField(1)]
        private string _npcStringId = "";

        public AIDialogQuest(
            string questId,
            Hero   questGiver,
            string description,
            int    durationDays)
            : base(questId, questGiver, CampaignTime.Now + CampaignTime.Days(durationDays), 0)
        {
            _npcStringId = questGiver.StringId;
            AddLog(new TextObject("{=!}" + Truncate(description, 400)));
        }

        // Marks this as a special/story quest so QuestManager.OnGameLoaded calls
        // InitializeQuestOnLoadWithQuestManager() instead of cancelling it.
        // This is the same pattern used by all RealmsForgotten quests.
        public override string SpecialQuestType => "RfAIDialog";

        public override TextObject Title
            => new TextObject("{=!}" + (QuestGiver?.Name?.ToString() ?? "NPC") + " — pending request");

        public override bool IsRemainingTimeHidden => false;

        protected override void SetDialogs() { }

        protected override void OnStartQuest()
        {
            RFAIDebug.Log($"AIDialogQuest.OnStartQuest: npc={_npcStringId}");
            _activeByNpcId[_npcStringId] = this;
        }

        protected override void InitializeQuestOnGameLoad()
        {
            // Called by QuestManager.OnGameLoaded -> InitializeQuestOnLoadWithQuestManager()
            // when SpecialQuestType is set. Repopulate the static lookup so ForNpc() works
            // and AIDialogBehavior.ReconstructQuestsFromNPCContexts skips re-creation.
            if (!string.IsNullOrWhiteSpace(_npcStringId))
            {
                _activeByNpcId[_npcStringId] = this;
                RFAIDebug.Log($"AIDialogQuest.InitializeQuestOnGameLoad: restored npc={_npcStringId}");
            }
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

        private static string Truncate(string s, int max)
            => s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
