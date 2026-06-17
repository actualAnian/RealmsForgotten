using RealmsForgotten.MissionEffects.EffectDurationTypes;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MissionEffects.MissionEffectTypes
{
    public abstract class TickMissionEffect : MissionEffect
    {
        public abstract float BaseTickInterval { get; }
        private float _tickAccumulator;

        protected TickMissionEffect(Agent belongsTo, IEffectDuration duration) : base(belongsTo, duration) { }
        public override void OnUpdate(float dt)
        {
            _tickAccumulator += dt;

            if (ShouldActivate())
            {
                OnActivate();
                _tickAccumulator = 0f;
            }
        }
        public virtual void OnActivate()
        {

        }
        private bool ShouldActivate()
        {
            return _tickAccumulator >= BaseTickInterval;
        }
    }
}
