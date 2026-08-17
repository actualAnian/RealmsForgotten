using System;
using System.Collections.Generic;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.MountAndBlade;

namespace SOTOR.MagicAccessories;

public static class MagicRuneService
{
	public const int WeaponSlotCount = 4;

	public static string GetRuneItemId(Hero hero, int slotIndex)
	{
		return hero?.GetExtendedInfo()?.GetWeaponRuneItemId(slotIndex);
	}

	public static string GetTargetItemId(Hero hero, int slotIndex)
	{
		return hero?.GetExtendedInfo()?.GetWeaponRuneTargetItemId(slotIndex);
	}

	public static ItemObject GetBattleWeapon(Hero hero, int slotIndex)
	{
		if (hero?.BattleEquipment == null || slotIndex < 0 || slotIndex >= WeaponSlotCount)
		{
			return null;
		}
		return hero.BattleEquipment[(EquipmentIndex)slotIndex].Item;
	}

	public static bool TryAssign(Hero hero, Equipment equipment, int slotIndex, string runeItemId,
		string targetItemId, out string failure)
	{
		failure = null;
		HeroExtendedInfo info = hero?.GetExtendedInfo();
		if (info == null || equipment == null || slotIndex < 0 || slotIndex >= WeaponSlotCount)
		{
			failure = "This weapon slot is empty.";
			return false;
		}

		ItemObject targetItem = equipment[(EquipmentIndex)slotIndex].Item;
		if (targetItem == null)
		{
			failure = "This weapon slot is empty.";
			return false;
		}
		if (!string.Equals(targetItem.StringId, targetItemId, StringComparison.OrdinalIgnoreCase))
		{
			failure = "The weapon in this slot changed.";
			return false;
		}
		if (!MagicRuneRegistry.TryGet(runeItemId, out MagicRuneData rune) || !MagicRuneRegistry.CanApply(rune, targetItem))
		{
			failure = "This rune is not compatible with that item.";
			return false;
		}

		info.SetWeaponRune(slotIndex, runeItemId, targetItemId);
		return true;
	}

	public static MagicAccessoryBonuses GetBonuses(Hero hero)
	{
		HeroExtendedInfo info = hero?.GetExtendedInfo();
		if (info == null)
		{
			return MagicAccessoryBonuses.Neutral;
		}

		float maxWinds = 0f;
		float recharge = 1f;
		float effectiveness = 1f;
		float windsCost = 1f;
		float cooldown = 1f;

		for (int slotIndex = 0; slotIndex < WeaponSlotCount; slotIndex++)
		{
			string runeItemId = info.GetWeaponRuneItemId(slotIndex);
			string targetItemId = info.GetWeaponRuneTargetItemId(slotIndex);
			ItemObject targetItem = GetBattleWeapon(hero, slotIndex);
			if (targetItem == null || !string.Equals(targetItem.StringId, targetItemId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (!MagicRuneRegistry.TryGet(runeItemId, out MagicRuneData rune) || !MagicRuneRegistry.CanApply(rune, targetItem))
			{
				continue;
			}

			maxWinds += rune.MaxWindsBonus;
			recharge *= rune.RechargeMultiplier;
			effectiveness *= rune.EffectivenessMultiplier;
			windsCost *= rune.WindsCostMultiplier;
			cooldown *= rune.CooldownMultiplier;
		}

		return new MagicAccessoryBonuses(maxWinds, recharge, effectiveness, windsCost, cooldown);
	}

	public static bool TryAssign(Hero hero, int slotIndex, string runeItemId, string targetItemId, out string failure)
	{
		failure = null;
		HeroExtendedInfo info = hero?.GetExtendedInfo();
		ItemObject targetItem = GetBattleWeapon(hero, slotIndex);
		if (info == null || targetItem == null)
		{
			failure = "This weapon slot is empty.";
			return false;
		}
		if (!string.Equals(targetItem.StringId, targetItemId, StringComparison.OrdinalIgnoreCase))
		{
			failure = "The weapon in this slot changed.";
			return false;
		}
		if (!MagicRuneRegistry.TryGet(runeItemId, out MagicRuneData rune) || !MagicRuneRegistry.CanApply(rune, targetItem))
		{
			failure = "This rune is not compatible with that item.";
			return false;
		}

		info.SetWeaponRune(slotIndex, runeItemId, targetItemId);
		return true;
	}

	public static IReadOnlyList<KeyValuePair<string, string>> GetTooltipRows(MagicRuneData rune, bool socketed)
	{
		if (rune == null)
		{
			return Array.Empty<KeyValuePair<string, string>>();
		}

		List<KeyValuePair<string, string>> rows = new List<KeyValuePair<string, string>>
		{
			new KeyValuePair<string, string>(socketed ? "Socketed Rune" : "Rune", rune.Name),
			new KeyValuePair<string, string>("Rune tier", rune.Tier.ToString()),
			new KeyValuePair<string, string>("Compatible with", FormatTargets(rune.Targets)),
			new KeyValuePair<string, string>("Effect", string.IsNullOrEmpty(rune.Description) ? "None" : rune.Description)
		};
		string bonuses = BuildBonusText(rune);
		if (!string.IsNullOrEmpty(bonuses))
		{
			rows.Add(new KeyValuePair<string, string>("Magic bonuses", bonuses));
		}
		return rows;
	}

	private static string FormatTargets(MagicRuneTarget targets)
	{
		List<string> names = new List<string>();
		if ((targets & MagicRuneTarget.Melee) != 0) names.Add("Melee");
		if ((targets & MagicRuneTarget.Shield) != 0) names.Add("Shield");
		if ((targets & MagicRuneTarget.Bow) != 0) names.Add("Bow");
		if ((targets & MagicRuneTarget.Thrown) != 0) names.Add("Thrown");
		if ((targets & MagicRuneTarget.Ammunition) != 0) names.Add("Ammunition");
		if ((targets & MagicRuneTarget.MagicFocus) != 0) names.Add("Staff/Wand");
		return names.Count == 0 ? "None" : string.Join(", ", names);
	}

	private static string BuildBonusText(MagicRuneData rune)
	{
		List<string> bonuses = new List<string>();
		if (Math.Abs(rune.MaxWindsBonus) > 0.001f) bonuses.Add("Max Mana " + FormatSigned(rune.MaxWindsBonus));
		AddMultiplier(bonuses, "Recharge", rune.RechargeMultiplier);
		AddMultiplier(bonuses, "Effectiveness", rune.EffectivenessMultiplier);
		AddMultiplier(bonuses, "Mana cost", rune.WindsCostMultiplier);
		AddMultiplier(bonuses, "Cooldown", rune.CooldownMultiplier);
		return bonuses.Count == 0 ? string.Empty : string.Join(", ", bonuses);
	}

	private static void AddMultiplier(List<string> bonuses, string label, float multiplier)
	{
		float percent = (multiplier - 1f) * 100f;
		if (Math.Abs(percent) > 0.05f) bonuses.Add(label + " " + FormatSigned(percent) + "%");
	}

	private static string FormatSigned(float value)
	{
		return value >= 0f ? "+" + value.ToString("0.#") : value.ToString("0.#");
	}

	public static bool TryClear(Hero hero, int slotIndex)
	{
		HeroExtendedInfo info = hero?.GetExtendedInfo();
		if (info == null || slotIndex < 0 || slotIndex >= WeaponSlotCount)
		{
			return false;
		}
		info.SetWeaponRune(slotIndex, null, null);
		return true;
	}

	public static ItemObject GetItemObject(string itemId)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			return null;
		}
		return MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
	}

