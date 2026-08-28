using System;
using System.Collections.Generic;
using System.Linq;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.StatusEffects;

public class StatusEffectComponent : AgentComponent, IDisposable
{
	private class Aggregate
	{
		public float HealthOverTime;

		public float DamageOverTime;

		public float SpeedProperties;

		public float AttackSpeedProperties;

		public float ReloadSpeedProperties;

		public float Thorns;

		public readonly Dictionary<AttackTypeMask, float[]> DamageAmplifications = NewMaskTable();

		public readonly Dictionary<AttackTypeMask, float[]> Resistances = NewMaskTable();

		private static Dictionary<AttackTypeMask, float[]> NewMaskTable()
		{
			Dictionary<AttackTypeMask, float[]> dictionary = new Dictionary<AttackTypeMask, float[]>();
			foreach (AttackTypeMask value in Enum.GetValues(typeof(AttackTypeMask)))
			{
				dictionary[value] = new float[8];
			}
			return dictionary;
		}

		public void AddEffect(StatusEffectTemplate t)
		{
			float baseEffectValue = t.BaseEffectValue;
			switch (t.Type)
			{
			case StatusEffectTemplate.EffectType.DamageOverTime:
				DamageOverTime += baseEffectValue;
				break;
			case StatusEffectTemplate.EffectType.HealthOverTime:
				HealthOverTime += baseEffectValue;
				break;
			case StatusEffectTemplate.EffectType.MovementManipulation:
				SpeedProperties += baseEffectValue;
				break;
			case StatusEffectTemplate.EffectType.AttackSpeedManipulation:
				AttackSpeedProperties += baseEffectValue;
				break;
			case StatusEffectTemplate.EffectType.ReloadSpeedManipulation:
				ReloadSpeedProperties += baseEffectValue;
				break;
			case StatusEffectTemplate.EffectType.DamageAmplification:
				AddToMaskTable(DamageAmplifications, t.DamageType, t.AttackTypeMask, baseEffectValue);
				break;
			case StatusEffectTemplate.EffectType.Resistance:
				AddToMaskTable(Resistances, t.DamageType, t.AttackTypeMask, baseEffectValue);
				break;
			case StatusEffectTemplate.EffectType.TemporaryAttributeOnly:
				if (t.TemporaryAttributes != null && t.TemporaryAttributes.Exists((string a) => a.Equals("Thorns", StringComparison.OrdinalIgnoreCase)))
				{
					Thorns += baseEffectValue;
				}
				break;
			case StatusEffectTemplate.EffectType.WindsOverTime:
			case StatusEffectTemplate.EffectType.LanceSteadiness:
				break;
			}
		}

		private static void AddToMaskTable(Dictionary<AttackTypeMask, float[]> table, DamageType dt, AttackTypeMask mask, float value)
		{
			if ((mask & AttackTypeMask.Ranged) != 0)
			{
				table[AttackTypeMask.Ranged][(int)dt] += value;
			}
			if ((mask & AttackTypeMask.Spell) != 0)
			{
				table[AttackTypeMask.Spell][(int)dt] += value;
			}
			if ((mask & AttackTypeMask.Melee) != 0)
			{
				table[AttackTypeMask.Melee][(int)dt] += value;
			}
		}
	}

	private class EffectData
	{
		public StatusEffect Effect;

		public GameEntity ParticleEntity;

		public EffectData(StatusEffect effect, GameEntity particleEntity)
		{
			Effect = effect;
			ParticleEntity = particleEntity;
		}
	}

	private readonly Dictionary<StatusEffect, EffectData> _currentEffects = new Dictionary<StatusEffect, EffectData>();

	private Aggregate _aggregate = new Aggregate();

	private const float UpdateFrequency = 1f;

	private float _deltaSinceLastTick;

	private bool _disabled;

	private readonly List<GameEntity> _groundEntities = new List<GameEntity>();

