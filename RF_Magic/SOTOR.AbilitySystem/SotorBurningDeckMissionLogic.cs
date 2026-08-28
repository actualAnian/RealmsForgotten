using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class SotorBurningDeckMissionLogic : MissionLogic
{
	private const float TickInterval = 1f;

	private float _timeElapsed;

	private readonly List<int> _ablazeDeckAgents = new List<int>(64);

	public override void OnMissionTick(float dt)
	{
		_timeElapsed += dt;
		if (_timeElapsed < 1f)
		{
			return;
		}
		float timeElapsed = _timeElapsed;
		_timeElapsed = 0f;
		if (!SotorSettings.EnableBurningDeckDamage)
		{
			return;
		}
		Mission current = Mission.Current;
		if (!SotorNavalBridge.IsNavalMission(current) || current.MissionEnded || current.IsMissionEnding)
		{
			return;
		}
		int damageAmount = (int)Math.Max(1f, SotorSettings.BurningDeckDamagePerSecond * timeElapsed);
		_ablazeDeckAgents.Clear();
		SotorNavalBridge.CollectAgentsOnAblazeDecks(current, _ablazeDeckAgents);
		if (_ablazeDeckAgents.Count == 0)
		{
			return;
		}
		for (int i = 0; i < _ablazeDeckAgents.Count; i++)
		{
			Agent agent = current.FindAgentWithIndex(_ablazeDeckAgents[i]);
			if (agent != null && agent.IsHuman && agent.IsActive() && !(agent.Health < 1f))
			{
				try
				{
					SotorDamageHelper.ApplyFireDamageOverTime(agent, damageAmount, agent);
				}
				catch (Exception ex)
				{
					SotorLog.Warn("BurningDeck DoT failed on '" + agent.Name + "': " + ex.Message);
				}
			}
		}
	}
}
