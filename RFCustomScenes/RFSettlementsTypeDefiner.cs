using System.Collections.Generic;
using TaleWorlds.SaveSystem;
using static RealmsForgotten.RFCustomSettlements.ArenaSettlementStateHandler;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class RFSettlementsTypeDefiner : SaveableTypeDefiner
    {
        public RFSettlementsTypeDefiner() : base(2876493) { }
        protected override void DefineClassTypes()
        {
            base.AddClassDefinition(typeof(RFCustomSettlement), 1, null);
            AddEnumDefinition(typeof(ArenaState), 2, null);
        }
        protected override void DefineContainerDefinitions()
        {
            base.ConstructContainerDefinition(typeof(List<RFCustomSettlement>));
        }
    }
}
