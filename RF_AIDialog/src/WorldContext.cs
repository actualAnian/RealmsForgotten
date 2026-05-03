using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_AIDialog
{
    /// <summary>
    /// Provides world context for NPC prompts:
    ///   - StaticLore : the fixed history and peoples of Aeurth (injected once per prompt)
    ///   - BuildDynamicState : current in-game events (wars, sieges) pulled at call time
    /// </summary>
    public static class WorldContext
    {
        // ── Static lore ───────────────────────────────────────────────────
        // Condensed from the Aeurth lore document. Token-efficient but rich enough
        // for NPCs to speak authentically about the world they live in.

        public const string StaticLore =
            "The world is called Aeurth, not Calradia. " +
            "Its peoples are: " +
            "MEN — divided into three realms (northern, western, southern), " +
            "descendants of survivors who crossed a frozen sea centuries ago after their homeland sank beneath ice; " +
            "ELVEANS — an ancient proud people of the High Garden (a mountain realm), " +
            "part elvish in blood, fiercely distrustful of outsiders, claiming to be the world's original sons; " +
            "NASORIA — settlers of the fertile western plains, " +
            "ancestors who fled a culture of endless warlord wars and built a new civilization; " +
            "PHARUN — sorcerer-kings ruling six rival city-states in the Jathari Desert, " +
            "whose ancient magic drained the land's life and turned it to wasteland; " +
            "XILANTLACAY — a powerful race of giants and half-giants, " +
            "expelled from their homeland by the Pharun, now a proud island kingdom in the tropical south; " +
            "DREADREALMS — undead remnants of an ancient civilization that once ruled Aeurth, " +
            "exiled by the Elveans to Eternivora (a valley of eternal winter), now returning to take vengeance; " +
            "DUGRAST — the small folk, the oldest race, dwellers beneath the mountains for millennia, " +
            "now venturing into the world to fight for its survival; " +
            "ALLKHUUR — fierce nomadic horse masters of the steppes between Eternivora and the realms of Men, " +
            "believers in absolute freedom — no kings, no queens, only the wind; " +
            "URKHAI — a violent orcish people of unknown origin, " +
            "said to be born from ancient dark sorcery (the Dragon Moon era), " +
            "territorial lords of the northern dark mountains.";

        // ── Dynamic world state ───────────────────────────────────────────

        /// <summary>
        /// Builds a short paragraph of current in-game world events:
        /// wars involving the NPC's faction and active sieges.
        /// All exceptions are caught — this must never crash the game.
        /// </summary>
        public static string BuildDynamicState(Hero npc)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("CURRENT WORLD EVENTS:");

                AppendWarState(sb, npc);
                AppendSieges(sb);

                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        // ── War state ─────────────────────────────────────────────────────

        private static void AppendWarState(StringBuilder sb, Hero npc)
        {
            try
            {
                if (!(npc.MapFaction is Kingdom npcKingdom)) return;

                var enemies = new List<string>();
                foreach (var k in Campaign.Current.Kingdoms)
                {
                    if (k == npcKingdom || k.IsEliminated) continue;
                    if (FactionManager.IsAtWarAgainstFaction(npcKingdom, k))
                        enemies.Add(k.Name.ToString());
                }

                if (enemies.Count > 0)
                    sb.AppendLine($"Your kingdom ({npcKingdom.Name}) is at war with: {string.Join(", ", enemies)}.");
                else
                    sb.AppendLine($"Your kingdom ({npcKingdom.Name}) is currently at peace.");

                // Player's faction if different
                if (Hero.MainHero?.MapFaction is Kingdom playerKingdom &&
                    playerKingdom != npcKingdom)
                {
                    var playerEnemies = new List<string>();
                    foreach (var k in Campaign.Current.Kingdoms)
                    {
                        if (k == playerKingdom || k.IsEliminated) continue;
                        if (FactionManager.IsAtWarAgainstFaction(playerKingdom, k))
                            playerEnemies.Add(k.Name.ToString());
                    }
                    if (playerEnemies.Count > 0)
                        sb.AppendLine(
                            $"The player's faction ({playerKingdom.Name}) is at war with: " +
                            $"{string.Join(", ", playerEnemies)}.");
                }
            }
            catch { /* never crash */ }
        }

        // ── Active sieges ─────────────────────────────────────────────────

        private static void AppendSieges(StringBuilder sb)
        {
            try
            {
                var lines = new List<string>();
                foreach (var settlement in Settlement.All)
                {
                    if (!settlement.IsUnderSiege) continue;

                    var siegeEvent = settlement.SiegeEvent;
                    if (siegeEvent == null) continue;

                    // Try to get the attacker's name from the besieger party
                    string attackerName = "Unknown";
                    try
                    {
                        var besiegerParty = siegeEvent.BesiegerParty;
                        if (besiegerParty?.LeaderHero != null)
                            attackerName = besiegerParty.LeaderHero.Name.ToString();
                        else if (besiegerParty?.MapFaction != null)
                            attackerName = besiegerParty.MapFaction.Name.ToString();
                    }
                    catch { }

                    lines.Add($"{attackerName} is besieging {settlement.Name}");
                    if (lines.Count >= 3) break; // cap at 3 to keep prompt lean
                }

                if (lines.Count > 0)
                    sb.AppendLine($"Active sieges: {string.Join("; ", lines)}.");
            }
            catch { /* never crash */ }
        }
    }
}
