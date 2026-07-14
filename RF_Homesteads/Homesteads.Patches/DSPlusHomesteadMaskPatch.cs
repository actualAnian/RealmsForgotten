using System;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace Homesteads.Patches;

internal static class DSPlusHomesteadMaskPatch
{
	private static readonly (string TypeName, string[] Methods)[] Targets = new(string, string[])[2]
	{
		("DistinguishedServicePlus.Services.PromotionManager", new string[2] { "GetTransferOutCondition", "GetTakeBackCondition" }),
		("DistinguishedService.PromotionManager", new string[2] { "GetGiveCompToClanPartyCondition", "GetTakeCompFromClanPartyCondition" })
	};

	internal static void TryApply(Harmony harmony)
	{
		MethodInfo method = typeof(DSPlusHomesteadMaskPatch).GetMethod("MaskForHomestead", BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null)
		{
			return;
		}
		HarmonyMethod postfix = new HarmonyMethod(method);
		int num = 0;
		(string, string[])[] targets = Targets;
		for (int i = 0; i < targets.Length; i++)
		{
			(string, string[]) tuple = targets[i];
			string item = tuple.Item1;
			string[] item2 = tuple.Item2;
			Type type = AccessTools.TypeByName(item);
			if (type == null)
			{
				continue;
			}
			string[] array = item2;
			foreach (string text in array)
			{
				try
				{
					MethodInfo methodInfo = AccessTools.Method(type, text);
					if (methodInfo == null || methodInfo.ReturnType != typeof(bool) || methodInfo.GetParameters().Length != 0)
					{
						TraceLogger.Write("DSPlusHomesteadMaskPatch", item + "." + text + " missing/incompatible — skipping.");
						continue;
					}
					harmony.Patch(methodInfo, null, postfix);
					num++;
					TraceLogger.Write("DSPlusHomesteadMaskPatch", "Masked " + item + "." + text + " for homestead parties.");
				}
				catch (Exception arg)
				{
					TraceLogger.Write("DSPlusHomesteadMaskPatch", $"Failed to patch {item}.{text}: {arg}");
				}
			}
		}
		if (num == 0)
		{
			TraceLogger.Write("DSPlusHomesteadMaskPatch", "No Distinguished Service flavor detected — nothing to mask.");
		}
	}

	private static void MaskForHomestead(ref bool __result)
	{
		if (!__result)
		{
			return;
		}
		try
		{
			if (IsHomesteadOwnedPartyLeader(Hero.OneToOneConversationHero))
			{
				__result = false;
			}
		}
		catch
		{
		}
	}

	private static bool IsHomesteadOwnedPartyLeader(Hero? hero)
	{
		MobileParty mobileParty = hero?.PartyBelongedTo;
		if (mobileParty == null)
		{
			return false;
		}
		PartyComponent partyComponent = mobileParty.PartyComponent;
		if (!(partyComponent is Homestead) && !(partyComponent is HomesteadPatrolPartyComponent) && !(partyComponent is HomesteadRecruiterComponent))
		{
			return partyComponent is HomesteadRaiderPartyComponent;
		}
		return true;
	}
}
