using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch]
internal class HomesteadDesertionPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(DesertionCampaignBehavior), "DailyTickParty");
	}

	[HarmonyPostfix]
	private static void Postfix(MobileParty mobileParty)
	{
		if (Homestead.GetFor(mobileParty) != null)
		{
			TraceLogger.Write("HomesteadDesertionPatch", string.Format("DailyTickParty postfix for '{0}' members={1} active={2} disbanding={3}", mobileParty?.StringId ?? "null", mobileParty?.Party?.NumberOfAllMembers ?? (-1), mobileParty?.IsActive ?? false, mobileParty?.IsDisbanding ?? false));
			if (mobileParty.IsActive && !mobileParty.IsDisbanding && mobileParty.Party.MapEvent == null && mobileParty.Party.NumberOfAllMembers <= 0)
			{
				TraceLogger.Write("HomesteadDesertionPatch", "Keeping empty active homestead party '" + mobileParty.StringId + "' after desertion daily tick.");
				mobileParty.SetMoveModeHold();
			}
		}
	}
}
