using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace RealmsForgotten.WorldState.YoungWorld
{
    /// <summary>
    /// Extends the kingdom wall-stage cap to EQUIPMENT (author's rule: a realm
    /// of palisades neither forges nor sells elite steel):
    ///
    ///   1. SHOPS — a daily sweep removes over-cap weapons/armor/shields from
    ///      the market roster of every town whose kingdom is below stage
    ///      (palisades: item tiers 1-2; keeps: up to 4; fortresses: all).
    ///      The economy restocks daily and the sweep prunes right after, so
    ///      merchants of young realms simply never display elite gear.
    ///   2. SMITHY — the Harmony postfix below closes crafting PIECES whose
    ///      PieceTier exceeds the stage cap of the town the player is smithing
    ///      in (Settlement.CurrentSettlement), on top of vanilla unlocks.
    ///
    /// Deliberately excluded: Musket/Pistol/Bullets item types (RF magic staffs
    /// live in those classes — the magic economy stays untouched), horses and
    /// harness, banners, goods. Everything is gated by its own MCM toggle
    /// (default OFF) and never touches player/party inventories.
    /// </summary>
    public class RFEquipmentStageCapBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // stateless — the sweep recomputes from world state every day
        }

        private void OnDailyTick()
        {
            if (!RFWorldSettings.EquipmentCap)
            {
                return;
            }

            foreach (Town town in Town.AllTowns)
            {
                Settlement? settlement = town?.Settlement;
                if (settlement == null)
                {
                    continue;
                }

                int cap = RFWorldStageCap.GetKingdomCap(settlement.MapFaction as Kingdom);
                if (cap == RFWorldStageCap.NoCap)
                {
                    continue;
                }

                ItemRoster? roster = settlement.ItemRoster;
                if (roster == null)
                {
                    continue;
                }

                for (int i = roster.Count - 1; i >= 0; i--)
                {
                    ItemRosterElement element = roster[i];
                    ItemObject? item = element.EquipmentElement.Item;
                    if (item != null && IsCappedEquipment(item) && RFWorldStageCap.IsAboveCap(cap, GetItemTier(item)))
                    {
                        roster.AddToCounts(element.EquipmentElement, -element.Amount);
                    }
                }
            }
        }

        internal static int GetItemTier(ItemObject item)
        {
            return (int)item.Tier + 1; // ItemTiers.Tier1 == 0
        }

        internal static bool IsCappedEquipment(ItemObject item)
        {
            switch (item.ItemType)
            {
                case ItemObject.ItemTypeEnum.OneHandedWeapon:
                case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                case ItemObject.ItemTypeEnum.Polearm:
                case ItemObject.ItemTypeEnum.Bow:
                case ItemObject.ItemTypeEnum.Crossbow:
                case ItemObject.ItemTypeEnum.Thrown:
                case ItemObject.ItemTypeEnum.Arrows:
                case ItemObject.ItemTypeEnum.Bolts:
                case ItemObject.ItemTypeEnum.Shield:
                case ItemObject.ItemTypeEnum.HeadArmor:
                case ItemObject.ItemTypeEnum.BodyArmor:
                case ItemObject.ItemTypeEnum.LegArmor:
                case ItemObject.ItemTypeEnum.HandArmor:
                case ItemObject.ItemTypeEnum.ChestArmor:
                case ItemObject.ItemTypeEnum.Cape:
                    return true;
                default:
                    return false; // horses, goods, banners, Musket/Pistol (RF magic staffs)
            }
        }
    }

    /// <summary>
    /// Smithy gate: pieces above the local kingdom's stage read as not yet
    /// unlocked. Postfix keeps every vanilla/RFSmithing rule intact — it can
    /// only CLOSE pieces, never open them. Applied by the SubModule's
    /// uncategorized [HarmonyPatch] attribute sweep.
    /// </summary>
    [HarmonyPatch(typeof(CraftingCampaignBehavior), "IsOpened")]
    internal static class CraftingCampaignBehavior_IsOpened_StageCapPatch
    {
        private static void Postfix(ref bool __result, CraftingPiece craftingPiece)
        {
            if (!__result || !RFWorldSettings.EquipmentCap || craftingPiece == null)
            {
                return;
            }

            int cap = RFWorldStageCap.GetKingdomCap(Settlement.CurrentSettlement?.MapFaction as Kingdom);
            if (RFWorldStageCap.IsAboveCap(cap, craftingPiece.PieceTier))
            {
                __result = false;
            }
        }
    }
}
