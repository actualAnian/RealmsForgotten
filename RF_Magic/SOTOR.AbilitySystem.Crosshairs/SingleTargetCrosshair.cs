using System;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace SOTOR.AbilitySystem.Crosshairs;

public class SingleTargetCrosshair : MissileCrosshair
{
	private Agent _cachedTarget;

	public Agent CachedTarget => _cachedTarget;

	public bool IsTargetLocked { get; private set; }

	public override Vec3 Position
	{
		get
		{
			if (_cachedTarget != null && _cachedTarget.IsActive())
			{
				return _cachedTarget.GetChestGlobalPosition();
			}
			if (TryGetGroundAimPoint(out var point))
			{
				return point;
			}
			return base.Position;
		}
	}

	private bool TryGetGroundAimPoint(out Vec3 point)
	{
		point = Vec3.Zero;
		if (_missionScreen == null || _caster == null)
		{
			return false;
		}
		try
		{
			if (TryResolveAimPoint(out var point2, out var _))
			{
				if (_caster.Position.Distance(point2) > _template.MaxDistance)
				{
					point2 = _caster.LookFrame.Advance(_template.MaxDistance).origin;
					point2.z = ResolveSurfaceZ(point2);
				}
				point = point2;
				return true;
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SingleTargetCrosshair.TryGetGroundAimPoint failed: " + ex.Message);
		}
		return false;
	}

	public SingleTargetCrosshair(AbilityTemplate template, Mission mission, MissionScreen missionScreen, Agent caster)
		: base(template, mission, missionScreen, caster)
	{
	}

	public override void Show()
	{
		base.Show();
		_cachedTarget = null;
	}

	public override void Hide()
	{
		base.Hide();
		UnlockTarget();
	}

	public override void Tick()
	{
		FindTarget();
	}

	private void FindTarget()
	{
		if (_mission == null || _missionScreen == null || _caster == null)
		{
			return;
		}
		Vec3 sourcePoint = default(Vec3);
		Vec3 targetPoint = default(Vec3);
		_missionScreen.ScreenPointToWorldRay(Input.MousePositionRanged, out sourcePoint, out targetPoint); // [RF-A] 1.4.7: parametro virou out
		Agent agent;
		float collisionDistance;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			agent = _mission.RayCastForClosestAgent(sourcePoint, targetPoint, -1, 0.05f, out collisionDistance);
		}
		if (agent == null)
		{
			UnlockTarget();
			return;
		}
		if (agent.IsMount && agent.RiderAgent != null)
		{
			agent = agent.RiderAgent;
		}
		bool flag = _template.AbilityTargetType == AbilityTargetType.SingleAlly || _template.AbilityTargetType == AbilityTargetType.AlliesInAOE;
		bool flag2 = (flag ? (!agent.IsEnemyOf(_caster)) : agent.IsEnemyOf(_caster));
		if (collisionDistance <= _template.MaxDistance && agent.IsActive() && agent.Health > 0f && !agent.IsFadingOut() && flag2)
		{
			if (agent != _cachedTarget)
			{
				UnlockTarget();
			}
			LockTarget(agent, flag ? friendColor : enemyColor);
		}
		else
		{
			UnlockTarget();
		}
	}

	private void LockTarget(Agent newTarget, uint? glowColor)
	{
		_cachedTarget = newTarget;
		SetContour(_cachedTarget, glowColor);
		IsTargetLocked = true;
	}

	public void UnlockTarget()
	{
		if (_cachedTarget != null)
		{
			SetContour(_cachedTarget, colorLess);
			_cachedTarget = null;
		}
		IsTargetLocked = false;
	}

	private static void SetContour(Agent agent, uint? color)
	{
		try
		{
			if (agent?.AgentVisuals != null)
			{
				agent.AgentVisuals.SetContourColor(color);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SingleTargetCrosshair.SetContour failed: " + ex.Message);
		}
	}
}
