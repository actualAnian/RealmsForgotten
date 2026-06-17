using RealmsForgotten.Alchemy.Bombs;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Alchemy
{
    public static class AlchemicalBombFactory
    {
        public static AbstractBomb? GetBombType(Agent caster, Vec3 position, ItemObject item)
        {
            if (item == null) return null;
            var id = item.StringId;
            if (id.Contains("anorit_fire")) return new HorseBane(position, caster);
            if (id.Contains("smoke_bomb"))
            {
                return new SmokeBomb(caster, position);
            }
            return null;
        }
    }
}
