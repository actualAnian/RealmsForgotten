using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace SOTOR.AbilitySystem.Crosshairs;

public class MissileCrosshair : AbilityCrosshair
{
	private GauntletLayer _layer;

	private ProjectileCrosshair_VM _vm;

	public override bool IsVisible
	{
		get
		{
			if (_vm != null)
			{
				return _vm.IsVisible;
			}
			return false;
		}
		protected set
		{
			if (_vm != null)
			{
				_vm.IsVisible = value;
			}
		}
	}

	public MissileCrosshair(AbilityTemplate template, Mission mission, MissionScreen missionScreen, Agent caster)
		: base(template, mission, missionScreen, caster)
	{
		try
		{
			_vm = new ProjectileCrosshair_VM();
			_layer = new GauntletLayer("GauntletLayer", 101);
			GauntletMovieIdentifier gauntletMovieIdentifier = _layer.LoadMovie("ProjectileCrosshair", _vm);
			((ScreenBase)(object)_missionScreen).AddLayer((ScreenLayer)_layer);
			_vm.IsVisible = false;
			SotorLog.Info($"MissileCrosshair: movie loaded={gauntletMovieIdentifier != null} layer added, sprite='{_vm.SpriteName}'.");
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MissileCrosshair: failed to load reticle movie: " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	public override void Show()
	{
		IsVisible = true;
		SotorLog.Info($"MissileCrosshair.Show: IsVisible={IsVisible} vm={_vm != null} layer={_layer != null}.");
	}

	public override void Hide()
	{
		IsVisible = false;
	}

	public override void Dispose()
	{
		base.Dispose();
		if (_layer != null)
		{
			try
			{
				((ScreenBase)(object)_missionScreen)?.RemoveLayer((ScreenLayer)_layer);
			}
			catch (Exception ex)
			{
				SotorLog.Warn("MissileCrosshair.Dispose: " + ex.Message);
			}
			_layer = null;
		}
		_vm = null;
	}
}
