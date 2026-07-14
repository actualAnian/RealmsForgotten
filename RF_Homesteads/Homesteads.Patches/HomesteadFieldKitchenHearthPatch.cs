using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(DefaultSettlementProsperityModel), "CalculateHearthChange")]
internal static class HomesteadFieldKitchenHearthPatch
{
	[HarmonyPostfix]
	private static void Postfix(Village village, bool includeDescriptions, ref ExplainedNumber __result)
	{
		HomesteadBehavior instance = HomesteadBehavior.Instance;
		if (instance?.HomesteadMobileParties == null || village?.Settlement == null)
		{
			return;
		}
		foreach (Homestead value in instance.HomesteadMobileParties.Values)
		{
			if (value != null && value.FieldKitchenAffectsVillage(village.Settlement))
			{
				TextObject textObject = new TextObject("{=homestead_fieldkitchen_hearth}{HOMESTEAD_NAME} (Field Kitchen)");
				textObject.SetTextVariable("HOMESTEAD_NAME", value.Name);
				__result.Add(2f, textObject);
			}
		}
	}
}
