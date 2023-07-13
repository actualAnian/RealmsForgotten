using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RFEffects
{
	// Token: 0x02000003 RID: 3
	public class AnoritDamageParticleModel : DefaultDamageParticleModel
	{
		// Token: 0x0600000C RID: 12 RVA: 0x000023A4 File Offset: 0x000005A4
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

		// Token: 0x0600000D RID: 13 RVA: 0x00002488 File Offset: 0x00000688
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

		// Token: 0x0600000E RID: 14 RVA: 0x00002515 File Offset: 0x00000715
		public override void GetMeleeAttackSweatParticles(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData, out HitParticleResultData particleResultData)
		{
			particleResultData.StartHitParticleIndex = this._sweatStartHitParticleIndex;
			particleResultData.ContinueHitParticleIndex = this._sweatContinueHitParticleIndex;
			particleResultData.EndHitParticleIndex = this._sweatEndHitParticleIndex;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x0000253E File Offset: 0x0000073E
		public override int GetMissileAttackParticle(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData)
		{
			if (blow.WeaponRecord.WeaponFlags.HasFlag(WeaponFlags.AffectsAreaBig))
			{
				this.AddToTheListOfFlame(attacker, victim);
				return this._fireMissileHitParticleIndex;
			}
			return this._missileHitParticleIndex;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002578 File Offset: 0x00000778
		private void AddToTheListOfFlame(Agent attacker, Agent victim)
		{
			AnoritMissionBehaviour missionBehavior = Mission.Current.GetMissionBehavior<AnoritMissionBehaviour>();
			missionBehavior.toBeAdded.Add(victim);
			if (!missionBehavior.attackerId.ContainsKey(victim.Index))
			{
				missionBehavior.attackerId.Add(victim.Index, attacker.Index);
				return;
			}
			missionBehavior.attackerId[victim.Index] = attacker.Index;
		}

		// Token: 0x04000001 RID: 1
		private readonly int _bloodStartHitParticleIndex = -1;

		// Token: 0x04000002 RID: 2
		private readonly int _bloodContinueHitParticleIndex = -1;

		// Token: 0x04000003 RID: 3
		private readonly int _bloodEndHitParticleIndex = -1;

		// Token: 0x04000004 RID: 4
		private readonly int _sweatStartHitParticleIndex = -1;

		// Token: 0x04000005 RID: 5
		private readonly int _sweatContinueHitParticleIndex = -1;

		// Token: 0x04000006 RID: 6
		private readonly int _sweatEndHitParticleIndex = -1;

		// Token: 0x04000007 RID: 7
		private readonly int _missileHitParticleIndex = -1;

		// Token: 0x04000008 RID: 8
		private readonly int _fireStartHitParticleIndex = -1;

		// Token: 0x04000009 RID: 9
		private readonly int _fireMissileHitParticleIndex = -1;
	}
}
