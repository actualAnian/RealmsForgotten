using TaleWorlds.SaveSystem;

namespace RealmsForgotten.WorldState.Refugees
{
    public class RefugeeSaveDefiner : SaveableTypeDefiner
    {
        // 585246000 is reserved for the world-state systems; it must never collide
        // with the other RF definers (see AUDIT_REPORT.md §2.4) nor change once a
        // save contains a refugee party.
        public RefugeeSaveDefiner() : base(585246000) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(RefugeePartyComponent), 1);
        }
    }
}
