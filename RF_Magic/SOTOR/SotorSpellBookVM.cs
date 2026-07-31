using System;
using System.Collections.Generic;
using SOTOR.AbilitySystem;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace SOTOR;

public class SotorSpellBookVM : ViewModel
{
	private readonly Action _closeAction;

	private Hero _hero;

	private List<Hero> _cycleHeroes = new List<Hero>();

	private int _cycleIndex;

	// [RF-C] "Magic Scroll", nao "SpellBook": a tela deixou de ser um livro de duas
	// paginas e passou a ser um pergaminho horizontal.
	private string _titleText = "Magic Scroll";

	private MBBindingList<SotorLoreObjectVM> _loreObjects;

	private MBBindingList<SotorLoreObjectVM> _loreObjectsLeft;

	private MBBindingList<SotorLoreObjectVM> _loreObjectsRight;

	private SotorLoreObjectVM _currentLore;

	private SotorSpellcraftBarVM _spellcraftBar;

	private CharacterViewModel _characterPortrait;

	private readonly Dictionary<string, bool> _stagedSelections = new Dictionary<string, bool>();

	private readonly HashSet<string> _stagedUnlocks = new HashSet<string>();

	private readonly HashSet<string> _stagedPurchases = new HashSet<string>();

	[DataSourceProperty]
	public bool CanCycleHeroes
	{
		get
		{
			if (_cycleHeroes != null)
			{
				return _cycleHeroes.Count > 1;
			}
			return false;
		}
	}

	public int HeroGold => Hero.MainHero?.Gold ?? 0;

	[DataSourceProperty]
	public string GoldText => (HeroGold - StagedSpend()).ToString("N0");

	[DataSourceProperty]
	public SotorSpellcraftBarVM SpellcraftBar
	{
		get
		{
			return _spellcraftBar;
		}
		set
		{
			if (value != _spellcraftBar)
			{
				_spellcraftBar = value;
				OnPropertyChangedWithValue(value, "SpellcraftBar");
			}
		}
	}

	[DataSourceProperty]
	public CharacterViewModel CharacterPortrait
	{
		get
		{
			return _characterPortrait;
		}
		set
		{
			if (value != _characterPortrait)
			{
				_characterPortrait = value;
				OnPropertyChangedWithValue(value, "CharacterPortrait");
			}
		}
	}

