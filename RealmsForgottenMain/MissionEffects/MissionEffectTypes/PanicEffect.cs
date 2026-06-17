using RealmsForgotten.MissionEffects.EffectDurationTypes;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MissionEffects.MissionEffectTypes
{
    internal class PanicEffect : MissionEffect
    {
        private readonly float _moraleChange;
        public PanicEffect(Agent belongsTo, IEffectDuration duration, float moraleChange = -30f) : base(belongsTo, duration)
        {
            _moraleChange = moraleChange;
        }

        public override void OnStart()
        {
            AgentEffectBelongsTo.ChangeMorale(_moraleChange);
        }
    }
}
