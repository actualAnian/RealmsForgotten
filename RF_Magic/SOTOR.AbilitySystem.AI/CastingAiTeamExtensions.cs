using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public static class CastingAiTeamExtensions
{
	public static List<Team> GetEnemyTeams(this Team team)
	{
		return Mission.Current.Teams.Where((Team x) => x.IsEnemyOf(team)).ToList();
	}

	public static List<Team> GetAllyTeams(this Team team)
	{
		return Mission.Current.Teams.Where((Team x) => x.IsFriendOf(team)).ToList();
	}

	public static List<Formation> GetFormations(this Team team)
	{
		return team.FormationsIncludingEmpty.ToList().FindAll((Formation form) => form.CountOfUnits > 0);
	}

	public static List<Team> GetEnemyTeamsOf(this Mission mission, Team team)
	{
		return mission.Teams.Where((Team x) => x.IsEnemyOf(team)).ToList();
	}
}