	[DataSourceProperty]
	public string HeroName => _hero?.Name?.ToString() ?? "";

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (!(value == _titleText))
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SotorLoreObjectVM> LoreObjects
	{
		get
		{
			return _loreObjects;
		}
		set
		{
			if (value != _loreObjects)
			{
				_loreObjects = value;
				OnPropertyChangedWithValue(value, "LoreObjects");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SotorLoreObjectVM> LoreObjectsLeft
	{
		get
		{
			return _loreObjectsLeft;
		}
		set
		{
			if (value != _loreObjectsLeft)
			{
				_loreObjectsLeft = value;
				OnPropertyChangedWithValue(value, "LoreObjectsLeft");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SotorLoreObjectVM> LoreObjectsRight
	{
		get
		{
			return _loreObjectsRight;
		}
		set
		{
			if (value != _loreObjectsRight)
			{
				_loreObjectsRight = value;
				OnPropertyChangedWithValue(value, "LoreObjectsRight");
			}
		}
	}

	[DataSourceProperty]
	public SotorLoreObjectVM CurrentLore
	{
		get
		{
			return _currentLore;
		}
		set
		{
			if (value != _currentLore)
			{
				_currentLore = value;
				OnPropertyChangedWithValue(value, "CurrentLore");
			}
		}
	}

	public SotorSpellBookVM(Action closeAction)
	{
		_closeAction = closeAction;
		_cycleHeroes = ExtendedInfoManager.GetSpellcasterPartyHeroes();
		_cycleIndex = 0;
		_hero = ((_cycleHeroes.Count > 0) ? _cycleHeroes[0] : Hero.MainHero);
		BuildForHero();
	}

	private void BuildForHero()
	{
		_stagedSelections.Clear();
		_stagedUnlocks.Clear();
		_stagedPurchases.Clear();
		_spellcraftBar = new SotorSpellcraftBarVM(_hero);
		_spellcraftBar.OnStagingChanged = RefreshAllStates;
		_spellcraftBar.StagedOwnedLoreTitlesProvider = GetStagedOwnedLoreTitles;
		_spellcraftBar.StagedOwnedSpellProvider = IsSpellPurchased;
		OnPropertyChanged("SpellcraftBar");
		BuildCharacterPortrait();
		InitializeLoreObjects();
		OnPropertyChanged("CanCycleHeroes");
		OnPropertyChanged("GoldText");
		OnPropertyChanged("HeroName");
	}

	private void SwitchToHeroAt(int index)
	{
		if (_cycleHeroes != null && _cycleHeroes.Count > 1)
		{
			_cycleIndex = (index % _cycleHeroes.Count + _cycleHeroes.Count) % _cycleHeroes.Count;
			_hero = _cycleHeroes[_cycleIndex];
			_spellcraftBar?.RevertChanges();
			BuildForHero();
		}
	}

	public void ExecuteSelectNextHero()
	{
		SwitchToHeroAt(_cycleIndex + 1);
	}

	public void ExecuteSelectPreviousHero()
	{
		SwitchToHeroAt(_cycleIndex - 1);
	}

	private List<string> GetStagedOwnedLoreTitles()
	{
		List<string> list = new List<string>();
		string[] allShownLores = SotorLores.AllShownLores;
		foreach (string text in allShownLores)
		{
			if (IsLoreOwned(text))
			{
				list.Add(SotorLores.Display.TryGetValue(text, out var value) ? value.Title : text);
			}
		}
		return list;
	}

	private int StagedCasterLevel()
	{
		if (_spellcraftBar == null)
		{
			return (int)SotorSpellcraftHelper.GetCastingLevel(_hero);
		}
		return (int)_spellcraftBar.GetStagedCastingLevel();
	}

	public bool IsLoreOwned(string loreId)
	{
		if (loreId == null)
		{
			return false;
		}
		if (_stagedUnlocks.Contains(loreId))
		{
			return true;
		}
		return (_hero?.GetExtendedInfo())?.HasLore(loreId) ?? false;
	}

	public bool IsLoreLocked(string loreId)
	{
		return !IsLoreOwned(loreId);
	}

	public bool IsUnlockStaged(string loreId)
	{
		return _stagedUnlocks.Contains(loreId);
	}

	private bool IsLibrarianActive()
	{
		PerkObject librarian = SotorPerks.Librarian;
		if (librarian == null)
		{
			return false;
		}
		if (_spellcraftBar != null && _spellcraftBar.IsPerkSelectedStaged(librarian))
		{
			return true;
		}
		if (_hero != null)
		{
			return _hero.GetPerkValue(librarian);
		}
		return false;
	}

	public int LorePrice(string loreId)
	{
		// [RF-B] preco base do XML (sotor_spell_prices.xml) quando listado, e desconto
		// se a escola for da propria tradicao do heroi.
		int basePrice = SOTOR.RFIntegration.RFSpellPrices.GetLorePrice(loreId, SotorLores.GetPrice(loreId));
		return SOTOR.RFIntegration.RFCultureLores.AdjustPriceForHero(_hero, loreId, ApplyLibrarian(basePrice));
	}

	private int StagedSpend()
	{
		int num = 0;
		foreach (string stagedUnlock in _stagedUnlocks)
		{
			num += LorePrice(stagedUnlock);
		}
		foreach (string stagedPurchase in _stagedPurchases)
		{
			num += SpellPriceById(stagedPurchase);
		}
		return num;
	}

	private bool CanAfford(int extra)
	{
		return (Hero.MainHero?.Gold ?? 0) >= StagedSpend() + extra;
	}

	public bool CanAffordLore(string loreId)
	{
		return CanAfford(LorePrice(loreId));
	}

	public bool MeetsCasterLevelForLore(string loreId)
	{
		SpellCastingLevel requiredCasterLevel = SotorLores.GetRequiredCasterLevel(loreId);
		if (requiredCasterLevel != SpellCastingLevel.None && StagedCasterLevel() < (int)requiredCasterLevel)
		{
			return false;
		}
		// [RF-B] piso de Arcane da escola, ja ajustado pela cultura do heroi.
		return SOTOR.RFIntegration.RFLoreRequirements.MeetsArcaneForLore(_hero, loreId);
	}

	public string LoreUnlockBlockReason(string loreId)
	{
		SpellCastingLevel requiredCasterLevel = SotorLores.GetRequiredCasterLevel(loreId);
		if (requiredCasterLevel != SpellCastingLevel.None && StagedCasterLevel() < (int)requiredCasterLevel)
		{
			return $"Requires {requiredCasterLevel} casting level";
		}
		// [RF-B] mensagem do piso de Arcane: diz o exigido E o que o heroi tem, para
		// que a penalidade cultural fique visivel em vez de parecer bug.
		if (!SOTOR.RFIntegration.RFLoreRequirements.MeetsArcaneForLore(_hero, loreId))
		{
			int need = SOTOR.RFIntegration.RFLoreRequirements.GetRequiredArcaneForHero(_hero, loreId);
			int have = SOTOR.RFIntegration.RFLoreRequirements.GetArcaneValue(_hero);
			bool foreign = !SOTOR.RFIntegration.RFCultureLores.IsNativeToHero(_hero, loreId);
			return $"Requires Arcane {need} (you have {have})" + (foreign ? " — foreign tradition" : "");
		}
		if (!CanAffordLore(loreId))
		{
			return "Not enough gold";
		}
		return "";
	}

	public void StageUnlockLore(string loreId)
	{
		if (loreId != null && !IsLoreOwned(loreId))
		{
			if (!CanAffordLore(loreId))
			{
				SotorLog.Info($"Spellbook: cannot afford lore '{loreId}' ({LorePrice(loreId)} gold, have {HeroGold}, already staged {StagedSpend()}).");
			}
			else
			{
				_stagedUnlocks.Add(loreId);
				SotorLog.Info($"Spellbook: STAGED unlock of lore '{loreId}' ({LorePrice(loreId)} gold).");
				RefreshAllStates();
			}
		}
	}

	private static AbilityTemplate TemplateById(string abilityId)
	{
		return AbilityFactory.GetTemplate(abilityId);
	}

	public int SpellPriceById(string abilityId)
	{
		return ApplyLibrarian(SotorSpellcraftHelper.GetSpellBaseGoldCost(TemplateById(abilityId)));
	}

	public int SpellPrice(AbilityTemplate t)
	{
		return ApplyLibrarian(SotorSpellcraftHelper.GetSpellBaseGoldCost(t));
	}

	private int ApplyLibrarian(int cost)
	{
		if (!IsLibrarianActive())
		{
			return cost;
		}
		return (int)((float)cost * 0.5f);
	}

	public bool IsSpellPurchased(string abilityId)
	{
		if (abilityId == null)
		{
			return false;
		}
		if (_stagedPurchases.Contains(abilityId))
		{
			return true;
		}
		return (_hero?.GetExtendedInfo())?.HasSpell(abilityId) ?? false;
	}

	public bool IsPurchaseStaged(string abilityId)
	{
		return _stagedPurchases.Contains(abilityId);
	}

	public bool CanBuySpell(string abilityId, string loreId, int spellTier)
	{
		if (IsLoreLocked(loreId) || IsSpellPurchased(abilityId))
		{
			return false;
		}
		return StagedCasterLevel() >= spellTier;
	}

	public bool CanAffordSpell(string abilityId)
	{
		return CanAfford(SpellPriceById(abilityId));
	}

	public string SpellBuyBlockReason(string abilityId, string loreId, int spellTier)
	{
		if (IsLoreLocked(loreId))
		{
			return "Unlock the lore first";
		}
		if (IsSpellPurchased(abilityId))
		{
			return "";
		}
		if (StagedCasterLevel() < spellTier)
		{
			return "Caster level too low";
		}
		if (!CanAffordSpell(abilityId))
		{
			return "Not enough gold";
		}
		return "";
	}

	public void StageBuySpell(string abilityId, string loreId, int spellTier)
	{
		if (abilityId != null && CanBuySpell(abilityId, loreId, spellTier))
		{
			if (!CanAffordSpell(abilityId))
			{
				SotorLog.Info($"Spellbook: cannot afford spell '{abilityId}' ({SpellPriceById(abilityId)} gold, have {HeroGold}, already staged {StagedSpend()}).");
			}
			else
			{
				_stagedPurchases.Add(abilityId);
				SotorLog.Info($"Spellbook: STAGED purchase of spell '{abilityId}' ({SpellPriceById(abilityId)} gold).");
				RefreshAllStates();
			}
		}
	}

	public bool IsSpellSelectedStaged(string abilityId)
	{
		if (_stagedSelections.TryGetValue(abilityId, out var value))
		{
			return value;
		}
		return (_hero?.GetExtendedInfo())?.IsAbilitySelected(abilityId) ?? false;
	}

	public bool IsSpellEquippable(string abilityId, string loreId)
	{
		if (IsLoreOwned(loreId))
		{
			return IsSpellPurchased(abilityId);
		}
		return false;
	}

	public void ToggleSpellStaged(string abilityId, string loreId)
	{
		if (abilityId != null && IsSpellEquippable(abilityId, loreId))
		{
			_stagedSelections[abilityId] = !IsSpellSelectedStaged(abilityId);
			SotorLog.Info($"Spellbook: STAGED spell '{abilityId}' selected={_stagedSelections[abilityId]}.");
			RefreshAllStates();
		}
	}

	private void RefreshAllStates()
	{
		if (_loreObjects != null)
		{
			foreach (SotorLoreObjectVM loreObject in _loreObjects)
			{
				loreObject.RefreshFromState();
				if (loreObject.SpellList == null)
				{
					continue;
				}
				foreach (SotorSpellItemVM spell in loreObject.SpellList)
				{
					spell.RefreshFromState();
				}
			}
		}
		_spellcraftBar?.RefreshValues();
		OnPropertyChanged("GoldText");
	}

	private void CommitBookChanges()
	{
		HeroExtendedInfo heroExtendedInfo = _hero?.GetExtendedInfo();
		if (heroExtendedInfo == null)
		{
			_stagedUnlocks.Clear();
			_stagedPurchases.Clear();
			_stagedSelections.Clear();
			return;
		}
		foreach (string stagedUnlock in _stagedUnlocks)
		{
			int num = LorePrice(stagedUnlock);
			if (num > 0)
			{
				Hero.MainHero.ChangeHeroGold(-num);
			}
			heroExtendedInfo.AddLore(stagedUnlock);
			SotorLog.Info($"Spellbook: COMMITTED unlock lore '{stagedUnlock}' (−{num} gold).");
		}
		foreach (string stagedPurchase in _stagedPurchases)
		{
			int num2 = SpellPriceById(stagedPurchase);
			if (num2 > 0)
			{
				Hero.MainHero.ChangeHeroGold(-num2);
			}
			heroExtendedInfo.AddSpell(stagedPurchase);
			if (!_hero.HasAbility(stagedPurchase))
			{
				_hero.AddAbility(stagedPurchase);
			}
			SotorLog.Info($"Spellbook: COMMITTED purchase spell '{stagedPurchase}' (−{num2} gold; now castable).");
		}
		foreach (KeyValuePair<string, bool> stagedSelection in _stagedSelections)
		{
			bool value = stagedSelection.Value;
			bool flag = heroExtendedInfo.IsAbilitySelected(stagedSelection.Key);
			if (value && !flag)
			{
				heroExtendedInfo.AddSelectedAbility(stagedSelection.Key);
			}
			else if (!value && flag)
			{
				heroExtendedInfo.RemoveSelectedAbility(stagedSelection.Key);
			}
		}
		if (_stagedSelections.Count > 0)
		{
			SotorLog.Info($"Spellbook: COMMITTED {_stagedSelections.Count} spell selection change(s).");
		}
		_stagedUnlocks.Clear();
		_stagedPurchases.Clear();
		_stagedSelections.Clear();
	}

	private void RevertBookChanges()
	{
		bool num = _stagedUnlocks.Count > 0 || _stagedPurchases.Count > 0 || _stagedSelections.Count > 0;
		_stagedUnlocks.Clear();
		_stagedPurchases.Clear();
		_stagedSelections.Clear();
		if (num)
		{
			SotorLog.Info("Spellbook: reverted staged unlocks + purchases + spell selections.");
		}
		RefreshAllStates();
	}

	private void BuildCharacterPortrait()
	{
		try
		{
			if (_hero?.CharacterObject != null)
			{
				CharacterViewModel characterViewModel = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
				characterViewModel.FillFrom(_hero.CharacterObject);
				if (_hero.BattleEquipment != null)
				{
					Equipment equipment = _hero.BattleEquipment.Clone();
					equipment[EquipmentIndex.ArmorItemEndSlot] = default(EquipmentElement);
					equipment[EquipmentIndex.HorseHarness] = default(EquipmentElement);
					characterViewModel.SetEquipment(equipment);
				}
				characterViewModel.MountCreationKey = null;
				CharacterPortrait = characterViewModel;
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SpellBook: BuildCharacterPortrait failed: " + ex.Message);
		}
	}

	public void ExecuteClose()
	{
		_closeAction?.Invoke();
	}

	public void ExecuteDone()
	{
		_spellcraftBar?.CommitChanges();
		CommitBookChanges();
		_closeAction?.Invoke();
	}

	public void ExecuteCancel()
	{
		_spellcraftBar?.RevertChanges();
		RevertBookChanges();
		_closeAction?.Invoke();
	}

	public void ExecuteReset()
	{
		_spellcraftBar?.RevertChanges();
		RevertBookChanges();
	}

	private void InitializeLoreObjects()
	{
		Hero hero = _hero;
		LoreObjects = new MBBindingList<SotorLoreObjectVM>();
		string[] allShownLores = SotorLores.AllShownLores;
		foreach (string text in allShownLores)
		{
			IReadOnlyList<AbilityTemplate> templatesByLore = AbilityFactory.GetTemplatesByLore(text);
			MBBindingList<SotorSpellItemVM> mBBindingList = new MBBindingList<SotorSpellItemVM>();
			foreach (AbilityTemplate item in templatesByLore)
			{
				mBBindingList.Add(new SotorSpellItemVM(this, hero, item, text));
			}
			SotorLores.LoreDisplay value;
			SotorLores.LoreDisplay loreDisplay = (SotorLores.Display.TryGetValue(text, out value) ? value : new SotorLores.LoreDisplay
			{
				LoreId = text,
				Title = text,
				SymbolSprite = "minormagic_symbol"
			});
			LoreObjects.Add(new SotorLoreObjectVM(this, SelectLoreObject, loreDisplay.Title, loreDisplay.SymbolSprite, mBBindingList, text));
			SotorLog.Info($"Spellbook tab built: '{loreDisplay.Title}' ({text}) — {mBBindingList.Count} spell(s), locked={IsLoreLocked(text)}.");
		}
		LoreObjectsLeft = new MBBindingList<SotorLoreObjectVM>();
		LoreObjectsRight = new MBBindingList<SotorLoreObjectVM>();
		foreach (SotorLoreObjectVM loreObject in LoreObjects)
		{
			if (loreObject.IsRightSide)
			{
				LoreObjectsRight.Add(loreObject);
			}
			else
			{
				LoreObjectsLeft.Add(loreObject);
			}
		}
		if (LoreObjects.Count > 0)
		{
			SelectLoreObject(LoreObjects[0]);
		}
		SotorLog.Info($"Spellbook initialized with {LoreObjects.Count} lore tab(s) ({LoreObjectsLeft.Count} left, {LoreObjectsRight.Count} right).");
	}

	private void SelectLoreObject(SotorLoreObjectVM lore)
	{
		foreach (SotorLoreObjectVM loreObject in LoreObjects)
		{
			loreObject.IsSelected = loreObject == lore;
			loreObject.IsVisible = loreObject != lore;
		}
		CurrentLore = lore;
	}
}
