using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects.Alchemy.Bombs
{
    internal class PanicDust : AbstractBomb
    {
        public PanicDust(Vec3 center, Agent caster) : base(center, caster) { }

        internal override float BaseDuration => 1f;

        internal override float BaseRadius => 6f;
    }
}
