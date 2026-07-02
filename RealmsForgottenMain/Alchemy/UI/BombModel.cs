using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Alchemy.UI
{
    public class BombModel
    {
        public ItemObject Item { get; }
        public int Count { get; private set; }

        public BombModel(ItemObject item, int count)
        {
            Item = item;
            Count = count;
        }
    }
}
