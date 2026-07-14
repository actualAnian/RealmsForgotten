using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadBattleSimulationPowerPatch
{
	private const float DefensivePowerBonusPerTier = 0.2f;

	private const float TacticalEdgePowerBonus = 0.1f;

	private static IEnumerable<MethodBase> TargetMethods()
	{
		List<Type> allTypes = (from type2 in AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetTypesSafe)
			where type2 != null && !type2.IsAbstract && typeof(MilitaryPowerModel).IsAssignableFrom(type2)
			select type2).ToList();
		foreach (Type type in allTypes)
		{
			if (!allTypes.Any((Type other) => other != type && type.IsAssignableFrom(other)))
			{
				MethodInfo methodInfo = AccessTools.Method(type, "GetPowerOfParty", new Type[3]
				{
					typeof(PartyBase),
					typeof(BattleSideEnum),
					typeof(MapEvent.PowerCalculationContext)
				});
				if (methodInfo != null)
				{
					yield return methodInfo;
				}
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

	private static void Postfix(PartyBase party, BattleSideEnum side, ref float __result)
	{
		if (party?.MobileParty == null || side != BattleSideEnum.Defender)
		{
			return;
		}
		Homestead homestead = Homestead.GetFor(party.MobileParty);
		if (homestead == null)
		{
			return;
		}
		int num = Math.Max(0, homestead.Tier);
		if (num <= 0)
		{
			return;
		}
		float num2 = 1f + (float)num * 0.2f;
		HomesteadBehavior instance = HomesteadBehavior.Instance;
		if (instance != null && instance.HasArmsMasterMasteryUnlocked)
		{
			Hero? armsMasterHero = homestead.ArmsMasterHero;
			if (armsMasterHero != null && armsMasterHero.IsAlive)
			{
				num2 += 0.1f;
			}
		}
		__result *= num2;
	}
}
