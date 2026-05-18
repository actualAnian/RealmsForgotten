using System;
using System.Collections.Generic;
using System.Linq;
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
                AppendRequestNeedProfile(sb, npc);
                AppendNearbySettlements(sb, npc);
                AppendWorldHistory(sb);
                AppendIntrigueState(sb, npc);
                AppendPlayerReputation(sb);

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

                        // Settlement scar — conquest history
                        string scarDesc = "";
                        try
                        {
                            var scar = SettlementScarStore.Instance?.GetScar(s);
                            if (scar != null)
                            {
                                if (scar.TimesChanged >= 3)
                                    scarDesc = $", a hotly contested holding (changed hands {scar.TimesChanged} times — most recently taken from {scar.PreviousOwnerKingdomName} on day {scar.DayOfCapture})";
                                else if (scar.TimesChanged >= 1)
                                    scarDesc = $", taken from {scar.PreviousOwnerKingdomName} by {scar.CapturerHeroName} on day {scar.DayOfCapture}";
                            }
                        }
                        catch { }

                        try
                        {
                            var memories = AIMemoryStore.GetSettlementMemories(s.StringId, maxCount: 2, maxDays: 0);
                            if (memories.Count > 0)
                                scarDesc += $", remembered locally: {string.Join(" / ", memories.Select(m => m.Text))}";
                        }
                        catch { }

                        lines.Add($"{type} {s.Name}{status}{prosperityDesc}{loyaltyDesc}{securityDesc}{scarDesc}");
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

                try
                {
                    var memories = AIMemoryStore.GetClanMemories(clan.StringId, maxCount: 3, maxDays: 0);
                    if (memories.Count > 0)
                    {
                        sb.AppendLine("Recent memories attached to your clan:");
                        foreach (var memory in memories)
                            sb.AppendLine($"  - {memory.Text}");
                    }
                }
                catch { }
            }
            catch { }
        }

        private static void AppendRequestNeedProfile(StringBuilder sb, Hero npc)
        {
            try
            {
                if (npc == null) return;

                var profile = AIRequestNeedEvaluator.Evaluate(npc);
                sb.AppendLine();
                sb.AppendLine("AI REQUEST NEED PROFILE (hard constraints for whether you should offer work):");
                sb.AppendLine($"  need_level: {profile.NeedLevel}");
                sb.AppendLine($"  should_offer_request: {(profile.ShouldOfferRequest ? "yes" : "no")}");
                sb.AppendLine($"  locality_scope: {profile.LocalityScope}");

                if (profile.NeedDomains.Count > 0)
                    sb.AppendLine($"  need_domains: {string.Join(", ", profile.NeedDomains)}");

                if (profile.NeedSignals.Count > 0)
                    sb.AppendLine($"  need_signals: {string.Join(", ", profile.NeedSignals)}");

                if (profile.AllowedQuestKinds.Count > 0)
                    sb.AppendLine($"  allowed_quest_kinds: {string.Join(", ", profile.AllowedQuestKinds)}");
                else
                    sb.AppendLine("  allowed_quest_kinds: none");

                foreach (var note in profile.Notes)
                    sb.AppendLine($"  note: {note}");
            }
            catch { }
        }

        private static void AppendNearbySettlements(StringBuilder sb, Hero npc)
        {
            try
            {
                var nearby = AIRequestNeedEvaluator.GetNearbySettlements(npc, 5);
                if (nearby.Count == 0) return;

                sb.AppendLine();
                sb.AppendLine("NEARBY SETTLEMENTS (prefer these when naming destinations, targets, or local trouble):");
                foreach (var settlement in nearby)
                {
                    string type = settlement.IsTown ? "town" :
                                  settlement.IsCastle ? "castle" :
                                  settlement.IsVillage ? "village" : "settlement";
                    string faction = settlement.MapFaction?.Name?.ToString() ?? "No faction";
                    sb.AppendLine($"  - {settlement.Name} [{type}, {faction}]");
                }
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

                sb.AppendLine();
                sb.AppendLine("RECENT WORLD HISTORY (significant events you would know about):");
                foreach (var e in events)
                    sb.AppendLine($"  [Day {e.Day}] {e.Description}");
            }
            catch { }
        }

        // ── 9. Political inner state (StrategicIntrigue bridge) ───────────
        //
        // This section feeds the lord's secret emotional and political state
        // into the LLM prompt. The NPC must never recite these facts mechanically
        // — they should colour tone, word choice, and what the NPC chooses to
        // reveal or conceal.

        private static void AppendIntrigueState(StringBuilder sb, Hero npc)
        {
            try
            {
                // Only relevant for lords that belong to a kingdom
                if (npc == null || !npc.IsLord || npc.Clan?.Kingdom == null) return;

                var intrigue = Campaign.Current.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
                if (intrigue == null) return;

                var clan   = npc.Clan;
                var cState = intrigue.GetState(clan);
                var kState = intrigue.GetKingdomState(clan.Kingdom);
                if (cState == null) return;

                var lines = new List<string>();

                // ── Loyalty / dissidence ──────────────────────────────────
                if (cState.Dissidence >= 85f)
                    lines.Add("Your loyalty to this kingdom is at a breaking point. You are actively considering your options.");
                else if (cState.Dissidence >= 65f)
                    lines.Add("Your loyalty is severely strained. You have serious doubts about staying in this kingdom.");
                else if (cState.Dissidence >= 40f)
                    lines.Add("You have growing frustrations with how the kingdom is run. Your loyalty is not unconditional.");
                // Below 40: no mention — lord is baseline loyal

                // ── Grievances (only if meaningful) ──────────────────────
                if (cState.FiefGrievance >= 50f)
                    lines.Add("You feel deeply wronged over land. Your clan deserves more than it has received.");
                else if (cState.FiefGrievance >= 30f)
                    lines.Add("You feel your clan has been passed over for lands it rightfully deserves.");

                if (cState.VoteResentment >= 50f)
                    lines.Add("Court decisions have repeatedly favoured others over you. You feel politically humiliated.");
                else if (cState.VoteResentment >= 30f)
                    lines.Add("You have been on the losing side of too many court votes. The resentment lingers.");

                if (cState.MilitaryFrustration >= 45f)
                    lines.Add("You are deeply frustrated with how wars are being waged. Losses feel avoidable and command feels incompetent.");
                else if (cState.MilitaryFrustration >= 25f)
                    lines.Add("The military situation troubles you. You question whether the right decisions are being made.");

                // ── Ambition ──────────────────────────────────────────────
                if (cState.ClaimantAmbition >= 65f)
                    lines.Add("Privately, you believe the throne should belong to a stronger hand — perhaps your own clan's.");
                else if (cState.ClaimantAmbition >= 40f)
                    lines.Add("You quietly wonder whether the current ruler is truly the strongest choice for this throne.");

                // ── Fear of ruler ─────────────────────────────────────────
                if (cState.FearOfRuler >= 60f)
                    lines.Add("You are wary of your ruler's reach. You do not speak openly of your grievances — not even in private.");
                else if (cState.FearOfRuler >= 35f)
                    lines.Add("You are careful about how you speak of your ruler. The walls have ears.");

                // ── Kingdom-level health ──────────────────────────────────
                if (kState != null)
                {
                    if (kState.RulerLegitimacy < 35f)
                        lines.Add("Many in court — including you — question whether your ruler truly commands loyalty or merely commands fear.");
                    else if (kState.RulerLegitimacy < 55f)
                        lines.Add("Your ruler's authority is not as firm as it once was. You sense it in the court's mood.");

                    if (kState.CourtFragmentation >= 60f)
                        lines.Add("The court is bitterly divided. Factions within your own kingdom undermine each other openly.");
                    else if (kState.CourtFragmentation >= 35f)
                        lines.Add("There are factions within the kingdom that do not see eye to eye. The court is uneasy.");

                    if (kState.WarExhaustion >= 60f)
                        lines.Add("This kingdom is exhausted. Soldiers, lords, and smallfolk alike are weary of constant war.");
                    else if (kState.WarExhaustion >= 35f)
                        lines.Add("The wars are beginning to wear on the kingdom. Not everyone is as eager to fight as they once were.");

                    if (kState.RebellionPressure >= 50f)
                        lines.Add("There is dangerous unrest in the realm. A rebellion is not unthinkable.");

                    // Kingdom objective (grand strategy purpose)
                    string objectiveDesc = kState.ObjectiveType switch
                    {
                        KingdomObjectiveType.CrushBattanianResistance  => "Your kingdom is committed to breaking Battanian resistance once and for all.",
                        KingdomObjectiveType.NobleWealthSupremacy      => "Your kingdom's ruling vision is the supremacy of noble wealth and influence over all else.",
                        KingdomObjectiveType.PreserveBattanianHomelands => "Your kingdom is sworn to defend Battanian lands and culture against all invaders.",
                        KingdomObjectiveType.UniteAseraiRealms         => "Your kingdom seeks to reunite all Aserai lands under one banner.",
                        KingdomObjectiveType.ClaimImperialLegitimacy   => "Your kingdom claims to be the true heir of the old Empire and seeks to prove it.",
                        KingdomObjectiveType.ForgeBorderEmpire         => "Your kingdom is driven to forge a new empire from border conquests.",
                        KingdomObjectiveType.ArcaneFrontier            => "Your kingdom has a destiny tied to the arcane frontier — the Battanian forests and their secrets.",
                        KingdomObjectiveType.SecureMountainHolds       => "Your kingdom's strategic goal is to control the mountain strongholds and hold them against all comers.",
                        KingdomObjectiveType.DefileMountainHolds       => "Your kingdom is committed to breaking the mountain holds and ending the threat they pose.",
                        KingdomObjectiveType.MartialGlory              => "Your kingdom prizes martial glory above all — war is not just strategy, it is identity.",
                        KingdomObjectiveType.UnbreakableRealm          => "Your kingdom's purpose is to become an unbreakable fortress-state — stability and defence above all.",
                        _                                              => ""
                    };
                    if (!string.IsNullOrEmpty(objectiveDesc))
                        lines.Add(objectiveDesc);
                }

                // ── Secret pact with the player ───────────────────────────
                if (intrigue.HasPlayerPact(clan))
                {
                    var goal = intrigue.GetActivePactGoal(clan);
                    string pactDesc = goal switch
                    {
                        IntriguePactGoal.PrepareProtectedVassalage =>
                            "You have a private understanding with the player: if this kingdom fractures, you intend to seek shelter under their banner.",
                        IntriguePactGoal.SupportFutureClaimant =>
                            "You have a secret understanding with the player: you are working toward a change in who holds the throne here.",
                        IntriguePactGoal.BreakAwayFromKingdom =>
                            "You have a secret understanding with the player: when the moment is right, you will break cleanly from this kingdom.",
                        _ =>
                            "You have a quiet private understanding with the player that you keep from all others."
                    };
                    lines.Add(pactDesc);
                }

                // ── Secret alliance with the player ───────────────────────
                var alliance = intrigue.GetPlayerAllianceWithClan(clan);
                if (alliance != null)
                {
                    string settlementPart = alliance.PromisedSettlement != null
                        ? $" {alliance.PromisedSettlement.Name} has been promised to your clan as the price of your support."
                        : "";
                    lines.Add(
                        $"You have a firm alliance with the player and have pledged your support when the break comes.{settlementPart}");
                }

                // ── Emit section only if there's something to say ─────────
                if (lines.Count == 0) return;

                sb.AppendLine();
                sb.AppendLine("YOUR POLITICAL INNER STATE (your private reality — never state these as facts, let them colour how you speak):");
                foreach (var line in lines)
                    sb.AppendLine($"  - {line}");
            }
            catch { }
        }

        // ── 10. Player reputation (known deeds — common knowledge) ────────
        //
        // Rumours spread. Every lord and notable knows roughly what kind of
        // person the player is. This section gives the NPC that common-knowledge
        // context so they can react with earned trust, fear, or contempt.

        private static void AppendPlayerReputation(StringBuilder sb)
        {
            try
            {
                var rep = PlayerReputationStore.Instance;
                if (rep == null) return;

                // Only emit if any score has moved meaningfully away from neutral
                bool hasHonorNote      = rep.HonorScore      <= 35f || rep.HonorScore      >= 65f;
                bool hasMercyNote      = rep.MercyScore      <= 35f || rep.MercyScore      >= 65f;
                bool hasAggressionNote = rep.AggressionScore <= 35f || rep.AggressionScore >= 65f;

                if (!hasHonorNote && !hasMercyNote && !hasAggressionNote) return;

                var lines = new List<string>();

                // Honor
                if (rep.HonorScore >= 75f)
                    lines.Add($"{Hero.MainHero?.Name} is known across Aeurth as someone who keeps their word and respects the customs of war. Lords trust their agreements with them.");
                else if (rep.HonorScore >= 65f)
                    lines.Add($"{Hero.MainHero?.Name} has a reputation for dealing honestly. Most lords consider them a reliable partner.");
                else if (rep.HonorScore <= 25f)
                    lines.Add($"{Hero.MainHero?.Name} is spoken of as treacherous — someone who breaks faith when convenient. Few lords trust their word.");
                else if (rep.HonorScore <= 35f)
                    lines.Add($"{Hero.MainHero?.Name} has a mixed reputation for honour. Some lords are wary of making agreements with them.");

                // Mercy
                if (rep.MercyScore >= 75f)
                    lines.Add($"{Hero.MainHero?.Name} is known for releasing prisoners generously — even without ransom. Enemies speak of them without hatred.");
                else if (rep.MercyScore >= 65f)
                    lines.Add($"{Hero.MainHero?.Name} treats defeated lords with reasonable mercy. They are not feared as a cruel conqueror.");
                else if (rep.MercyScore <= 25f)
                    lines.Add($"{Hero.MainHero?.Name} has a dark reputation: they execute defeated lords without hesitation. This is known and feared across the realm.");
                else if (rep.MercyScore <= 35f)
                    lines.Add($"{Hero.MainHero?.Name} has executed captured lords before. Others remember this.");

                // Aggression
                if (rep.AggressionScore >= 75f)
                    lines.Add($"{Hero.MainHero?.Name} is a relentless aggressor — raiding villages, declaring wars without hesitation. Their name is associated with conquest.");
                else if (rep.AggressionScore >= 65f)
                    lines.Add($"{Hero.MainHero?.Name} has a reputation for boldness in war. They do not shy from conflict.");
                else if (rep.AggressionScore <= 25f)
                    lines.Add($"{Hero.MainHero?.Name} is known as a peacekeeper — preferring diplomacy and restraint over war. Rarely the one to draw first blood.");
                else if (rep.AggressionScore <= 35f)
                    lines.Add($"{Hero.MainHero?.Name} tends toward caution and peace over aggression.");

                if (lines.Count == 0) return;

                sb.AppendLine();
                sb.AppendLine($"THE PLAYER'S KNOWN REPUTATION (common knowledge in Aeurth — you may factor this into how you read their motives and character):");
                foreach (var line in lines)
                    sb.AppendLine($"  - {line}");
            }
            catch { }
        }
    }
}
