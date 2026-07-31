using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SOTOR.AbilitySystem.TriggeredScripts;

public static class SummonHelper
{
	private const int AgentSafetyBuffer = 150;

	private const int MinSlotsForSummoning = 5;

	private static ActionIndexCache? _actRaiseFromGround;

	private static bool _riseAnimResolved;

	private static ActionIndexCache RiseFromGroundAction
	{
		get
		{
			if (!_riseAnimResolved)
			{
				try
				{
					_actRaiseFromGround = ActionIndexCache.Create("act_raisefromground");
				}
				catch
				{
					_actRaiseFromGround = null;
				}
				_riseAnimResolved = true;
			}
			return _actRaiseFromGround ?? ActionIndexCache.act_none;
		}
	}

	private static int GetCurrentAgentCount()
	{
		if (Mission.Current != null)
		{
			return Mission.Current.AllAgents.Count;
		}
		return 0;
	}

	private static int GetMissionAgentLimit()
	{
		return Math.Max(0, DefaultBattleMissionAgentSpawnLogic.MaxNumberOfAgentsForMission - 150); // [RF-A] 1.4.7: classe concreta renomeada
	}

	public static int GetAvailableSummonSlots()
	{
		return Math.Max(0, GetMissionAgentLimit() - GetCurrentAgentCount());
	}

	public static bool CanSummon()
	{
		return GetAvailableSummonSlots() >= 5;
	}

	public static int GetClampedSummonCount(int desiredCount)
	{
		return Math.Min(desiredCount, GetAvailableSummonSlots());
	}

	public static AgentBuildData GetAgentBuildData(Agent caster, string summonedUnitId)
	{
		BasicCharacterObject basicCharacterObject = MBObjectManager.Instance.GetObject<BasicCharacterObject>(summonedUnitId);
		if (basicCharacterObject == null)
		{
			SotorLog.Warn("SummonHelper: troop '" + summonedUnitId + "' not found (troop XML not loaded?).");
			return null;
		}
		Team team = caster.Team;
		Formation formation = team?.GetFormation(FormationClass.Infantry) ?? caster.Formation;
		BasicBattleAgentOrigin troopOrigin = new BasicBattleAgentOrigin(basicCharacterObject);
		Vec2 direction = Vec2.Forward;
		return new AgentBuildData(basicCharacterObject).Team(team).Formation(formation).ClothingColor1((uint)(((int?)team?.Color) ?? (-1)))
			.ClothingColor2((uint)(((int?)team?.Color2) ?? (-1)))
			.Equipment(basicCharacterObject.RandomBattleEquipment)
			.TroopOrigin(troopOrigin)
			.IsReinforcement(isReinforcement: true)
			.InitialDirection(in direction);
	}

	public static Agent SpawnAgent(AgentBuildData buildData, Vec3 position, bool withAnimation)
	{
		Vec3 position2 = position;
		if (Mission.Current.Scene.GetNavigationMeshForPosition(in position) == UIntPtr.Zero)
		{
			position2 = Mission.Current.GetRandomPositionAroundPoint(position, 0.05f, 5f, nearFirst: true);
		}
		if (!position2.IsValid || !position2.IsNonZero || Mission.Current.Scene.GetNavigationMeshForPosition(in position2) == UIntPtr.Zero)
		{
			SotorLog.Warn($"SummonHelper.SpawnAgent: no valid navmesh at {position} (nudged {position2}); spawning nothing.");
			return null;
		}
		buildData = buildData.InitialPosition(in position2);
		Agent agent = Mission.Current.SpawnAgent(buildData);
		if (agent == null)
		{
			return null;
		}
		agent.FadeIn();
		agent.WieldInitialWeapons();
		agent.SetWatchState(Agent.WatchState.Alarmed);
		if (withAnimation)
		{
			ActionIndexCache actionIndexCache = RiseFromGroundAction;
			if (actionIndexCache.Index != ActionIndexCache.act_none.Index)
			{
				agent.SetActionChannel(0, in actionIndexCache, ignorePriority: false, (AnimFlags)0uL);
				agent.SetCurrentActionProgress(0, 0f);
				agent.SetCurrentActionSpeed(0, 1f);
			}
		}
		return agent;
	}
}
