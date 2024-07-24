using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Religions
{
    [SaveableClass]
    public class ReligionObject : MBObjectBase
    {
        [SaveableField(1)]
        private string name;

        [SaveableField(2)]
        private string description;

        [SaveableField(3)]
        private CultureObject culture;

        public string Name
        {
            get => name;
            private set => name = value;
        }

        public string Description
        {
            get => description;
            private set => description = value;
        }

        public CultureObject Culture
        {
            get => culture;
            private set => culture = value;
        }

        public ReligionObject(string name, string description, CultureObject culture)
        {
            this.name = name;
            this.description = description;
            this.culture = culture;
        }

        // Parameterless constructor for serialization
        protected ReligionObject()
        {
        }

        public static MBReadOnlyList<ReligionObject> All => MBObjectManager.Instance.GetObjectTypeList<ReligionObject>();
    }
}
