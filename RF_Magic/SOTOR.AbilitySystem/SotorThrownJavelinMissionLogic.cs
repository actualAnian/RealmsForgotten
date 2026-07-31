using System;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class SotorThrownJavelinMissionLogic : MissionLogic
{
	private const float FullPowerFraction = 1f;

	private const float WieldSettleGrace = 0.4f;

	private bool _readied;

	private Agent _readyCaster;

	private EquipmentIndex _readySlot = EquipmentIndex.None;

	private ThrownWeaponAbility _readyAbility;

	private EquipmentIndex _preCastWieldedSlot = EquipmentIndex.None;

	private bool _throwTriggered;

	private float _wieldedNoneSince = -1f;

	public static SotorThrownJavelinMissionLogic Instance { get; private set; }

	public SotorThrownJavelinMissionLogic()
	{
		Instance = this;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		Instance = this;
	}

	protected override void OnEndMission()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void OnAmberJavelinReadied(Agent caster, ThrownWeaponAbility ability, EquipmentIndex preCastWieldedSlot)
	{
		try
		{
			base.Mission?.SetThrowingMissileSpeedModifier(1f);
			_readied = true;
			_throwTriggered = false;
			_wieldedNoneSince = -1f;
			_readyCaster = caster;
			_readyAbility = ability;
			_readySlot = caster?.GetPrimaryWieldedItemIndex() ?? EquipmentIndex.None;
			_preCastWieldedSlot = ((preCastWieldedSlot == EquipmentIndex.ExtraWeaponSlot) ? EquipmentIndex.None : preCastWieldedSlot);
			PlaySpearSound("sotor_create_amber_spear", caster);
		}
		catch (Exception ex)
		{
			SotorLog.Warn("OnAmberJavelinReadied failed: " + ex.Message);
		}
	}

	private void PlaySpearSound(string soundName, Agent at)
	{
		try
		{
			if (base.Mission != null && at != null)
			{
				int eventIdFromString = SoundEvent.GetEventIdFromString(soundName);
				if (eventIdFromString >= 0)
				{
					base.Mission.MakeSound(eventIdFromString, at.Position, soundCanBePredicted: false, isReliable: true, at.Index, -1);
				}
				else
				{
					SotorLog.Warn("Amber spear sound '" + soundName + "' not registered (eventId=-1).");
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("PlaySpearSound('" + soundName + "') failed: " + ex.Message);
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, in affectorWeapon, in blow, in attackCollisionData);
		try
		{
			if (_readied && affectedAgent != null && affectedAgent == _readyCaster && (blow.BlowFlag.HasAnyFlag(BlowFlags.KnockDown) || blow.BlowFlag.HasAnyFlag(BlowFlags.KnockBack)))
			{
				CancelReadiedJavelin("stagger/knockdown");
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorThrownJavelinMissionLogic.OnAgentHit failed: " + ex.Message);
		}
	}

	public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
	{
		base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
		try
		{
			if (shooterAgent == null || !IsAmberJavelinInSlot(shooterAgent, weaponIndex))
			{
				return;
			}
			if (_readied && shooterAgent == _readyCaster)
			{
				_readyAbility?.SetCoolDown(_readyAbility.Template.CoolDown);
				if (Game.Current?.GameType is Campaign)
				{
					_readyAbility?.GrantThrowSpellcraftXp(shooterAgent.GetHero());
				}
				PlaySpearSound("sotor_throw_amber_spear", shooterAgent);
				EquipmentIndex preCastWieldedSlot = _preCastWieldedSlot;
				_readied = false;
				_throwTriggered = false;
				_wieldedNoneSince = -1f;
				_readyCaster = null;
				_readyAbility = null;
				_readySlot = EquipmentIndex.None;
				_preCastWieldedSlot = EquipmentIndex.None;
				RestoreWieldedWeapon(shooterAgent, preCastWieldedSlot);
			}
			if (Game.Current?.GameType is Campaign)
			{
				Hero hero = shooterAgent.GetHero();
				HeroExtendedInfo heroExtendedInfo = hero?.GetExtendedInfo();
				if (hero != null && heroExtendedInfo != null)
				{
					AbilityTemplate template = AbilityFactory.GetTemplate("AmberSpearThrown");
					int num = ((template != null) ? hero.GetEffectiveWindsCostForSpell(template) : 3);
					hero.SetWindsOfMagic(Math.Max(0f, hero.GetWindsOfMagic() - (float)num));
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorThrownJavelinMissionLogic.OnAgentShootMissile failed: " + ex.Message);
		}
	}

	public void DriveThrowFlagsFromControlTick()
	{
		if (!_readied || _readyCaster == null)
		{
			return;
		}
		if (!_readyCaster.IsActive() || !IsAmberJavelinInSlot(_readyCaster, _readySlot))
		{
			_readied = false;
			_throwTriggered = false;
			_wieldedNoneSince = -1f;
			_readyCaster = null;
			_readyAbility = null;
			_readySlot = EquipmentIndex.None;
			return;
		}
		EquipmentIndex primaryWieldedItemIndex = _readyCaster.GetPrimaryWieldedItemIndex();
		bool flag = primaryWieldedItemIndex != _readySlot && primaryWieldedItemIndex >= EquipmentIndex.WeaponItemBeginSlot && primaryWieldedItemIndex <= EquipmentIndex.ExtraWeaponSlot;
		if (!_throwTriggered && flag)
		{
			SotorLog.Debug($"Amber javelin: weapon-swap detected (wielded {primaryWieldedItemIndex}); vanishing readied javelin.");
			CancelReadiedJavelin("weapon swap", restoreWeapon: false);
			return;
		}
		if (!_throwTriggered && primaryWieldedItemIndex == EquipmentIndex.None)
		{
			float num = ((base.Mission != null) ? base.Mission.CurrentTime : 0f);
			if (_wieldedNoneSince < 0f)
			{
				_wieldedNoneSince = num;
			}
			else if (!(num - _wieldedNoneSince < 0.4f))
			{
				SotorLog.Debug($"Amber javelin: wield did not seat within {0.4f:0.0}s; cancelling cleanly.");
				CancelReadiedJavelin("wield stuck (None persisted)");
			}
			return;
		}
		_wieldedNoneSince = -1f;
		if (Input.IsKeyPressed(InputKey.RightMouseButton))
		{
			CancelReadiedJavelin("right-click");
			return;
		}
		try
		{
			if (!_throwTriggered)
			{
				if (Input.IsKeyPressed(InputKey.LeftMouseButton))
				{
					_throwTriggered = true;
					_readyCaster.MovementFlags &= ~Agent.MovementControlFlag.AttackMask;
					return;
				}
				Agent.MovementControlFlag movementControlFlag = _readyCaster.AttackDirectionToMovementFlag(_readyCaster.GetAttackDirection());
				if ((movementControlFlag & Agent.MovementControlFlag.AttackMask) == 0)
				{
					movementControlFlag = Agent.MovementControlFlag.AttackUp;
				}
				Agent.MovementControlFlag movementControlFlag2 = (Agent.MovementControlFlag)((uint)_readyCaster.MovementFlags & 0xFFFFFC3Fu);
				_readyCaster.MovementFlags = movementControlFlag2 | movementControlFlag;
			}
			else
			{
				_readyCaster.MovementFlags &= ~Agent.MovementControlFlag.AttackMask;
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("Javelin cock/throw tick failed: " + ex.Message);
		}
	}

	public void CancelReadiedJavelin(string reason, bool restoreWeapon = true)
	{
		if (!_readied || _readyCaster == null)
		{
			return;
		}
		try
		{
			Agent readyCaster = _readyCaster;
			EquipmentIndex readySlot = _readySlot;
			EquipmentIndex preCastWieldedSlot = _preCastWieldedSlot;
			SotorLog.Debug($"Amber javelin cancelled ({reason}); restoreWeapon={restoreWeapon}.");
			_readied = false;
			_throwTriggered = false;
			_wieldedNoneSince = -1f;
			_readyCaster = null;
			_readyAbility = null;
			_readySlot = EquipmentIndex.None;
			_preCastWieldedSlot = EquipmentIndex.None;
			readyCaster.MovementFlags &= ~Agent.MovementControlFlag.AttackMask;
			readyCaster.RemoveEquippedWeapon(readySlot);
			base.Mission?.SetThrowingMissileSpeedModifier(1f);
			if (restoreWeapon)
			{
				RestoreWieldedWeapon(readyCaster, preCastWieldedSlot);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("Javelin cancel (" + reason + ") failed: " + ex.Message);
		}
	}

	private void RestoreWieldedWeapon(Agent caster, EquipmentIndex slot)
	{
		if (caster != null && caster.IsActive() && slot >= EquipmentIndex.WeaponItemBeginSlot && slot <= EquipmentIndex.ExtraWeaponSlot && !caster.Equipment[slot].IsEmpty)
		{
			caster.TryToWieldWeaponInSlot(slot, Agent.WeaponWieldActionType.WithAnimation, isWieldedOnSpawn: false);
		}
	}

	public override void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction, Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex)
	{
		base.OnMissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex);
		try
		{
			if (collisionReaction == Mission.MissileCollisionReaction.PassThrough)
			{
				return;
			}
			if (base.Mission?.MissilesList != null)
			{
				foreach (Mission.Missile missiles in base.Mission.MissilesList)
				{
					if (missiles != null && missiles.ShooterAgent == attackerAgent && !missiles.Weapon.IsEmpty && !(missiles.Weapon.Item?.StringId != "sotor_amber_javelin"))
					{
						missiles.Entity?.SetVisibilityExcludeParents(visible: false);
					}
				}
			}
			if (attachedAgent == null)
			{
				return;
			}
			for (int num = attachedAgent.GetAttachedWeaponsCount() - 1; num >= 0; num--)
			{
				MissionWeapon attachedWeapon = attachedAgent.GetAttachedWeapon(num);
				if (!attachedWeapon.IsEmpty && attachedWeapon.Item?.StringId == "sotor_amber_javelin")
				{
					attachedAgent.DeleteAttachedWeapon(num);
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorThrownJavelinMissionLogic.OnMissileCollisionReaction failed: " + ex.Message);
		}
	}

	private static bool IsAmberJavelinInSlot(Agent agent, EquipmentIndex slot)
	{
		if (agent == null || slot < EquipmentIndex.WeaponItemBeginSlot || slot > EquipmentIndex.ExtraWeaponSlot)
		{
			return false;
		}
		MissionWeapon missionWeapon = agent.Equipment[slot];
		if (!missionWeapon.IsEmpty)
		{
			return missionWeapon.Item?.StringId == "sotor_amber_javelin";
		}
		return false;
	}
}
