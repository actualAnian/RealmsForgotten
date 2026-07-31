using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace SOTOR.AbilitySystem.Crosshairs;

public class TargetedAOECrosshair : AbilityCrosshair
{
	private const string RunePrefab = "circular_targeting_rune";

	private GameEntity _runeEntity;

	private Vec3 _groundPosition;

	private Vec3 _groundNormal = Vec3.Up;

	private readonly AbilityTargetType _targetType;

	private readonly MBList<Agent> _targets = new MBList<Agent>();

	private readonly List<Agent> _previousTargets = new List<Agent>();

	public MBReadOnlyList<Agent> Targets => new MBReadOnlyList<Agent>(_targets);

	public override Vec3 Position => _groundPosition;

	public TargetedAOECrosshair(AbilityTemplate template, Mission mission, MissionScreen missionScreen, Agent caster)
		: base(template, mission, missionScreen, caster)
	{
		_targetType = template.AbilityTargetType;
		TryCreateRune();
	}

	private void TryCreateRune()
	{
		try
		{
			_runeEntity = GameEntity.Instantiate(_mission.Scene, "circular_targeting_rune", callScriptCallbacks: true, createPhysics: true, string.Empty);
			if (_runeEntity != null)
			{
				MatrixFrame frame = _runeEntity.GetFrame();
				float num = ((_template.TargetCapturingRadius > 0f) ? _template.TargetCapturingRadius : _template.Radius);
				frame.Scale(new Vec3(num * 2f, num * 2f, 1f));
				_runeEntity.SetFrame(ref frame);
				_runeEntity.SetVisibilityExcludeParents(visible: false);
			}
			else
			{
				SotorLog.Debug("TargetedAOECrosshair: rune prefab 'circular_targeting_rune' not found; using position-only fallback.");
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("TargetedAOECrosshair.TryCreateRune failed: " + ex.Message);
			_runeEntity = null;
		}
	}

	public override void Show()
	{
		base.Show();
		_runeEntity?.SetVisibilityExcludeParents(visible: true);
	}

	public override void Hide()
	{
		base.Hide();
		_runeEntity?.SetVisibilityExcludeParents(visible: false);
		ClearGlow();
	}

	public override void Tick()
	{
		if (_caster != null && _mission != null && _missionScreen != null)
		{
			UpdatePosition();
			_previousTargets.Clear();
			_previousTargets.AddRange(_targets);
			UpdateTargets();
			UpdateGlow();
			CycleColor(_runeEntity);
		}
	}

	private void UpdatePosition()
	{
		if (TryResolveAimPoint(out var point, out var normal))
		{
			if (_caster.Position.Distance(point) > _template.MaxDistance)
			{
				point = _caster.LookFrame.Advance(_template.MaxDistance).origin;
				point.z = ResolveSurfaceZ(point);
				normal = Vec3.Up;
			}
			_groundPosition = point;
			_groundNormal = normal;
		}
		else
		{
			point = _caster.LookFrame.Advance(_template.MaxDistance).origin;
			point.z = ResolveSurfaceZ(point);
			_groundPosition = point;
			_groundNormal = Vec3.Up;
		}
		if (_runeEntity != null)
		{
			Mat3 rot = Mat3.CreateMat3WithForward(in _groundNormal);
			rot.RotateAboutSide(0f - 90f.ToRadians());
			rot.Orthonormalize();
			MatrixFrame frame = new MatrixFrame(in rot, in _groundPosition);
			float num = ((_template.TargetCapturingRadius > 0f) ? _template.TargetCapturingRadius : _template.Radius);
			frame.Scale(new Vec3(num * 2f, num * 2f, 1f));
			_runeEntity.SetFrame(ref frame);
		}
	}

	private void UpdateTargets()
	{
		_targets.Clear();
		float radius = ((_template.TargetCapturingRadius > 0f) ? _template.TargetCapturingRadius : _template.Radius);
		switch (_targetType)
		{
		case AbilityTargetType.AlliesInAOE:
			_mission.GetNearbyAllyAgents(_groundPosition.AsVec2, radius, _mission.PlayerTeam, _targets);
			break;
		case AbilityTargetType.EnemiesInAOE:
			_mission.GetNearbyEnemyAgents(_groundPosition.AsVec2, radius, _mission.PlayerTeam, _targets);
			break;
		}
	}

	private void UpdateGlow()
	{
		uint? color = ((_targetType == AbilityTargetType.AlliesInAOE) ? friendColor : enemyColor);
		foreach (Agent target in _targets)
		{
			SetContour(target, color);
		}
		foreach (Agent item in _previousTargets.Except(_targets))
		{
			SetContour(item, colorLess);
		}
	}

	private void ClearGlow()
	{
		foreach (Agent target in _targets)
		{
			SetContour(target, colorLess);
		}
		foreach (Agent previousTarget in _previousTargets)
		{
			SetContour(previousTarget, colorLess);
		}
		_targets.Clear();
		_previousTargets.Clear();
	}

	private static void SetContour(Agent agent, uint? color)
	{
		try
		{
			if (agent != null && agent.AgentVisuals != null && (agent.State == AgentState.Active || agent.State == AgentState.Routed))
			{
				agent.AgentVisuals.SetContourColor(color);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("TargetedAOECrosshair.SetContour failed: " + ex.Message);
		}
	}

	public override void Dispose()
	{
		ClearGlow();
		if (!(_runeEntity != null))
		{
			return;
		}
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
}
