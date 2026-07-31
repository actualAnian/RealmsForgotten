using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace SOTOR.AbilitySystem;

[HarmonyPatch(typeof(Campaign), "InitializeDefaultCampaignObjects")]
public static class SotorPerksPatch
{
	public static void Postfix()
	{
		try
		{
			new SotorPerks();
		}
		catch (Exception ex)
		{
			SotorLog.Error("SotorPerksPatch failed to register perks: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
