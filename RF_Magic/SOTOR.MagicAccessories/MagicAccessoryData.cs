namespace SOTOR.MagicAccessories;

public enum MagicAccessorySlot
{
	Ring,
	Necklace
}

public sealed class MagicAccessoryData
{
	public string ItemId { get; }

	public MagicAccessorySlot Slot { get; }

	public float MaxWindsBonus { get; }

	public float RechargeMultiplier { get; }

	public float EffectivenessMultiplier { get; }

	public float WindsCostMultiplier { get; }

	public float CooldownMultiplier { get; }

	public MagicAccessoryData(string itemId, MagicAccessorySlot slot, float maxWindsBonus, float rechargeMultiplier,
		float effectivenessMultiplier, float windsCostMultiplier, float cooldownMultiplier)
	{
		ItemId = itemId;
		Slot = slot;
		MaxWindsBonus = maxWindsBonus;
		RechargeMultiplier = rechargeMultiplier;
		EffectivenessMultiplier = effectivenessMultiplier;
		WindsCostMultiplier = windsCostMultiplier;
		CooldownMultiplier = cooldownMultiplier;
	}
}

public readonly struct MagicAccessoryBonuses
{
	public static MagicAccessoryBonuses Neutral => new MagicAccessoryBonuses(0f, 1f, 1f, 1f, 1f);

	public float MaxWindsBonus { get; }

	public float RechargeMultiplier { get; }

	public float EffectivenessMultiplier { get; }

	public float WindsCostMultiplier { get; }

	public float CooldownMultiplier { get; }

	public MagicAccessoryBonuses(float maxWindsBonus, float rechargeMultiplier, float effectivenessMultiplier,
		float windsCostMultiplier, float cooldownMultiplier)
	{
		MaxWindsBonus = maxWindsBonus;
		RechargeMultiplier = rechargeMultiplier;
		EffectivenessMultiplier = effectivenessMultiplier;
		WindsCostMultiplier = windsCostMultiplier;
		CooldownMultiplier = cooldownMultiplier;
	}
}
