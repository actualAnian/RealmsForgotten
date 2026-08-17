using System;

namespace SOTOR.MagicAccessories;

[Flags]
public enum MagicRuneTarget
{
	None = 0,
	Melee = 1,
	Shield = 2,
	Bow = 4,
	Thrown = 8,
	Ammunition = 16,
	MagicFocus = 32
}

public enum MagicRuneTier
{
	Lesser,
	Greater,
	Ancient
}

public enum MagicRuneEffect
{
	None,
	Sundering,
	Impact,
	KeenEdge,
	Piercing,
	Executioner,
	Vampiric,
	Precision,
	Wind,
	Windlass,
	FarSight,
	Huntsman,
	Returning,
	Flame,
	Frost,
	Storm,
	Explosive,
	Bulwark,
	Reprisal,
	Mirror,
	Lightness,
	Arcane,
	Focus,
	Reservoir,
	Echo
}

public sealed class MagicRuneData
{
	public string ItemId { get; }

	public MagicRuneTarget Targets { get; }

	public string Name { get; }

	public string Description { get; }

	public MagicRuneTier Tier { get; }

	public MagicRuneEffect Effect { get; }

	public float PrimaryValue { get; }

	public float SecondaryValue { get; }

	public float MaxWindsBonus { get; }

	public float RechargeMultiplier { get; }

	public float EffectivenessMultiplier { get; }

	public float WindsCostMultiplier { get; }

	public float CooldownMultiplier { get; }

	public MagicRuneData(string itemId, MagicRuneTarget targets, string name, string description,
		MagicRuneTier tier, MagicRuneEffect effect, float primaryValue, float secondaryValue,
		float maxWindsBonus, float rechargeMultiplier, float effectivenessMultiplier,
		float windsCostMultiplier, float cooldownMultiplier)
	{
		ItemId = itemId;
		Targets = targets;
		Name = name ?? itemId;
		Description = description ?? string.Empty;
		Tier = tier;
		Effect = effect;
		PrimaryValue = primaryValue;
		SecondaryValue = secondaryValue;
		MaxWindsBonus = maxWindsBonus;
		RechargeMultiplier = rechargeMultiplier;
		EffectivenessMultiplier = effectivenessMultiplier;
		WindsCostMultiplier = windsCostMultiplier;
		CooldownMultiplier = cooldownMultiplier;
	}
}
