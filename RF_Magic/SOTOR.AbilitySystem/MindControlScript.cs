using System;
using System.Collections.Generic;
using System.Linq;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class MindControlScript : AbilityScript
{
	public const int MaxTargets = 10;

	private const string ConvertMarkParticle = "general_life_buff";

	private bool _done;

	protected override void OnAfterTick(float dt)
	{
		if (_done || base.CasterAgent == null || base.Ability == null || !base.HasTickedOnce)
		{
			return;
		}
		_done = true;
		try
		{
			DoMindControl();
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MindControlScript.DoMindControl failed: " + ex.Message);
		}
		finally
		{
			Stop();
		}
	}

	private void DoMindControl()
	{
		Mission current = Mission.Current;
		if (current == null)
		{
			return;
		}
		Agent casterAgent = base.CasterAgent;
		Team team = casterAgent.Team;
		if (team == null)
		{
			return;
		}
		Hero hero = casterAgent.GetHero();
		int num = ((casterAgent.Character == null) ? 1 : casterAgent.Character.Level);
		float num2 = ((base.Ability.Template.Radius > 0f) ? base.Ability.Template.Radius : 5f);
		Vec3 currentGlobalPosition = base.CurrentGlobalPosition;
		MBList<Agent> nearbyEnemyAgents = current.GetNearbyEnemyAgents(currentGlobalPosition.AsVec2, num2, team, new MBList<Agent>());
		SotorMindControlMissionLogic missionBehavior = current.GetMissionBehavior<SotorMindControlMissionLogic>();
		IEnumerable<Agent> enumerable = nearbyEnemyAgents.OrderBy((Agent _) => MBRandom.RandomFloat).Take(10);
		int num3 = 0;
		foreach (Agent item in enumerable)
		{
			if (item != null && item.IsActive() && !item.IsFadingOut())
			{
				num3++;
				int enemyLevel = ((item.Character != null) ? item.Character.Level : num);
				float enemyHpFraction = ((item.HealthLimit > 0f) ? (item.Health / item.HealthLimit) : 1f);
				float targetChance = SotorMindControlHelper.GetTargetChance(hero, num, enemyLevel, enemyHpFraction);
				if (MBRandom.RandomFloat < targetChance)
				{
					Convert(item, team, missionBehavior);
				}
			}
		}
		SotorLog.Info($"MindControl: cast by '{casterAgent.Name}' at {currentGlobalPosition}, {num3} target(s) rolled (radius {num2}).");
	}

	private void Convert(Agent target, Team casterTeam, SotorMindControlMissionLogic logic)
	{
		target.SetTeam(casterTeam, sync: false);
		logic?.OnAgentConverted(target);
		ApplyConvertMark(target);
	}

	private void ApplyConvertMark(Agent target)
	{
		try
		{
			MBAgentVisuals agentVisuals = target.AgentVisuals;
			Skeleton skeleton = agentVisuals?.GetSkeleton();
			if (skeleton == null || !skeleton.IsValid)
			{
				return;
			}
			Scene scene = Mission.Current?.Scene;
			if (scene == null)
			{
				return;
			}
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(scene);
			MatrixFrame boneLocalFrame = MatrixFrame.Identity;
			ParticleSystem particleSystem = ParticleSystem.CreateParticleSystemAttachedToEntity("general_life_buff", gameEntity, ref boneLocalFrame);
			if (particleSystem == null)
			{
				gameEntity.Remove(0);
				return;
			}
			agentVisuals.AddChildEntity(gameEntity);
			sbyte b = 1;
			if (b < skeleton.GetBoneCount())
			{
				skeleton.AddComponentToBone(b, particleSystem);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Debug("MindControlScript.ApplyConvertMark: " + ex.Message + " (particle not carved yet?).");
		}
	}
}
