using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

public static class DogAgentUtils
{
	public const float BiteRange = 2f;

	public const int BiteDamage = 10;

	public const float DogFollowSpeed = 9f;

	public const float WanderSpeed = 2.5f;

	public const float AggroRange = 6f;

	public const float FollowStopDistance = 2f;

	public const float AiPathFarThreshold = 5.5f;

	public const float AiPathNearThreshold = 3.5f;

	private static readonly Agent.AIScriptedFrameFlags ScriptedMoveFlags = Agent.AIScriptedFrameFlags.GoToPosition;

	private static readonly HashSet<Agent> _scriptedMoveUnsupported = new HashSet<Agent>();

	private static readonly Dictionary<Agent, (Vec3 lastPos, float elapsed)> _aiMoveWatchdog = new Dictionary<Agent, (Vec3, float)>();

	private const float AiWatchdogInterval = 0.5f;

	private const float AiWatchdogMinMoveSq = 0.04f;

	public static readonly ActionIndexCache ActionIdle = ActionIndexCache.Create("act_horse_stand_1");

	public static readonly ActionIndexCache ActionWalk = ActionIndexCache.Create("act_horse_forward_walk");

	public static readonly ActionIndexCache ActionTrot = ActionIndexCache.Create("act_horse_forward_trot");

	public static readonly ActionIndexCache ActionCanter = ActionIndexCache.Create("act_horse_forward_canter");

	public static readonly ActionIndexCache ActionGallop = ActionIndexCache.Create("act_horse_forward_gallop_right_foot");

	public static readonly ActionIndexCache ActionJump = ActionIndexCache.Create("act_horse_strike_front");

	public const float ZClimbThreshold = 0.25f;

	private const float GroundQueryRayHeight = 1f;

	private const float ZGuardSlop = 0.05f;

	private static readonly string[] BarkEventCandidates = new string[4] { "event:/mission/movement/foley/animals/dog/idle_2", "event:/mission/movement/foley/animals/dog/idle", "mission/movement/foley/animals/dog/idle_2", "mission/movement/foley/animals/dog/idle" };

	private static int _barkSoundId = -2;

	public static ActionIndexCache AnimForSpeed(float speed)
	{
		if (speed >= 6.2999997f)
		{
			return ActionGallop;
		}
		if (speed >= 4.5f)
		{
			return ActionCanter;
		}
		if (speed >= 3f)
		{
			return ActionTrot;
		}
		return ActionWalk;
	}

	public static void ResetPathingState()
	{
		_scriptedMoveUnsupported.Clear();
		_aiMoveWatchdog.Clear();
	}

	public static bool MoveToTarget(Agent dog, Vec3 target, float stopDistance, float dt, float speed, bool snapToGround)
	{
		return MoveToTarget(dog, target, stopDistance, dt, speed, snapToGround, allowAiPathing: true);
	}

	public static bool MoveToTarget(Agent dog, Vec3 target, float stopDistance, float dt, float speed, bool snapToGround, bool allowAiPathing)
	{
		Vec3 vec = target - dog.Position;
		vec.z = 0f;
		float length = vec.Length;
		bool flag = allowAiPathing && (length > 5.5f || (!(length < 3.5f) && dog.Controller == AgentControllerType.AI));
		if (flag && !_scriptedMoveUnsupported.Contains(dog))
		{
			if (dog.Controller != AgentControllerType.AI)
			{
				dog.Controller = AgentControllerType.AI;
			}
			dog.SetMaximumSpeedLimit(speed, isMultiplier: false);
			bool flag2 = false;
			try
			{
				flag2 = dog.CanBeAssignedForScriptedMovement();
			}
			catch (Exception ex)
			{
				TraceLogger.Write("DogAgentUtils", "MoveToTarget: CanBeAssignedForScriptedMovement threw " + ex.GetType().Name + ". Suppressing and falling back to manual movement. Exception: " + ex.Message);
				flag2 = false;
			}
			if (flag2)
			{
				WorldPosition position = new WorldPosition(Mission.Current.Scene, target);
				dog.SetScriptedPosition(ref position, addHumanLikeDelay: false, ScriptedMoveFlags);
				GuardZ(dog, target.z, snapToGround);
				if (!CheckAiMoveWatchdog(dog, dt))
				{
					return length > stopDistance;
				}
			}
			else
			{
				_scriptedMoveUnsupported.Add(dog);
			}
		}
		_aiMoveWatchdog.Remove(dog);
		if (dog.Controller != AgentControllerType.None)
		{
			dog.DisableScriptedMovement();
			dog.Controller = AgentControllerType.None;
		}
		return StepToward(dog, target, stopDistance, dt, speed, snapToGround);
	}

