using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class TopScreenVM : ViewModel
    {
        private MBBindingList<CareerChoiceObjectVM> _choices = new();
        private string _groupName = "Test";
        private string _freeCareerPoints;

        public TopScreenVM()
        {
            RefreshValues();
            _freeCareerPoints = "Free career points: " + (PlayerCareerExtension.PointsSystem?.AvailablePoints).ToString();
        }

        public override void RefreshValues()
        {
            FreeCareerPoints = "Free career points: " + (PlayerCareerExtension.PointsSystem?.AvailablePoints).ToString();
        }
        [DataSourceProperty]
        public string FreeCareerPoints
        {
            get
            {
                return _freeCareerPoints;
            }
            set
            {
                if (value != _freeCareerPoints)
                {
                    _freeCareerPoints = value;
                    OnPropertyChangedWithValue(value, "FreeCareerPoints");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CareerChoiceObjectVM> Choices
        {
            get
            {
                return _choices;
            }
            set
            {
                if (value != _choices)
                {
                    _choices = value;
                    OnPropertyChangedWithValue(value, "Choices");
                }
            }
        }
        [DataSourceProperty]
        public string GroupName
        {
            get
            {
                return _groupName;
            }
            set
            {
                if (value != _groupName)
                {
                    _groupName = value;
                    OnPropertyChangedWithValue(value, "GroupName");
                }
            }
        }
    }
}
