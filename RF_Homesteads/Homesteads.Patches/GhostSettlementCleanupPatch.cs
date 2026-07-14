using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(CampaignObjectManager), "AfterLoad")]
internal class GhostSettlementCleanupPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		if (Campaign.Current?.Settlements == null)
		{
			return;
		}
		List<Settlement> list = Campaign.Current.Settlements.Where((Settlement s) => !s.IsReady || string.IsNullOrEmpty(s.Name?.ToString()) || s.Culture == null).ToList();
		if (list.Count == 0)
		{
			return;
		}
		HashSet<Settlement> hashSet = new HashSet<Settlement>(list);
		foreach (Settlement item in list)
		{
			MBObjectManager.Instance.UnregisterObject(item);
			TraceLogger.Write("GhostSettlementCleanupPatch", string.Format("Purged ghost settlement from save: '{0}' name='{1}' ready={2} culture={3}.", item.StringId, item.Name, item.IsReady, item.Culture?.StringId ?? "NULL"));
		}
		try
		{
			FieldInfo fieldInfo = AccessTools.Field(typeof(MobileParty), "_currentSettlement");
			if (!(fieldInfo != null))
			{
				return;
			}
			foreach (MobileParty item2 in MobileParty.All)
			{
				if (fieldInfo.GetValue(item2) is Settlement settlement && hashSet.Contains(settlement))
				{
					fieldInfo.SetValue(item2, null);
					TraceLogger.Write("GhostSettlementCleanupPatch", "Cleared dangling CurrentSettlement reference to purged ghost '" + settlement.StringId + "' on party '" + item2.StringId + "'.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("GhostSettlementCleanupPatch", "Dangling-reference cleanup failed: " + ex.Message);
		}
	}
}
