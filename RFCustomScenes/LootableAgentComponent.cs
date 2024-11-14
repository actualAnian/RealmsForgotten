using RealmsForgotten.HuntableHerds.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static RealmsForgotten.RFCustomSettlements.CustomSettlementBuildData;

namespace RFCustomSettlements
{
    internal class LootableAgentComponent : AgentComponent
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
