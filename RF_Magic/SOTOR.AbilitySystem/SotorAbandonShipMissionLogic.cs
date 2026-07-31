using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace SOTOR.AbilitySystem;

public class SotorAbandonShipMissionLogic : MissionLogic
{
	private sealed class FleeState
	{
		public int TargetShipIndex;

		public Vec3 TargetPoint;

		public int Quality;

		public bool NeedsSwim;

		public Vec3 WaterEntryPoint;

		public bool InWaterPhase;

		public float NextScreamTime;

		public Vec3 LastPos;

		public int StuckSweeps;

		public bool Jumped;
	}

	private const float SweepInterval = 0.75f;

	private const string SkeletonCultureId = "sotor_skeleton";

	private float _timeElapsed;

	private const float ScreamCooldownMin = 6f;

	private const float ScreamCooldownMax = 9f;

	private readonly Dictionary<int, FleeState> _fleeing = new Dictionary<int, FleeState>();

	private readonly List<int> _pruneScratch = new List<int>(64);

	private static bool IsSkeleton(Agent agent)
	{
		BasicCultureObject basicCultureObject = agent?.Character?.Culture;
		if (basicCultureObject != null)
		{
			return basicCultureObject.StringId == "sotor_skeleton";
		}
		return false;
	}

	public override void OnMissionTick(float dt)
	{
		_timeElapsed += dt;
		if (_timeElapsed < 0.75f)
		{
			return;
		}
		_timeElapsed = 0f;
		if (!SotorSettings.EnableAbandonShipAI)
		{
			if (_fleeing.Count > 0)
			{
				_fleeing.Clear();
			}
			return;
		}
		Mission current = Mission.Current;
		if (!SotorNavalBridge.IsNavalMission(current))
		{
			return;
		}
		if (current.MissionEnded || current.IsMissionEnding)
		{
			if (_fleeing.Count <= 0)
			{
				return;
			}
			foreach (int key in _fleeing.Keys)
			{
				Agent agent = current.FindAgentWithIndex(key);
				if (agent != null && agent.IsActive())
				{
					Release(agent, _fleeing[key]);
				}
			}
			_fleeing.Clear();
			return;
		}
		_pruneScratch.Clear();
		foreach (int key2 in _fleeing.Keys)
		{
			Agent agent2 = current.FindAgentWithIndex(key2);
			if (agent2 == null || !agent2.IsActive() || !agent2.IsHuman || agent2.Health < 1f)
			{
				_pruneScratch.Add(key2);
			}
		}
		for (int i = 0; i < _pruneScratch.Count; i++)
		{
			_fleeing.Remove(_pruneScratch[i]);
		}
		AgentReadOnlyList agents = current.Agents;
		for (int j = 0; j < agents.Count; j++)
		{
			Agent agent3 = agents[j];
			if (agent3 == null || !agent3.IsHuman)
			{
				continue;
			}
			FleeState value;
			bool flag = _fleeing.TryGetValue(agent3.Index, out value);
			if (!agent3.IsActive() || agent3.Health < 1f)
			{
				if (flag)
				{
					_fleeing.Remove(agent3.Index);
				}
			}
			else
			{
				if (IsSkeleton(agent3))
				{
					continue;
				}
				Vec3 deckPoint;
				int targetShipIndex;
				int quality;
				bool needsSwim;
				Vec3 waterEntryPoint;
				bool flag2 = SotorNavalBridge.TryGetEscapeTarget(agent3, out deckPoint, out targetShipIndex, out quality, out needsSwim, out waterEntryPoint);
				float currentTime = current.CurrentTime;
				if (!flag)
				{
					if (flag2)
					{
						FleeState fleeState = new FleeState
						{
							TargetShipIndex = targetShipIndex,
							TargetPoint = deckPoint,
							Quality = quality,
							NeedsSwim = needsSwim,
							WaterEntryPoint = waterEntryPoint,
							InWaterPhase = false
						};
						if (DriveFor(agent3, fleeState))
						{
							fleeState.NextScreamTime = currentTime + MBRandom.RandomFloatRanged(0f, 2.5f);
							_fleeing[agent3.Index] = fleeState;
						}
					}
					continue;
				}
				if (SotorNavalBridge.IsAgentOnSafeShip(agent3))
				{
					Release(agent3, value);
					_fleeing.Remove(agent3.Index);
					continue;
				}
				if (!flag2)
				{
					Release(agent3, value);
					_fleeing.Remove(agent3.Index);
					continue;
				}
				if (value.NeedsSwim && !value.InWaterPhase && agent3.IsInWater())
				{
					value.InWaterPhase = true;
					try
					{
						agent3.StopRetreating();
					}
					catch
					{
					}
					DriveFor(agent3, value);
				}
				else if (!value.InWaterPhase && !agent3.IsInWater())
				{
					float num = agent3.Position.Distance(value.LastPos);
					if (value.LastPos != Vec3.Zero && num < 1f)
					{
						value.StuckSweeps++;
						if (value.StuckSweeps >= 4)
						{
							value.StuckSweeps = 0;
							if (value.NeedsSwim)
							{
								value.Jumped = false;
								DriveFor(agent3, value);
							}
							else
							{
								value.NeedsSwim = true;
								value.Jumped = false;
								DriveFor(agent3, value);
							}
						}
					}
					else
					{
						value.StuckSweeps = 0;
					}
					value.LastPos = agent3.Position;
				}
				if (SotorNavalBridge.IsShipIndexDoomed(value.TargetShipIndex))
				{
					try
					{
						if (agent3.IsUsingGameObject)
						{
							agent3.StopUsingGameObject();
						}
					}
					catch
					{
					}
					if (flag2 && targetShipIndex != value.TargetShipIndex)
					{
						value.TargetShipIndex = targetShipIndex;
						value.TargetPoint = deckPoint;
						value.Quality = quality;
						value.NeedsSwim = needsSwim;
						value.WaterEntryPoint = waterEntryPoint;
						DriveFor(agent3, value);
					}
					else if (!flag2)
					{
						Release(agent3, value);
						_fleeing.Remove(agent3.Index);
					}
					continue;
				}
				if (value.NeedsSwim && value.InWaterPhase)
				{
					DriveFor(agent3, value);
				}
				bool isUsingGameObject = agent3.IsUsingGameObject;
				bool flag3 = quality > value.Quality;
				bool flag4 = targetShipIndex != value.TargetShipIndex && quality >= value.Quality;
				if (!isUsingGameObject && (flag3 || flag4))
				{
					value.TargetShipIndex = targetShipIndex;
					value.TargetPoint = deckPoint;
					value.Quality = quality;
					value.NeedsSwim = needsSwim;
					value.WaterEntryPoint = waterEntryPoint;
					if (!agent3.IsInWater())
					{
						value.InWaterPhase = false;
					}
					DriveFor(agent3, value);
				}
				if (currentTime >= value.NextScreamTime)
				{
					ScreamInPanic(agent3, value, currentTime);
				}
			}
		}
	}

