using System;
using HarmonyLib;
using TaleWorlds.Core;

namespace SOTOR.AbilitySystem;

[HarmonyPatch(typeof(Game), "InitializeDefaultGameObjects")]
public static class SotorSkillsPatch
{
	public static void Postfix()
	{
		try
		{
			new SotorSkills();
		}
		catch (Exception ex)
		{
			SotorLog.Error("SotorSkillsPatch failed to register skills: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
