using System;
using System.Collections.Generic;

namespace RealmsForgotten.RFEffects.MissionEffects
{
    public class EffectInstance
    {
        public IMissionEffect Effect { get; }
        public object Target { get; }
        // Optional fixed duration (if > 0)
        public float Duration { get; }
        public float Elapsed { get; private set; }

        // Optional keep-alive condition. If provided, effect is considered active while this returns true.
        // When it becomes false, PostDuration controls how long the effect remains after leaving.
        public Func<object, bool> KeepAliveCondition { get; }
        public float PostDuration { get; }
        private float _postElapsed;

        private bool _expired;

        public bool IsExpired => _expired || (Duration > 0f && Elapsed >= Duration);

        public EffectInstance(IMissionEffect effect, object target, float duration, Func<object, bool> keepAliveCondition = null, float postDuration = 0f)
        {
            Effect = effect;
            Target = target;
            Duration = duration;
            Elapsed = 0f;
            KeepAliveCondition = keepAliveCondition;
            PostDuration = postDuration;
            _postElapsed = 0f;
            _expired = false;
        }

        public void Start()
        {
            Effect.OnApply(Target);
        }

        public void Update(float dt)
        {
            if (IsExpired) return;

            // If there's a keep-alive condition, respect it
            if (KeepAliveCondition != null)
            {
                bool alive = false;
                try { alive = KeepAliveCondition(Target); } catch { alive = false; }

                if (alive)
                {
                    // while alive, reset post timer and update normally
                    _postElapsed = 0f;
                    Effect.OnUpdate(Target, dt);
                }
                else
                {
                    if (PostDuration <= 0f)
                    {
                        // remove immediately
                        Effect.OnEnd(Target);
                        _expired = true;
                        return;
                    }

                    // during post-duration, still update effect
                    _postElapsed += dt;
                    Effect.OnUpdate(Target, dt);
                    if (_postElapsed >= PostDuration)
                    {
                        Effect.OnEnd(Target);
                        _expired = true;
                        return;
                    }
                }
            }
            else
            {
                // fixed duration behavior
                Effect.OnUpdate(Target, dt);
                Elapsed += dt;
                if (Duration > 0f && Elapsed >= Duration)
                {
                    Effect.OnEnd(Target);
                    _expired = true;
                    return;
                }
            }
        }

        public void ForceEnd()
        {
            if (!IsExpired)
            {
                Effect.OnEnd(Target);
                Elapsed = Duration;
            }
        }
    }
}
