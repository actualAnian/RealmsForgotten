using System;
using Bannerlord.UIExtenderEx.Attributes;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SOTOR.MagicAccessories;

public sealed class MagicRuneSocketVM : ViewModel
{
	private readonly int _slotIndex;
	private readonly Action<int> _onClick;
	private readonly Action<int> _onHoverBegin;
	private readonly Action<int> _onHoverEnd;
	private readonly Action<int> _onDiscard;
	private readonly Action<int> _onUnequip;
	private ItemImageIdentifierVM _image;
	private bool _occupied;
	private bool _isSelected;
	private HintViewModel _sellHint;
	private HintViewModel _unequipHint;
	private HintViewModel _runeHint;
	private uint _tierColor = InventoryRuneTierTint.White;
	private ItemObject _targetItem;
	private ItemObject _runeItem;
	private bool _targetMatches;

	[DataSourceProperty]
	public ItemImageIdentifierVM Image
	{
		get => _image;
		private set
		{
			if (value != _image)
			{
				_image = value;
				OnPropertyChanged(nameof(Image));
			}
		}
	}

	[DataSourceProperty]
	public bool Occupied
	{
		get => _occupied;
		private set
		{
			if (value != _occupied)
			{
				_occupied = value;
				OnPropertyChangedWithValue(value, nameof(Occupied));
				OnPropertyChangedWithValue(!value, nameof(Empty));
				OnPropertyChangedWithValue(Occupied && IsSelected, nameof(ShowControls));
			}
		}
	}

	[DataSourceProperty]
	public bool Empty => !Occupied;

