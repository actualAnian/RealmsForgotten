using TaleWorlds.Core;

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
