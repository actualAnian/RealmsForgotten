using System;
using System.Collections.Generic;
using SOTOR.AbilitySystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.MagicAccessories;

public sealed class MagicRuneMissionLogic : MissionLogic
{
	private sealed class TimedEffect
	{
		public Agent Source;
		public float Value;
		public float EndsAt;
		public float NextTick;
	}

	private static readonly Dictionary<Agent, TimedEffect> _burns = new Dictionary<Agent, TimedEffect>();
	private static readonly Dictionary<Agent, TimedEffect> _frost = new Dictionary<Agent, TimedEffect>();

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_burns.Clear();
		_frost.Clear();
		MagicRuneCombatFeedback.Reset();
	}

	protected override void OnEndMission()
	{
		_burns.Clear();
		_frost.Clear();
		MagicRuneCombatFeedback.Reset();
		base.OnEndMission();
	}

	public static float GetFrostMultiplier(Agent agent)
	{
		float now = Mission.Current?.CurrentTime ?? 0f;
		return agent != null && _frost.TryGetValue(agent, out TimedEffect effect) && effect.EndsAt > now
			? Math.Max(0.1f, 1f - effect.Value / 100f)
			: 1f;
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon,
		in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, in affectorWeapon, in blow, in attackCollisionData);
		try
		{
			if (affectedAgent == null || affectorAgent == null || affectedAgent == affectorAgent)
			{
				return;
			}

			if (attackCollisionData.AttackBlockedWithShield &&
				MagicRuneService.HasEffect(affectedAgent, MagicRuneEffect.Reprisal, out MagicRuneData reprisal))
			{
				float actualReflected = SotorDamageHelper.ApplyReflectedDamage(affectorAgent, (int)reprisal.PrimaryValue, affectedAgent);
				if (actualReflected > 0f)
				{
					MagicRuneCombatFeedback.Report(affectedAgent, MagicRuneEffect.Reprisal, "shield_reprisal",
						$"{reprisal.Name}: dealt {actualReflected:0} retaliatory damage", Colors.Green);
				}
			}

			if (!MagicRuneService.TryGetForWeapon(affectorAgent, in affectorWeapon, out MagicRuneData rune) ||
				!affectedAgent.IsEnemyOf(affectorAgent))
			{
				return;
			}
			if (blow.InflictedDamage > 0 && MagicRuneCombatFeedback.TryConsumeWeaponModifier(affectorAgent, affectedAgent, out MagicRuneData weaponModifier))
			{
				MagicRuneCombatFeedback.Report(affectorAgent, weaponModifier.Effect, "weapon_modifier_hit",
					$"{weaponModifier.Name}: active (+{weaponModifier.PrimaryValue:0}% weapon modifier); hit dealt {blow.InflictedDamage:0} damage", Colors.Green);
			}

			float now = base.Mission?.CurrentTime ?? 0f;
			switch (rune.Effect)
			{
			case MagicRuneEffect.Vampiric:
				if (!affectedAgent.IsActive() || affectedAgent.Health < 1f)
				{
					float before = affectorAgent.Health;
					affectorAgent.Health = Math.Min(affectorAgent.HealthLimit, affectorAgent.Health + rune.PrimaryValue);
					float healed = affectorAgent.Health - before;
					if (healed > 0f)
					{
						MagicRuneCombatFeedback.Report(affectorAgent, MagicRuneEffect.Vampiric, "kill_heal",
							$"{rune.Name}: restored {healed:0} health", Colors.Green);
					}
				}
				break;
			case MagicRuneEffect.Flame:
				_burns[affectedAgent] = new TimedEffect { Source = affectorAgent, Value = rune.PrimaryValue, EndsAt = now + rune.SecondaryValue, NextTick = now + 1f };
				MagicRuneCombatFeedback.Report(affectorAgent, MagicRuneEffect.Flame, "burn_applied",
					$"{rune.Name}: burn applied ({rune.PrimaryValue:0} damage/sec for {rune.SecondaryValue:0}s)", Colors.Red);
				break;
			case MagicRuneEffect.Frost:
				_frost[affectedAgent] = new TimedEffect { Source = affectorAgent, Value = rune.PrimaryValue, EndsAt = now + rune.SecondaryValue };
				MagicRuneCombatFeedback.Report(affectorAgent, MagicRuneEffect.Frost, "frost_applied",
					$"{rune.Name}: slowed target by {rune.PrimaryValue:0}% for {rune.SecondaryValue:0}s", Colors.Cyan);
				break;
			case MagicRuneEffect.Storm:
				if (MBRandom.RandomFloat * 100f < rune.SecondaryValue)
				{
					int affected = DamageNearbyEnemy(affectedAgent, affectorAgent, rune.PrimaryValue, 4f, out float actualDamage);
					if (affected > 0 && actualDamage > 0f)
					{
						MagicRuneCombatFeedback.Report(affectorAgent, MagicRuneEffect.Storm, "chain_damage",
							$"{rune.Name}: dealt {actualDamage:0} damage to {affected} enemy", Colors.Cyan);
					}
				}
				break;
			case MagicRuneEffect.Explosive:
				int targets = DamageNearbyEnemies(affectedAgent, affectorAgent, rune.PrimaryValue, rune.SecondaryValue, out float explosionDamage);
				if (targets > 0 && explosionDamage > 0f)
				{
					MagicRuneCombatFeedback.Report(affectorAgent, MagicRuneEffect.Explosive, "area_damage",
						$"{rune.Name}: dealt {explosionDamage:0} damage to {targets} enemies", Colors.Red);
				}
				break;
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MagicRuneMissionLogic.OnAgentHit failed: " + ex.Message);
		}
	}

	public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position,
		Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
	{
		base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
		try
		{
			if (shooterAgent == null || !MagicRuneService.HasEffect(shooterAgent, MagicRuneEffect.Returning, out MagicRuneData rune) ||
				MBRandom.RandomFloat * 100f >= rune.PrimaryValue)
			{
				return;
			}
			EquipmentIndex restoreSlot = weaponIndex;
			ItemObject.ItemTypeEnum type = shooterAgent.Equipment[weaponIndex].Item?.Type ?? ItemObject.ItemTypeEnum.Invalid;
			if (type == ItemObject.ItemTypeEnum.Bow || type == ItemObject.ItemTypeEnum.Crossbow || type == ItemObject.ItemTypeEnum.Sling)
			{
				for (EquipmentIndex slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.Weapon3; slot++)
				{
					ItemObject.ItemTypeEnum candidate = shooterAgent.Equipment[slot].Item?.Type ?? ItemObject.ItemTypeEnum.Invalid;
					if (candidate == ItemObject.ItemTypeEnum.Arrows || candidate == ItemObject.ItemTypeEnum.Bolts ||
						candidate == ItemObject.ItemTypeEnum.SlingStones || candidate == ItemObject.ItemTypeEnum.Bullets)
					{
						restoreSlot = slot;
						break;
					}
				}
			}
			short amount = shooterAgent.Equipment[restoreSlot].Amount;
			shooterAgent.SetWeaponAmountInSlot(restoreSlot, (short)(amount + 1), true);
			short restored = shooterAgent.Equipment[restoreSlot].Amount;
			if (restored > amount)
			{
				MagicRuneCombatFeedback.Report(shooterAgent, MagicRuneEffect.Returning, "projectile_returned",
					$"{rune.Name}: recovered {restored - amount} projectile", Colors.Green);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MagicRuneMissionLogic.OnAgentShootMissile failed: " + ex.Message);
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		float now = base.Mission?.CurrentTime ?? 0f;
		foreach (Agent agent in new List<Agent>(_burns.Keys))
		{
			TimedEffect burn = _burns[agent];
			if (agent == null || !agent.IsActive() || now >= burn.EndsAt)
			{
				_burns.Remove(agent);
				continue;
			}
			if (now >= burn.NextTick)
			{
				burn.NextTick = now + 1f;
				float actualDamage = SotorDamageHelper.ApplyDamageOverTime(agent, Math.Max(1, (int)burn.Value), burn.Source);
				SotorLog.Debug($"Rune Flame tick: source='{burn.Source?.Name}' target='{agent.Name}' damage={actualDamage:0}.");
			}
		}
		foreach (Agent agent in new List<Agent>(_frost.Keys))
		{
			if (agent == null || !agent.IsActive() || now >= _frost[agent].EndsAt)
			{
				_frost.Remove(agent);
			}
		}
	}

	private static int DamageNearbyEnemy(Agent center, Agent source, float damage, float radius, out float actualDamage)
	{
		actualDamage = 0f;
		foreach (Agent agent in Mission.Current.GetNearbyAgents(center.Position.AsVec2, radius, new MBList<Agent>()))
		{
			if (agent != center && agent.IsActive() && agent.IsEnemyOf(source))
			{
				actualDamage = SotorDamageHelper.ApplyDamageOverTime(agent, Math.Max(1, (int)damage), source);
				return actualDamage > 0f ? 1 : 0;
			}
		}
		return 0;
	}

	private static int DamageNearbyEnemies(Agent center, Agent source, float damage, float radius, out float actualDamage)
	{
		actualDamage = 0f;
		int affected = 0;
		foreach (Agent agent in Mission.Current.GetNearbyAgents(center.Position.AsVec2, radius, new MBList<Agent>()))
		{
			if (agent != center && agent.IsActive() && agent.IsEnemyOf(source))
			{
				float damageDealt = SotorDamageHelper.ApplyDamageOverTime(agent, Math.Max(1, (int)damage), source);
				if (damageDealt > 0f)
				{
					affected++;
					actualDamage += damageDealt;
				}
			}
		}
		return affected;
	}
}
