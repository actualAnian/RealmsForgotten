using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class CareerChoiceObjectVM : ViewModel
    {
        public enum ChoiceState
        {
            Taken,
            AvailableToTake,
            UnavailableToTake
        }
        public CareerChoiceObject choice;
        private CareerChoiceDoubleGroupObjectVM _group;
        private string _description;
        private string _name;
        private ChoiceState buttonState;
        private bool _isTaken = false;
        private bool _isUnavailableToTake = true;
        private bool _isAvailableToTake = false;

        public CareerChoiceObjectVM()
        {
            _description = string.Empty;
        }
        public CareerChoiceObjectVM(CareerChoiceObject choice, CareerChoiceDoubleGroupObjectVM group, ChoiceState curState)
        {
            _group = group;
            this.choice = choice;
            _name = this.choice.Name.ToString();
            _description = this.choice.Description.ToString();
            SetState(curState);
            //RefreshValues();
        }
        public void SetState(ChoiceState newState)
        {
            switch (newState)
            {
                case ChoiceState.Taken:
                    IsTaken = true;
                    IsAvailableToTake = false;
                    IsUnavailableToTake = false;
                    break;
                case ChoiceState.AvailableToTake:
                    IsTaken = false;
                    IsAvailableToTake = true;
                    IsUnavailableToTake = false;
                    break;
                case ChoiceState.UnavailableToTake:
                    IsTaken = false;
                    IsAvailableToTake = false;
                    IsUnavailableToTake = true;
                    break;
            }
        }
        public override void RefreshValues()
        {
            SetState(buttonState);
        }

        public void SelectChoice()
        {
            if (PlayerCareerExtension.TryAddCareerChoice(choice))
                _group.OnPerkTaken(choice);
        }

        public void DeSelectChoice()
        {
            if (PlayerCareerExtension.TryRemoveCareerChoice(choice)) RefreshValues();
        }

        public void ExecuteBeginHint()
        {
            MBInformationManager.ShowHint(Description);
        }

        public void ExecuteEndHint()
        {
            MBInformationManager.HideInformations();
        }

        [DataSourceProperty]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, "Name");
                }
            }
        }

        [DataSourceProperty]
        public bool IsTaken
        {
            get
            {
                return _isTaken;
            }
            set
            {
                if (value != _isTaken)
                {
                    _isTaken = value;
                    OnPropertyChangedWithValue(value, "IsTaken");
                }
            }
        }
        [DataSourceProperty]
        public bool IsAvailableToTake
        {
            get
            {
                return _isAvailableToTake;
            }
            set
            {
                if (value != _isAvailableToTake)
                {
                    _isAvailableToTake = value;
                    OnPropertyChangedWithValue(value, "IsAvailableToTake");
                }
            }
        }
        [DataSourceProperty]
        public bool IsUnavailableToTake
        {
            get
            {
                return _isUnavailableToTake;
            }
            set
            {
                if (value != _isUnavailableToTake)
                {
                    _isUnavailableToTake = value;
                    OnPropertyChangedWithValue(value, "IsUnavailableToTake");
                }
            }
        }

        [DataSourceProperty]
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (value != _description)
                {
                    _description = value;
                    OnPropertyChangedWithValue(value, "Description");
                }
            }
        }
    }
}
