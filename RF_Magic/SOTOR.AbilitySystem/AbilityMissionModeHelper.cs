using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

internal static class AbilityMissionModeHelper
{
	public static bool IsAbilityHudMissionMode(Mission mission)
	{
		if (mission == null)
		{
			return false;
		}
		int mode = (int)mission.Mode;
		if (mode != 2)
		{
			return mode == 4;
		}
		return true;
	}

	public static bool IsBattleAbilityContext(Mission mission)
	{
		if (mission == null)
		{
			return false;
		}
		if (IsAbilityHudMissionMode(mission))
		{
			return true;
		}
		if (!mission.IsFriendlyMission && mission.CombatType != Mission.MissionCombatType.ArenaCombat)
		{
			return mission.CombatType != Mission.MissionCombatType.NoCombat;
		}
		return false;
	}
}
