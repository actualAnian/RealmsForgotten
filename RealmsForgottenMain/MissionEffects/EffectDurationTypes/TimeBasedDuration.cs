namespace RealmsForgotten.MissionEffects.EffectDurationTypes
{
    public class TimeBasedDuration : EffectDurationBase
    {
        private readonly float _duration;
        private float _elapsed;

        public TimeBasedDuration(float duration)
        {
            _duration = duration;
        }

        public override void OnMissionTick(float dt)
        {
            _elapsed += dt;

            if (_elapsed >= _duration)
                IsExpired = true;
        }
    }
}