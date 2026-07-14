using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Homesteads.Models;
using Homesteads.Views;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Homesteads;

public static class HomesteadConsoleCommands
{
	[CommandLineFunctionality.CommandLineArgumentFunction("list_homesteads", "campaign")]
	public static string ListHomesteads(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (!TryGetBehavior(out HomesteadBehavior behavior, out string error))
		{
			return error;
		}
		if (behavior.HomesteadMobileParties.Count == 0)
		{
			return "No homesteads found.";
		}
		return string.Join("\n", behavior.HomesteadMobileParties.Select<KeyValuePair<MobileParty, Homestead>, string>(delegate(KeyValuePair<MobileParty, Homestead> pair)
		{
			Homestead value = pair.Value;
			MobileParty key = pair.Key;
			float num = ((MobileParty.MainParty == null) ? (-1f) : key.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D));
			return $"{key.StringId}: \"{value.Name}\" tier={value.Tier} distance={num:0.0}";
		}));
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_homestead_tier", "campaign")]
	public static string SetHomesteadTier(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (!TryGetBehavior(out HomesteadBehavior behavior, out string error))
		{
			return error;
		}
		if (args.Count < 1 || !int.TryParse(args[0], out var result))
		{
			return "Usage: campaign.set_homestead_tier <0-3> [homestead name or party id]. If no target is given, uses the current homestead or nearest homestead.";
		}
		result = Math.Max(0, Math.Min(3, result));
		Homestead homestead = ResolveHomestead(behavior, args.Skip(1).ToList());
		if (homestead == null)
		{
			return "No matching homestead found. Use campaign.list_homesteads to see available homesteads.";
		}
		int tier = homestead.Tier;
		homestead.Tier = result;
		homestead.TierProgress = 0f;
		homestead.MobileParty?.Party?.SetVisualAsDirty();
		Homestead.SetGameTextsForMenus();
		TraceLogger.Write("HomesteadConsoleCommands", $"Console set tier for '{homestead.Name}' from {tier} to {result}");
		return $"Set homestead \"{homestead.Name}\" tier from {tier} to {result}.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_build_test_town", "campaign")]
	public static string BuildTestTown(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null || MobileParty.MainParty == null)
		{
			return "No active campaign.";
		}
		Vec2 getPosition2D = MobileParty.MainParty.GetPosition2D;
		CultureObject cultureObject = Hero.MainHero?.Culture;
		Clan playerClan = Clan.PlayerClan;
		if (cultureObject == null || playerClan == null)
		{
			return "No player culture/clan.";
		}
		Homestead homestead = HomesteadBehavior.Instance?.HomesteadMobileParties?.Values?.FirstOrDefault();
		string text = ((args.Count > 0) ? string.Join(" ", args) : ((homestead != null) ? HomesteadSettlementBuilder.TownNameFor(homestead) : "Test Town"));
		Settlement settlement = HomesteadSettlementBuilder.CreateTown(new HomesteadSettlementPlacementMapView.Result
		{
			Town = new HomesteadSettlementPlacementMapView.Placed
			{
				Position = getPosition2D
			},
			Gate = new HomesteadSettlementPlacementMapView.Placed
			{
				Position = getPosition2D,
				Rotation = 0f
			}
		}, text, cultureObject, playerClan);
		if (settlement == null)
		{
			return "Failed to build the town — check HomesteadsReloaded.trace.log.";
		}
		return "Built town \"" + text + "\" (" + settlement.StringId + ") at your position. Save & reload to verify it persists.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_place_settlement", "campaign")]
	public static string PlaceSettlement(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null)
		{
			return "No active campaign.";
		}
		if (!TryGetBehavior(out HomesteadBehavior _, out string error))
		{
			return error;
		}
		Homestead homestead = ResolveHomestead(HomesteadBehavior.Instance, (args.Count > 0) ? args : new List<string>());
		if (homestead == null)
		{
			return "No homestead found. Use campaign.list_homesteads.";
		}
		if (homestead.SettlementCharterGranted)
		{
			return $"\"{homestead.Name}\" already has its charter. Talk to your homestead leader to begin placement.";
		}
		if (!homestead.SettlementUpgradeReady)
		{
			homestead.SettlementUpgradeReady = true;
		}
		homestead.OnSettlementCharterGranted();
		return $"Charter granted for \"{homestead.Name}\". Talk to your homestead leader to begin placement.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_list_player_settlements", "campaign")]
	public static string ListPlayerSettlements(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null)
		{
			return "No active campaign.";
		}
		List<Settlement> campaignList = Campaign.Current.Settlements.Where((Settlement s) => s.StringId != null && s.StringId.StartsWith("hsr_settlement_")).ToList();
		List<Settlement> list = (from s in MBObjectManager.Instance.GetObjectTypeList<Settlement>()
			where s.StringId != null && s.StringId.StartsWith("hsr_settlement_")
			select s).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Campaign.Settlements (persisted set): {campaignList.Count}");
		foreach (Settlement item in campaignList)
		{
			stringBuilder.AppendLine(string.Format("   {0} '{1}' pos={2} visible={3} faction={4}", item.StringId, item.Name, item.GetPosition2D, item.IsVisible, item.MapFaction?.Name?.ToString() ?? "null"));
		}
		stringBuilder.AppendLine($"MBObjectManager Settlement list: {list.Count}");
		foreach (Settlement item2 in list.Where((Settlement s) => !campaignList.Contains(s)))
		{
			stringBuilder.AppendLine($"   (MB-only) {item2.StringId} '{item2.Name}'");
		}
		return stringBuilder.ToString().TrimEnd(Array.Empty<char>());
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_list_buildings", "campaign")]
	public static string ListBuildings(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null)
		{
			return "No active campaign.";
		}
		if (args.Count < 1)
		{
			return "Usage: campaign.homestead_list_buildings [SettlementIdOrNamePart]";
		}
		Settlement settlement = FindPlayerSettlement(string.Join(" ", args));
		if (settlement?.Town == null)
		{
			return "No player town/castle matching '" + string.Join(" ", args) + "'.";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"{settlement.Name} ({settlement.StringId}):");
		foreach (Building building in settlement.Town.Buildings)
		{
			if (building?.BuildingType != null && !building.BuildingType.IsDailyProject)
			{
				stringBuilder.AppendLine($"   {building.BuildingType.StringId}  level={building.CurrentLevel}  progress={building.BuildingProgress:0}");
			}
		}
		return stringBuilder.ToString().TrimEnd(Array.Empty<char>());
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_set_building_level", "campaign")]
	public static string SetBuildingLevel(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null)
		{
			return "No active campaign.";
		}
		if (args.Count < 3)
		{
			return "Usage: campaign.homestead_set_building_level [SettlementIdOrNamePart] [BuildingTypeId] [Level 0-3]";
		}
		if (!int.TryParse(args[args.Count - 1], out var result) || result < 0 || result > 3)
		{
			return "Level must be 0-3.";
		}
		string buildingId = args[args.Count - 2];
		Settlement settlement = FindPlayerSettlement(string.Join(" ", args.Take(args.Count - 2)));
		if (settlement?.Town == null)
		{
			return "No player town/castle matching '" + string.Join(" ", args.Take(args.Count - 2)) + "'.";
		}
		Building building = settlement.Town.Buildings.FirstOrDefault((Building x) => x?.BuildingType?.StringId == buildingId);
		if (building == null)
		{
			return $"Building '{buildingId}' not found in {settlement.Name} — run campaign.homestead_list_buildings first.";
		}
		building.CurrentLevel = result;
		building.BuildingProgress = 0f;
		return $"{settlement.Name}: '{buildingId}' set to level {result}. Save the game to persist it.";
	}

	private static Settlement? FindPlayerSettlement(string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return null;
		}
		List<Settlement> source = Campaign.Current.Settlements.Where((Settlement x) => x.StringId != null && x.StringId.StartsWith("hsr_settlement_")).ToList();
		return source.FirstOrDefault((Settlement x) => string.Equals(x.StringId, query, StringComparison.OrdinalIgnoreCase)) ?? source.FirstOrDefault((Settlement x) => x.Name != null && x.Name.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_fix_settlement_pathing", "campaign")]
	public static string FixSettlementPathing(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null)
		{
			return "No active campaign.";
		}
		string filter = ((args != null && args.Count > 0) ? string.Join(" ", args) : null);
		List<Settlement> list = Campaign.Current.Settlements.Where((Settlement s) => s.StringId != null && s.StringId.StartsWith("hsr_settlement_") && (s.IsTown || s.IsCastle || s.IsVillage)).Where(delegate(Settlement s)
		{
			if (filter != null)
			{
				TextObject name = s.Name;
				if ((object)name == null)
				{
					return false;
				}
				return name.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
			}
			return true;
		}).ToList();
		if (list.Count == 0)
		{
			if (filter != null)
			{
				return "No player-built settlement matching '" + filter + "'.";
			}
			return "No player-built settlements found.";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Settlement item in list)
		{
			HomesteadSettlementBuilder.TryEvaluateConnectivity(item.GatePosition.ToVec2(), out var ratio);
			if (HomesteadSettlementBuilder.TryFindBetterGate(item, out var bestGate, out var bestRatio))
			{
				bool flag = HomesteadSettlementBuilder.ApplyGate(item, bestGate);
				stringBuilder.AppendLine(string.Format("{0}: gate detour {1:0.00} -> {2:0.00} {3}", item.Name, ratio, bestRatio, flag ? "(moved)" : "(FAILED to apply)"));
			}
			else
			{
				stringBuilder.AppendLine($"{item.Name}: gate detour {ratio:0.00} — no better-connected spot nearby, left as-is.");
			}
		}
		stringBuilder.AppendLine("Save and reload for the change to fully take effect on world-map pathing.");
		return stringBuilder.ToString().TrimEnd(Array.Empty<char>());
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_fix_settlement_culture", "campaign")]
	public static string FixSettlementCulture(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null)
		{
			return "No active campaign.";
		}
		Clan playerClan = Clan.PlayerClan;
		if (playerClan?.Culture == null)
		{
			return "Player clan/culture not found.";
		}
		int num = HomesteadSettlementBuilder.FixSettlementCultureForClan(playerClan);
		if (num == 0)
		{
			return "No settlements needed fixing (already matching your clan's culture, or none owned).";
		}
		return $"Updated {num} settlement(s) to '{playerClan.Culture.StringId}' culture. Loyalty/recruit-pool " + "effects apply immediately; save and reload for the town/castle/village SCENES to rebuild with matching visuals (map icons stay Empire regardless — that's a separate, deliberate choice).";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_list_notables", "campaign")]
	public static string ListNotables(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null)
		{
			return "No active campaign.";
		}
		string filter = ((args != null && args.Count > 0) ? args[0] : null);
		List<Settlement> list = Campaign.Current.Settlements.Where(delegate(Settlement s)
		{
			if (s.StringId != null && s.StringId.StartsWith("hsr_settlement_"))
			{
				if (filter != null && !s.StringId.Contains(filter))
				{
					TextObject name = s.Name;
					if ((object)name == null)
					{
						return false;
					}
					return name.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
				}
				return true;
			}
			return false;
		}).ToList();
		if (list.Count == 0)
		{
			return "No matching settlement(s).";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Settlement item in list)
		{
			List<Hero> list2 = item.Notables?.ToList() ?? new List<Hero>();
			stringBuilder.AppendLine($"'{item.Name}' ({item.StringId}) — {list2.Count} notable(s):");
			foreach (Hero item2 in list2)
			{
				stringBuilder.AppendLine($"   {item2.Name} — {item2.Occupation} — template='{item2.CharacterObject?.StringId}' culture='{item2.Culture?.StringId}' alive={item2.IsAlive}");
			}
		}
		return stringBuilder.ToString().TrimEnd(Array.Empty<char>());
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_clear_test_settlements", "campaign")]
	public static string ClearTestSettlements(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null)
		{
			return "No active campaign.";
		}
		int num = HomesteadSettlementBuilder.ClearSaveFile();
		int num2 = HomesteadSettlementBehavior.Instance?.ClearBuilt() ?? 0;
		return $"Cleared {num} persisted + {num2} tracked player-built settlement(s). " + "Save and reload (or start a fresh save) to remove them from the world — already-loaded ones remain this session.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_force_quest_timeout", "campaign")]
	public static string ForceQuestTimeout(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (!TryGetBehavior(out HomesteadBehavior behavior, out string error))
		{
			return error;
		}
		Homestead homestead = ResolveHomestead(behavior, args);
		if (homestead == null)
		{
			return "No matching homestead found. Use campaign.list_homesteads to see available homesteads.";
		}
		HomesteadHeadmanTrustQuest activeHeadmanTrustQuest = behavior.GetActiveHeadmanTrustQuest(homestead);
		if (activeHeadmanTrustQuest != null)
		{
			activeHeadmanTrustQuest.ForceTimeout();
			return $"Forced Headman's Trust timeout for \"{homestead.Name}\" — angry villagers should now be marching on it.";
		}
		HomesteadLandPatentQuest activeLandPatentQuest = behavior.GetActiveLandPatentQuest(homestead);
		if (activeLandPatentQuest != null)
		{
			activeLandPatentQuest.ForceTimeout();
			return $"Forced Land Patent timeout for \"{homestead.Name}\" — the owning clan's men-at-arms should now be marching on it.";
		}
		return $"\"{homestead.Name}\" has no active Headman's Trust or Land Patent quest to time out. (Accept the quest from the homestead leader first.)";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("list_homestead_buildings", "campaign")]
	public static string ListHomesteadBuildings(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (!TryGetBehavior(out HomesteadBehavior behavior, out string error))
		{
			return error;
		}
		Homestead homestead = ResolveHomestead(behavior, args);
		if (homestead == null)
		{
			return "No matching homestead found. Use campaign.list_homesteads to see available homesteads.";
		}
		List<HomesteadSceneSavedEntity> savedEntities = homestead.GetHomesteadScene().SavedEntities;
		if (savedEntities == null || savedEntities.Count == 0)
		{
			return $"Homestead \"{homestead.Name}\" has no saved buildings.";
		}
		List<string> values = (from e in savedEntities
			group e by e.Placeable.PrefabName into g
			select $"  {g.Key} x{g.Count()}").ToList();
		return $"Homestead \"{homestead.Name}\" has {savedEntities.Count} building(s):\n" + string.Join("\n", values);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("demolish_homestead_building", "campaign")]
	public static string DemolishHomesteadBuilding(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (!TryGetBehavior(out HomesteadBehavior behavior, out string error))
		{
			return error;
		}
		if (args.Count < 1)
		{
			return "Usage: campaign.demolish_homestead_building <prefab_name> [homestead name or party id]\nExample: campaign.demolish_homestead_building homestead_lumberjack_camp\nUse campaign.list_homestead_buildings to see prefab names. Removes ONE instance and refunds items to the homestead stash.";
		}
		string prefabName = args[0].Trim();
		Homestead homestead = ResolveHomestead(behavior, args.Skip(1).ToList());
		if (homestead == null)
		{
			return "No matching homestead found. Use campaign.list_homesteads to see available homesteads.";
		}
		HomesteadScene homesteadScene = homestead.GetHomesteadScene();
		HomesteadSceneSavedEntity homesteadSceneSavedEntity = homesteadScene.SavedEntities?.FirstOrDefault((HomesteadSceneSavedEntity e) => e.Placeable.PrefabName.Equals(prefabName, StringComparison.OrdinalIgnoreCase));
		if (homesteadSceneSavedEntity == null)
		{
			return $"No building with prefab name '{prefabName}' found in homestead \"{homestead.Name}\".\n" + "Use campaign.list_homestead_buildings to see what is built.";
		}
		if (homestead.Stash != null && homesteadSceneSavedEntity.Placeable.ItemRequirements != null)
		{
			foreach (KeyValuePair<string, int> itemRequirement in homesteadSceneSavedEntity.Placeable.ItemRequirements)
			{
				ItemObject itemObject = Campaign.Current.ObjectManager.GetObject<ItemObject>(itemRequirement.Key);
				if (itemObject != null)
				{
					homestead.Stash.AddToCounts(itemObject, itemRequirement.Value);
				}
			}
		}
		homesteadScene.CurrentlyUsedBuildPoints -= homesteadSceneSavedEntity.Placeable.BuildPointsRequired;
		homesteadScene.TotalProductivity -= homesteadSceneSavedEntity.Placeable.ProductivityIncrease;
		homesteadScene.TotalSpace -= homesteadSceneSavedEntity.Placeable.SpaceIncrease;
		homesteadScene.TotalLeisure -= homesteadSceneSavedEntity.Placeable.LeisureIncrease;
		homesteadScene.SavedEntities.Remove(homesteadSceneSavedEntity);
		homestead.MobileParty?.Party?.SetVisualAsDirty();
		Homestead.SetGameTextsForMenus();
		TraceLogger.Write("HomesteadConsoleCommands", $"Console demolished '{prefabName}' from '{homestead.Name}', refunded items.");
		return $"Demolished '{prefabName}' from \"{homestead.Name}\" and refunded its items to the stash.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_force_quest", "campaign")]
	public static string ForceQuest(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (!TryGetBehavior(out HomesteadBehavior behavior, out string error))
		{
			return error;
		}
		if (args.Count < 1)
		{
			return "Usage: campaign.homestead_force_quest <Fetch|Delivery|Raid|Apparel|Building|Apprentice|off>\nForces the next \"Is there anything you need?\" offer to a specific quest type. Use 'off' to return to random.";
		}
		return behavior.SetForcedOfferType(args[0].Trim());
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_debug_mastery", "campaign")]
	public static string DebugMastery(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (!TryGetBehavior(out HomesteadBehavior behavior, out string error))
		{
			return error;
		}
		Homestead homestead = ResolveHomestead(behavior, new List<string>());
		if (homestead == null)
		{
			return "No homestead found.";
		}
		string text = $"Mastery Debug for {homestead.Name}:\n";
		bool flag = args.Count > 0 && args[0].Equals("fix", StringComparison.OrdinalIgnoreCase);
		if (homestead.HoundMasterHero != null)
		{
			string stringId = homestead.HoundMasterHero.StringId;
			behavior._notableApprenticeCount.TryGetValue(stringId, out var value);
			if (flag && value < 2)
			{
				behavior._notableApprenticeCount[stringId] = 2;
				value = 2;
			}
			text += $"- Hound Master: QuestsCompleted={value}, KnockdownUnlocked={behavior.HasHoundmasterKnockdownUnlocked}\n";
		}
		if (homestead.AmbassadorHero != null)
		{
			string stringId2 = homestead.AmbassadorHero.StringId;
			behavior._notableApprenticeCount.TryGetValue(stringId2, out var value2);
			if (flag && value2 < 2)
			{
				behavior._notableApprenticeCount[stringId2] = 2;
				value2 = 2;
			}
			text += $"- Ambassador: QuestsCompleted={value2}, IntroUnlocked={behavior.HasAmbassadorTactfulIntroductionUnlocked}\n";
		}
		if (homestead.MarketLadyHero != null)
		{
			string stringId3 = homestead.MarketLadyHero.StringId;
			behavior._notableApprenticeCount.TryGetValue(stringId3, out var value3);
			text += $"- Market Lady: QuestsCompleted={value3}\n";
		}
		if (homestead.ArmsMasterHero != null)
		{
			text = text + $"- Arms Master: '{homestead.ArmsMasterHero.Name}' (relation={homestead.ArmsMasterHero.GetRelationWithPlayer()})" + " TacticalEdge=" + ((homestead.ArmsMasterHero.GetRelationWithPlayer() >= 50f) ? "ACTIVE" : "inactive") + "\n";
		}
		else
		{
			HomesteadArmsMasterRecruitQuest activeArmsMasterRecruitQuest = behavior.GetActiveArmsMasterRecruitQuest(homestead);
			text += ((activeArmsMasterRecruitQuest != null) ? "- Arms Master: QUEST ACTIVE (win a tournament to recruit)\n" : $"- Arms Master: none (training field={homestead.HasTrainingFieldBuilding})\n");
		}
		if (!flag)
		{
			text += "\nRun 'campaign.homestead_debug_mastery fix' to instantly set completed quests to 2 for these notables.";
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_test_building_offer", "campaign")]
	public static string TestBuildingOffer(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (!TryGetBehavior(out HomesteadBehavior behavior, out string error))
		{
			return error;
		}
		Homestead homestead = ResolveHomestead(behavior, args);
		if (homestead == null)
		{
			return "No matching homestead found. Use campaign.list_homesteads to see available homesteads.";
		}
		return behavior.DebugBuildingSelection(homestead);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("start_race", "campaign")]
	public static string StartRace(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (!TryGetBehavior(out HomesteadBehavior behavior, out string error))
		{
			return error;
		}
		if (Mission.Current != null)
		{
			return "Run this from the campaign map (finish/leave the current scene first).";
		}
		RaceTrack raceTrack = null;
		if (args.Count >= 1)
		{
			raceTrack = RaceTracks.ById(args[0].Trim());
			if (raceTrack == null)
			{
				return "Unknown track id. Available:\n" + string.Join("\n", RaceTracks.All.Select((RaceTrack t) => $"  {t.Id} — {t.Name}  ({t.Scene}, {t.Laps} lap(s))"));
			}
		}
		behavior.StartRaceFromConsole(raceTrack);
		if (raceTrack == null)
		{
			return "Opening race setup — pick a track (if more than one) and your rivals.";
		}
		return "Starting '" + raceTrack.Name + "' — pick your rivals.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_list_workshop_owners", "campaign")]
	public static string ListWorkshopOwners(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null)
		{
			return "No active campaign.";
		}
		Settlement settlement = ResolveOurSettlement(args);
		if (settlement?.Town?.Workshops == null)
		{
			return "No matching player-built settlement with workshops found. Usage: campaign.homestead_list_workshop_owners <settlement name>";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Workshops in '{settlement.Name}' ({settlement.StringId}):");
		for (int i = 0; i < settlement.Town.Workshops.Length; i++)
		{
			Workshop workshop = settlement.Town.Workshops[i];
			Hero hero = workshop?.Owner;
			stringBuilder.AppendLine(string.Format("  [{0}] {1} — owner: ", i, workshop?.WorkshopType?.StringId ?? "empty") + ((hero == null) ? "none" : $"'{hero.Name}' (Occupation={hero.Occupation}, StringId={hero.StringId})"));
		}
		return stringBuilder.ToString().TrimEnd(Array.Empty<char>());
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("homestead_replace_workshop_owner", "campaign")]
	public static string ReplaceWorkshopOwner(List<string> args)
	{
		if (!NativeConfig.CheatMode)
		{
			return "Cheat mode is disabled!";
		}
		if (Campaign.Current == null)
		{
			return "No active campaign.";
		}
		if (args.Count < 1 || !int.TryParse(args[0], out var result))
		{
			return "Usage: campaign.homestead_replace_workshop_owner <workshop_index> [settlement name]\nUse campaign.homestead_list_workshop_owners first to find the index.";
		}
		Settlement s = ResolveOurSettlement(args.Skip(1).ToList());
		if (s?.Town?.Workshops == null)
		{
			return "No matching player-built settlement with workshops found.";
		}
		if (result < 0 || result >= s.Town.Workshops.Length)
		{
			return $"Workshop index out of range (0-{s.Town.Workshops.Length - 1}).";
		}
		Workshop ws = s.Town.Workshops[result];
		Hero owner = ws?.Owner;
		if (owner == null)
		{
			return "That workshop has no owner to replace.";
		}
		Hero hero = s.Notables.Where((Hero n) => n != null && !n.IsDead && !n.IsDisabled && n != owner && !s.Town.Workshops.Any((Workshop w) => w != ws && w.Owner == n)).FirstOrDefault();
		if (hero == null)
		{
			return $"No spare notable in '{s.Name}' to take over this workshop (everyone already owns one, or there are no other notables).";
		}
		string text = owner.Name?.ToString();
		ChangeOwnerOfWorkshopAction.ApplyByDeath(ws, hero);
		KillCharacterAction.ApplyByRemove(owner, showNotification: false, isForced: false);
		TraceLogger.Write("HomesteadConsoleCommands", $"ReplaceWorkshopOwner: '{s.StringId}' workshop[{result}] '{text}' -> '{hero.Name}'.");
		return $"Replaced workshop [{result}] owner '{text}' with '{hero.Name}' in '{s.Name}'.";
	}

	private static Settlement? ResolveOurSettlement(List<string> targetArgs)
	{
		List<Settlement> list = (from s in MBObjectManager.Instance.GetObjectTypeList<Settlement>()
			where s.StringId != null && s.StringId.StartsWith("hsr_settlement_") && s.Town != null
			select s).ToList();
		if (targetArgs.Count == 0)
		{
			if (list.Count != 1)
			{
				return Settlement.CurrentSettlement;
			}
			return list[0];
		}
		string target = string.Join(" ", targetArgs).Trim();
		return list.FirstOrDefault((Settlement s) => s.StringId.Equals(target, StringComparison.OrdinalIgnoreCase) || s.Name.ToString().IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static bool TryGetBehavior(out HomesteadBehavior behavior, out string error)
	{
		behavior = HomesteadBehavior.Instance;
		if (Campaign.Current == null || behavior == null)
		{
			error = "Campaign was not started or Homesteads behavior is not loaded.";
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static Homestead? ResolveHomestead(HomesteadBehavior behavior, List<string> targetArgs)
	{
		if (targetArgs.Count == 0)
		{
			if (behavior.CurrentHomestead != null)
			{
				return behavior.CurrentHomestead;
			}
			return (from pair in behavior.HomesteadMobileParties
				orderby (MobileParty.MainParty != null) ? pair.Key.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D) : float.MaxValue
				select pair.Value).FirstOrDefault();
		}
		string target = string.Join(" ", targetArgs).Trim();
		return behavior.HomesteadMobileParties.FirstOrDefault<KeyValuePair<MobileParty, Homestead>>((KeyValuePair<MobileParty, Homestead> pair) => pair.Key.StringId.Equals(target, StringComparison.OrdinalIgnoreCase) || pair.Value.Name.ToString().IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0).Value;
	}
}