	private readonly List<GameEntity> _extraCarriers = new List<GameEntity>();

	private EquipmentIndex _weaponFxWieldedIndex = EquipmentIndex.None;

	private bool _hasWeaponAttachedEffect;

	private bool _hadSpeedModifier;

	public bool HasActiveEffects => _currentEffects.Count > 0;

	public bool NeedsStatusEffectTick => _currentEffects.Count > 0;

	public StatusEffectComponent(Agent agent)
		: base(agent)
	{
		_deltaSinceLastTick = MBRandom.RandomFloatRanged(0f, 0.1f);
	}

	private bool HasUsableVisuals()
	{
		if (Agent != null && !Agent.IsFadingOut() && Agent.AgentVisuals != null)
		{
			return Agent.AgentVisuals.IsValid();
		}
		return false;
	}

	public bool RunStatusEffect(string effectId, Agent applierAgent, float duration, bool append, string originSpellName = null)
	{
		if (Agent == null || _disabled)
		{
			return false;
		}
		StatusEffect statusEffect = _currentEffects.Keys.FirstOrDefault((StatusEffect e) => e.Template.StringID == effectId);
		if (statusEffect != null)
		{
			statusEffect.CurrentDuration = (append ? (statusEffect.CurrentDuration + duration) : duration);
			if (string.IsNullOrEmpty(statusEffect.OriginSpellName) && !string.IsNullOrEmpty(originSpellName))
			{
				statusEffect.OriginSpellName = originSpellName;
			}
			return false;
		}
		StatusEffect statusEffect2 = StatusEffectManager.CreateNewStatusEffect(effectId, applierAgent);
		if (statusEffect2 == null)
		{
			SotorLog.Warn("StatusEffectComponent: unknown status effect '" + effectId + "'.");
			return false;
		}
		statusEffect2.CurrentDuration = duration;
		statusEffect2.OriginSpellName = originSpellName;
		AddEffect(statusEffect2);
		SotorLog.Info($"StatusEffect '{effectId}' applied to '{Agent?.Name}' for {duration}s (type={statusEffect2.Template.Type}).");
		return true;
	}

	public new void OnTick(float dt)
	{
		if (_currentEffects.Count != 0)
		{
			UpdateGroundEntities();
			RefreshWeaponAttachedParticlesOnSwap();
			_deltaSinceLastTick += dt;
			if (_deltaSinceLastTick > 1f)
			{
				_deltaSinceLastTick = MBRandom.RandomFloatRanged(0f, 0.1f);
				OnElapsed();
			}
		}
	}

	private void UpdateGroundEntities()
	{
		if (_groundEntities.Count == 0 || Agent == null || !Agent.IsActive() || Agent.IsFadingOut())
		{
			return;
		}
		MatrixFrame frame = new MatrixFrame(Mat3.Identity, Agent.GetChestGlobalPosition());
		foreach (GameEntity groundEntity in _groundEntities)
		{
			if (groundEntity != null)
			{
				groundEntity.SetGlobalFrame(in frame);
			}
		}
	}

	private void RefreshWeaponAttachedParticlesOnSwap()
	{
		if (!_hasWeaponAttachedEffect || Agent == null || !Agent.IsActive() || Agent.IsFadingOut())
		{
			return;
		}
		EquipmentIndex primaryWieldedItemIndex = Agent.GetPrimaryWieldedItemIndex();
		if (primaryWieldedItemIndex == _weaponFxWieldedIndex)
		{
			return;
		}
		_weaponFxWieldedIndex = primaryWieldedItemIndex;
		foreach (KeyValuePair<StatusEffect, EffectData> item in _currentEffects.ToList())
		{
			EffectData value = item.Value;
			if (value.Effect?.Template == null || !value.Effect.Template.ApplyToWeapon)
			{
				continue;
			}
			RemoveVisuals(value);
			foreach (GameEntity extraCarrier in _extraCarriers)
			{
				try
				{
					extraCarrier?.RemoveAllParticleSystems();
					if (HasUsableVisuals())
					{
						Agent.AgentVisuals.RemoveChildEntity(extraCarrier, 0);
					}
					else
					{
						extraCarrier?.Remove(0);
					}
				}
				catch
				{
				}
			}
			_extraCarriers.Clear();
			value.ParticleEntity = TryAttachParticle(value.Effect.Template.ParticleId, weaponAttach: true);
		}
	}

