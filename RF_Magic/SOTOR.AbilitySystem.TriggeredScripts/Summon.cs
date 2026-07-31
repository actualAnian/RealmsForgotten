using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.TriggeredScripts;

public class Summon : ITriggeredScript
{
	public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration, TriggeredEffectTemplate template, string originSpell)
	{
		if (triggeredByAgent == null || !triggeredByAgent.IsActive() || triggeredByAgent.Team == null || template == null || string.IsNullOrWhiteSpace(template.TroopIdToSummon) || template.TroopIdToSummon.Equals("none", StringComparison.OrdinalIgnoreCase) || template.NumberToSummon <= 0)
		{
			return;
		}
		if (!IsSummonPointValid(position))
		{
			if (triggeredByAgent == Agent.Main)
			{
				InformationManager.DisplayMessage(new InformationMessage("You cannot raise the dead over open water.", Colors.Red));
			}
			SotorLog.Info($"Summon '{originSpell}': BLOCKED — cast point {position} is off-navmesh (open water?); no skeletons spawned.");
			return;
		}
		int numberToSummon = template.NumberToSummon;
		Vec3 vec = position;
		for (int i = 0; i < numberToSummon; i++)
		{
			AgentBuildData agentBuildData = SummonHelper.GetAgentBuildData(triggeredByAgent, template.TroopIdToSummon);
			if (agentBuildData == null)
			{
				SotorLog.Warn("Summon: troop '" + template.TroopIdToSummon + "' not found; aborting summon.");
				break;
			}
			vec = Mission.Current.GetRandomPositionAroundPoint(vec, 0.1f, 0.6f);
			try
			{
				Agent agent = SummonHelper.SpawnAgent(agentBuildData, vec, withAnimation: true);
				if (agent != null)
				{
					SotorSummonNavalGuardMissionLogic.EnqueueSummonedAgent(agent);
				}
			}
			catch (Exception ex)
			{
				SotorLog.Warn($"Summon: spawn {i + 1}/{numberToSummon} of '{template.TroopIdToSummon}' failed ({ex.GetType().Name}): {ex.Message}");
			}
		}
	}

	private static bool IsSummonPointValid(Vec3 position)
	{
		try
		{
			Scene scene = Mission.Current?.Scene;
			if (scene == null)
			{
				return false;
			}
			if (scene.GetNavigationMeshForPosition(in position) != UIntPtr.Zero)
			{
				return true;
			}
			Vec3 position2 = Mission.Current.GetRandomPositionAroundPoint(position, 0.05f, 5f, nearFirst: true);
			if (!position2.IsValid || !position2.IsNonZero)
			{
				return false;
			}
			return scene.GetNavigationMeshForPosition(in position2) != UIntPtr.Zero;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("Summon.IsSummonPointValid failed: " + ex.Message);
			return false;
		}
	}
}
