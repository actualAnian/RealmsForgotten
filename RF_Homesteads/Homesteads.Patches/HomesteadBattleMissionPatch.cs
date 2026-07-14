using Homesteads.MissionLogics;
using Homesteads.Models;
using TaleWorlds.MountAndBlade;

namespace Homesteads.Patches;

internal static class HomesteadBattleMissionPatch
{
	internal static void TryAttachBattleSceneLogic(object? missionResult, string source)
	{
		if (!(missionResult is Mission mission))
		{
			TraceLogger.Write("HomesteadBattleMissionPatch", "Could not attach battle scene logic via " + source + " because mission result was '" + (missionResult?.GetType().FullName ?? "null") + "'.");
			return;
		}
		string text = null;
		try
		{
			text = mission.Scene?.GetName();
		}
		catch
		{
		}
		if (HomesteadMissionStateOpenNewPatch.IsArenaMissionName(text))
		{
			TraceLogger.Write("HomesteadBattleMissionPatch", "Skipped companion dog injection for arena/tournament scene '" + (text ?? "null") + "' via " + source + ".");
			return;
		}
		if (mission.GetMissionBehavior<HomesteadCompanionDogMissionLogic>() == null)
		{
			mission.AddMissionBehavior(new HomesteadCompanionDogMissionLogic());
		}
		Homestead homestead = HomesteadBattleContext.TryGetHomesteadForMissionAttach();
		if (homestead != null)
		{
			if (mission.GetMissionBehavior<HomesteadBattleSceneMissionLogic>() != null)
			{
				TraceLogger.Write("HomesteadBattleMissionPatch", $"Homestead battle scene logic already attached via {source} for '{homestead.Name}'.");
				HomesteadBattleContext.MarkMissionAttached(source);
				return;
			}
			mission.AddMissionBehavior(new HomesteadBattleSceneMissionLogic(homestead));
			mission.AddMissionBehavior(new HomesteadDogCombatLogic(homestead));
			HomesteadBattleContext.MarkMissionAttached(source);
			TraceLogger.Write("HomesteadBattleMissionPatch", $"Attached homestead battle scene logic via {source} for '{homestead.Name}'.");
		}
	}
}
