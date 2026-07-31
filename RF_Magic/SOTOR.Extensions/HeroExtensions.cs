using System;
using SOTOR.AbilitySystem;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;

namespace SOTOR.Extensions;

public static class HeroExtensions
{
	public static string GetInfoKey(this Hero hero)
	{
		return hero.StringId;
	}

	public static HeroExtendedInfo GetExtendedInfo(this Hero hero)
	{
		return ExtendedInfoManager.Instance?.GetHeroInfoFor(hero.GetInfoKey());
	}

	public static bool HasAbility(this Hero hero, string abilityId)
	{
		return hero.GetExtendedInfo()?.AllAbilities.Contains(abilityId) ?? false;
	}

	public static void AddAbility(this Hero hero, string abilityId)
	{
		HeroExtendedInfo extendedInfo = hero.GetExtendedInfo();
		if (extendedInfo != null && !extendedInfo.AcquiredAbilities.Contains(abilityId))
		{
			extendedInfo.AcquiredAbilities.Add(abilityId);
		}
	}

	public static void AddAttribute(this Hero hero, string attribute)
	{
		HeroExtendedInfo extendedInfo = hero.GetExtendedInfo();
		if (extendedInfo != null && !extendedInfo.AcquiredAttributes.Contains(attribute))
		{
			extendedInfo.AcquiredAttributes.Add(attribute);
		}
	}

	public static bool HasAttribute(this Hero hero, string attribute)
	{
		return hero.GetExtendedInfo()?.AllAttributes.Contains(attribute) ?? false;
	}

	public static bool IsAbilityUser(this Hero hero)
	{
		return hero.HasAttribute("AbilityUser");
	}

	public static float GetWindsOfMagic(this Hero hero)
	{
		return hero.GetExtendedInfo()?.WindsOfMagic ?? 0f;
	}

	public static float GetMaxWindsOfMagic(this Hero hero)
	{
		return hero.GetExtendedInfo()?.MaxWindsOfMagic ?? 0f;
	}

	public static void AddWindsOfMagic(this Hero hero, float amount, bool allowOverMax = false)
	{
		hero.GetExtendedInfo()?.AddWindsOfMagic(amount, allowOverMax);
	}

	public static void SetWindsOfMagic(this Hero hero, float amount)
	{
		hero.GetExtendedInfo()?.SetWindsOfMagic(amount);
	}

	public static int GetEffectiveWindsCostForSpell(this Hero hero, AbilityTemplate template)
	{
		int num = template?.WindsOfMagicCost ?? 0;
		if (hero == null || num <= 0)
		{
			return num;
		}
		float num2 = 1f;
		if (SotorPerks.OverCaster != null && hero.GetPerkValue(SotorPerks.OverCaster))
		{
			num2 += 0.3f;
		}
		if (SotorPerks.EfficientSpellCaster != null && hero.GetPerkValue(SotorPerks.EfficientSpellCaster))
		{
			num2 -= 0.3f;
		}
		int perkAdjusted = Math.Max(0, (int)Math.Round((float)num * num2));
		// [RF-B] afinidade do cajado: instrumento afim com a escola gasta menos Mana,
		// instrumento estranho gasta mais. Ponto unico do custo.
		return SOTOR.RFIntegration.ArcaneFocusAffinity.ApplyToWindsCost(perkAdjusted, hero, template?.BelongsToLoreID);
	}
}
