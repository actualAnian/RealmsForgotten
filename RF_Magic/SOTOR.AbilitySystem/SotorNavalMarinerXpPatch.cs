using System;
using System.Reflection;
using HarmonyLib;

namespace SOTOR.AbilitySystem;

public static class SotorNavalMarinerXpPatch
{
	private static bool _applied;

	public static void ApplyIfNeeded()
	{
		if (_applied)
		{
			return;
		}
		_applied = true;
		try
		{
			MethodInfo methodInfo = SotorNavalBridge.ResolveNavalOnCombatHitMethod();
			if (methodInfo == null)
			{
				SotorLog.Info("SotorNavalMarinerXpPatch: OnCombatHit not found (no War Sails?) — Mariner guard inactive.");
				return;
			}
			Harmony harmonyInstance = SubModule.HarmonyInstance;
			if (harmonyInstance == null)
			{
				SotorLog.Warn("SotorNavalMarinerXpPatch: no Harmony instance; Mariner guard NOT applied.");
				return;
			}
			HarmonyMethod prefix = new HarmonyMethod(typeof(SotorNavalMarinerXpPatch).GetMethod("OnCombatHitPrefix", BindingFlags.Static | BindingFlags.NonPublic));
			harmonyInstance.Patch(methodInfo, prefix);
			SotorLog.Info("SotorNavalMarinerXpPatch: Mariner-XP-from-spells guard applied.");
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalMarinerXpPatch.ApplyIfNeeded failed (" + ex.GetType().Name + "): " + ex.Message);
		}
	}

	private static bool OnCombatHitPrefix()
	{
		return !SotorDamageHelper.InSpellBlow;
	}
}
