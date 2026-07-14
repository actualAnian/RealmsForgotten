using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace Homesteads.MissionLogics;

internal sealed class DogBannerOverlay
{
	private sealed class BannerVM : ViewModel
	{
		private string _bannerText = "";

		[DataSourceProperty]
		public string BannerText
		{
			get
			{
				return _bannerText;
			}
			set
			{
				if (value != _bannerText)
				{
					_bannerText = value;
					OnPropertyChangedWithValue(value, "BannerText");
				}
			}
		}
	}

	private const float ShowDuration = 6f;

	private GauntletLayer? _layer;

	private ScreenBase? _screen;

	private BannerVM? _vm;

	private float _timer;

	private bool _active;

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
				_vm = new BannerVM();
				_layer = new GauntletLayer("HomesteadDogBanner", 90);
				_layer.LoadMovie("HomesteadDogBanner", _vm);
				_layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
				topScreen.AddLayer(_layer);
				_screen = topScreen;
			}
			_vm.BannerText = text;
			_timer = 6f;
			_active = true;
			TraceLogger.Write("DogBannerOverlay", "Banner shown: \"" + text + "\".");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("DogBannerOverlay", "Show failed: " + ex.GetType().Name + ": " + ex.Message);
			Remove();
		}
	}

	public void Tick(float dt)
	{
		if (_active)
		{
			_timer -= dt;
			if (_timer <= 0f)
			{
				Remove();
			}
		}
	}

	public void Remove()
	{
		_active = false;
		try
		{
			if (_screen != null && _layer != null && _screen.HasLayer(_layer))
			{
				_screen.RemoveLayer(_layer);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("DogBannerOverlay", "Remove failed: " + ex.Message);
		}
		_layer = null;
		_screen = null;
		_vm = null;
	}
}
