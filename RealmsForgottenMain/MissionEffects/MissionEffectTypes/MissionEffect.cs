using RealmsForgotten.MissionEffects.EffectDurationTypes;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MissionEffects.MissionEffectTypes
{
    public abstract class MissionEffect
    {
        public Agent AgentEffectBelongsTo { get; internal set; }
        public IEffectDuration Duration { get; internal set; }
        protected MissionEffect(Agent belongsTo, IEffectDuration duration)
        {
            AgentEffectBelongsTo = belongsTo;
            Duration = duration;
        }

        public virtual void OnStart() { }
        public virtual void OnEnd() { }
        public virtual void OnUpdate(float dt) { }
        internal void SetDuration(IEffectDuration duration)
        {
            Duration = duration;
            Duration.OnStarted();
        }
    }
}