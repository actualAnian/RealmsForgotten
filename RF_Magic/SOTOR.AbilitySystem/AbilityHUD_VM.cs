using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class AbilityHUD_VM : ViewModel
{
	private string _name = string.Empty;

	private string _spriteName = string.Empty;

	private string _coolDownLeft = string.Empty;

	private string _windsOfMagicLeft = "-";

	private bool _isVisible;

	private bool _onCoolDown;

	private bool _isSpell;

	private string _windsCost = string.Empty;

	private bool _isDisabled;

	private string _disabledText = string.Empty;

	private string _abilityType = string.Empty;

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
	public string WindsOfMagicLeft
	{
		get
		{
			return _windsOfMagicLeft;
		}
		set
		{
			_windsOfMagicLeft = value;
			OnPropertyChangedWithValue(value, "WindsOfMagicLeft");
		}
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
			if (!(value == _name))
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
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
			if (!(value == _spriteName))
			{
				_spriteName = value;
				OnPropertyChangedWithValue(value, "SpriteName");
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
			if (!(value == _coolDownLeft))
			{
				_coolDownLeft = value;
				OnPropertyChangedWithValue(value, "CoolDownLeft");
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
				OnPropertyChangedWithValue(value, "IsOnCoolDown");
			}
		}
	}

	[DataSourceProperty]
	public string WindsCost
	{
		get
		{
			return _windsCost;
		}
		set
		{
			if (!(value == _windsCost))
			{
				_windsCost = value;
				OnPropertyChangedWithValue(value, "WindsCost");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSpell
	{
		get
		{
			return _isSpell;
		}
		set
		{
			if (value != _isSpell)
			{
				_isSpell = value;
				OnPropertyChangedWithValue(value, "IsSpell");
			}
		}
	}

	[DataSourceProperty]
	public string AbilityType
	{
		get
		{
			return _abilityType;
		}
		set
		{
			if (!(value == _abilityType))
			{
				_abilityType = value;
				OnPropertyChangedWithValue(value, "AbilityType");
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
				OnPropertyChangedWithValue(value, "IsDisabled");
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
			if (!(value == _disabledText))
			{
				_disabledText = value;
				OnPropertyChangedWithValue(value, "DisabledText");
			}
		}
	}

	public override void RefreshValues()
	{
		Ability ability = Agent.Main?.GetCurrentAbility();
		AbilityManagerMissionLogic abilityManagerMissionLogic = Mission.Current?.GetMissionBehavior<AbilityManagerMissionLogic>();
		Mission current = Mission.Current;
		// [RF-B] sem foco arcano empunhado nao ha bateria para mostrar — a barra de
		// Mana some junto com a capacidade de conjurar.
		IsVisible = ability != null && abilityManagerMissionLogic != null && current != null && AbilityMissionModeHelper.IsAbilityHudMissionMode(current) && IsHudAllowedByMode(abilityManagerMissionLogic) && SOTOR.RFIntegration.ArcaneFocusGate.CanCastAtAll(Agent.Main);
		if (IsVisible)
		{
			AbilityType = "(" + ability.Template.AbilityType.ToString() + ")";
			IsSpell = ability.Template.AbilityType == SOTOR.AbilitySystem.AbilityType.Spell;
			SpriteName = ability.Template.SpriteName;
			Name = ability.Template.Name;
			WindsCost = ability.Template.WindsOfMagicCost.ToString();
			Hero hero = Agent.Main?.GetHero();
			if (hero != null && hero.GetExtendedInfo() != null)
			{
				// [RF-C] "atual/maximo" em vez de so o atual: no RF o TETO vem do
				// cajado, e mostrar apenas o atual tornava a troca de foco invisivel
				// (varinha e cajado grande liam o mesmo numero quando a mana estava
				// abaixo dos dois tetos).
				WindsOfMagicLeft = (int)hero.GetWindsOfMagic() + "/" + (int)hero.GetMaxWindsOfMagic();
			}
			else
			{
				WindsOfMagicLeft = "-";
			}
			CoolDownLeft = ability.GetCoolDownLeft().ToString();
			IsOnCoolDown = ability.IsOnCooldown();
			if (ability.IsDisabled(Agent.Main, out var disabledReason))
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

	private static bool IsHudAllowedByMode(AbilityManagerMissionLogic abilityLogic)
	{
		switch (SotorSettings.HudMode)
		{
		case 2:
			return false;
		case 1:
			if (abilityLogic != null)
			{
				return abilityLogic.CurrentState == AbilityModeState.QuickMenuSelection;
			}
			return false;
		default:
			return true;
		}
	}
}
