using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace SOTOR.AbilitySystem;

public class SotorUndeadMoraleMissionLogic : MissionLogic
{
	private const string SkeletonCultureId = "sotor_skeleton";

	private const float CrumbleThreshold = 15f;

	private const float TickInterval = 0.5f;

	private const int CrumbleDamage = 9999;

	private const float OverboardSinkAccel = 60f;

	private float _timeElapsed;

	private static bool IsSkeleton(Agent agent)
	{
		BasicCultureObject basicCultureObject = agent?.Character?.Culture;
		if (basicCultureObject != null)
		{
			return basicCultureObject.StringId == "sotor_skeleton";
		}
		return false;
	}

	public override void OnMissionTick(float dt)
	{
		_timeElapsed += dt;
		if (_timeElapsed < 0.5f)
		{
			return;
		}
		_timeElapsed = 0f;
		if (Mission.Current == null)
		{
			return;
		}
		AgentReadOnlyList agents = Mission.Current.Agents;
		for (int i = 0; i < agents.Count; i++)
		{
			Agent agent = agents[i];
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.Health < 1f || !IsSkeleton(agent))
			{
				continue;
			}
			if (agent.IsInWater())
			{
				try
				{
					agent.AddAcceleration(new Vec3(0f, 0f, -60f));
					SotorDamageHelper.ApplyDamageOverTime(agent, 9999, agent);
				}
				catch (Exception ex)
				{
					SotorLog.Warn("UndeadMorale overboard-sink failed: " + ex.Message);
				}
				continue;
			}
			CommonAIComponent commonAIComponent = agent.CommonAIComponent;
			if (commonAIComponent != null && commonAIComponent.Morale < 15f)
			{
				try
				{
					SotorDamageHelper.ApplyDamageOverTime(agent, 9999, agent);
				}
				catch (Exception ex2)
				{
					SotorLog.Warn("UndeadMorale crumble failed: " + ex2.Message);
				}
			}
		}
	}
}
