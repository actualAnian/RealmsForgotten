using System.Collections.Generic;
using SOTOR.AbilitySystem;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SOTOR.Extensions;

public static class AgentExtensions
{
	public static bool IsAbilityUser(this Agent agent)
	{
		return agent.GetAttributes().Contains("AbilityUser");
	}

	public static bool IsSpellCaster(this Agent agent)
	{
		return agent.GetAttributes().Contains("SpellCaster");
	}

	public static Ability GetCurrentAbility(this Agent agent)
	{
		return agent.GetComponent<AbilityComponent>()?.CurrentAbility;
	}

	public static Ability GetAbility(this Agent agent, int index)
	{
		List<Ability> list = agent.GetComponent<AbilityComponent>()?.KnownAbilitySystem;
		if (list == null || index < 0 || index >= list.Count)
		{
			return null;
		}
		return list[index];
	}

	public static void SelectAbility(this Agent agent, int abilityIndex)
	{
		agent.GetComponent<AbilityComponent>()?.SelectAbility(abilityIndex);
	}

	public static void SelectAbility(this Agent agent, Ability ability)
	{
		agent.GetComponent<AbilityComponent>()?.SelectAbility(ability);
	}

	public static bool TryCastCurrentAbility(this Agent agent, out TextObject failureReason)
	{
		AbilityComponent component = agent.GetComponent<AbilityComponent>();
		if (component?.CurrentAbility != null)
		{
			return component.CurrentAbility.TryCast(agent, out failureReason);
		}
		failureReason = new TextObject("{=sotor_cast_fail_no_ability}No ability selected.");
		return false;
	}

	public static Hero GetHero(this Agent agent)
	{
		if (agent?.Character == null || !(Game.Current?.GameType is Campaign))
		{
			return null;
		}
		if (agent.Character is CharacterObject { IsHero: not false } characterObject)
		{
			return characterObject.HeroObject;
		}
		return null;
	}

	public static List<string> GetSelectedAbilities(this Agent agent)
	{
		Hero hero = agent?.GetHero();
		if (hero != null)
		{
			HeroExtendedInfo extendedInfo = hero.GetExtendedInfo();
			if (extendedInfo != null)
			{
				return new List<string>(extendedInfo.SelectedAbilities);
			}
		}
		// [RF-B] tropa sem heroi: repertorio vem da CULTURA dela, limitado pelo tier
		// do foco que carrega. Antes devolvia lista vazia — nenhuma tropa conjurava.
		return SOTOR.RFIntegration.ArcaneFocusCasterSource.GetAbilities(agent);
	}

	public static List<string> GetAttributes(this Agent agent)
	{
		List<string> list = new List<string>();
		Hero hero = agent?.GetHero();
		if (hero != null)
		{
			HeroExtendedInfo extendedInfo = hero.GetExtendedInfo();
			if (extendedInfo != null)
			{
				foreach (string allAttribute in extendedInfo.AllAttributes)
				{
					if (!list.Contains(allAttribute))
					{
						list.Add(allAttribute);
					}
				}
			}
		}
		else if (agent?.Character != null)
		{
			foreach (string attribute in agent.Character.GetAttributes())
			{
				if (!list.Contains(attribute))
				{
					list.Add(attribute);
				}
			}
			// [RF-B] tropa que CARREGA um foco arcano e conjuradora, mesmo sem
			// atributo no XML de tropa. Identidade do mod: o instrumento faz o mago.
			foreach (string attribute in SOTOR.RFIntegration.ArcaneFocusCasterSource.GetAttributes(agent))
			{
				if (!list.Contains(attribute))
				{
					list.Add(attribute);
				}
			}
		}
		return list;
	}
}
