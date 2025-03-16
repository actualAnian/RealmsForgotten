using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class CareerObjectVM : ViewModel
    {
        private string _name;
        private string _spriteName;
        private string _abilityName;
        private string _abilityDescription;
        private string _description;
        private readonly CareerObject _career;
        private CareerChoiceDoubleGroupObjectVM _choiceDoubleGroup1;
        private CareerChoiceDoubleGroupObjectVM _choiceDoubleGroup2;
        private CareerChoiceDoubleGroupObjectVM _choiceDoubleGroup3;
        private AbilityVM baseAbility;
        private AbilityVM upgradedAbility;
        private string _choiceGroup1Name;
        private string _choiceGroup2Name;
        private string _choiceGroup3Name;
        private string _freeCareerPoints;
        private CareerChoiceDoubleGroupObjectVM _highlightedGroup;
        private TopScreenVM _topscreen;
        private List<CareerChoiceObjectVM> selectedChoices = new();
        public CareerObjectVM(CareerObject career)
        {
            _career = career;
            _name = career.Name.Value;
            _spriteName = "CareerSystem\\Illustrations\\" + career.StringId;
            baseAbility = new(_career.Ability.Sprite, this);
            upgradedAbility = new(_career.Ability.SpriteUpgraded, this);
            _abilityName = _career.Ability.Name.ToString();
            _abilityDescription = null;//new MBBindingList<CareerAbilityEffectVM>();
            _description = _career.Description.ToString();
            _topscreen = new();
            List<List<CareerChoiceGroupObject>> groups = new() { new(), new(), new() };
            foreach (CareerChoiceGroupObject group in _career.ChoiceGroups)
            {
                switch (group.Tier)
                {
                    case 1:
                        groups[0].Add(group);
                        break;
                    case 2:
                        groups[1].Add(group);
                        break;
                    case 3:
                        groups[2].Add(group);
                        break;
                    default:
                        break;
                }
            }
            _choiceDoubleGroup1 = new CareerChoiceDoubleGroupObjectVM(groups[0], this, _topscreen);
            _choiceDoubleGroup2 = new CareerChoiceDoubleGroupObjectVM(groups[1], this, _topscreen);
            _choiceDoubleGroup3 = new CareerChoiceDoubleGroupObjectVM(groups[2], this, _topscreen);

            _choiceGroup1Name = GameTexts.FindText("class_choicegroup1_name_" + _career.StringId).ToString();
            _choiceGroup2Name = GameTexts.FindText("class_choicegroup2_name_" + _career.StringId).ToString();
            _choiceGroup3Name = GameTexts.FindText("career_choicegroup3_name_" + _career.StringId).ToString();
            _topscreen.Choices = _choiceDoubleGroup1.GetChoices();
            _topscreen.GroupName = _choiceDoubleGroup1.GroupName;
            SetAvailability();
            RefreshValues();
        }
        public void SetAvailability()
        {
            _choiceDoubleGroup1.IsActive = true;
            if (_choiceDoubleGroup2.GetChoices().Count > 0 && (_choiceDoubleGroup2.GetChoices()[0].IsTaken || baseAbility.IsTaken))
                _choiceDoubleGroup2.IsActive = true;
            if (_choiceDoubleGroup3.GetChoices().Count > 0 && (_choiceDoubleGroup3.GetChoices()[0].IsTaken || upgradedAbility.IsTaken))
                _choiceDoubleGroup3.IsActive = true;
        }
        public void HandleAddPerk(CareerChoiceObjectVM choice)
        {
            selectedChoices.Add(choice);
            if (_choiceDoubleGroup1.IsLastChoice(choice)) baseAbility.SetState(ThreeStateObjectVM.State.AvailableToTake);
            if (_choiceDoubleGroup2.IsLastChoice(choice)) upgradedAbility.SetState(ThreeStateObjectVM.State.AvailableToTake);
            _topscreen.RefreshValues();
        }
        public void RefundPerks()
        {
            PlayerCareerExtension.PointsSystem.ReturnPoints(selectedChoices.Count);
            for (int i = selectedChoices.Count - 1; i >= 0; i--)
            {
                if (_choiceDoubleGroup1.IsLastChoice(selectedChoices[i]))
                    _choiceDoubleGroup2.IsActive = false;
                if (_choiceDoubleGroup2.IsLastChoice(selectedChoices[i]))
                    _choiceDoubleGroup3.IsActive = false;
                selectedChoices[i].DeSelectChoice();
                selectedChoices.RemoveAt(i);
            }
            _choiceDoubleGroup1.RefreshValues();
            _choiceDoubleGroup2.RefreshValues();
            _choiceDoubleGroup3.RefreshValues();
            _topscreen.RefreshValues();
        }
        internal void UnlockAbility(AbilityVM abilityVM)
        {
            if (abilityVM == baseAbility)
            {
                _career.Ability.IsEnabled = true;
                _choiceDoubleGroup2.IsActive = true;
            }
            else
            {
                _choiceDoubleGroup3.IsActive = true;
                _career.Ability.IsUpgraded = true;
            }
        }
        internal void GiveActivePerkBonuses()
        {
            foreach (CareerChoiceObjectVM choice in selectedChoices)
            {
                CareerChoiceObject.ActiveEffect active = choice.choice.Active;
                if (active == null) continue;
                active.TryExecute();
            }
        }
        public override void RefreshValues()
        {
            FreeCareerPoints = "Free career points: " + (PlayerCareerExtension.PointsSystem?.AvailablePoints).ToString();
            SetAvailability();
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
        [DataSourceProperty]
        public string CurrentSpriteName
        {
            get
            {
                return _career.Ability.Sprite;
            }
        }

        [DataSourceProperty]
        public string SpriteName
        {
            get
            {
                return _spriteName;
            }
            set
            {
                if (value != _spriteName)
                {
                    _spriteName = value;
                    OnPropertyChangedWithValue(value, "SpriteName");
                }
            }
        }

        [DataSourceProperty]
        public string AbilityName
        {
            get
            {
                return _abilityName;
            }
            set
            {
                if (value != _abilityName)
                {
                    _abilityName = value;
                    OnPropertyChangedWithValue(value, "AbilityName");
                }
            }
        }

        [DataSourceProperty]
        public string AbilityEffects
        {
            get
            {
                return _abilityDescription;
            }
            set
            {
                if (value != _abilityDescription)
                {
                    _abilityDescription = value;
                    OnPropertyChangedWithValue(value, "AbilityEffects");
                }
            }
        }
        [DataSourceProperty]
        public AbilityVM BaseAbility
        {
            get
            {
                return baseAbility;
            }
            set
            {
                if (value != baseAbility)
                {
                    baseAbility = value;
                    OnPropertyChangedWithValue(value, "BaseAbility");
                }   
            }
        }
        [DataSourceProperty]
        public AbilityVM UpgradedAbility
        {
            get
            {
                return upgradedAbility;
            }
            set
            {
                if (value != upgradedAbility)
                {
                    upgradedAbility = value;
                    OnPropertyChangedWithValue(value, "UpgradedAbility");
                }
            }
        }
        [DataSourceProperty]
        public CareerChoiceDoubleGroupObjectVM DoubleGroupTier1
        {
            get
            {
                return _choiceDoubleGroup1;
            }
            set
            {
                if (value != _choiceDoubleGroup1)
                {
                    _choiceDoubleGroup1 = value;
                    OnPropertyChangedWithValue(value, "DoubleGroupTier1");
                }
            }
        }
        [DataSourceProperty]
        public CareerChoiceDoubleGroupObjectVM DoubleGroupTier2
        {
            get
            {
                return _choiceDoubleGroup2;
            }
            set
            {
                if (value != _choiceDoubleGroup2)
                {
                    _choiceDoubleGroup2 = value;
                    OnPropertyChangedWithValue(value, "DoubleGroupTier2");
                }
            }
        }
        [DataSourceProperty]
        public CareerChoiceDoubleGroupObjectVM DoubleGroupTier3
        {
            get
            {
                return _choiceDoubleGroup3;
            }
            set
            {
                if (value != _choiceDoubleGroup3)
                {
                    _choiceDoubleGroup3 = value;
                    OnPropertyChangedWithValue(value, "DoubleGroupTier3");
                }
            }
        }
        [DataSourceProperty]
        public string ChoiceGroup1Name
        {
            get
            {
                return _choiceGroup1Name;
            }
            set
            {
                if (value != _choiceGroup1Name)
                {
                    _choiceGroup1Name = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup1Name");
                }
            }
        }

        [DataSourceProperty]
        public string ChoiceGroup2Name
        {
            get
            {
                return _choiceGroup2Name;
            }
            set
            {
                if (value != _choiceGroup2Name)
                {
                    _choiceGroup2Name = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup2Name");
                }
            }
        }

        [DataSourceProperty]
        public string ChoiceGroup3Name
        {
            get
            {
                return _choiceGroup3Name;
            }
            set
            {
                if (value != _choiceGroup3Name)
                {
                    _choiceGroup3Name = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup3Name");
                }
            }
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
        public TopScreenVM TopScreen
        {
            get
            {
                return _topscreen;
            }
            set
            {
                if (value != _topscreen)
                {
                    _topscreen = value;
                    OnPropertyChangedWithValue(value, "TopScreen");
                }
            }
        }
        [DataSourceProperty]
        public bool IsEnabled
        {
            get { return _career.Ability.IsEnabled; }
        }
        [DataSourceProperty]
        public bool IsDisabled
        {
            get { return !_career.Ability.IsEnabled; }
        }
        [DataSourceProperty]
        public bool IsUpgraded
        {
            get { return _career.Ability.IsUpgraded; }
        }
    }
}
