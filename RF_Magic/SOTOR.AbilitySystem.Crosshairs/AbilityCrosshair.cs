using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace SOTOR.AbilitySystem.Crosshairs;

public abstract class AbilityCrosshair : IDisposable, ICrosshair
{
	protected readonly uint? friendColor = new Color(0f, 0.255f, 0f).ToUnsignedInteger();

	protected readonly uint? enemyColor = new Color(0.255f, 0f, 0f).ToUnsignedInteger();

	protected readonly uint? colorLess = new Color(0f, 0f, 0f, 0f).ToUnsignedInteger();

	protected readonly AbilityTemplate _template;

	protected readonly Mission _mission;

	protected readonly MissionScreen _missionScreen;

	protected readonly Agent _caster;

	private List<uint> _cycleColors;

	private int _colorIndex;

	public CrosshairType CrosshairType { get; }

	public virtual bool IsVisible { get; protected set; }

	public virtual Vec3 Position
	{
		get
		{
			if (_caster == null)
			{
				return Vec3.Zero;
			}
			return _caster.GetChestGlobalPosition();
		}
	}

	public virtual MatrixFrame Frame
	{
		get
		{
			if (_caster == null)
			{
				return MatrixFrame.Identity;
			}
			return _caster.LookFrame;
		}
	}

	protected void CycleColor(GameEntity runeEntity)
	{
		if (!(runeEntity == null))
		{
			if (_cycleColors == null)
			{
				BuildCycleColors();
			}
			_colorIndex = (_colorIndex + 1) % _cycleColors.Count;
			runeEntity.SetFactorColor(_cycleColors[_colorIndex]);
		}
	}

	private void BuildCycleColors()
	{
		_cycleColors = new List<uint>();
		float red = 0.255f;
		float num = 0f;
		float blue = 0f;
		for (num = 0f; num < 0.254f; num += 0.001f)
		{
			_cycleColors.Add(new Color(red, num, blue).ToUnsignedInteger());
		}
		for (red = 0.254f; red > 0.001f; red -= 0.001f)
		{
			_cycleColors.Add(new Color(red, num, blue).ToUnsignedInteger());
		}
		for (blue = 0f; blue < 0.254f; blue += 0.001f)
		{
			_cycleColors.Add(new Color(red, num, blue).ToUnsignedInteger());
		}
		for (num = 0.254f; num > 0.001f; num -= 0.001f)
		{
			_cycleColors.Add(new Color(red, num, blue).ToUnsignedInteger());
		}
		for (red = 0f; red < 0.254f; red += 0.001f)
		{
			_cycleColors.Add(new Color(red, num, blue).ToUnsignedInteger());
		}
		for (blue = 0.254f; blue > 0.001f; blue -= 0.001f)
		{
			_cycleColors.Add(new Color(red, num, blue).ToUnsignedInteger());
		}
	}

	protected AbilityCrosshair(AbilityTemplate template, Mission mission, MissionScreen missionScreen, Agent caster)
	{
		_template = template;
		_mission = mission;
		_missionScreen = missionScreen;
		_caster = caster;
		CrosshairType = template.CrosshairType;
	}

	public bool TryGetCameraAimDirection(out Vec3 direction)
	{
		direction = Vec3.Forward;
		if (_missionScreen == null || _caster == null)
		{
			return false;
		}
		try
		{
			Vec3 vec = default(Vec3);
			Vec3 vec2 = default(Vec3);
			_missionScreen.ScreenPointToWorldRay(Input.MousePositionRanged, out vec, out vec2); // [RF-A] 1.4.7: parametro virou out
			if ((vec2 - vec).LengthSquared < 1E-06f)
			{
				return false;
			}
			Agent agent;
			using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
			{
				agent = _mission.RayCastForClosestAgent(vec, vec2, -1, 0.05f, out var _);
			}
			Vec3 obj = agent?.GetChestGlobalPosition() ?? vec2;
			Vec3 eyeGlobalPosition = _caster.GetEyeGlobalPosition();
			Vec3 vec3 = obj - eyeGlobalPosition;
			if (vec3.LengthSquared < 1E-06f)
			{
				return false;
			}
			direction = vec3.NormalizedCopy();
			return true;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("TryGetCameraAimDirection failed: " + ex.Message);
			return false;
		}
	}

	protected float ResolveSurfaceZ(Vec3 at)
	{
		float groundHeightAtPosition;
		float waterLevelAtPosition;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			groundHeightAtPosition = _mission.Scene.GetGroundHeightAtPosition(at);
			waterLevelAtPosition = _mission.Scene.GetWaterLevelAtPosition(at.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: true);
		}
		return Math.Max(groundHeightAtPosition, waterLevelAtPosition);
	}

	protected bool TryResolveAimPoint(out Vec3 point, out Vec3 normal)
	{
		point = Vec3.Zero;
		normal = Vec3.Up;
		Vec3 vec = default(Vec3);
		Vec3 vec2 = default(Vec3);
		bool projectedMousePositionOnGround = _missionScreen.GetProjectedMousePositionOnGround(out vec, out vec2, BodyFlags.CommonFocusRayCastExcludeFlags, true); // [RF-A] 1.4.7: parametro virou out
		Vec3 vec3 = default(Vec3);
		bool projectedMousePositionOnWater = _missionScreen.GetProjectedMousePositionOnWater(out vec3); // [RF-A] 1.4.7: parametro virou out
		if (!projectedMousePositionOnGround && !projectedMousePositionOnWater)
		{
			return false;
		}
		if (projectedMousePositionOnGround && !projectedMousePositionOnWater)
		{
			point = vec;
			normal = vec2;
			return true;
		}
		if (projectedMousePositionOnWater && !projectedMousePositionOnGround)
		{
			point = vec3;
			normal = Vec3.Up;
			return true;
		}
		Vec3 vec4 = ((_caster != null) ? _caster.GetEyeGlobalPosition() : vec);
		if (vec4.DistanceSquared(vec3) < vec4.DistanceSquared(vec))
		{
			point = vec3;
			normal = Vec3.Up;
		}
		else
		{
			point = vec;
			normal = vec2;
		}
		return true;
	}

	public virtual void Tick()
	{
	}

	public virtual void Show()
	{
		IsVisible = true;
	}

	public virtual void Hide()
	{
		IsVisible = false;
	}

	public virtual void Dispose()
	{
	}
}
