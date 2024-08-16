using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Career
{
    [SaveableClass]
    public class CareerObject
    {
        [SaveableField(1)]
        private CareerType _type;

        [SaveableField(2)]
        private string _name;

        [SaveableField(3)]
        private List<CareerTier> _tiers = new List<CareerTier>();

        public CareerType Type
        {
            get => _type;
            set => _type = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public List<CareerTier> Tiers
        {
            get => _tiers;
            set => _tiers = value;
        }
    }

    [SaveableClass]
    public class CareerTier
    {
        [SaveableField(1)]
        private string _name;

        [SaveableField(2)]
        private string _benefit;

        [SaveableField(3)]
        private string _progressionRequirement;

        [SaveableField(4)]
        private int _requiredValue;

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Benefit
        {
            get => _benefit;
            set => _benefit = value;
        }

        public string ProgressionRequirement
        {
            get => _progressionRequirement;
            set => _progressionRequirement = value;
        }

        public int RequiredValue
        {
            get => _requiredValue;
            set => _requiredValue = value;
        }
    }
}
