using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadScenePlaceable
{
	[SaveableField(1)]
	public string DisplayName;

	[SaveableField(2)]
	public string Description;

	[SaveableField(3)]
	public string PrefabName;

	[SaveableField(4)]
	public int BuildPointsRequired;

	[SaveableField(5)]
	public int ProductivityIncrease;

	[SaveableField(6)]
	public int SpaceIncrease;

	[SaveableField(7)]
	public int LeisureIncrease;

	[SaveableField(9)]
	public Dictionary<string, int> ItemRequirements;

	[SaveableField(10)]
	public List<HomesteadScenePlaceableProducedItem> ProduceItems;

	[SaveableField(11)]
	public string BuilderMenuCategoryString;

	[SaveableField(12)]
	public int MedicalCareIncrease;

	[SaveableField(13)]
	public string NpcAction = "";

	[SaveableField(14)]
	public string NpcColor = "";

	public int MaxBuildCount;

	public int TierRequired;

	public bool IsNpcActionFlag => !string.IsNullOrEmpty(NpcAction);

	public HomesteadScenePlaceable(string builderMenuCategoryString, string displayName, string desc, string prefabName, int buildPointsRequired, int productivity, int space, int leisure, int medicalCare, int maxBuildCount, List<HomesteadScenePlaceableProducedItem> produceItems, Dictionary<string, int> itemRequirements, string npcAction = "", string npcColor = "")
	{
		BuilderMenuCategoryString = builderMenuCategoryString;
		NpcAction = npcAction ?? "";
		NpcColor = npcColor ?? "";
		DisplayName = Utils.GetLocalizedString(displayName);
		Description = "--------------------\n" + Utils.GetLocalizedString(desc) + "\n";
		PrefabName = prefabName;
		BuildPointsRequired = buildPointsRequired;
		ProductivityIncrease = productivity;
		SpaceIncrease = space;
		LeisureIncrease = leisure;
		MedicalCareIncrease = medicalCare;
		MaxBuildCount = maxBuildCount;
		ProduceItems = produceItems;
		ItemRequirements = itemRequirements;
		TextObject textObject = new TextObject("{=homestead_current_placeable_buildpoints_required}{BUILD_POINTS_NEEDED} BUILD POINTS NEEDED\n");
		textObject.SetTextVariable("BUILD_POINTS_NEEDED", buildPointsRequired);
		TextObject textObject2 = new TextObject("{=homestead_current_placeable_increases}+{PRODUCTIVITY} Productivity | +{SPACE} Space | +{LEISURE} Leisure | +{MEDICAL_CARE} Medical Care");
		textObject2.SetTextVariable("PRODUCTIVITY", productivity);
		textObject2.SetTextVariable("SPACE", space);
		textObject2.SetTextVariable("LEISURE", leisure);
		textObject2.SetTextVariable("MEDICAL_CARE", medicalCare);
		string text = "";
		if (maxBuildCount > 0)
		{
			TextObject textObject3 = new TextObject("{=homestead_current_placeable_build_limit}BUILD LIMIT: {BUILD_LIMIT}\n");
			textObject3.SetTextVariable("BUILD_LIMIT", maxBuildCount);
			text = textObject3.ToString();
		}
		string text2 = "";
		if (produceItems.Count > 0)
		{
			List<string> list = new List<string>();
			foreach (HomesteadScenePlaceableProducedItem produceItem in ProduceItems)
			{
				string[] array = produceItem.ItemProducedID.Split(new char[1] { '|' });
				List<string> list2 = new List<string>();
				string[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					ItemObject itemFromID = Utils.GetItemFromID(array2[i]);
					if (itemFromID != null)
					{
						list2.Add(itemFromID.Name.ToString());
					}
				}
				string text3 = string.Join(Utils.GetLocalizedString("{=homestead_current_placeable_or} OR "), list2) + " x" + produceItem.AmountToProduce;
				if (produceItem.RequiredItemsToProduce.Count > 0)
				{
					text3 += Utils.GetLocalizedString("{=homestead_current_placeable_needs} NEEDS ");
					List<string> list3 = new List<string>();
					foreach (KeyValuePair<string, int> item in produceItem.RequiredItemsToProduce)
					{
						string[] array3 = item.Key.Split(new char[1] { '|' });
						List<string> list4 = new List<string>();
						array2 = array3;
						for (int i = 0; i < array2.Length; i++)
						{
							ItemObject itemFromID2 = Utils.GetItemFromID(array2[i]);
							if (itemFromID2 != null)
							{
								list4.Add(itemFromID2.Name.ToString());
							}
						}
						list3.Add(string.Join(Utils.GetLocalizedString("{=homestead_current_placeable_or} OR "), list4) + " x" + item.Value);
					}
					text3 += string.Join(", ", list3);
				}
				list.Add(text3);
			}
			text2 = new TextObject("\n{=homestead_current_placeable_produces}PRODUCES:\n" + string.Join(", \n", list)).ToString() + "\n--------------------";
		}
		string text4 = "";
		if (itemRequirements.Count > 0)
		{
			List<string> list5 = new List<string>();
			foreach (KeyValuePair<string, int> itemRequirement in itemRequirements)
			{
				ItemObject itemFromID3 = Utils.GetItemFromID(itemRequirement.Key);
				if (itemFromID3 != null)
				{
					list5.Add(itemFromID3.Name.ToString() + " x" + itemRequirement.Value);
				}
			}
			text4 = new TextObject("\n{=homestead_current_placeable_item_requirements}ITEMS REQUIRED:\n" + string.Join(", \n", list5)).ToString();
		}
		if (text2 != "" || text4 != "")
		{
			textObject2 = new TextObject(textObject2.ToString() + "\n--------------------");
		}
		Description = Description + "--------------------\n" + textObject.ToString() + text + textObject2.ToString() + text2 + text4;
	}

	public static List<HomesteadScenePlaceable> GetTierGroup(int tier)
	{
		return GetFromXMLs(tier);
	}

	public static List<HomesteadScenePlaceable> GetAllPlaceables()
	{
		HashSet<string> hashSet = new HashSet<string>();
		List<HomesteadScenePlaceable> list = new List<HomesteadScenePlaceable>();
		foreach (HomesteadScenePlaceable item in GetTierGroup(4))
		{
			if (item.PrefabName == null || hashSet.Add(item.PrefabName))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static List<HomesteadScenePlaceable> GetAllPlaceablesForPicker()
	{
		HashSet<string> hashSet = new HashSet<string>();
		List<HomesteadScenePlaceable> list = new List<HomesteadScenePlaceable>();
		foreach (HomesteadScenePlaceable item2 in GetTierGroup(4))
		{
			string item = item2.PrefabName + "|" + item2.DisplayName + "|" + item2.NpcAction;
			if (hashSet.Add(item))
			{
				list.Add(item2);
			}
		}
		return list;
	}

	public static HomesteadScenePlaceable? FindByPrefabName(string prefabName)
	{
		for (int i = 0; i <= 4; i++)
		{
			foreach (HomesteadScenePlaceable item in GetTierGroup(i))
			{
				if (item.PrefabName == prefabName)
				{
					return item;
				}
			}
		}
		return null;
	}

	public static float GetBattleFootprintRadius(string prefabName)
	{
		switch (prefabName)
		{
		case "homestead_watchtower":
			return 4.5f;
		case "european_village_barn_a":
			return 6f;
		case "homestead_prison_guardhouse":
			return 5f;
		case "hut_c_fishing":
			return 4f;
		case "pict_town_house_d":
			return 5.5f;
		case "arabian_fountain_a":
			return 3f;
		case "battania_castle_wall_a_l3":
		case "battania_castle_wall_c":
			return 3f;
		case "battania_castle_gatehouse_a_l3":
			return 6f;
		case "homestead_stonewall_2b3_scaled":
			return 3f;
		case "homestead_large_building_plot":
			return 0f;
		case "castle_plank_wall_a":
		case "castle_plank_wall_b":
		case "castle_plank_wall_c":
			return 2f;
		case "sturgia_arena_stakes_a":
			return 2.5f;
		case "homestead_tent_big":
		case "homestead_native_market_tent":
		case "homestead_native_khuzait_interior_tent_b":
		case "homestead_native_khuzait_grandbazaar_tent":
			return 4.5f;
		case "homestead_vlandian_tent":
		case "homestead_khuzait_yurt":
		case "homestead_medical_tent":
		case "homestead_native_canopy_tent":
		case "homestead_native_desert_shelter":
		case "homestead_native_empire_tent_03":
		case "homestead_native_empire_tent_04":
		case "homestead_native_khuzait_tent_b":
			return 3.5f;
		case "homestead_butchers_tent":
		case "homestead_musician_tent":
		case "homestead_native_battania_tent":
		case "homestead_native_painted_tent":
		case "homestead_native_khuzait_tent":
		case "homestead_native_sturgia_tent":
		case "homestead_vlandian_tent_small":
		case "homestead_native_aserai_tent":
		case "homestead_native_empire_tent_02":
			return 3f;
		case "homestead_dog_kennel":
			return 11f;
		case "homestead_cattle_pen":
		case "homestead_sheep_farm":
		case "homestead_hog_farm":
			return 9f;
		case "homestead_native_horse_paddock":
		case "homestead_training_field":
			return 5.5f;
		case "homestead_grain_farm":
		case "homestead_lumberjack_camp":
		case "homestead_flax_farm":
			return 4.5f;
		case "homestead_clay_gatherer":
		case "homestead_field_kitchen":
		case "homestead_chicken_coop":
		case "homestead_flax_weaver":
			return 3f;
		case "homestead_ambasador_hall":
			return 9f;
		case "homestead_tavern":
			return 10f;
		case "homestead_hero_hangout":
			return 2.5f;
		case "ballista_a":
		case "ballista_b":
		case "ballista_a_fire":
		case "ballista_b_fire":
			return 2.5f;
		case "arrow_barrel":
			return 1.5f;
		case "homestead_native_hay_cart":
		case "homestead_native_supply_wagon":
		case "homestead_table_with_benches":
		case "homestead_native_wine_cart":
			return 2.5f;
		case "fish_smoker_b":
		case "homestead_native_barrel_stack":
		case "homestead_native_water_trough":
		case "homestead_native_storage_corner":
		case "homestead_cage_wooden":
		case "homestead_smithy":
		case "homestead_native_hand_cart":
		case "homestead_well":
			return 2f;
		case "homestead_native_dirt_mound":
		case "homestead_native_work_table":
		case "homestead_campfire":
		case "homestead_native_cutting_table":
		case "homestead_native_hitching_post":
		case "homestead_native_tavern_table":
		case "homestead_war_banner_big":
		case "homestead_native_log_bench":
		case "archery_target":
		case "homestead_native_decorated_work_table":
			return 1.5f;
		case "homestead_keepout_small":
			return 4f;
		case "homestead_keepout_large":
			return 8f;
		case "homestead_winery":
			return 9f;
		case "homestead_pottery_kiln":
			return 5.5f;
		case "homestead_iron_mine":
		case "homestead_silver_mine":
			return 5.5f;
		case "homestead_silversmith":
			return 3.5f;
		case "homestead_market":
			return 6f;
		case "homestead_nav_point":
			return 0f;
		case "torch_short_a":
		case "torch_long_d2":
		case "torch_table_a_fire":
		case "torch_wall_b":
		case "homestead_native_flower_pottery_decor":
			return 0f;
		default:
			if (prefabName.Contains("fence"))
			{
				return 1.5f;
			}
			if (prefabName.Contains("wall"))
			{
				return 2f;
			}
			if (prefabName.Contains("barricade"))
			{
				return 2f;
			}
			if (prefabName.Contains("stakes"))
			{
				return 2f;
			}
			if (prefabName.Contains("platform"))
			{
				return 1.5f;
			}
			if (prefabName.Contains("stairs"))
			{
				return 1.5f;
			}
			if (prefabName.Contains("ladder"))
			{
				return 1f;
			}
			return 2f;
		}
	}

	public static List<HomesteadScenePlaceable> GetFromXMLs(int tier)
	{
		List<HomesteadScenePlaceable> list = new List<HomesteadScenePlaceable>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string location = Assembly.GetExecutingAssembly().Location;
			if (!string.IsNullOrEmpty(location))
			{
				string directoryName = Path.GetDirectoryName(location);
				if (!string.IsNullOrEmpty(directoryName))
				{
					string text = Directory.GetParent(directoryName)?.FullName;
					if (!string.IsNullOrEmpty(text))
					{
						string text2 = Directory.GetParent(text)?.FullName;
						if (!string.IsNullOrEmpty(text2))
						{
							string text3 = Path.Combine(text2, "HomesteadsPlaceables.xml");
							if (File.Exists(text3))
							{
								TraceLogger.Write("HomesteadScenePlaceable", $"Robust load: loading own placeables from '{text3}' for tier={tier}");
								list.AddRange(GetFromXMLStringPath(text3, tier));
								hashSet.Add(Path.GetFullPath(text3));
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadScenePlaceable", "Failed to load own placeables directly: " + ex.Message);
		}
		List<string> list2 = new List<string>();
		try
		{
			string text4 = Path.Combine(BasePath.Name, "Modules");
			if (Directory.Exists(text4))
			{
				list2.Add(text4);
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadScenePlaceable", "Failed to resolve BasePath modules directory: " + ex2.Message);
		}
		try
		{
			string location2 = Assembly.GetExecutingAssembly().Location;
			if (!string.IsNullOrEmpty(location2))
			{
				string text5 = Directory.GetParent(location2)?.FullName;
				string text6 = ((text5 == null) ? null : Directory.GetParent(text5)?.FullName);
				string text7 = ((text6 == null) ? null : Directory.GetParent(text6)?.FullName);
				string text8 = ((text7 == null) ? null : Directory.GetParent(text7)?.FullName);
				if (!string.IsNullOrEmpty(text8) && Directory.Exists(text8) && !list2.Contains(text8))
				{
					list2.Add(text8);
				}
			}
		}
		catch (Exception ex3)
		{
			TraceLogger.Write("HomesteadScenePlaceable", "Failed to resolve 4-level-up fallback directory: " + ex3.Message);
		}
		foreach (string item in list2)
		{
			try
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(item);
				if (!directoryInfo.Exists)
				{
					continue;
				}
				DirectoryInfo[] directories = directoryInfo.GetDirectories();
				foreach (DirectoryInfo directoryInfo2 in directories)
				{
					try
					{
						FileInfo[] files = directoryInfo2.GetFiles("HomesteadsPlaceables.xml");
						for (int j = 0; j < files.Length; j++)
						{
							string fullPath = Path.GetFullPath(files[j].FullName);
							if (!hashSet.Contains(fullPath))
							{
								TraceLogger.Write("HomesteadScenePlaceable", $"Robust load: loading submod placeables from '{fullPath}' for tier={tier}");
								list.AddRange(GetFromXMLStringPath(fullPath, tier));
								hashSet.Add(fullPath);
							}
						}
					}
					catch (Exception ex4)
					{
						TraceLogger.Write("HomesteadScenePlaceable", "Error scanning subdirectory '" + directoryInfo2.Name + "': " + ex4.Message);
					}
				}
			}
			catch (Exception ex5)
			{
				TraceLogger.Write("HomesteadScenePlaceable", "Error scanning modules directory '" + item + "': " + ex5.Message);
			}
		}
		return list;
	}

	private static List<HomesteadScenePlaceable> GetFromXMLStringPath(string fullXMLPath, int tier)
	{
		TraceLogger.Write("HomesteadScenePlaceable", $"Loading scene placeables from path='{fullXMLPath}' tier={tier}");
		List<HomesteadScenePlaceable> list = new List<HomesteadScenePlaceable>();
		foreach (XElement item in XElement.Load(fullXMLPath).Descendants("Placeable"))
		{
			int num = (int)item.Element("tierRequired");
			if (num > tier)
			{
				continue;
			}
			string value = item.Element("menuCategory").Value;
			string value2 = item.Element("displayName").Value;
			string value3 = item.Element("description").Value;
			string value4 = item.Element("prefabName").Value;
			int buildPointsRequired = (int)item.Element("buildPointsRequired");
			int productivity = (int)item.Element("productivityIncrease");
			int space = (int)item.Element("spaceIncrease");
			int leisure = (int)item.Element("leisureIncrease");
			int valueOrDefault = ((int?)item.Element("medicalCareIncrease")).GetValueOrDefault();
			int valueOrDefault2 = ((int?)item.Element("maxBuildCount")).GetValueOrDefault();
			string npcAction = ((string?)item.Element("npcAction")) ?? "";
			string npcColor = ((string?)item.Element("npcColor")) ?? "";
			List<HomesteadScenePlaceableProducedItem> list2 = new List<HomesteadScenePlaceableProducedItem>();
			XElement xElement = item.Element("ProduceItems");
			if (xElement != null)
			{
				foreach (XElement item2 in xElement.Descendants("ProduceItem"))
				{
					string value5 = item2.Element("name").Value;
					int amountToProduce = (int)item2.Element("amount");
					float dailyChance = (float)item2.Element("dailyChance");
					Dictionary<string, int> dictionary = new Dictionary<string, int>();
					XElement xElement2 = item2.Element("RequiredItems");
					if (xElement2 != null)
					{
						foreach (XElement item3 in xElement2.Descendants("RequiredItem"))
						{
							string value6 = item3.Element("name").Value;
							int value7 = (int)item3.Element("amount");
							dictionary[value6] = value7;
						}
					}
					list2.Add(new HomesteadScenePlaceableProducedItem(value5, amountToProduce, dailyChance, dictionary));
				}
			}
			Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
			XElement xElement3 = item.Element("ItemRequirements");
			if (xElement3 != null)
			{
				foreach (XElement item4 in xElement3.Descendants("Item"))
				{
					dictionary2.Add(item4.Element("name").Value, (int)item4.Element("amount"));
				}
			}
			HomesteadScenePlaceable homesteadScenePlaceable = new HomesteadScenePlaceable(value, value2, value3, value4, buildPointsRequired, productivity, space, leisure, valueOrDefault, valueOrDefault2, list2, dictionary2, npcAction, npcColor);
			homesteadScenePlaceable.TierRequired = num;
			list.Add(homesteadScenePlaceable);
		}
		return list;
	}
}
