using System;
using System.Collections.Generic;
using System.Reflection;
using NecromancyAndSummoning.CustomClass;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace NecromancyAndSummoning
{
	// Token: 0x02000002 RID: 2
	internal class BuildingTroopFromParts
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public static void GetBodyPart()
		{
			string[] boneParts = SubModule.UnitBuildFromPartConfig.BoneParts;
			int maxValue = BuildingTroopFromParts.CalculateBodyPart();
			for (int i = 0; i < boneParts.Length; i++)
			{
				int amount = BuildingTroopFromParts.random.Next(maxValue);
				BuildingTroopFromParts.GiveBodyPart(boneParts[i], amount);
			}
		}

		// Token: 0x06000002 RID: 2 RVA: 0x0000209C File Offset: 0x0000029C
		public static int CalculateBodyPart()
		{
			MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
			bool flag = playerMapEvent != null;
			if (flag)
			{
				bool flag2 = playerMapEvent.DefeatedSide == BattleSideEnum.Attacker;
				MapEventSide mapEventSide;
				if (flag2)
				{
					mapEventSide = playerMapEvent.AttackerSide;
				}
				else
				{
					mapEventSide = playerMapEvent.DefenderSide;
				}
				bool flag3 = mapEventSide.Casualties > 0;
				if (flag3)
				{
					return BuildingTroopFromParts.random.Next(mapEventSide.Casualties);
				}
			}
			return 0;
		}

		// Token: 0x06000003 RID: 3 RVA: 0x0000210C File Offset: 0x0000030C
		private static void GiveBodyPart(string itemId, int amount)
		{
			ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
			bool flag = @object != null;
			if (flag)
			{
				MobileParty.MainParty.ItemRoster.AddToCounts(@object, amount);
				string value = "{=get_body_part}You've get {amount} {body_part}";
				TextObject textObject = new TextObject(value, null);
				textObject.SetTextVariable("amount", amount);
				textObject.SetTextVariable("body_part", @object.Name);
				InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
			}
			else
			{
				string value2 = "{=invalid_body_part}Invalid body part to be given";
				TextObject textObject2 = new TextObject(value2, null);
				InformationManager.DisplayMessage(new InformationMessage(textObject2.ToString()));
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000021A8 File Offset: 0x000003A8
		public static void BuildTroopMenu(CampaignGameStarter campaignGameStarter)
		{
			bool enableBuildTroopFromPart = SubModule.Config.EnableBuildTroopFromPart;
			if (enableBuildTroopFromPart)
			{
				campaignGameStarter.AddGameMenuOption("village", "necromancy_enter_menu", "{=necromancy_enter_menu}Enter Necromancy Menu", delegate(MenuCallbackArgs args)
				{
					args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
					return true;
				}, delegate(MenuCallbackArgs args)
				{
					GameMenu.SwitchToMenu("necromancy_menu");
				}, false, 1, false, null);
				campaignGameStarter.AddGameMenu("necromancy_menu", "{=necromancy_menu}Necromancy Menu\n{bone_info}", delegate(MenuCallbackArgs args)
				{
					BuildingTroopFromParts.UpdateTextVariables();
				}, GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
				campaignGameStarter.AddGameMenuOption("necromancy_menu", "necromancy_menu_leave", "{=necromancy_menu_leave}Leave", delegate(MenuCallbackArgs args)
				{
					args.optionLeaveType = GameMenuOption.LeaveType.Trade;
					return true;
				}, delegate(MenuCallbackArgs args)
				{
					GameMenu.SwitchToMenu("village");
				}, false, 1, false, null);
				List<UnitBuildFromPart> unitBuildFromPart = SubModule.UnitBuildFromPartConfig.UnitBuildFromPart;
				for (int i = 0; i < unitBuildFromPart.Count; i++)
				{
					int[] boneNeeded = unitBuildFromPart[i].BoneNeeded;
					int amount = unitBuildFromPart[i].Amount;
					List<string> unitList = new List<string>();
					foreach (string text in unitBuildFromPart[i].UnitId)
					{
						bool flag = NecroSummon.GetCharacterObject(text) != null;
						if (flag)
						{
							unitList.Add(text);
						}
					}
					bool flag2 = unitList.Count > 0;
					if (flag2)
					{
						campaignGameStarter.AddGameMenuOption("necromancy_menu", "necromancy_menu_build_option_" + i.ToString(), string.Concat(new string[]
						{
							BuildingTroopFromParts.FormBuildUnitOptionName(unitList),
							" x ",
							amount.ToString(),
							" ",
							BuildingTroopFromParts.FormBuildUnitBoneNeeded(boneNeeded)
						}), delegate(MenuCallbackArgs args)
						{
							args.optionLeaveType = GameMenuOption.LeaveType.Trade;
							return true;
						}, delegate(MenuCallbackArgs args)
						{
							CharacterObject characterObject = NecroSummon.GetCharacterObject(BuildingTroopFromParts.GetBuildUnitId(unitList));
							bool flag3 = characterObject != null;
							if (flag3)
							{
								BuildingTroopFromParts.BuildTroop(characterObject, boneNeeded, amount);
							}
						}, false, 1, false, null);
					}
				}
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002400 File Offset: 0x00000600
		private static string FormBuildUnitOptionName(List<string> unitList)
		{
			string text = "";
			foreach (string unitId in unitList)
			{
				bool flag = unitList.Count == 1;
				if (flag)
				{
					string str = text;
					TextObject name = NecroSummon.GetCharacterObject(unitId).Name;
					text = str + ((name != null) ? name.ToString() : null);
				}
				else
				{
					string str2 = text;
					TextObject name2 = NecroSummon.GetCharacterObject(unitId).Name;
					text = str2 + ((name2 != null) ? name2.ToString() : null) + " / ";
				}
			}
			return text;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000024B0 File Offset: 0x000006B0
		private static string FormBuildUnitBoneNeeded(int[] boneNeeded)
		{
			string text = "(";
			for (int i = 0; i < boneNeeded.Length; i++)
			{
				bool flag = i + 1 == boneNeeded.Length;
				if (flag)
				{
					text = text + boneNeeded[i].ToString() + ")";
				}
				else
				{
					text = text + boneNeeded[i].ToString() + " ,";
				}
			}
			return text;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002524 File Offset: 0x00000724
		private static ItemObject GetItemObject(string itemId)
		{
			ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
			bool flag = @object != null;
			ItemObject result;
			if (flag)
			{
				result = @object;
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002550 File Offset: 0x00000750
		internal static string GetBuildUnitId(List<string> unitList)
		{
			bool flag = false;
			int num = 0;
			while (!flag)
			{
				int index = BuildingTroopFromParts.random.Next(0, unitList.Count);
				bool flag2 = NecroSummon.GetCharacterObject(unitList[index]) != null;
				string result;
				if (flag2)
				{
					result = unitList[index];
				}
				else
				{
					num++;
					bool flag3 = !flag && num > unitList.Count + 10;
					if (!flag3)
					{
						continue;
					}
					result = null;
				}
				return result;
			}
			return null;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000025CC File Offset: 0x000007CC
		public static void BuildTroop(CharacterObject unit, int[] boneNeeded, int amount)
		{
			bool flag = BuildingTroopFromParts.EnoughBone(boneNeeded);
			if (flag)
			{
				MobileParty.MainParty.AddElementToMemberRoster(unit, amount, false);
				BuildingTroopFromParts.DeductBone(boneNeeded);
				string value = "{=enough_bone}You've got {unit}";
				TextObject textObject = new TextObject(value, null);
				textObject.SetTextVariable("unit", unit.Name);
				InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
			}
			else
			{
				string value2 = "{=not_enough_bone}Not Enough Bone to build {unit}";
				TextObject textObject2 = new TextObject(value2, null);
				textObject2.SetTextVariable("unit", unit.Name);
				InformationManager.DisplayMessage(new InformationMessage(textObject2.ToString()));
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x00002664 File Offset: 0x00000864
		private static void DeductBone(int[] boneNeeded)
		{
			string[] boneParts = SubModule.UnitBuildFromPartConfig.BoneParts;
			for (int i = 0; i < boneParts.Length; i++)
			{
				ItemObject itemObject = BuildingTroopFromParts.GetItemObject(boneParts[i]);
				bool flag = itemObject != null;
				if (flag)
				{
					MobileParty.MainParty.ItemRoster.AddToCounts(itemObject, -boneNeeded[i]);
				}
				else
				{
					string value = "{=invalid_bone}Invalid bone to be deduct";
					TextObject textObject = new TextObject(value, null);
					InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
				}
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000026E4 File Offset: 0x000008E4
		private static bool EnoughBone(int[] boneNeeded)
		{
			string[] boneParts = SubModule.UnitBuildFromPartConfig.BoneParts;
			for (int i = 0; i < boneParts.Length; i++)
			{
				bool flag = !BuildingTroopFromParts.EnoughAmountOfItem(boneParts[i], boneNeeded[i]);
				if (flag)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002730 File Offset: 0x00000930
		private static bool EnoughAmountOfItem(string itemId, int amount)
		{
			ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
			FieldInfo fieldInfo = (FieldInfo)Util.GetInstanceField<ItemRoster>(itemRoster, "_data");
			ItemRosterElement[] array = (ItemRosterElement[])fieldInfo.GetValue(itemRoster);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = new string[0];
				bool flag = array[i].Amount > 0;
				if (flag)
				{
					array2 = array[i].ToString().Replace(" ", "").Split(new char[]
					{
						'x'
					});
				}
				bool flag2 = array2.Length >= 2;
				if (flag2)
				{
					bool flag3 = array2[0].Equals(itemId);
					if (flag3)
					{
						int num = 0;
						int.TryParse(array2[1], out num);
						bool flag4 = num >= amount;
						if (flag4)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002823 File Offset: 0x00000A23
		private static void UpdateTextVariables()
		{
			MBTextManager.SetTextVariable("bone_info", BuildingTroopFromParts.DisplayBoneNumber(), false);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x00002838 File Offset: 0x00000A38
		private static string DisplayBoneNumber()
		{
			string[] boneParts = SubModule.UnitBuildFromPartConfig.BoneParts;
			string text = "Current Materials: \n";
			for (int i = 0; i < boneParts.Length; i++)
			{
				ItemObject itemObject = BuildingTroopFromParts.GetItemObject(boneParts[i]);
				bool flag = itemObject != null;
				if (flag)
				{
					string[] array = new string[5];
					array[0] = text;
					int num = 1;
					TextObject name = itemObject.Name;
					array[num] = ((name != null) ? name.ToString() : null);
					array[2] = " x ";
					array[3] = MobileParty.MainParty.ItemRoster.GetItemNumber(itemObject).ToString();
					array[4] = "\n";
					text = string.Concat(array);
				}
			}
			return text;
		}

		// Token: 0x04000001 RID: 1
		private static Random random = new Random();
	}
}
