using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Homesteads.MissionLogics;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadMissionStateOpenNewPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(MissionState), "OpenNew", new Type[5]
		{
			typeof(string),
			typeof(MissionInitializerRecord),
			typeof(InitializeMissionBehaviorsDelegate),
			typeof(bool),
			typeof(bool)
		});
	}

	private static void Prefix(string missionName, ref MissionInitializerRecord rec, ref InitializeMissionBehaviorsDelegate handler)
	{
		// Custom Battle, the main menu and other non-campaign missions run this
		// too. Homesteads only exist inside a campaign, so there is nothing to
		// inject here — and touching campaign state (TryApplyTo) throws NRE when
		// Campaign.Current is null. Leave those missions completely untouched.
		if (Campaign.Current == null)
		{
			return;
		}
		bool num = IsBattleMissionName(missionName);
		bool flag = IsArenaMissionName(missionName);
		if (!num)
		{
			if (flag)
			{
				TraceLogger.Write("HomesteadMissionStateOpenNewPatch", "Skipped companion dog injection for arena/tournament mission '" + (missionName ?? "null") + "'.");
				return;
			}
			HomesteadBehavior instance = HomesteadBehavior.Instance;
			if (instance != null && instance.HasAdoptedDog)
			{
				InitializeMissionBehaviorsDelegate nonBattleOriginal = handler;
				handler = delegate(Mission mission)
				{
					List<MissionBehavior> list = (nonBattleOriginal?.Invoke(mission) ?? Enumerable.Empty<MissionBehavior>()).ToList();
					if (!list.OfType<HomesteadCompanionDogMissionLogic>().Any() && !list.OfType<HomesteadSpawningMissionLogic>().Any() && !list.OfType<HomesteadDogCombatLogic>().Any())
					{
						list.Add(new HomesteadCompanionDogMissionLogic());
						TraceLogger.Write("HomesteadMissionStateOpenNewPatch", "Injected companion dog into non-battle mission '" + (missionName ?? "null") + "'.");
					}
					return list;
				};
			}
			else
			{
				TraceLogger.Write("HomesteadMissionStateOpenNewPatch", "Skipped MissionState.OpenNew for non-battle mission '" + (missionName ?? "null") + "' scene='" + (rec.SceneName ?? "null") + "'.");
			}
			return;
		}
		if (flag)
		{
			TraceLogger.Write("HomesteadMissionStateOpenNewPatch", "Skipped companion dog injection for arena/tournament battle mission '" + (missionName ?? "null") + "'.");
			return;
		}
		InitializeMissionBehaviorsDelegate originalHandler = handler;
		handler = delegate(Mission mission)
		{
			List<MissionBehavior> list = (originalHandler?.Invoke(mission) ?? Enumerable.Empty<MissionBehavior>()).ToList();
			if (!list.OfType<HomesteadCompanionDogMissionLogic>().Any())
			{
				list.Insert(0, new HomesteadCompanionDogMissionLogic());
			}
			Homestead homestead = HomesteadBattleContext.TryGetHomesteadForMissionAttach();
			if (homestead != null && !list.OfType<HomesteadBattleSceneMissionLogic>().Any())
			{
				list.Insert(0, new HomesteadBattleSceneMissionLogic(homestead));
				list.Insert(0, new HomesteadDogCombatLogic(homestead));
				HomesteadBattleContext.MarkMissionAttached("HomesteadMissionStateOpenNewPatch.handler");
				TraceLogger.Write("HomesteadBattleMissionPatch", $"Injected homestead battle scene logic during mission behavior initialization for '{homestead.Name}'.");
			}
			return list;
		};
		HomesteadBattleContext.TryApplyTo(ref rec, "HomesteadMissionStateOpenNewPatch");
	}

	private static bool IsBattleMissionName(string? missionName)
	{
		if (string.IsNullOrEmpty(missionName))
		{
			return false;
		}
		string text = missionName.ToLowerInvariant();
		if (!text.Contains("battle") && !text.Contains("combat"))
		{
			return text.Contains("caravan");
		}
		return true;
	}

	internal static bool IsArenaMissionName(string? missionName)
	{
		if (string.IsNullOrEmpty(missionName))
		{
			return false;
		}
		string text = missionName.ToLowerInvariant();
		if (!text.Contains("arena"))
		{
			return text.Contains("tournament");
		}
		return true;
	}

	private static void Postfix(Mission __result)
	{
		// Same reasoning as Prefix: no campaign means no homestead, so do not
		// attach the companion dog / battle-scene logic to Custom Battle etc.
		if (Campaign.Current == null)
		{
			return;
		}
		HomesteadBattleMissionPatch.TryAttachBattleSceneLogic(__result, "HomesteadMissionStateOpenNewPatch");
	}
}
