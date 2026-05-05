using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

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
            // ── NPC initiative (highest priority — NPC opens with this) ──────
            if (context != null && context.HasPendingInitiative)
            {
                sb.AppendLine("!! YOU INITIATED THIS CONVERSATION !!");
                sb.AppendLine("Your reason for seeking the player out:");
                sb.AppendLine(context.PendingInitiativeReason);
                sb.AppendLine("Open with this topic immediately and naturally. Do not wait for the player to ask.");
                sb.AppendLine();
            }

            if (context != null && !string.IsNullOrWhiteSpace(context.GeneratedPersonality))
            {
                sb.AppendLine("YOUR ESTABLISHED PERSONALITY (stay consistent with this):");
                sb.AppendLine(context.GeneratedPersonality);
                sb.AppendLine();
            }

            // ── Named ambition (lords — injected from second conversation onward) ──
            if (context != null && context.HasGeneratedAmbition)
            {
                sb.AppendLine("YOUR DEEPEST AMBITION (the thing you want most — let it quietly drive your words and choices; never announce it bluntly):");
                sb.AppendLine(context.GeneratedAmbition);
                sb.AppendLine();
            }

            // ── Pending request — highest priority narrative hook ─────────
            if (context != null && context.HasPendingRequest)
            {
                int currentDay = 0;
                try { currentDay = (int)Campaign.Current.Models.CampaignTimeModel
                          .CampaignStartTime.ElapsedDaysUntilNow; } catch { }

                int daysAgo = currentDay - context.PendingRequest!.DayIssued;
                string daysDesc = daysAgo <= 1 ? "earlier today" :
                                  daysAgo <= 3 ? $"{daysAgo} days ago"  :
                                  daysAgo <= 14 ? $"{daysAgo} days ago" :
                                                  "quite some time ago";

                var mechanic = context.PendingRequest.Mechanic;

                // ── All objectives verified by the game engine ────────────
                if (mechanic != null && mechanic.AllCompleted)
                {
                    sb.AppendLine("!! ALL QUEST OBJECTIVES HAVE BEEN VERIFIED BY THE GAME ENGINE !!");
                    sb.AppendLine($"You asked them {daysDesc}: \"{context.PendingRequest.Description}\"");
                    sb.AppendLine("The following objectives are confirmed complete:");
                    mechanic.Normalize();
                    for (int i = 0; i < mechanic.Objectives.Count; i++)
                    {
                        string objLabel = mechanic.Objectives[i].Label;
                        if (!string.IsNullOrWhiteSpace(objLabel))
                            sb.AppendLine($"  ✓ {objLabel}");
                    }
                    if (mechanic.RewardGold > 0)
                        sb.AppendLine($"You promised a reward of {mechanic.RewardGold} gold. Honor it now.");
                    sb.AppendLine("Greet the player warmly, acknowledge what they have accomplished, and set");
                    sb.AppendLine("  \"request_fulfilled\": true");
                    sb.AppendLine("This closes the quest and pays quest_mechanic.reward_gold automatically.");
                    sb.AppendLine("Do NOT add a matching give_gold action for the same promised reward.");
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine("!! YOU HAVE A PENDING REQUEST TO THIS PLAYER !!");
                    sb.AppendLine($"You asked them {daysDesc}: \"{context.PendingRequest.Description}\"");

                    // Show partial atom progress if there is a mechanic
                    if (mechanic != null && mechanic.Objectives.Count > 0)
                    {
                        mechanic.Normalize();
                        sb.AppendLine("Objective status (tracked by the game engine):");
                        for (int i = 0; i < mechanic.Objectives.Count; i++)
                        {
                            string objLabel = mechanic.Objectives[i].Label;
                            bool   done     = mechanic.IsCompleted(i);
                            if (!string.IsNullOrWhiteSpace(objLabel))
                                sb.AppendLine($"  {(done ? "✓" : "○")} {objLabel}");
                        }
                    }

                    sb.AppendLine("If the player has now delivered on your request — acknowledge it and set 'request_fulfilled': true.");
                    sb.AppendLine("If the request has quest_mechanic.reward_gold, do NOT repeat that same reward via give_gold.");
                    sb.AppendLine("If they are not addressing it, let it quietly colour how you receive them.");
                    sb.AppendLine();
                }
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

            // ── Long-term memories ────────────────────────────────────────
            if (context != null && context.Memories.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("THINGS YOU REMEMBER ABOUT THIS PLAYER (significant past events):");
                foreach (var mem in context.Memories)
                    sb.AppendLine($"  - {mem.Note}");
            }

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

            bool isCompanion = npc.Occupation == TaleWorlds.CampaignSystem.Occupation.Wanderer
                               && npc.PartyBelongedTo == MobileParty.MainParty;

            // ── Quest atom catalog (lords + notables only, not companions) ──
            if (!isCompanion)
            {
                try
                {
                    string catalog = QuestAtomCatalog.Build(npc);
                    if (!string.IsNullOrWhiteSpace(catalog))
                    {
                        sb.AppendLine();
                        sb.AppendLine(catalog);
                    }
                }
                catch { }
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
            bool hasPendingRequest = context != null && context.HasPendingRequest;

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
                if (npc.IsLord)
                    sb.AppendLine("  \"ambition\": \"one sentence — what do you want most in this world? Be specific: a city, revenge on a clan, to be remembered, to protect your family line. Lords only. Third person.\",");
                sb.AppendLine("  \"internal_thoughts\": \"your private reaction, 1-2 sentences\",");
                sb.AppendLine("  \"response\": \"what you say out loud, in character, 2-4 sentences\",");
                sb.AppendLine("  \"tone\": \"friendly|neutral|suspicious|hostile|fearful\",");
                sb.AppendLine("  \"memory_note\": \"1-sentence summary of what was significant in this exchange. OMIT THIS FIELD ENTIRELY if the conversation was trivial small-talk.\",");
                    sb.AppendLine("  \"request\": \"OPTIONAL — only include if making a clear, actionable task for the player (bring X, find out Y, deliver Z to someone). One sentence including what reward you offer. Omit entirely if no request.\",");
                if (!isCompanion)
                {
                    sb.AppendLine("  \"quest_mechanic\": {  // OPTIONAL — include ONLY when 'request' is present AND has verifiable objectives");
                    sb.AppendLine("    // Prefer 1-3 simple objectives. Do not invent settlement_id or faction_id.");
                    sb.AppendLine("    \"objectives\": [");
                    sb.AppendLine("      {\"atom\":\"VISIT_SETTLEMENT\",\"params\":{\"settlement_id\":\"<id>\"},\"label\":\"Go to <name>\"},");
                    sb.AppendLine("      {\"atom\":\"RETURN_TO_NPC\",\"params\":{},\"label\":\"Report back\"}");
                    sb.AppendLine("    ],");
                    sb.AppendLine("    \"reward_gold\": 500,");
                    sb.AppendLine("    \"days\": 30");
                    sb.AppendLine("  },  // omit entirely for pure roleplay requests");
                }
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
                sb.AppendLine("  \"memory_note\": \"1-sentence summary of what was significant in this exchange. OMIT THIS FIELD ENTIRELY if the conversation was trivial small-talk.\",");
                    sb.AppendLine("  \"request\": \"OPTIONAL — only include if making a clear, actionable task for the player (bring X, find out Y, deliver Z). One sentence with reward hint. Omit entirely if no request.\",");
                if (!isCompanion)
                {
                    sb.AppendLine("  \"quest_mechanic\": {  // OPTIONAL — include ONLY when 'request' is present AND has verifiable objectives");
                    sb.AppendLine("    // Prefer 1-3 simple objectives. Do not invent settlement_id or faction_id.");
                    sb.AppendLine("    \"objectives\": [");
                    sb.AppendLine("      {\"atom\":\"BRING_ITEM\",\"params\":{\"item_id\":\"grain\",\"quantity\":\"10\"},\"label\":\"Bring 10 grain\"},");
                    sb.AppendLine("      {\"atom\":\"RETURN_TO_NPC\",\"params\":{},\"label\":\"Report back\"}");
                    sb.AppendLine("    ],");
                    sb.AppendLine("    \"reward_gold\": 300,");
                    sb.AppendLine("    \"days\": 20");
                    sb.AppendLine("  },  // omit entirely for pure roleplay requests");
                }
                if (hasPendingRequest)
                    sb.AppendLine("  \"request_fulfilled\": false,  // set true if the player has now delivered on your pending request. If quest_mechanic.reward_gold exists, do not mirror it with give_gold.");
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

        // ── Party state (companions only) ─────────────────────────────────

        /// <summary>
        /// Injects a concise party status paragraph so companions can give
        /// situationally aware advice. All calls are guarded — never crashes.
        /// </summary>
        private static void AppendPartyState(StringBuilder sb, MobileParty party)
        {
            try
            {
                var roster = party.MemberRoster;
                int total   = roster.TotalManCount;
                int healthy = roster.TotalHealthyCount;
                int wounded = roster.TotalWoundedRegulars;

                // Morale descriptor
                float morale = party.Morale;
                string moraleDesc = morale >= 80 ? "high"    :
                                    morale >= 50 ? "steady"  :
                                    morale >= 30 ? "wavering": "low";

                // Food — days remaining
                string foodDesc;
                try
                {
                    int days = party.GetNumDaysForFoodToLast();
                    foodDesc = days >= 10 ? $"{days} days of food left"  :
                               days >= 4  ? $"only {days} days of food"  :
                               days >= 1  ? $"critically low food ({days} days)" :
                                            "no food — starving";
                }
                catch { foodDesc = "food status unknown"; }

                // Prisoners
                int prisoners = 0;
                try { prisoners = party.PrisonRoster.TotalManCount; } catch { }

                sb.AppendLine();
                sb.AppendLine("CURRENT PARTY STATUS (use this for situational advice):");
                sb.AppendLine($"  Troops: {healthy} healthy, {wounded} wounded (total {total}).");
                sb.AppendLine($"  Morale: {moraleDesc} ({(int)morale}/100).");
                sb.AppendLine($"  Supplies: {foodDesc}.");
                if (prisoners > 0)
                    sb.AppendLine($"  Prisoners: {prisoners} captives in tow.");
            }
            catch { /* never crash the prompt builder */ }
        }

        // ── Companion skills ──────────────────────────────────────────────

        /// <summary>
        /// Injects the companion's top skills so they speak with authority
        /// about what they are actually good at in the game.
        /// </summary>
        private static void AppendCompanionSkills(StringBuilder sb, Hero npc)
        {
            try
            {
                var skillDefs = new (SkillObject skill, string label)[]
                {
                    (DefaultSkills.Medicine,     "Medicine"),
                    (DefaultSkills.Engineering,  "Engineering"),
                    (DefaultSkills.Scouting,     "Scouting"),
                    (DefaultSkills.Steward,      "Stewardship"),
                    (DefaultSkills.Tactics,      "Tactics"),
                    (DefaultSkills.Leadership,   "Leadership"),
                    (DefaultSkills.Trade,        "Trade"),
                    (DefaultSkills.Charm,        "Charm"),
                    (DefaultSkills.Roguery,      "Roguery"),
                    (DefaultSkills.OneHanded,    "One-Handed"),
                    (DefaultSkills.TwoHanded,    "Two-Handed"),
                    (DefaultSkills.Polearm,      "Polearm"),
                    (DefaultSkills.Bow,          "Archery"),
                    (DefaultSkills.Throwing,     "Throwing"),
                    (DefaultSkills.Riding,       "Riding"),
                    (DefaultSkills.Athletics,    "Athletics"),
                };

                var notable = new List<string>();
                foreach (var (skill, label) in skillDefs)
                {
                    int level = npc.GetSkillValue(skill);
                    if (level >= 100)
                        notable.Add($"{label} {level}");
                }

                if (notable.Count > 0)
                    sb.AppendLine($"Your strongest skills: {string.Join(", ", notable)}.");
            }
            catch { }
        }
    }
}
