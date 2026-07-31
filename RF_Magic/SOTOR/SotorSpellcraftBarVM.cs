using System;
using System.Collections.Generic;
using System.Linq;
using SOTOR.AbilitySystem;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

using TaleWorlds.Localization;

namespace SOTOR;

public class SotorSpellcraftBarVM : ViewModel
{
	private readonly Hero _hero;

	private string _skillName = "Spellcraft";

	private int _skillLevel;

	private int _focus;

	private int _maxFocus = 5;

	private int _unspentFocus;

	private string _attributeName = "Intelligence";

	private int _attributeValue;

	private int _unspentAttribute;

	private string _castingLevelText = "";

	private int _skillXpProgress;

	private int _skillXpForNext;

	private MBBindingList<PerkVM> _perks;

	private int _fullLearningRateLevel;

	private float _learningRate;

	private bool _canLearnSkill = true;

	private MBBindingList<SotorStatItemVM> _derivedStats;

	private PerkSelectionVM _perkSelection;

	private int _pendingFocus;

	public Action OnStagingChanged;

	public Func<List<string>> StagedOwnedLoreTitlesProvider;

	public Func<string, bool> StagedOwnedSpellProvider;

	private static readonly HashSet<string> _diagnosed = new HashSet<string>();

	[DataSourceProperty]
	public PerkSelectionVM PerkSelection
	{
		get
		{
			return _perkSelection;
		}
		set
		{
			if (value != _perkSelection)
			{
				_perkSelection = value;
				OnPropertyChangedWithValue(value, "PerkSelection");
			}
		}
	}

	public bool HasPendingChanges
	{
		get
		{
			if (_perkSelection == null || !_perkSelection.IsAnyPerkSelected())
			{
				return _pendingFocus > 0;
			}
			return true;
		}
	}

