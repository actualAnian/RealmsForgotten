using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Homesteads.Models;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Homesteads;

public static class Utils
{
	private const float AnimalPenPlacementScale = 1.2f;

	private static readonly HashSet<string> ScaledAnimalPenPrefabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "homestead_hog_farm", "homestead_sheep_farm", "homestead_cattle_pen" };

	private static bool? _isNavalDlcLoadedCache;

	public static void ApplyRandomPersonalityTraits(Hero? hero)
	{
		if (hero == null)
		{
			return;
		}
		try
		{
			List<TraitObject> list = new List<TraitObject>
			{
				DefaultTraits.Honor,
				DefaultTraits.Mercy,
				DefaultTraits.Valor,
				DefaultTraits.Generosity,
				DefaultTraits.Calculating
			};
			float randomFloat = MBRandom.RandomFloat;
			int num = ((!(randomFloat < 0.1f)) ? ((randomFloat < 0.5f) ? 1 : ((randomFloat < 0.9f) ? 2 : 3)) : 0);
			for (int i = 0; i < num; i++)
			{
				if (list.Count <= 0)
				{
					break;
				}
				int index = MBRandom.RandomInt(list.Count);
				TraitObject traitObject = list[index];
				list.RemoveAt(index);
				int value = ((MBRandom.RandomFloat < 0.5f) ? MBRandom.RandomInt(traitObject.MinValue, 0) : MBRandom.RandomInt(1, traitObject.MaxValue + 1));
				hero.SetTraitLevel(traitObject, value);
			}
		}
		catch
		{
		}
	}

	public static bool IsNavalDlcLoaded()
	{
		if (_isNavalDlcLoadedCache.HasValue)
		{
			return _isNavalDlcLoadedCache.Value;
		}
		try
		{
			_isNavalDlcLoadedCache = AppDomain.CurrentDomain.GetAssemblies().SelectMany(delegate(Assembly a)
			{
				try
				{
					return a.GetTypes();
				}
				catch
				{
					return Array.Empty<Type>();
				}
			}).Any((Type t) => t.FullName == "NavalDLC.CampaignBehaviors.ShipUpgradeCampaignBehavior");
		}
		catch
		{
			_isNavalDlcLoadedCache = false;
		}
		return _isNavalDlcLoadedCache.Value;
	}

	public static ItemObject? GetItemFromID(string itemID)
	{
		return Campaign.Current.ObjectManager.GetObject<ItemObject>(itemID);
	}

	public static bool DoesItemRosterHaveItems(ItemRoster itemRoster, Dictionary<string, int> itemsRequired, bool takeItems = false)
	{
		if (itemsRequired.Count == 0)
		{
			return true;
		}
		Dictionary<ItemObject, int> dictionary = new Dictionary<ItemObject, int>();
		foreach (KeyValuePair<string, int> item in itemsRequired)
		{
			string[] array = item.Key.Split(new char[1] { '|' });
			ItemObject itemObject = null;
			ItemRosterElement invalid = ItemRosterElement.Invalid;
			string[] array2 = array;
			foreach (string objectName in array2)
			{
				ItemObject thisItem = Campaign.Current.ObjectManager.GetObject<ItemObject>(objectName);
				if (thisItem == null)
				{
					PrintDebugMessage(item.Key + " IS NOT A VALID ITEM ID", 255f, 0f, 0f);
					continue;
				}
				try
				{
					if (itemRoster.First((ItemRosterElement x) => x.EquipmentElement.Item == thisItem).Amount < item.Value)
					{
						invalid = ItemRosterElement.Invalid;
						continue;
					}
					itemObject = thisItem;
				}
				catch (InvalidOperationException)
				{
					continue;
				}
				break;
			}
			if (itemObject == null)
			{
				return false;
			}
			dictionary[itemObject] = item.Value;
		}
		if (takeItems)
		{
			foreach (KeyValuePair<ItemObject, int> item2 in dictionary)
			{
				itemRoster.AddToCounts(item2.Key, -item2.Value);
			}
		}
		return true;
	}

	public static void DiagnosePrefabXml(string prefabName)
	{
		try
		{
			string text = System.IO.Path.Combine(System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "..", "..")), "_Module", "Prefabs", prefabName + ".xml");
			if (!File.Exists(text))
			{
				TraceLogger.Write("PrefabDiag", "XML not found at '" + text + "' — prefab may be native (no child diagnosis available).");
				return;
			}
			TraceLogger.Write("PrefabDiag", "=== Diagnosing '" + prefabName + "' from: " + text + " ===");
			foreach (XElement item in XDocument.Load(text).Descendants("game_entity"))
			{
				string text2 = item.Attribute("name")?.Value ?? "(unnamed)";
				string text3 = item.Attribute("old_prefab_name")?.Value ?? "";
				string text4 = item.Attribute("prefab")?.Value ?? "";
				if (!string.IsNullOrEmpty(text4))
				{
					TraceLogger.Write("PrefabDiag", "  [BAD]  '" + text2 + "': prefab=\"" + text4 + "\" is scene-file syntax — use old_prefab_name=\"" + text4 + "\" instead.");
					continue;
				}
				if (!string.IsNullOrEmpty(text3))
				{
					bool flag = GameEntity.PrefabExists(text3);
					TraceLogger.Write("PrefabDiag", "  [" + (flag ? " OK " : "MISS") + "]  '" + text2 + "': old_prefab_name=\"" + text3 + "\"");
				}
				foreach (XElement item2 in item.Descendants("meta_mesh_component"))
				{
					string text5 = item2.Attribute("name")?.Value ?? "";
					if (!string.IsNullOrEmpty(text5))
					{
						TraceLogger.Write("PrefabDiag", "  [MESH]  '" + text2 + "': meta_mesh_component=\"" + text5 + "\"");
					}
				}
				if (item.Descendants("particle_system_instanced_component").Any())
				{
					TraceLogger.Write("PrefabDiag", "  [BAD]  '" + text2 + "': particle_system_instanced_component with GUID — invalid in prefab XML; use old_prefab_name=\"torch_outdoors_a_burning\" or a named particle_system_component instead.");
				}
				foreach (XElement item3 in item.Elements("scripts").Elements("script"))
				{
					string text6 = item3.Attribute("name")?.Value ?? "";
					if (!string.IsNullOrEmpty(text6))
					{
						TraceLogger.Write("PrefabDiag", "  [SCRP]  '" + text2 + "': script=\"" + text6 + "\"");
					}
				}
			}
			TraceLogger.Write("PrefabDiag", "=== End diagnosis for '" + prefabName + "' ===");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("PrefabDiag", "DiagnosePrefabXml threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public static GameEntity CreateGameEntityWithPrefab(string prefabName, Vec3 position, Mat3 rotation, bool enablePhysics = true, bool makeStatic = true, bool applyPhysicsState = true, bool callScriptCallbacks = true)
	{
		MatrixFrame frame = MatrixFrame.Identity;
		frame.rotation = ApplyPrefabPlacementScale(prefabName, rotation);
		frame.origin = position;
		GameEntity gameEntity = GameEntity.Instantiate(Mission.Current.Scene, prefabName, frame, callScriptCallbacks);
		gameEntity.SetGlobalFrame(in frame);
		if (applyPhysicsState)
		{
			ApplyEntityPhysicsState(gameEntity, enablePhysics, makeStatic);
		}
		return gameEntity;
	}

	public static Mat3 ApplyPrefabPlacementScale(string prefabName, Mat3 rotation)
	{
		if (!ScaledAnimalPenPrefabs.Contains(prefabName))
		{
			return rotation;
		}
		Mat3 result = rotation;
		result.ApplyScaleLocal(new Vec3(1.2f, 1.2f, 1.2f));
		return result;
	}

	private static void ApplyEntityPhysicsState(GameEntity entity, bool enablePhysics, bool makeStatic)
	{
		entity.SetPhysicsState(enablePhysics, setChildren: true);
		if (makeStatic && !IsUsableEntity(entity))
		{
			entity.SetMobility(GameEntity.Mobility.Stationary);
		}
		foreach (GameEntity child in entity.GetChildren())
		{
			ApplyEntityPhysicsState(child, enablePhysics, makeStatic);
		}
	}

	private static bool IsUsableEntity(GameEntity entity)
	{
		try
		{
			return entity.GetFirstScriptOfType<UsableMachine>() != null || entity.GetFirstScriptOfType<UsableMissionObject>() != null;
		}
		catch
		{
			return false;
		}
	}

	public static void ShowNameHomesteadScreen(Homestead homestead, Action? doneAction = null)
	{
		string localizedString = GetLocalizedString("{=homestead_rename_title}Name Homestead");
		string localizedString2 = GetLocalizedString("{=homestead_rename_text}What would you like to name this homestead?");
		ShowTextInputMessage(localizedString, localizedString2, delegate(string name)
		{
			homestead.ChangeName(name);
			if (doneAction != null)
			{
				doneAction();
			}
		});
	}

	public static void ShowSelectNewHomesteadLeaderScreen(Homestead homestead, bool fromHomesteadMenu = false, bool endConversation = false)
	{
		ShowHeroSelectionScreen(new TextObject("{=homestead_choose_new_leader}CHOOSE NEW HOMESTEAD LEADER").ToString(), GetLocalizedString("{=homestead_choose_new_leader_body}Choose a companion to lead the homestead."), Campaign.Current.AliveHeroes.Where((Hero x) => x.PartyBelongedTo != null && x.PartyBelongedTo == MobileParty.MainParty && !x.IsHumanPlayerCharacter).ToList(), delegate(List<InquiryElement> elements)
		{
			if (elements == null || elements.Count == 0)
			{
				TraceLogger.Write("Utils", "ShowSelectNewHomesteadLeaderScreen: No elements selected in callback.");
			}
			else if (!(elements[0].Identifier is Hero hero))
			{
				TraceLogger.Write("Utils", "ShowSelectNewHomesteadLeaderScreen: Selected element Identifier is not a Hero: " + (elements[0].Identifier?.GetType()?.Name ?? "null"));
			}
			else
			{
				TraceLogger.Write("Utils", string.Format("ShowSelectNewHomesteadLeaderScreen: Changing leader to '{0}' for homestead '{1}'. MobileParty={2}, Leader={3}", hero.Name, homestead.Name, homestead.MobileParty?.StringId ?? "NULL", homestead.MobileParty?.LeaderHero?.Name?.ToString() ?? "NULL"));
				if (endConversation)
				{
					Campaign.Current.ConversationManager.EndConversation();
				}
				homestead.DoChangePartyLeader(hero);
				TraceLogger.Write("Utils", "ShowSelectNewHomesteadLeaderScreen: After ChangePartyLeader call. MobileParty.LeaderHero=" + (homestead.MobileParty?.LeaderHero?.Name?.ToString() ?? "NULL") + ", homestead.Leader=" + (homestead.Leader?.Name?.ToString() ?? "NULL"));
				if (fromHomesteadMenu)
				{
					GameMenu.SwitchToMenu("homestead_menu_main");
				}
			}
		});
	}

	public static void ShowHeroSelectionScreen(string title, string text, List<Hero> heroes, Action<List<InquiryElement>> onPressedOk)
	{
		ShowHeroSelectionScreen(title, text, heroes, 1, 1, onPressedOk);
	}

	public static void ShowHeroSelectionScreen(string title, string text, List<Hero> heroes, int minCount, int maxCount, Action<List<InquiryElement>> onPressedOk, Action<List<InquiryElement>>? onCancel = null)
	{
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (Hero hero in heroes)
		{
			InquiryElement item = new InquiryElement(hero, hero.Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(hero.CharacterObject)), isEnabled: true, GetHeroPropertiesHint(hero));
			list.Add(item);
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, text, list, isExitShown: true, minCount, maxCount, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), onPressedOk, onCancel), pauseGameActiveState: true, prioritize: true);
	}

	public static void ShowMessageBox(string title, string text, bool pauseGameActiveState = true, bool priority = true)
	{
		InformationManager.ShowInquiry(new InquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_done").ToString(), null, null, null), pauseGameActiveState, priority);
	}

	public static void ShowTextInputMessage(string title, string text, Action<string> onPressedOk)
	{
		InformationManager.ShowTextInquiry(new TextInquiryData(title, text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_done").ToString(), null, onPressedOk, null), pauseGameActiveState: true, prioritize: true);
	}

	public static string GetLocalizedString(string str, params (string, string)[] textVars)
	{
		TextObject textObject = new TextObject(str);
		for (int i = 0; i < textVars.Length; i++)
		{
			(string, string) tuple = textVars[i];
			textObject.SetTextVariable(tuple.Item1, tuple.Item2);
		}
		return textObject.ToString();
	}

	public static void PrintLocalizedMessage(string localizationString, string str, float r = 255f, float g = 255f, float b = 255f, params (string, string)[] textVars)
	{
		float[] array = new float[3]
		{
			r / 255f,
			g / 255f,
			b / 255f
		};
		InformationManager.DisplayMessage(new InformationMessage(color: new Color(array[0], array[1], array[2]), information: GetLocalizedString("{=" + localizationString + "}" + str, textVars)));
	}

	public static void PrintDebugMessage(string str, float r = 255f, float g = 255f, float b = 255f)
	{
		MCMSettings? instance = GlobalSettings<MCMSettings>.Instance;
		if (instance != null && instance.EnableDebugLogging)
		{
			float red = r / 255f;
			float green = g / 255f;
			float blue = b / 255f;
			InformationManager.DisplayMessage(new InformationMessage(str, new Color(red, green, blue)));
		}
	}

	private static string GetHeroPropertiesHint(Hero hero)
	{
		GameTexts.SetVariable("newline", "\n");
		string content = hero.Name.ToString();
		TextObject textObject = GameTexts.FindText("str_STR1_space_STR2");
		textObject.SetTextVariable("STR1", GameTexts.FindText("str_enc_sf_age").ToString());
		textObject.SetTextVariable("STR2", ((int)hero.Age).ToString());
		string content2 = GameTexts.FindText("str_attributes").ToString();
		foreach (CharacterAttribute item in Attributes.All)
		{
			GameTexts.SetVariable("LEFT", item.Name.ToString());
			GameTexts.SetVariable("RIGHT", hero.GetAttributeValue(item));
			string content3 = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString();
			GameTexts.SetVariable("STR1", content2);
			GameTexts.SetVariable("STR2", content3);
			content2 = GameTexts.FindText("str_string_newline_string").ToString();
		}
		int num = 0;
		string content4 = GameTexts.FindText("str_skills").ToString();
		foreach (SkillObject item2 in Skills.All)
		{
			int skillValue = hero.GetSkillValue(item2);
			if (skillValue > 50)
			{
				GameTexts.SetVariable("LEFT", item2.Name.ToString());
				GameTexts.SetVariable("RIGHT", skillValue);
				string content5 = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString();
				GameTexts.SetVariable("STR1", content4);
				GameTexts.SetVariable("STR2", content5);
				content4 = GameTexts.FindText("str_string_newline_string").ToString();
				num++;
			}
		}
		GameTexts.SetVariable("STR1", content);
		GameTexts.SetVariable("STR2", textObject.ToString());
		string content6 = GameTexts.FindText("str_string_newline_string").ToString();
		GameTexts.SetVariable("newline", "\n \n");
		GameTexts.SetVariable("STR1", content6);
		GameTexts.SetVariable("STR2", content2);
		content6 = GameTexts.FindText("str_string_newline_string").ToString();
		if (num > 0)
		{
			GameTexts.SetVariable("STR1", content6);
			GameTexts.SetVariable("STR2", content4);
			content6 = GameTexts.FindText("str_string_newline_string").ToString();
		}
		GameTexts.SetVariable("newline", "\n");
		return content6;
	}
}
