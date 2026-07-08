using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RF_AIDialog
{
    internal static class RFAIDebugMenu
    {
        public static void Show()
        {
            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "RF AI Debug",
                    BuildText(),
                    true,
                    false,
                    "Close",
                    "",
                    () => { },
                    null));
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"RFAIDebugMenu.Show failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static string BuildText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("RF AI Debug Menu");
            sb.AppendLine($"External memory path: {AIMemoryStore.DirectoryPath}");
            sb.AppendLine($"Remote API: {(AIConfig.UseRemoteAPI ? "yes" : "no")}");
            sb.AppendLine($"API model: {AIConfig.APIModelName}");
            sb.AppendLine($"Local model: {AIConfig.ModelName}");
            sb.AppendLine();

            AppendConversationNpc(sb);
            AppendActiveRequests(sb);
            AppendWorldSummary(sb);

            string text = sb.ToString().TrimEnd();
            return text.Length <= 12000 ? text : text.Substring(0, 12000) + "\n\n[debug text truncated]";
        }

        private static void AppendConversationNpc(StringBuilder sb)
        {
            Hero? npc = Hero.OneToOneConversationHero;
            if (npc == null)
            {
                sb.AppendLine("Current conversation NPC: none");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("Current conversation NPC:");
            sb.AppendLine($"  name: {npc.Name}");
            sb.AppendLine($"  id: {npc.StringId}");
            sb.AppendLine($"  clan: {npc.Clan?.Name?.ToString() ?? "none"}");
            sb.AppendLine($"  faction: {npc.MapFaction?.Name?.ToString() ?? "none"}");

            NPCContext? ctx = NPCContextStore.Instance?.GetExisting(npc.StringId);
            if (ctx == null)
            {
                sb.AppendLine("  AI context: none yet");
                sb.AppendLine();
                return;
            }

            AppendContext(sb, ctx, "  ");
            sb.AppendLine();
        }

        private static void AppendActiveRequests(StringBuilder sb)
        {
            var contexts = NPCContextStore.Instance?.GetAll()
                .Where(c => c != null && c.HasPendingRequest)
                .Take(12)
                .ToList() ?? new List<NPCContext>();

            sb.AppendLine($"Active AI requests: {contexts.Count}");
            foreach (var ctx in contexts)
            {
                string npcName = ResolveHeroName(ctx.HeroId);
                sb.AppendLine($"- {npcName} ({ctx.HeroId})");
                AppendRequest(sb, ctx.PendingRequest, "  ");
            }

            if (contexts.Count == 0)
                sb.AppendLine("  none");
            sb.AppendLine();
        }

        private static void AppendWorldSummary(StringBuilder sb)
        {
            var summary = AIMemoryStore.GetSummary("world", "recent");
            if (summary == null)
                return;

            sb.AppendLine("World memory summary:");
            sb.AppendLine($"  {summary.Text}");
        }

        private static void AppendContext(StringBuilder sb, NPCContext ctx, string indent)
        {
            sb.AppendLine($"{indent}context:");
            sb.AppendLine($"{indent}  personality: {(string.IsNullOrWhiteSpace(ctx.GeneratedPersonality) ? "no" : "yes")}");
            sb.AppendLine($"{indent}  ambition: {(string.IsNullOrWhiteSpace(ctx.GeneratedAmbition) ? "no" : "yes")}");
            sb.AppendLine($"{indent}  recent history entries: {ctx.RecentHistory?.Count ?? 0}");
            sb.AppendLine($"{indent}  pending initiative: {(ctx.HasPendingInitiative ? ctx.PendingInitiativeReason : "none")}");

            var summary = AIMemoryStore.GetSummary("npc", ctx.HeroId);
            sb.AppendLine($"{indent}  memory summary: {(summary != null ? summary.Text : "none")}");

            var memories = AIMemoryStore.GetNpcMemories(ctx.HeroId, maxCount: 5, maxDays: 0);
            sb.AppendLine($"{indent}  external memories: {memories.Count}");
            foreach (var mem in memories)
                sb.AppendLine($"{indent}    [Day {mem.Day}] {mem.Text}");

            AppendRequest(sb, ctx.PendingRequest, indent);
        }

        private static void AppendRequest(StringBuilder sb, PendingRequest? request, string indent)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Description))
            {
                sb.AppendLine($"{indent}pending request: none");
                return;
            }

            sb.AppendLine($"{indent}pending request:");
            sb.AppendLine($"{indent}  issued day: {request.DayIssued}");
            sb.AppendLine($"{indent}  desc: {request.Description}");

            QuestMechanic? mechanic = request.Mechanic;
            if (mechanic == null)
            {
                sb.AppendLine($"{indent}  mechanic: none / roleplay-only");
                return;
            }

            mechanic.Normalize();
            sb.AppendLine($"{indent}  mechanic kind: {mechanic.QuestKind}");
            sb.AppendLine($"{indent}  reward: {mechanic.RewardGold} gold");
            sb.AppendLine($"{indent}  duration: {mechanic.DurationDays} days");
            sb.AppendLine($"{indent}  all completed: {mechanic.AllCompleted}");

            for (int i = 0; i < mechanic.Objectives.Count; i++)
            {
                QuestAtom atom = mechanic.Objectives[i];
                string status = mechanic.IsCompleted(i) ? "done" : "pending";
                string label = string.IsNullOrWhiteSpace(atom.Label) ? atom.AtomType : atom.Label;
                string paramText = atom.Params.Count == 0
                    ? ""
                    : " | " + string.Join(", ", atom.Params.Select(p => $"{p.Key}={p.Value}"));
                sb.AppendLine($"{indent}    [{i}] {status}: {atom.AtomType} - {label}{paramText}");
            }

            if (mechanic.Progress.Count > 0)
                sb.AppendLine($"{indent}  progress: {string.Join(", ", mechanic.Progress.Select(p => $"{p.Key}={p.Value}"))}");
        }

        private static string ResolveHeroName(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId))
                return "Unknown NPC";

            try
            {
                Hero? hero = Hero.AllAliveHeroes.FirstOrDefault(h =>
                    h != null && h.StringId.Equals(heroId, StringComparison.OrdinalIgnoreCase));
                return hero?.Name?.ToString() ?? "Unknown NPC";
            }
            catch
            {
                return "Unknown NPC";
            }
        }
    }
}
