using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MissionEffects.EffectDurationTypes
{
    public interface IEffectDuration
    {
        bool IsExpired { get; }
        void OnStarted();
        void OnMissionTick(float dt);
        void OnAgentKilled(Agent removedAgent);
    }
}