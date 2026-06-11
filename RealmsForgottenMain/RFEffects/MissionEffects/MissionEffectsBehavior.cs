using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects.MissionEffects
{
    public class MissionEffectsBehavior : MissionBehavior
    {
        public static MissionEffectsBehavior Instance;

        private readonly List<EffectInstance> _activeEffects = new();

        public MissionEffectsBehavior()
        {
            Instance = this;
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public void ApplyBurn(Agent agent, float durationSecondsAfterLeave, float damagePerSecond = 5f)
        {
            if (agent == null || !agent.IsActive()) return;

            var burn = new BurnEffect(damagePerSecond);
            ApplyEffect(burn, agent, 0f, null, durationSecondsAfterLeave);
        }

        /// <summary>
        /// Apply an effect. If keepAliveCondition is provided, the effect remains while the condition is true.
        /// If keepAliveCondition becomes false, the effect is removed immediately if postDuration is 0, otherwise after postDuration seconds.
        /// If keepAliveCondition is null and durationSeconds &gt; 0, the effect lasts for durationSeconds.
        /// </summary>
        public void ApplyEffect(IMissionEffect effect, object target, float durationSeconds = 0f, System.Func<object, bool> keepAliveCondition = null, float postDuration = 0f)
        {
            if (effect == null || target == null) return;

            // If effect already present for same target and effect type, remove old so we replace with a fresh one
            var existing = _activeEffects.FirstOrDefault(e => e.Target == target && e.Effect.GetType() == effect.GetType());
            if (existing != null)
            {
                existing.ForceEnd();
                _activeEffects.Remove(existing);
            }

            var instance = new EffectInstance(effect, target, durationSeconds, keepAliveCondition, postDuration);
            instance.Start();
            _activeEffects.Add(instance);
        }

        public override void OnAgentDeleted(Agent agent)
        {
            _activeEffects.RemoveAll(e => e.Target == agent);
        }

        public override void OnMissionTick(float dt)
        {
            if (_activeEffects.Count == 0) return;

            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var inst = _activeEffects[i];
                inst.Update(dt);
                if (inst.IsExpired)
                {
                    _activeEffects.RemoveAt(i);
                }
            }
        }
    }
}
