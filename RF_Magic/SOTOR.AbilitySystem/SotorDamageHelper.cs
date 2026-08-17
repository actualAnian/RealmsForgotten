using System;
using System.Collections.Generic;
using SOTOR.AbilitySystem.StatusEffects;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using SOTOR.MagicAccessories;

namespace SOTOR.AbilitySystem;

public static class SotorDamageHelper
{
	public static bool InSpellBlow;

	public static float ApplyDamageOverTime(Agent agent, int damageAmount, Agent applier)
	{
		float before = agent?.Health ?? 0f;
		if (agent != null && agent.IsHuman && agent.IsActive() && !(agent.Health < 1f) && !agent.IsFadingOut() && damageAmount > 0)
		{
			if (agent.Health > (float)damageAmount)
			{
				agent.Health -= damageAmount;
			}
			else
			{
				ApplyDamage(agent, damageAmount, agent.Position, applier, hasShockWave: false);
			}
		}
		return Math.Max(0f, before - (agent?.Health ?? before));
	}

	public static float ApplyReflectedDamage(Agent attacker, int damageAmount, Agent reflector)
	{
		float before = attacker?.Health ?? 0f;
		if (attacker != null && damageAmount > 0)
		{
			ApplyDamage(attacker, damageAmount, attacker.GetChestGlobalPosition(), reflector, hasShockWave: false);
		}
		return Math.Max(0f, before - (attacker?.Health ?? before));
	}

