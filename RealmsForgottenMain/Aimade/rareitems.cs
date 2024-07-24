using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Behaviors
{
    public class CustomItemCategories
    {
        public ItemCategory CustomItem { get; private set; }

        public void Initialize()
        {
            CustomItem = Game.Current.ObjectManager.RegisterPresumedObject(new ItemCategory("rare_items"));
            CustomItem.InitializeObject(true, 20, 5, ItemCategory.Property.BonusToFoodStores);
        }
    }
}
