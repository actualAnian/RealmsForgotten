using HuntableHerds.Models;
using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements
{
    public class LootableAgentComponent : AgentComponent
    {
        private ItemRoster itemDrops;

        public LootableAgentComponent(Agent agent, ItemDropsData itemDrops) : base(agent)
        {
            this.itemDrops = RandomizeLoot(itemDrops);
        }
        public ItemRoster RandomizeLoot(ItemDropsData itemDrops)
        {
            ItemRoster itemRoster = new();
            foreach (ItemDrop drop in itemDrops.ItemDrops)
            {
                ItemObject? item = null;
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
                int amount = 0;

                if (MBRandom.RandomFloatRanged(0f, 1f) < drop.DropChance)
                {
                    if (drop.AmountMax > drop.AmountMin)
                        amount = MBRandom.RandomInt(drop.AmountMin, drop.AmountMax);
                    else amount = drop.AmountMax;
                }
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
