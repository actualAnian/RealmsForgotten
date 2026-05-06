using System;
using System.Collections.Generic;
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
    ///   VISIT_SETTLEMENT - OnSettlementEntered
    ///   LEAVE_SETTLEMENT - OnSettlementLeftEvent
    ///   DEFEAT_PARTY     - MobilePartyDestroyed
    ///   TALK_TO_PARTY    - ConversationEnded
    ///   BRING_ITEM       - checked on conversation start (CheckConversationAtoms)
    ///   BRING_TROOPS     - checked on conversation start (CheckConversationAtoms)
    ///   BRING_PRISONER_HERO - checked on conversation start (CheckConversationAtoms)
    ///   WIN_TOURNAMENT   - TournamentFinished
    ///   RETURN_TO_NPC    - checked on conversation start, all others complete first
    ///
    /// Persistence: completion state lives in PendingRequest.Mechanic and is
    /// JSON-serialized by NPCContextStore automatically.
    /// </summary>
    public class QuestAtomEngine : CampaignBehaviorBase
    {
        public static QuestAtomEngine? Instance { get; private set; }

        public override void RegisterEvents()
        {
            Instance = this;
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnPartyDestroyed);
            CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
            CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnGameLoaded()
        {
            RFAIDebug.Log("QuestAtomEngine: OnGameLoaded - mechanics active from NPCContextStore");
        }

        private void OnSettlementEntered(MobileParty enteredBy, Settlement settlement, Hero _hero)
        {
            if (enteredBy != MobileParty.MainParty || settlement == null)
                return;

            try
            {
                foreach (var ctx in GetContextsWithMechanics())
                {
                    if (TryCompleteSettlementObjective(ctx, settlement, "VISIT_SETTLEMENT", "Visited"))
                    {
                        CheckMechanicCompletion(ctx);
                        CheckNearCompletion(ctx);
                    }
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QuestAtomEngine.OnSettlementEntered exception: {ex.Message}");
            }
        }

        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            if (party != MobileParty.MainParty || settlement == null)
                return;

            try
            {
                foreach (var ctx in GetContextsWithMechanics())
                {
                    if (TryCompleteSettlementObjective(ctx, settlement, "LEAVE_SETTLEMENT", "Left"))
                    {
                        CheckMechanicCompletion(ctx);
                        CheckNearCompletion(ctx);
                    }
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QuestAtomEngine.OnSettlementLeft exception: {ex.Message}");
            }
        }

        private void OnPartyDestroyed(MobileParty destroyed, PartyBase destroyerBase)
        {
            if (destroyed == null)
                return;

            var destroyer = destroyerBase?.MobileParty;
            bool playerInvolved = destroyer == MobileParty.MainParty
                || destroyer?.LeaderHero == Hero.MainHero
                || (MobileParty.MainParty?.Army != null && destroyer?.Army == MobileParty.MainParty.Army);

            if (!playerInvolved)
                return;

            string destroyedFaction = destroyed.MapFaction?.StringId ?? "";
            string destroyedPartyId = destroyed.StringId ?? "";
            string destroyedHeroId = destroyed.LeaderHero?.StringId ?? "";

            try
            {
                foreach (var ctx in GetContextsWithMechanics())
                {
                    var mechanic = ctx.PendingRequest?.Mechanic;
                    if (mechanic == null)
                        continue;

                    int idx = mechanic.IndexOfFirstIncomplete("DEFEAT_PARTY");
                    if (idx < 0)
                        continue;

                    var atom = mechanic.Objectives[idx];
                    string targetFaction = atom.GetParam("faction_id");
                    string targetPartyId = atom.GetParam("party_id");
                    string targetHeroId = atom.GetParam("hero_id");

                    bool factionMatches = string.IsNullOrWhiteSpace(targetFaction) ||
                        destroyedFaction.Equals(targetFaction, StringComparison.OrdinalIgnoreCase);
                    bool partyMatches = string.IsNullOrWhiteSpace(targetPartyId) ||
                        destroyedPartyId.Equals(targetPartyId, StringComparison.OrdinalIgnoreCase);
                    bool heroMatches = string.IsNullOrWhiteSpace(targetHeroId) ||
                        destroyedHeroId.Equals(targetHeroId, StringComparison.OrdinalIgnoreCase);

                    if (!factionMatches || !partyMatches || !heroMatches)
                        continue;

                    int required = atom.GetParamInt("count", 1);
                    string progressKey = $"defeat_{idx}";
                    mechanic.IncrementProgress(progressKey);
                    int current = mechanic.GetProgress(progressKey);

                    string label = atom.Label.Length > 0
                        ? atom.Label
                        : $"Defeat {atom.GetParam("faction_name", "enemies")} parties";

                    if (current >= required)
                    {
                        mechanic.MarkCompleted(idx);
                        NPCContextStore.Instance?.MarkDirty(ctx);
                        RFAIDebug.Log($"QuestAtomEngine: DEFEAT_PARTY completed for {ctx.HeroId} ({current}/{required})");
                        UpdateQuestLog(ctx.HeroId, $"✓ {label}");
                        Notify($"Quest objective: ✓ {label}");
                        CheckMechanicCompletion(ctx);
                        CheckNearCompletion(ctx);
                    }
                    else
                    {
                        NPCContextStore.Instance?.MarkDirty(ctx);
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

        private void OnConversationEnded(IEnumerable<CharacterObject> _)
        {
            try
            {
                MobileParty? conversationParty = MobileParty.ConversationParty;
                Hero? conversationHero = Hero.OneToOneConversationHero;

                if (conversationParty == null && conversationHero == null)
                    return;

                foreach (var ctx in GetContextsWithMechanics())
                {
                    if (TryProgressTalkToPartyObjective(ctx, conversationParty, conversationHero))
                    {
                        CheckMechanicCompletion(ctx);
                        CheckNearCompletion(ctx);
                    }
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QuestAtomEngine.OnConversationEnded exception: {ex.Message}");
            }
        }

        private void OnTournamentFinished(
            CharacterObject winner,
            MBReadOnlyList<CharacterObject> _participants,
            Town town,
            ItemObject _prize)
        {
            try
            {
                if (winner?.HeroObject != Hero.MainHero || town?.Settlement == null)
                    return;

                foreach (var ctx in GetContextsWithMechanics())
                {
                    if (TryProgressTournamentObjective(ctx, town.Settlement))
                    {
                        CheckMechanicCompletion(ctx);
                        CheckNearCompletion(ctx);
                    }
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QuestAtomEngine.OnTournamentFinished exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Called by AIDialogBehavior when the player opens a conversation with an NPC.
        /// Handles conversation-time atoms that must react immediately while the NPC is still active.
        /// </summary>
        public bool CheckConversationAtoms(Hero npc, NPCContext ctx)
        {
            if (npc == null || ctx?.PendingRequest?.Mechanic == null)
                return false;

            var mechanic = ctx.PendingRequest.Mechanic;
            mechanic.Normalize();
            bool anyNew = false;

            for (int i = 0; i < mechanic.Objectives.Count; i++)
            {
                if (mechanic.Completed[i])
                    continue;

                var atom = mechanic.Objectives[i];
                switch (atom.AtomType)
                {
                    case "BRING_ITEM":
                        if (CheckBringItem(atom))
                        {
                            mechanic.MarkCompleted(i);
                            anyNew = true;
                            LogConversationAtom(ctx.HeroId, atom, "BRING_ITEM");
                        }
                        break;

                    case "BRING_TROOPS":
                        if (CheckBringTroops(atom))
                        {
                            mechanic.MarkCompleted(i);
                            anyNew = true;
                            LogConversationAtom(ctx.HeroId, atom, "BRING_TROOPS");
                        }
                        break;

                    case "BRING_PRISONER_HERO":
                        if (CheckBringPrisonerHero(atom))
                        {
                            mechanic.MarkCompleted(i);
                            anyNew = true;
                            LogConversationAtom(ctx.HeroId, atom, "BRING_PRISONER_HERO");
                        }
                        break;

                    case "RETURN_TO_NPC":
                        bool allPriorDone = true;
                        for (int j = 0; j < i; j++)
                        {
                            if (!mechanic.Completed[j])
                            {
                                allPriorDone = false;
                                break;
                            }
                        }
                        if (allPriorDone)
                        {
                            mechanic.MarkCompleted(i);
                            anyNew = true;
                            RFAIDebug.Log($"QuestAtomEngine: RETURN_TO_NPC complete for {ctx.HeroId}");
                        }
                        break;

                    case "TALK_TO_PARTY":
                        if (TryProgressTalkToPartyObjective(ctx, MobileParty.ConversationParty, npc))
                            anyNew = true;
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

        private IEnumerable<NPCContext> GetContextsWithMechanics()
        {
            return NPCContextStore.Instance?.GetAll()
                       ?.Where(x => x.PendingRequest?.Mechanic != null)
                   ?? Enumerable.Empty<NPCContext>();
        }

        private bool TryCompleteSettlementObjective(
            NPCContext ctx,
            Settlement settlement,
            string atomType,
            string fallbackVerb)
        {
            var mechanic = ctx.PendingRequest?.Mechanic;
            if (mechanic == null)
                return false;

            bool changed = false;
            mechanic.Normalize();

            for (int i = 0; i < mechanic.Objectives.Count; i++)
            {
                if (mechanic.Completed[i])
                    continue;

                var atom = mechanic.Objectives[i];
                if (!MatchesSettlementAtom(atom, atomType))
                    continue;

                string targetId = atom.GetParam("settlement_id");
                if (string.IsNullOrWhiteSpace(targetId) ||
                    !settlement.StringId.Equals(targetId, StringComparison.OrdinalIgnoreCase))
                    continue;

                mechanic.MarkCompleted(i);
                NPCContextStore.Instance?.MarkDirty(ctx);

                string label = atom.Label.Length > 0 ? atom.Label : $"{fallbackVerb} {settlement.Name}";
                RFAIDebug.Log($"QuestAtomEngine: {atomType} completed for {ctx.HeroId} - {label}");
                UpdateQuestLog(ctx.HeroId, $"✓ {label}");
                Notify($"Quest objective: ✓ {label}");
                changed = true;
            }

            return changed;
        }

        private static bool MatchesSettlementAtom(QuestAtom atom, string atomType)
        {
            if (atom.AtomType.Equals(atomType, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!atomType.Equals("LEAVE_SETTLEMENT", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!atom.AtomType.Equals("VISIT_SETTLEMENT", StringComparison.OrdinalIgnoreCase))
                return false;

            string trigger = atom.GetParam("trigger", atom.GetParam("event", atom.GetParam("mode", "")));
            if (trigger.Equals("leave", StringComparison.OrdinalIgnoreCase) ||
                trigger.Equals("exit", StringComparison.OrdinalIgnoreCase))
                return true;

            string label = atom.Label ?? "";
            return label.IndexOf("leave", StringComparison.OrdinalIgnoreCase) >= 0
                || label.IndexOf("exit", StringComparison.OrdinalIgnoreCase) >= 0
                || label.IndexOf("sair", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool TryProgressTalkToPartyObjective(
            NPCContext ctx,
            MobileParty? conversationParty,
            Hero? conversationHero)
        {
            var mechanic = ctx.PendingRequest?.Mechanic;
            if (mechanic == null || conversationParty == null)
                return false;

            bool changed = false;
            string currentFaction = conversationParty.MapFaction?.StringId ?? "";
            string currentPartyId = conversationParty.StringId ?? "";
            string currentHeroId = conversationHero?.StringId ?? "";

            mechanic.Normalize();

            for (int i = 0; i < mechanic.Objectives.Count; i++)
            {
                if (mechanic.Completed[i])
                    continue;

                var atom = mechanic.Objectives[i];
                if (!atom.AtomType.Equals("TALK_TO_PARTY", StringComparison.OrdinalIgnoreCase))
                    continue;

                string targetFaction = atom.GetParam("faction_id");
                string targetPartyId = atom.GetParam("party_id");
                string targetHeroId = atom.GetParam("hero_id");

                if (!string.IsNullOrWhiteSpace(targetFaction) &&
                    !targetFaction.Equals(currentFaction, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(targetPartyId) &&
                    !targetPartyId.Equals(currentPartyId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(targetHeroId) &&
                    !targetHeroId.Equals(currentHeroId, StringComparison.OrdinalIgnoreCase))
                    continue;

                string seenKey = $"talk_seen_{i}";
                string progressKey = $"talk_count_{i}";
                string uniqueTargetId = !string.IsNullOrWhiteSpace(currentPartyId)
                    ? currentPartyId
                    : (!string.IsNullOrWhiteSpace(currentHeroId) ? currentHeroId : $"{currentFaction}_{i}");

                if (mechanic.HasSeenTarget(seenKey, uniqueTargetId))
                    continue;

                mechanic.MarkTargetSeen(seenKey, uniqueTargetId);
                mechanic.IncrementProgress(progressKey);

                int current = mechanic.GetProgress(progressKey);
                int required = atom.GetParamInt("count", 1);
                string label = atom.Label.Length > 0
                    ? atom.Label
                    : $"Talk to {required} parties";

                NPCContextStore.Instance?.MarkDirty(ctx);

                if (current >= required)
                {
                    mechanic.MarkCompleted(i);
                    RFAIDebug.Log($"QuestAtomEngine: TALK_TO_PARTY completed for {ctx.HeroId} ({current}/{required})");
                    UpdateQuestLog(ctx.HeroId, $"✓ {label}");
                    Notify($"Quest objective: ✓ {label}");
                }
                else
                {
                    RFAIDebug.Log($"QuestAtomEngine: TALK_TO_PARTY progress {current}/{required} for {ctx.HeroId}");
                    UpdateQuestLog(ctx.HeroId, $"Progress: {label} ({current}/{required})");
                    Notify($"Quest progress: {label} ({current}/{required})");
                }

                changed = true;
            }

            return changed;
        }

        private void CheckMechanicCompletion(NPCContext ctx)
        {
            var mechanic = ctx.PendingRequest?.Mechanic;
            if (mechanic == null || !mechanic.AllCompleted)
                return;

            RFAIDebug.Log($"QuestAtomEngine: ALL objectives complete for {ctx.HeroId}");

            if (mechanic.HasReturnStep)
            {
                NPCContextStore.Instance?.MarkDirty(ctx);
                Notify("All objectives complete - return to the NPC to collect your reward.");
                RFAIDebug.Log($"QuestAtomEngine: RETURN_TO_NPC pending for {ctx.HeroId} - closure deferred to conversation");
                return;
            }

            var quest = AIDialogQuest.ForNpc(ctx.HeroId);
            if (quest != null && quest.IsOngoing)
            {
                quest.MarkFulfilled();
                RFAIDebug.Log($"QuestAtomEngine: auto-fulfilled for {ctx.HeroId}");
            }

            if (mechanic.RewardGold > 0)
            {
                try
                {
                    Hero.MainHero?.ChangeHeroGold(mechanic.RewardGold);
                    RFAIDebug.Log($"QuestAtomEngine: auto-rewarded {mechanic.RewardGold} gold");
                    Notify($"Quest complete! You received {mechanic.RewardGold} gold.");
                }
                catch { }
            }
            else
            {
                Notify("Quest complete!");
            }

            ctx.PendingRequest = null;
            NPCContextStore.Instance?.MarkDirty(ctx);
        }

        private static void CheckNearCompletion(NPCContext ctx)
        {
            var mechanic = ctx.PendingRequest?.Mechanic;
            if (mechanic == null || mechanic.AllCompleted || !mechanic.HasReturnStep || !mechanic.AllExceptReturnCompleted)
                return;

            Notify("All objectives done - return to the NPC to collect your reward.");
            RFAIDebug.Log($"QuestAtomEngine: all pre-return objectives done for {ctx.HeroId}");
        }

        private static bool CheckBringItem(QuestAtom atom)
        {
            string itemId = atom.GetParam("item_id");
            int required = atom.GetParamInt("quantity", 1);
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            try
            {
                var party = MobileParty.MainParty;
                if (party?.ItemRoster == null)
                    return false;

                int total = 0;
                foreach (var element in party.ItemRoster)
                {
                    if (string.Equals(
                            element.EquipmentElement.Item?.StringId,
                            itemId,
                            StringComparison.OrdinalIgnoreCase))
                        total += element.Amount;
                }
                return total >= required;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckBringTroops(QuestAtom atom)
        {
            int required = atom.GetParamInt("troop_count", 1);
            try
            {
                return MobileParty.MainParty?.MemberRoster?.TotalHealthyCount >= required;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckBringPrisonerHero(QuestAtom atom)
        {
            string heroId = atom.GetParam("hero_id");
            string factionId = atom.GetParam("faction_id");
            if (string.IsNullOrWhiteSpace(heroId))
                return false;

            try
            {
                var party = MobileParty.MainParty;
                if (party?.PrisonRoster == null)
                    return false;

                foreach (var prisoner in party.PrisonRoster.GetTroopRoster())
                {
                    var prisonerHero = prisoner.Character?.HeroObject;
                    if (prisonerHero == null)
                        continue;

                    if (!heroId.Equals(prisonerHero.StringId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!string.IsNullOrWhiteSpace(factionId) &&
                        !string.Equals(prisonerHero.MapFaction?.StringId, factionId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return true;
                }
            }
            catch { }

            return false;
        }

        private bool TryProgressTournamentObjective(NPCContext ctx, Settlement tournamentSettlement)
        {
            var mechanic = ctx.PendingRequest?.Mechanic;
            if (mechanic == null)
                return false;

            bool changed = false;
            mechanic.Normalize();

            for (int i = 0; i < mechanic.Objectives.Count; i++)
            {
                if (mechanic.Completed[i])
                    continue;

                var atom = mechanic.Objectives[i];
                if (!atom.AtomType.Equals("WIN_TOURNAMENT", StringComparison.OrdinalIgnoreCase))
                    continue;

                string townId = atom.GetParam("town_id");
                if (!string.IsNullOrWhiteSpace(townId) &&
                    !string.Equals(tournamentSettlement.StringId, townId, StringComparison.OrdinalIgnoreCase))
                    continue;

                int required = atom.GetParamInt("count", 1);
                string progressKey = $"tournament_{i}";
                mechanic.IncrementProgress(progressKey);
                int current = mechanic.GetProgress(progressKey);

                string label = atom.Label.Length > 0
                    ? atom.Label
                    : $"Win {required} tournaments";

                NPCContextStore.Instance?.MarkDirty(ctx);

                if (current >= required)
                {
                    mechanic.MarkCompleted(i);
                    RFAIDebug.Log($"QuestAtomEngine: WIN_TOURNAMENT completed for {ctx.HeroId} ({current}/{required})");
                    UpdateQuestLog(ctx.HeroId, $"âœ“ {label}");
                    Notify($"Quest objective: âœ“ {label}");
                }
                else
                {
                    RFAIDebug.Log($"QuestAtomEngine: WIN_TOURNAMENT progress {current}/{required} for {ctx.HeroId}");
                    UpdateQuestLog(ctx.HeroId, $"Progress: {label} ({current}/{required})");
                    Notify($"Quest progress: {label} ({current}/{required})");
                }

                changed = true;
            }

            return changed;
        }

        private static void LogConversationAtom(string npcId, QuestAtom atom, string atomType)
        {
            string label = atom.Label.Length > 0 ? atom.Label : atomType;
            RFAIDebug.Log($"QuestAtomEngine: {atomType} complete via conversation for {npcId}");
            UpdateQuestLog(npcId, $"✓ {label}");
            Notify($"Quest objective: ✓ {label}");
        }

        private static void UpdateQuestLog(string npcId, string message)
        {
            try
            {
                var quest = AIDialogQuest.ForNpc(npcId);
                quest?.AddObjectiveLog(message);
            }
            catch { }
        }

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
