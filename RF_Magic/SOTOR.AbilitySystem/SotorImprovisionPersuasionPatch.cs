using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Library;

namespace SOTOR.AbilitySystem;

[HarmonyPatch(typeof(DefaultPersuasionModel), "GetChances")]
public static class SotorImprovisionPersuasionPatch
{
	private const float ImprovisionPersuasionBonus = 0.1f;

	public static void Postfix(ref float successChance)
	{
		try
		{
			PerkObject improvision = SotorPerks.Improvision;
			if (improvision != null && Hero.MainHero != null && Hero.MainHero.GetPerkValue(improvision))
			{
				successChance = MBMath.ClampFloat(successChance * 1.1f, 0f, 1f);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorImprovisionPersuasionPatch failed: " + ex.Message);
		}
	}
}
