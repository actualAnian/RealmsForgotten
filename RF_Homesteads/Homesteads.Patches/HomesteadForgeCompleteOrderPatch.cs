using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(WeaponDesignVM), "ExecuteFinalizeCrafting")]
internal static class HomesteadForgeCompleteOrderPatch
{
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo getCurrent = AccessTools.PropertyGetter(typeof(Settlement), "CurrentSettlement");
		MethodInfo replacement = AccessTools.Method(typeof(HomesteadForgeContext), "CurrentOrBackingSettlement");
		int replaced = 0;
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.Calls(getCurrent))
			{
				yield return new CodeInstruction(OpCodes.Call, replacement);
				replaced++;
			}
			else
			{
				yield return instruction;
			}
		}
		if (replaced == 0)
		{
			TraceLogger.Write("HomesteadForgeCompleteOrderPatch", "Transpiler replaced 0 CurrentSettlement reads — completion may NRE off-settlement.");
		}
	}
}
