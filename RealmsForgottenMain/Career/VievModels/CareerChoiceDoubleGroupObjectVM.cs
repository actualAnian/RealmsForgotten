using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class CareerChoiceDoubleGroupObjectVM : ViewModel
    {
        private readonly CareerChoiceGroupObject _choiceGroup0;
        private readonly CareerChoiceGroupObject _choiceGroup1;
        private MBBindingList<CareerChoiceObjectVM> _choices0;
        private MBBindingList<CareerChoiceObjectVM> _choices1;
        private string _groupName;
        private readonly CareerObjectVM _careerObject;
        private readonly TopScreenVM _topScreenVM;
        private bool _isActive = false;
        private readonly int tier;

        public CareerChoiceDoubleGroupObjectVM(List<CareerChoiceGroupObject> choiceGroup, CareerObjectVM careerObject, TopScreenVM topScreenVM, int ttier)
        {
            _topScreenVM = topScreenVM;
            _choiceGroup0 = choiceGroup[0];
            _choiceGroup1 = choiceGroup[1];
            _groupName = _choiceGroup0.Name.ToString();
            _careerObject = careerObject;
            _choices0 = new MBBindingList<CareerChoiceObjectVM>();
            _choices1 = new MBBindingList<CareerChoiceObjectVM>();
            tier = ttier;

            int lastTaken = Math.Max(
                _choiceGroup0.Choices.FindLastIndex(c => PlayerCareerExtension.HasCareerChoice(c)),
                _choiceGroup1.Choices.FindLastIndex(c => PlayerCareerExtension.HasCareerChoice(c))
            );
            for (int i = 0; i < _choiceGroup0.Choices.Count; i++)
            {
                (ThreeStateObjectVM.State, ThreeStateObjectVM.State) choiceStates = GetChoiceState(lastTaken, i);
                _choices0.Add(new CareerChoiceObjectVM(_choiceGroup0.Choices[i], this, choiceStates.Item1));
                _choices1.Add(new CareerChoiceObjectVM(_choiceGroup1.Choices[i], this, choiceStates.Item2));
            }
            _topScreenVM.Choices = GetChoices();
            RefreshValues();
        }
        public bool IsLastPerkTaken()
        {
            return GetChoices().Count == Math.Min(_choiceGroup0.Choices.Count, _choiceGroup1.Choices.Count);
        }
        private (ThreeStateObjectVM.State, ThreeStateObjectVM.State) GetChoiceState(int lastTaken, int index)
        {
            ThreeStateObjectVM.State state0;
            ThreeStateObjectVM.State state1;
            if (!IsActive)
            {
                state0 = state1 = ThreeStateObjectVM.State.UnavailableToTake;
            }
            else if (index <= lastTaken)
            {
                state0 = PlayerCareerExtension.HasCareerChoice(_choiceGroup0.Choices[index]) ? ThreeStateObjectVM.State.Taken : ThreeStateObjectVM.State.UnavailableToTake;
                state1 = PlayerCareerExtension.HasCareerChoice(_choiceGroup1.Choices[index]) ? ThreeStateObjectVM.State.Taken : ThreeStateObjectVM.State.UnavailableToTake;
            }
            else if (index == lastTaken + 1 && PlayerCareerExtension.PointsSystem!.HasAvailablePoints())
            {
                state0 = state1 = ThreeStateObjectVM.State.AvailableToTake;
            }
            else
            {
                state0 = state1 = ThreeStateObjectVM.State.UnavailableToTake;
            }
            return (state0, state1);
        }
        public override void RefreshValues()
        {
            IsActive = _careerObject.ShouldBeActive(tier);
            int LastTaken = Math.Max(
                _choiceGroup0.Choices.FindLastIndex(c => PlayerCareerExtension.HasCareerChoice(c)),
                _choiceGroup1.Choices.FindLastIndex(c => PlayerCareerExtension.HasCareerChoice(c))
            );
            for (int i = 0; i < _choiceGroup0.Choices.Count; i++)
            {
                (ThreeStateObjectVM.State, ThreeStateObjectVM.State) choiceStates = GetChoiceState(LastTaken, i);
                _choices0[i].SetState(choiceStates.Item1);
                _choices1[i].SetState(choiceStates.Item2);
            }
        }
        public void OnPerkTaken(CareerChoiceObject choice)
        {
            PlayerCareerExtension.PointsSystem!.SpendPoint();
            bool canTake = PlayerCareerExtension.PointsSystem.HasAvailablePoints();
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
                _choices0[index].SetState(ThreeStateObjectVM.State.Taken);
                _choices1[index].SetState(ThreeStateObjectVM.State.UnavailableToTake);
                _careerObject.HandleAddPerk(_choices0[index]);
            }
            else
            {
                _choices1[index].SetState(ThreeStateObjectVM.State.Taken);
                _choices0[index].SetState(ThreeStateObjectVM.State.UnavailableToTake);
                _careerObject.HandleAddPerk(_choices1[index]);
            }
            if (_choices0.Count >= index + 2) _choices0[index + 1].SetState(canTake? ThreeStateObjectVM.State.AvailableToTake : ThreeStateObjectVM.State.UnavailableToTake);
            if (_choices1.Count >= index + 2) _choices1[index + 1].SetState(canTake ? ThreeStateObjectVM.State.AvailableToTake : ThreeStateObjectVM.State.UnavailableToTake);
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
                    _choices0[i].SetState(ThreeStateObjectVM.State.Taken);
                    topScreenChoices.Add(_choices0[i]);
                }
                else if (PlayerCareerExtension.HasCareerChoice(_choiceGroup1.Choices[i]))
                {
                    _choices1[i].SetState(ThreeStateObjectVM.State.Taken);
                    topScreenChoices.Add(_choices1[i]);
                }
                //else topScreenChoices.Add(new());
            }
            return topScreenChoices;
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
