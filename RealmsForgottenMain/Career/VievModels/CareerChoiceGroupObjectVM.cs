using RealmsForgotten.AiMade.Career;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class CareerChoiceGroupObjectVM : ViewModel
    {
        private CareerChoiceGroupObject _choiceGroup;
        private MBBindingList<CareerChoiceObjectVM> _choices;
        private string _groupName;
        private bool _isActive;
        private bool _areButtonsVisible;
        private Action _choiceChangedAction;
        private TopScreenVM _topScreenVM;

        public CareerChoiceGroupObjectVM(CareerChoiceGroupObject choiceGroup, Action choiceChangedAction, TopScreenVM topScreenVM)
        {
            _choiceGroup = choiceGroup;
            _groupName = _choiceGroup.Name.ToString();
            _choiceChangedAction = choiceChangedAction;
            _isActive = _choiceGroup.IsActiveForHero(Hero.MainHero);
            _areButtonsVisible = true;
            _choices = new MBBindingList<CareerChoiceObjectVM>();
            choiceGroup.Choices.ForEach(x => _choices.Add(new CareerChoiceObjectVM(x)));
            _topScreenVM = topScreenVM;
        }

        private void ExecuteClickIncrease()
        {
            for (int i = 0; i < _choices.Count; i++)
            {
                if (!_choices[i].IsTaken)
                {
                    _choices[i].SelectChoice();
                    break;
                }
                else continue;
            }
            if (_choiceChangedAction != null) _choiceChangedAction();
        }

        private void ExecuteClickDecrease()
        {
            for (int i = _choices.Count - 1; i >= 0; i--)
            {
                if (_choices[i].IsTaken)
                {
                    _choices[i].DeSelectChoice();
                    break;
                }
                else continue;
            }
            if (_choiceChangedAction != null) _choiceChangedAction();
        }

        private void ExecuteBeginHover()
        {
            _topScreenVM.GroupName = _groupName;
            _topScreenVM.Choices = _choices;
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
        public bool ButtonsVisible
        {
            get
            {
                return _areButtonsVisible;
            }
            set
            {
                if (value != _areButtonsVisible)
                {
                    _areButtonsVisible = value;
                    OnPropertyChangedWithValue(value, "ButtonsVisible");
                }
            }
        }
        public CareerChoiceGroupObject ChoiceGroup { get { return  _choiceGroup; } }
    }
}