	public static bool StepToward(Agent dog, Vec3 target, float stopDistance, float dt)
	{
		return StepToward(dog, target, stopDistance, dt, 9f, snapToGround: true);
	}

	public static bool StepToward(Agent dog, Vec3 target, float stopDistance, float dt, float speed)
	{
		return StepToward(dog, target, stopDistance, dt, speed, snapToGround: true);
	}

	public static bool StepToward(Agent dog, Vec3 target, float stopDistance, float dt, float speed, bool snapToGround)
	{
		Vec3 position = dog.Position;
		Vec3 vec = target - position;
		vec.z = 0f;
		float length = vec.Length;
		bool num = length > stopDistance;
		Vec3 targetDirection;
		if (num)
		{
			targetDirection = ((length > 0.01f) ? (vec * (1f / length)) : new Vec3(Vec2.Forward.x, Vec2.Forward.y));
		}
		else
		{
			targetDirection = new Vec3(dog.LookDirection.x, dog.LookDirection.y);
			if (targetDirection.LengthSquared < 0.01f)
			{
				targetDirection = new Vec3(0f, 1f);
			}
			else
			{
				targetDirection.Normalize();
			}
		}
		if (num)
		{
			float num2 = Math.Min(speed * dt, length - stopDistance);
			Vec3 vec2 = position + targetDirection * num2;
			if (snapToGround)
			{
				Vec3 position2 = vec2;
				position2.z = Math.Max(position.z, target.z) + 1f;
				float num3 = Mission.Current.Scene.GetGroundHeightAtPosition(position2) - position.z;
				float num4 = ((Math.Abs(num3) > 0.5f) ? 1f : ((num3 > 0f) ? 1f : Math.Min(1f, 8f * dt)));
				vec2.z = position.z + num3 * num4;
			}
			else
			{
				Vec3 position3 = vec2;
				position3.z = Math.Max(position.z, target.z) + 1f;
				float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(position3);
				float num5 = Math.Max(groundHeightAtPosition, target.z);
				float num6 = ((length > 0.01f) ? (num2 / length) : 1f);
				float val = position.z + (num5 - position.z) * num6;
				vec2.z = Math.Max(groundHeightAtPosition, val);
			}
			dog.TeleportToPosition(vec2);
		}
		if (num)
		{
			dog.SetTargetPositionAndDirection((dog.Position + targetDirection).AsVec2, in targetDirection);
		}
		return num;
	}

	private static bool CheckAiMoveWatchdog(Agent dog, float dt)
	{
		if (!_aiMoveWatchdog.TryGetValue(dog, out (Vec3, float) value))
		{
			_aiMoveWatchdog[dog] = (dog.Position, dt);
			return false;
		}
		float num = value.Item2 + dt;
		if (num < 0.5f)
		{
			_aiMoveWatchdog[dog] = (value.Item1, num);
			return false;
		}
		if ((dog.Position - value.Item1).LengthSquared < 0.04f)
		{
			_scriptedMoveUnsupported.Add(dog);
			_aiMoveWatchdog.Remove(dog);
			TraceLogger.Write("DogAgentUtils", $"AI-move watchdog: dog#{dog.Index} stuck in AI controller — switching to manual stepping.");
			return true;
		}
		_aiMoveWatchdog[dog] = (dog.Position, 0f);
		return false;
	}

	private static void GuardZ(Agent dog, float targetZ, bool snapToGround)
	{
		try
		{
			Vec3 position = dog.Position;
			float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(position);
			float num = (snapToGround ? groundHeightAtPosition : Math.Max(groundHeightAtPosition, targetZ));
			if (position.z < num - 0.05f)
			{
				position.z = num;
				dog.TeleportToPosition(position);
			}
		}
		catch
		{
		}
	}

