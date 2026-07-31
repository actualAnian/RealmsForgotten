using System.Collections.Generic;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace SOTOR.AbilitySystem.StatusEffects;

public class StatusEffectMissionLogic : MissionLogic
{
	private const float PermanentAuraDuration = 100000f;

	private readonly HashSet<Agent> _captainAurasApplied = new HashSet<Agent>();

	public override void OnAgentCreated(Agent agent)
	{
		if (agent != null && agent.IsHuman)
		{
			agent.AddComponent(new StatusEffectComponent(agent));
			ApplySelfPerkAuras(agent);
		}
	}

	private static void ApplySelfPerkAuras(Agent agent)
	{
		Hero hero = agent.GetHero();
		if (hero != null && SotorPerks.Dampener != null && hero.GetPerkValue(SotorPerks.Dampener))
		{
			agent.GetComponent<StatusEffectComponent>()?.RunStatusEffect("dampener_ward_save", agent, 100000f, append: false);
			SotorLog.Info($"Dampener: applied 5% ward-save aura to {hero.Name}.");
		}
	}

	private void ApplyCaptainPerkAuras(Agent agent)
	{
		if (_captainAurasApplied.Contains(agent))
		{
			return;
		}
		Agent agent2 = agent.Formation?.Captain;
		if (agent2 == null)
		{
			return;
		}
		_captainAurasApplied.Add(agent);
		Hero hero = agent2.GetHero();
		if (hero == null)
		{
			return;
		}
		StatusEffectComponent component = agent.GetComponent<StatusEffectComponent>();
		if (component != null)
		{
			if (SotorPerks.ArcaneLink != null && hero.GetPerkValue(SotorPerks.ArcaneLink))
			{
				component.RunStatusEffect("arcanelink_magic_10", agent, 100000f, append: false);
				SotorLog.Debug($"ArcaneLink: +10% dmg aura on '{agent.Name}' (captain {hero.Name}).");
			}
			if (SotorPerks.Dampener != null && hero.GetPerkValue(SotorPerks.Dampener))
			{
				component.RunStatusEffect("dampener_formation_spellres", agent, 100000f, append: false);
				SotorLog.Debug($"Dampener: -30% spell-dmg formation aura on '{agent.Name}' (captain {hero.Name}).");
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		AgentReadOnlyList agentReadOnlyList = Mission.Current?.AllAgents;
		if (agentReadOnlyList == null)
		{
			return;
		}
		foreach (Agent item in agentReadOnlyList)
		{
			if (item != null)
			{
				if (item.IsHuman && !_captainAurasApplied.Contains(item))
				{
					ApplyCaptainPerkAuras(item);
				}
				StatusEffectComponent component = item.GetComponent<StatusEffectComponent>();
				if (component != null && component.NeedsStatusEffectTick)
				{
					component.OnTick(dt);
				}
			}
		}
		SotorSpellDamageLog.FlushExpired(Mission.Current);
	}

	public override void OnRemoveBehavior()
	{
		SotorSpellDamageLog.Reset();
		base.OnRemoveBehavior();
	}
}
