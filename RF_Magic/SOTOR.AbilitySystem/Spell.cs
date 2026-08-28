using System;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class Spell : Ability
{
	public Spell(AbilityTemplate template)
		: base(template)
	{
	}

	private static bool TryGetWindsHero(Agent casterAgent, out Hero hero)
	{
		hero = null;
		if (!(Game.Current?.GameType is Campaign))
		{
			return false;
		}
		hero = casterAgent?.GetHero();
		if (hero != null)
		{
			return hero.GetExtendedInfo() != null;
		}
		return false;
	}

	public override bool IsDisabled(Agent casterAgent, out TextObject disabledReason)
	{
		if (base.IsDisabled(casterAgent, out disabledReason))
		{
			return true;
		}
		if (SotorSettings.DisableMagicInSieges && Mission.Current != null && Mission.Current.IsSiegeBattle)
		{
			disabledReason = new TextObject("{=sotor_spell_no_siege_magic}Magic is disabled during sieges");
			return true;
		}
		// [RF-B] tropa conjuradora tem pool proprio: sem isto ela conjuraria sem
		// limite, porque so heroi tem HeroExtendedInfo para descontar mana.
		if (SOTOR.RFIntegration.TroopWindsPool.AppliesTo(casterAgent)
			&& !SOTOR.RFIntegration.TroopWindsPool.CanAfford(casterAgent, base.Template))
		{
			disabledReason = new TextObject("{=sotor_spell_not_enough_wom}Not enough Mana");
			return true;
		}
		if (TryGetWindsHero(casterAgent, out var hero))
		{
			int effectiveWindsCostForSpell = hero.GetEffectiveWindsCostForSpell(base.Template);
			if (hero.GetWindsOfMagic() < (float)effectiveWindsCostForSpell)
			{
				disabledReason = new TextObject("{=sotor_spell_not_enough_wom}Not enough Mana");
				return true;
			}
		}
		return false;
	}

	protected override void OnCastSucceeded(Agent casterAgent)
	{
		// [RF-B] cobra o pool da tropa (heroi segue pelo caminho de baixo).
		SOTOR.RFIntegration.TroopWindsPool.Spend(casterAgent, base.Template);
		if (TryGetWindsHero(casterAgent, out var hero))
		{
			int effectiveWindsCostForSpell = hero.GetEffectiveWindsCostForSpell(base.Template);
			if (effectiveWindsCostForSpell > 0)
			{
				float windsOfMagic = hero.GetWindsOfMagic();
				hero.AddWindsOfMagic(-effectiveWindsCostForSpell);
				SotorLog.Info($"Winds spent: {base.StringID} cost {effectiveWindsCostForSpell} | {windsOfMagic:0} -> {hero.GetWindsOfMagic():0} / {hero.GetMaxWindsOfMagic():0}.");
			}
			GrantSpellcraftXp(hero);
		}
	}

	protected void GrantSpellcraftXp(Hero hero)
	{
		SkillObject spellcraft = SotorSkills.Spellcraft;
		if (spellcraft != null)
		{
			int num = Math.Max(1, base.Template.WindsOfMagicCost) * 20;
			bool flag = SotorPerks.Librarian != null && hero.GetPerkValue(SotorPerks.Librarian);
			if (flag)
			{
				num = (int)((float)num * 1.25f);
			}
			hero.AddSkillXp(spellcraft, num);
			SotorLog.Info($"Spellcraft XP: {base.StringID} +{num} (librarian={flag}) -> skill now {hero.GetSkillValue(spellcraft)}.");
		}
	}
}
