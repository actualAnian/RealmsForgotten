using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace RF_Settlers
{
    public class SettlersSaveDefiner : SaveableTypeDefiner
    {
        // 585247000 is reserved for the settlers system; it must never collide
        // with the other RF definers (see AUDIT_REPORT.md §2.4) nor change once
        // a save contains a settler party or camp.
        public SettlersSaveDefiner() : base(585247000) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(SettlerPartyComponent), 1);
            AddClassDefinition(typeof(SettlerCampComponent), 2);
            AddClassDefinition(typeof(SettlerVillageRecord), 3);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<SettlerVillageRecord>));
        }
    }
}
