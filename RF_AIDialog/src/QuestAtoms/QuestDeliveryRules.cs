using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace RF_AIDialog
{
    public static class QuestDeliveryRules
    {
        public static readonly HashSet<string> KnownDeliveryItemIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "grain", "wine", "hides", "linen", "tools",
                "silver_ore", "iron_ore", "wool", "pottery", "salt", "dates",
                "cloth", "leather", "oil", "beer", "velvet", "flax", "cotton",
                "fish", "meat", "butter", "cheese", "olives", "fur", "wood"
            };

        public static HashSet<string> GetAllowedDeliveryItemIds(Hero? npc)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (npc == null)
                return result;

            try
            {
                switch (npc.Occupation)
                {
                    case Occupation.Merchant:
                        foreach (string id in KnownDeliveryItemIds)
                            result.Add(id);
                        AddWorkshopGoods(npc, result);
                        break;

                    case Occupation.Artisan:
                        AddWorkshopGoods(npc, result);
                        break;

                    case Occupation.RuralNotable:
                    case Occupation.Headman:
                        AddVillageProductionGoods(npc, result);
                        break;
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QuestDeliveryRules.GetAllowedDeliveryItemIds failed for {npc.StringId}: {ex.Message}");
            }

            return result;
        }

        public static bool IsDeliveryItemAppropriate(Hero? npc, string itemId)
        {
            if (npc == null || string.IsNullOrWhiteSpace(itemId))
                return true;

            if (npc.IsLord || npc.Occupation == Occupation.Wanderer)
                return true;

            var allowed = GetAllowedDeliveryItemIds(npc);
            if (allowed.Count == 0)
                return false;

            return allowed.Contains(itemId);
        }

        public static ItemObject? ResolveDeliveryItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            var objectManager = MBObjectManager.Instance;
            var direct = objectManager.GetObject<ItemObject>(itemId);
            if (direct != null)
                return direct;

            var category = objectManager.GetObject<ItemCategory>(itemId);
            if (category == null)
                return null;

            return objectManager.GetObjectTypeList<ItemObject>()
                .Where(item => item != null &&
                               item.Type == ItemObject.ItemTypeEnum.Goods &&
                               !item.NotMerchandise &&
                               item.ItemCategory != null &&
                               item.ItemCategory.StringId.Equals(category.StringId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Value)
                .FirstOrDefault();
        }

        private static void AddWorkshopGoods(Hero npc, HashSet<string> result)
        {
            if (npc.OwnedWorkshops == null)
                return;

            foreach (var workshop in npc.OwnedWorkshops)
            {
                var workshopType = workshop?.WorkshopType;
                if (workshopType == null || workshopType.IsHidden)
                    continue;

                foreach (var production in workshopType.Productions)
                {
                    foreach (var input in production.Inputs)
                        AddCategoryGoods(input.Item1, result);

                    foreach (var output in production.Outputs)
                        AddCategoryGoods(output.Item1, result);
                }
            }
        }

        private static void AddVillageProductionGoods(Hero npc, HashSet<string> result)
        {
            var village = npc.CurrentSettlement?.Village ?? npc.HomeSettlement?.Village;
            var productions = village?.VillageType?.Productions;
            if (productions == null)
                return;

            foreach (var production in productions)
            {
                if (production.Item1 != null)
                    result.Add(production.Item1.StringId);
            }
        }

        private static void AddCategoryGoods(ItemCategory? category, HashSet<string> result)
        {
            if (category == null)
                return;

            result.Add(category.StringId);

            var item = ResolveDeliveryItem(category.StringId);
            if (item != null)
                result.Add(item.StringId);
        }
    }
}
