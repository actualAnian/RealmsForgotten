using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace Homesteads.MissionLogics;

internal sealed class TrainingMatchOverlay
{
	private sealed class MatchVM : ViewModel
	{
		private string _matchText = "";

		[DataSourceProperty]
		public string MatchText
		{
			get
			{
				return _matchText;
			}
			set
			{
				if (value != _matchText)
				{
					_matchText = value;
					OnPropertyChangedWithValue(value, "MatchText");
				}
			}
		}
	}

	private GauntletLayer? _layer;

	private ScreenBase? _screen;

	private MatchVM? _vm;

	public bool IsShown => _layer != null;

	public void Show(string text)
	{
		try
		{
			if (_layer == null)
			{
				ScreenBase topScreen = ScreenManager.TopScreen;
				if (topScreen == null)
				{
					return;
				}
				_vm = new MatchVM();
				_layer = new GauntletLayer("HomesteadTrainingMatchHUD", 90);
				_layer.LoadMovie("HomesteadTrainingMatchHUD", _vm);
				_layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
				topScreen.AddLayer(_layer);
				_screen = topScreen;
			}
			_vm.MatchText = text;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("TrainingMatchOverlay", "Show failed: " + ex.GetType().Name + ": " + ex.Message);
			Remove();
		}
	}

	public void SetText(string text)
	{
		if (_vm != null)
		{
			_vm.MatchText = text;
		}
	}

	public void Remove()
	{
		try
		{
			if (_screen != null && _layer != null && _screen.HasLayer(_layer))
			{
				_screen.RemoveLayer(_layer);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("TrainingMatchOverlay", "Remove failed: " + ex.Message);
		}
		_layer = null;
		_screen = null;
		_vm = null;
	}
}
