using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

internal sealed class HomesteadBallistaTurretController
{
	private sealed class TurretState
	{
		public Agent? Target;

		public float FireCooldown;

		public float ReacquireTimer;

		public bool Initialized;

		public WeakGameEntity? NavelEntity;

		public MatrixFrame InitialNavelLocalFrame;

		public WeakGameEntity? StandingPointEntity;

		public MatrixFrame InitialStandingPointLocalFrame;

		public bool HasStandingPoint;

		public float CurrentRelativeYaw;
	}

	private const float MaxEngageRange = 200f;

	private const float MaxEngageRangeSq = 40000f;

	private const float VisualTurnRateRadPerSec = TaleWorlds.Library.MathF.PI;

	private const float FireIntervalSeconds = 4f;

	private const float ReacquireSeconds = 0.6f;

	private const float AimHeightOffset = 1f;

	private const float MuzzleHeight = 1.6f;

	private const float BoltSpeed = 45f;

	private readonly Dictionary<Ballista, TurretState> _turrets = new Dictionary<Ballista, TurretState>();

	private bool _loggedFirstShot;

	private bool _loggedNoBolt;

	private static FieldInfo? _origMissileField;

	private static FieldInfo? _missileIdField;

	private static bool _reflectionResolved;

	private static readonly HashSet<int> _trackedBallistaMissileIndices = new HashSet<int>();

	private bool _missileRemovedHookInstalled;

	private const string BallistaNavelTag = "BallistaNavel";

	public int Count => _turrets.Count;

	public static bool IsTrackedBallistaMissile(int missileIndex)
	{
		return _trackedBallistaMissileIndices.Contains(missileIndex);
	}

