using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class CareerObjectVM : ViewModel
    {
        private string _name;
        private string _spriteName;
        private string _description;
        private string _abilityName;
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
        private readonly CareerChoiceDoubleGroupObjectVM _highlightedGroup;
        private TopScreenVM _topscreen;
        private readonly List<CareerChoiceObjectVM> selectedChoices = new();
        private bool boughtAbility;
        private bool boughtAbilityUpgrade;
        public string _abilityDescription;
        private string _currentSpriteName;
        public CareerObjectVM(CareerObject career)
        {
            _career = career;
            _name = career.Name.Value;
            _spriteName = career.StringId + "_icon";
            baseAbility = new(_career.Ability.Sprite, this, false);
            upgradedAbility = new(_career.Ability.SpriteUpgraded, this, true);
            _abilityName = _career.Ability.Name.ToString();
            _description = career.Description.ToString() + '\n' + '\n' + PlayerCareerExtension.PointsSystem!.Description;
            _abilityDescription = _career.Ability.Description;
            _freeCareerPoints = PlayerCareerExtension.PointsSystem.AvailablePoints.ToString();
            _topscreen = new();
            _currentSpriteName = _career.Ability.Sprite;
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
            _choiceDoubleGroup1 = new CareerChoiceDoubleGroupObjectVM(groups[0], this, _topscreen, 1);
            _choiceDoubleGroup2 = new CareerChoiceDoubleGroupObjectVM(groups[1], this, _topscreen, 2);
            _choiceDoubleGroup3 = new CareerChoiceDoubleGroupObjectVM(groups[2], this, _topscreen, 3);

            _choiceGroup1Name = GameTexts.FindText("class_choicegroup1_name_" + _career.StringId).ToString();
            _choiceGroup2Name = GameTexts.FindText("class_choicegroup2_name_" + _career.StringId).ToString();
            _choiceGroup3Name = GameTexts.FindText("career_choicegroup3_name_" + _career.StringId).ToString();
            _topscreen.Choices = _choiceDoubleGroup1.GetChoices();
            _topscreen.GroupName = _choiceDoubleGroup1.GroupName;
            _highlightedGroup = _choiceDoubleGroup1;
            RefreshValues();
        }
        public void SetAvailability()
        {
            _choiceDoubleGroup1.IsActive = true;
            baseAbility.RefreshValues();
            upgradedAbility.RefreshValues();
            if (baseAbility.IsTaken)
                _choiceDoubleGroup2.IsActive = true;
            if (upgradedAbility.IsTaken)
                _choiceDoubleGroup3.IsActive = true;
            _choiceDoubleGroup2.RefreshValues();
            _choiceDoubleGroup3.RefreshValues();
        }
        public void HandleAddPerk(CareerChoiceObjectVM choice)
        {
            selectedChoices.Add(choice);
            _topscreen.RefreshValues();
            baseAbility.RefreshValues();
            upgradedAbility.RefreshValues();
        }
        public bool IsGroupCompleted(int tier)
        {
            return tier switch
            {
                0 => DoubleGroupTier1.GetChoices().Count > 0 && DoubleGroupTier1.IsLastPerkTaken(),
                1 => DoubleGroupTier2.GetChoices().Count > 0 && DoubleGroupTier2.IsLastPerkTaken(),
                _ => false,
            };
        }
        public void RefundPerks()
        {
            int pointsToReturn = selectedChoices.Count;
            if (boughtAbility)
                pointsToReturn += 1;
            if (boughtAbilityUpgrade)
                pointsToReturn += 1;
            PlayerCareerExtension.PointsSystem!.ReturnPoints(pointsToReturn);
            boughtAbility = false;
            _career.Ability.IsEnabled = false;
            baseAbility.SetState(ThreeStateObjectVM.State.UnavailableToTake);
            baseAbility.RefreshValues();

            boughtAbilityUpgrade = false;
            _career.Ability.IsUpgraded = false;
            upgradedAbility.SetState(ThreeStateObjectVM.State.UnavailableToTake);
            upgradedAbility.RefreshValues();

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
            baseAbility.RefreshValues();
            upgradedAbility.RefreshValues();
            _topscreen.RefreshValues();
        }
        internal void UnlockAbility(AbilityVM abilityVM)
        {
            PlayerCareerExtension.PointsSystem!.SpendPoint();
            if (abilityVM == baseAbility)
            {
                boughtAbility = true;
                _career.Ability.IsEnabled = true;
            }
            else
            {
                boughtAbilityUpgrade = true;
                _career.Ability.IsUpgraded = true;
            }
            _topscreen.RefreshValues();
            SetAvailability();
        }
        internal void GiveActivePerkBonuses()
        {
            foreach (CareerChoiceObjectVM choice in selectedChoices)
            {
                choice.choice.Active?.TryExecute();
            }
        }

        public override void RefreshValues()
        {
            FreeCareerPoints = "Free career points: " + (PlayerCareerExtension.PointsSystem?.AvailablePoints).ToString();
            SetAvailability();
        }

        internal bool ShouldBeActive(int tier)
        {
            return tier switch
            {
                1 => true,
                2 => _career.Ability.IsEnabled,
                3 => _career.Ability.IsUpgraded,
                _ => false,
            };
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
                return _currentSpriteName;
            }
            set
            {
                if (value != _currentSpriteName)
                {
                    _currentSpriteName = value;
                    OnPropertyChangedWithValue(value, "CurrentSpriteName");
                }
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
        public string AbilityDescription
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
                    OnPropertyChangedWithValue(value, "AbilityDescription");
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
