using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(SettlementMenuOverlayVM), "UpdateProperties")]
internal static class HomesteadSettlementOverlayCrashPatch
{
	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception)
	{
		if (__exception == null)
		{
			return null;
		}
		Settlement settlement = MobileParty.MainParty?.CurrentSettlement ?? MobileParty.MainParty?.LastVisitedSettlement;
		if (settlement != null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_settlement_"))
		{
			TraceLogger.Write("HomesteadSettlementOverlayCrashPatch", "Swallowed overlay UpdateProperties error for '" + settlement.StringId + "': " + __exception.Message);
			return null;
		}
		return __exception;
	}
}
