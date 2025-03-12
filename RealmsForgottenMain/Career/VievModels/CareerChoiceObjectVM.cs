using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class CareerChoiceObjectVM : ThreeStateObjectVM
    {
        public CareerChoiceObject choice;
        private CareerChoiceDoubleGroupObjectVM _group;
        private string _description;
        private string _name;

        public CareerChoiceObjectVM()
        {
            _description = string.Empty;
        }
        public CareerChoiceObjectVM(CareerChoiceObject choice, CareerChoiceDoubleGroupObjectVM group, State curState)
        {
            _group = group;
            this.choice = choice;
            _name = this.choice.Name.ToString();
            _description = this.choice.Description.ToString();
            SetState(curState);
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
