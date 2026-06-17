using RealmsForgotten.MissionEffects.EffectDurationTypes;
using RealmsForgotten.MissionEffects.MissionEffectTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MissionEffects
{
    public class MissionEffectsBehavior : MissionBehavior
    {
        public static MissionEffectsBehavior? Instance
        {
            get
            {
                if (Mission.Current == null) return null;
                return Mission.Current.GetMissionBehavior<MissionEffectsBehavior>();
            }
        }

        private readonly Dictionary<Agent, List<MissionEffect>> _agentsEffects = new();
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        public void ApplyEffect(MissionEffect effect)
        {
            var target = effect.AgentEffectBelongsTo;
            effect.OnStart();
            if (!_agentsEffects.TryGetValue(target, out var effects))
            {
                effects = new List<MissionEffect>();
                _agentsEffects[target] = effects;
            }
            effects.Add(effect);
        }

        public bool RemoveEffect(Agent agent, Type effectType)
        {
            if (!_agentsEffects.TryGetValue(agent, out var effects)) return false;

            var effectToRemove = effects.FirstOrDefault(e => e.GetType() == effectType);
            if (effectToRemove == null) return false;

            effects.Remove(effectToRemove);
            effectToRemove.OnEnd();

            if (effects.Count == 0) _agentsEffects.Remove(agent);
            return true;
        }
        public bool ChangeEffectDuration(MissionEffect effect, IEffectDuration duration)
        {
            if (effect == null || duration == null) return false;
            effect.SetDuration(duration);
            return true;
        }

        public IReadOnlyList<MissionEffect> GetEffectsForAgent(Agent agent)
        {
            if (agent == null)
                return new List<MissionEffect>();

            if (_agentsEffects.TryGetValue(agent, out var effects))
                return effects;

            return new List<MissionEffect>();
        }
        public override void OnMissionTick(float dt)
        {
            foreach (var agent in _agentsEffects.Keys.ToList())
            {
                var effects = _agentsEffects[agent];

                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    var effect = effects[i];

                    if (effect.Duration.IsExpired)
                        effects.RemoveAt(i);

                    effect.OnUpdate(dt);

                }
                if (effects.Count == 0)
                    _agentsEffects.Remove(agent);
            }
        }
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (_agentsEffects.TryGetValue(affectedAgent, out var effects))
                foreach (var effect in effects)
                    effect.Duration.OnAgentKilled(affectedAgent);

            if (_agentsEffects.TryGetValue(affectorAgent, out var ownedEffects))
            {
                foreach (var effect in ownedEffects)
                    effect.OnEnd();
                _agentsEffects.Remove(affectorAgent);
            }
        }
    }
}