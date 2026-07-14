using Homesteads.MissionLogics;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace Homesteads.Views;

public class HomesteadBuildingPickerView : MissionView
{
	public static HomesteadBuildingPickerView? Instance;

	private GauntletLayer? _layer;

	private HomesteadBuildingPickerVM? _dataSource;

	public static bool IsOpen { get; private set; }

	public override void OnMissionScreenInitialize()
	{
		((MissionView)this).OnMissionScreenInitialize();
		Instance = this;
	}

	public override void OnMissionScreenFinalize()
	{
		Close();
		if (Instance == this)
		{
			Instance = null;
		}
		((MissionView)this).OnMissionScreenFinalize();
	}

	public void Open(HomesteadSceneEditingMissionLogic owner)
	{
		if (!IsOpen && ((MissionView)this).MissionScreen != null)
		{
			_dataSource = new HomesteadBuildingPickerVM();
			_dataSource.Initialize(owner);
			_dataSource.CloseRequested = Close;
			_layer = new GauntletLayer("HomesteadBuildingPicker", 4000)
			{
				IsFocusLayer = true
			};
			_layer.InputRestrictions.SetInputRestrictions();
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			_layer.LoadMovie("HomesteadBuildingPicker", _dataSource);
			((ScreenBase)(object)((MissionView)this).MissionScreen).AddLayer((ScreenLayer)_layer);
			ScreenManager.TrySetFocus(_layer);
			MBCommon.PauseGameEngine();
			MouseManager.ShowCursor(show: true);
			IsOpen = true;
		}
	}

	public void Close()
	{
		if (!IsOpen)
		{
			return;
		}
		IsOpen = false;
		try
		{
			MBCommon.UnPauseGameEngine();
		}
		catch
		{
		}
		if (_layer != null)
		{
			try
			{
				_layer.InputRestrictions.ResetInputRestrictions();
				if (((MissionView)this).MissionScreen != null)
				{
					((ScreenBase)(object)((MissionView)this).MissionScreen).RemoveLayer((ScreenLayer)_layer);
				}
			}
			catch
			{
			}
		}
		_layer = null;
		_dataSource = null;
	}

	public override void OnMissionScreenTick(float dt)
	{
		((MissionView)this).OnMissionScreenTick(dt);
		if (IsOpen && _layer != null && (_layer.Input.IsHotKeyReleased("Exit") || _layer.Input.IsKeyReleased(InputKey.Escape) || (TaleWorlds.InputSystem.Input.IsGamepadActive && _layer.Input.IsKeyReleased(InputKey.ControllerRRight))))
		{
			Close();
		}
	}
}
