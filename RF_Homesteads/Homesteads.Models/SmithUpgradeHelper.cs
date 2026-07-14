using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace Homesteads.Models;

public static class SmithUpgradeHelper
{
	public static List<SmithUpgradeOption> CollectOptions(Hero hero)
	{
		List<SmithUpgradeOption> list = new List<SmithUpgradeOption>();
		if (hero == null)
		{
			return list;
		}
		for (int i = 0; i < 12; i++)
		{
			AddEquipmentIfUpgradable(list, hero.BattleEquipment[(EquipmentIndex)i], civilian: false, (EquipmentIndex)i);
			AddEquipmentIfUpgradable(list, hero.CivilianEquipment[(EquipmentIndex)i], civilian: true, (EquipmentIndex)i);
		}
		ItemRoster itemRoster = MobileParty.MainParty?.ItemRoster;
		if (itemRoster != null)
		{
			foreach (ItemRosterElement item in itemRoster)
			{
				EquipmentElement equipmentElement = item.EquipmentElement;
				if (equipmentElement.Item != null)
				{
					ItemModifier nextModifier = GetNextModifier(equipmentElement);
					if (nextModifier != null)
					{
						list.Add(new SmithUpgradeOption
						{
							IsInventory = true,
							Item = equipmentElement.Item,
							Current = equipmentElement,
							Target = nextModifier,
							Cost = GetCost(equipmentElement, nextModifier)
						});
					}
				}
			}
		}
		return list;
	}

	private static void AddEquipmentIfUpgradable(List<SmithUpgradeOption> list, EquipmentElement ee, bool civilian, EquipmentIndex slot)
	{
		if (ee.Item != null)
		{
			ItemModifier nextModifier = GetNextModifier(ee);
			if (nextModifier != null)
			{
				list.Add(new SmithUpgradeOption
				{
					IsInventory = false,
					IsCivilian = civilian,
					Slot = slot,
					Item = ee.Item,
					Current = ee,
					Target = nextModifier,
					Cost = GetCost(ee, nextModifier)
				});
			}
		}
	}

	public static ItemModifier? GetNextModifier(EquipmentElement ee)
	{
		ItemModifierGroup modifierGroup = GetModifierGroup(ee.Item);
		if (modifierGroup?.ItemModifiers == null)
		{
			return null;
		}
		float num = ee.ItemModifier?.PriceMultiplier ?? 1f;
		ItemModifier result = null;
		float num2 = float.MaxValue;
		foreach (ItemModifier itemModifier in modifierGroup.ItemModifiers)
		{
			if (itemModifier != null && itemModifier.PriceMultiplier > num && itemModifier.PriceMultiplier < num2)
			{
				num2 = itemModifier.PriceMultiplier;
				result = itemModifier;
			}
		}
		return result;
	}

	private static ItemModifierGroup? GetModifierGroup(ItemObject item)
	{
		if (item?.ItemComponent == null || item.ItemType == ItemObject.ItemTypeEnum.Horse)
		{
			return null;
		}
		if (item.IsCraftedWeapon)
		{
			return null;
		}
		return item.ItemComponent.ItemModifierGroup;
	}

	public static int GetCost(EquipmentElement current, ItemModifier target)
	{
		int itemValue = current.ItemValue;
		int itemValue2 = new EquipmentElement(current.Item, target).ItemValue;
		return Math.Max(0, (int)(0.5f * (float)(itemValue2 - itemValue)));
	}
}
