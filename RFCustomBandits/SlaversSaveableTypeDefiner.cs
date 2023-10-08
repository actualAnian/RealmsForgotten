using TaleWorlds.SaveSystem;

namespace RealmsForgotten.RFCustomBandits
{
    public class SlaversSaveableTypeDefiner : SaveableTypeDefiner
    {
        public SlaversSaveableTypeDefiner() : base(65300) {}
        protected override void DefineClassTypes()
        {
            base.AddClassDefinition(typeof(SlaversBanditPartyComponent), 1, null);
        }
    }
}