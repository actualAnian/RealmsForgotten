using System.Collections.Generic;
using TaleWorlds.Library;

namespace HuntableHerds.Models
{
    public class ItemDrop
    {
        public string ItemId { get; }
        public int AmountMin { get; }
        public int AmountMax { get; }
        public double DropChance { get; }
        public ItemDrop(string itemId, int amountMin, int amountMax, double dropChance)
        {
            ItemId = itemId;
            AmountMin = amountMin;
            AmountMax = amountMax;
            if (dropChance < 0 || dropChance > 1)
            {
                dropChance = 0;
            }
            DropChance = dropChance;
        }
    }
    public class ItemDropsData
    {
        private static Vec3 _smallLootArea = new(1f, 1f, 1f);
        private static Vec3 _mediumLootArea = new(1.5f, 1.5f, 1.5f);
        private static Vec3 _largeLootArea = new(3f, 3f, 3f);
        public ItemDropsData(List<ItemDrop> itemDrops, string dropsId, string? lootArea = null)
        {
            ItemDrops = itemDrops;
            DropsId = dropsId;
            LootArea = lootArea switch
            {
                "Small" => _smallLootArea,
                "Large" => _largeLootArea,
                _ => _mediumLootArea,
            };
        }

        public Vec3 LootArea { get; }
        public List<ItemDrop> ItemDrops { get; }
        public string DropsId { get; }
    }
}
