using System;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace SOTOR.MagicAccessories;

public static class MagicAccessoryService
{
	public static string GetEquippedItemId(Hero hero, MagicAccessorySlot slot)
	{
		HeroExtendedInfo info = hero?.GetExtendedInfo();
		if (info == null)
		{
			return null;
		}
		return slot == MagicAccessorySlot.Ring ? info.EquippedRingItemId : info.EquippedNecklaceItemId;
	}

	public static MagicAccessoryData GetEquippedAccessory(Hero hero, MagicAccessorySlot slot)
	{
		string itemId = GetEquippedItemId(hero, slot);
		if (MagicAccessoryRegistry.TryGet(itemId, out MagicAccessoryData accessory) && accessory.Slot == slot)
		{
			return accessory;
		}
		return null;
	}

	public static MagicAccessoryBonuses GetBonuses(Hero hero)
	{
		if (hero == null)
		{
			return MagicAccessoryBonuses.Neutral;
		}

		float maxWinds = 0f;
		float recharge = 1f;
		float effectiveness = 1f;
		float windsCost = 1f;
		float cooldown = 1f;

		Add(GetEquippedAccessory(hero, MagicAccessorySlot.Ring), ref maxWinds, ref recharge, ref effectiveness, ref windsCost, ref cooldown);
		Add(GetEquippedAccessory(hero, MagicAccessorySlot.Necklace), ref maxWinds, ref recharge, ref effectiveness, ref windsCost, ref cooldown);

		MagicAccessoryBonuses runeBonuses = MagicRuneService.GetBonuses(hero);
		maxWinds += runeBonuses.MaxWindsBonus;
		recharge *= runeBonuses.RechargeMultiplier;
		effectiveness *= runeBonuses.EffectivenessMultiplier;
		windsCost *= runeBonuses.WindsCostMultiplier;
		cooldown *= runeBonuses.CooldownMultiplier;

		return new MagicAccessoryBonuses(maxWinds, recharge, effectiveness, windsCost, cooldown);
	}

	public static float ApplyMaxWinds(Hero hero, float currentMaximum)
	{
		if (currentMaximum <= 0f)
		{
			return currentMaximum;
		}
		return Math.Max(0f, currentMaximum + GetBonuses(hero).MaxWindsBonus);
	}

	public static int ApplyWindsCost(Hero hero, int currentCost)
	{
		if (currentCost <= 0)
		{
			return currentCost;
		}
		return Math.Max(1, (int)Math.Round(currentCost * GetBonuses(hero).WindsCostMultiplier));
	}

	public static int ApplyCooldown(Hero hero, int currentCooldown)
	{
		if (currentCooldown <= 0)
		{
			return currentCooldown;
		}
		return Math.Max(1, (int)Math.Round(currentCooldown * GetBonuses(hero).CooldownMultiplier));
	}

	public static bool TryEquip(Hero hero, MagicAccessorySlot slot, string itemId, ItemRoster inventory, out string failure)
	{
		failure = null;
		HeroExtendedInfo info = hero?.GetExtendedInfo();
		if (info == null || inventory == null)
		{
			failure = "Hero or inventory is unavailable.";
			return false;
		}

		if (!MagicAccessoryRegistry.TryGet(itemId, out MagicAccessoryData accessory) || accessory.Slot != slot)
		{
			failure = "This item does not belong in that accessory slot.";
			return false;
		}

		string previousId = GetEquippedItemId(hero, slot);
		if (string.Equals(previousId, itemId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		ItemObject newItem = ResolveItem(itemId);
		if (newItem == null || inventory.GetItemNumber(newItem) < 1)
		{
			failure = "The accessory is not present in the inventory.";
			return false;
		}

		inventory.AddToCounts(newItem, -1);
		ItemObject previousItem = ResolveItem(previousId);
		if (previousItem != null)
		{
			inventory.AddToCounts(previousItem, 1);
		}
		SetEquippedItemId(info, slot, itemId);
		return true;
	}

	public static bool TryAssign(Hero hero, MagicAccessorySlot slot, string itemId, out string failure)
	{
		failure = null;
		HeroExtendedInfo info = hero?.GetExtendedInfo();
		if (info == null)
		{
			failure = "Hero is unavailable.";
			return false;
		}

		if (!MagicAccessoryRegistry.TryGet(itemId, out MagicAccessoryData accessory) || accessory.Slot != slot)
		{
			failure = "This item does not belong in that accessory slot.";
			return false;
		}

		SetEquippedItemId(info, slot, itemId);
		return true;
	}

	public static bool TryClear(Hero hero, MagicAccessorySlot slot)
	{
		HeroExtendedInfo info = hero?.GetExtendedInfo();
		if (info == null)
		{
			return false;
		}

		SetEquippedItemId(info, slot, null);
		return true;
	}

	public static ItemObject GetItemObject(string itemId)
	{
		return ResolveItem(itemId);
	}

	public static bool TryUnequip(Hero hero, MagicAccessorySlot slot, ItemRoster inventory, out string failure)
	{
		failure = null;
		HeroExtendedInfo info = hero?.GetExtendedInfo();
		if (info == null || inventory == null)
		{
			failure = "Hero or inventory is unavailable.";
			return false;
		}

		string itemId = GetEquippedItemId(hero, slot);
		if (string.IsNullOrEmpty(itemId))
		{
			return true;
		}

		ItemObject item = ResolveItem(itemId);
		SetEquippedItemId(info, slot, null);
		if (item == null)
		{
			failure = "The equipped accessory no longer exists; the invalid slot was cleared.";
			return false;
		}

		inventory.AddToCounts(item, 1);
		return true;
	}

	private static void Add(MagicAccessoryData accessory, ref float maxWinds, ref float recharge,
		ref float effectiveness, ref float windsCost, ref float cooldown)
	{
		if (accessory == null)
		{
			return;
		}
		maxWinds += accessory.MaxWindsBonus;
		recharge *= accessory.RechargeMultiplier;
		effectiveness *= accessory.EffectivenessMultiplier;
		windsCost *= accessory.WindsCostMultiplier;
		cooldown *= accessory.CooldownMultiplier;
	}

	private static ItemObject ResolveItem(string itemId)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			return null;
		}
		return MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
	}

	private static void SetEquippedItemId(HeroExtendedInfo info, MagicAccessorySlot slot, string itemId)
	{
		if (slot == MagicAccessorySlot.Ring)
		{
			info.SetEquippedRingItemId(itemId);
		}
		else
		{
			info.SetEquippedNecklaceItemId(itemId);
		}
	}
}
