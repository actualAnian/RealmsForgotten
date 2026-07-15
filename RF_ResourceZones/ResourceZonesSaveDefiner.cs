using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace RF_ResourceZones
{
    public class ResourceZonesSaveDefiner : SaveableTypeDefiner
    {
        // 728850000 is reserved for the resource-zones system. Audited unique
        // against every other RF definer (see AUDIT_REPORT.md §2.4 discipline):
        // 65841, 576011, 585820, 2876493, 8803412, 287656493, 321601531,
        // 456789012, 585242820, 585245000, 585246000, 585247000, 877885323,
        // 1992358567. Must never change once a save contains a zone.
        public ResourceZonesSaveDefiner() : base(728850000) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(ResourceZonePartyComponent), 1);
            AddClassDefinition(typeof(ResourceCaravanPartyComponent), 2);
            AddClassDefinition(typeof(ResourceZoneRecord), 3);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<ResourceZoneRecord>));
        }
    }
}
