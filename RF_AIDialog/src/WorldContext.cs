using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_AIDialog
{
    /// <summary>
    /// Provides world context for NPC prompts:
    ///   - StaticLore        : the fixed history and peoples of Aeurth (injected once per prompt)
    ///   - BuildDynamicState : current in-game events pulled at call time
    ///
    /// Dynamic sections (in order):
    ///   1. Season
    ///   2. Wars + diplomatic state (peace/war with each kingdom)
    ///   3. Active sieges
    ///   4. Fallen kingdoms (eliminated)
    ///   5. NPC's personal military situation
    ///   6. NPC's fiefs and their health
    ///   7. Clan power (renown, influence)
    /// </summary>
    public static class WorldContext
    {
        // ── Static lore ───────────────────────────────────────────────────
        // Condensed from the Aeurth lore document. Token-efficient but rich enough
        // for NPCs to speak authentically about the world they live in.
        public const string StaticLore =
            "This world is Aeurth. Its peoples: " +
            "MEN (three realms: north, west, south — ancient refugees who crossed a frozen sea); " +
            "ELVEANS (proud mountain-dwellers of the High Garden, distrustful of all outsiders); " +
            "NASORIA (western plains settlers, fled endless warlord wars); " +
            "PHARUN (six rival sorcerer-king city-states in the Jathari Desert — their magic turned fertile land to wasteland); " +
            "XILANTLACAY (giants and half-giants, exiled south — now a powerful island kingdom); " +
            "DREADREALMS (ancient undead civilization, exiled to Eternivora's eternal winter, returning for vengeance); " +
            "DUGRAST (small folk, oldest race, emerging from underground to defend the world); " +
            "ALLKHUUR (nomadic steppe horse-lords, believers in absolute freedom); " +
            "URKHAI (orcish people of dark sorcery origin, violent lords of the northern mountains).";

        // ── Dynamic world state ───────────────────────────────────────────

        /// <summary>
        /// Builds a contextual paragraph of current in-game world events.
        /// All sections are wrapped in try/catch — this must never crash the game.
        /// </summary>
        public static string BuildDynamicState(Hero npc)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("CURRENT WORLD EVENTS:");

                AppendSeason(sb);
                AppendWarAndDiplomacy(sb, npc);
                AppendSieges(sb);
                AppendEliminatedKingdoms(sb);
                AppendNpcPersonalSituation(sb, npc);
                AppendNpcFiefs(sb, npc);
                AppendClanPower(sb, npc);

                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        // ── 1. Season ─────────────────────────────────────────────────────

        private static void AppendSeason(StringBuilder sb)
        {
            try
            {
                var now = CampaignTime.Now;
                // GetSeasonOfYear is a property returning a CampaignTime.Seasons enum
                // cast to int: Spring=0, Summer=1, Autumn=2, Winter=3
                int seasonInt = (int)now.GetSeasonOfYear;
                string seasonName = seasonInt == 0 ? "Spring" :
                                    seasonInt == 1 ? "Summer" :
                                    seasonInt == 2 ? "Autumn" :
                                    seasonInt == 3 ? "Winter" : "Unknown";
                string seasonNote = seasonInt == 3 ? " — campaigning is brutal, roads are treacherous"  :
                                    seasonInt == 0 ? " — armies are beginning to move after winter"      :
                                    seasonInt == 1 ? " — peak campaigning season, marches are common"   :
                                                     " — harvest time, lords eye their granaries";
                sb.AppendLine($"Season: {seasonName}{seasonNote}.");
            }
            catch { }
        }

        // ── 2. Wars and diplomatic state ──────────────────────────────────

        private static void AppendWarAndDiplomacy(StringBuilder sb, Hero npc)
        {
            try
            {
                if (!(npc.MapFaction is Kingdom npcKingdom)) return;

                var enemies   = new List<string>();
                var peaceful  = new List<string>();

                foreach (var k in Campaign.Current.Kingdoms)
                {
                    if (k == npcKingdom || k.IsEliminated) continue;
                    if (FactionManager.IsAtWarAgainstFaction(npcKingdom, k))
                        enemies.Add(k.Name.ToString());
                    else
                        peaceful.Add(k.Name.ToString());
                }

                if (enemies.Count > 0)
                    sb.AppendLine($"Your kingdom ({npcKingdom.Name}) is at war with: {string.Join(", ", enemies)}.");
                else
                    sb.AppendLine($"Your kingdom ({npcKingdom.Name}) is at peace — no active wars.");

                // Only list peaceful kingdoms if the list is short enough to be meaningful
                if (peaceful.Count > 0 && peaceful.Count <= 3)
                    sb.AppendLine($"Currently at peace with: {string.Join(", ", peaceful)}.");
                else if (peaceful.Count > 3)
                    sb.AppendLine($"At peace with {peaceful.Count} other kingdoms.");

                // Player's faction wars (if different from NPC's kingdom)
                if (Hero.MainHero?.MapFaction is Kingdom playerKingdom
                    && playerKingdom != npcKingdom
                    && !playerKingdom.IsEliminated)
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
                    else
                        sb.AppendLine($"The player's fac