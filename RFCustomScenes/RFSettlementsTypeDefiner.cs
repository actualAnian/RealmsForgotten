using RFCustomSettlements.Quests;
using System;
using System.Collections.Generic;
using TaleWorlds.SaveSystem;
using static RealmsForgotten.RFCustomSettlements.ArenaSettlementStateHandler;
using static RFCustomSettlements.Quests.CustomSettlementQuestSync;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class RFSettlementsTypeDefiner : SaveableTypeDefiner
    {
        public RFSettlementsTypeDefiner() : base(2876493) { }
        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(RFCustomSettlement), 1, null);
            AddEnumDefinition(typeof(ArenaState), 2, null);
            AddClassDefinition(typeof(CustomSettlementQuestData), 3, null);
            AddClassDefinition(typeof(CustomSettlementQuest), 4, null);
        }
        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(Dictionary<string, CustomSettlementQuestData>));
            ConstructContainerDefinition(typeof(List<RFCustomSettlement>));
        }
    }
}
