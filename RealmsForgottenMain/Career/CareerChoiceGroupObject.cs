using RealmsForgotten.AiMade.Career;
using RealmsForgotten.Career;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Career
{
    public class CareerChoiceGroupObject : PropertyObject
    {
        public CareerObject? OwnerCareer { get; private set; }
        public int Tier { get; private set; }
        public List<CareerChoiceObject> Choices { get; private set; } = new List<CareerChoiceObject>();
        public override string ToString() => Name.ToString();

        public CareerChoiceGroupObject(string stringId) : base(stringId) { }

        public void Initialize(string name, CareerObject ownerCareer, int tier)
        {
            base.Initialize(new TextObject(name), new TextObject("Choice group for " + name));
            OwnerCareer = ownerCareer;
            Tier = tier;
            OwnerCareer.ChoiceGroups.Add(this);
            AfterInitialized();
        }

    }
}
