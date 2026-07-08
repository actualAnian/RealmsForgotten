using System.Collections.Generic;
using System.Collections.Specialized;
using RealmsForgotten.Career.Ability;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Career
{
    public enum PointsSystemType
    {
        LevelUp,
        Renown,
        Deeds
    }

    public class CareerObject : PropertyObject
    {
        public ClassAbility Ability { get; private set; }
        public List<CareerChoiceGroupObject> ChoiceGroups { get; private set; } = new List<CareerChoiceGroupObject>();
        public PointsSystemType pointsSystem;
        public List<CareerChoiceObject> AllChoices
        {
            get
            {
                List<CareerChoiceObject> result = new();
                ChoiceGroups.ForEach(x => x.Choices.ForEach(y => result.Add(y)));
                return result;
            }
        }
        public CareerObject(string stringId, ClassAbility ability, PointsSystemType pointsType) : base(stringId)
        {
            Ability = ability;
            pointsSystem = pointsType;
        }

        public override string ToString() => Name.ToString();
        public void Initialize(string name, string description)
        {
            Initialize(new TextObject(name), new TextObject(description));
            AfterInitialized();
        }
    }
}