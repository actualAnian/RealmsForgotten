using HuntableHerds.Models;
using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.HuntableHerds.AgentComponents
{
    public class LootableAgentComponent : AgentComponent
    {
        private readonly ItemRoster itemDrops;
        public int GoldDrop { get { return _goldDrop; } }
        public Vec3 LootArea { get; }

        private int _goldDrop = 0;
        public LootableAgentComponent(Agent agent, ItemDropsData itemDrops) : base(agent)
        {
            LootArea = itemDrops.LootArea;
            this.itemDrops = RandomizeLoot(itemDrops);
        }
        public ItemRoster RandomizeLoot(ItemDropsData itemDrops)
        {
            ItemRoster itemRoster = new();
            foreach (ItemDrop drop in itemDrops.ItemDrops)
            {
                if (MBRandom.RandomFloatRanged(0f, 1f) >= drop.DropChance)
                    continue;
                int amount;
                if (drop.AmountMax > drop.AmountMin)
                    amount = MBRandom.RandomInt(drop.AmountMin, drop.AmountMax);
                else amount = drop.AmountMax;
                if (drop.ItemId == "gold")
                    _goldDrop += amount;
                ItemObject? item;
                try
                {
                    item = Game.Current.ObjectManager.GetObject<ItemObject>(drop.ItemId);
                }
                catch (NullReferenceException)
                {
                    continue;
                }
                if (item == null)
                    continue;
                itemRoster.AddToCounts(item, amount);
            }
            return itemRoster;
        }
        public ItemRoster GetItemDrops()
        {
            return itemDrops;
        }

        public void ClearItemDrops()
        {
            itemDrops.Clear();
        }
    }
}