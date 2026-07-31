using System.Timers;
using SOTOR.Extensions;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class AbilityRadialSelection_VM : ViewModel
{
	private bool _isVisible;

	private MBBindingList<AbilityRadialSelectionItem_VM> _abilities = new MBBindingList<AbilityRadialSelectionItem_VM>();

	private AbilityHUD_VM _abilityVm;

	private bool _errorMessageVisible;

	private string _errorMessageText = string.Empty;

	private readonly Timer _timer;

	private AbilityManagerMissionLogic _abilityLogic;

	[DataSourceProperty]
	public AbilityHUD_VM CurrentAbility
	{
		get
		{
			return _abilityVm;
		}
		set
		{
			if (value != _abilityVm)
			{
				_abilityVm = value;
				OnPropertyChangedWithValue(value, "CurrentAbility");
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
				OnPropertyChangedWithValue(value, "IsVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool ErrorMessageVisible
	{
		get
		{
			return _errorMessageVisible;
		}
		set
		{
			if (value != _errorMessageVisible)
			{
				_errorMessageVisible = value;
				OnPropertyChangedWithValue(value, "ErrorMessageVisible");
			}
		}
	}

	[DataSourceProperty]
	public string ErrorMessageText
	{
		get
		{
			return _errorMessageText;
		}
		set
		{
			if (!(value == _errorMessageText))
			{
				_errorMessageText = value;
				OnPropertyChangedWithValue(value, "ErrorMessageText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<AbilityRadialSelectionItem_VM> Abilities
	{
		get
		{
			return _abilities;
		}
		set
		{
			if (value != _abilities)
			{
				_abilities = value;
				OnPropertyChangedWithValue(value, "Abilities");
			}
		}
	}

	public AbilityRadialSelection_VM()
	{
		_timer = new Timer(2000.0)
		{
			AutoReset = false
		};
		_timer.Elapsed += delegate
		{
			_timer.Stop();
			ErrorMessageVisible = false;
		};
	}

	public override void RefreshValues()
	{
		if (_abilityLogic == null)
		{
			_abilityLogic = Mission.Current?.GetMissionBehavior<AbilityManagerMissionLogic>();
		}
		if (_abilityLogic == null)
		{
			return;
		}
		IsVisible = _abilityLogic.CurrentState == AbilityModeState.QuickMenuSelection;
		if (IsVisible)
		{
			if (CurrentAbility == null)
			{
				AbilityHUD_VM abilityHUD_VM = (CurrentAbility = new AbilityHUD_VM());
			}
			CurrentAbility.RefreshValues();
		}
	}

	public void FillAbilities(Agent agent)
	{
		_abilities.Clear();
		AbilityComponent component = agent.GetComponent<AbilityComponent>();
		if (component == null || component.KnownAbilitySystem.Count == 0)
		{
			return;
		}
		foreach (Ability item in component.KnownAbilitySystem)
		{
			_abilities.Add(new AbilityRadialSelectionItem_VM(item, OnItemSelected));
		}
	}

	public void DisplayErrorMessage(string message)
	{
		if (!ErrorMessageVisible || !_timer.Enabled)
		{
			ErrorMessageVisible = true;
			ErrorMessageText = message;
			_timer.Start();
		}
	}

	private void OnItemSelected(Ability ability)
	{
		Agent.Main?.SelectAbility(ability);
	}
}
