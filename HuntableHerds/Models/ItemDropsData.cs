using System.Collections.Generic;

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
        public ItemDropsData(List<ItemDrop> itemDrops, string dropsId)
        {
            ItemDrops = itemDrops;
            DropsId = dropsId;
        }

        public List<ItemDrop> ItemDrops { get; }
        public string DropsId { get; }
    }
}
