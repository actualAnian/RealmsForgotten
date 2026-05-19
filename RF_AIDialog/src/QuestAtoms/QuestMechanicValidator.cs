using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_AIDialog
{
    /// <summary>
    /// Sanitizes LLM-generated quest mechanics before they become real quests.
    /// Unknown atoms are dropped, invalid params are corrected when possible,
    /// and quest objectives are rebuilt into a small set of supported archetypes.
    /// </summary>
    public static class QuestMechanicValidator
    {
        private static readonly HashSet<string> AllowedAtoms =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "VISIT_SETTLEMENT",
                "LEAVE_SETTLEMENT",
                "DEFEAT_PARTY",
                "TALK_TO_PARTY",
                "BRING_ITEM",
                "BRING_TROOPS",
                "BRING_PRISONER_HERO",
                "WIN_TOURNAMENT",
                "RETURN_TO_NPC"
            };

        private static readonly HashSet<string> AllowedItemIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "grain", "wine", "hides", "linen", "tools",
                "silver_ore", "wool", "pottery", "salt", "dates"
            };

        private static readonly HashSet<string> AllowedQuestKinds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "travel_report",
                "delivery",
                "recruitment",
                "retaliation",
                "scouting",
                "delivery_under_pressure",
                "escort_with_ambush",
                "capture_prisoner",
                "courtship_tournament",
                "rescue_prisoner_noble"
            };

        public static QuestMechanic? Sanitize(QuestMechanic? mechanic, Hero? questGiver = null)
        {
            if (mechanic == null || mechanic.Objectives == null)
                return null;

            string questKind = ResolveQuestKind(mechanic);
            if (string.IsNullOrWhiteSpace(questKind))
                return null;

            if (!IsQuestKindAppropriateForNpc(questKind, questGiver))
                return null;

            var sanitized = new QuestMechanic
            {
                QuestKind = questKind,
                RewardGold = NormalizeRewardGold(questKind, mechanic.RewardGold),
                DurationDays = ClampDurationDays(mechanic.DurationDays)
            };

            if (!TryBuildCanonicalObjectives(questKind, mechanic, sanitized.Objectives))
                return null;

            sanitized.Normalize();
            if (sanitized.Objectives.Count == 0)
                return null;

            return PassesQualityRules(sanitized, questGiver) ? sanitized : null;
        }

        private static string ResolveQuestKind(QuestMechanic mechanic)
        {
            string explicitKind = (mechanic.QuestKind ?? "").Trim().ToLowerInvariant();
            if (AllowedQuestKinds.Contains(explicitKind))
                return explicitKind;

            return InferQuestKind(mechanic);
        }

        private static string InferQuestKind(QuestMechanic mechanic)
        {
            bool hasVisit = false;
            bool hasLeave = false;
            bool hasDefeat = false;
            bool hasTalk = false;
            bool hasBringItem = false;
            bool hasBringTroops = false;
            bool hasBringPrisonerHero = false;
            bool hasWinTournament = false;

            foreach (var atom in mechanic.Objectives ?? Enumerable.Empty<QuestAtom>())
            {
                string type = (atom?.AtomType ?? "").Trim().ToUpperInvariant();
                switch (type)
                {
                    case "VISIT_SETTLEMENT":
                        hasVisit = true;
                        break;
                    case "LEAVE_SETTLEMENT":
                        hasLeave = true;
                        break;
                    case "DEFEAT_PARTY":
                        hasDefeat = true;
                        break;
                    case "TALK_TO_PARTY":
                        hasTalk = true;
                        break;
                    case "BRING_ITEM":
                        hasBringItem = true;
                        break;
                    case "BRING_TROOPS":
                        hasBringTroops = true;
                        break;
                    case "BRING_PRISONER_HERO":
                        hasBringPrisonerHero = true;
                        break;
                    case "WIN_TOURNAMENT":
                        hasWinTournament = true;
                        break;
                }
            }

            bool hasMixedTravelCombat = hasVisit && hasDefeat;
            int categoryCount = 0;
            if (hasVisit) categoryCount++;
            if (hasDefeat) categoryCount++;
            if (hasBringItem) categoryCount++;
            if (hasBringTroops) categoryCount++;
            if (hasLeave || hasTalk) categoryCount++;
            if (hasBringPrisonerHero) categoryCount++;
            if (hasWinTournament) categoryCount++;

            if (hasBringItem && hasVisit && !hasDefeat)
                return "delivery_under_pressure";
            if (hasVisit && hasDefeat && !hasBringItem)
                return "escort_with_ambush";
            if (hasBringPrisonerHero)
                return "capture_prisoner";
            if (hasWinTournament)
                return "courtship_tournament";

            if (hasMixedTravelCombat || categoryCount != 1)
                return "";

            if (hasBringItem) return "delivery";
            if (hasBringTroops) return "recruitment";
            if (hasDefeat) return "retaliation";
            if (hasLeave || hasTalk) return "scouting";
            if (hasVisit) return "travel_report";
            return "";
        }

        private static bool TryBuildCanonicalObjectives(
            string questKind,
            QuestMechanic source,
            List<QuestAtom> destination)
        {
            destination.Clear();

            switch (questKind)
            {
                case "travel_report":
                {
                    var visit = GetFirstSanitizedAtom(source, "VISIT_SETTLEMENT");
                    if (visit == null) return false;

                    EnsureLabel(visit, "Visit the target settlement");
                    destination.Add(visit);
                    destination.Add(BuildReturnAtom(source, "Return and report"));
                    return true;
                }

                case "delivery":
                {
                    var bringItem = GetFirstSanitizedAtom(source, "BRING_ITEM");
                    if (bringItem == null) return false;

                    EnsureLabel(bringItem, "Deliver the requested goods");
                    destination.Add(bringItem);
                    destination.Add(BuildReturnAtom(source, "Return with the goods"));
                    return true;
                }

                case "recruitment":
                {
                    var bringTroops = GetFirstSanitizedAtom(source, "BRING_TROOPS");
                    if (bringTroops == null) return false;

                    EnsureLabel(bringTroops, "Muster the requested troops");
                    destination.Add(bringTroops);
                    destination.Add(BuildReturnAtom(source, "Return with your force assembled"));
                    return true;
                }

                case "retaliation":
                {
                    var defeat = GetFirstSanitizedAtom(source, "DEFEAT_PARTY");
                    if (defeat == null) return false;

                    EnsureLabel(defeat, "Defeat the enemy parties");
                    destination.Add(defeat);
                    destination.Add(BuildReturnAtom(source, "Return after striking the enemy"));
                    return true;
                }

                case "scouting":
                {
                    var leave = GetFirstSanitizedAtom(source, "LEAVE_SETTLEMENT");
                    var talk = GetFirstSanitizedAtom(source, "TALK_TO_PARTY");
                    if (talk == null) return false;

                    if (leave != null)
                    {
                        EnsureLabel(leave, "Leave the settlement and begin scouting");
                        destination.Add(leave);
                    }

                    EnsureLabel(talk, "Make contact with the target parties");
                    destination.Add(talk);
                    destination.Add(BuildReturnAtom(source, "Return with your report"));
                    return true;
                }

                case "delivery_under_pressure":
                {
                    var bringItem = GetFirstSanitizedAtom(source, "BRING_ITEM");
                    var visit = GetFirstSanitizedAtom(source, "VISIT_SETTLEMENT");
                    if (bringItem == null || visit == null) return false;

                    EnsureLabel(bringItem, "Carry the requested goods");
                    EnsureLabel(visit, "Reach the destination despite the danger");
                    destination.Add(bringItem);
                    destination.Add(visit);
                    destination.Add(BuildReturnAtom(source, "Return after the dangerous delivery"));
                    return true;
                }

                case "escort_with_ambush":
                {
                    var visit = GetFirstSanitizedAtom(source, "VISIT_SETTLEMENT");
                    var defeat = GetFirstSanitizedAtom(source, "DEFEAT_PARTY");
                    if (visit == null || defeat == null) return false;

                    EnsureLabel(defeat, "Repel the attacking enemy force");
                    EnsureLabel(visit, "Reach the destination after the ambush");
                    destination.Add(defeat);
                    destination.Add(visit);
                    destination.Add(BuildReturnAtom(source, "Return after seeing the escort through"));
                    return true;
                }

                case "capture_prisoner":
                {
                    var prisoner = GetFirstSanitizedAtom(source, "BRING_PRISONER_HERO");
                    if (prisoner == null) return false;

                    EnsureLabel(prisoner, "Bring the captured hero back alive");
                    destination.Add(prisoner);
                    destination.Add(BuildReturnAtom(source, "Return with your captive"));
                    return true;
                }

                case "courtship_tournament":
                {
                    var tournament = GetFirstSanitizedAtom(source, "WIN_TOURNAMENT");
                    if (tournament == null) return false;

                    EnsureLabel(tournament, "Win the required tournaments");
                    destination.Add(tournament);
                    destination.Add(BuildReturnAtom(source, "Return after proving yourself in the arena"));
                    return true;
                }

                case "rescue_prisoner_noble":
                {
                    var visit = GetFirstSanitizedAtom(source, "VISIT_SETTLEMENT");
                    if (visit == null) return false;

                    EnsureLabel(visit, "Go to the place where the imprisoned noble is being held");
                    destination.Add(visit);
                    destination.Add(BuildReturnAtom(source, "Return with word of the rescue attempt"));
                    return true;
                }

                default:
                    return false;
            }
        }

        private static QuestAtom? GetFirstSanitizedAtom(QuestMechanic source, string atomType)
        {
            foreach (var atom in source.Objectives ?? Enumerable.Empty<QuestAtom>())
            {
                if (atom == null ||
                    !string.Equals(atom.AtomType, atomType, StringComparison.OrdinalIgnoreCase))
                    continue;

                var sanitized = SanitizeAtom(atom);
                if (sanitized != null)
                    return sanitized;
            }

            return null;
        }

        private static QuestAtom BuildReturnAtom(QuestMechanic source, string fallbackLabel)
        {
            var existing = GetFirstSanitizedAtom(source, "RETURN_TO_NPC");
            if (existing != null)
            {
                EnsureLabel(existing, fallbackLabel);
                return existing;
            }

            return new QuestAtom
            {
                AtomType = "RETURN_TO_NPC",
                Label = fallbackLabel,
                Params = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        private static void EnsureLabel(QuestAtom atom, string fallbackLabel)
        {
            if (string.IsNullOrWhiteSpace(atom.Label))
                atom.Label = fallbackLabel;
        }

        private static QuestAtom? SanitizeAtom(QuestAtom? atom)
        {
            if (atom == null || string.IsNullOrWhiteSpace(atom.AtomType))
                return null;

            string atomType = atom.AtomType.Trim().ToUpperInvariant();
            if (!AllowedAtoms.Contains(atomType))
                return null;

            var sanitized = new QuestAtom
            {
                AtomType = atomType,
                Label = SanitizeLabel(atom.Label),
                Params = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            switch (atomType)
            {
                case "VISIT_SETTLEMENT":
                case "LEAVE_SETTLEMENT":
                    return CopySettlementAtom(atom, sanitized);

                case "DEFEAT_PARTY":
                    return CopyDefeatPartyAtom(atom, sanitized);

                case "TALK_TO_PARTY":
                    return CopyTalkToPartyAtom(atom, sanitized);

                case "BRING_ITEM":
                    return CopyBringItemAtom(atom, sanitized);

                case "BRING_TROOPS":
                    return CopyBringTroopsAtom(atom, sanitized);

                case "BRING_PRISONER_HERO":
                    return CopyBringPrisonerHeroAtom(atom, sanitized);

                case "WIN_TOURNAMENT":
                    return CopyWinTournamentAtom(atom, sanitized);

                case "RETURN_TO_NPC":
                    return sanitized;

                default:
                    return null;
            }
        }

        private static QuestAtom? CopySettlementAtom(QuestAtom source, QuestAtom target)
        {
            string settlementId = source.GetParam("settlement_id").Trim();
            if (string.IsNullOrWhiteSpace(settlementId) ||
                !QuestAtomCatalog.IsValidSettlementId(settlementId))
                return null;

            target.Params["settlement_id"] = settlementId;
            return target;
        }

        private static QuestAtom? CopyDefeatPartyAtom(QuestAtom source, QuestAtom target)
        {
            string factionId = source.GetParam("faction_id").Trim();
            string partyId = source.GetParam("party_id").Trim();
            string heroId = source.GetParam("hero_id").Trim();

            if (!string.IsNullOrWhiteSpace(factionId) &&
                !QuestAtomCatalog.IsValidFactionId(factionId))
                factionId = "";

            if (string.IsNullOrWhiteSpace(factionId) &&
                string.IsNullOrWhiteSpace(partyId) &&
                string.IsNullOrWhiteSpace(heroId))
                return null;

            if (!string.IsNullOrWhiteSpace(factionId))
                target.Params["faction_id"] = factionId;
            if (!string.IsNullOrWhiteSpace(partyId))
                target.Params["party_id"] = partyId;
            if (!string.IsNullOrWhiteSpace(heroId))
                target.Params["hero_id"] = heroId;

            string factionName = source.GetParam("faction_name").Trim();
            if (!string.IsNullOrWhiteSpace(factionName))
                target.Params["faction_name"] = factionName;

            target.Params["count"] = ClampCount(source.GetParamInt("count", 1)).ToString();
            return target;
        }

        private static QuestAtom? CopyTalkToPartyAtom(QuestAtom source, QuestAtom target)
        {
            string factionId = source.GetParam("faction_id").Trim();
            string partyId = source.GetParam("party_id").Trim();
            string heroId = source.GetParam("hero_id").Trim();
            string settlementId = source.GetParam("settlement_id").Trim();

            if (!string.IsNullOrWhiteSpace(factionId) &&
                !QuestAtomCatalog.IsValidFactionId(factionId))
                factionId = "";

            if (string.IsNullOrWhiteSpace(factionId) &&
                string.IsNullOrWhiteSpace(partyId) &&
                string.IsNullOrWhiteSpace(heroId))
                return null;

            if (!string.IsNullOrWhiteSpace(factionId))
                target.Params["faction_id"] = factionId;
            if (!string.IsNullOrWhiteSpace(partyId))
                target.Params["party_id"] = partyId;
            if (!string.IsNullOrWhiteSpace(heroId))
                target.Params["hero_id"] = heroId;

            if (!string.IsNullOrWhiteSpace(settlementId) &&
                QuestAtomCatalog.IsValidSettlementId(settlementId))
            {
                target.Params["settlement_id"] = settlementId;
                target.Params["radius"] = ClampRadius(source.GetParamInt("radius", 80)).ToString();
            }

            target.Params["count"] = ClampCount(source.GetParamInt("count", 1)).ToString();
            return target;
        }

        private static QuestAtom? CopyBringItemAtom(QuestAtom source, QuestAtom target)
        {
            string itemId = source.GetParam("item_id").Trim();
            if (string.IsNullOrWhiteSpace(itemId) || !AllowedItemIds.Contains(itemId))
                return null;

            target.Params["item_id"] = itemId;
            target.Params["quantity"] = ClampCount(source.GetParamInt("quantity", 1)).ToString();
            return target;
        }

        private static QuestAtom? CopyBringTroopsAtom(QuestAtom source, QuestAtom target)
        {
            target.Params["troop_count"] = ClampCount(source.GetParamInt("troop_count", 1)).ToString();
            return target;
        }

        private static QuestAtom? CopyBringPrisonerHeroAtom(QuestAtom source, QuestAtom target)
        {
            string heroId = source.GetParam("hero_id").Trim();
            if (string.IsNullOrWhiteSpace(heroId))
                return null;

            target.Params["hero_id"] = heroId;

            string factionId = source.GetParam("faction_id").Trim();
            if (!string.IsNullOrWhiteSpace(factionId) &&
                QuestAtomCatalog.IsValidFactionId(factionId))
                target.Params["faction_id"] = factionId;

            return target;
        }

        private static QuestAtom? CopyWinTournamentAtom(QuestAtom source, QuestAtom target)
        {
            target.Params["count"] = ClampCount(source.GetParamInt("count", 1)).ToString();

            string townId = source.GetParam("town_id").Trim();
            if (!string.IsNullOrWhiteSpace(townId) &&
                QuestAtomCatalog.IsValidSettlementId(townId))
                target.Params["town_id"] = townId;

            return target;
        }

        private static int ClampCount(int value) => Math.Max(1, Math.Min(20, value));

        private static int ClampRadius(int value) => Math.Max(20, Math.Min(150, value <= 0 ? 80 : value));

        private static int ClampRewardGold(int value) => Math.Max(0, Math.Min(AIConfig.MaxGoldTransfer, value));

        private static int NormalizeRewardGold(string questKind, int requested)
        {
            int min = 150;
            int suggested = 300;
            int max = AIConfig.MaxGoldTransfer;

            switch ((questKind ?? "").Trim().ToLowerInvariant())
            {
                case "travel_report":
                    min = 150;
                    suggested = 300;
                    break;
                case "delivery":
                    min = 200;
                    suggested = 350;
                    break;
                case "recruitment":
                    min = 300;
                    suggested = 500;
                    break;
                case "scouting":
                    min = 250;
                    suggested = 450;
                    break;
                case "delivery_under_pressure":
                    min = 400;
                    suggested = 700;
                    break;
                case "retaliation":
                    min = 500;
                    suggested = 900;
                    break;
                case "escort_with_ambush":
                    min = 600;
                    suggested = 1000;
                    break;
                case "capture_prisoner":
                    min = 700;
                    suggested = 1200;
                    break;
                case "rescue_prisoner_noble":
                    min = 900;
                    suggested = 1500;
                    break;
                case "courtship_tournament":
                    min = 300;
                    suggested = 600;
                    break;
            }

            int value = requested <= 0 ? suggested : requested;
            if (value < min)
                value = min;

            return ClampRewardGold(Math.Min(value, max));
        }

        private static int ClampDurationDays(int value) => Math.Max(1, Math.Min(120, value <= 0 ? 30 : value));

        private static bool IsQuestKindAppropriateForNpc(string questKind, Hero? questGiver)
        {
            if (questGiver == null)
                return true;

            try
            {
                var profile = AIRequestNeedEvaluator.Evaluate(questGiver);
                if (profile.AllowedQuestKinds.Count == 0)
                    return true;

                bool allowed = profile.AllowedQuestKinds.Any(k =>
                    k.Equals(questKind, StringComparison.OrdinalIgnoreCase));

                if (!allowed)
                    RFAIDebug.Log($"QuestMechanicValidator: rejected {questKind} for {questGiver.StringId} - not in allowed_quest_kinds");

                return allowed;
            }
            catch
            {
                return true;
            }
        }

        private static bool PassesQualityRules(QuestMechanic mechanic, Hero? questGiver)
        {
            try
            {
                if (questGiver == null)
                    return true;

                if (!PassesWarTargetRules(mechanic, questGiver))
                    return false;

                if (!PassesLocalityRules(mechanic, questGiver))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QuestMechanicValidator: quality rules failed open: {ex.GetType().Name}: {ex.Message}");
                return true;
            }
        }

        private static bool PassesWarTargetRules(QuestMechanic mechanic, Hero questGiver)
        {
            bool isWarQuest =
                mechanic.QuestKind.Equals("retaliation", StringComparison.OrdinalIgnoreCase) ||
                mechanic.QuestKind.Equals("scouting", StringComparison.OrdinalIgnoreCase) ||
                mechanic.QuestKind.Equals("capture_prisoner", StringComparison.OrdinalIgnoreCase);

            if (!isWarQuest || questGiver.MapFaction == null)
                return true;

            foreach (var atom in mechanic.Objectives)
            {
                string factionId = atom.GetParam("faction_id");
                if (string.IsNullOrWhiteSpace(factionId))
                    continue;

                var targetFaction = FindKingdom(factionId);
                if (targetFaction == null)
                    continue;

                if (targetFaction == questGiver.MapFaction)
                {
                    RFAIDebug.Log($"QuestMechanicValidator: rejected {mechanic.QuestKind} - target faction is quest giver faction ({factionId})");
                    return false;
                }

                if (questGiver.Occupation == Occupation.Lord &&
                    !FactionManager.IsAtWarAgainstFaction(questGiver.MapFaction, targetFaction))
                {
                    RFAIDebug.Log($"QuestMechanicValidator: rejected {mechanic.QuestKind} - {questGiver.MapFaction.StringId} is not at war with {factionId}");
                    return false;
                }
            }

            return true;
        }

        private static bool PassesLocalityRules(QuestMechanic mechanic, Hero questGiver)
        {
            var profile = AIRequestNeedEvaluator.Evaluate(questGiver);
            if (!profile.LocalityScope.Equals("local", StringComparison.OrdinalIgnoreCase))
                return true;

            var nearby = AIRequestNeedEvaluator.GetNearbySettlements(questGiver, maxCount: 8);
            if (nearby == null || nearby.Count == 0)
                return true;

            var allowedSettlementIds = new HashSet<string>(
                nearby.Where(s => s != null).Select(s => s.StringId),
                StringComparer.OrdinalIgnoreCase);

            foreach (var atom in mechanic.Objectives)
            {
                string settlementId = atom.GetParam("settlement_id");
                if (string.IsNullOrWhiteSpace(settlementId))
                    continue;

                if (!allowedSettlementIds.Contains(settlementId))
                {
                    RFAIDebug.Log($"QuestMechanicValidator: rejected {mechanic.QuestKind} - settlement {settlementId} outside local scope for {questGiver.StringId}");
                    return false;
                }
            }

            return true;
        }

        private static Kingdom? FindKingdom(string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId))
                return null;

            try
            {
                return Kingdom.All.FirstOrDefault(k =>
                    k != null &&
                    !k.IsEliminated &&
                    k.StringId.Equals(factionId, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private static string SanitizeLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return "";

            string trimmed = label.Trim().Replace("\r", " ").Replace("\n", " ");
            return trimmed.Length <= 160 ? trimmed : trimmed.Substring(0, 160);
        }
    }
}