	private void OnElapsed()
	{
		foreach (KeyValuePair<StatusEffect, EffectData> item in _currentEffects.ToList())
		{
			item.Key.CurrentDuration -= 1f;
			if (item.Key.CurrentDuration <= 0f)
			{
				RemoveEffect(item.Key);
			}
		}
		CalculateAggregate();
		if (Agent == null || !Agent.IsActive() || Agent.IsFadingOut())
		{
			return;
		}
		if (_aggregate.DamageOverTime > 0f)
		{
			foreach (StatusEffect item2 in _currentEffects.Keys.Where((StatusEffect x) => x.Template.Type == StatusEffectTemplate.EffectType.DamageOverTime).ToList())
			{
				if (Agent == null || !Agent.IsActive() || Agent.Health < 1f || Agent.IsFadingOut())
				{
					break;
				}
				int num = (int)item2.Template.BaseEffectValue;
				if (num > 0)
				{
					float health = Agent.Health;
					if (item2.Template.DamageType == DamageType.Fire)
					{
						SotorDamageHelper.ApplyFireDamageOverTime(Agent, num, item2.ApplierAgent);
					}
					else
					{
						SotorDamageHelper.ApplyDamageOverTime(Agent, num, item2.ApplierAgent);
					}
					int num2 = (int)(health - Agent.Health);
					bool killed = health > 0f && (!Agent.IsActive() || Agent.Health < 1f);
					SotorLog.Info($"StatusEffect DoT tick: '{Agent?.Name}' takes {num2} from '{item2.OriginSpellName ?? item2.Template.StringID}' (health now {Agent?.Health:0}).");
					if (num2 > 0 && item2.ApplierAgent != null)
					{
						SotorSpellDamageLog.BookHit(item2.ApplierAgent, Agent, item2.Template.DamageType, num2, killed, item2.OriginSpellName);
					}
				}
			}
			return;
		}
		if (!(_aggregate.HealthOverTime > 0f))
		{
			return;
		}
		int num3 = (int)_aggregate.HealthOverTime;
		float health2 = Agent.Health;
		Agent.Health = Math.Min(Agent.Health + (float)num3, Agent.HealthLimit);
		float num4 = Agent.Health - health2;
		SotorLog.Info($"StatusEffect heal tick: '{Agent?.Name}' +{num3} HP ({health2:0} -> {Agent.Health:0} / {Agent.HealthLimit:0}).");
		if (num4 > 0f)
		{
			StatusEffect statusEffect = _currentEffects.Keys.FirstOrDefault((StatusEffect x) => x.Template.Type == StatusEffectTemplate.EffectType.HealthOverTime);
			Agent obj = statusEffect?.ApplierAgent;
			Hero hero = obj.GetHero();
			if (hero != null)
			{
				SotorSpellcraftHelper.GrantAbilityOutcomeXp(hero, (int)num4 / 5, singleTarget: false);
			}
			SotorSpellDamageLog.BookHeal(obj, Agent, (int)num4, statusEffect?.OriginSpellName);
		}
	}

	private void CalculateAggregate()
	{
		_aggregate = new Aggregate();
		foreach (StatusEffect key in _currentEffects.Keys)
		{
			_aggregate.AddEffect(key.Template);
		}
		bool flag = _aggregate.SpeedProperties != 0f || _aggregate.AttackSpeedProperties != 0f;
		if ((flag || _hadSpeedModifier) && Agent != null && Agent.IsActive())
		{
			Agent.UpdateAgentProperties();
		}
		_hadSpeedModifier = flag;
	}

