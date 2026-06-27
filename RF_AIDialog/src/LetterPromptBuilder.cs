using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;

namespace RF_AIDialog
{
    public static class LetterPromptBuilder
    {
        public static string BuildInitiativePrompt(Hero sender, NPCContext? context, string reason)
        {
            var sb = new StringBuilder();
            AppendSharedHeader(sb, sender, Hero.MainHero, context);
            sb.AppendLine("FORMAT:");
            sb.AppendLine("You are writing a medieval letter or messenger speech to the player.");
            sb.AppendLine("You are not meeting in person. Keep it grounded, diegetic, and suitable for delayed travel.");
            sb.AppendLine("The letter should be 2-5 sentences and feel like a real in-world message.");
            sb.AppendLine("If the matter can wait for correspondence, keep escalate_to_meeting false.");
            sb.AppendLine("Return JSON only:");
            sb.AppendLine("{\"message_text\":\"...\",\"topic\":\"war_warning\",\"requires_reply\":true,\"escalate_to_meeting\":false}");
            sb.AppendLine();
            sb.AppendLine("YOUR REASON FOR WRITING:");
            sb.AppendLine(reason);
            return sb.ToString();
        }

        public static string BuildReplyPrompt(
            Hero sender,
            NPCContext? context,
            string playerReply,
            List<AIMessageRecord> threadHistory)
        {
            var sb = new StringBuilder();
            AppendSharedHeader(sb, sender, Hero.MainHero, context);
            sb.AppendLine("FORMAT:");
            sb.AppendLine("You are replying by messenger to the player's previous letter.");
            sb.AppendLine("Keep the reply 2-5 sentences, in character, and suitable for delayed correspondence.");
            sb.AppendLine("Only demand a physical meeting if the subject truly requires it.");
            sb.AppendLine("Return JSON only:");
            sb.AppendLine("{\"message_text\":\"...\",\"topic\":\"diplomacy\",\"requires_reply\":true,\"escalate_to_meeting\":false}");
            sb.AppendLine();
            if (threadHistory != null && threadHistory.Count > 0)
            {
                sb.AppendLine("LETTER THREAD SO FAR:");
                int skip = Math.Max(0, threadHistory.Count - 6);
                foreach (var message in threadHistory.Skip(skip))
                {
                    string senderName = string.IsNullOrWhiteSpace(message.SenderName) ? message.SenderHeroId : message.SenderName;
                    sb.AppendLine($"- {senderName}: {message.MessageText}");
                }
                sb.AppendLine();
            }
            sb.AppendLine("PLAYER'S NEW LETTER:");
            sb.AppendLine(playerReply);
            return sb.ToString();
        }

        private static void AppendSharedHeader(StringBuilder sb, Hero sender, Hero? recipient, NPCContext? context)
        {
            sb.AppendLine("You are a character in the medieval fantasy world of Aeurth.");
            sb.AppendLine("Never mention AI, prompts, JSON, or modern communication.");
            sb.AppendLine("Do not write as if this were instant chat. This is a delayed message carried by messenger.");
            sb.AppendLine();
            sb.AppendLine($"WRITER: {sender?.Name?.ToString() ?? "Unknown"}");
            sb.AppendLine($"WRITER ROLE: {sender?.Occupation}");
            sb.AppendLine($"RECIPIENT: {recipient?.Name?.ToString() ?? "the player"}");
            try
            {
                int relation = (int)(sender?.GetRelationWithPlayer() ?? 0f);
                sb.AppendLine($"RELATION TO PLAYER: {relation}");
            }
            catch { }

            if (context != null)
            {
                if (!string.IsNullOrWhiteSpace(context.GeneratedPersonality))
                {
                    sb.AppendLine();
                    sb.AppendLine("ESTABLISHED PERSONALITY:");
                    sb.AppendLine(context.GeneratedPersonality);
                }

                if (!string.IsNullOrWhiteSpace(context.GeneratedAmbition))
                {
                    sb.AppendLine();
                    sb.AppendLine("DEEPEST AMBITION:");
                    sb.AppendLine(context.GeneratedAmbition);
                }
            }

            try
            {
                string dynamicState = WorldContext.BuildDynamicState(sender);
                if (!string.IsNullOrWhiteSpace(dynamicState))
                {
                    sb.AppendLine();
                    sb.AppendLine(dynamicState);
                }
            }
            catch { }

            try
            {
                List<AIMemoryRecord> memories = AIMemoryStore.GetNpcMemories(sender, maxCount: 4, maxDays: 0);
                if (memories.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("THINGS YOU REMEMBER ABOUT THE PLAYER:");
                    foreach (var memory in memories)
                        sb.AppendLine($"- {memory.Text}");
                }
            }
            catch { }

            sb.AppendLine();
        }
    }
}
