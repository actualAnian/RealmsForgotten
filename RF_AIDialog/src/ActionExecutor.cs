using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// Executes game actions returned by the LLM in the "actions" JSON field.
    /// Called on the main thread after the NPC's response is shown.
    ///
    /// All actions include sanity guards — the LLM cannot break the game
    /// by requesting unreasonable values.
    /// </summary>
    public static class ActionExecutor
    {
        // Hard caps pulled from AIConfig
        private static int MaxRelationDelta => AIConfig.MaxRelationDelta;
        private static int MaxGoldTransfer  => AIConfig.MaxGoldTransfer;

        // Items the LLM is allowed to give or take (must match IDs in PromptBuilder)
        private static readonly HashSet<string> AllowedItemIds = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "grain", "wine", "hides", "linen", "tools",
            "silver_ore", "wool", "pottery", "salt", "dates"
        };

        // ── Entry point ───────────────────────────────────────────────────

        public static void Execute(List<AIAction>? actions, Hero npc)
        {
            if (actions == null || actions.Count == 0) return;

            // Guard: take_item must always be paired with give_item or give_gold.
            // If the LLM fires take_item alone it means it treated the item as a
            // "deposit to inspect later" — reject the whole batch so the player
            // keeps their goods and can re-negotiate.
            bool hasTakeItem = actions.Any(a =>
                string.Equals(a.Type, "take_item", StringComparison.OrdinalIgnoreCase));
            bool hasReturn = actions.Any(a =>
                string.Equals(a.Type, "give_item", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Type, "give_gold", StringComparison.OrdinalIgnoreCase));

            if (hasTakeItem && !hasReturn)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] {npc.Name} has not agreed to give anything in return — no trade executed. Try offering again with clearer terms.",
                    Color.FromUint(0xFF_FF_A0_00u)));  // orange
                return;
            }

            foreach (var action in actions)
            {
                try   { ExecuteOne(action, npc); }
                catch (Exception ex)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[AI] Action '{action.Type}' failed: {ex.Message}",
                        Color.FromUint(0xFF_FF_60_60u)));
                }
            }
        }

        // ── Individual action handlers ────────────────────────────────────

        private static void ExecuteOne(AIAction action, Hero npc)
        {
            switch (action.Type?.ToLowerInvariant())
            {
                case "relation_change":
                    RelationChange(npc, action.Value);
                    break;

                case "give_gold":
                    GiveGold(npc, action.Value);
                    break;

                case "take_gold":
                    TakeGold(npc, action.Value);
                    break;

                case "give_item":
                    GiveItem(npc, action.ItemId, Math.Max(1, action.Value));
                    break;

                case "take_item":
                    TakeItem(npc, action.ItemId, Math.Max(1, action.Value));
                    break;

                case "assign_role":
                    AssignRole(npc, action.Role);
                    break;
            }
        }

        // ── relation_change ───────────────────────────────────────────────

        private static void RelationChange(Hero npc, int delta)
        {
            delta = Math.Max(-MaxRelationDelta, Math.Min(MaxRelationDelta, delta));
            if (delta == 0) return;

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                npc, Hero.MainHero, delta);

            string sign    = delta > 0 ? "+" : "";
            Color  color   = delta > 0
                ? Color.FromUint(0xFF_80_FF_80u)   // green
                : Color.FromUint(0xFF_FF_60_60u);  // red

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] Relation with {npc.Name}: {sign}{delta}.", color));
        }

        // ── give_gold ─────────────────────────────────────────────────────

        private static void GiveGold(Hero npc, int amount)
        {
            if (amount <= 0) return;

            if (npc.Gold < amount)
            {
                // NPC can't afford it — silently skip (they shouldn't have offered)
                return;
            }

            npc.ChangeHeroGold(-amount);
            Hero.MainHero.ChangeHeroGold(amount);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name} gave you {amount} gold.",
                Color.FromUint(0xFF_FF_D7_00u)));
        }

        // ── take_gold ─────────────────────────────────────────────────────

        private static void TakeGold(Hero npc, int amount)
        {
            if (amount <= 0) return;

            if (Hero.MainHero.Gold < amount)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] You don't have enough gold ({amount} required, you have {Hero.MainHero.Gold}).",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            Hero.MainHero.ChangeHeroGold(-amount);
            npc.ChangeHeroGold(amount);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] You paid {npc.Name} {amount} gold.",
                Color.FromUint(0xFF_FF_D7_00u)));
        }

        // ── give_item ─────────────────────────────────────────────────────

        private static void GiveItem(Hero npc, string? itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            if (!AllowedItemIds.Contains(itemId))  return;

            var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item == null) return;

            MobileParty.MainParty.ItemRoster.AddToCounts(item, quantity);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name} gave you {quantity}x {item.Name}.",
                Color.FromUint(0xFF_A0_D0_FFu)));  // light blue
        }

        // ── take_item ─────────────────────────────────────────────────────

        private static void TakeItem(Hero npc, string? itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            if (!AllowedItemIds.Contains(itemId))  return;

            var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item == null) return;

            int available = MobileParty.MainParty.ItemRoster.GetItemNumber(item);
            if (available < quantity)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] You don't have enough {item.Name} ({quantity} required, you have {available}).",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            MobileParty.MainParty.ItemRoster.AddToCounts(item, -quantity);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] You gave {npc.Na