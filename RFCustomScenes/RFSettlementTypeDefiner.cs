using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class RFSettlementTypeDefiner : SaveableTypeDefiner
    {
        public RFSettlementTypeDefiner() : base(2876493) { }
        protected override void DefineClassTypes()
        {
            base.AddClassDefinition(typeof(RFCustomSettlement), 1, null);
        }
        protected override void DefineContainerDefinitions()
        {
            base.ConstructContainerDefinition(typeof(List<RFCustomSettlement>));
        }
    }
}
