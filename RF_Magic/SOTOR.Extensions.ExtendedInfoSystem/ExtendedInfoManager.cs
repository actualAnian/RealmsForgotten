using System;
using System.Collections.Generic;
using SOTOR.AbilitySystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace SOTOR.Extensions.ExtendedInfoSystem;

public class ExtendedInfoManager : CampaignBehaviorBase
{
	private static ExtendedInfoManager _instance;

	private Dictionary<string, HeroExtendedInfo> _heroInfos = new Dictionary<string, HeroExtendedInfo>();

	private const float WindsRechargePerHour = 2f;

	private float _lastLoggedPlayerRate = float.NaN;

	public static ExtendedInfoManager Instance => _instance;

	public ExtendedInfoManager()
	{
		_instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAdded);
	}

	private void OnHourlyTick()
	{
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (allAliveHero != null && !allAliveHero.IsNotable && _heroInfos.TryGetValue(allAliveHero.GetInfoKey(), out var value) && value.AllAttributes.Contains("SpellCaster"))
			{
				float armorWeight;
				float armorRechargeFactor = GetArmorRechargeFactor(allAliveHero, out armorWeight);
				float num = (2f + SotorSpellcraftHelper.GetWindsRechargeSkillBonus(allAliveHero)) * armorRechargeFactor
					* GetCatalystTownFactor(allAliveHero)
					* SOTOR.MagicAccessories.MagicAccessoryService.GetBonuses(allAliveHero).RechargeMultiplier;
				value.AddWindsOfMagic(num);
				if (allAliveHero.IsHumanPlayerCharacter && Math.Abs(num - _lastLoggedPlayerRate) > 0.001f)
				{
					_lastLoggedPlayerRate = num;
					float num2 = Math.Max(0f, (armorWeight - 5f) / 15f);
					SotorLog.Info($"Winds regen rate changed: {allAliveHero.Name} armorWt={armorWeight:0.#}lb " + $"penalty={num2:0.##} factor={armorRechargeFactor:0.##} -> +{num:0.##}/hr " + $"({value.WindsOfMagic:0}/{value.MaxWindsOfMagic:0}).");
				}
			}
		}
	}

	private static float GetArmorRechargeFactor(Hero hero, out float armorWeight)
	{
		armorWeight = 0f;
		Equipment equipment = hero?.BattleEquipment;
		if (equipment == null)
		{
			return 1f;
		}
		armorWeight = equipment.GetTotalWeightOfArmor(forHuman: true);
		float num = Math.Max(0f, (armorWeight - 5f) / 15f);
		num *= SotorSettings.ArmorWomRechargeEffectMultiplier;
		return Math.Max(0f, 1f - num);
	}

	public static float GetWindsRechargePerHour(Hero hero)
	{
		float armorWeight;
		return (2f + SotorSpellcraftHelper.GetWindsRechargeSkillBonus(hero)) * GetArmorRechargeFactor(hero, out armorWeight)
			* GetCatalystTownFactor(hero)
			* SOTOR.MagicAccessories.MagicAccessoryService.GetBonuses(hero).RechargeMultiplier;
	}

	private static float GetCatalystTownFactor(Hero hero)
	{
		PerkObject catalyst = SotorPerks.Catalyst;
		if (catalyst == null || hero == null || !hero.GetPerkValue(catalyst))
		{
			return 1f;
		}
		Settlement settlement = hero.PartyBelongedTo?.CurrentSettlement;
		if (settlement == null || !settlement.IsTown)
		{
			return 1f;
		}
		return 1.2f;
	}

	public HeroExtendedInfo GetHeroInfoFor(string heroId)
	{
		if (!_heroInfos.TryGetValue(heroId, out var value))
		{
			return null;
		}
		return value;
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		InitializeHeroes();
		EnsureStarterSpellSetup();
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int index)
	{
		if (index == 98)
		{
			InitializeHeroes();
			EnsureStarterSpellSetup();
		}
	}

	private void OnHeroCreated(Hero hero, bool bornNaturally)
	{
		EnsureHeroInfo(hero);
	}

	private void OnNewCompanionAdded(Hero hero)
	{
		if (hero != null && SotorSettings.EnableCompanionSpellcasters)
		{
			EnsureCasterSetupFor(hero);
		}
	}

	private void InitializeHeroes()
	{
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			EnsureHeroInfo(allAliveHero);
		}
	}

	private void EnsureHeroInfo(Hero hero)
	{
		if (hero != null && !hero.IsNotable)
		{
			string infoKey = hero.GetInfoKey();
			if (!_heroInfos.ContainsKey(infoKey))
			{
				_heroInfos.Add(infoKey, new HeroExtendedInfo(hero.CharacterObject));
			}
		}
	}

	public static List<Hero> GetSpellcasterPartyHeroes()
	{
		List<Hero> list = new List<Hero>();
		Hero mainHero = Hero.MainHero;
		if (mainHero == null)
		{
			return list;
		}
		list.Add(mainHero);
		if (!SotorSettings.EnableCompanionSpellcasters)
		{
			return list;
		}
		Clan playerClan = Clan.PlayerClan;
		if (playerClan != null)
		{
			foreach (Hero hero in playerClan.Heroes)
			{
				if (hero != null && hero.IsAlive && !hero.IsNotable && !hero.IsChild && !list.Contains(hero))
				{
					list.Add(hero);
				}
			}
			foreach (Hero companion in playerClan.Companions)
			{
				if (companion != null && companion.IsAlive && !companion.IsNotable && !companion.IsChild && !list.Contains(companion))
				{
					list.Add(companion);
				}
			}
		}
		if (_instance != null)
		{
			foreach (Hero item in list)
			{
				_instance.EnsureCasterSetupFor(item);
			}
		}
		return list;
	}

	private void EnsureStarterSpellSetup()
	{
		foreach (Hero spellcasterPartyHero in GetSpellcasterPartyHeroes())
		{
			EnsureCasterSetupFor(spellcasterPartyHero);
		}
	}

	private void EnsureCasterSetupFor(Hero hero)
	{
		if (hero == null)
		{
			return;
		}
		EnsureHeroInfo(hero);
		hero.AddAttribute("AbilityUser");
		hero.AddAttribute("SpellCaster");
		HeroExtendedInfo extendedInfo = hero.GetExtendedInfo();
		List<string> list = extendedInfo?.AcquiredLores ?? new List<string>();
		int num = 0;
		foreach (string item in list)
		{
			foreach (AbilityTemplate item2 in AbilityFactory.GetTemplatesByLore(item))
			{
				if (extendedInfo != null && extendedInfo.HasSpell(item2.StringID) && !hero.HasAbility(item2.StringID))
				{
					hero.AddAbility(item2.StringID);
					num++;
				}
			}
		}
		SotorLog.Info($"Granted {num} purchased spell(s) across {list.Count} owned lore(s).");
		List<string> values = hero.GetExtendedInfo()?.SelectedAbilities ?? new List<string>();
		SotorLog.Info(string.Format("Starter spell setup. hero={0} abilityUser={1} known={2} winds={3:0}/{4:0} selected=[{5}]", hero.GetInfoKey(), hero.HasAttribute("AbilityUser"), hero.GetExtendedInfo()?.AllAbilities.Count ?? 0, hero.GetExtendedInfo()?.WindsOfMagic, hero.GetExtendedInfo()?.MaxWindsOfMagic, string.Join(",", values)));
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_heroInfos", ref _heroInfos);
	}
}
