using System;
using System.Collections.Generic;
using System.Linq;

namespace RF_AIDialog
{
    /// <summary>
    /// Sanitizes LLM-generated quest mechanics before they become real quests.
    /// Unknown atoms are dropped, invalid params are corrected when possible,
    /// and impossible objective orderings are normalized.
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
                "RETURN_TO_NPC"
            };

        private static readonly HashSet<string> AllowedItemIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "grain", "wine", "hides", "linen", "tools",
                "silver_ore", "wool", "pottery", "salt", "dates"
            };

        public static QuestMechanic? Sanitize(QuestMechanic? mechanic)
        {
            if (mechanic == null)
                return null;

            var sanitized = new QuestMechanic
            {
                RewardGold = ClampRewardGold(mechanic.RewardGold),
                DurationDays = ClampDurationDays(mechanic.DurationDays)
            };

            if (mechanic.Objectives == null)
                return null;

            var returnObjectives = new List<QuestAtom>();
            foreach (var atom in mechanic.Objectives)
            {
                var normalized = SanitizeAtom(atom);
                if (normalized == null)
                    continue;

                if (normalized.AtomType.Equals("RETURN_TO_NPC", StringComparison.OrdinalIgnoreCase))
                    returnObjectives.Add(normalized);
                else
                    sanitized.Objectives.Add(normalized);
            }

            sanitized.Objectives.AddRange(returnObjectives);
            sanitized.Normalize();

            return sanitized.Objectives.Count == 0 ? null : sanitized;
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

                case "RETURN_TO_NPC":
                    return sanitized;

                default:
                    return null;
            }
        }

        private static QuestAtom? CopySettlementAtom(QuestAtom source, QuestAtom target)
        {
            string settlementId = source.GetParam("settlement_id").Trim();
            if (string.IsNullOrWhiteSpace(settlementId))
                return null;

            target.Params["settlement_id"] = settlementId;
            return target;
        }

        private static QuestAtom? CopyDefeatPartyAtom(QuestAtom source, QuestAtom target)
        {
            string factionId = source.GetParam("faction_id").Trim();
            if (string.IsNullOrWhiteSpace(factionId))
                return null;

            target.Params["faction_id"] = factionId;

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
