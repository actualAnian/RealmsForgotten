using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

namespace RFEffects
{
	// Token: 0x02000005 RID: 5
	internal class Patch
	{
        [HarmonyPatch(typeof(Mission), "DecideAgentHitParticles")]
		public class AKDecideAgentHitParticlesPatchForRealisticBattleMod
		{
			// Token: 0x0600001D RID: 29 RVA: 0x00002A54 File Offset: 0x00000C54
			[HarmonyPriority(0)]
			[HarmonyAfter(new string[]
			{
				"com.rbmcombat"
			})]
			private static void Postfix(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData, ref HitParticleResultData hprd)
			{
				if (!blow.WeaponRecord.WeaponFlags.HasFlag(WeaponFlags.CanKnockDown) || !blow.WeaponRecord.WeaponFlags.HasFlag(WeaponFlags.CanHook))
				{
					return;
				}
				int runtimeIdByName = ParticleSystemManager.GetRuntimeIdByName("battleground_fire_smoke_square");
				if (hprd.StartHitParticleIndex == runtimeIdByName)
				{
					return;
				}
				if (victim == null || (blow.InflictedDamage <= 0 && (double)victim.Health > 0.0))
				{
					return;
				}
				BlowWeaponRecord weaponRecord = blow.WeaponRecord;
				bool flag;
				if (weaponRecord.HasWeapon() && !blow.WeaponRecord.WeaponFlags.HasAnyFlag(WeaponFlags.NoBlood))
				{
					AttackCollisionData attackCollisionData = collisionData;
					flag = attackCollisionData.IsAlternativeAttack;
				}
				else
				{
					flag = true;
				}
				if (flag)
				{
					return;
				}
				hprd.StartHitParticleIndex = runtimeIdByName;
				if (Harmony.HasAnyPatches("com.rbmcombat"))
				{
					return;
				}
				AnoritMissionBehaviour missionBehavior = Mission.Current.GetMissionBehavior<AnoritMissionBehaviour>();
				missionBehavior.toBeAdded.Add(victim);
				if (!missionBehavior.attackerId.ContainsKey(victim.Index))
				{
					missionBehavior.attackerId.Add(victim.Index, attacker.Index);
					return;
				}
				missionBehavior.attackerId[victim.Index] = attacker.Index;
			}
		}
	}
}
