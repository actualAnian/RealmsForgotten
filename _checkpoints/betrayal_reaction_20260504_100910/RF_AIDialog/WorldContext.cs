using System;
using System.Collections.Generic;
using System.Text;
using RealmsForgotten.AiMade.StrategicIntrigue.Campaign;
using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
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
    ///   8. World history (recorded events)
    ///   9. Political inner state (StrategicIntrigue bridge)
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
                AppendWorldHistory(sb);
                AppendIntrigueState(sb, npc);

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
                        sb.AppendLine($"The player's faction ({playerKingdom.Name}) is currently at peace.");
                }
            }
            catch { }
        }

        // ── 3. Active sieges ──────────────────────────────────────────────

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

                    string attackerName = "Unknown";
                    try
                    {
                        var leaderParty = siegeEvent.BesiegerCamp?.LeaderParty;
                        if (leaderParty?.LeaderHero != null)
                            attackerName = leaderParty.LeaderHero.Name.ToString();
                        else if (leaderParty?.MapFaction != null)
                            attackerName = leaderParty.MapFaction.Name.ToString();
                    }
                    catch { }

                    lines.Add($"{attackerName} is besieging {settlement.Name}");
                    if (lines.Count >= 4) break; // cap to keep prompt lean
                }

                if (lines.Count > 0)
                    sb.AppendLine($"Active sieges: {string.Join("; ", lines)}.");
            }
            catch { }
        }

        // ── 4. Eliminated (fallen) kingdoms ───────────────────────────────

        private static void AppendEliminatedKingdoms(StringBuilder sb)
        {
            try
            {
                var fallen = new List<string>();
                foreach (var k in Campaign.Current.Kingdoms)
                {
                    if (k.IsEliminated)
                        fallen.Add(k.Name.ToString());
                }
                if (fallen.Count > 0)
                    sb.AppendLine($"Fallen kingdoms (destroyed): {string.Join(", ", fallen)}. " +
                                  "Their lords are scattered, their lands seized.");
            }
            catch { }
        }

        // ── 5. NPC's personal military situation ──────────────────────────

        private static void AppendNpcPersonalSituation(StringBuilder sb, Hero npc)
        {
            try
            {
                if (npc == null) return;

                // Prisoner
                if (npc.IsPrisoner)
                {
                    sb.AppendLine("NOTE: You are currently a prisoner. This weighs heavily on you.");
                    return; // nothing else is relevant if imprisoned
                }

                // In active battle
                try
                {
                    if (npc.PartyBelongedTo?.MapEvent != null)
                        sb.AppendLine("NOTE: You are currently engaged in battle. Your mind is on the fight.");
                }
                catch { }

                // Besieging a settlement
                try
                {
                    var besieged = npc.PartyBelongedTo?.BesiegedSettlement;
                    if (besieged != null)
                        sb.AppendLine($"NOTE: You are currently leading a siege against {besieged.Name}.");
                }
                catch { }

                // Their own settlement under siege
                try
                {
                    var home = npc.HomeSettlement ?? npc.BornSettlement;
                    if (home != null && home.IsUnderSiege && home.OwnerClan == npc.Clan)
                        sb.AppendLine($"NOTE: Your settlement {home.Name} is under siege right now. You are desperate.");
                }
                catch { }
            }
            catch { }
        }

        // ── 6. NPC's fiefs ────────────────────────────────────────────────

        private static void AppendNpcFiefs(StringBuilder sb, Hero npc)
        {
            try
            {
                var fiefs = npc.Clan?.Fiefs;
                if (fiefs == null || fiefs.Count == 0) return;

                var lines = new List<string>();
                int count = 0;
                foreach (var town in fiefs)
                {
                    if (count >= 3) break; // cap at 3 fiefs
                    try
                    {
                        var s = town.Settlement;
                        string type = s.IsTown ? "Town" : "Castle";
                        string status = s.IsUnderSiege ? " [BESIEGED]" : "";

                        // Prosperity descriptor (towns only)
                        string prosperityDesc = "";
                        if (s.IsTown)
                        {
                            float p = town.Prosperity;
                            prosperityDesc = p >= 6000 ? ", thriving"    :
                                             p >= 4000 ? ", prosperous"  :
                                             p >= 2000 ? ", struggling"  : ", impoverished";
                        }

                        // Loyalty / security (towns only)
                        string loyaltyDesc = "";
                        if (s.IsTown && town.Loyalty < 40)
                            loyaltyDesc = ", low loyalty";
                        string securityDesc = "";
                        if (s.IsTown && town.Security < 30)
                            securityDesc = ", unsafe";

                        lines.Add($"{type} {s.Name}{status}{prosperityDesc}{loyaltyDesc}{securityDesc}");
                        count++;
                    }
                    catch { }
                }

                if (lines.Count > 0)
                {
                    string extra = fiefs.Count > 3 ? $" (and {fiefs.Count - 3} more)" : "";
                    sb.AppendLine($"Your holdings: {string.Join("; ", lines)}{extra}.");
                }
            }
            catch { }
        }

        // ── 7. Clan power ─────────────────────────────────────────────────


        private static void AppendClanPower(StringBuilder sb, Hero npc)
        {
            try
            {
                var clan = npc.Clan;
                if (clan == null) return;

                float renown    = clan.Renown;
                float influence = clan.Influence;
                int   tier      = clan.Tier;

                string renownDesc = renown >= 1000 ? "legendary renown" :
                                    renown >= 500  ? "great renown"     :
                                    renown >= 200  ? "solid renown"     :
                                    renown >= 50   ? "modest renown"    : "little renown";

                string influenceDesc = influence >= 500 ? "great political influence" :
                                       influence >= 200 ? "decent influence"          :
                                       influence >= 50  ? "some influence"            : "little political sway";

                sb.AppendLine(
                    $"Your clan ({clan.Name}) is Tier {tier} with {renownDesc} " +
                    $"({(int)renown}) and {influenceDesc} ({(int)influence}).");
            }
            catch { }
        }

        // ── 8. World history (recorded events) ───────────────────────────

        private static void AppendWorldHistory(StringBuilder sb)
        {
            try
            {
                var store = WorldHistoryStore.Instance;
                if (store == null) return;

                // Last 15 events within the past 90 in-game days
                var events = store.GetRecentEvents(maxCount: 15, maxDays: 90);
                if (events == null || events.Count == 0) return;

                sb.AppendLine