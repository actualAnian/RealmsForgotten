using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class CareerChoiceDoubleGroupObjectVM : ViewModel
    {
        private CareerChoiceGroupObject _choiceGroup0;
        private CareerChoiceGroupObject _choiceGroup1;
        private MBBindingList<CareerChoiceObjectVM> _choices0;
        private MBBindingList<CareerChoiceObjectVM> _choices1;
        private string _groupName;
        private bool _isActive;
        private Action _choiceChangedAction;
        private TopScreenVM _topScreenVM;

        public CareerChoiceDoubleGroupObjectVM(CareerChoiceGroupObject choiceGroup, Action choiceChangedAction, TopScreenVM topScreenVM)
        {
            _topScreenVM = topScreenVM;
            _choiceGroup0 = choiceGroup;
            _choiceGroup1 = choiceGroup;
            _groupName = _choiceGroup0.Name.ToString();
            _choiceChangedAction = choiceChangedAction;
            _isActive = _choiceGroup0.IsActiveForHero(Hero.MainHero);
            _choices0 = new MBBindingList<CareerChoiceObjectVM>();
            _choices1 = new MBBindingList<CareerChoiceObjectVM>();
            int LastTaken = Math.Max(
                _choiceGroup0.Choices.FindLastIndex(c => PlayerCareerExtension.HasCareerChoice(c)),
                _choiceGroup1.Choices.FindLastIndex(c => PlayerCareerExtension.HasCareerChoice(c))
            );
            for (int i =  0; i < _choiceGroup0.Choices.Count; i++)
            {
                CareerChoiceObjectVM.ChoiceState state0;
                CareerChoiceObjectVM.ChoiceState state1;
                if (i <= LastTaken)
                {
                    state0 = PlayerCareerExtension.HasCareerChoice(_choiceGroup0.Choices[i]) ? CareerChoiceObjectVM.ChoiceState.Taken : CareerChoiceObjectVM.ChoiceState.UnavailableToTake;
                    state1 = PlayerCareerExtension.HasCareerChoice(_choiceGroup1.Choices[i]) ? CareerChoiceObjectVM.ChoiceState.Taken : CareerChoiceObjectVM.ChoiceState.UnavailableToTake;
                }
                else if (i == LastTaken + 1)
                {
                    state0 = state1 = CareerChoiceObjectVM.ChoiceState.AvailableToTake;
                }
                else
                {
                    state0 = state1 = CareerChoiceObjectVM.ChoiceState.UnavailableToTake;
                }
                _choices0.Add(new CareerChoiceObjectVM(_choiceGroup0.Choices[i], this, state0));
                _choices1.Add(new CareerChoiceObjectVM(_choiceGroup1.Choices[i], this, state1));
            }
        }
        public void OnPerkTaken(CareerChoiceObject choice)
        {
            int index = _choiceGroup0.Choices.IndexOf(choice);
            int group = 0;
            CareerChoiceGroupObject groupToEdit = _choiceGroup0;
            if (index == -1) 
            {
                index = _choiceGroup1.Choices.IndexOf(choice);
                groupToEdit = _choiceGroup1;
                group = 1;
            }
            if (group == 0)
            {
                _choices0[index].SetState(CareerChoiceObjectVM.ChoiceState.UnavailableToTake);
                _choices1[index].SetState(CareerChoiceObjectVM.ChoiceState.Taken);
            }
            else
            {
                _choices1[index].SetState(CareerChoiceObjectVM.ChoiceState.UnavailableToTake);
            }
            if (_choices0.Count >= index + 1) _choices0[index + 1].SetState(CareerChoiceObjectVM.ChoiceState.AvailableToTake);
            if (_choices1.Count >= index + 1) _choices1[index + 1].SetState(CareerChoiceObjectVM.ChoiceState.AvailableToTake);
        }

        private void ExecuteBeginHover()
        {
            _topScreenVM.GroupName = _groupName;
            _topScreenVM.Choices = _choices0;
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

        [DataSourceProperty]
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                if (value != _isActive)
                {
                    _isActive = value;
                    OnPropertyChangedWithValue(value, "IsActive");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CareerChoiceObjectVM> Choices0
        {
            get
            {
                return _choices0;
            }
            set
            {
                if (value != _choices0)
                {
                    _choices0 = value;
                    OnPropertyChangedWithValue(value, "Choices0");
                }
            }
        }
        [DataSourceProperty]
        public MBBindingList<CareerChoiceObjectVM> Choices1
        {
            get
            {
                return _choices1;
            }
            set
            {
                if (value != _choices1)
                {
                    _choices1 = value;
                    OnPropertyChangedWithValue(value, "Choices1");
                }
            }
        }
        public CareerChoiceGroupObject ChoiceGroup { get { return  _choiceGroup0; } }
    }
}
