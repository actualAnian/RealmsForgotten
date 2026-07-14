using System;
using HarmonyLib;
using Homesteads.MissionLogics;
using TaleWorlds.MountAndBlade;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(Agent), "RegisterBlow")]
internal static class HomesteadBallistaFriendlyFirePatch
{
	[HarmonyPrefix]
	private static void Prefix(Agent __instance, ref Blow blow, in AttackCollisionData collisionData)
	{
		try
		{
			if (collisionData.IsMissile && (blow.InflictedDamage > 0 || blow.SelfInflictedDamage > 0) && HomesteadBallistaTurretController.IsTrackedBallistaMissile(collisionData.AffectorWeaponSlotOrMissileIndex))
			{
				Team team = __instance?.Team;
				Team team2 = Mission.Current?.PlayerTeam;
				if (team != null && team2 != null && !team.IsEnemyOf(team2))
				{
					blow.InflictedDamage = 0;
					blow.SelfInflictedDamage = 0;
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBallistaFriendlyFirePatch", "Prefix failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
