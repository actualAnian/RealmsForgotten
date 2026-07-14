using System;
using HarmonyLib;
using Homesteads.MissionLogics;
using Homesteads.Models;
using SandBox.Objects;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(PassageUsePoint), "OnUse")]
internal static class HomesteadTavernPassagePatch
{
	[HarmonyPostfix]
	private static void Postfix(PassageUsePoint __instance, Agent userAgent)
	{
		try
		{
			if (userAgent == null || !userAgent.IsMainAgent)
			{
				return;
			}
			HomesteadBehavior instance = HomesteadBehavior.Instance;
			Homestead homestead = instance?.CurrentHomestead;
			if (instance == null || homestead == null)
			{
				return;
			}
			if (Mission.Current?.GetMissionBehavior<HomesteadTavernMissionLogic>() != null)
			{
				TraceLogger.Write("HomesteadTavernPassagePatch", $"Tavern exit door used — returning to outside walk-around of '{homestead.Name}'.");
				instance.RequestReturnNearTavernEntrance();
				instance.ScheduleReturnToHomestead(homestead);
			}
			else if (__instance != null)
			{
				_ = ((ScriptComponentBehavior)(object)__instance).GameEntity;
				if (0 == 0 && (((ScriptComponentBehavior)(object)__instance).GameEntity.HasTag("homestead_tavern_door") || (((ScriptComponentBehavior)(object)__instance).GameEntity.Parent != null && ((ScriptComponentBehavior)(object)__instance).GameEntity.Parent.HasTag("homestead_tavern_door"))))
				{
					TraceLogger.Write("HomesteadTavernPassagePatch", $"Tavern door used — entering tavern of '{homestead.Name}'.");
					instance.ScheduleTavernVisit(homestead);
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTavernPassagePatch", "Postfix failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