	[DataSourceProperty]
	public MBBindingList<SotorStatItemVM> DerivedStats
	{
		get
		{
			return _derivedStats;
		}
		set
		{
			if (value != _derivedStats)
			{
				_derivedStats = value;
				OnPropertyChangedWithValue(value, "DerivedStats");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PerkVM> Perks
	{
		get
		{
			return _perks;
		}
		set
		{
			if (value != _perks)
			{
				_perks = value;
				OnPropertyChangedWithValue(value, "Perks");
			}
		}
	}

	[DataSourceProperty]
	public float LearningRate
	{
		get
		{
			return _learningRate;
		}
		set
		{
			if (value != _learningRate)
			{
				_learningRate = value;
				OnPropertyChangedWithValue(value, "LearningRate");
			}
		}
	}

	[DataSourceProperty]
	public bool CanLearnSkill
	{
		get
		{
			return _canLearnSkill;
		}
		set
		{
			if (value != _canLearnSkill)
			{
				_canLearnSkill = value;
				OnPropertyChangedWithValue(value, "CanLearnSkill");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentFocusLevel => _focus;

	[DataSourceProperty]
	public string CurrentLearningRateText => "Learning Rate: x " + _learningRate.ToString("0.00");

	[DataSourceProperty]
	public string FocusPointsText => "Focus Points";

	[DataSourceProperty]
	public int CurrentSkillXP => _skillXpProgress;

	[DataSourceProperty]
	public int XpRequiredForNextLevel
	{
		get
		{
			if (_skillXpForNext <= 0)
			{
				return 1;
			}
			return _skillXpForNext;
		}
	}

	[DataSourceProperty]
	public string ProgressText => _skillXpProgress + " / " + XpRequiredForNextLevel + " XP";

	[DataSourceProperty]
	public int FullLearningRateLevel
	{
		get
		{
			return _fullLearningRateLevel;
		}
		set
		{
			if (value != _fullLearningRateLevel)
			{
				_fullLearningRateLevel = value;
				OnPropertyChangedWithValue(value, "FullLearningRateLevel");
			}
		}
	}

	[DataSourceProperty]
	public string SkillName
	{
		get
		{
			return _skillName;
		}
		set
		{
			if (value != _skillName)
			{
				_skillName = value;
				OnPropertyChangedWithValue(value, "SkillName");
			}
		}
	}

	[DataSourceProperty]
	public int SkillLevel
	{
		get
		{
			return _skillLevel;
		}
		set
		{
			if (value != _skillLevel)
			{
				_skillLevel = value;
				OnPropertyChangedWithValue(value, "SkillLevel");
				OnPropertyChanged("SkillLevelText");
			}
		}
	}

	[DataSourceProperty]
	public string SkillLevelText => _skillLevel.ToString();

	[DataSourceProperty]
	public int Focus
	{
		get
		{
			return _focus;
		}
		set
		{
			if (value != _focus)
			{
				_focus = value;
				OnPropertyChangedWithValue(value, "Focus");
			}
		}
	}

	[DataSourceProperty]
	public int MaxFocus
	{
		get
		{
			return _maxFocus;
		}
		set
		{
			if (value != _maxFocus)
			{
				_maxFocus = value;
				OnPropertyChangedWithValue(value, "MaxFocus");
			}
		}
	}

	[DataSourceProperty]
	public int UnspentFocus
	{
		get
		{
			return _unspentFocus;
		}
		set
		{
			if (value != _unspentFocus)
			{
				_unspentFocus = value;
				OnPropertyChangedWithValue(value, "UnspentFocus");
				OnPropertyChanged("CanAddFocus");
			}
		}
	}

	[DataSourceProperty]
	public bool CanAddFocus
	{
		get
		{
			if (_unspentFocus > 0)
			{
				return _focus < _maxFocus;
			}
			return false;
		}
	}

	[DataSourceProperty]
	public string AttributeName
	{
		get
		{
			return _attributeName;
		}
		set
		{
			if (value != _attributeName)
			{
				_attributeName = value;
				OnPropertyChangedWithValue(value, "AttributeName");
			}
		}
	}

	[DataSourceProperty]
	public int AttributeValue
	{
		get
		{
			return _attributeValue;
		}
		set
		{
			if (value != _attributeValue)
			{
				_attributeValue = value;
				OnPropertyChangedWithValue(value, "AttributeValue");
				OnPropertyChanged("AttributeValueText");
			}
		}
	}

	[DataSourceProperty]
	public string AttributeValueText => _attributeValue.ToString();

	[DataSourceProperty]
	public int UnspentAttribute
	{
		get
		{
			return _unspentAttribute;
		}
		set
		{
			if (value != _unspentAttribute)
			{
				_unspentAttribute = value;
				OnPropertyChangedWithValue(value, "UnspentAttribute");
				OnPropertyChanged("CanAddAttribute");
			}
		}
	}

	[DataSourceProperty]
	public bool CanAddAttribute => _unspentAttribute > 0;

	[DataSourceProperty]
	public string CastingLevelText
	{
		get
		{
			return _castingLevelText;
		}
		set
		{
			if (value != _castingLevelText)
			{
				_castingLevelText = value;
				OnPropertyChangedWithValue(value, "CastingLevelText");
			}
		}
	}

	[DataSourceProperty]
	public int SkillXpProgress
	{
		get
		{
			return _skillXpProgress;
		}
		set
		{
			if (value != _skillXpProgress)
			{
				_skillXpProgress = value;
				OnPropertyChangedWithValue(value, "SkillXpProgress");
			}
		}
	}

	[DataSourceProperty]
	public int SkillXpForNext
	{
		get
		{
			return _skillXpForNext;
		}
		set
		{
			if (value != _skillXpForNext)
			{
				_skillXpForNext = value;
				OnPropertyChangedWithValue(value, "SkillXpForNext");
			}
		}
	}

	public SotorSpellcraftBarVM(Hero hero)
	{
		_hero = hero;
		_derivedStats = new MBBindingList<SotorStatItemVM>();
		if (_hero?.HeroDeveloper != null)
		{
			_perkSelection = new PerkSelectionVM(_hero.HeroDeveloper, delegate
			{
				OnPerkStagingChanged();
			}, OnPerkStagingChanged);
		}
		BuildPerks();
		RefreshValues();
	}

	private void OnPerkStagingChanged()
	{
		RefreshValues();
		OnStagingChanged?.Invoke();
	}

	public void CommitChanges()
	{
		HeroDeveloper heroDeveloper = _hero?.HeroDeveloper;
		SkillObject spellcraft = SotorSkills.Spellcraft;
		if (heroDeveloper == null || spellcraft == null)
		{
			return;
		}
		if (_pendingFocus > 0)
		{
			int pendingFocus = _pendingFocus;
			_pendingFocus = 0;
			for (int i = 0; i < pendingFocus; i++)
			{
				if (heroDeveloper.UnspentFocusPoints <= 0)
				{
					break;
				}
				if (!heroDeveloper.CanAddFocusToSkill(spellcraft))
				{
					break;
				}
				heroDeveloper.AddFocus(spellcraft, 1);
			}
		}
		_perkSelection?.ApplySelectedPerks();
		SotorLog.Info($"Spellbook: committed staged perks + focus (Spellcraft {_hero.GetSkillValue(spellcraft)}).");
		RefreshValues();
	}

	public void RevertChanges()
	{
		_perkSelection?.ResetSelectedPerks();
		if (_perkSelection != null)
		{
			_perkSelection.IsActive = false;
		}
		_pendingFocus = 0;
		RefreshValues();
	}

	public SpellCastingLevel GetStagedCastingLevel()
	{
		if (_hero == null)
		{
			return SpellCastingLevel.None;
		}
		if (SotorPerks.Instance != null)
		{
			if (SotorPerks.Archmage != null && IsPerkSelected(SotorPerks.Archmage))
			{
				return SpellCastingLevel.Archmage;
			}
			if (SotorPerks.MasterSpells != null && IsPerkSelected(SotorPerks.MasterSpells))
			{
				return SpellCastingLevel.Master;
			}
			if (SotorPerks.AdeptSpells != null && IsPerkSelected(SotorPerks.AdeptSpells))
			{
				return SpellCastingLevel.Adept;
			}
			if (SotorPerks.EntrySpells != null && IsPerkSelected(SotorPerks.EntrySpells))
			{
				return SpellCastingLevel.Entry;
			}
			return SpellCastingLevel.Minor;
		}
		return SotorSpellcraftHelper.GetCastingLevel(_hero);
	}

	private string GetStagedCastingLevelText()
	{
		return GetStagedCastingLevel().ToString();
	}

	private float GetStagedCasterPerkDamageFactor()
	{
		float num = 1f;
		if (SotorPerks.OverCaster != null && IsPerkSelected(SotorPerks.OverCaster))
		{
			num += 0.2f;
		}
		if (SotorPerks.EfficientSpellCaster != null && IsPerkSelected(SotorPerks.EfficientSpellCaster))
		{
			num -= 0.2f;
		}
		if (SotorPerks.Dampener != null && IsPerkSelected(SotorPerks.Dampener))
		{
			num -= 0.15f;
		}
		return num;
	}

	private void RefreshDerivedStats()
	{
		if (_derivedStats == null || _hero == null)
		{
			return;
		}
		_derivedStats.Clear();
		List<SotorStatItemVM> list = new List<SotorStatItemVM>();
		list.Add(new SotorStatItemVM("Spell Casting Level:", GetStagedCastingLevelText()));
		float windsOfMagic = _hero.GetWindsOfMagic();
		float maxWindsOfMagic = _hero.GetMaxWindsOfMagic();
		float windsRechargePerHour = ExtendedInfoManager.GetWindsRechargePerHour(_hero);
		list.Add(new SotorStatItemVM(new TextObject("{=rf_mana_current}Current Mana:").ToString(), ((int)Math.Round(windsOfMagic)).ToString(), "winds_icon_45"));
		list.Add(new SotorStatItemVM(new TextObject("{=rf_mana_max}Maximum Mana:").ToString(), ((int)Math.Round(maxWindsOfMagic)).ToString(), "winds_icon_45"));
		list.Add(new SotorStatItemVM(new TextObject("{=rf_mana_regen}Mana Recharge:").ToString(), windsRechargePerHour.ToString("0.00") + " / hour", "winds_icon_45"));
		int num = (int)Math.Round((SotorSpellcraftHelper.GetSpellDamageFactor(_hero) * GetStagedCasterPerkDamageFactor() - 1f) * 100f);
		list.Add(new SotorStatItemVM("Spell Effectiveness:", ((num >= 0) ? "+" : "") + num + "%"));
		int num2 = (int)Math.Round((SotorSpellcraftHelper.GetSpellDurationFactor(_hero) - 1f) * 100f);
		list.Add(new SotorStatItemVM("Spell Duration:", ((num2 >= 0) ? "+" : "") + num2 + "%"));
		AddSpellModifierPerkRows(list, "winds_icon_45");
		AddSelfAllyPerkRows(list, "winds_icon_45");
		List<string> list2 = StagedOwnedLoreTitlesProvider?.Invoke();
		if (list2 == null)
		{
			list2 = (_hero.GetExtendedInfo()?.AcquiredLores ?? new List<string>()).Select((string id) => (!SotorLores.Display.TryGetValue(id, out var value2)) ? id : value2.Title).ToList();
		}
		list.Add(new SotorStatItemVM("Known Magic Lores:", (list2.Count > 0) ? string.Join(", ", list2) : "None"));
		bool num3;
		if (StagedOwnedSpellProvider == null)
		{
			Hero hero = _hero;
			if (hero == null)
			{
				goto IL_02b7;
			}
			num3 = hero.GetExtendedInfo()?.HasSpell("MindControl") == true;
		}
		else
		{
			num3 = StagedOwnedSpellProvider("MindControl");
		}
		if (num3)
		{
			list.Add(new SotorStatItemVM("Base Mind Control Chance:", (int)Math.Round(SotorMindControlHelper.GetBaseChance(_hero) * 100f) + "%"));
		}
		goto IL_02b7;
		IL_02b7:
		SotorLores.LoreDisplay value;
		string item = (SotorLores.Display.TryGetValue("LoreOfNecromancy", out value) ? value.Title : "Lore of Necromancy");
		if (list2.Contains(item))
		{
			int num4 = (int)(GetStagedCastingLevel() - 2);
			int num5 = ((num4 > 0) ? (num4 * 20) : 0);
			list.Add(new SotorStatItemVM("Skeleton Troop Weight:", (num5 > 0) ? ("-" + num5 + "%") : "0%"));
		}
		for (int num6 = list.Count - 1; num6 >= 0; num6--)
		{
			_derivedStats.Add(list[num6]);
		}
		OnPropertyChanged("DerivedStats");
	}

	private void AddSpellModifierPerkRows(List<SotorStatItemVM> rows, string windsIcon)
	{
		if (SotorPerks.Instance != null)
		{
			if (SotorPerks.OverCaster != null && IsPerkSelected(SotorPerks.OverCaster))
			{
				rows.Add(new SotorStatItemVM("Spell Winds Cost:", "+30%", windsIcon));
			}
			else if (SotorPerks.EfficientSpellCaster != null && IsPerkSelected(SotorPerks.EfficientSpellCaster))
			{
				rows.Add(new SotorStatItemVM("Spell Winds Cost:", "-30%", windsIcon));
			}
			if (SotorPerks.Dampener != null && IsPerkSelected(SotorPerks.Dampener))
			{
				rows.Add(new SotorStatItemVM("Ward Save:", "5%"));
			}
		}
	}

	private void AddSelfAllyPerkRows(List<SotorStatItemVM> rows, string windsIcon)
	{
		if (SotorPerks.Instance != null)
		{
			if (SotorPerks.Selfish != null && IsPerkSelected(SotorPerks.Selfish))
			{
				rows.Add(new SotorStatItemVM("Self Spell Damage:", "-90%"));
			}
			if (SotorPerks.WellControlled != null && IsPerkSelected(SotorPerks.WellControlled))
			{
				rows.Add(new SotorStatItemVM("Friendly Spell Damage:", "-30%"));
			}
			if (SotorPerks.Catalyst != null && IsPerkSelected(SotorPerks.Catalyst))
			{
				rows.Add(new SotorStatItemVM("Catalyst (legendary gear):", "+5 / item", windsIcon));
			}
		}
	}

	private void BuildPerks()
	{
		_perks = new MBBindingList<PerkVM>();
		if (_hero == null || SotorPerks.Instance == null)
		{
			return;
		}
		PerkObject[] array = new PerkObject[14]
		{
			SotorPerks.EntrySpells,
			SotorPerks.Selfish,
			SotorPerks.WellControlled,
			SotorPerks.AdeptSpells,
			SotorPerks.Librarian,
			SotorPerks.StoryTeller,
			SotorPerks.OverCaster,
			SotorPerks.EfficientSpellCaster,
			SotorPerks.MasterSpells,
			SotorPerks.Improvision,
			SotorPerks.Catalyst,
			SotorPerks.Dampener,
			SotorPerks.ArcaneLink,
			SotorPerks.TrueTransmutation
		};
		foreach (PerkObject perkObject in array)
		{
			if (perkObject != null)
			{
				PerkVM.PerkAlternativeType alternativeType = ((perkObject.AlternativePerk != null) ? ((string.CompareOrdinal(perkObject.StringId, perkObject.AlternativePerk.StringId) < 0) ? PerkVM.PerkAlternativeType.FirstAlternative : PerkVM.PerkAlternativeType.SecondAlternative) : PerkVM.PerkAlternativeType.NoAlternative);
				PerkVM perkVM = new PerkVM(perkObject, IsPerkAvailable(perkObject), alternativeType, OnStartPerkSelection, delegate
				{
				}, IsPerkSelected, IsPreviousPerkSelected);
				// [RF-C] convencao VANILLA: "SPPerks\" + StringId, sem tirar o prefixo
				// "Sotor". Os perks agora vivem na skill Arcane e aparecem na tela de
				// personagem do RF, que monta o nome do sprite exatamente assim
				// (PerkVM: PerkId = "SPPerks\\" + perk.StringId). Os sprites foram
				// renomeados no RF_MagicSpriteData para casar, e assim a barra propria
				// do SOTOR e a tela do RF usam o MESMO nome.
				perkVM.PerkId = "SPPerks\\" + perkObject.StringId;
				DiagnoseSprite(perkVM.PerkId);
				_perks.Add(perkVM);
			}
		}
	}

	private static void DiagnoseSprite(string perkId)
	{
		if (!_diagnosed.Add(perkId))
		{
			return;
		}
		try
		{
			SpriteData spriteData = UIResourceManager.SpriteData;
			Sprite sprite = spriteData?.GetSprite(perkId);
			Sprite sprite2 = spriteData?.GetSprite("fireball_icon");
			int num = spriteData?.SpriteCategories?.Count ?? (-1);
			bool valueOrDefault = spriteData?.SpriteCategories?.ContainsKey("ui_sotor") == true;
			bool valueOrDefault2 = spriteData?.SpriteCategories?.ContainsKey("ui_sotor_perks") == true;
			bool flag = valueOrDefault2 && spriteData.SpriteCategories["ui_sotor_perks"].IsLoaded;
			SotorLog.Info("SpriteDiag: '" + perkId + "' -> " + ((sprite != null) ? $"FOUND ({sprite.Width}x{sprite.Height} hasTex={sprite.Texture != null})" : "NULL") + "; " + string.Format("fireball_icon -> {0}; categories={1} ui_sotor={2} ", (sprite2 != null) ? "FOUND" : "NULL", num, valueOrDefault) + $"ui_sotor_perks={valueOrDefault2} perksLoaded={flag}.");
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SpriteDiag failed: " + ex.Message);
		}
	}

	private bool IsPerkAvailable(PerkObject perk)
	{
		SkillObject spellcraft = SotorSkills.Spellcraft;
		if (_hero != null && spellcraft != null)
		{
			return _hero.GetSkillValue(spellcraft) >= (int)perk.RequiredSkillValue;
		}
		return false;
	}

	private bool IsPerkSelected(PerkObject perk)
	{
		if (_hero != null)
		{
			if (!_hero.GetPerkValue(perk))
			{
				if (_perkSelection != null)
				{
					return _perkSelection.IsPerkSelected(perk);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public bool IsPerkSelectedStaged(PerkObject perk)
	{
		return IsPerkSelected(perk);
	}

	private bool IsPreviousPerkSelected(PerkObject perk)
	{
		SkillObject sc = SotorSkills.Spellcraft;
		if (perk == null || sc == null)
		{
			return false;
		}
		List<PerkObject> list = PerkObject.All.Where((PerkObject p) => p.Skill == sc && p.RequiredSkillValue < perk.RequiredSkillValue).ToList();
		if (list.Count == 0)
		{
			return true;
		}
		PerkObject perkObject = list.OrderByDescending((PerkObject p) => p.RequiredSkillValue).First();
		if (IsPerkSelected(perkObject))
		{
			return true;
		}
		if (perkObject.AlternativePerk != null)
		{
			return IsPerkSelected(perkObject.AlternativePerk);
		}
		return false;
	}

	private void OnStartPerkSelection(PerkVM perk)
	{
		PerkObject perkObject = perk?.Perk;
		if (_hero?.HeroDeveloper != null && perkObject != null && _perkSelection != null && !IsPerkSelected(perkObject))
		{
			if (!IsPerkAvailable(perkObject) || !IsPreviousPerkSelected(perkObject))
			{
				SotorLog.Info($"Spellbook: perk '{perkObject.StringId}' not selectable (avail={IsPerkAvailable(perkObject)} prevSel={IsPreviousPerkSelected(perkObject)}).");
			}
			else
			{
				_perkSelection.SetCurrentSelectionPerk(perk);
			}
		}
	}

	public sealed override void RefreshValues()
	{
		base.RefreshValues();
		SkillObject spellcraft = SotorSkills.Spellcraft;
		HeroDeveloper heroDeveloper = _hero?.HeroDeveloper;
		if (_hero == null || spellcraft == null || heroDeveloper == null)
		{
			return;
		}
		int num = heroDeveloper.GetFocus(spellcraft) + _pendingFocus;
		SkillLevel = _hero.GetSkillValue(spellcraft);
		Focus = num;
		UnspentFocus = Math.Max(0, heroDeveloper.UnspentFocusPoints - _pendingFocus);
		AttributeValue = _hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);
		UnspentAttribute = heroDeveloper.UnspentAttributePoints;
		CastingLevelText = SotorSpellcraftHelper.GetCastingLevel(_hero).ToString();
		try
		{
			CharacterDevelopmentModel characterDevelopmentModel = Campaign.Current?.Models?.CharacterDevelopmentModel;
			if (characterDevelopmentModel != null)
			{
				SkillXpProgress = heroDeveloper.GetSkillXpProgress(spellcraft);
				SkillXpForNext = characterDevelopmentModel.GetXpRequiredForSkillLevel(SkillLevel + 1) - characterDevelopmentModel.GetXpRequiredForSkillLevel(SkillLevel);
			}
		}
		catch
		{
		}
		try
		{
			CharacterDevelopmentModel characterDevelopmentModel2 = Campaign.Current?.Models?.CharacterDevelopmentModel;
			if (characterDevelopmentModel2 != null && spellcraft != null)
			{
				FullLearningRateLevel = (int)Math.Round(characterDevelopmentModel2.CalculateLearningLimit(_hero.CharacterAttributes, num, spellcraft).ResultNumber);
				LearningRate = characterDevelopmentModel2.CalculateLearningRate(_hero.CharacterAttributes, num, SkillLevel, spellcraft).ResultNumber;
				CanLearnSkill = SkillLevel < FullLearningRateLevel;
			}
		}
		catch
		{
		}
		OnPropertyChanged("CurrentFocusLevel");
		OnPropertyChanged("CurrentLearningRateText");
		OnPropertyChanged("CurrentSkillXP");
		OnPropertyChanged("XpRequiredForNextLevel");
		OnPropertyChanged("ProgressText");
		if (_perks != null)
		{
			foreach (PerkVM perk in _perks)
			{
				perk.RefreshState();
			}
		}
		RefreshDerivedStats();
	}

	public void ExecuteAddFocus()
	{
		SkillObject spellcraft = SotorSkills.Spellcraft;
		HeroDeveloper heroDeveloper = _hero?.HeroDeveloper;
		SotorLog.Info($"CLICKDIAG focus: FIRED unspent={heroDeveloper?.UnspentFocusPoints ?? (-1)} pending={_pendingFocus} focus={((spellcraft != null && heroDeveloper != null) ? heroDeveloper.GetFocus(spellcraft) : (-1))}.");
		if (spellcraft != null && heroDeveloper != null)
		{
			int num = heroDeveloper.UnspentFocusPoints - _pendingFocus;
			int num2 = heroDeveloper.GetFocus(spellcraft) + _pendingFocus;
			int num3 = Campaign.Current?.Models?.CharacterDevelopmentModel?.MaxFocusPerSkill ?? 5;
			if (num > 0 && num2 < num3)
			{
				_pendingFocus++;
				SotorLog.Info($"Spellbook: STAGED +1 focus (pending={_pendingFocus}).");
				RefreshValues();
			}
		}
	}

	public void ExecuteAddAttribute()
	{
		HeroDeveloper heroDeveloper = _hero?.HeroDeveloper;
		if (heroDeveloper != null && heroDeveloper.UnspentAttributePoints > 0)
		{
			heroDeveloper.AddAttribute(DefaultCharacterAttributes.Intelligence, 1);
			SotorLog.Info($"Spellbook: +1 Intelligence (now {_hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence)}, unspent {heroDeveloper.UnspentAttributePoints}).");
			RefreshValues();
		}
	}
}
