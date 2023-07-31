using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects
{
	public class AnoritDamageParticleModel : DefaultDamageParticleModel
	{
		public AnoritDamageParticleModel()
		{
			this._bloodStartHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_enter");
			this._bloodContinueHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_inside");
			this._bloodEndHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_exit");
			this._sweatStartHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
			this._sweatContinueHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
			this._sweatEndHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
			this._missileHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_enter");
			this._fireStartHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("battleground_fire_smoke_square");
			this._fireMissileHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("battleground_fire_smoke_square");
		}

		public override void GetMeleeAttackBloodParticles(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData, out HitParticleResultData particleResultData)
		{
			particleResultData.StartHitParticleIndex = this._bloodStartHitParticleIndex;
			particleResultData.ContinueHitParticleIndex = this._bloodContinueHitParticleIndex;
			particleResultData.EndHitParticleIndex = this._bloodEndHitParticleIndex;
			if (blow.WeaponRecord.WeaponFlags.HasFlag(WeaponFlags.CanKnockDown) && blow.WeaponRecord.WeaponFlags.HasFlag(WeaponFlags.CanHook))
			{
				particleResultData.StartHitParticleIndex = this._fireStartHitParticleIndex;
				this.AddToTheListOfFlame(attacker, victim);
			}
		}

		public override void GetMeleeAttackSweatParticles(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData, out HitParticleResultData particleResultData)
		{
			particleResultData.StartHitParticleIndex = this._sweatStartHitParticleIndex;
			particleResultData.ContinueHitParticleIndex = this._sweatContinueHitParticleIndex;
			particleResultData.EndHitParticleIndex = this._sweatEndHitParticleIndex;
		}
		public override int GetMissileAttackParticle(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData)
		{
			if (blow.WeaponRecord.WeaponFlags.HasFlag(WeaponFlags.AffectsAreaBig))
			{
				this.AddToTheListOfFlame(attacker, victim);
				return this._fireMissileHitParticleIndex;
			}
			return this._missileHitParticleIndex;
		}
		private void AddToTheListOfFlame(Agent attacker, Agent victim)
		{
            RFMissionBehaviour missionBehavior = Mission.Current.GetMissionBehavior<RFMissionBehaviour>();
			missionBehavior.toBeAdded.Add(victim);
			if (!missionBehavior.attackerId.ContainsKey(victim.Index))
			{
				missionBehavior.attackerId.Add(victim.Index, attacker.Index);
				return;
			}
			missionBehavior.attackerId[victim.Index] = attacker.Index;
		}
		private readonly int _bloodStartHitParticleIndex = -1;

		private readonly int _bloodContinueHitParticleIndex = -1;

		private readonly int _bloodEndHitParticleIndex = -1;

		private readonly int _sweatStartHitParticleIndex = -1;

		private readonly int _sweatContinueHitParticleIndex = -1;
		private readonly int _sweatEndHitParticleIndex = -1;

		private readonly int _missileHitParticleIndex = -1;

		private readonly int _fireStartHitParticleIndex = -1;

		private readonly int _fireMissileHitParticleIndex = -1;
	}
}