	public static void DamageAgents(IEnumerable<Agent> agents, int minDamage, int maxDamage, Agent damager, TriggeredEffectTemplate template, bool hasShockWave, Vec3 impactPosition, bool singleTarget = false, string spellName = null)
	{
		if (agents == null || damager == null)
		{
			return;
		}
		Mission current = Mission.Current;
		int num = 0;
		int num2 = 0;
		foreach (Agent agent in agents)
		{
			num++;
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.Health < 1f || agent.IsFadingOut() || (current != null && current.FindAgentWithIndex(agent.Index) != agent))
			{
				continue;
			}
			int num3 = ((maxDamage < minDamage) ? minDamage : MBRandom.RandomInt(minDamage, maxDamage));
			if (num3 < 0)
			{
				continue;
			}
			num2++;
			float health = agent.Health;
			bool flag = damager != null && agent.IsEnemyOf(damager);
			SotorLog.Info($"DamageAgents: '{agent.Name}' dmg={num3} hpBefore={health:0} enemyOfCaster={flag} " + $"(effect='{template?.StringID}', shockwave={hasShockWave})");
			if (impactPosition != Vec3.Zero && hasShockWave && template != null && template.Radius > 0f)
			{
				float num4 = agent.Position.Distance(impactPosition);
				num3 = (int)((template.Radius - num4) / template.Radius * (float)num3);
			}
			num3 = ScaleByDamageType(num3, template?.DamageType ?? DamageType.Physical, damager, agent);
			if (num3 < 0)
			{
				num3 = 0;
			}
			num3 = ScaleBySpellcraft(num3, damager, agent);
			ApplyDamage(agent, num3, impactPosition, damager, hasShockWave);
			SotorLog.Info($"DamageAgents: '{agent.Name}' hpAfter={agent.Health:0} (delta={health - agent.Health:0}).");
			int amount = (int)(health - agent.Health);
			bool killed = health > 0f && (!agent.IsActive() || agent.Health < 1f);
			SotorSpellDamageLog.BookHit(damager, agent, template?.DamageType ?? DamageType.Physical, amount, killed, spellName);
			if (num3 > 0 && damager != null && agent.IsEnemyOf(damager))
			{
				Hero hero = damager.GetHero();
				if (hero != null)
				{
					SotorSpellcraftHelper.GrantAbilityOutcomeXp(hero, num3 / 5, singleTarget);
				}
			}
			if (num3 > 0 && health > 0f && (!agent.IsActive() || agent.Health < 1f))
			{
				TryGrantWindsOnMagicKill(damager, agent);
			}
		}
		SotorLog.Info($"DamageAgents summary: effect='{template?.StringID}' considered={num} applied={num2}.");
	}

	private static int ScaleByDamageType(int amount, DamageType damageType, Agent attacker, Agent victim)
	{
		if (amount <= 0 || damageType == DamageType.All || damageType == DamageType.Invalid)
		{
			return amount;
		}
		float amp;
		float resist;
		float ward;
		float damageFactor = SotorResistanceHelper.GetDamageFactor(attacker, victim, AttackTypeMask.Spell, damageType, out amp, out resist, out ward);
		if (damageFactor == 1f)
		{
			return amount;
		}
		int num = (int)((float)amount * damageFactor);
		SotorLog.Debug($"Spell damage-type scale ({damageType}): amp={amp:0.00} resist={resist:0.00} ward={ward:0.00} | {amount} -> {num}.");
		return num;
	}

	public static float GetSpellcraftDamageFactorFor(Agent damager)
	{
		Hero hero = damager?.GetHero();
		float factor = hero == null ? 1f : SotorSpellcraftHelper.GetSpellDamageFactor(hero) * SotorSpellcraftHelper.GetCasterPerkDamageFactor(hero);
		if (MagicRuneService.HasEffect(damager, MagicRuneEffect.Arcane, out MagicRuneData rune))
		{
			factor *= 1f + rune.PrimaryValue / 100f;
		}
		return factor;
	}

	private static int ScaleBySpellcraft(int amount, Agent damager, Agent victim)
	{
		if (amount <= 0)
		{
			return amount;
		}
		Hero hero = damager.GetHero();
		float victimPerkDamageFactor = hero == null ? 1f : SotorSpellcraftHelper.GetVictimPerkDamageFactor(hero, damager, victim);
		float casterDamageFactor = GetSpellcraftDamageFactorFor(damager);
		float num = casterDamageFactor * victimPerkDamageFactor;
		if (num == 1f)
		{
			return amount;
		}
		int num2 = (int)((float)amount * num);
		if (MagicRuneService.HasEffect(damager, MagicRuneEffect.Arcane, out MagicRuneData rune))
		{
			MagicRuneCombatFeedback.Report(damager, MagicRuneEffect.Arcane, "spell_damage",
				$"{rune.Name}: active (+{rune.PrimaryValue:0}% spell-damage calculation)", Colors.Cyan);
		}
		SotorLog.Debug($"Spell damage scale: caster={casterDamageFactor:0.000} victimPerk={victimPerkDamageFactor:0.000} => x{num:0.000} | {amount} -> {num2}.");
		return num2;
	}

	private static void ApplyDamage(Agent agent, int damageAmount, Vec3 impactPosition, Agent damager, bool hasShockWave)
	{
		if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.Health < 1f)
		{
			return;
		}
		try
		{
			if (agent.IsFadingOut() || (agent.State != AgentState.Active && agent.State != AgentState.Routed))
			{
				return;
			}
			Agent agent2 = damager ?? agent;
			Blow blow = new Blow(agent2.Index);
			blow.DamageType = DamageTypes.Blunt;
			blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
			blow.GlobalPosition = agent.GetChestGlobalPosition();
			blow.BaseMagnitude = damageAmount;
			blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
			blow.WeaponRecord.Weight = 5f;
			blow.InflictedDamage = damageAmount;
			Vec3 vec = ((blow.GlobalPosition == impactPosition) ? (-agent.LookDirection) : (blow.GlobalPosition - impactPosition));
			vec.Normalize();
			blow.Direction = vec;
			blow.SwingDirection = vec;
			blow.DamageCalculated = true;
			blow.AttackType = AgentAttackType.Kick;
			blow.BlowFlag = BlowFlags.NoSound;
			blow.VictimBodyPart = BoneBodyPartType.Chest;
			blow.StrikeType = StrikeType.Thrust;
			if (hasShockWave)
			{
				blow.BlowFlag |= (BlowFlags)(agent.HasMount ? 2048 : 32);
				blow.BaseMagnitude = 1000f;
			}
			blow.WeaponRecord.Velocity = vec * blow.BaseMagnitude;
			sbyte mainHandItemBoneIndex = agent2.Monster.MainHandItemBoneIndex;
			AttackCollisionData collisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(_attackBlockedWithShield: false, _correctSideShieldBlock: false, _isAlternativeAttack: false, _isColliderAgent: true, _collidedWithShieldOnBack: false, _isMissile: false, _isMissileBlockedWithWeapon: false, _missileHasPhysics: false, _entityExists: false, _thrustTipHit: false, _missileGoneUnderWater: false, _missileGoneOutOfBorder: false, CombatCollisionResult.StrikeAgent, -999, 1, 2, blow.BoneIndex, blow.VictimBodyPart, mainHandItemBoneIndex, Agent.UsageDirection.AttackUp, -1, CombatHitResultFlags.NormalHit, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, Vec3.Up, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, agent.Velocity, Vec3.Up);
			InSpellBlow = true;
			try
			{
				agent.RegisterBlow(blow, in collisionData);
			}
			finally
			{
				InSpellBlow = false;
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorDamageHelper.ApplyDamage failed: " + ex.Message);
		}
	}

	private static void TryGrantWindsOnMagicKill(Agent caster, Agent victim)
	{
		try
		{
			if (!SotorSettings.EnableWindsOnMagicKill)
			{
				return;
			}
			float windsOnMagicKillAmount = SotorSettings.WindsOnMagicKillAmount;
			if (windsOnMagicKillAmount != 0f && caster != null && victim != null && caster != victim && caster.IsPlayerControlled && victim.IsEnemyOf(caster))
			{
				Hero hero = caster.GetHero();
				if (hero != null)
				{
					hero.AddWindsOfMagic(windsOnMagicKillAmount, allowOverMax: true);
					SotorLog.Info($"Winds on magic kill: {windsOnMagicKillAmount:0.0} to '{hero.Name}' (killed '{victim.Name}').");
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("TryGrantWindsOnMagicKill failed: " + ex.Message);
		}
	}
}
