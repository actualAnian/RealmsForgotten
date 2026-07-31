using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class MissileScript : AbilityScript
{
	private const float MinimumSweepDistanceSquared = 0.0001f;

	private const int WorldBodyFlags = 79617;

	private const float PierceSweepThickness = 1.5f;

	private readonly HashSet<int> _piercedAgents = new HashSet<int>();

	private float _pierceTravelled;

	private const float HeadshotMultiplier = 2f;

	private const float HeadZoneBelowEye = 0.25f;

	private float AgentSweepThickness
	{
		get
		{
			if (base.Ability == null || !(base.Ability.Template.Radius > 0.4f))
			{
				return 0.4f;
			}
			return base.Ability.Template.Radius;
		}
	}

	protected override void OnAfterTick(float dt)
	{
		if (!base.CanCollide || base.IsFading || base.Ability.Template.TriggerType != TriggerType.OnCollision)
		{
			return;
		}
		if (base.Ability.Template.Piercing)
		{
			PierceTick();
			return;
		}
		Vec3 lastFrameGlobalPosition = base.LastFrameGlobalPosition;
		Vec3 currentGlobalPosition = base.CurrentGlobalPosition;
		Vec3 vec = currentGlobalPosition - lastFrameGlobalPosition;
		if (vec.LengthSquared <= 0.0001f)
		{
			return;
		}
		Vec3 vec2 = vec.NormalizedCopy();
		float length = vec.Length;
		int excludedAgentIndex = ((base.CasterAgent.Health <= 0f) ? (-1) : base.CasterAgent.Index);
		Agent agent;
		float collisionDistance;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			agent = Mission.Current.RayCastForClosestAgent(lastFrameGlobalPosition, currentGlobalPosition, excludedAgentIndex, AgentSweepThickness, out collisionDistance);
		}
		if (agent != null)
		{
			Agent mountAgent = base.CasterAgent.MountAgent;
			if (mountAgent != null && agent.Index == mountAgent.Index)
			{
				agent = null;
				collisionDistance = float.MaxValue;
			}
		}
		if (agent == null)
		{
			collisionDistance = float.MaxValue;
		}
		float num = float.MaxValue;
		Vec3 vec3 = default(Vec3);
		bool flag;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			flag = Mission.Current.Scene.RayCastForClosestEntityOrTerrain(lastFrameGlobalPosition, currentGlobalPosition, out var collisionDistance2, out var closestPoint, out var _);
			if (flag)
			{
				num = collisionDistance2;
				vec3 = closestPoint;
			}
		}
		float crossDist = float.MaxValue;
		Vec3 crossPoint = default(Vec3);
		bool num2 = TryGetWaterCrossing(lastFrameGlobalPosition, currentGlobalPosition, out crossPoint, out crossDist);
		bool flag2 = collisionDistance <= length;
		bool flag3 = flag && num <= length;
		bool flag4 = num2 && crossDist <= length;
		if (flag2 || flag3 || flag4)
		{
			if (flag2 && (!flag3 || collisionDistance <= num) && (!flag4 || collisionDistance <= crossDist))
			{
				Vec3 vec4 = lastFrameGlobalPosition + vec2 * collisionDistance;
				SotorLog.Info($"MissileScript '{base.Ability.StringID}': agent hit '{agent.Name}' at {vec4} (agentDist={collisionDistance:0.0}, worldDist={num:0.0}, waterDist={crossDist:0.0}).");
				HandleCollision(vec4, -vec2);
			}
			else if (flag3 && (!flag4 || num <= crossDist))
			{
				Vec3 vec5 = ((vec3.IsValid && vec3.IsNonZero) ? vec3 : (lastFrameGlobalPosition + vec2 * num));
				SotorLog.Info($"MissileScript '{base.Ability.StringID}': world/terrain hit at {vec5} (worldDist={num:0.0}, agentDist={collisionDistance:0.0}, waterDist={crossDist:0.0}).");
				HandleCollision(vec5, -vec2);
			}
			else
			{
				SotorLog.Info($"MissileScript '{base.Ability.StringID}': water-surface hit at {crossPoint} (waterDist={crossDist:0.0}, agentDist={collisionDistance:0.0}, worldDist={num:0.0}).");
				HandleCollision(crossPoint, Vec3.Up);
			}
		}
	}

	private void PierceTick()
	{
		Vec3 lastFrameGlobalPosition = base.LastFrameGlobalPosition;
		Vec3 currentGlobalPosition = base.CurrentGlobalPosition;
		Vec3 vec = currentGlobalPosition - lastFrameGlobalPosition;
		if (vec.LengthSquared <= 0.0001f)
		{
			return;
		}
		Vec3 vec2 = vec.NormalizedCopy();
		float length = vec.Length;
		_pierceTravelled += length;
		float num = (base.Ability?.Template?.MaxDistance ?? 50f) + 10f;
		if (_pierceTravelled > num)
		{
			Stop();
			return;
		}
		float num2 = float.MaxValue;
		Vec3 vec3 = default(Vec3);
		bool flag;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			flag = Mission.Current.Scene.RayCastForClosestEntityOrTerrain(lastFrameGlobalPosition, currentGlobalPosition, out var collisionDistance, out var closestPoint, out var _);
			if (flag)
			{
				num2 = collisionDistance;
				vec3 = closestPoint;
			}
		}
		Vec3 crossPoint;
		float crossDist;
		bool flag2 = TryGetWaterCrossing(lastFrameGlobalPosition, currentGlobalPosition, out crossPoint, out crossDist);
		float num3 = length;
		if (flag && num2 < num3)
		{
			num3 = num2;
		}
		if (flag2 && crossDist < num3)
		{
			num3 = crossDist;
		}
		Vec3 targetPoint = lastFrameGlobalPosition + vec2 * num3;
		int num4 = 0;
		for (int i = 0; i < 32; i++)
		{
			int excludedAgentIndex = ((base.CasterAgent != null && base.CasterAgent.Health > 0f) ? base.CasterAgent.Index : (-1));
			Agent agent;
			float collisionDistance2;
			using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
			{
				agent = Mission.Current.RayCastForClosestAgent(lastFrameGlobalPosition, targetPoint, excludedAgentIndex, 1.5f, out collisionDistance2);
			}
			if (agent == null || _piercedAgents.Contains(agent.Index) || agent == base.CasterAgent || agent.IsFriendOf(base.CasterAgent) || !agent.IsActive())
			{
				break;
			}
			Vec3 hitPos = lastFrameGlobalPosition + vec2 * MBMath.ClampFloat(collisionDistance2, 0f, num3);
			TryPierceAgent(agent, hitPos, -vec2);
			num4++;
		}
		if (flag && num2 <= length && (!flag2 || num2 <= crossDist))
		{
			Vec3 vec4 = ((vec3.IsValid && vec3.IsNonZero) ? vec3 : (lastFrameGlobalPosition + vec2 * num2));
			SotorLog.Info($"MissileScript '{base.Ability.StringID}': pierce ended on world hit at {vec4} (pierced total={_piercedAgents.Count}).");
			Stop();
		}
		else if (flag2 && crossDist <= length)
		{
			SotorLog.Info($"MissileScript '{base.Ability.StringID}': pierce ended at water surface {crossPoint} (pierced total={_piercedAgents.Count}).");
			Stop();
		}
	}

	public void TryPierceAgent(Agent agent, Vec3 hitPos, Vec3 normal)
	{
		if (agent == null || !agent.IsActive() || agent == base.CasterAgent || agent.IsFriendOf(base.CasterAgent) || !_piercedAgents.Add(agent.Index))
		{
			return;
		}
		bool flag = false;
		try
		{
			float z = agent.GetEyeGlobalPosition().Z;
			if (hitPos.Z >= z - 0.25f)
			{
				flag = true;
			}
		}
		catch
		{
		}
		float damageMultiplier = (flag ? 2f : 1f);
		SotorLog.Info(string.Format("MissileScript '{0}': PIERCED '{1}'{2} at {3} (pierced so far={4}).", base.Ability.StringID, agent.Name, flag ? " HEADSHOT x2" : "", hitPos, _piercedAgents.Count));
		TriggerEffectsOnAgent(agent, hitPos, normal, damageMultiplier);
	}
}
