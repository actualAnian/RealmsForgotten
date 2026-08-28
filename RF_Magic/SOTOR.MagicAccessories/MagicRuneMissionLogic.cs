using System;
using System.Collections.Generic;
using SOTOR.AbilitySystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
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

		/// <summary>Entidades portadoras das particulas de fogo presas ao esqueleto do
		/// alvo (Flame) — uma por osso, padrao TOWParticleSystem. Null/vazio nos
		/// efeitos sem visual (Frost).</summary>
		public List<GameEntity> Carriers;
	}

	private static readonly Dictionary<Agent, TimedEffect> _burns = new Dictionary<Agent, TimedEffect>();
	private static readonly Dictionary<Agent, TimedEffect> _frost = new Dictionary<Agent, TimedEffect>();

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_burns.Clear();
		_frost.Clear();
		_flameWeapons.Clear();
		MagicRuneCombatFeedback.Reset();
	}

	protected override void OnEndMission()
	{
		_burns.Clear();
		_frost.Clear();
		_flameWeapons.Clear();
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
			{
				// Reaproveita a chama de um burn ja ativo no mesmo alvo (refresh) em vez
				// de empilhar particulas a cada golpe.
				List<GameEntity> carriers = _burns.TryGetValue(affectedAgent, out TimedEffect existing) ? existing.Carriers : null;
				if (carriers == null || carriers.Count == 0)
					carriers = AttachBurnParticles(affectedAgent);
				_burns[affectedAgent] = new TimedEffect { Source = affectorAgent, Value = rune.PrimaryValue, EndsAt = now + rune.SecondaryValue, NextTick = now + 1f, Carriers = carriers };
				MagicRuneCombatFeedback.Report(affectorAgent, MagicRuneEffect.Flame, "burn_applied",
					$"{rune.Name}: burn applied ({rune.PrimaryValue:0} damage/sec for {rune.SecondaryValue:0}s)", Colors.Red);
				break;
			}
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

	// ---- arma flamejante da runa Flame ------------------------------------
	// Receita dos fire swords do RFEffects (WeaponParticlesBehavior +
	// TOWParticleSystem.ApplyParticleToWeapon), replicada aqui porque RF_Magic nao
	// referencia o RealmsForgottenMain: particula "fire_sword" presa a ENTIDADE DA
	// ARMA em degraus de 0,1u ao longo da lamina + componente no osso da mao.
	// Detectada por polling (0,5s) da arma empunhada — sem o hack de drop/pickup do
	// FireLord2, que poderia baguncar os slots das runas.
	private const string WeaponFireParticle = "fire_sword";
	private const float WieldPollInterval = 0.5f;

	private sealed class FlameWeaponState
	{
		public ItemObject Item;
		public Skeleton Skeleton;
		public readonly List<ParticleSystem> Particles = new List<ParticleSystem>();
	}

	private static readonly Dictionary<Agent, FlameWeaponState> _flameWeapons = new Dictionary<Agent, FlameWeaponState>();
	private float _wieldPollTimer;

	private void PollFlameWeapons(float dt)
	{
		_wieldPollTimer += dt;
		if (_wieldPollTimer < WieldPollInterval)
		{
			return;
		}
		_wieldPollTimer = 0f;

		try
		{
			foreach (Agent agent in Mission.Agents)
			{
				if (agent == null || !agent.IsHuman)
				{
					continue;
				}

				bool alive = agent.IsActive();
				MissionWeapon wielded = alive ? agent.WieldedWeapon : MissionWeapon.Invalid;
				bool hasFlameRune = alive && wielded.Item != null
					&& MagicRuneService.TryGetForWeapon(agent, in wielded, out MagicRuneData rune)
					&& rune.Effect == MagicRuneEffect.Flame;

				_flameWeapons.TryGetValue(agent, out FlameWeaponState state);
				if (hasFlameRune)
				{
					if (state != null && state.Item == wielded.Item)
					{
						continue; // ja flamejando nesta arma
					}
					if (state == null)
					{
						state = new FlameWeaponState();
						_flameWeapons[agent] = state;
					}
					else
					{
						RemoveWeaponFlames(state);
					}
					ApplyWeaponFlames(agent, wielded, state);
				}
				else if (state != null)
				{
					RemoveWeaponFlames(state);
					_flameWeapons.Remove(agent);
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MagicRuneMissionLogic.PollFlameWeapons: " + ex.Message);
		}
	}

	private static void ApplyWeaponFlames(Agent agent, in MissionWeapon weapon, FlameWeaponState state)
	{
		try
		{
			Skeleton skeleton = agent.AgentVisuals?.GetSkeleton();
			EquipmentIndex slot = agent.GetPrimaryWieldedItemIndex();
			if (skeleton == null || slot == EquipmentIndex.None)
			{
				return;
			}

			GameEntity weaponEntity = GameEntity.CreateFromWeakEntity(agent.GetWeaponEntityFromEquipmentSlot(slot));
			if (weaponEntity == null)
			{
				return;
			}

			if (ParticleSystemManager.GetRuntimeIdByName(WeaponFireParticle) == -1)
			{
				SotorLog.Warn("MagicRuneMissionLogic: particula '" + WeaponFireParticle + "' inexistente — arma da runa fica sem chama.");
				return;
			}

			int weaponLength = (int)Math.Round(weapon.GetWeaponStatsData()[0].WeaponLength / 10.0);
			int segments = Math.Max(2, weaponLength);
			sbyte handBone = agent.Monster.MainHandItemBoneIndex;
			for (int i = 1; i < segments; i++)
			{
				MatrixFrame baseFrame = new MatrixFrame(Mat3.Identity, default(Vec3));
				MatrixFrame boneLocalFrame = baseFrame.Elevate(i * 0.1f);
				ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(WeaponFireParticle, weaponEntity, ref boneLocalFrame);
				if (particle != null)
				{
					skeleton.AddComponentToBone(handBone, particle);
					state.Particles.Add(particle);
				}
			}

			state.Skeleton = skeleton;
			state.Item = weapon.Item;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MagicRuneMissionLogic.ApplyWeaponFlames: " + ex.Message);
		}
	}

	private static void RemoveWeaponFlames(FlameWeaponState state)
	{
		foreach (ParticleSystem particle in state.Particles)
		{
			try
			{
				state.Skeleton?.RemoveComponent(particle);
			}
			catch
			{
			}
		}
		state.Particles.Clear();
		state.Item = null;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		PollFlameWeapons(dt);
		float now = base.Mission?.CurrentTime ?? 0f;
		foreach (Agent agent in new List<Agent>(_burns.Keys))
		{
			TimedEffect burn = _burns[agent];
			if (agent == null || !agent.IsActive() || now >= burn.EndsAt)
			{
				DetachBurnParticle(agent, burn);
				_burns.Remove(agent);
				continue;
			}
			if (now >= burn.NextTick)
			{
				burn.NextTick = now + 1f;
				float actualDamage = SotorDamageHelper.ApplyFireDamageOverTime(agent, Math.Max(1, (int)burn.Value), burn.Source);
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

	/// <summary>Particula de vitima em chamas dos fire swords do RFEffects
	/// (weapons_effects.xml, victim_particle="fire_ground") — visual comprovado em jogo.
	/// Fallback vanilla se a arte do RF nao estiver carregada.</summary>
	private const string BurnParticleName = "fire_ground";
	private const string BurnParticleFallback = "psys_game_burning_agent";

	/// <summary>Ossos usados pelo TOWParticleSystem do RFEffects para cobrir o corpo.</summary>
	private static readonly sbyte[] BurnBones = { 0, 1, 2, 3, 5, 6, 7, 9, 12, 13, 15, 17, 22, 24 };

	/// <summary>
	/// Poe o alvo visivelmente em chamas — padrao da casa (RFEffects/TOWParticleSystem):
	/// uma entidade portadora por osso, particula "fire_ground". O bug original: a runa
	/// Flame so aplicava o dano por segundo, sem visual nenhum (autor, 2026-08-19); a 1ª
	/// tentativa com psys unico no osso 1 tambem nao rendeu chama visivel.
	/// </summary>
	private static List<GameEntity> AttachBurnParticles(Agent agent)
	{
		List<GameEntity> carriers = new List<GameEntity>();
		try
		{
			Skeleton skeleton = agent?.AgentVisuals?.GetSkeleton();
			Scene scene = Mission.Current?.Scene;
			if (skeleton == null || !skeleton.IsValid || scene == null)
			{
				return carriers;
			}

			string particleName = BurnParticleName;
			if (ParticleSystemManager.GetRuntimeIdByName(particleName) == -1)
			{
				SotorLog.Warn("MagicRuneMissionLogic: particula '" + particleName + "' inexistente no runtime — usando fallback '" + BurnParticleFallback + "'.");
				particleName = BurnParticleFallback;
				if (ParticleSystemManager.GetRuntimeIdByName(particleName) == -1)
				{
					SotorLog.Warn("MagicRuneMissionLogic: fallback tambem inexistente — burn fica sem visual.");
					return carriers;
				}
			}

			int boneCount = skeleton.GetBoneCount();
			foreach (sbyte bone in BurnBones)
			{
				if (bone >= boneCount)
				{
					continue;
				}
				GameEntity carrier = GameEntity.CreateEmpty(scene);
				MatrixFrame boneLocalFrame = MatrixFrame.Identity;
				ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(particleName, carrier, ref boneLocalFrame);
				if (particle == null)
				{
					carrier.Remove(0);
					continue;
				}
				agent.AgentVisuals.AddChildEntity(carrier);
				skeleton.AddComponentToBone(bone, particle);
				carriers.Add(carrier);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MagicRuneMissionLogic.AttachBurnParticles failed: " + ex.Message);
		}
		return carriers;
	}

	private static void DetachBurnParticle(Agent agent, TimedEffect burn)
	{
		List<GameEntity> carriers = burn?.Carriers;
		if (carriers == null)
		{
			return;
		}
		burn.Carriers = null;
		foreach (GameEntity carrier in carriers)
		{
			if (carrier == null)
			{
				continue;
			}
			try
			{
				carrier.RemoveAllParticleSystems();
				if (agent?.AgentVisuals != null)
				{
					agent.AgentVisuals.RemoveChildEntity(carrier, 0);
				}
				else
				{
					carrier.Remove(0);
				}
			}
			catch (Exception ex)
			{
				SotorLog.Warn("MagicRuneMissionLogic.DetachBurnParticle failed: " + ex.Message);
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
