using SOTOR.AbilitySystem;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

using TaleWorlds.Localization;

namespace SOTOR;

public class SotorSpellItemVM : ViewModel
{
	private readonly SotorSpellBookVM _book;

	private readonly Hero _hero;

	private readonly string _abilityId;

	private readonly string _loreId;

	private readonly int _spellTier;

	private readonly string _description;

	private string _abilitySpriteName;

	private string _name;

	private bool _isKnown = true;

	private bool _isDisabled;

	private bool _isSelected;

	private bool _canLearn;

	private bool _isPurchased;

	private bool _isBuyable;

	private bool _showBuyOverlay;

	private string _buyText;

	private MBBindingList<SotorStatItemVM> _statItems;

	private BasicTooltipViewModel _abilityHint;

	[DataSourceProperty]
	public string AbilitySpriteName
	{
		get
		{
			return _abilitySpriteName;
		}
		set
		{
			if (!(value == _abilitySpriteName))
			{
				_abilitySpriteName = value;
				OnPropertyChangedWithValue(value, "AbilitySpriteName");
			}
		}
	}

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
	public MBBindingList<SotorStatItemVM> AbilityStatItems
	{
		get
		{
			return _statItems;
		}
		set
		{
			if (value != _statItems)
			{
				_statItems = value;
				OnPropertyChangedWithValue(value, "AbilityStatItems");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel AbilityHint
	{
		get
		{
			return _abilityHint;
		}
		set
		{
			if (value != _abilityHint)
			{
				_abilityHint = value;
				OnPropertyChangedWithValue(value, "AbilityHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsKnown
	{
		get
		{
			return _isKnown;
		}
		set
		{
			if (value != _isKnown)
			{
				_isKnown = value;
				OnPropertyChangedWithValue(value, "IsKnown");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
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
	public bool CanLearn
	{
		get
		{
			return _canLearn;
		}
		set
		{
			if (value != _canLearn)
			{
				_canLearn = value;
				OnPropertyChangedWithValue(value, "CanLearn");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPurchased
	{
		get
		{
			return _isPurchased;
		}
		set
		{
			if (value != _isPurchased)
			{
				_isPurchased = value;
				OnPropertyChangedWithValue(value, "IsPurchased");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBuyable
	{
		get
		{
			return _isBuyable;
		}
		set
		{
			if (value != _isBuyable)
			{
				_isBuyable = value;
				OnPropertyChangedWithValue(value, "IsBuyable");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowBuyOverlay
	{
		get
		{
			return _showBuyOverlay;
		}
		set
		{
			if (value != _showBuyOverlay)
			{
				_showBuyOverlay = value;
				OnPropertyChangedWithValue(value, "ShowBuyOverlay");
			}
		}
	}

	[DataSourceProperty]
	public string BuyText
	{
		get
		{
			return _buyText;
		}
		set
		{
			if (value != _buyText)
			{
				_buyText = value;
				OnPropertyChangedWithValue(value, "BuyText");
			}
		}
	}

	public SotorSpellItemVM(SotorSpellBookVM book, Hero hero, AbilityTemplate template, string loreId)
	{
		_book = book;
		_hero = hero;
		_abilityId = template.StringID;
		_loreId = loreId;
		_spellTier = template.SpellTier;
		_name = template.Name;
		_description = template.TooltipDescription;
		_abilitySpriteName = template.SpriteName;
		_statItems = new MBBindingList<SotorStatItemVM>
		{
			new SotorStatItemVM("Cooldown:", template.CoolDown + " seconds"),
			new SotorStatItemVM("Spell Type:", template.AbilityEffectType.ToString()),
			new SotorStatItemVM("Spell Tier:", ((SpellCastingLevel)template.SpellTier/*cast due to constrained. prefix*/).ToString()),
			new SotorStatItemVM(new TextObject("{=rf_mana_cost}Mana cost:").ToString(), template.WindsOfMagicCost.ToString())
			// [RF-C] removida a linha "Spell Name:" — no layout do RF o nome e o
			// TITULO do card, entao esta era uma repeticao. Com ela o card tinha 6
			// linhas e estourava a celula de 78px da grade, invadindo o card de
			// baixo (foi o texto sobreposto reportado in-game).
		};
		_abilityHint = new BasicTooltipViewModel(() => _description);
		RefreshFromState();
	}

	public void ExecuteSelectAbility()
	{
		if (_book != null)
		{
			if (!_book.IsSpellPurchased(_abilityId))
			{
				_book.StageBuySpell(_abilityId, _loreId, _spellTier);
			}
			else
			{
				_book.ToggleSpellStaged(_abilityId, _loreId);
			}
		}
	}

	public void ExecuteBuySpell()
	{
		_book?.StageBuySpell(_abilityId, _loreId, _spellTier);
	}

	public void RefreshFromState()
	{
		if (_book == null)
		{
			IsSelected = (_hero?.GetExtendedInfo())?.IsAbilitySelected(_abilityId) ?? false;
			IsKnown = _hero == null || _hero.HasAbility(_abilityId);
			IsPurchased = IsKnown;
			IsBuyable = false;
			ShowBuyOverlay = false;
			return;
		}
		IsKnown = _book.IsLoreOwned(_loreId);
		IsPurchased = _book.IsSpellPurchased(_abilityId);
		IsSelected = _book.IsSpellSelectedStaged(_abilityId);
		bool flag = _book.CanBuySpell(_abilityId, _loreId, _spellTier);
		IsBuyable = flag && _book.CanAffordSpell(_abilityId);
		ShowBuyOverlay = IsKnown && !IsPurchased;
		if (!IsPurchased && _book.IsLoreOwned(_loreId))
		{
			string text = _book.SpellBuyBlockReason(_abilityId, _loreId, _spellTier);
			BuyText = (string.IsNullOrEmpty(text) ? $"Learn\n{_book.SpellPriceById(_abilityId):N0} Gold" : text);
		}
		else
		{
			BuyText = "";
		}
	}
}
