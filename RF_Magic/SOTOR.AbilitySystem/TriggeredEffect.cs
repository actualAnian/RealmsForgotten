using System;
using System.Collections.Generic;
using SOTOR.AbilitySystem.StatusEffects;
using SOTOR.AbilitySystem.TriggeredScripts;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class TriggeredEffect
{
	private readonly TriggeredEffectTemplate _template;

	public bool OwnerIsSingleTarget { get; set; }

	public string OwnerSpellName { get; set; }

	public string OwnerShipTag { get; set; } = "untagged";

	public int OwnerSpellTier { get; set; }

	public TriggeredEffect(TriggeredEffectTemplate template)
	{
		_template = template;
	}

	public void Trigger(Vec3 position, Vec3 normal, Agent triggererAgent, MBList<Agent> targets = null, float damageMultiplier = 1f)
	{
		if (_template == null || triggererAgent == null || !triggererAgent.IsActive())
		{
			return;
		}
		// [RF-B] perks "Staff" de Arcane do RF multiplicam o RAIO de area. Aplicado
		// AQUI, na variavel que alimenta a selecao de alvos e todo o resto do efeito,
		// para valer em todos os caminhos (dano, status, particula).
		float radius = _template.Radius * SOTOR.RFIntegration.RFArcanePerks.GetRadiusFactor(triggererAgent.GetHero());
		if (targets == null)
		{
			targets = new MBList<Agent>();
			switch (_template.TargetType)
			{
			case TargetType.Self:
				targets.Add(triggererAgent);
				break;
			case TargetType.Enemy:
				targets = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, radius, triggererAgent.Team, targets);
				break;
			case TargetType.Friendly:
				targets = Mission.Current.GetNearbyAllyAgents(position.AsVec2, radius, triggererAgent.Team, targets);
				break;
			default:
				targets = Mission.Current.GetNearbyAgents(position.AsVec2, radius, targets);
				break;
			}
		}
		targets = NormalizeTriggeredTargets(targets);
		PlaySound(position);
		if (_template.DamageAmount > 0)
		{
			int minDamage = (int)((float)_template.DamageAmount * (1f - _template.DamageVariance) * damageMultiplier);
			int maxDamage = (int)((float)_template.DamageAmount * (1f + _template.DamageVariance) * damageMultiplier);
			SotorDamageHelper.DamageAgents(targets, minDamage, maxDamage, triggererAgent, _template, _template.HasShockWave, position, OwnerIsSingleTarget, OwnerSpellName);
		}
		TryDamageShip(position, triggererAgent, targets, damageMultiplier);
		float num = _template.ImbuedStatusEffectDuration;
		if (_template.ImbuedStatusEffectDuration > 1.5f)
		{
			Hero hero = triggererAgent.GetHero();
			if (hero != null)
			{
				num *= SotorSpellcraftHelper.GetSpellDurationFactor(hero);
			}
		}
		ApplyStatusEffects(targets, triggererAgent, num);
		RunTriggeredScript(position, triggererAgent, targets, num);
		SpawnVisuals(position, normal);
	}

	private void TryDamageShip(Vec3 position, Agent triggererAgent, MBList<Agent> targets, float damageMultiplier)
	{
		try
		{
			Mission current = Mission.Current;
			if (!SotorNavalBridge.IsNavalMission(current) || string.IsNullOrEmpty(OwnerShipTag) || OwnerShipTag.Equals("untagged", StringComparison.OrdinalIgnoreCase) || !SotorSettings.EnableSpellShipDamage || _template.DamageAmount <= 0)
			{
				return;
			}
			float spellShipDamageMultiplier = SotorSettings.SpellShipDamageMultiplier;
			if (spellShipDamageMultiplier <= 0f)
			{
				return;
			}
			float tierWeight = SotorNavalBridge.GetTierWeight(OwnerSpellTier);
			float spellcraftDamageFactorFor = SotorDamageHelper.GetSpellcraftDamageFactorFor(triggererAgent);
			float num = (float)_template.DamageAmount * tierWeight * spellcraftDamageFactorFor * spellShipDamageMultiplier;
			if (num <= 0f)
			{
				return;
			}
			int nearbyAgentHintIndex = -1;
			if (targets != null)
			{
				for (int i = 0; i < targets.Count; i++)
				{
					if (targets[i] != null && targets[i].IsActive())
					{
						nearbyAgentHintIndex = targets[i].Index;
						break;
					}
				}
			}
			SotorNavalBridge.ApplyShipDamage(current, triggererAgent, position, OwnerShipTag, num, nearbyAgentHintIndex, _template.DamageType, OwnerSpellName);
		}
		catch (Exception ex)
		{
			SotorLog.Warn("TriggeredEffect.TryDamageShip failed: " + ex.Message);
		}
	}

	private void RunTriggeredScript(Vec3 position, Agent triggererAgent, MBList<Agent> targets, float scaledDuration)
	{
		ITriggeredScript triggeredScript = TriggeredScriptRegistry.Resolve(_template.ScriptNameToTrigger);
		if (triggeredScript == null)
		{
			return;
		}
		try
		{
			triggeredScript.OnTrigger(position, triggererAgent, targets, scaledDuration, _template, OwnerSpellName);
		}
		catch (Exception ex)
		{
			SotorLog.Warn("TriggeredEffect script '" + _template.ScriptNameToTrigger + "' failed: " + ex.Message);
		}
	}

	private void ApplyStatusEffects(MBList<Agent> targets, Agent triggererAgent, float scaledDuration)
	{
		if (_template.ImbuedStatusEffects == null || _template.ImbuedStatusEffects.Count == 0)
		{
			return;
		}
		bool flag = _template.TargetType == TargetType.Friendly || _template.TargetType == TargetType.FriendlyHero;
		bool flag2 = false;
		if (targets != null)
		{
			foreach (Agent target in targets)
			{
				if (target != null && target != triggererAgent)
				{
					flag2 = true;
					break;
				}
			}
		}
		if (flag && flag2 && targets != null && !targets.Contains(triggererAgent) && SotorPerks.ArcaneLink != null)
		{
			Hero hero = triggererAgent.GetHero();
			if (hero != null && hero.GetPerkValue(SotorPerks.ArcaneLink))
			{
				targets.Add(triggererAgent);
				SotorLog.Debug($"ArcaneLink: caster '{triggererAgent.Name}' added to ally-buff targets (buffed {targets.Count - 1} ally/allies).");
			}
		}
		Hero hero2 = triggererAgent.GetHero();
		HashSet<int> hashSet = ((hero2 != null) ? new HashSet<int>() : null);
		foreach (string imbuedStatusEffect in _template.ImbuedStatusEffects)
		{
			if (string.IsNullOrWhiteSpace(imbuedStatusEffect))
			{
				continue;
			}
			foreach (Agent target2 in targets)
			{
				if (target2 != null && target2.IsActive() && !(target2.Health < 1f) && !target2.IsFadingOut())
				{
					StatusEffectComponent component = target2.GetComponent<StatusEffectComponent>();
					if (component != null && component.RunStatusEffect(imbuedStatusEffect, triggererAgent, scaledDuration, append: true, OwnerSpellName) && hashSet != null && hashSet.Add(target2.Index))
					{
						SotorSpellcraftHelper.GrantAbilityOutcomeXp(hero2, 10, OwnerIsSingleTarget);
					}
				}
			}
		}
	}

	private static MBList<Agent> NormalizeTriggeredTargets(IEnumerable<Agent> rawTargets)
	{
		MBList<Agent> mBList = new MBList<Agent>();
		if (rawTargets == null)
		{
			return mBList;
		}
		Mission current = Mission.Current;
		HashSet<int> hashSet = new HashSet<int>();
		foreach (Agent rawTarget in rawTargets)
		{
			if (rawTarget != null && rawTarget.IsActive() && rawTarget.Health >= 1f && !rawTarget.IsFadingOut() && (current == null || current.FindAgentWithIndex(rawTarget.Index) == rawTarget) && hashSet.Add(rawTarget.Index))
			{
				mBList.Add(rawTarget);
			}
		}
		return mBList;
	}

	private void SpawnVisuals(Vec3 position, Vec3 normal)
	{
		string text = _template?.BurstParticleEffectPrefab?.Trim();
		if (string.IsNullOrWhiteSpace(text) || text.Equals("none", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		try
		{
			GameEntity gameEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
			MatrixFrame boneLocalFrame = MatrixFrame.Identity;
			ParticleSystem.CreateParticleSystemAttachedToEntity(text, gameEntity, ref boneLocalFrame);
			Vec3 direction = normal;
			if (Math.Abs(direction.x) + Math.Abs(direction.y) + Math.Abs(direction.z) < 0.0001f)
			{
				direction = Vec3.Forward;
			}
			gameEntity.SetGlobalFrame(new MatrixFrame(Mat3.CreateMat3WithForward(in direction), in position));
			gameEntity.FadeOut(_template.SoundEffectLength, isRemovingFromScene: true);
		}
		catch (Exception ex)
		{
			SotorLog.Warn("TriggeredEffect.SpawnVisuals('" + text + "') failed: " + ex.Message);
		}
	}

	private void PlaySound(Vec3 position)
	{
		string text = _template?.SoundEffectId?.Trim();
		if (string.IsNullOrWhiteSpace(text) || text.Equals("none", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		try
		{
			int eventIdFromString = SoundEvent.GetEventIdFromString(text);
			if (eventIdFromString >= 0)
			{
				Mission.Current.MakeSound(eventIdFromString, position, soundCanBePredicted: false, isReliable: false, -1, -1);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("TriggeredEffect.PlaySound('" + text + "') failed: " + ex.Message);
		}
	}
}