	private static int ResolveBarkSoundId()
	{
		if (_barkSoundId != -2)
		{
			return _barkSoundId;
		}
		_barkSoundId = -1;
		string[] barkEventCandidates = BarkEventCandidates;
		for (int i = 0; i < barkEventCandidates.Length; i++)
		{
			int eventIdFromString = SoundEvent.GetEventIdFromString(barkEventCandidates[i]);
			if (eventIdFromString != -1)
			{
				_barkSoundId = eventIdFromString;
				break;
			}
		}
		if (_barkSoundId == -1)
		{
			TraceLogger.Write("DogAgentUtils", "PlayBark: no dog bark sound event resolved.");
		}
		return _barkSoundId;
	}

	public static void PlayBark(Agent dog)
	{
		if (dog == null || !dog.IsActive() || Mission.Current == null)
		{
			return;
		}
		int num = ResolveBarkSoundId();
		if (num < 0)
		{
			return;
		}
		try
		{
			Mission.Current.MakeSound(num, dog.Position, soundCanBePredicted: false, isReliable: true, dog.Index, -1);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("DogAgentUtils", "PlayBark threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public static void BiteEnemy(Agent dog, Agent enemy)
	{
		BiteEnemy(dog, enemy, 10);
	}

	public static void BiteEnemy(Agent dog, Agent enemy, int damage)
	{
		if (enemy.Health <= 0f)
		{
			return;
		}
		try
		{
			Agent mainAgent = Mission.Current.MainAgent;
			if (mainAgent == null || !mainAgent.IsActive())
			{
				return;
			}
			Blow blow = new Blow(mainAgent.Index);
			blow.DamageType = DamageTypes.Pierce;
			if (HomesteadBehavior.Instance != null && HomesteadBehavior.Instance.HasHoundmasterKnockdownUnlocked)
			{
				blow.BlowFlag = BlowFlags.KnockDown;
				if (enemy.HasMount)
				{
					blow.BlowFlag |= BlowFlags.CanDismount;
				}
				blow.BaseMagnitude = 1000f;
			}
			else
			{
				blow.BlowFlag = BlowFlags.None;
				blow.BaseMagnitude = damage;
			}
			if (enemy.HasMount)
			{
				dog.SetActionChannel(0, in ActionJump, ignorePriority: true, (AnimFlags)0uL);
			}
			blow.InflictedDamage = damage;
			blow.GlobalPosition = enemy.Position;
			blow.Direction = (enemy.Position - dog.Position).NormalizedCopy();
			enemy.RegisterBlow(blow, default(AttackCollisionData));
			SoundEvent.PlaySound2D("event:/mission/combat/hit/flesh_flesh");
			PlayBark(dog);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("DogAgentUtils", "BiteEnemy threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public static Agent? FindNearestEnemy(Agent dog, float aggroRange, out float nearestDistSq)
	{
		Agent result = null;
		nearestDistSq = aggroRange * aggroRange;
		if (dog == null || !dog.IsActive())
		{
			return null;
		}
		Team team = Mission.Current?.PlayerTeam;
		if (team == null)
		{
			return null;
		}
		Agent agent = Mission.Current?.MainAgent;
		List<Agent> list;
		try
		{
			list = new List<Agent>(Mission.Current.Agents);
		}
		catch
		{
			return null;
		}
		foreach (Agent item in list)
		{
			try
			{
				if (item != null && item.IsActive() && item.IsHuman && item != agent && IsHostileToPlayer(item, team, agent))
				{
					float num = dog.Position.DistanceSquared(item.Position);
					if (num < nearestDistSq)
					{
						nearestDistSq = num;
						result = item;
					}
				}
			}
			catch
			{
			}
		}
		return result;
	}

	private static bool IsHostileToPlayer(Agent enemy, Team playerTeam, Agent? mainAgent)
	{
		try
		{
			if (enemy.Team != null && enemy.Team.IsEnemyOf(playerTeam))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (mainAgent != null && enemy != mainAgent && mainAgent.IsEnemyOf(enemy))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}
}
