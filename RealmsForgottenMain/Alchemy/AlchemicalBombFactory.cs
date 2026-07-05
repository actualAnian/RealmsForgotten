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
            if (id == "heat_bomb") return new HeatBomb(position, caster);
            if (id == "smoke_bomb") return new SmokeBomb(position, caster);
            if (id == "horse_bane") return new HorseBane(position, caster);
            if (id == "panic_dust") return new PanicDust(position, caster);
            return null;
        }
    }
}