	public void Rebuild(IEnumerable<KeyValuePair<GameEntity, HomesteadSceneSavedEntity>> loadedEntities)
	{
		EnsureMissileRemovedHookInstalled();
		_turrets.Clear();
		if (loadedEntities == null)
		{
			return;
		}
		foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> loadedEntity in loadedEntities)
		{
			GameEntity key = loadedEntity.Key;
			if (!(key == null))
			{
				Ballista ballista;
				try
				{
					ballista = key.GetFirstScriptOfType<Ballista>();
				}
				catch
				{
					ballista = null;
				}
				if (ballista != null && !_turrets.ContainsKey(ballista))
				{
					_turrets[ballista] = new TurretState
					{
						FireCooldown = MBRandom.RandomFloat * 4f
					};
				}
			}
		}
		TraceLogger.Write("HomesteadBallistaTurretController", $"Rebuilt turret list: {_turrets.Count} ballista(s) will auto-fire.");
	}

	private void EnsureMissileRemovedHookInstalled()
	{
		if (!_missileRemovedHookInstalled && Mission.Current != null)
		{
			_missileRemovedHookInstalled = true;
			Mission.Current.OnMissileRemovedEvent += delegate(int idx)
			{
				_trackedBallistaMissileIndices.Remove(idx);
			};
		}
	}

	public void Tick(float dt, Team? playerTeam)
	{
		if (_turrets.Count == 0 || playerTeam == null || Mission.Current == null)
		{
			return;
		}
		Agent mainAgent = Mission.Current.MainAgent;
		foreach (KeyValuePair<Ballista, TurretState> turret in _turrets)
		{
			Ballista key = turret.Key;
			TurretState value = turret.Value;
			if (key == null || key.GameEntity == null)
			{
				continue;
			}
			if (key.PilotAgent != null)
			{
				value.Target = null;
				continue;
			}
			value.FireCooldown -= dt;
			value.ReacquireTimer -= dt;
			try
			{
				if (value.ReacquireTimer <= 0f || !IsValidTarget(key, value.Target, playerTeam))
				{
					value.ReacquireTimer = 0.6f;
					value.Target = FindBestTarget(key, playerTeam);
				}
				if (value.Target == null)
				{
					continue;
				}
				Vec3 position = value.Target.Position;
				position.z += 1f;
				RotateTurretTowardTarget(key, value, position, dt);
				if (value.FireCooldown <= 0f && mainAgent != null && HasClearShot(key, value.Target))
				{
					if (!IsFriendlyNextToTarget(value.Target, playerTeam))
					{
						bool flag = FireCustomBolt(key, value.Target, mainAgent);
						value.FireCooldown = (flag ? 4f : 1f);
					}
					else
					{
						value.FireCooldown = 1f;
					}
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadBallistaTurretController", "Tick: drive failed for a ballista (" + ex.GetType().Name + ": " + ex.Message + "); skipping it this frame.");
			}
		}
	}

	private bool FireCustomBolt(Ballista ballista, Agent target, Agent shooter)
	{
		ItemObject itemObject = ResolveMissileItem(ballista);
		if (itemObject == null || !itemObject.HasWeaponComponent)
		{
			if (!_loggedNoBolt)
			{
				_loggedNoBolt = true;
				TraceLogger.Write("HomesteadBallistaTurretController", "FireCustomBolt: could not resolve a bolt item with a weapon component; ballista will not fire.");
			}
			return false;
		}
		Vec3 globalPosition = ballista.GameEntity.GlobalPosition;
		globalPosition.z += 1.6f;
		Vec3 position = target.Position;
		position.z += 1f;
		Vec3 toTarget = position - globalPosition;
		if (toTarget.LengthSquared < 0.0001f)
		{
			return false;
		}
		MissionWeapon missionWeapon = new MissionWeapon(itemObject, null, null);
		Vec3 vec = ComputeElevatedFireDirection(itemObject, missionWeapon, toTarget);
		Mat3 orientation = BuildOrientation(vec);
		try
		{
			Mission.Missile missile = Mission.Current.AddCustomMissile(shooter, missionWeapon, globalPosition, vec, orientation, 45f, 45f, addRigidBody: true, ballista);
			if (missile != null)
			{
				_trackedBallistaMissileIndices.Add(missile.Index);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBallistaTurretController", "FireCustomBolt: AddCustomMissile threw " + ex.GetType().Name + ": " + ex.Message + ".");
			return false;
		}
		if (!_loggedFirstShot)
		{
			_loggedFirstShot = true;
			TraceLogger.Write("HomesteadBallistaTurretController", $"First auto-fire: spawned custom bolt '{itemObject.StringId}' at speed {45f}.");
		}
		return true;
	}

	private static Vec3 ComputeElevatedFireDirection(ItemObject bolt, MissionWeapon weapon, Vec3 toTarget)
	{
		Vec3 result = toTarget.NormalizedCopy();
		try
		{
			WeaponStatsData weaponStatsData = weapon.GetWeaponStatsDataForUsage(0);
			float airFrictionConstant = ItemObject.GetAirFrictionConstant(bolt.PrimaryWeapon.WeaponClass, bolt.PrimaryWeapon.WeaponFlags);
			float missileVerticalAimCorrection = Mission.GetMissileVerticalAimCorrection(toTarget, 45f, ref weaponStatsData, airFrictionConstant);
			if (float.IsNaN(missileVerticalAimCorrection) || missileVerticalAimCorrection > TaleWorlds.Library.MathF.PI / 2f)
			{
				return result;
			}
			Vec3 vec = new Vec3(toTarget.x, toTarget.y).NormalizedCopy();
			vec.z += TaleWorlds.Library.MathF.Sin(missileVerticalAimCorrection);
			return vec.NormalizedCopy();
		}
		catch
		{
			return result;
		}
	}

	private static bool IsAimable(Ballista ballista, Vec3 aimPoint)
	{
		try
		{
			return ballista.GetTargetReleaseAngle(aimPoint) <= TaleWorlds.Library.MathF.PI / 2f;
		}
		catch
		{
			return true;
		}
	}

	private static void RotateTurretTowardTarget(Ballista ballista, TurretState state, Vec3 aimPoint, float dt)
	{
		WeakGameEntity? navelEntity;
		if (!state.Initialized)
		{
			state.Initialized = true;
			List<WeakGameEntity> list = new List<WeakGameEntity>();
			ballista.GameEntity.GetChildrenWithTagRecursive(list, "BallistaNavel");
			state.NavelEntity = ((list.Count > 0) ? new WeakGameEntity?(list[0]) : ((WeakGameEntity?)null));
			navelEntity = state.NavelEntity;
			if (navelEntity.HasValue)
			{
				state.InitialNavelLocalFrame = navelEntity.GetValueOrDefault().GetFrame();
			}
			state.StandingPointEntity = ballista.PilotStandingPoint?.GameEntity;
			navelEntity = state.StandingPointEntity;
			if (navelEntity.HasValue)
			{
				state.InitialStandingPointLocalFrame = navelEntity.GetValueOrDefault().GetFrame();
				state.HasStandingPoint = true;
			}
		}
		navelEntity = state.NavelEntity;
		if (!navelEntity.HasValue)
		{
			return;
		}
		WeakGameEntity valueOrDefault = navelEntity.GetValueOrDefault();
		Vec3 globalPosition = ballista.GameEntity.GlobalPosition;
		float num = aimPoint.x - globalPosition.x;
		float num2 = aimPoint.y - globalPosition.y;
		if (num * num + num2 * num2 < 0.0001f)
		{
			return;
		}
		Vec3 s = state.InitialNavelLocalFrame.rotation.s;
		float num3 = WrapAngle(TaleWorlds.Library.MathF.Atan2(s.y, s.x) - TaleWorlds.Library.MathF.PI / 2f);
		float num4 = WrapAngle(WrapAngle(TaleWorlds.Library.MathF.Atan2(num2, num) - num3) - state.CurrentRelativeYaw);
		float num5 = TaleWorlds.Library.MathF.PI * dt;
		state.CurrentRelativeYaw += ((TaleWorlds.Library.MathF.Abs(num4) <= num5) ? num4 : (num5 * (float)Math.Sign(num4)));
		MatrixFrame frame = state.InitialNavelLocalFrame;
		frame.rotation.RotateAboutUp(state.CurrentRelativeYaw);
		valueOrDefault.SetLocalFrame(ref frame, isTeleportation: false);
		if (state.HasStandingPoint)
		{
			navelEntity = state.StandingPointEntity;
			if (navelEntity.HasValue)
			{
				WeakGameEntity valueOrDefault2 = navelEntity.GetValueOrDefault();
				MatrixFrame frame2 = frame.TransformToParent(state.InitialNavelLocalFrame.TransformToLocal(in state.InitialStandingPointLocalFrame));
				valueOrDefault2.SetLocalFrame(ref frame2, isTeleportation: false);
			}
		}
	}

	private static float WrapAngle(float angle)
	{
		float num = TaleWorlds.Library.MathF.PI * 2f;
		while (angle > TaleWorlds.Library.MathF.PI)
		{
			angle -= num;
		}
		while (angle <= -TaleWorlds.Library.MathF.PI)
		{
			angle += num;
		}
		return angle;
	}

	private static Mat3 BuildOrientation(Vec3 dir)
	{
		Mat3 identity = Mat3.Identity;
		identity.f = dir;
		Vec3 vec = Vec3.CrossProduct(Vec3.Up, dir);
		if (vec.LengthSquared < 0.0001f)
		{
			vec = Vec3.Side;
		}
		identity.s = vec.NormalizedCopy();
		identity.u = Vec3.CrossProduct(identity.s, identity.f).NormalizedCopy();
		identity.f = identity.f.NormalizedCopy();
		return identity;
	}

	private static bool HasClearShot(Ballista ballista, Agent target)
	{
		if (ballista != null)
		{
			_ = ballista.GameEntity;
			if (0 == 0 && target != null)
			{
				Vec3 globalPosition = ballista.GameEntity.GlobalPosition;
				globalPosition.z += 1.6f;
				Vec3 position = target.Position;
				position.z += 1f;
				Vec3 vec = position - globalPosition;
				float length = vec.Length;
				if (length < 2f)
				{
					return true;
				}
				Vec3 vec2 = vec * (1f / length);
				Vec3 sourcePoint = globalPosition + vec2 * 1.5f;
				float num = length - 1.5f;
				try
				{
					if (Mission.Current.Scene.RayCastForClosestEntityOrTerrain(sourcePoint, position, out var collisionDistance, 0.05f, BodyFlags.CommonCollisionExcludeFlagsForMissile) && collisionDistance < num - 1f)
					{
						return false;
					}
				}
				catch
				{
				}
				return true;
			}
		}
		return false;
	}

	private static bool IsFriendlyNextToTarget(Agent target, Team playerTeam)
	{
		Vec3 position = target.Position;
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent != null && agent.IsActive() && agent.IsHuman && !(agent.Health <= 0f) && agent != target && (agent.Team == null || !agent.Team.IsEnemyOf(playerTeam)) && agent.Position.DistanceSquared(position) < 4f)
			{
				return true;
			}
		}
		return false;
	}

	private static float HorizontalDistanceSq(Vec3 a, Vec3 b)
	{
		float num = a.x - b.x;
		float num2 = a.y - b.y;
		return num * num + num2 * num2;
	}

	private static bool IsValidTarget(Ballista ballista, Agent? target, Team playerTeam)
	{
		if (target == null || !target.IsActive() || !target.IsHuman || target.Health <= 0f)
		{
			return false;
		}
		if (target.Team == null || !target.Team.IsEnemyOf(playerTeam))
		{
			return false;
		}
		if (HorizontalDistanceSq(ballista.GameEntity.GlobalPosition, target.Position) > 40000f)
		{
			return false;
		}
		Vec3 position = target.Position;
		position.z += 1f;
		if (!IsAimable(ballista, position))
		{
			return false;
		}
		return true;
	}

	private static Agent? FindBestTarget(Ballista ballista, Team playerTeam)
	{
		Vec3 globalPosition = ballista.GameEntity.GlobalPosition;
		Agent result = null;
		float num = 40000f;
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent == null || !agent.IsActive() || !agent.IsHuman || agent.IsMount || agent.Health <= 0f || agent.Team == null || !agent.Team.IsEnemyOf(playerTeam))
			{
				continue;
			}
			float num2 = HorizontalDistanceSq(globalPosition, agent.Position);
			if (!(num2 >= num) && HasClearShot(ballista, agent))
			{
				Vec3 position = agent.Position;
				position.z += 1f;
				if (IsAimable(ballista, position))
				{
					num = num2;
					result = agent;
				}
			}
		}
		return result;
	}

	private static ItemObject? ResolveMissileItem(Ballista ballista)
	{
		ResolveReflection();
		try
		{
			if (_origMissileField?.GetValue(ballista) is ItemObject result)
			{
				return result;
			}
		}
		catch
		{
		}
		try
		{
			if (_missileIdField?.GetValue(ballista) is string text && !string.IsNullOrEmpty(text))
			{
				return Game.Current?.ObjectManager?.GetObject<ItemObject>(text);
			}
		}
		catch
		{
		}
		return null;
	}

	private static void ResolveReflection()
	{
		if (_reflectionResolved)
		{
			return;
		}
		_reflectionResolved = true;
		try
		{
			_origMissileField = AccessTools.Field(typeof(RangedSiegeWeapon), "OriginalMissileItem");
			_missileIdField = AccessTools.Field(typeof(RangedSiegeWeapon), "MissileItemID");
			TraceLogger.Write("HomesteadBallistaTurretController", $"Reflection resolved: origMissile={_origMissileField != null}, missileId={_missileIdField != null}.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBallistaTurretController", "ResolveReflection failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
