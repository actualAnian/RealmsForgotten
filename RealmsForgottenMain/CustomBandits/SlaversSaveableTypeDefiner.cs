using TaleWorlds.SaveSystem;

namespace RealmsForgotten.CustomBandits
{
    public class SlaversSaveableTypeDefiner : SaveableTypeDefiner
    {
        public SlaversSaveableTypeDefiner() : base(65841) {}
        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(SlaversBanditPartyComponent), 1, null);
        }
    }
}