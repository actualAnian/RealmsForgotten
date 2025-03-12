using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Ability
{
    internal class AbilityHudVM : ViewModel
    {
        private ClassAbility _ability;
        private string _name = "";
        private string _spriteName = "";
        private string _coolDownLeft = "";
        private bool _isVisible;
        private bool _onCoolDown;
        private bool _isSpell;
        private AbilityManagerMissionLogic _abilityLogic;
        private bool _isDisabled;
        private string _disabledText;
        private string _abilityType;

        public AbilityHudVM() : base() { }

        public override void RefreshValues()
        {
            _ability = PlayerCareerExtension.GetCareer().Ability;
            if (_abilityLogic == null) _abilityLogic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            IsVisible = _ability != null && _abilityLogic != null && (Mission.Current.Mode == MissionMode.Battle || Mission.Current.Mode == MissionMode.Stealth);
            if (IsVisible)
            {
                SpriteName = _ability.CurrentSprite;
                Name = _ability.Name.ToString();
                CoolDownLeft = _ability.GetCoolDownLeft().ToString();
                IsOnCoolDown = _ability.IsOnCooldown();
                TextObject disabledReason;
                if (_ability.IsDisabled(Agent.Main, out disabledReason))
                {
                    IsDisabled = true;
                    DisabledText = disabledReason.ToString();
                }
                else
                {
                    IsDisabled = false;
                    DisabledText = string.Empty;
                }
            }
        }


        [DataSourceProperty]
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    base.OnPropertyChangedWithValue(value, "IsVisible");
                }
            }
        }
        [DataSourceProperty]
        public String Name
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
                    base.OnPropertyChangedWithValue(value, "Name");
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
                    base.OnPropertyChangedWithValue(value, "SpriteName");
                }
            }
        }

        [DataSourceProperty]
        public string CoolDownLeft
        {
            get
            {
                return _coolDownLeft;
            }
            set
            {
                if (value != _coolDownLeft)
                {
                    _coolDownLeft = value;
                    base.OnPropertyChangedWithValue(value, "CoolDownLeft");
                }
            }
        }

        [DataSourceProperty]
        public bool IsOnCoolDown
        {
            get
            {
                return _onCoolDown;
            }
            set
            {
                if (value != _onCoolDown)
                {
                    _onCoolDown = value;
                    base.OnPropertyChangedWithValue(value, "IsOnCoolDown");
                }
            }
        }

        [DataSourceProperty]
        public bool IsDisabled
        {
            get
            {
                return _isDisabled;
            }
            set
            {
                if (value != _isDisabled)
                {
                    _isDisabled = value;
                    base.OnPropertyChangedWithValue(value, "IsDisabled");
                }
            }
        }

        [DataSourceProperty]
        public string DisabledText
        {
            get
            {
                return _disabledText;
            }
            set
            {
                if (value != _disabledText)
                {
                    _disabledText = value;
                    base.OnPropertyChangedWithValue(value, "DisabledText");
                }
            }
        }
    }
}
