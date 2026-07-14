using System;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.Encounters;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(PlayerEncounter), "Init")]
internal class HomesteadEncounterInitPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		try
		{
			if (!HomesteadBattleContext.SuppressEncounterReinit)
			{
				HomesteadBehavior.Instance?.TryReapplyHomesteadDefenderSetup();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadEncounterInitPatch", "Exception in PlayerEncounter.Init postfix: " + ex.Message);
		}
	}
}
