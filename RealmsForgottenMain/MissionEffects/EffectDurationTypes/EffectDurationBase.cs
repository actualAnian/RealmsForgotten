using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MissionEffects.EffectDurationTypes
{
    public abstract class EffectDurationBase : IEffectDuration
    {
        public bool IsExpired { get; protected set; }

        public virtual void OnStarted()
        {
        }

        public virtual void OnMissionTick(float dt)
        {
        }

        public virtual void OnAgentKilled(Agent removedAgent)
        {
        }
    }
}