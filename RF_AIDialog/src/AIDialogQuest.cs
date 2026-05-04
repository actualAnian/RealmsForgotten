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
    /// Persistence strategy: AIDialogQuest is NOT registered in a SaveableTypeDefiner.
    /// On save, the quest is written by QuestManager but the type is unknown to the
    /// save system, so it comes back as null on load and is cleaned up by
    /// QuestManager.PreAfterLoad — no crash. AIDialogBehavior.OnGameLoaded then
    /// reconstructs the quest from NPCContext.PendingRequest (which persists via JSON).
    ///
    /// This mirrors the pattern used by MerchantDeliveryQuest in RealmsForgottenMain.
    /// </summary>
    public class AIDialogQuest : QuestBase
    {
        private static readonly Dictionary<string, AIDialogQuest> _activeByNpcId
            = new Dictionary<string, AIDialogQuest>();

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
            // Not called — quest is not persisted via SaveDefiner.
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
