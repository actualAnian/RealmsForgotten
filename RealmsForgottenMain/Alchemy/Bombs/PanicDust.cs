using RealmsForgotten.MissionEffects;
using RealmsForgotten.MissionEffects.EffectDurationTypes;
using RealmsForgotten.MissionEffects.MissionEffectTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Alchemy.Bombs
{
    internal class PanicDust : AbstractBomb
    {
        public PanicDust(Vec3 center, Agent caster) : base(center, caster) { }
        public override string Description
        {
            get
            {
                return "TODO";
            }
        }

        internal override float BaseDuration => 1f;

        internal override float BaseRadius => 6f;

        public override void OnAgentEntered(Agent agent)
        {
            var effect = new PanicEffect(agent, new TimeBasedDuration(BaseDuration));
            MissionEffectsBehavior.Instance?.ApplyEffect(effect);
        }
        public override void OnLeft(Agent agent)
        {
            MissionEffectsBehavior.Instance?.RemoveEffect(agent, typeof(PanicEffect));
        }
    }
}
