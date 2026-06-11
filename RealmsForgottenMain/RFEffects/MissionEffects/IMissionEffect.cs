using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects.MissionEffects
{
    public interface IMissionEffect
    {
        void OnApply(object target);

        void OnUpdate(object target, float dt);

        void OnEnd(object target);
    }
}
