using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public static class SotorSpellcraftHelper
{
	public const float DamagePerSpellcraftPoint = 0.0005f;

	public const float DurationPerSpellcraftPoint = 0.0005f;

	public const float MaxWindsPerSpellcraftPoint = 0.3f;

	public const float WindsRechargePerSpellcraftPoint = 0.0075f;

	public const float BaseMaxWinds = 10f;

	public static SpellCastingLevel GetCastingLevel(Hero hero)
	{
		if (hero == null)
		{
			return SpellCastingLevel.None;
		}
		if (SotorPerks.Instance != null)
		{
			if (SotorPerks.Archmage != null && hero.GetPerkValue(SotorPerks.Archmage))
			{
				return SpellCastingLevel.Archmage;
			}
			if (SotorPerks.MasterSpells != null && hero.GetPerkValue(SotorPerks.MasterSpells))
			{
				return SpellCastingLevel.Master;
			}
			if (SotorPerks.AdeptSpells != null && hero.GetPerkValue(SotorPerks.AdeptSpells))
			{
				return SpellCastingLevel.Adept;
			}
			if (SotorPerks.EntrySpells != null && hero.GetPerkValue(SotorPerks.EntrySpells))
			{
				return SpellCastingLevel.Entry;
			}
			return SpellCastingLevel.Minor;
		}
		SkillObject spellcraft = SotorSkills.Spellcraft;
		int num = ((spellcraft != null) ? hero.GetSkillValue(spellcraft) : 0);
		if (num >= 200)
		{
			return SpellCastingLevel.Master;
		}
		if (num >= 100)
		{
			return SpellCastingLevel.Adept;
		}
		if (num >= 25)
		{
			return SpellCastingLevel.Entry;
		}
		return SpellCastingLevel.Minor;
	}

	public static int GetSpellBaseGoldCost(AbilityTemplate template)
	{
		if (template == null)
		{
			return 0;
		}
		int sotorDefault = template.SpellTier switch
		{
			1 => 5000,
			2 => 10000,
			3 => 25000,
			4 => 50000,
			_ => 0,
		};
		// [RF-B] preco por tier, ou preco proprio do feitico, vindo do XML.
		return SOTOR.RFIntegration.RFSpellPrices.GetSpellPrice(template.StringID, template.SpellTier, sotorDefault);
	}

	public static int GetSpellGoldCost(Hero hero, AbilityTemplate template)
	{
		int num = GetSpellBaseGoldCost(template);
		if (hero != null && SotorPerks.Librarian != null && hero.GetPerkValue(SotorPerks.Librarian))
		{
			num = (int)((float)num * 0.5f);
		}
		return num;
	}

	private static int SpellcraftValue(Hero hero)
	{
		SkillObject spellcraft = SotorSkills.Spellcraft;
		if (hero == null || spellcraft == null)
		{
			return 0;
		}
		return hero.GetSkillValue(spellcraft);
	}

	public static float GetSpellDurationFactor(Hero hero)
	{
		float num = 1f + 0.0005f * (float)SpellcraftValue(hero);
		if (hero != null && SotorPerks.Selfish != null && hero.GetPerkValue(SotorPerks.Selfish))
		{
			num += 0.15f;
		}
		return num;
	}

	public static float GetMaxWinds(Hero hero)
	{
		// [RF-B] o cajado e a bateria e a skill AMPLIFICA o instrumento (nao soma por
		// cima), para que nenhum nivel de Arcane compense um cajado pior. Com o gate
		// desligado devolve a formula original do SOTOR (10 + 0.3/ponto).
		int arcane = SpellcraftValue(hero);
		return SOTOR.RFIntegration.ArcaneFocusGate.ComputeMaxWinds(hero, arcane, 10f + 0.3f * (float)arcane);
	}

	public static float GetWindsRechargeSkillBonus(Hero hero)
	{
		// [RF-B] o cajado multiplica a recarga; sem foco nao regenera.
		return 0.0075f * (float)SpellcraftValue(hero) * SOTOR.RFIntegration.ArcaneFocusGate.GetRechargeMultiplier(hero);
	}

	public static float GetSpellDamageFactor(Hero hero)
	{
		SkillObject spellcraft = SotorSkills.Spellcraft;
		if (hero == null || spellcraft == null)
		{
			return 1f;
		}
		// [RF-B] o cajado multiplica a efetividade do feitico.
		return (1f + 0.0005f * (float)hero.GetSkillValue(spellcraft) + SotorSettings.SpellEffectivenessBonusFraction) * SOTOR.RFIntegration.ArcaneFocusGate.GetEffectivenessMultiplier(hero);
	}

	public static float GetCasterPerkDamageFactor(Hero hero)
	{
		if (hero == null)
		{
			return 1f;
		}
		// [RF-B] perks "Talisman" de Arcane do RF multiplicam o DANO do feitico.
		float num = SOTOR.RFIntegration.RFArcanePerks.GetDamageFactor(hero);
		if (SotorPerks.OverCaster != null && hero.GetPerkValue(SotorPerks.OverCaster))
		{
			num += 0.2f;
		}
		if (SotorPerks.EfficientSpellCaster != null && hero.GetPerkValue(SotorPerks.EfficientSpellCaster))
		{
			num -= 0.2f;
		}
		if (SotorPerks.Dampener != null && hero.GetPerkValue(SotorPerks.Dampener))
		{
			num -= 0.15f;
		}
		return num;
	}

	public static float GetVictimPerkDamageFactor(Hero casterHero, Agent caster, Agent victim)
	{
		if (casterHero == null || caster == null || victim == null)
		{
			return 1f;
		}
		float num = 1f;
		if (victim == caster && SotorPerks.Selfish != null && casterHero.GetPerkValue(SotorPerks.Selfish))
		{
			num *= 0.1f;
		}
		else if (victim != caster && !victim.IsEnemyOf(caster) && SotorPerks.WellControlled != null && casterHero.GetPerkValue(SotorPerks.WellControlled))
		{
			num *= 0.7f;
		}
		return num;
	}

	public static void GrantAbilityOutcomeXp(Hero caster, int rawXp, bool singleTarget)
	{
		SkillObject spellcraft = SotorSkills.Spellcraft;
		if (caster != null && spellcraft != null && rawXp > 0)
		{
			int num = (singleTarget ? (rawXp * 5) : rawXp);
			caster.AddSkillXp(spellcraft, num);
			SotorLog.Debug($"Spellcraft outcome XP: +{num} (raw={rawXp}, singleTarget={singleTarget}) -> skill now {caster.GetSkillValue(spellcraft)}.");
		}
	}
}
