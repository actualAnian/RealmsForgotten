using System.Data.Odbc;
using TaleWorlds.Core;

namespace RealmsForgotten.RFEffects.Alchemy
{
    public static class AlchemicalBombFactory
    {
        public static IAlchemicalBomb? GetBombType(ItemObject item)
        {
            if (item == null) return null;
            var id = item.StringId;
            if (id.Contains("anorit_fire")) return new SmokeBomb();
            if (id.Contains("smoke_bomb"))
            {
                return new SmokeBomb();
            }
            return null;
        }
    }
}
