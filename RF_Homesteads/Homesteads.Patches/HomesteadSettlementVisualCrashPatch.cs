using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(SettlementVisualManager), "GetSettlementVisual")]
internal static class HomesteadSettlementVisualCrashPatch
{
	private static FieldInfo? _settlementVisualsField;

	private static MethodInfo? _addNewPartyVisualForPartyMethod;

	[HarmonyPrefix]
	private static void Prefix(SettlementVisualManager __instance, Settlement settlement)
	{
		try
		{
			if (settlement == null || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_"))
			{
				return;
			}
			PartyBase party = settlement.Party;
			if (party == null)
			{
				return;
			}
			if ((object)_settlementVisualsField == null)
			{
				_settlementVisualsField = AccessTools.Field(typeof(SettlementVisualManager), "_settlementVisuals");
			}
			if (_settlementVisualsField?.GetValue(__instance) is IDictionary dictionary && !dictionary.Contains(party))
			{
				if ((object)_addNewPartyVisualForPartyMethod == null)
				{
					_addNewPartyVisualForPartyMethod = AccessTools.Method(typeof(SettlementVisualManager), "AddNewPartyVisualForParty", new Type[1] { typeof(PartyBase) });
				}
				if (!(_addNewPartyVisualForPartyMethod == null))
				{
					_addNewPartyVisualForPartyMethod.Invoke(__instance, new object[1] { party });
					TraceLogger.Write("HomesteadSettlementVisualCrashPatch", "'" + settlement.StringId + "' had no registered SettlementVisual — self-healed by registering it now instead of crashing.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementVisualCrashPatch", "Prefix failed: " + ex.Message);
		}
	}
}
