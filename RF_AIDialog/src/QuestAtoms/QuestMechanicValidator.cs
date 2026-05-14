using System;
using System.Collections.Generic;
using System.Linq;

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

        public static QuestMechanic? Sanitize(QuestMechanic? mechanic)
        {
            if (mechanic == null || mechanic.Objectives == null)
                return null;

            string questKind = ResolveQuestKind(mechanic);
            if (string.IsNullOrWhiteSpace(questKind))
                return null;

            var sanitized = new QuestMechanic
            {
                QuestKind = questKind,
                RewardGold = ClampRewardGold(mechanic.RewardGold),
                DurationDays = ClampDurationDays(mechanic.DurationDays)
            };

            if (!TryBuildCanonicalObjectives(questKind, mechanic, sanitized.Objectives))
                return null;

            sanitized.Normalize();
            return sanitized.Objectives.Count == 0 ? null : sanitized;
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

        private static int ClampRewardGold(int value) => Math.Max(0, Math.Min(AIConfig.MaxGoldTransfer, value));

        private static int ClampDurationDays(int value) => Math.Max(1, Math.Min(120, value <= 0 ? 30 : value));

        private static string SanitizeLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return "";

            string trimmed = label.Trim().Replace("\r", " ").Replace("\n", " ");
            return trimmed.Length <= 160 ? trimmed : trimmed.Substring(0, 160);
        }
    }
}
