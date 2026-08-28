using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class SotorMindControlMissionLogic : MissionLogic
{
	private readonly List<Agent> _convertedAgents = new List<Agent>();

	private readonly Dictionary<CharacterObject, int> _recruitTally = new Dictionary<CharacterObject, int>();

	private readonly HashSet<Agent> _freezeFixRemoved = new HashSet<Agent>();

	private bool _freezeFixDone;

	public static readonly Dictionary<CharacterObject, int> PendingRecruits = new Dictionary<CharacterObject, int>();

	public bool HasConverts => _convertedAgents.Count > 0;

	public void OnAgentConverted(Agent agent)
	{
		if (agent != null)
		{
			if (!_convertedAgents.Contains(agent))
			{
				_convertedAgents.Add(agent);
			}
			if (!agent.IsHero && agent.Character is CharacterObject key)
			{
				_recruitTally.TryGetValue(key, out var value);
				_recruitTally[key] = value + 1;
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (!_freezeFixDone && _convertedAgents.Count != 0)
		{
			Mission current = Mission.Current;
			if (current != null && !current.MissionEnded && current.PlayerEnemyTeam != null && !AnyActiveTrueEnemy(current))
			{
				RemoveConvertsNonLethally();
				_freezeFixDone = true;
			}
		}
	}

	private bool AnyActiveTrueEnemy(Mission mission)
	{
		// Team com MBTeam nativo invalido faz IsEnemyOf estourar NRE por dentro
		// (missoes de conversa/loading) — validar os dois lados, idioma vanilla.
		if (mission.PlayerTeam == null || !mission.PlayerTeam.IsValid)
		{
			return false;
		}
		foreach (Team team in mission.Teams)
		{
			if (team == null || !team.IsValid || !team.IsEnemyOf(mission.PlayerTeam))
			{
				continue;
			}
			foreach (Agent activeAgent in team.ActiveAgents)
			{
				if (activeAgent != null && activeAgent.IsHuman)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void RemoveConvertsNonLethally()
	{
		foreach (Agent convertedAgent in _convertedAgents)
		{
			try
			{
				if (convertedAgent != null && convertedAgent.IsActive())
				{
					Blow blow = new Blow(convertedAgent.Index);
					blow.DamageType = DamageTypes.Invalid;
					blow.BaseMagnitude = 10000f;
					blow.GlobalPosition = convertedAgent.Position;
					blow.DamagedPercentage = 1f;
					Blow b = blow;
					_freezeFixRemoved.Add(convertedAgent);
					convertedAgent.Die(b, Agent.KillInfo.TeamSwitch);
				}
			}
			catch (Exception ex)
			{
				SotorLog.Warn("MindControl freeze-fix: Die(TeamSwitch) failed: " + ex.Message);
			}
		}
		SotorLog.Info($"MindControl freeze-fix: removed {_convertedAgents.Count} leftover convert(s) non-lethally to end the battle.");
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		if (_recruitTally.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<CharacterObject, int> item in _recruitTally)
		{
			CharacterObject key = item.Key;
			int num = CountSurvivors(key);
			if (key != null && num > 0)
			{
				PendingRecruits.TryGetValue(key, out var value);
				PendingRecruits[key] = value + num;
			}
		}
		SotorLog.Info($"MindControl: stashed {PendingRecruits.Count} troop type(s) of surviving converts for post-battle recruit.");
	}

	private int CountSurvivors(CharacterObject troop)
	{
		int num = 0;
		foreach (Agent convertedAgent in _convertedAgents)
		{
			if (convertedAgent != null && !convertedAgent.IsHero && convertedAgent.Character == troop && (_freezeFixRemoved.Contains(convertedAgent) || (convertedAgent.State != AgentState.Killed && convertedAgent.State != AgentState.Deleted)))
			{
				num++;
			}
		}
		return num;
	}
}
