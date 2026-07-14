using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(PartyBase), "CalculateVisibilityAndInspected")]
internal class HomesteadPartyBaseCalculateVisibilityPatch
{
	[HarmonyPostfix]
	private static void Postfix(IMapPoint mapPoint, ref bool isVisible, ref bool isInspected)
	{
		if (mapPoint is MobileParty mobileParty && Homestead.GetFor(mobileParty) != null)
		{
			isVisible = true;
			isInspected = true;
		}
	}
}
