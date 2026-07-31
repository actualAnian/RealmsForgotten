using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace SOTOR.AbilitySystem.Crosshairs;

public class WindCrosshair : AbilityCrosshair
{
	private const string RunePrefab = "linear_targeting_rune";

	private GameEntity _parent;

	private GameEntity _runeEntity;

	private Vec3 _position;

	private Vec3 _normal = Vec3.Up;

	private MatrixFrame _frame = MatrixFrame.Identity;

	public override Vec3 Position => _position;

	public override MatrixFrame Frame => _frame;

	public WindCrosshair(AbilityTemplate template, Mission mission, MissionScreen missionScreen, Agent caster)
		: base(template, mission, missionScreen, caster)
	{
		TryCreateRune();
	}

	private void TryCreateRune()
	{
		try
		{
			_parent = GameEntity.CreateEmpty(_mission.Scene, isModifiableFromEditor: false);
			_runeEntity = GameEntity.Instantiate(_mission.Scene, "linear_targeting_rune", callScriptCallbacks: false, createPhysics: true, string.Empty);
			if (_runeEntity != null)
			{
				MatrixFrame frame = _runeEntity.GetFrame();
				frame.Scale(new Vec3(_template.Radius, _template.Radius, 1f));
				frame = frame.Advance(-0.8f).Strafe(0.025f);
				_runeEntity.SetFrame(ref frame);
				_parent.AddChild(_runeEntity);
				_parent.SetVisibilityExcludeParents(visible: false);
				_runeEntity.SetVisibilityExcludeParents(visible: false);
				SotorLog.Info(string.Format("WindCrosshair: rune '{0}' created (parent/child), scale=Radius({1}).", "linear_targeting_rune", _template.Radius));
			}
			else
			{
				SotorLog.Debug("WindCrosshair: rune prefab 'linear_targeting_rune' not found; using position-only fallback.");
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("WindCrosshair.TryCreateRune failed: " + ex.Message);
			_runeEntity = null;
		}
	}

	public override void Show()
	{
		base.Show();
		_parent?.SetVisibilityExcludeParents(visible: true);
		_runeEntity?.SetVisibilityExcludeParents(visible: true);
	}

	public override void Hide()
	{
		base.Hide();
		_parent?.SetVisibilityExcludeParents(visible: false);
		_runeEntity?.SetVisibilityExcludeParents(visible: false);
	}

	public override void Tick()
	{
		if (_caster != null && _mission != null && _missionScreen != null)
		{
			if (!TryResolveAimPoint(out var point, out var normal))
			{
				point = _caster.Position;
				normal = Vec3.Up;
			}
			float num = _caster.Position.Distance(point);
			Vec3 lookDirection = _caster.LookDirection;
			lookDirection.z = 0f;
			MatrixFrame frame = ((lookDirection.LengthSquared < 0.0001f) ? _caster.LookFrame : new MatrixFrame(Mat3.CreateMat3WithForward(lookDirection.NormalizedCopy()), _caster.Position));
			frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			if (num < _template.MinDistance)
			{
				point = frame.Advance(_template.MinDistance).origin;
			}
			else if (num > _template.MaxDistance)
			{
				point = frame.Advance(_template.MaxDistance).origin;
			}
			point.z = ResolveSurfaceZ(point);
			_position = point;
			_normal = normal;
			_frame = frame;
			_frame.origin = _position;
			Mat3 mat = Mat3.CreateMat3WithForward(in normal);
			_frame.rotation.u = mat.f;
			_frame.rotation.RotateAboutSide(5f.ToRadians());
			_frame.rotation.Orthonormalize();
			_parent?.SetGlobalFrame(in _frame);
			CycleColor(_runeEntity);
		}
	}

	public override void Dispose()
	{
		if (_runeEntity != null)
		{
			try
			{
				_runeEntity.FadeOut(0.05f, isRemovingFromScene: true);
			}
			catch
			{
				try
				{
					_runeEntity.Remove(0);
				}
				catch
				{
				}
			}
			_runeEntity = null;
		}
		if (!(_parent != null))
		{
			return;
		}
		try
		{
			_parent.FadeOut(0.05f, isRemovingFromScene: true);
		}
		catch
		{
			try
			{
				_parent.Remove(0);
			}
			catch
			{
			}
		}
		_parent = null;
	}
}
