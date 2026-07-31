using System;
using SOTOR.AbilitySystem;
using TaleWorlds.Library;

namespace SOTOR;

public class SotorLoreObjectVM : ViewModel
{
	private readonly SotorSpellBookVM _parent;

	private readonly Action<SotorLoreObjectVM> _onSelected;

	private readonly string _loreId;

	private string _name;

	private string _spriteName;

	private bool _isSelected;

	private bool _isVisible = true;

	private bool _isLocked;

	private bool _isUnlockable = true;

	private string _unlockText;

	private bool _isRightSide;

	private MBBindingList<SotorSpellItemVM> _spellList;

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (!(value == _name))
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string SpriteName
	{
		get
		{
			return _spriteName;
		}
		set
		{
			if (!(value == _spriteName))
			{
				_spriteName = value;
				OnPropertyChangedWithValue(value, "SpriteName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRightSide
	{
		get
		{
			return _isRightSide;
		}
		set
		{
			if (value != _isRightSide)
			{
				_isRightSide = value;
				OnPropertyChangedWithValue(value, "IsRightSide");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUnlockable
	{
		get
		{
			return _isUnlockable;
		}
		set
		{
			if (value != _isUnlockable)
			{
				_isUnlockable = value;
				OnPropertyChangedWithValue(value, "IsUnlockable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				OnPropertyChangedWithValue(value, "IsVisible");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SotorSpellItemVM> SpellList
	{
		get
		{
			return _spellList;
		}
		set
		{
			if (value != _spellList)
			{
				_spellList = value;
				OnPropertyChangedWithValue(value, "SpellList");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLocked
	{
		get
		{
			return _isLocked;
		}
		set
		{
			if (value != _isLocked)
			{
				_isLocked = value;
				OnPropertyChangedWithValue(value, "IsLocked");
			}
		}
	}

	[DataSourceProperty]
	public string UnlockText
	{
		get
		{
			return _unlockText;
		}
		set
		{
			if (value != _unlockText)
			{
				_unlockText = value;
				OnPropertyChangedWithValue(value, "UnlockText");
			}
		}
	}

	public SotorLoreObjectVM(SotorSpellBookVM parent, Action<SotorLoreObjectVM> onSelected, string name, string spriteName, MBBindingList<SotorSpellItemVM> spellList, string loreId)
	{
		_parent = parent;
		_onSelected = onSelected;
		_name = name;
		_loreId = loreId;
		_isRightSide = SotorLores.IsRightSideLore(loreId);
		_spriteName = (_isRightSide ? (spriteName + "_r") : spriteName);
		_spellList = spellList;
		RefreshFromState();
	}

	public void ExecuteUnlockLore()
	{
		if (_parent != null && _loreId != null && _parent.IsLoreLocked(_loreId))
		{
			if (!_parent.MeetsCasterLevelForLore(_loreId))
			{
				SotorLog.Info("Spellbook: unlock '" + _loreId + "' blocked — caster level too low.");
			}
			else if (!_parent.CanAffordLore(_loreId))
			{
				SotorLog.Info($"Spellbook: unlock '{_loreId}' blocked — not enough gold (need {_parent.LorePrice(_loreId)}, have {_parent.HeroGold}).");
			}
			else
			{
				_parent.StageUnlockLore(_loreId);
			}
		}
	}

	public void RefreshFromState()
	{
		if (_parent == null || _loreId == null)
		{
			IsLocked = false;
			return;
		}
		IsLocked = _parent.IsLoreLocked(_loreId);
		if (!(IsUnlockable = _parent.MeetsCasterLevelForLore(_loreId)))
		{
			UnlockText = _name + "\n" + _parent.LoreUnlockBlockReason(_loreId);
			return;
		}
		int num = _parent.LorePrice(_loreId);
		UnlockText = $"Unlock {_name}\n{num:N0} Gold";
	}

	public void ExecuteSelectLoreObject()
	{
		_onSelected?.Invoke(this);
	}
}
