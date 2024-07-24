using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Career
{
    [SaveableClass]
    public class ExampleConfig
    {
        [SaveableField(1)]
        private string displayName;

        [SaveableField(2)]
        private List<string> troopIds;

        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public List<string> TroopIds
        {
            get => troopIds;
            set => troopIds = value;
        }

        public ExampleConfig(string displayName, List<string> troopIds)
        {
            this.displayName = displayName;
            this.troopIds = troopIds;
        }
    }
}

