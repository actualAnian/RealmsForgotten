using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;

namespace SOTOR.AbilitySystem;

[HarmonyPatch(typeof(DefaultCombatSimulationModel), "GetBattleAdvantage")]
public static class SotorWellControlledSimPatch
{
	private const float WellControlledSimBonus = 0.05f;

	public static void Postfix(MapEvent mapEvent, ref ExplainedNumber defenderAdvantage, ref ExplainedNumber attackerAdvantage)
	{
		try
		{
			PerkObject wellControlled = SotorPerks.WellControlled;
			if (wellControlled != null && mapEvent != null)
			{
				Hero hero = mapEvent.DefenderSide?.LeaderParty?.LeaderHero;
				if (hero != null && hero.GetPerkValue(wellControlled))
				{
					defenderAdvantage.Add(0.05f);
				}
				Hero hero2 = mapEvent.AttackerSide?.LeaderParty?.LeaderHero;
				if (hero2 != null && hero2.GetPerkValue(wellControlled))
				{
					attackerAdvantage.Add(0.05f);
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorWellControlledSimPatch failed: " + ex.Message);
		}
	}
}
