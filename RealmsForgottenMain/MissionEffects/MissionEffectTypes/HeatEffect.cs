using RealmsForgotten.MissionEffects.EffectDurationTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MissionEffects.MissionEffectTypes
{
    internal class HeatEffect : MissionEffect
    {
        public HeatEffect(Agent belongsTo, IEffectDuration duration) : base(belongsTo, duration) { }
        public override void OnStart()
        {

            AgentEffectBelongsTo.AgentDrivenProperties.SwingSpeedMultiplier *= 1.2f;
            AgentEffectBelongsTo.AgentDrivenProperties.ReloadSpeed *= 1.2f;
            AgentEffectBelongsTo.UpdateCustomDrivenProperties();
        }
        public override void OnEnd()
        {
            AgentEffectBelongsTo.AgentDrivenProperties.SwingSpeedMultiplier *= 5 / 6;
            AgentEffectBelongsTo.AgentDrivenProperties.ReloadSpeed *= 5 / 6f;
            AgentEffectBelongsTo.UpdateCustomDrivenProperties();
        }
    }
}
