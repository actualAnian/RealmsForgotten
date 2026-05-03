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
    public static class ActionExecutor
    {
        private static int MaxRelationDelta => AIConfig.MaxRelationDelta;
        private static int MaxGoldTransfer  => AIConfig.MaxGoldTransfer;

        private static readonly HashSet<string> AllowedItemIds = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "grain", "wine", "hides", "linen", "tools",
            "silver_ore", "wool", "pottery", "salt", "dates"
        };

        public static void Execute(List<AIAction>? actions, Hero npc)
        {
            if (actions == null || actions.Count == 0) return;

            bool hasTakeItem = actions.Any(a =>
                string.Equals(a.Type, "take_item", StringComparison.OrdinalIgnoreCase));
            bool hasReturn = actions.Any(a =>
                string.Equals(a.Type, "give_item", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Type, "give_gold", StringComparison.OrdinalIgnoreCase));

            if (hasTakeItem && !hasReturn)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] {npc.Name} has not agreed to give anything in return — no trade executed.",
                    Color.FromUint(0xFF_FF_A0_00u)));
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

        private static void ExecuteOne(AIAction action, Hero npc)
        {
            switch (action.Type?.ToLowerInvariant())
            {
                case "relation_change": RelationChange(npc, action.Value);               break;
                case "give_gold":       GiveGold(npc, action.Value);                     break;
                case "take_gold":       TakeGold(npc, action.Value);                     break;
                case "give_item":       GiveItem(npc, action.ItemId, Math.Max(1, action.Value)); break;
                case "take_item":       TakeItem(npc, action.ItemId, Math.Max(1, action.Value)); break;
                case "assign_role":     AssignRole(npc, action.Role);                    break;
                case "give_troops":     GiveTroops(npc, Math.Max(1, action.Value));      break;
            }
        }

        private static void RelationChange(Hero npc, int delta)
        {
            delta = Math.Max(-MaxRelationDelta, Math.Min(MaxRelationDelta, delta));
            if (delta == 0) return;
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(npc, Hero.MainHero, delta);
            string sign  = delta > 0 ? "+" : "";
            Color  color = delta > 0 ? Color.FromUint(0xFF_80_FF_80u) : Color.FromUint(0xFF_FF_60_60u);
            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] Relation with {npc.Name}: {sign}{delta}.", color));
        }

        private static void GiveGold(Hero npc, int amount)
        {
            if (amount <= 0 || npc.Gold < amount) return;
            npc.ChangeHeroGold(-amount);
            Hero.MainHero.ChangeHeroGold(amount);
            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name} gave you {amount} gold.", Color.FromUint(0xFF_FF_D7_00u)));
        }

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
                $"[AI] You paid {npc.Name} {amount} gold.", Color.FromUint(0xFF_FF_D7_00u)));
        }

        private static void GiveItem(Hero npc, string? itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !AllowedItemIds.Contains(itemId)) return;
            var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item == null) return;
            MobileParty.MainParty.ItemRoster.AddToCounts(item, quantity);
            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name} gave you {quantity}x {item.Name}.", Color.FromUint(0xFF_A0_D0_FFu)));
        }

        private static void TakeItem(Hero npc, string? itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !AllowedItemIds.Contains(itemId)) return;
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
                $"[AI] You gave {npc.Name} {quantity}x {item.Name}.", Color.FromUint(0xFF_A0_D0_FFu)));
        }

        private static void GiveTroops(Hero npc, int count)
        {
            var npcParty = npc.PartyBelongedTo;
            if (npcParty == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] {npc.Name} has no party to draw troops from.", Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            var available = npcParty.MemberRoster.GetTroopRoster()
                .Where(e => !e.Character.IsHero && e.Number > 0)
                .OrderByDescending(e => e.Number)
                .ToList();

            if (available.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] {npc.Name} has no troops to give.", Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            int remaining = Math.Min(count, 20);
            foreach (var element in available)
            {
                if (remaining <= 0) break;
                int toTransfer = Math.Min(remaining, element.Number);
                npcParty.MemberRoster.AddToCounts(element.Character, -toTransfer);
                MobileParty.MainParty.MemberRoster.AddToCounts(element.Character, toTransfer);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] {npc.Name} transferred {toTransfer}x {element.Character.Name} to your party.",
                    Color.FromUint(0xFF_80_FF_80u)));
                remaining -= toTransfer;
            }
        }

        private static void AssignRole(Hero npc, string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return;
            var party = MobileParty.MainParty;
            if (npc.PartyBelongedTo != party)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] {npc.Name} is not in your party — role assignment skipped.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            string roleLabel;
            switch (role.Trim().ToLowerInvariant())
            {
                case "engineer":     party.SetPartyEngineer(npc);     roleLabel = "Engineer";     break;
                case "scout":        party.SetPartyScout(npc);        roleLabel = "Scout";        break;
                case "surgeon":      party.SetPartySurgeon(npc);      roleLabel = "Surgeon";      break;
                case "quartermaster":party.SetPartyQuartermaster(npc);roleLabel = "Quartermaster";break;
                default:
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[AI] Unknown role '{role}' — valid: engineer, scout, surgeon, quartermaster.",
                        Color.FromUint(0xFF_FF_60_60u)));
                    return;
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name} is now your party's {roleLabel}.", Color.FromUint(0xFF_80_FF_80u)));
        }
    }
}
