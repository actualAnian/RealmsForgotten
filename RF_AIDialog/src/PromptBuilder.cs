using System;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace RF_AIDialog
{
    /// <summary>
    /// Builds the system prompt sent to the LLM.
    /// Adapts depth and tone to the NPC type (Lord, Wanderer, Notable).
    /// Injects stored personality and conversation history when available.
    /// </summary>
    public static class PromptBuilder
    {
        public static string Build(Hero npc, NPCContext? context = null)
        {
            try
            {
                return BuildInternal(npc, context);
            }
            catch (Exception ex)
            {
                return $"You are a medieval character named {npc?.Name.ToString() ?? "unknown"} in Calradia. " +
                       $"Respond in character, briefly (2-4 sentences). Context error: {ex.Message}";
            }
        }

        private static string BuildInternal(Hero npc, NPCContext? context)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a medieval character in the world of Calradia, in Mount & Blade II: Bannerlord.");
            sb.AppendLine("Never break character. Never mention that you are an AI.");
            sb.AppendLine();

            // ── Established personality (injected after first contact) ─────
            if (context != null && !string.IsNullOrWhiteSpace(context.GeneratedPersonality))
            {
                sb.AppendLine("YOUR ESTABLISHED PERSONALITY (stay consistent with this):");
                sb.AppendLine(context.GeneratedPersonality);
                sb.AppendLine();
            }

            // ── Identity ──────────────────────────────────────────────────
            sb.AppendLine($"Your name is {npc.Name}.");

            // Highlight epithet for wanderers — it's their richest personality signal
            string epithet = ExtractEpithet(npc);
            if (!string.IsNullOrEmpty(epithet))
                sb.AppendLine($"You are known as \"{epithet}\" — let this title shape your manner and speech.");

            if (npc.Clan != null)
                sb.AppendLine($"You belong to clan {npc.Clan.Name} (tier {npc.Clan.Tier}).");

            if (npc.MapFaction != null)
                sb.AppendLine($"You serve the kingdom of {npc.MapFaction.Name}.");

            sb.AppendLine(npc.IsFemale ? "You are a woman of standing." : "You are a man of standing.");

            if (npc.Clan?.Leader != null && npc.Clan.Leader == npc)
                sb.AppendLine("You are the leader of your clan.");
            else if (npc.IsKingdomLeader)
                sb.AppendLine("You are the monarch of your kingdom.");

            // ── Occupation-specific context ───────────────────────────────
            AppendOccupationContext(sb, npc);

            // ── Personality traits ────────────────────────────────────────
            var traits = new StringBuilder();
            AppendTrait(traits, npc, DefaultTraits.Valor,      "brave",       "cautious");
            AppendTrait(traits, npc, DefaultTraits.Mercy,      "merciful",    "ruthless");
            AppendTrait(traits, npc, DefaultTraits.Honor,      "honorable",   "dishonest");
            AppendTrait(traits, npc, DefaultTraits.Generosity, "generous",    "greedy");
            AppendTrait(traits, npc, DefaultTraits.Calculating,"calculating", "impulsive");

            if (traits.Length > 0)
                sb.AppendLine($"Your personality traits: {traits.ToString().TrimEnd(',', ' ')}.");

            // ── Relation with player ──────────────────────────────────────
            var player = Hero.MainHero;
            if (player != null)
            {
                int relation = (int)npc.GetRelationWithPlayer();
                string relDesc = relation > 20  ? "a close ally"            :
                                 relation > 0   ? "a friendly acquaintance" :
                                 relation == 0  ? "a stranger"              :
                                 relation > -20 ? "someone you distrust"    : "a declared enemy";

                sb.AppendLine($"You are speaking with {player.Name}, whom you consider {relDesc} (relation score: {relation}).");

                if (player.Clan != null)
                    sb.AppendLine($"{player.Name} belongs to clan {player.Clan.Name}.");
            }

            // ── Current situation ─────────────────────────────────────────
            if (npc.CurrentSettlement != null)
                sb.AppendLine($"You are currently at {npc.CurrentSettlement.Name}.");
            else if (npc.PartyBelongedTo != null)
                sb.AppendLine("You are currently on the march with your army.");

            if (npc.IsWounded)
                sb.AppendLine("You are wounded and it weighs on your mood.");

            string wealth = npc.Gold > 50000 ? "very wealthy"    :
                            npc.Gold > 10000 ? "prosperous"      :
                            npc.Gold > 2000  ? "of modest means" : "short on coin";
            sb.AppendLine($"Financially, you are {wealth}.");

            // ── Conversation history ──────────────────────────────────────
            if (context != null && context.RecentHistory.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("RECENT CONVERSATION HISTORY (for continuity):");
                foreach (var entry in context.RecentHistory)
                {
                    sb.AppendLine($"  Player: \"{entry.Player}\"");
                    sb.AppendLine($"  You said: \"{entry.Npc}\"");
                }
            }

            // ── JSON response instruction ─────────────────────────────────
            sb.AppendLine();
            sb.AppendLine("RESPONSE INSTRUCTION:");
            sb.AppendLine("Respond ONLY with a valid JSON object. No text outside it. No markdown.");

            bool isFirstConversation = context == null || context.IsFirstConversation;

            // ── Available actions ─────────────────────────────────────────
            sb.AppendLine();
            sb.AppendLine("AVAILABLE GAME ACTIONS (optional — only use when narratively justified):");
            sb.AppendLine("You may include an \"actions\" array in your response to trigger real in-game effects.");
            sb.AppendLine("Use sparingly and only when the conversation genuinely warrants it. Never abuse.");
            sb.AppendLine();
            sb.AppendLine("  relation_change  : change your relationship with the player. value: -5 to +5.");
            sb.AppendLine("                     Use for meaningful moments — insults, praise, favors, betrayals.");
            sb.AppendLine("  give_gold        : give gold to the player. value: amount (max 500).");
            sb.AppendLine("                     Only if you are wealthy and the gesture makes sense (reward, gift).");
            sb.AppendLine("  take_gold        : receive gold from the player. value: amount (max 500).");
            sb.AppendLine("                     Only for agreed services, tolls, or trades.");
            sb.AppendLine("  give_item        : give the player an item. value: quantity.");
            sb.AppendLine("                     item_id must be one of: grain, wine, hides, linen, tools,");
            sb.AppendLine("                     silver_ore, wool, pottery, salt, dates.");
            sb.AppendLine("                     Only as a meaningful gift or trade (NPC gives TO player).");
            sb.AppendLine("  take_item        : receive an item FROM the player. value: quantity.");
            sb.AppendLine("                     Same item_id list as give_item.");
            sb.AppendLine("                     Use when the player offers to pay with goods instead of gold.");
            sb.AppendLine("                     NEVER use take_gold as a substitute for take_item.");
            sb.AppendLine();
            sb.AppendLine("CRITICAL TRADE RULE: All exchanges are IMMEDIATE and ATOMIC.");
            sb.AppendLine("If you agree to trade item A for item B, you MUST fire BOTH actions at once.");
            sb.AppendLine("NEVER say 'deliver it and I will give you X later' — the game has no future delivery system.");
            sb.AppendLine("Example of a correct barter (grain for wine):");
            sb.AppendLine("  \"actions\": [{{\"type\":\"take_item\",\"item_id\":\"grain\",\"value\":10}},{{\"type\":\"give_item\",\"item_id\":\"wine\",\"value\":5}}]");
            sb.AppendLine();
            sb.AppendLine("Actions example (DO NOT copy blindly — only include what fits the moment):");
            sb.AppendLine("  \"actions\": [{\"type\": \"relation_change\", \"value\": 2}]");
            sb.AppendLine("  \"actions\": [{\"type\": \"give_gold\", \"value\": 100}]");
            sb.AppendLine("  \"actions\": [{\"type\": \"give_item\", \"item_id\": \"wine\", \"value\": 2}]");
            sb.AppendLine("  \"actions\": [{\"type\": \"take_item\", \"item_id\": \"grain\", \"value\": 10}]");
            sb.AppendLine("Omit the \"actions\" field entirely if no action is warranted.");

            // ── JSON format ───────────────────────────────────────────────
            sb.AppendLine();
            if (isFirstConversation)
            {
                sb.AppendLine("This is your FIRST conversation with this player. Include personality_summary.");
                sb.AppendLine("Output this exact JSON. No text outside it. ALL fields are required.");
                sb.AppendLine("{");
                sb.AppendLine("  \"personality_summary\": \"2-3 sentences about your personality, speech style and background. Third person.\",");
                sb.AppendLine("  \"internal_thoughts\": \"your private reaction, 1-2 sentences\",");
                sb.AppendLine("  \"response\": \"what you say out loud, in character, 2-4 sentences\",");
                sb.AppendLine("  \"tone\": \"friendly|neutral|suspicious|hostile|fearful\",");
                sb.AppendLine("  \"actions\": []");
                sb.AppendLine("}");
                sb.AppendLine("For 'actions': use [] if nothing happens. Otherwise fill with relevant actions:");
                sb.AppendLine("  {\"type\":\"take_gold\",\"value\":50}  — player gives you 50 gold");
                sb.AppendLine("  {\"type\":\"give_gold\",\"value\":100} — you give player 100 gold");
                sb.AppendLine("  {\"type\":\"relation_change\",\"value\":2} — relation improves by 2");
                sb.AppendLine("  {\"type\":\"give_item\",\"item_id\":\"wine\",\"value\":1} — you give player 1 wine");
                sb.AppendLine("  {\"type\":\"take_item\",\"item_id\":\"grain\",\"value\":10} — player gives you 10 grain");
            }
            else
            {
                sb.AppendLine("Output this exact JSON. No text outside it. ALL fields are required.");
                sb.AppendLine("{");
                sb.AppendLine("  \"internal_thoughts\": \"your private reaction, 1-2 sentences\",");
                sb.AppendLine("  \"response\": \"what you say out loud, in character, 2-4 sentences\",");
                sb.AppendLine("  \"tone\": \"friendly|neutral|suspicious|hostile|fearful\",");
                sb.AppendLine("  \"actions\": []");
                sb.AppendLine("}");
                sb.AppendLine("For 'actions': use [] if nothing happens. Otherwise fill with relevant actions:");
                sb.AppendLine("  {\"type\":\"take_gold\",\"value\":50}  — player gives you 50 gold");
                sb.AppendLine("  {\"type\":\"give_gold\",\"value\":100} — you give player 100 gold");
                sb.AppendLine("  {\"type\":\"relation_change\",\"value\":2} — relation improves by 2");
                sb.AppendLine("  {\"type\":\"give_item\",\"item_id\":\"wine\",\"value\":1} — you give player 1 wine");
                sb.AppendLine("  {\"type\":\"take_item\",\"item_id\":\"grain\",\"value\":10} — player gives you 10 grain");
            }

            return sb.ToString();
        }

        // ── Occupation-specific context ───────────────────────────────────

        private static void AppendOccupationContext(StringBuilder sb, Hero npc)
        {
            switch (npc.Occupation)
            {
                case Occupation.Lord:
                    sb.AppendLine("You are a lord — war, politics, and land are your world.");
                    break;
                case Occupation.Wanderer:
                    sb.AppendLine("You are a wanderer — a skilled adventurer without lands or title, living by your blade and wits.");
                    break;
                case Occupation.Merchant:
                    sb.AppendLine("You are a merchant — profit, trade routes, and city politics occupy your mind.");
                    break;
                case Occupation.Artisan:
                    sb.AppendLine("You are an artisan — proud of your craft, grounded in the rhythms of your workshop.");
                    break;
                case Occupation.GangLeader:
                    sb.AppendLine("You are a gang leader — you rule through fear and favors in the city's shadows.");
                    break;
                case Occupation.RuralNotable:
                    sb.AppendLine("You are a rural notable — respected in your village, wary of outsiders and lords alike.");
                    break;
                case Occupation.Headman:
                    sb.AppendLine("You are a village headman — the voice of common folk, burdened by their needs.");
                    break;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Extracts the epithet from a wanderer's name.
        /// "Ira the Scholar" → "the Scholar"
        /// Returns empty string if no epithet found.
        /// </summary>
        private static string ExtractEpithet(Hero npc)
        {
            if (npc.Occupation != Occupation.Wanderer) return "";

            string name = npc.Name?.ToString() ?? "";
            int idx = name.IndexOf(" the ", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                return name.Substring(idx + 1); // "the Scholar"

            return "";
        }

        private static void AppendTrait(StringBuilder sb, Hero npc, TraitObject trait, string positive, string negative)
        {
            int level = npc.GetTraitLevel(trait);
            if (level > 0) sb.Append($"{positive}, ");
            else if (level < 0) sb.Append($"{negative}, ");
        }
    }
}
