using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch]
internal class HomesteadPartySizePatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		foreach (Type type in from type2 in AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetTypesSafe)
			where type2 != null && !type2.IsAbstract && typeof(PartySizeLimitModel).IsAssignableFrom(type2)
			select type2)
		{
			MethodInfo methodInfo = AccessTools.Method(type, "GetPartyMemberSizeLimit", new Type[2]
			{
				typeof(PartyBase),
				typeof(bool)
			});
			if (methodInfo != null)
			{
				yield return methodInfo;
			}
			MethodInfo methodInfo2 = AccessTools.Method(type, "GetPartyPrisonerSizeLimit", new Type[2]
			{
				typeof(PartyBase),
				typeof(bool)
			});
			if (methodInfo2 != null)
			{
				yield return methodInfo2;
			}
		}
	}

	private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where((Type type) => type != null).Cast<Type>();
		}
		catch
		{
			return Enumerable.Empty<Type>();
		}
	}

	[HarmonyPrefix]
	private static bool Prefix(MethodBase __originalMethod, PartyBase party, bool includeDescriptions, ref ExplainedNumber __result)
	{
		if (party == null || party.MobileParty == null)
		{
			return true;
		}
		Homestead homestead = Homestead.GetFor(party.MobileParty);
		if (homestead == null)
		{
			return true;
		}
		try
		{
			if (__originalMethod.Name == "GetPartyMemberSizeLimit")
			{
				__result = new ExplainedNumber(Math.Max(1, homestead.GetTroopLimit()), includeDescriptions);
				return false;
			}
			if (__originalMethod.Name == "GetPartyPrisonerSizeLimit")
			{
				__result = new ExplainedNumber(Math.Max(0, homestead.GetPrisonerLimit()), includeDescriptions);
				return false;
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadPartySizePatch", $"{__originalMethod.Name} failed for '{party.MobileParty.StringId}': {arg}");
			__result = new ExplainedNumber(Math.Max(1, party.NumberOfAllMembers), includeDescriptions);
			return false;
		}
		return true;
	}
}
