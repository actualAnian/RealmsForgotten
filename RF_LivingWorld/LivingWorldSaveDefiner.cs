using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace RF_LivingWorld
{
    public sealed class LivingWorldSaveDefiner : SaveableTypeDefiner
    {
        public LivingWorldSaveDefiner() : base(585248000) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(LivingWorldPartyComponent), 1);
            AddClassDefinition(typeof(LivingWorldRumorReport), 2);
            AddClassDefinition(typeof(LivingWorldRumorSourceState), 3);
            AddClassDefinition(typeof(LivingWorldContextCooldownState), 4);
            AddClassDefinition(typeof(LivingWorldPendingEvent), 5);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<LivingWorldRumorReport>));
            ConstructContainerDefinition(typeof(List<LivingWorldRumorSourceState>));
            ConstructContainerDefinition(typeof(List<LivingWorldContextCooldownState>));
            ConstructContainerDefinition(typeof(List<LivingWorldPendingEvent>));
        }
    }
}
