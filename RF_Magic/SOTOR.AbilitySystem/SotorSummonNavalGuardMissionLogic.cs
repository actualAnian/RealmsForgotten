using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class SotorSummonNavalGuardMissionLogic : MissionLogic
{
	private struct Pending
	{
		public Agent Agent;

		public float CheckAtTime;
	}

	private const float BindGraceSeconds = 1.5f;

	private static readonly List<Pending> _pending = new List<Pending>(16);

	public static void EnqueueSummonedAgent(Agent agent)
	{
		try
		{
			Mission current = Mission.Current;
			if (agent != null && current != null && SotorNavalBridge.IsNavalMission(current))
			{
				_pending.Add(new Pending
				{
					Agent = agent,
					CheckAtTime = current.CurrentTime + 1.5f
				});
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SummonNavalGuard.Enqueue failed: " + ex.Message);
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (SotorNavalBridge.IsNavalMission(Mission.Current))
		{
			SotorNavalMarinerXpPatch.ApplyIfNeeded();
		}
		if (_pending.Count == 0)
		{
			return;
		}
		Mission current = Mission.Current;
		if (current == null || current.MissionEnded || current.IsMissionEnding)
		{
			_pending.Clear();
			return;
		}
		float currentTime = current.CurrentTime;
		for (int num = _pending.Count - 1; num >= 0; num--)
		{
			Pending pending = _pending[num];
			if (!(currentTime < pending.CheckAtTime))
			{
				_pending.RemoveAt(num);
				Agent agent = pending.Agent;
				if (agent != null && agent.IsActive() && SotorNavalBridge.GetSummonedAgentNavalBinding(agent) == 2)
				{
					SotorLog.Warn("Summon naval guard: '" + agent.Name + "' never bound to a ship in a naval battle -> removing (would crash Mission.Tick).");
					try
					{
						agent.FadeOut(hideInstantly: true, hideMount: true);
					}
					catch
					{
					}
				}
			}
		}
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		_pending.Clear();
	}
}