	public static bool TryGetForSlot(Agent agent, int slotIndex, out MagicRuneData rune)
	{
		rune = null;
		if (agent?.Equipment == null || slotIndex < 0 || slotIndex >= WeaponSlotCount)
		{
			return false;
		}

		ItemObject targetItem = agent.Equipment[(EquipmentIndex)slotIndex].Item;
		if (targetItem == null)
		{
			return false;
		}

		string runeItemId = null;
		Hero hero = agent.GetHero();
		HeroExtendedInfo info = hero?.GetExtendedInfo();
		if (info != null && string.Equals(info.GetWeaponRuneTargetItemId(slotIndex), targetItem.StringId,
			StringComparison.OrdinalIgnoreCase))
		{
			runeItemId = info.GetWeaponRuneItemId(slotIndex);
		}
		if (string.IsNullOrEmpty(runeItemId))
		{
			runeItemId = MagicRuneLoadoutRegistry.GetRuneItemId(agent.Character?.StringId, slotIndex);
		}

		return MagicRuneRegistry.TryGet(runeItemId, out rune) && MagicRuneRegistry.CanApply(rune, targetItem);
	}

	public static bool TryGetForWeapon(Agent agent, in MissionWeapon weapon, out MagicRuneData rune)
	{
		rune = null;
		string itemId = weapon.Item?.StringId;
		if (string.IsNullOrEmpty(itemId))
		{
			return false;
		}
		for (int i = 0; i < WeaponSlotCount; i++)
		{
			if (string.Equals(agent?.Equipment?[(EquipmentIndex)i].Item?.StringId, itemId,
				StringComparison.OrdinalIgnoreCase) && TryGetForSlot(agent, i, out rune))
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGetForWieldedWeapon(Agent agent, out MagicRuneData rune)
	{
		rune = null;
		if (agent == null || agent.Equipment == null)
		{
			return false;
		}

		EquipmentIndex slot;
		try
		{
			slot = agent.GetPrimaryWieldedItemIndex();
		}
		catch (NullReferenceException)
		{
			return false;
		}
		if (slot >= EquipmentIndex.Weapon0 && slot <= EquipmentIndex.Weapon3 && TryGetForSlot(agent, (int)slot, out rune))
		{
			return true;
		}
		try
		{
			slot = agent.GetOffhandWieldedItemIndex();
		}
		catch (NullReferenceException)
		{
			return false;
		}
		return slot >= EquipmentIndex.Weapon0 && slot <= EquipmentIndex.Weapon3 && TryGetForSlot(agent, (int)slot, out rune);
	}

	public static bool HasEffect(Agent agent, MagicRuneEffect effect, out MagicRuneData rune)
	{
		for (int i = 0; i < WeaponSlotCount; i++)
		{
			if (TryGetForSlot(agent, i, out rune) && rune.Effect == effect)
			{
				return true;
			}
		}
		rune = null;
		return false;
	}
}
