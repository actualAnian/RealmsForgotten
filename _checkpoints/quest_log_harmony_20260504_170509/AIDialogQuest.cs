using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// Thin QuestBase wrapper that surfaces an NPC pending request in the
    /// native Bannerlord quest log. NPCContext.PendingRequest is ground truth;
    /// this class persists across save/load via RF_AIDialogSaveDefiner.
    ///
    /// Lifecycle:
    ///   Created   — when the LLM returns a non-empty "request" field
    ///   Fulfilled — when the LLM returns "request_fulfilled": true
    ///   Cancelled — when the NPC makes a new request (replace flow)
    ///   Timed-out — after 30 in-game days; also clears NPCContext.PendingRequest
    ///
    /// Note: IsSpecialQuest cannot be overridden in 1.3.0 (property is not virtual
    /// in the binary). AIDialogQuestPatch (Harmony postfix) handles this by forcing
    /// IsSpecialQuest = true at runtime so QuestManager.OnGameLoaded keeps the quest
    /// alive instead of cancelling it.
    /// </summary>
    public class AIDialogQuest : QuestBase
    {
        // In-memory registry keyed by questGiver.StringId.
        private static readonly Dictionary<string, AIDialogQuest> _activeByNpcId
            = new Dictionary<string, AIDialogQuest>();

        [SaveableField(1)] private string _npcStringId = "";
        [SaveableField(2)] private string _description  = "";

        public AIDialogQuest(
            string questId,
            Hero   questGiver,
            string description,
            int    durationDays)
            : base(questId, questGiver, CampaignTime.Now + CampaignTime.Days(durationDays), 0)
        {
            _npcStringId = questGiver.StringId;
            _description = description;
        }

        public override TextObject Title
            => new TextObject("{=!}" + BuildTitle());

        public override bool IsRemainingTimeHidden => false;

        protected override void SetDialogs() { }

        protected override void InitializeQuestOnGameLoad()
        {
            if (!string.IsNullOrWhiteSpace(_npcStringId))
                _activeByNpcId[_npcStringId] = this;
        }

        protected override void OnStartQuest()
        {
            _activeByNpcId[_npcStringId] = this;
            AddLog(new TextObject("{=!}" + Truncate(_description, 400)));
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

        private string BuildTitle()
        {
            int dot = _description.IndexOf('.');
            string candidate = (dot > 0 && dot <= 60)
                ? _description.Substring(0, dot)
                : Truncate(_description, 50);
            return candidate.Trim();
        }

        private static string Truncate(string s, int max)
            => s.Length <= max ? s : s.Substring(0, max) + "...";
    }

    /// <summary>
    /// Auto-discovered by Bannerlord's reflection scan.
    /// Base ID 912_345_678 is distinct from:
    ///   RealmsForgottenMain : 287_656_493
    ///   RFReligions         : 1_992_358_567
    /// </summary>
    public class RF_AIDialogSaveDefiner : SaveableTypeDefiner
    {
        public RF_AIDialogSaveDefiner() : base(912_345_678) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(AIDialogQuest), 1);
        }
    }
}
