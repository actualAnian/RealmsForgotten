using System;
using System.Collections.Generic;
using SOTOR.AbilitySystem;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.MagicAccessories;

public sealed class MagicPowerRingMissionLogic : MissionLogic
{
	private sealed class TimedEffect
	{
		public Agent Source;
		public float NextTick;
		public int TicksRemaining;
	}

	private sealed class WaterState
	{
		public float EligibleAt = -1f;
		public float NextHealAt;
		public float Healed;
	}

	private static readonly Dictionary<Agent, TimedEffect> _fires = new Dictionary<Agent, TimedEffect>();
	private static readonly Dictionary<Agent, WaterState> _waterStates = new Dictionary<Agent, WaterState>();

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		MagicPowerRingCombat.ClearWinterStates();
		ClearStates();
	}

	protected override void OnEndMission()
	{
		MagicPowerRingCombat.ClearWinterStates();
		ClearStates();
		base.OnEndMission();
	}

	public override void OnAgentCreated(Agent agent)
	{
		agent?.UpdateAgentProperties();
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		agent?.UpdateAgentProperties();
		base.OnAgentBuild(agent, banner);
	}

	public override void OnAgentTeamChanged(Team previousTeam, Team newTeam, Agent agent)
	{
		agent?.UpdateAgentProperties();
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon,
		in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, in affectorWeapon, in blow, in attackCollisionData);
		try
		{
			if (affectedAgent == null || affectorAgent == null || affectedAgent == affectorAgent ||
				!affectedAgent.IsEnemyOf(affectorAgent))
			{
				return;
			}

			Hero hero = affectorAgent.GetHero();
			MagicAccessoryData ring = MagicAccessoryService.GetEquippedPowerRing(hero);
			if (ring == null)
			{
				return;
			}

			if (SotorDamageHelper.InSpellBlow || blow.InflictedDamage <= 0 || affectorWeapon.IsEmpty)
			{
				return;
			}
			float now = Mission?.CurrentTime ?? 0f;
			if (ring.Effect == MagicPowerRingEffect.Fire)
			{
				_fires[affectedAgent] = new TimedEffect { Source = affectorAgent, NextTick = now + 1f, TicksRemaining = 4 };
				MagicRuneCombatFeedback.Report(affectorAgent, MagicRuneEffect.Flame, "power_ring_fire",
					$"{ring.Name}: ignited target (4 damage/sec for 4s)", Colors.Red);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MagicPowerRingMissionLogic.OnAgentHit failed: " + ex.Message);
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		float now = Mission?.CurrentTime ?? 0f;
		TickFires(now);
		TickWater(now);
	}

	private static void TickFires(float now)
	{
		foreach (Agent target in new List<Agent>(_fires.Keys))
		{
			TimedEffect fire = _fires[target];
			if (target == null || !target.IsActive() || fire.TicksRemaining <= 0)
			{
				_fires.Remove(target);
				continue;
			}
			if (now >= fire.NextTick)
			{
				fire.NextTick = now + 1f;
				fire.TicksRemaining--;
				float actual = SotorDamageHelper.ApplyFireDamageOverTime(target, 4, fire.Source);
				if (actual > 0f)
				{
					SotorLog.Debug($"Power Ring Fire tick: source='{fire.Source?.Name}' target='{target.Name}' damage={actual:0}.");
				}
				if (fire.TicksRemaining == 0)
				{
					_fires.Remove(target);
				}
			}
		}
	}

	private static void TickWater(float now)
	{
		Mission mission = Mission.Current;
		if (mission == null)
		{
			return;
		}
		foreach (Agent agent in mission.Agents)
		{
			Hero hero = agent?.GetHero();
			if (hero == null || !agent.IsActive() || MagicAccessoryService.GetEquippedPowerRing(hero, MagicPowerRingEffect.Water) == null)
			{
				continue;
			}
			if (!_waterStates.TryGetValue(agent, out WaterState state))
			{
				state = new WaterState();
				_waterStates[agent] = state;
			}
			if (state.Healed >= 20f || agent.HealthLimit <= 0f)
			{
				continue;
			}
			if (agent.Health / agent.HealthLimit >= 0.6f)
			{
				state.EligibleAt = -1f;
				state.NextHealAt = 0f;
				continue;
			}
			if (state.EligibleAt < 0f)
			{
				state.EligibleAt = now;
				continue;
			}
			if (now - state.EligibleAt < 5f || now < state.NextHealAt)
			{
				continue;
			}
			state.NextHealAt = now + 5f;
			float before = agent.Health;
			agent.Health = Math.Min(agent.HealthLimit, agent.Health + Math.Min(2f, 20f - state.Healed));
			float healed = Math.Max(0f, agent.Health - before);
			state.Healed += healed;
			if (healed > 0f)
			{
				MagicRuneCombatFeedback.Report(agent, MagicRuneEffect.Vampiric, "power_ring_water",
					$"Ring of the Deep Tide: restored {healed:0} Health ({state.Healed:0}/20 this battle)", Colors.Cyan);
			}
		}
	}

	private static void ClearStates()
	{
		_fires.Clear();
		_waterStates.Clear();
	}
}