	public float[] GetResistances(AttackTypeMask mask)
	{
		return (_aggregate ?? (_aggregate = new Aggregate())).Resistances[mask];
	}

	public float[] GetAmplifiers(AttackTypeMask mask)
	{
		return (_aggregate ?? (_aggregate = new Aggregate())).DamageAmplifications[mask];
	}

	public float GetMovementSpeedModifier()
	{
		return (_aggregate ?? (_aggregate = new Aggregate())).SpeedProperties;
	}

	public float GetAttackSpeedModifier()
	{
		return (_aggregate ?? (_aggregate = new Aggregate())).AttackSpeedProperties;
	}

	public float GetThorns()
	{
		return (_aggregate ?? (_aggregate = new Aggregate())).Thorns;
	}

	private static bool IsMeleeWeaponClass(WeaponClass wc)
	{
		if ((uint)(wc - 1) <= 5u || (uint)(wc - 8) <= 3u)
		{
			return true;
		}
		return false;
	}

	private void AddEffect(StatusEffect effect)
	{
		GameEntity particleEntity = TryAttachParticle(effect.Template.ParticleId, effect.Template.ApplyToWeapon, effect.Template.DoNotAttachToSkeleton);
		_currentEffects.Add(effect, new EffectData(effect, particleEntity));
		if (effect.Template.ApplyToWeapon)
		{
			_hasWeaponAttachedEffect = true;
			if (Agent != null && Agent.IsActive())
			{
				_weaponFxWieldedIndex = Agent.GetPrimaryWieldedItemIndex();
			}
		}
		CalculateAggregate();
	}

