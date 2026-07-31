using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace SOTOR.AbilitySystem;

[DefaultView]
public class AbilityHUDMissionView : MissionView
{
	private int _countOfAbilities;

	private bool _isInitialized;

	private AbilityHUD_VM _abilityHudVm;

	private AbilityRadialSelection_VM _abilityRadialSelectionVm;

	private GauntletLayer _abilityLayer;

	private GauntletLayer _radialMenuLayer;

	public override void OnBehaviorInitialize()
	{
		// [RF-A] artefato ILSpy: emitiu "((MissionBehavior)this).OnBehaviorInitialize()".
		// Cast em `this` NAO desliga dispatch virtual em C#, entao o metodo chamava a
		// si mesmo — StackOverflow ao entrar em batalha. O original era chamada de
		// base; MissionView nao sobrescreve, logo base == MissionBehavior.
		base.OnBehaviorInitialize();
		Mission.Current.OnMainAgentChanged += CheckMainAgent;
		_abilityHudVm = new AbilityHUD_VM();
		_abilityLayer = new GauntletLayer("GauntletLayer", 100);
		_abilityLayer.LoadMovie("AbilityHUD", _abilityHudVm);
		((ScreenBase)(object)((MissionView)this).MissionScreen).AddLayer((ScreenLayer)_abilityLayer);
		_abilityRadialSelectionVm = new AbilityRadialSelection_VM();
		_radialMenuLayer = new GauntletLayer("GauntletLayer", 98);
		_radialMenuLayer.LoadMovie("AbilityRadialSelection", _abilityRadialSelectionVm);
		((ScreenBase)(object)((MissionView)this).MissionScreen).AddLayer((ScreenLayer)_radialMenuLayer);
		_isInitialized = true;
	}

	private void CheckMainAgent(Agent oldAgent)
	{
		if (Agent.Main != null)
		{
			AbilityComponent component = Agent.Main.GetComponent<AbilityComponent>();
			if (component != null)
			{
				_countOfAbilities = component.KnownAbilitySystem.Count;
				_abilityRadialSelectionVm?.FillAbilities(Agent.Main);
			}
		}
	}

	public void DisplayErrorMessage(string message)
	{
		_abilityRadialSelectionVm?.DisplayErrorMessage(message);
	}

	public void OnQuickMenuOpened()
	{
		if (_isInitialized && Agent.Main != null)
		{
			EnsureMainAgentAbilitiesCached();
			_abilityRadialSelectionVm?.FillAbilities(Agent.Main);
			_abilityRadialSelectionVm?.RefreshValues();
			SotorLog.Info($"Quick menu UI refresh. abilities={_countOfAbilities}");
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (!_isInitialized)
		{
			return;
		}
		EnsureMainAgentAbilitiesCached();
		AbilityManagerMissionLogic obj = Mission.Current?.GetMissionBehavior<AbilityManagerMissionLogic>();
		bool flag = obj != null && obj.CurrentState == AbilityModeState.QuickMenuSelection;
		if (flag)
		{
			_abilityRadialSelectionVm?.RefreshValues();
		}
		if (IsAgentControlContext())
		{
			if (_countOfAbilities > 0)
			{
				_abilityHudVm.RefreshValues();
				if (!flag)
				{
					_abilityRadialSelectionVm.RefreshValues();
				}
			}
		}
		else if (!flag)
		{
			_abilityHudVm.IsVisible = false;
			_abilityRadialSelectionVm.IsVisible = false;
		}
		else
		{
			_abilityHudVm.IsVisible = false;
		}
	}

	private bool IsAgentControlContext()
	{
		Agent main = Agent.Main;
		Mission current = Mission.Current;
		if (main != null && main.State == AgentState.Active && AbilityMissionModeHelper.IsAbilityHudMissionMode(current) && ((MissionView)this).MissionScreen.CustomCamera == null && !((MissionView)this).MissionScreen.IsViewingCharacter() && !((MissionView)this).MissionScreen.IsPhotoModeEnabled && !current.IsOrderMenuOpen)
		{
			return !ScreenManager.GetMouseVisibility();
		}
		return false;
	}

	private void EnsureMainAgentAbilitiesCached()
	{
		if (Agent.Main == null)
		{
			return;
		}
		AbilityComponent component = Agent.Main.GetComponent<AbilityComponent>();
		if (component != null)
		{
			int count = component.KnownAbilitySystem.Count;
			if (count != _countOfAbilities)
			{
				_countOfAbilities = count;
				_abilityRadialSelectionVm?.FillAbilities(Agent.Main);
				SotorLog.Info($"HUD ability count updated: {_countOfAbilities}");
			}
		}
	}
}
