using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RF_AIDialog
{
    /// <summary>
    /// Listens to campaign events and marks QuestAtom objectives complete
    /// when the real game state satisfies them.
    ///
    /// Supported atoms:
    ///   VISIT_SETTLEMENT — OnSettlementEntered
    ///   DEFEAT_PARTY     — MobilePartyDestroyed
    ///   BRING_ITEM       — checked on conversation start (CheckConversationAtoms)
    ///   BRING_TROOPS     — checked on conversation start (CheckConversationAtoms)
    ///   RETURN_TO_NPC    — checked on conversation start, all others complete first
    ///
    /// Persistence: completion state lives in PendingRequest.Mechanic.Completed
    /// which is JSON-serialized by NPCContextStore automatically.
    /// </summary>
    public class QuestAtomEngine : CampaignBehaviorBase
    {
        public static QuestAtomEngine? Instance { get; private set; }

        public override void RegisterEvents()
        {
            Instance = this;
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                this, OnGameLoaded);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(
                this, OnSettlementEntered);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(
                this, OnPartyDestroyed);
        }

        public override void SyncData(IDataStore dataStore) { }

        // ── Event handlers ────────────────────────────────────────────────

        private void OnGameLoaded()
        {
            // State is already loaded from NPCContextStore JSON.
            // Nothing extra needed — atoms re-activate on next relevant event.
            RFAIDebug.Log("QuestAtomEngine: OnGameLoaded — mechanics active from NPCContextStore");
        }

        private void OnSettlementEntered(MobileParty enteredBy, Settlement settlement, Hero _hero)
        {
            if (enteredBy != MobileParty.MainParty) return;
            if (settlement == null) return;

            try
            {
                var store = NPCContextStore.Instance;
                if (store == null) return;

                foreach (var ctx in store.GetAll())
                {
                    var mechanic = ctx.PendingRequest?.Mechanic;
                    if (mechanic == null) continue;

                    int idx = mechanic.IndexOfFirstIncomplete("VISIT_SETTLEMENT");
                    if (idx < 0) continue;

                    var atom = mechanic.Objectives[idx];
                    string targetId = atom.GetParam("settlement_id");
                    if (string.IsNullOrWhiteSpace(targetId)) continue;
                    if (!settlement.StringId.Equals(targetId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    mechanic.MarkCompleted(idx);
                    store.MarkDirty(ctx);

                    string label = atom.Label.Length > 0 ? atom.Label : $"Visited {settlement.Name}";
                    RFAIDebug.Log($"QuestAtomEngine: VISIT_SETTLEMENT completed for {ctx.HeroId} — {label}");

                    // Add journal entry to the active quest
                    UpdateQuestLog(ctx.HeroId, $"✓ {label}");

                    // Notify player
                    Notify($"Quest objective: {label}");

                    // Check if all done
                    CheckMechanicCompletion(ctx);
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QuestAtomEngine.OnSettlementEntered exception: {ex.Message}");
            }
        }

        private void OnPartyDestroyed(MobileParty destroyed, PartyBase destroyerBase)
        {
            if (destroyed == null) return;

            // Only count if main party was the destroyer (or part of the winning side)
            var destroyer = destroyerBase?.MobileParty;
            bool playerInvolved = destroyer == MobileParty.MainParty
                || destroyer?.LeaderHero == Hero.MainHero
                || (MobileParty.MainParty?.Army != null
                    && destroyer?.Army == MobileParty.MainParty.Army);

            if (!playerInvolved) return;

            string destroyedFaction = destroyed.MapFaction?.StringId ?? "";
            if (string.IsNullOrWhiteSpace(destroyedFaction)) return;

            try
            {
                var store = NPCContextStore.Instance;
                if (store == null) return;

                foreach (var ctx in store.GetAll())
                {
                    var mechanic = ctx.PendingRequest?.Mechanic;
                    if (mechanic == null) continue;

                    int idx = mechanic.IndexOfFirstIncomplete("DEFEAT_PARTY");
                    if (idx < 0) continue;

                    var atom = mechanic.Objectives[idx];
                    string targetFaction = atom.GetParam("faction_id");
                    if (string.IsNullOrWhiteSpace(targetFaction)) continue;
                    if (!destroyedFaction.Equals(targetFaction, StringComparison.OrdinalIgnoreCase))
                        continue;

                    int required = atom.GetParamInt("count", 1);
                    string progressKey = $"defeat_{idx}";
                    mechanic.IncrementProgress(progressKey);
                    int current = mechanic.GetProgress(progressKey);

                    string label = atom.Label.Length > 0 ? atom.Label
                        : $"Defeat {atom.GetParam("faction_name", "enemies")} parties";

                    if (current >= required)
                    {
                        mechanic.MarkCompleted(idx);
                        store.MarkDirty(ctx);
                        RFAIDebug.Log($"QuestAtomEngine: DEFEAT_PARTY completed for {ctx.HeroId} ({current}/{required})");
                        UpdateQuestLog(ctx.HeroId, $"✓ {label}");
                        Notify($"Quest objective: {label}");
                        CheckMechanicCompletion(ctx);
                    }
                    else
                    {
                        store.MarkDirty(ctx);
                        RFAIDebug.Log($"QuestAtomEngine: DEFEAT_PARTY progress {current}/{required} for {ctx.HeroId}");
                        UpdateQuestLog(ctx.HeroId, $"Progress: {label} ({current}/{required})");
                    }
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QuestAtomEngine.OnPartyDestroyed exception: {ex.Message}");
            }
        }

        // ── Conversation-based atom check ─────────────────────────────────

        /// <summary>
        /// Called by AIDialogBehavior when the player opens a conversation
        /// with the NPC who issued the mechanic quest.
        ///
        /// Checks BRING_ITEM, BRING_TROOPS in the player's party right now.
        /// Checks RETURN_TO_NPC when all prior objectives are satisfied.
        ///
        /// Returns true if any new objective was marked complete.
        /// </summary>
        public bool CheckConversationAtoms(Hero npc, NPCContext ctx)
        {
            if (npc == null || ctx?.PendingRequest?.Mechanic == null) return false;

            var mechanic = ctx.PendingRequest.Mechanic;
            mechanic.Normalize();
            bool anyNew = false;

            for (int i = 0; i < mechanic.Objectives.Count; i++)
            {
                if (mechanic.Completed[i]) continue;
                var atom = mechanic.Objectives[i];

                switch (atom.AtomType)
                {
                    case "BRING_ITEM":
                        if (CheckBringItem(atom))
                        {
                            mechanic.MarkCompleted(i);
                            anyNew = true;
                            RFAIDebug.Log($"QuestAtomEngine: BRING_ITEM complete via conversation for {ctx.HeroId}");
                        }
                        break;

                    case "BRING_TROOPS":
                        if (CheckBringTroops(atom))
                        {
                            mechanic.MarkCompleted(i);
                            anyNew = true;
                            RFAIDebug.Log($"QuestAtomEngine: BRING_TROOPS complete via conversation for {ctx.HeroId}");
                        }
                        break;

                    case "RETURN_TO_NPC":
                        // All prior objectives must be done
                        bool allPriorDone = true;
                        for (int j = 0; j < i; j++)
                        {
                            if (!mechanic.Completed[j]) { allPriorDone = false; break; }
                        }
                        if (allPriorDone)
                        {
                            mechanic.MarkCompleted(i);
                            anyNew = true;
                            RFAIDebug.Log($"QuestAtomEngine: RETURN_TO_NPC complete for {ctx.HeroId}");
                        }
                        break;
                }
            }

            if (anyNew)
            {
                NPCContextStore.Instance?.MarkDirty(ctx);
                CheckMechanicCompletion(ctx);
            }

            return anyNew;
        }

        // ── Completion ────────────────────────────────────────────────────

        private void CheckMechanicCompletion(NPCContext ctx)
        {
            var mechanic = ctx.PendingRequest?.Mechanic;
            if (mechanic == null || !mechanic.AllCompleted) return;

            RFAIDebug.Log($"QuestAtomEngine: ALL objectives complete for {ctx.HeroId}");

            // Mark the AIDialogQuest fulfilled
            var quest = AIDialogQuest.ForNpc(ctx.HeroId);
            if (quest != null && quest.IsOngoing)
            {
                quest.MarkFulfilled();
                RFAIDebug.Log($"QuestAtomEngine: AIDialogQuest fulfilled for {ctx.HeroId}");
            }

            // Reward gold if specified
            if (mechanic.RewardGold > 0)
            {
                try
                {
                    Hero.MainHero?.ChangeHeroGold(mechanic.RewardGold);
                    RFAIDebug.Log($"QuestAtomEngine: rewarded {mechanic.RewardGold} gold");
                    Notify($"Quest complete! You received {mechanic.RewardGold} gold.");
                }
                catch { }
            }
            else
            {
                Notify("All objectives complete — speak to the NPC to collect your reward.");
            }
        }

        // ── Inventory checks ──────────────────────────────────────────────

        private static bool CheckBringItem(QuestAtom atom)
        {
            string itemId   = atom.GetParam("item_id");
            int    required = atom.GetParamInt("quantity", 1);
            if (string.IsNullOrWhiteSpace(itemId)) return false;

            try
            {
                var party = MobileParty.MainParty;
                if (party?.ItemRoster == null) return false;

                int total = 0;
                foreach (var element in party.ItemRoster)
                {
                    if (element.EquipmentElement.Item?.StringId
                            ?.IndexOf(itemId, StringComparison.OrdinalIgnoreCase) >= 0)
                        total += element.Amount;
                }
                return total >= required;
            }
            catch { return false; }
        }

        private static bool CheckBringTroops(QuestAtom atom)
        {
            int required = atom.GetParamInt("troop_count", 1);
            try
            {
                return MobileParty.MainParty?.MemberRoster?.TotalHealthyCount >= required;
            }
            catch { return false; }
        }

        // ── Quest log update ──────────────────────────────────────────────

        private static void UpdateQuestLog(string npcId, string message)
        {
            try
            {
                var quest = AIDialogQuest.ForNpc(npcId);
                quest?.AddObjectiveLog(message);
            }
            catch { }
        }

        // ── Notification ──────────────────────────────────────────────────

        private static void Notify(string msg)
        {
            try
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"📋 {msg}", Color.FromUint(0xFF_C8_E6_C9u)));
            }
            catch { }
        }
    }
}
