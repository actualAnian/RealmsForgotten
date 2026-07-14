using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadTownMenuInitCrashPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		Type t = typeof(PlayerTownVisitCampaignBehavior);
		BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
		string[] array = new string[3] { "game_menu_town_on_init", "game_menu_castle_on_init", "game_menu_village_on_init" };
		foreach (string name in array)
		{
			MethodInfo method = t.GetMethod(name, flags);
			if (method != null)
			{
				yield return method;
			}
		}
	}

	[HarmonyPrefix]
	private static bool Prefix(MenuCallbackArgs args)
	{
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement == null || currentSettlement.StringId == null || !currentSettlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			if ((currentSettlement.IsVillage || currentSettlement.Town != null) && currentSettlement.OwnerClan != null && currentSettlement.OwnerClan.IsReady && currentSettlement.LocationComplex != null)
			{
				if (currentSettlement.HeroesWithoutParty.Count == 0)
				{
					TraceLogger.WriteOnce("MenuInitSelfHeal:" + currentSettlement.StringId, "HomesteadTownMenuInitCrashPatch", "'" + currentSettlement.StringId + "' menu-init found an empty HeroesWithoutParty — re-running notable restoration before native reads it.");
					try
					{
						HomesteadSettlementBuilder.RestoreNotablesToSettlements(new List<Settlement> { currentSettlement });
					}
					catch (Exception ex)
					{
						TraceLogger.Write("HomesteadTownMenuInitCrashPatch", "Self-heal failed: " + ex.Message);
					}
				}
				return true;
			}
			TraceLogger.Write("HomesteadTownMenuInitCrashPatch", $"'{currentSettlement.StringId}' isn't fully ready yet (Town={currentSettlement.Town != null}, " + $"OwnerClanReady={currentSettlement.OwnerClan?.IsReady}, LC={currentSettlement.LocationComplex != null}) — " + "showing a minimal placeholder menu instead of running native init (which unconditionally dereferences that state and would crash).");
			args.MenuTitle = new TextObject("{=homestead_settlement_loading_title}Loading...");
			TextObject textObject = new TextObject("{=homestead_settlement_intro_fallback}You have arrived at {SETTLEMENT_LINK}.");
			textObject.SetTextVariable("SETTLEMENT_LINK", currentSettlement.EncyclopediaLinkWithName);
			MBTextManager.SetTextVariable("SETTLEMENT_INFO", textObject);
			return false;
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadTownMenuInitCrashPatch", "Prefix failed: " + ex2.Message);
			return true;
		}
	}
}
