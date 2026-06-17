using RealmsForgotten.MissionEffects.EffectDurationTypes;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MissionEffects.MissionEffectTypes
{
    internal class SmokeEffect : MissionEffect
    {
        public SmokeEffect(Agent belongsTo, IEffectDuration duration) : base(belongsTo, duration) { }

        public override void OnStart()
        {
            AgentEffectBelongsTo.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire);
        }

        public override void OnEnd()
        {
            AgentEffectBelongsTo.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.FireAtWill);
        }
    }
}