	private GameEntity TryAttachParticle(string particleId, bool weaponAttach, bool groundEffect = false)
	{
		string text = particleId?.Trim();
		if (string.IsNullOrWhiteSpace(text) || text.Equals("none", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		if (!HasUsableVisuals())
		{
			return null;
		}
		try
		{
			Scene scene = Mission.Current?.Scene;
			if (scene == null)
			{
				return null;
			}
			if (weaponAttach)
			{
				Skeleton skeleton = Agent.AgentVisuals?.GetSkeleton();
				if (skeleton == null || !skeleton.IsValid)
				{
					return null;
				}
				sbyte b = (sbyte)((Agent.Monster != null) ? Agent.Monster.MainHandItemBoneIndex : (-1));
				if (b < 0 || b >= skeleton.GetBoneCount())
				{
					return null;
				}
				EquipmentIndex primaryWieldedItemIndex = Agent.GetPrimaryWieldedItemIndex();
				if (primaryWieldedItemIndex == EquipmentIndex.None)
				{
					return null;
				}
				MissionWeapon missionWeapon = Agent.Equipment[primaryWieldedItemIndex];
				if (missionWeapon.IsEmpty || missionWeapon.CurrentUsageItem == null)
				{
					return null;
				}
				if (!IsMeleeWeaponClass(missionWeapon.CurrentUsageItem.WeaponClass))
				{
					return null;
				}
				float num = missionWeapon.CurrentUsageItem.GetRealWeaponLength();
				if (num <= 0.1f)
				{
					num = 1f;
				}
				float num2 = num * 0.3f;
				int num3 = (int)((num - num2) / 0.1f);
				if (num3 < 1)
				{
					num3 = 1;
				}
				GameEntity gameEntity = null;
				for (int i = 0; i < num3; i++)
				{
					GameEntity gameEntity2 = GameEntity.CreateEmpty(scene);
					MatrixFrame boneLocalFrame = MatrixFrame.Identity;
					boneLocalFrame.Elevate(num2 + (float)i * 0.1f);
					ParticleSystem particleSystem = ParticleSystem.CreateParticleSystemAttachedToEntity(text, gameEntity2, ref boneLocalFrame);
					if (particleSystem == null)
					{
						gameEntity2.Remove(0);
						continue;
					}
					Agent.AgentVisuals.AddChildEntity(gameEntity2);
					skeleton.AddComponentToBone(b, particleSystem);
					if (gameEntity == null)
					{
						gameEntity = gameEntity2;
					}
					else
					{
						_extraCarriers.Add(gameEntity2);
					}
				}
				return gameEntity;
			}
			if (groundEffect)
			{
				GameEntity gameEntity3 = GameEntity.CreateEmpty(scene);
				gameEntity3.SetGlobalFrame(new MatrixFrame(Mat3.Identity, Agent.GetChestGlobalPosition()));
				MatrixFrame boneLocalFrame2 = MatrixFrame.Identity;
				if (ParticleSystem.CreateParticleSystemAttachedToEntity(text, gameEntity3, ref boneLocalFrame2) == null)
				{
					gameEntity3.Remove(0);
					return null;
				}
				_groundEntities.Add(gameEntity3);
				return gameEntity3;
			}
			Skeleton skeleton2 = Agent.AgentVisuals?.GetSkeleton();
			if (skeleton2 == null || !skeleton2.IsValid)
			{
				return null;
			}
			GameEntity gameEntity4 = GameEntity.CreateEmpty(scene);
			MatrixFrame boneLocalFrame3 = MatrixFrame.Identity;
			ParticleSystem particleSystem2 = ParticleSystem.CreateParticleSystemAttachedToEntity(text, gameEntity4, ref boneLocalFrame3);
			if (particleSystem2 == null)
			{
				gameEntity4.Remove(0);
				return null;
			}
			Agent.AgentVisuals.AddChildEntity(gameEntity4);
			sbyte b2 = 1;
			if (b2 < skeleton2.GetBoneCount())
			{
				skeleton2.AddComponentToBone(b2, particleSystem2);
			}
			return gameEntity4;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("StatusEffectComponent.TryAttachParticle('" + text + "') failed: " + ex.Message);
			return null;
		}
	}

	private void RemoveEffect(StatusEffect effect)
	{
		if (_currentEffects.TryGetValue(effect, out var value))
		{
			RemoveVisuals(value);
			_currentEffects.Remove(effect);
			SotorLog.Info("StatusEffect '" + effect.Template.StringID + "' expired on '" + Agent?.Name + "'.");
		}
	}

	private void RemoveVisuals(EffectData data)
	{
		if (data?.ParticleEntity == null)
		{
			return;
		}
		try
		{
			data.ParticleEntity.RemoveAllParticleSystems();
			if (_groundEntities.Remove(data.ParticleEntity))
			{
				data.ParticleEntity.Remove(0);
			}
			else if (HasUsableVisuals())
			{
				Agent.AgentVisuals.RemoveChildEntity(data.ParticleEntity, 0);
			}
			else
			{
				data.ParticleEntity.Remove(0);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("StatusEffectComponent.RemoveVisuals failed: " + ex.Message);
		}
		data.ParticleEntity = null;
	}

	public override void OnAgentRemoved()
	{
		CleanUp();
	}

	public override void OnComponentRemoved()
	{
		CleanUp();
	}

	private void CleanUp()
	{
		foreach (EffectData item in _currentEffects.Values.ToList())
		{
			RemoveVisuals(item);
		}
		foreach (GameEntity extraCarrier in _extraCarriers)
		{
			try
			{
				extraCarrier?.RemoveAllParticleSystems();
				if (HasUsableVisuals())
				{
					Agent.AgentVisuals.RemoveChildEntity(extraCarrier, 0);
				}
				else
				{
					extraCarrier?.Remove(0);
				}
			}
			catch
			{
			}
		}
		_extraCarriers.Clear();
		_currentEffects.Clear();
		_aggregate = null;
		_disabled = true;
	}

	public void Dispose()
	{
		CleanUp();
	}
}
