using System;
using HarmonyLib;
using Homesteads.Models;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MobilePartyVisual), "AddMobileIconComponents")]
internal class HomesteadMapAppearanceModelPatch
{
	[HarmonyPrefix]
	private static bool Prefix(MobilePartyVisual __instance, PartyBase party, ref bool clearBannerComponentCache, ref bool clearBannerEntityCache)
	{
		MobileParty mobileParty = party?.MobileParty;
		if (mobileParty == null)
		{
			return true;
		}
		if (Homestead.GetFor(mobileParty) == null)
		{
			return true;
		}
		try
		{
			TraceLogger.Write("HomesteadMapAppearanceModelPatch", "AddMobileIconComponents prefix for '" + (mobileParty.StringId ?? "null") + "'; suppressing default party icon and applying homestead map visual");
			__instance.StrategicEntity.SetVisibilityExcludeParents(visible: false);
			foreach (GameEntity child in __instance.StrategicEntity.GetChildren())
			{
				child.SetVisibilityExcludeParents(visible: false);
			}
			clearBannerComponentCache = true;
			clearBannerEntityCache = true;
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadMapAppearanceModelPatch", string.Format("Failed applying homestead map visual for '{0}': {1}", mobileParty.StringId ?? "null", arg));
		}
		return false;
	}
}