	private bool DriveFor(Agent agent, FleeState st)
	{
		try
		{
			if (st.NeedsSwim)
			{
				if (!st.InWaterPhase)
				{
					if (st.Jumped)
					{
						return true;
					}
					st.Jumped = SotorNavalBridge.MakeAgentJumpOverboard(agent, st.TargetPoint);
					return st.Jumped;
				}
				if (SotorNavalBridge.TryBoardViaClimbingNet(agent, st.TargetShipIndex))
				{
					return true;
				}
				agent.SetTargetPosition(st.TargetPoint.AsVec2);
				return true;
			}
			agent.SetTargetPosition(st.TargetPoint.AsVec2);
			return true;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("AbandonShip.DriveFor failed for '" + agent?.Name + "': " + ex.Message);
			return false;
		}
	}

	private void Release(Agent agent, FleeState st)
	{
		try
		{
			agent.StopRetreating();
			agent.ClearTargetFrame();
		}
		catch (Exception ex)
		{
			SotorLog.Warn("AbandonShip.Release failed for '" + agent?.Name + "': " + ex.Message);
		}
	}

	private static void ScreamInPanic(Agent a, FleeState st, float now)
	{
		try
		{
			a.MakeVoice(SkinVoiceManager.VoiceType.Fear, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
			st.NextScreamTime = now + MBRandom.RandomFloatRanged(6f, 9f);
		}
		catch (Exception ex)
		{
			SotorLog.Warn("AbandonShip.ScreamInPanic failed for '" + a?.Name + "': " + ex.Message);
		}
	}

	protected override void OnEndMission()
	{
		_fleeing.Clear();
	}
}
