using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private CareerObjectVM _careerObject;
        private TopScreenVM _topScreenVM;
        private bool _isActive = false;
        public bool IsActive
        {
            get 
            {
                return _isActive;
            }
            set
            {
                _isActive = value;
                RefreshValues();
            } 
        }

        public CareerChoiceDoubleGroupObjectVM(List<CareerChoiceGroupObject> choiceGroup, CareerObjectVM careerObject, TopScreenVM topScreenVM)
        {
            _topScreenVM = topScreenVM;
            _choiceGroup0 = choiceGroup[0];
            _choiceGroup1 = choiceGroup[1];
            _groupName = _choiceGroup0.Name.ToString();
            _careerObject = careerObject;
            _choices0 = new MBBindingList<CareerChoiceObjectVM>();
            _choices1 = new MBBindingList<CareerChoiceObjectVM>();

            int LastTaken = Math.Max(
                _choiceGroup0.Choices.FindLastIndex(c => PlayerCareerExtension.HasCareerChoice(c)),
                _choiceGroup1.Choices.FindLastIndex(c => PlayerCareerExtension.HasCareerChoice(c))
            );
            for (int i = 0; i < _choiceGroup0.Choices.Count; i++)
            {
                (CareerChoiceObjectVM.ChoiceState, CareerChoiceObjectVM.ChoiceState) choiceStates = GetChoiceState(LastTaken, i);
                _choices0.Add(new CareerChoiceObjectVM(_choiceGroup0.Choices[i], this, choiceStates.Item1));
                _choices1.Add(new CareerChoiceObjectVM(_choiceGroup1.Choices[i], this, choiceStates.Item2));
            }
            _topScreenVM.Choices = GetChoices();
        }
        private (CareerChoiceObjectVM.ChoiceState, CareerChoiceObjectVM.ChoiceState) GetChoiceState(int lastTaken, int index)
        {
            CareerChoiceObjectVM.ChoiceState state0;
            CareerChoiceObjectVM.ChoiceState state1;
            if (!IsActive)
            {
                state0 = state1 = CareerChoiceObjectVM.ChoiceState.UnavailableToTake;
            }
            else if (index <= lastTaken)
            {
                state0 = PlayerCareerExtension.HasCareerChoice(_choiceGroup0.Choices[index]) ? CareerChoiceObjectVM.ChoiceState.Taken : CareerChoiceObjectVM.ChoiceState.UnavailableToTake;
                state1 = PlayerCareerExtension.HasCareerChoice(_choiceGroup1.Choices[index]) ? CareerChoiceObjectVM.ChoiceState.Taken : CareerChoiceObjectVM.ChoiceState.UnavailableToTake;
            }
            else if (index == lastTaken + 1)
            {
                state0 = state1 = CareerChoiceObjectVM.ChoiceState.AvailableToTake;
            }
            else
            {
                state0 = state1 = CareerChoiceObjectVM.ChoiceState.UnavailableToTake;
            }
            return (state0, state1);
        }
        public override void RefreshValues()
        {
            int LastTaken = Math.Max(
                _choiceGroup0.Choices.FindLastIndex(c => PlayerCareerExtension.HasCareerChoice(c)),
                _choiceGroup1.Choices.FindLastIndex(c => PlayerCareerExtension.HasCareerChoice(c))
            );
            for (int i = 0; i < _choiceGroup0.Choices.Count; i++)
            {
                (CareerChoiceObjectVM.ChoiceState, CareerChoiceObjectVM.ChoiceState) choiceStates = GetChoiceState(LastTaken, i);
                _choices0[i].SetState(choiceStates.Item1);
                _choices1[i].SetState(choiceStates.Item2);
            }
            _topScreenVM.Choices = GetChoices();
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
                _choices0[index].SetState(CareerChoiceObjectVM.ChoiceState.Taken);
                _choices1[index].SetState(CareerChoiceObjectVM.ChoiceState.UnavailableToTake);
                _careerObject.HandleAddPerk(_choices0[index]);
            }
            else
            {
                _choices1[index].SetState(CareerChoiceObjectVM.ChoiceState.Taken);
                _choices0[index].SetState(CareerChoiceObjectVM.ChoiceState.UnavailableToTake);
                _careerObject.HandleAddPerk(_choices1[index]);
            }
            if (_choices0.Count >= index + 2) _choices0[index + 1].SetState(CareerChoiceObjectVM.ChoiceState.AvailableToTake);
            if (_choices1.Count >= index + 2) _choices1[index + 1].SetState(CareerChoiceObjectVM.ChoiceState.AvailableToTake);
            _topScreenVM.Choices = GetChoices();
        }

        private void ExecuteBeginHover()
        {
            _topScreenVM.GroupName = _groupName;
            _topScreenVM.Choices = GetChoices();
        }
        public bool IsLastChoice(CareerChoiceObjectVM choice)
        {
            return _choices0.Count != 0 && choice == _choices0.Last() || _choices1.Count != 0 &&  choice == _choices1.Last();
        }
        public MBBindingList<CareerChoiceObjectVM> GetChoices()
        {
            MBBindingList<CareerChoiceObjectVM> topScreenChoices = new();
            for (int i = 0; i < _choiceGroup0.Choices.Count; i++)
            {
                if (PlayerCareerExtension.HasCareerChoice(_choiceGroup0.Choices[i]))
                {
                    _choices0[i].IsTaken = true;
                    topScreenChoices.Add(_choices0[i]);
                }
                else if (PlayerCareerExtension.HasCareerChoice(_choiceGroup1.Choices[i]))
                {
                    _choices1[i].IsTaken = true;
                    topScreenChoices.Add(_choices1[i]);
                }
                else topScreenChoices.Add(new());
            }
            return topScreenChoices;
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
