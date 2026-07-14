using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(GarrisonPartyComponent), "OnInitialize")]
internal static class HomesteadGarrisonInitializeCrashPatch
{
	private static FieldInfo? _settlementBackingField;

	[HarmonyPrefix]
	private static bool Prefix(GarrisonPartyComponent __instance)
	{
		try
		{
			Settlement settlement = __instance.Settlement;
			if (settlement == null || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			if (settlement.Town != null)
			{
				return true;
			}
			Settlement settlement2 = MBObjectManager.Instance.GetObject<Settlement>(settlement.StringId);
			if (settlement2?.Town != null)
			{
				settlement2.Town.GarrisonPartyComponent = __instance;
				if ((object)_settlementBackingField == null)
				{
					_settlementBackingField = AccessTools.Field(typeof(GarrisonPartyComponent), "<Settlement>k__BackingField");
				}
				if (_settlementBackingField != null)
				{
					_settlementBackingField.SetValue(__instance, settlement2);
					TraceLogger.Write("HomesteadGarrisonInitializeCrashPatch", "Re-resolved stale GarrisonPartyComponent.Settlement for '" + settlement.StringId + "' onto the canonical settlement object (both directions).");
				}
				else
				{
					TraceLogger.Write("HomesteadGarrisonInitializeCrashPatch", "Re-resolved '" + settlement.StringId + "' onto the canonical settlement object, but could not locate the Settlement backing field to fix the forward reference — this party's OwnerClan/MapFaction fallbacks may keep firing all session.");
				}
			}
			else
			{
				TraceLogger.Write("HomesteadGarrisonInitializeCrashPatch", "GarrisonPartyComponent.OnInitialize: '" + settlement.StringId + "' has a stale Settlement reference (Town == null) and no canonical replacement could be found — skipping to avoid a crash.");
			}
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadGarrisonInitializeCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
