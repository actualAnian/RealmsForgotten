using System;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public static class CommonAIDecisionFunctions
{
	public static Func<Target, float> WindsOfMagicRemainingRatio(Agent behaviorAgent)
	{
		HeroExtendedInfo info = behaviorAgent.GetHero()?.GetExtendedInfo();
		if (info == null)
		{
			// [RF-B] tropa nao tem HeroExtendedInfo: o ratio caia em 1.0 fixo e a IA
			// concluia "mana cheia" para sempre, insistindo em conjurar com o pool
			// vazio. Agora le o pool proprio da tropa; sem pool, 1.0 como antes.
			return (Target _) => SOTOR.RFIntegration.TroopWindsPool.AppliesTo(behaviorAgent)
				? SOTOR.RFIntegration.TroopWindsPool.GetRemainingRatio(behaviorAgent)
				: 1f;
		}
		return (Target _) => (info.MaxWindsOfMagic <= 0f) ? 1f : (info.WindsOfMagic / info.MaxWindsOfMagic);
	}

	public static Func<Target, float> FormationUnderFire()
	{
		return (Target target) => target.Formation.QuerySystem.UnderRangedAttackRatio;
	}

	public static Func<Target, float> TargetDistanceToHostiles(Team team = null)
	{
		return delegate(Target target)
		{
			if (team != null)
			{
				return target.TacticalPosition.Position.AsVec2.Distance(team.QuerySystem.AverageEnemyPosition);
			}
			if (target.Formation != null)
			{
				FormationQuerySystem cachedClosestEnemyFormation = target.Formation.CachedClosestEnemyFormation;
				if (cachedClosestEnemyFormation == null || cachedClosestEnemyFormation.Formation == null)
				{
					return float.MaxValue;
				}
				Vec3 positionPrioritizeCalculated = target.GetPositionPrioritizeCalculated();
				if (positionPrioritizeCalculated == Vec3.Invalid)
				{
					return float.MaxValue;
				}
				return positionPrioritizeCalculated.AsVec2.Distance(cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
			}
			return 0f;
		};
	}

	public static Func<Target, float> DistanceToTarget(Func<Vec3> provider)
	{
		return delegate(Target target)
		{
			Vec3 position = target.GetPosition();
			return (position == Vec3.Invalid) ? float.MaxValue : provider().Distance(position);
		};
	}

	public static Func<Target, float> FormationPower()
	{
		return (Target target) => target.Formation.QuerySystem.FormationPower;
	}

	public static Func<Target, float> RangedUnitRatio()
	{
		return (Target target) => target.Formation.QuerySystem.RangedUnitRatio;
	}

	public static Func<Target, float> CavalryUnitRatio()
	{
		return (Target target) => target.Formation.QuerySystem.CavalryUnitRatio;
	}

	public static Func<Target, float> Dispersedness()
	{
		return (Target target) => target.Formation.UnitSpacing;
	}

	public static Func<Target, float> TargetSpeed()
	{
		return (Target target) => target.Formation.CachedCurrentVelocity.Length;
	}

	public static Func<Target, float> BalanceOfPower(Agent agent)
	{
		return (Target _) => agent.Team.QuerySystem.TeamPower / (CalculateEnemyTotalPower(agent.Team) + agent.Team.QuerySystem.TeamPower);
	}

	public static float CalculateEnemyTotalPower(Team chosenTeam)
	{
		float num = 0f;
		foreach (Team item in Mission.Current.GetEnemyTeamsOf(chosenTeam))
		{
			num += item.QuerySystem.TeamPower;
		}
		return num;
	}

	public static float CalculateTeamTotalPower(Team chosenTeam)
	{
		return chosenTeam.QuerySystem.TeamPower;
	}
}
