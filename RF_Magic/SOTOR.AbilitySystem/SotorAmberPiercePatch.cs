using System;
using HarmonyLib;
using SandBox.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

[HarmonyPatch(typeof(SandboxAgentApplyDamageModel), "DecideMissileWeaponFlags")]
public static class SotorAmberPiercePatch
{
	private const ulong MultiplePenetration = 1073741824uL;

	private const ulong CanPenetrateShield = 131072uL;

	public static void Postfix(in MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags)
	{
		try
		{
			if (!missileWeapon.IsEmpty && !(missileWeapon.Item?.StringId != "sotor_amber_javelin"))
			{
				missileWeaponFlags = missileWeaponFlags | WeaponFlags.MultiplePenetration | WeaponFlags.CanPenetrateShield;
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorAmberPiercePatch failed: " + ex.Message);
		}
	}
}