	[DataSourceProperty]
	public bool IsSelected
	{
		get => _isSelected;
		private set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, nameof(IsSelected));
				OnPropertyChangedWithValue(Occupied && value, nameof(ShowControls));
			}
		}
	}

	[DataSourceProperty]
	public bool ShowControls => Occupied && IsSelected;

	[DataSourceProperty]
	public HintViewModel SellHint
	{
		get => _sellHint;
		private set
		{
			if (value != _sellHint)
			{
				_sellHint = value;
				OnPropertyChanged(nameof(SellHint));
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel UnequipHint
	{
		get => _unequipHint;
		private set
		{
			if (value != _unequipHint)
			{
				_unequipHint = value;
				OnPropertyChanged(nameof(UnequipHint));
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RuneHint
	{
		get => _runeHint;
		private set
		{
			if (value != _runeHint)
			{
				_runeHint = value;
				OnPropertyChanged(nameof(RuneHint));
			}
		}
	}

	[DataSourceProperty]
	public uint TierColor
	{
		get => _tierColor;
		private set
		{
			if (value != _tierColor)
			{
				_tierColor = value;
				OnPropertyChangedWithValue(value, nameof(TierColor));
			}
		}
	}

	public MagicRuneSocketVM(int slotIndex, Action<int> onClick, Action<int> onHoverBegin,
		Action<int> onHoverEnd, Action<int> onDiscard, Action<int> onUnequip)
	{
		_slotIndex = slotIndex;
		_onClick = onClick;
		_onHoverBegin = onHoverBegin;
		_onHoverEnd = onHoverEnd;
		_onDiscard = onDiscard;
		_onUnequip = onUnequip;
		SellHint = new HintViewModel(new TextObject("Discard this rune"));
		UnequipHint = new HintViewModel(new TextObject("Return this rune to inventory"));
		RuneHint = new HintViewModel(new TextObject("Empty rune socket"));
	}

	[DataSourceMethod]
	public void ExecuteClick()
	{
		_onClick?.Invoke(_slotIndex);
	}

	[DataSourceMethod]
	public void ExecuteHoverBegin()
	{
		_onHoverBegin?.Invoke(_slotIndex);
	}

	[DataSourceMethod]
	public void ExecuteHoverEnd()
	{
		_onHoverEnd?.Invoke(_slotIndex);
	}

	[DataSourceMethod]
	public void ExecuteSellSingle()
	{
		_onDiscard?.Invoke(_slotIndex);
	}

	[DataSourceMethod]
	public void ExecuteUnequipItem()
	{
		_onUnequip?.Invoke(_slotIndex);
	}

	public void Refresh(ItemObject targetItem, ItemObject runeItem, bool targetMatches)
	{
		Image = new ItemImageIdentifierVM(runeItem, string.Empty);
		Occupied = runeItem != null;
		TierColor = runeItem != null && MagicRuneRegistry.TryGet(runeItem.StringId, out MagicRuneData tierRune)
			? InventoryRuneTierTint.ColorFor(tierRune.Tier)
			: InventoryRuneTierTint.White;
		_targetItem = targetItem;
		_runeItem = runeItem;
		_targetMatches = targetMatches;
		RuneHint = new HintViewModel(new TextObject(BuildRuneHintText(targetItem, runeItem, targetMatches)));
		if (runeItem == null)
		{
			SetSelected(false);
		}
	}

	public void SetSelected(bool isSelected)
	{
		IsSelected = isSelected;
	}

	private static string BuildRuneHintText(ItemObject targetItem, ItemObject runeItem, bool targetMatches)
	{
		if (runeItem == null)
		{
			return targetItem == null
				? "Empty rune socket\nEquip a weapon or shield in this slot first."
				: "Empty rune socket\nSelect a compatible rune in your inventory, then click this socket.";
		}

		if (!MagicRuneRegistry.TryGet(runeItem.StringId, out MagicRuneData rune))
		{
			return runeItem.Name.ToString();
		}

		string text = "Socketed Rune: " + rune.Name
			+ "\nRune tier: " + rune.Tier
			+ "\nCompatible with: " + FormatTargets(rune.Targets)
			+ "\nEffect: " + (string.IsNullOrEmpty(rune.Description) ? "None" : rune.Description);
		string bonuses = BuildBonusText(rune);
		if (!string.IsNullOrEmpty(bonuses))
		{
			text += "\nMagic bonuses: " + bonuses;
		}
		if (targetItem != null)
		{
			text += "\nSocketed on: " + targetItem.Name;
		}
		if (!targetMatches)
		{
			text += "\nInactive: the item in this slot changed.";
		}
		return text;
	}

	private static string FormatTargets(MagicRuneTarget targets)
	{
		string text = string.Empty;
		AddTarget(ref text, targets, MagicRuneTarget.Melee, "Melee");
		AddTarget(ref text, targets, MagicRuneTarget.Shield, "Shield");
		AddTarget(ref text, targets, MagicRuneTarget.Bow, "Bow");
		AddTarget(ref text, targets, MagicRuneTarget.Thrown, "Thrown");
		AddTarget(ref text, targets, MagicRuneTarget.Ammunition, "Ammunition");
		return string.IsNullOrEmpty(text) ? "None" : text;
	}

	private static void AddTarget(ref string text, MagicRuneTarget targets, MagicRuneTarget target, string label)
	{
		if ((targets & target) == 0)
		{
			return;
		}
		text = string.IsNullOrEmpty(text) ? label : text + ", " + label;
	}

	private static string BuildBonusText(MagicRuneData rune)
	{
		string text = string.Empty;
		if (Math.Abs(rune.MaxWindsBonus) > 0.001f)
		{
			AddBonus(ref text, "Max Winds " + FormatSigned(rune.MaxWindsBonus));
		}
		AddMultiplier(ref text, "Recharge", rune.RechargeMultiplier);
		AddMultiplier(ref text, "Effectiveness", rune.EffectivenessMultiplier);
		AddMultiplier(ref text, "Winds cost", rune.WindsCostMultiplier);
		AddMultiplier(ref text, "Cooldown", rune.CooldownMultiplier);
		return text;
	}

	private static void AddMultiplier(ref string text, string label, float multiplier)
	{
		float percent = (multiplier - 1f) * 100f;
		if (Math.Abs(percent) > 0.05f)
		{
			AddBonus(ref text, label + " " + FormatSigned(percent) + "%");
		}
	}

	private static void AddBonus(ref string text, string bonus)
	{
		text = string.IsNullOrEmpty(text) ? bonus : text + ", " + bonus;
	}

	private static string FormatSigned(float value)
	{
		return value >= 0f ? "+" + value.ToString("0.#") : value.ToString("0.#");
	}
}
