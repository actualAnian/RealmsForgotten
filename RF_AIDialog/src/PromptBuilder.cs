using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;

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

            // ── World: Aeurth lore (static) ───────────────────────────────
            sb.AppendLine("WORLD LORE:");
            sb.AppendLine(WorldContext.StaticLore);
            sb.AppendLine();

            // ── Current world events (dynamic) ────────────────────────────
            string dynamicState = WorldContext.BuildDynamicState(npc);
            if (!string.IsNullOrWhiteSpace(dynamicState))
                sb.AppendLine(dynamicState);

            sb.AppendLine("You are a character living in the world of Aeurth.");
            sb.AppendLine("Never break character. Never mention that you are an AI.");
            sb.AppendLine();
            sb.AppendLine("BARTER RULE — READ THIS FIRST:");
            sb.AppendLine("When you accept a trade offer, the exchange happens ON THE SPOT. No inspection phase. No 'bring it and I will pay later'.");
            sb.AppendLine("Accepting a barter REQUIRES both actions in the SAME response:");
            sb.AppendLine("  {\"type\":\"take_item\",\"item_id\":\"grain\",\"value\":10}  <- you receive this");
            sb.AppendLine("  {\"type\":\"give_item\",\"item_id\":\"wine\",\"value\":5}   <- player receives this");
            sb.AppendLine("If you are not ready to give your goods immediately, REFUSE the offer. Do NOT take items and promise delivery later.");
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

            // ── Companion context ─────────────────────────────────────────
            if (npc.Occupation == TaleWorlds.CampaignSystem.Occupation.Wanderer
                && npc.PartyBelongedTo == MobileParty.MainParty)
            {
                sb.AppendLine($"You are a companion traveling with {Hero.MainHero?.Name}'s party.");
                sb.AppendLine("You are loyal but have your own opinions. Speak as a trusted ally, not a servant.");
                sb.AppendLine("You may push back, give advice, or express concerns — you are not blindly obedient.");

                // Current party role if assigned
                var party = MobileParty.MainParty;
                string? currentRole =
                    party.EffectiveEngineer      == npc ? "Engineer"      :
                    party.EffectiveScout         == npc ? "Scout"         :
                    party.EffectiveSurgeon       == npc ? "Surgeon"       :
                    party.EffectiveQuartermaster == npc ? "Quartermaster" : null;

                if (currentRole != null)
                    sb.AppendLine($"Your current party role is: {currentRole}.");

                // Top skills — companions should speak with authority about their expertise
                AppendCompanionSkills(sb, npc);

                // Party state — gives companions situational awareness
                AppendPartyState(sb, party);
            }

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
            bool isCompanion = npc.Occupation == TaleWorlds.CampaignSystem.Occupation.Wanderer
                               && npc.PartyBelongedTo == MobileParty.MainParty;

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

            if (isCompanion)
            {
                sb.AppendLine("  assign_role      : accept a role in the player's party. role: one of:");
                sb.AppendLine("                     \"engineer\", \"scout\", \"surgeon\", \"quartermaster\".");
                sb.AppendLine("                     Use ONLY when the player explicitly asks you to take a role.");
                sb.AppendLine("                     Example: {\"type\":\"assign_role\",\"role\":\"surgeon\"}");
            }

            bool isLordWithParty = npc.Occupation == TaleWorlds.CampaignSystem.Occupation.Lord
                                   && npc.PartyBelongedTo != null
                                   && npc.PartyBelongedTo.MemberRoster.TotalManCount > 5;
            if (isLordWithParty)
            {
                sb.AppendLine("  give_troops      : transfer some of your soldiers to the player. value: count (max 20).");
                sb.AppendLine("                     Use only as reward, mercenary deal, or significant alliance gesture.");
                sb.AppendLine("                     Example: {\"type\":\"give_troops\",\"value\":10}");
            }

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
                sb.AppendLine();
                sb.AppendLine("BARTER CONFIRMATION EXAMPLE — follow this pattern exactly when a trade is agreed:");
                sb.AppendLine("Player says: \"ok deal, 3 grain for 1 wine\"");
                sb.AppendLine("You respond: {\"personality_summary\":\"...\",...,\"actions\":[{\"type\":\"take_item\",\"item_id\":\"grain\",\"value\":3},{\"type\":\"give_item\",\"item_id\":\"wine\",\"value\":1}]}");
                sb.AppendLine("BOTH actions fire at the same time. Never only one.");
                sb.AppendLine();
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
                sb.AppendLine("BARTER CONFIRMATION EXAMPLE — follow this pattern exactly when a trade is agreed:");
                sb.AppendLine("Player says: \"ok deal, 3 grain for 1 wine\"");
                sb.AppendLine("You respond: {\"internal_thoughts\":\"...\",...,\"actions\":[{\"type\":\"take_item\",\"item_id\":\"grain\",\"value\":3},{\"type\":\"give_item\",\"item_id\":\"wine\",\"value\":1}]}");
                sb.AppendLine("BOTH actions fire at the same time. Never only one.");
                sb.AppendLine();
                sb.AppendLine("Output this exact JSON. No text outside it. ALL fields are required.");
                sb.AppendLine("{");
                sb.AppendLine("  \"internal_thoughts\": \"your private reaction, 1-2 sentences\",");
                sb.AppendLine("  \"response\": \"what you say out loud, in character, 2-4 sentences\",");
                sb.AppendLine("  \"tone\": \"friendly|neutral|suspicious|hostile|fearful\",");
                sb.AppendLine("  \"actions\": []");
                sb.AppendLine("}");
        