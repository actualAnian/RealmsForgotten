using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace Homesteads.MissionLogics;

internal sealed class LoadingFadeOverlay
{
	private sealed class EmptyVM : ViewModel
	{
	}

	private readonly float _settleTime;

	private GauntletLayer? _layer;

	private ScreenBase? _screen;

	private bool _removed;

	private float _settle;

	public bool IsDone => _removed;

	public LoadingFadeOverlay(float settleTime = 0.8f)
	{
		_settleTime = settleTime;
		_settle = settleTime;
	}

	public void Show()
	{
		if (_removed || _layer != null)
		{
			return;
		}
		try
		{
			ScreenBase topScreen = ScreenManager.TopScreen;
			if (topScreen != null)
			{
				_layer = new GauntletLayer("HomesteadLoadingFade", 1000);
				_layer.LoadMovie("HomesteadLoadingFade", new EmptyVM());
				topScreen.AddLayer(_layer);
				_screen = topScreen;
				TraceLogger.Write("LoadingFadeOverlay", "Loading fade shown.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("LoadingFadeOverlay", "Show failed (" + ex.GetType().Name + ": " + ex.Message + "); revealing scene.");
			_layer = null;
			_screen = null;
			_removed = true;
		}
	}

	public void Tick(float dt, bool ready)
	{
		if (_removed)
		{
			return;
		}
		if (_layer == null)
		{
			Show();
		}
		if (_layer != null && ready)
		{
			_settle -= dt;
			if (_settle <= 0f)
			{
				Remove();
			}
		}
	}

	public void Remove()
	{
		_removed = true;
		try
		{
			if (_screen != null && _layer != null && _screen.HasLayer(_layer))
			{
				_screen.RemoveLayer(_layer);
			}
			TraceLogger.Write("LoadingFadeOverlay", "Loading fade removed — scene revealed.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("LoadingFadeOverlay", "Remove failed: " + ex.Message);
		}
		_layer = null;
		_screen = null;
	}
}
