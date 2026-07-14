using Homesteads.Models;
using Homesteads.ViewModels;
using SandBox.View.Map;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace Homesteads.Views;

public static class HomesteadMovingHUD
{
	private static GauntletLayer? _layer;

	private static HomesteadPlacementVM? _vm;

	public static void UpdateAll()
	{
		if (HomesteadBehavior.Instance == null || HomesteadBehavior.Instance.HomesteadMobileParties == null)
		{
			return;
		}
		bool flag = false;
		foreach (Homestead value in HomesteadBehavior.Instance.HomesteadMobileParties.Values)
		{
			if (value.IsMoving)
			{
				flag = true;
				if (_layer == null && MapScreen.Instance != null)
				{
					_vm = new HomesteadPlacementVM(value);
					_layer = new GauntletLayer("HomesteadMovingHUD", 100)
					{
						IsFocusLayer = false
					};
					_layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
					_layer.LoadMovie("HomesteadMovingHUD", _vm);
					((ScreenBase)(object)MapScreen.Instance).AddLayer((ScreenLayer)_layer);
				}
				_vm?.UpdateTargetPosition(value.MobileParty.TargetPosition.ToVec2());
				break;
			}
		}
		if (!flag)
		{
			Remove();
		}
	}

	public static void Remove()
	{
		if (_layer != null && MapScreen.Instance != null)
		{
			((ScreenBase)(object)MapScreen.Instance).RemoveLayer((ScreenLayer)_layer);
			_layer = null;
			_vm = null;
		}
	}
}
