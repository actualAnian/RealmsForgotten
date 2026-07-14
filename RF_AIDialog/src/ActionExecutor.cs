using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
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
                case "go_to_settlement":
                    GoToSettlement(npc, action.SettlementId, action.Reason);
                    break;
                case "wait_near_settlement":
                    WaitNearSettlement(npc, action.SettlementId, action.Hours, action.Reason);
                    break;
                case "patrol_settlement":
                    PatrolSettlement(npc, action.SettlementId, action.Radius, action.Reason);
                    break;
                case "attack_party":
                    AttackParty(npc, action.PartyId, action.Reason);
                    break;
                case "raid_village":
                    RaidVillage(npc, action.SettlementId, action.Reason);
                    break;
                case "siege_settlement":
                    SiegeSettlement(npc, action.SettlementId, action.Reason);
                    break;
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
            // Clamp to MaxGoldTransfer — the LLM must not be coaxed into
            // draining an NPC's entire treasury in a single action.
            amount = Math.Min(amount, MaxGoldTransfer);
            if (amount <= 0 || npc.Gold < amount) return;
            npc.ChangeHeroGold(-amount);
            Hero.MainHero.ChangeHeroGold(amount);
            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name} gave you {amount} gold.", Color.FromUint(0xFF_FF_D7_00u)));
        }

        private static void TakeGold(Hero npc, int amount)
        {
            // Clamp to MaxGoldTransfer — cap how much the player can be made to pay.
            amount = Math.Min(amount, MaxGoldTransfer);
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

        private static void GoToSettlement(Hero npc, string? settlementId, string? reason)
        {
            var party = ValidateNpcPartyOrder(npc, settlementId, out Settlement? settlement);
            if (party == null || settlement == null)
                return;

            var navigationType = ResolveNavigationType(party);
            bool isFromPort = party.IsCurrentlyAtSea;
            SetPartyAiAction.GetActionForVisitingSettlement(
                party,
                settlement,
                navigationType,
                isFromPort,
                isTargetingPort: false);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name}'s party is moving to {settlement.Name}." + AppendReason(reason),
                Color.FromUint(0xFF_80_FF_80u)));
        }

        private static void WaitNearSettlement(Hero npc, string? settlementId, int hours, string? reason)
        {
            var party = ValidateNpcPartyOrder(npc, settlementId, out Settlement? settlement);
            if (party == null || settlement == null)
                return;

            int clampedHours = Math.Max(1, Math.Min(72, hours <= 0 ? 12 : hours));
            var navigationType = ResolveNavigationType(party);
            bool isFromPort = party.IsCurrentlyAtSea;

            // Bannerlord has no simple "go there, then hold for N hours" single AI action.
            // The safest approximation is a light patrol around the target settlement.
            SetPartyAiAction.GetActionForPatrollingAroundSettlement(
                party,
                settlement,
                navigationType,
                isFromPort,
                isTargetingPort: false);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name}'s party will wait near {settlement.Name} for a while (about {clampedHours}h)." + AppendReason(reason),
                Color.FromUint(0xFF_80_FF_80u)));
        }

        private static void PatrolSettlement(Hero npc, string? settlementId, int radius, string? reason)
        {
            var party = ValidateNpcPartyOrder(npc, settlementId, out Settlement? settlement);
            if (party == null || settlement == null)
                return;

            int clampedRadius = Math.Max(4, Math.Min(20, radius <= 0 ? 10 : radius));
            var navigationType = ResolveNavigationType(party);
            bool isFromPort = party.IsCurrentlyAtSea;
            SetPartyAiAction.GetActionForPatrollingAroundSettlement(
                party,
                settlement,
                navigationType,
                isFromPort,
                isTargetingPort: false);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name}'s party is patrolling around {settlement.Name} (radius {clampedRadius})." + AppendReason(reason),
                Color.FromUint(0xFF_80_FF_80u)));
        }

        private static void AttackParty(Hero npc, string? targetPartyId, string? reason)
        {
            var party = ValidateNpcLeaderParty(npc);
            if (party == null)
                return;

            if (string.IsNullOrWhiteSpace(targetPartyId))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[AI] Attack order rejected: no target party id was provided.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            MobileParty? targetParty = MobileParty.All
                .FirstOrDefault(p => string.Equals(p.StringId, targetPartyId, StringComparison.OrdinalIgnoreCase));

            if (targetParty == null || !targetParty.IsActive)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Attack order rejected: party '{targetPartyId}' is invalid or inactive.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (targetParty == party)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[AI] Attack order rejected: a party cannot attack itself.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (targetParty.MapEvent != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Attack order rejected: {targetParty.Name} is already in an encounter.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (party.MapFaction == null || targetParty.MapFaction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[AI] Attack order rejected: one of the parties has no valid faction.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (!party.MapFaction.IsAtWarWith(targetParty.MapFaction))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Attack order rejected: {npc.Name} is not at war with {targetParty.Name}.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            var navigationType = ResolveNavigationType(party);
            bool isFromPort = party.IsCurrentlyAtSea;
            SetPartyAiAction.GetActionForEngagingParty(
                party,
                targetParty,
                navigationType,
                isFromPort);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name}'s party is moving to engage {targetParty.Name}." + AppendReason(reason),
                Color.FromUint(0xFF_80_FF_80u)));
        }

        private static void RaidVillage(Hero npc, string? settlementId, string? reason)
        {
            var party = ValidateNpcPartyOrder(npc, settlementId, out Settlement? settlement);
            if (party == null || settlement == null)
                return;

            if (!settlement.IsVillage)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Raid order rejected: {settlement.Name} is not a village.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (settlement.Village == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Raid order rejected: {settlement.Name} has no valid village data.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (party.MapFaction == null || settlement.MapFaction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[AI] Raid order rejected: invalid faction state.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (!party.MapFaction.IsAtWarWith(settlement.MapFaction))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Raid order rejected: {npc.Name} is not at war with {settlement.Name}.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (settlement.Party.MapEvent != null || settlement.SiegeEvent != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Raid order rejected: {settlement.Name} is already in an active conflict.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (settlement.Village.VillageState != Village.VillageStates.Normal)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Raid order rejected: {settlement.Name} is not in a normal state for raiding.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            var navigationType = ResolveNavigationType(party);
            bool isFromPort = party.IsCurrentlyAtSea;
            SetPartyAiAction.GetActionForRaidingSettlement(
                party,
                settlement,
                navigationType,
                isFromPort,
                isTargetingPort: false);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name}'s party is moving to raid {settlement.Name}." + AppendReason(reason),
                Color.FromUint(0xFF_FF_A0_00u)));
        }

        private static void SiegeSettlement(Hero npc, string? settlementId, string? reason)
        {
            var party = ValidateNpcPartyOrder(npc, settlementId, out Settlement? settlement);
            if (party == null || settlement == null)
                return;

            if (!settlement.IsFortification)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Siege order rejected: {settlement.Name} is not a fortification.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (party.MapFaction == null || settlement.MapFaction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[AI] Siege order rejected: invalid faction state.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (!party.MapFaction.IsAtWarWith(settlement.MapFaction))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Siege order rejected: {npc.Name} is not at war with {settlement.Name}.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (party.BesiegedSettlement != null && party.BesiegedSettlement != settlement)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Siege order rejected: {npc.Name}'s party is already committed to another siege.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (party.IsCurrentlyAtSea)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[AI] Siege order rejected: the party is currently at sea.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (settlement.Party.MapEvent != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Siege order rejected: {settlement.Name} is already in an active encounter.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            if (settlement.SiegeEvent != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Siege order rejected: {settlement.Name} is already under siege.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return;
            }

            var navigationType = ResolveNavigationType(party);
            bool isFromPort = party.IsCurrentlyAtSea;
            SetPartyAiAction.GetActionForBesiegingSettlement(
                party,
                settlement,
                navigationType,
                isFromPort);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {npc.Name}'s party is moving to besiege {settlement.Name}." + AppendReason(reason),
                Color.FromUint(0xFF_FF_A0_00u)));
        }

        private static MobileParty? ValidateNpcPartyOrder(Hero npc, string? settlementId, out Settlement? settlement)
        {
            settlement = null;

            var party = ValidateNpcLeaderParty(npc);
            if (party == null)
                return null;

            if (string.IsNullOrWhiteSpace(settlementId))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[AI] Party order rejected: no target settlement id was provided.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return null;
            }

            settlement = MBObjectManager.Instance.GetObject<Settlement>(settlementId);
            if (settlement == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Party order rejected: settlement '{settlementId}' is invalid.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return null;
            }

            if (!settlement.IsActive)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] Party order rejected: {settlement.Name} is not active.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return null;
            }

            return party;
        }

        private static MobileParty? ValidateNpcLeaderParty(Hero npc)
        {
            if (!npc.IsLord)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] {npc.Name} cannot issue campaign party orders.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return null;
            }

            var party = npc.PartyBelongedTo;
            if (party == null || party.MapEvent != null || party.Army != null && party.Army.LeaderParty != party)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] {npc.Name}'s party is not in a safe state to receive map orders.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return null;
            }

            if (party.LeaderHero != npc)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI] {npc.Name} is not leading the party that would receive this order.",
                    Color.FromUint(0xFF_FF_60_60u)));
                return null;
            }

            return party;
        }

        private static MobileParty.NavigationType ResolveNavigationType(MobileParty party)
        {
            return party.IsCurrentlyAtSea
                ? MobileParty.NavigationType.Naval
                : MobileParty.NavigationType.Default;
        }

        private static string AppendReason(string? reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Reason: {reason}";
        }
    }
}
