using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Helpers;
using MCM.Abstractions.Base.Global;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class Homestead : PartyComponent
{
	private readonly struct TrainingFieldDailyResult(int xpAdded, int troopsUpgraded, int goldSpent, int upgradeItemsSpent)
	{
		public readonly int XpAdded = xpAdded;

		public readonly int TroopsUpgraded = troopsUpgraded;

		public readonly int GoldSpent = goldSpent;

		public readonly int UpgradeItemsSpent = upgradeItemsSpent;

		public bool HasResults
		{
			get
			{
				if (XpAdded <= 0 && TroopsUpgraded <= 0 && GoldSpent <= 0)
				{
					return UpgradeItemsSpent > 0;
				}
				return true;
			}
		}

		public static TrainingFieldDailyResult None => new TrainingFieldDailyResult(0, 0, 0, 0);
	}

	private readonly struct MapVisualCandidate(string meshName, float scale)
	{
		public readonly string MeshName = meshName;

		public readonly float Scale = scale;
	}

	internal enum StableHorseKind
	{
		Pack,
		Riding,
		War,
		Noble
	}

	public const int MaxTier = 3;

	public const int PaidUpgradeMaxTier = 4;

	private const string MapVisualTagPrefix = "homestead_custom_map_visual";

	private const float AutoRecruitVillageRadius = 35f;

	private const float PatrolTerritoryRadius = 42f;

	private const int AutoRecruitVolunteerSlotCount = 6;

	private const float AnimalPenBreedingDailyChance = 0.33f;

	private const int AutoFoodBuyTargetDays = 2;

	private const int AutoFoodBuyMinVariety = 4;

	private const float AutoFoodBuyMaxGoldFraction = 0.25f;

	private const string TrainingFieldPrefabName = "homestead_training_field";

	private const int TrainingFieldDailyXpPerTroop = 20;

	private const float SupportingHeroHomesteadSkillXpShare = 1f / 3f;

	private const float NearbyHomesteadSceneReuseRadius = 75f;

	// Zero offset: the camp model sits exactly on the party anchor, so the
	// hover/selection circle and the nameplate (which follow the party position)
	// visually belong to the camp — it IS the homestead's map icon.
	private static readonly Vec3 MapVisualOffset = new Vec3(0f, 0f);

	private static readonly float[] HomesteadSceneSampleRadii = new float[5] { 8f, 16f, 32f, 64f, 128f };

	private static readonly float[] HomesteadSceneSampleAngles = new float[8] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

	private const int MaxMapVisualTierIndex = 4;

	[SaveableField(1)]
	private string name = "UNNAMED HOMESTEAD";

	[SaveableField(2)]
	private Hero leader;

	[SaveableField(3)]
	public int Tier;

	[SaveableField(4)]
	public int GoldStored;

	[SaveableField(5)]
	public float TierProgress;

	[SaveableField(48)]
	public bool Tier1GrowthReady;

	[SaveableField(49)]
	public bool Tier1ApprovalGranted;

	[SaveableField(50)]
	public bool AmbassadorAidingHeadman;

	[SaveableField(51)]
	public bool Tier2GrowthReady;

	[SaveableField(52)]
	public bool Tier2ApprovalGranted;

	[SaveableField(53)]
	public bool SettlementUpgradeReady;

	[SaveableField(54)]
	public bool SettlementCharterGranted;

	[SaveableField(8)]
	private HomesteadScene? homesteadScene;

	[SaveableField(9)]
	public bool AutoRecruitEnabled = true;

	[SaveableField(11)]
	private bool isRetiredOrDestroyed;

	[SaveableField(12)]
	private float mapAnchorPositionX;

	[SaveableField(13)]
	private float mapAnchorPositionY;

	[SaveableField(14)]
	private bool hasMapAnchorPosition;

	[SaveableField(15)]
	public bool AutoPatrolEnabled = true;

	[SaveableField(16)]
	private MobileParty? patrolParty;

	[SaveableField(17)]
	private int _patrolTargetIndex;

	[SaveableField(18)]
	private MobileParty? _patrolEngageTarget;

	[SaveableField(19)]
	private int _patrolChaseHours;

	private const int MaxPatrolChaseHours = 10;

	[SaveableField(20)]
	private bool _patrolFollowingPlayer;

	[SaveableField(21)]
	public bool CaravanTradingEnabled = true;

	[SaveableField(22)]
	public bool AutoFoodBuyEnabled = true;

	[SaveableField(23)]
	private Hero? _houndMasterHero;

	[SaveableField(24)]
	private Hero? _marketLadyHero;

	[SaveableField(25)]
	private Hero? _ambassadorHero;

	[SaveableField(26)]
	private List<Hero> _residentHeroes = new List<Hero>();

	[SaveableField(27)]
	private Hero? _armsMasterHero;

	[SaveableField(32)]
	private Hero? _tavernKeeperHero;

	[SaveableField(36)]
	private Hero? _masterSmithHero;

	[SaveableField(38)]
	private bool _smithUpgradeActive;

	[SaveableField(39)]
	private string? _smithUpgradeItemId;

	[SaveableField(40)]
	private string? _smithUpgradeModifierId;

	[SaveableField(41)]
	private bool _smithUpgradeIsCivilian;

	[SaveableField(42)]
	private int _smithUpgradeSlot;

	[SaveableField(43)]
	private float _smithUpgradeReadyDay;

	[SaveableField(44)]
	private bool _smithUpgradeReadyNotified;

	[SaveableField(45)]
	private ItemObject? _smithUpgradeItem;

	[SaveableField(46)]
	private bool _smithUpgradeFromInventory;

	[SaveableField(35)]
	private Hero? _troubadourHero;

	[SaveableField(55)]
	public string? PreferredTavernCultureId;

	[SaveableField(47)]
	private Hero? _stableMasterHero;

	private const int ApprenticeGraduationGarrisonBonus = 5;

	private const int ApprenticeGraduationInventoryBonus = 50;

	[SaveableField(380)]
	private int _apprenticeGraduationGarrisonBonus;

	[SaveableField(381)]
	private int _apprenticeGraduationInventoryBonus;

	[SaveableField(382)]
	private List<string> _apprenticeBonusGrantedIds = new List<string>();

	public const int MinNotableRolesForSettlement = 5;

	private const int RequiredPackAnimals = 2;

	private const int RequiredRidingHorses = 2;

	private const int RequiredWarHorses = 2;

	private const int RequiredNobleMounts = 1;

	private HashSet<string> _ambassadorNearbyPartyIds = new HashSet<string>();

	private HashSet<string> _tavernNearbyVillagerIds = new HashSet<string>();

	private int _tavernTariffAccruedToday;

	private const int MaxSceneCandidates = 12;

	private const float Tier0ProgressRate = 0.002f;

	private const float Tier1ProgressRate = 0.0006f;

	private const float Tier2ProgressRate = 0.0005f;

	private const float Tier0SkillWeight = 1f;

	private const float Tier1SkillWeight = 0.25f;

	private const float Tier2SkillWeight = 0f;

	public const float FieldKitchenHearthBonus = 2f;

	private GameEntity? _standaloneMapIcon;

	private int _standaloneMapIconTier = -1;

	private const int AmbassadorDailyRelationAssist = 1;

	public override Hero PartyOwner => leader;

	public override TextObject Name => new TextObject(name);

	public override Settlement HomeSettlement => Hero.MainHero.HomeSettlement;

	public override Hero Leader => leader;

	public override bool AvoidHostileActions => true;

	public TroopRoster Prisoners => base.MobileParty.PrisonRoster;

	public TroopRoster Troops => base.MobileParty.MemberRoster;

	public ItemRoster Stash => base.MobileParty.ItemRoster;

	public TextObject HomesteadInformation => BuildInformationTextObject();

	public int MedicalCare => homesteadScene?.TotalMedicalCare ?? 0;

	public int SceneTotalProductivity => homesteadScene?.TotalProductivity ?? 0;

	public int SceneTotalLeisure => homesteadScene?.TotalLeisure ?? 0;

	public int SceneBuildPointsLeft => homesteadScene?.BuildPointsLeftToUse ?? 0;

	public bool HasScene => homesteadScene != null;

	public Hero? HoundMasterHero => _houndMasterHero;

	public Hero? MarketLadyHero => _marketLadyHero;

	public Hero? AmbassadorHero => _ambassadorHero;

	[SaveableProperty(28)]
	public int SparringWins1v1 { get; set; }

	[SaveableProperty(29)]
	public int SparringLosses1v1 { get; set; }

	[SaveableProperty(30)]
	public int SparringWins5v5 { get; set; }

	[SaveableProperty(31)]
	public int SparringLosses5v5 { get; set; }

	public Hero? ArmsMasterHero => _armsMasterHero;

	[SaveableProperty(33)]
	public int AvailableMercenaryCount { get; set; }

	[SaveableProperty(34)]
	public string AvailableMercenaryTypeId { get; set; } = "";

	[SaveableProperty(50)]
	public bool IsMoving { get; private set; }

	[SaveableProperty(51)]
	public Vec2 TargetPosition { get; private set; } = Vec2.Zero;

	public Hero? TavernKeeperHero => _tavernKeeperHero;

	public Hero? MasterSmithHero => _masterSmithHero;

	[SaveableProperty(37)]
	public bool MasterSmithRecruited { get; set; }

	public bool SmithUpgradePending => _smithUpgradeActive;

	public bool SmithUpgradeReady
	{
		get
		{
			if (_smithUpgradeActive)
			{
				return (float)CampaignTime.Now.ToDays >= _smithUpgradeReadyDay;
			}
			return false;
		}
	}

	public string? SmithUpgradeItemId => _smithUpgradeItemId;

	public string? SmithUpgradeModifierId => _smithUpgradeModifierId;

	public bool SmithUpgradeIsCivilian => _smithUpgradeIsCivilian;

	public int SmithUpgradeSlot => _smithUpgradeSlot;

	public ItemObject? SmithUpgradeItem => _smithUpgradeItem;

	public bool SmithUpgradeFromInventory => _smithUpgradeFromInventory;

	public Hero? TroubadourHero => _troubadourHero;

	public Hero? StableMasterHero => _stableMasterHero;

	[SaveableProperty(48)]
	public bool StableMasterRecruited { get; set; }

	[SaveableProperty(49)]
	public bool StableMasterMasteryUnlocked { get; set; }

	public int FilledKeyNotableRoleCount => new Hero[8] { HoundMasterHero, MarketLadyHero, AmbassadorHero, ArmsMasterHero, TavernKeeperHero, MasterSmithHero, TroubadourHero, StableMasterHero }.Count((Hero h) => h?.IsAlive ?? false);

	public bool HasStable
	{
		get
		{
			HomesteadScene? obj = homesteadScene;
			if (obj == null)
			{
				return false;
			}
			return obj.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_stables") == true;
		}
	}

	public IReadOnlyList<Hero> ResidentHeroes
	{
		get
		{
			if (_residentHeroes == null)
			{
				_residentHeroes = new List<Hero>();
			}
			return _residentHeroes;
		}
	}

	public bool HasAmbassadorHall
	{
		get
		{
			HomesteadScene? obj = homesteadScene;
			if (obj == null)
			{
				return false;
			}
			return obj.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_ambasador_hall") == true;
		}
	}

	public bool HasMarket
	{
		get
		{
			HomesteadScene? obj = homesteadScene;
			if (obj == null)
			{
				return false;
			}
			return obj.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_market") == true;
		}
	}

	public bool HasFieldKitchen
	{
		get
		{
			HomesteadScene? obj = homesteadScene;
			if (obj == null)
			{
				return false;
			}
			return obj.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_field_kitchen") == true;
		}
	}

	public bool HasTrainingFieldBuilding
	{
		get
		{
			HomesteadScene? obj = homesteadScene;
			if (obj == null)
			{
				return false;
			}
			return obj.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_training_field") == true;
		}
	}

	public bool HasTavernBuilding
	{
		get
		{
			HomesteadScene? obj = homesteadScene;
			if (obj == null)
			{
				return false;
			}
			return obj.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_tavern") == true;
		}
	}

	public bool HasDogKennel
	{
		get
		{
			HomesteadScene? obj = homesteadScene;
			if (obj == null)
			{
				return false;
			}
			return obj.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_dog_kennel") == true;
		}
	}

	public bool HasSmithy
	{
		get
		{
			HomesteadScene? obj = homesteadScene;
			if (obj == null)
			{
				return false;
			}
			return obj.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_smithy") == true;
		}
	}

	public float MarketPriceDiscount
	{
		get
		{
			if (!HasMarket)
			{
				return 0f;
			}
			return 0.1f;
		}
	}

	public MobileParty? PatrolParty => patrolParty;

	public bool PatrolFollowingPlayer => _patrolFollowingPlayer;

	public bool HasActivePatrol
	{
		get
		{
			if (patrolParty != null && patrolParty.IsActive && !patrolParty.IsDisbanding)
			{
				return patrolParty.Party.NumberOfAllMembers > 0;
			}
			return false;
		}
	}

	public bool IsRetiredOrDestroyed => isRetiredOrDestroyed;

	public bool CanRepickScene
	{
		get
		{
			HomesteadScene obj = GetHomesteadScene();
			if (obj == null)
			{
				return false;
			}
			return obj.SavedEntities?.Count == 0;
		}
	}

	public int GraduatedApprenticeCount => _residentHeroes?.Count((Hero r) => r?.IsAlive ?? false) ?? 0;

	private List<MapVisualCandidate> GetMapIconCandidates()
	{
		int num = Math.Max(0, Math.Min(Tier, 4));
		string text = "emp";
		List<MapVisualCandidate> list = new List<MapVisualCandidate>();
		switch (num)
		{
		case 0:
			Add("mi_" + text + "_house_a", 1.15f);
			Add("mi_" + text + "_city_house_a", 1.15f);
			Add("mi_" + text + "_city_house_1", 1.15f);
			Add("mi_" + text + "_house_b", 1.15f);
			break;
		case 1:
			Add("mi_" + text + "_house_c", 1.3f);
			Add("mi_" + text + "_city_house_b", 1.3f);
			Add("mi_" + text + "_city_house_2", 1.3f);
			Add("mi_" + text + "_house_a", 1.3f);
			break;
		case 2:
			Add("mi_" + text + "_tavern", 1.05f);
			Add("mi_" + text + "_house_e", 1.2f);
			Add("mi_" + text + "_city_house_3", 1.2f);
			Add("mi_" + text + "_barracks", 1.05f);
			break;
		default:
			Add("mi_" + text + "_house_f", 1.45f);
			Add("mi_" + text + "_house_e", 1.45f);
			Add("mi_" + text + "_city_house_4", 1.45f);
			Add("mi_" + text + "_keep_1", 1f);
			break;
		}
		Add("mi_market_tent_a", 1f + (float)num * 0.6f);
		Add("map_icon_siege_camp_1", 0.7f + (float)num * 0.45f);
		return list;
		void Add(string name, float scale)
		{
			list.Add(new MapVisualCandidate(name, scale * 0.7f));
		}
	}

	public int CountBuiltPlaceable(string prefabName)
	{
		return (homesteadScene?.SavedEntities?.Count((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == prefabName)).GetValueOrDefault();
	}

	public int GetLeisureProductivityBalance()
	{
		int num = Prisoners?.TotalRegulars ?? 0;
		return SceneTotalLeisure - (SceneTotalProductivity - 2 * num);
	}

	public void SetPendingSmithUpgrade(ItemObject item, bool fromInventory, bool isCivilian, int slot, string modifierId, float readyDay)
	{
		_smithUpgradeActive = true;
		_smithUpgradeItem = item;
		_smithUpgradeFromInventory = fromInventory;
		_smithUpgradeIsCivilian = isCivilian;
		_smithUpgradeSlot = slot;
		_smithUpgradeItemId = item?.StringId;
		_smithUpgradeModifierId = modifierId;
		_smithUpgradeReadyDay = readyDay;
		_smithUpgradeReadyNotified = false;
	}

	public void ClearPendingSmithUpgrade()
	{
		_smithUpgradeActive = false;
		_smithUpgradeItem = null;
		_smithUpgradeFromInventory = false;
		_smithUpgradeItemId = null;
		_smithUpgradeModifierId = null;
		_smithUpgradeIsCivilian = false;
		_smithUpgradeSlot = 0;
		_smithUpgradeReadyDay = 0f;
		_smithUpgradeReadyNotified = false;
	}

	private void NotifySmithUpgradeReadyIfDue()
	{
		if (!SmithUpgradeReady || _smithUpgradeReadyNotified)
		{
			return;
		}
		_smithUpgradeReadyNotified = true;
		try
		{
			ItemObject itemObject = _smithUpgradeItem ?? MBObjectManager.Instance?.GetObject<ItemObject>(_smithUpgradeItemId ?? string.Empty);
			TextObject textObject = new TextObject("{=homestead_smith_upgrade_ready_banner}{ITEM} is ready — return to {SMITH} to collect it.");
			textObject.SetTextVariable("ITEM", itemObject?.Name ?? new TextObject("Your piece"));
			textObject.SetTextVariable("SMITH", _masterSmithHero?.Name ?? new TextObject("{=homestead_master_smith_fallback}the Master Smith"));
			MBInformationManager.AddQuickInformation(textObject, 0, _masterSmithHero?.CharacterObject);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "NotifySmithUpgradeReadyIfDue threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	internal void GrantApprenticeGraduationBonus(Hero? apprentice)
	{
		if (apprentice != null && apprentice.StringId != null)
		{
			if (_apprenticeBonusGrantedIds == null)
			{
				_apprenticeBonusGrantedIds = new List<string>();
			}
			if (!_apprenticeBonusGrantedIds.Contains(apprentice.StringId))
			{
				_apprenticeBonusGrantedIds.Add(apprentice.StringId);
				_apprenticeGraduationGarrisonBonus += 5;
				_apprenticeGraduationInventoryBonus += 50;
				TraceLogger.Write("Homestead", $"GrantApprenticeGraduationBonus: '{apprentice.Name}' → +{5} garrison, " + $"+{50} inventory for '{name}' (totals now " + $"{_apprenticeGraduationGarrisonBonus}/{_apprenticeGraduationInventoryBonus}).");
			}
		}
	}

	internal void RetroactivelyGrantApprenticeBonuses()
	{
		if (_residentHeroes == null)
		{
			return;
		}
		foreach (Hero item in _residentHeroes.ToList())
		{
			if (item != null && item.IsAlive)
			{
				GrantApprenticeGraduationBonus(item);
			}
		}
	}

	internal static TextObject GetRoleTitleFor(Homestead? hs, Hero? notable)
	{
		if (hs != null && notable != null)
		{
			if (notable == hs._houndMasterHero)
			{
				return new TextObject("{=homestead_role_hound_master}Hound Master");
			}
			if (notable == hs._marketLadyHero)
			{
				return new TextObject("{=homestead_role_market_lady}Market Lady");
			}
			if (notable == hs._ambassadorHero)
			{
				return new TextObject("{=homestead_role_ambassador}Ambassador");
			}
			if (notable == hs._armsMasterHero)
			{
				return new TextObject("{=homestead_role_arms_master}Arms Master");
			}
			if (notable == hs._tavernKeeperHero)
			{
				return new TextObject("{=homestead_role_tavern_keeper}Tavern Keeper");
			}
			if (notable == hs._masterSmithHero)
			{
				return new TextObject("{=homestead_role_master_smith}Master Smith");
			}
			if (notable == hs._troubadourHero)
			{
				if (!notable.IsFemale)
				{
					return new TextObject("{=homestead_role_troubadour}Troubadour");
				}
				return new TextObject("{=homestead_role_trobairitz}Trobairitz");
			}
			if (notable == hs._stableMasterHero)
			{
				return new TextObject("{=homestead_role_stable_master}Stable Master");
			}
		}
		return new TextObject("{=homestead_role_notable_fallback}Notable");
	}

	public bool HasStableFoundingHerd()
	{
		int pack = 0;
		int riding = 0;
		int war = 0;
		int noble = 0;
		CountHorsesInHoldings(out pack, out riding, out war, out noble);
		if (pack >= 2 && riding >= 2 && war >= 2)
		{
			return noble >= 1;
		}
		return false;
	}

	public string GetFoundingHerdProgress()
	{
		int pack = 0;
		int riding = 0;
		int war = 0;
		int noble = 0;
		CountHorsesInHoldings(out pack, out riding, out war, out noble);
		return $"Pack: {Math.Min(pack, 2)}/{2}, " + $"Riding: {Math.Min(riding, 2)}/{2}, " + $"War: {Math.Min(war, 2)}/{2}, " + $"Noble: {Math.Min(noble, 1)}/{1}";
	}

	private void CountHorsesInHoldings(out int pack, out int riding, out int war, out int noble)
	{
		pack = 0;
		riding = 0;
		war = 0;
		noble = 0;
		ItemRoster[] array = new ItemRoster[2]
		{
			Stash,
			MobileParty.MainParty?.ItemRoster
		};
		foreach (ItemRoster itemRoster in array)
		{
			if (itemRoster == null)
			{
				continue;
			}
			foreach (ItemRosterElement item2 in itemRoster)
			{
				ItemObject item = item2.EquipmentElement.Item;
				if (item != null && (item.IsMountable || item.ItemCategory == DefaultItemCategories.PackAnimal))
				{
					if (item.ItemCategory == DefaultItemCategories.PackAnimal)
					{
						pack += item2.Amount;
					}
					else if (item.ItemCategory == DefaultItemCategories.Horse)
					{
						riding += item2.Amount;
					}
					else if (item.ItemCategory == DefaultItemCategories.WarHorse)
					{
						war += item2.Amount;
					}
					else if (item.ItemCategory == DefaultItemCategories.NobleHorse)
					{
						noble += item2.Amount;
					}
				}
			}
		}
	}

	public void ConsumeStableFoundingHerd()
	{
		ConsumeHorseCategory(DefaultItemCategories.PackAnimal, 2);
		ConsumeHorseCategory(DefaultItemCategories.Horse, 2);
		ConsumeHorseCategory(DefaultItemCategories.WarHorse, 2);
		ConsumeHorseCategory(DefaultItemCategories.NobleHorse, 1);
	}

	private void ConsumeHorseCategory(ItemCategory category, int amountToConsume)
	{
		int num = amountToConsume;
		ItemRoster[] array = new ItemRoster[2]
		{
			MobileParty.MainParty?.ItemRoster,
			Stash
		};
		foreach (ItemRoster itemRoster in array)
		{
			if (itemRoster == null || num <= 0)
			{
				continue;
			}
			List<ItemRosterElement> list = new List<ItemRosterElement>();
			foreach (ItemRosterElement item2 in itemRoster)
			{
				ItemObject item = item2.EquipmentElement.Item;
				if (item != null && item.ItemCategory == category)
				{
					int num2 = Math.Min(item2.Amount, num);
					list.Add(new ItemRosterElement(item2.EquipmentElement, num2));
					num -= num2;
					if (num <= 0)
					{
						break;
					}
				}
			}
			foreach (ItemRosterElement item3 in list)
			{
				itemRoster.AddToCounts(item3.EquipmentElement, -item3.Amount);
			}
		}
	}

	public void AddResidentHero(Hero hero)
	{
		if (hero != null && hero.IsAlive)
		{
			if (_residentHeroes == null)
			{
				_residentHeroes = new List<Hero>();
			}
			if (!_residentHeroes.Contains(hero))
			{
				_residentHeroes.Add(hero);
			}
		}
	}

	public Homestead(Hero initialLeader)
	{
		leader = initialLeader;
	}

	public void InitializeHomesteadSceneAtPosition(CampaignVec2 position, string reason)
	{
		if (homesteadScene == null)
		{
			string text = ResolveSceneNameForPosition(position, reason);
			homesteadScene = new HomesteadScene(text, this);
			TraceLogger.Write("Homestead", $"Initialized scene for '{Name}' during {reason}: scene='{text}' position=({position.X:0.###}, {position.Y:0.###}).");
		}
	}

	public void CaptureCurrentMapAnchor()
	{
		if (base.MobileParty != null)
		{
			Vec2 getPosition2D = base.MobileParty.GetPosition2D;
			mapAnchorPositionX = getPosition2D.X;
			mapAnchorPositionY = getPosition2D.Y;
			hasMapAnchorPosition = true;
		}
	}

	public void EnsurePartyStaysAtAnchor(string reason)
	{
		if (base.MobileParty != null && !IsMoving)
		{
			if (!hasMapAnchorPosition)
			{
				CaptureCurrentMapAnchor();
			}
			CampaignVec2 mapAnchorPosition = GetMapAnchorPosition();
			float num = base.MobileParty.GetPosition2D.Distance(mapAnchorPosition.ToVec2());
			if (num > 0.05f)
			{
				base.MobileParty.SetPositionAfterMapChange(mapAnchorPosition);
				base.MobileParty.Party.SetVisualAsDirty();
				TraceLogger.Write("Homestead", $"Repositioned '{base.MobileParty.StringId}' to homestead anchor during {reason} (distance {num:0.###})");
			}
			base.MobileParty.SetMoveModeHold();
		}
	}

	public override Banner GetDefaultComponentBanner()
	{
		return leader?.ClanBanner ?? Hero.MainHero.ClanBanner ?? Banner.CreateOneColoredEmptyBanner(0);
	}

	protected override void OnChangePartyLeader(Hero newLeader)
	{
		Hero hero = leader;
		string text = new StackTrace(1, fNeedFileInfo: false).ToString();
		string text2 = string.Join(" | ", from l in text.Split(new char[1] { '\n' }).Take(8)
			select l.Trim() into l
			where l.Length > 0
			select l);
		TraceLogger.Write("Homestead", "[DIAG] OnChangePartyLeader called. Old leader='" + (hero?.Name?.ToString() ?? "null") + "', New leader='" + (newLeader?.Name?.ToString() ?? "null") + "' STACK: " + text2);
		leader = newLeader;
		if (newLeader != null)
		{
			if (newLeader.PartyBelongedTo != base.MobileParty)
			{
				TraceLogger.Write("Homestead", string.Format("OnChangePartyLeader: moving newLeader '{0}' from '{1}' into garrison.", newLeader.Name, newLeader.PartyBelongedTo?.Name?.ToString() ?? "null"));
				AddHeroToPartyAction.Apply(newLeader, base.MobileParty);
			}
			else
			{
				TraceLogger.Write("Homestead", $"OnChangePartyLeader: newLeader '{newLeader.Name}' already in garrison — skipping Apply.");
			}
			if (hero != null && hero != newLeader && hero.PartyBelongedTo == base.MobileParty && !hero.IsHumanPlayerCharacter)
			{
				TraceLogger.Write("Homestead", $"OnChangePartyLeader: returning old leader '{hero.Name}' to MainParty.");
				AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
			}
		}
		if (!PartyScreenHelper.IsHomesteadPartyScreenOpen && !PartyScreenHelper.IsApplyingRosterChanges)
		{
			PartyScreenHelper.GetActivePartyState()?.PartyScreenLogic.DoneLogic(isForced: true);
		}
		SetGameTextsForMenus();
	}

	public void DoChangePartyLeader(Hero newLeader)
	{
		OnChangePartyLeader(newLeader);
	}

	public void ClearLeaderIfMatches(Hero unavailableLeader, string reason)
	{
		if (leader == unavailableLeader)
		{
			leader = null;
			TraceLogger.Write("Homestead", "Cleared unavailable leader '" + (unavailableLeader?.StringId ?? "null") + "' during " + reason + ".");
			SetGameTextsForMenus();
		}
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		DestroyStandaloneMapIcon();
		isRetiredOrDestroyed = true;
		if (HomesteadBehavior.Instance != null)
		{
			HomesteadBehavior.Instance.HomesteadMobileParties.Remove(base.MobileParty);
			if (HomesteadBehavior.Instance.CurrentHomestead == this)
			{
				HomesteadBehavior.Instance.CurrentHomestead = null;
			}
		}
	}

	public void MarkRetiredOrDestroyed(string reason)
	{
		if (!isRetiredOrDestroyed)
		{
			isRetiredOrDestroyed = true;
			TraceLogger.Write("Homestead", $"Marked '{Name}' as retired/destroyed during {reason}.");
		}
	}

	public void RestoreForSaveCompatibility(string reason)
	{
		if (isRetiredOrDestroyed)
		{
			isRetiredOrDestroyed = false;
			TraceLogger.Write("Homestead", $"Restored retired/destroyed flag for '{Name}' during {reason}.");
		}
	}

	public HomesteadScene GetHomesteadScene()
	{
		if (homesteadScene == null)
		{
			CampaignVec2 position = (hasMapAnchorPosition ? GetMapAnchorPosition() : base.MobileParty.Position);
			string text = ResolveSceneNameForPosition(position, "lazy initialization");
			homesteadScene = new HomesteadScene(text, this);
			TraceLogger.Write("Homestead", $"Lazy initialized scene for '{Name}': scene='{text}' position=({position.X:0.###}, {position.Y:0.###}).");
		}
		return homesteadScene;
	}

	private CampaignVec2 GetMapAnchorPosition()
	{
		return new CampaignVec2(new Vec2(mapAnchorPositionX, mapAnchorPositionY), isOnLand: true);
	}

	private string ResolveSceneNameForPosition(CampaignVec2 position, string reason)
	{
		string battleSceneNameForPosition = GetBattleSceneNameForPosition(position);
		List<string> nearbyUsedHomesteadSceneNames = GetNearbyUsedHomesteadSceneNames(position);
		if (!nearbyUsedHomesteadSceneNames.Contains(battleSceneNameForPosition))
		{
			return battleSceneNameForPosition;
		}
		foreach (CampaignVec2 deterministicNearbySceneSamplePosition in GetDeterministicNearbySceneSamplePositions(position))
		{
			string battleSceneNameForPosition2 = GetBattleSceneNameForPosition(deterministicNearbySceneSamplePosition);
			if (!string.IsNullOrEmpty(battleSceneNameForPosition2) && !nearbyUsedHomesteadSceneNames.Contains(battleSceneNameForPosition2))
			{
				TraceLogger.Write("Homestead", string.Format("Selected alternate nearby scene for '{0}' during {1}: exact='{2}' alternate='{3}' anchor=({4:0.###}, {5:0.###}) sample=({6:0.###}, {7:0.###}) nearbyUsed='{8}'.", Name, reason, battleSceneNameForPosition, battleSceneNameForPosition2, position.X, position.Y, deterministicNearbySceneSamplePosition.X, deterministicNearbySceneSamplePosition.Y, string.Join(",", nearbyUsedHomesteadSceneNames)));
				return battleSceneNameForPosition2;
			}
		}
		TraceLogger.Write("Homestead", string.Format("Could not find alternate nearby scene for '{0}' during {1}; using exact scene '{2}'. nearbyUsed='{3}'.", Name, reason, battleSceneNameForPosition, string.Join(",", nearbyUsedHomesteadSceneNames)));
		return battleSceneNameForPosition;
	}

	private static string GetBattleSceneNameForPosition(CampaignVec2 position)
	{
		try
		{
			string battleSceneForMapPatch = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(in position), isNavalEncounter: false);
			if (!string.IsNullOrEmpty(battleSceneForMapPatch))
			{
				return battleSceneForMapPatch;
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", $"Failed resolving battle scene at ({position.X:0.###}, {position.Y:0.###}): {ex.Message}");
		}
		return "battle_terrain_v1";
	}

	public List<string> GetCandidateSceneNames()
	{
		CampaignVec2 position = (hasMapAnchorPosition ? GetMapAnchorPosition() : base.MobileParty.Position);
		List<string> battleSceneCandidatesForPosition = GetBattleSceneCandidatesForPosition(position, includeAdjacent: true);
		List<string> nearbyUsed = GetNearbyUsedHomesteadSceneNames(position);
		string current = homesteadScene?.SceneName;
		battleSceneCandidatesForPosition = battleSceneCandidatesForPosition.Where((string id) => id == current || !nearbyUsed.Contains(id)).ToList();
		if (!string.IsNullOrEmpty(current))
		{
			battleSceneCandidatesForPosition.Remove(current);
			battleSceneCandidatesForPosition.Insert(0, current);
		}
		return battleSceneCandidatesForPosition;
	}

	public static List<string> GetBattleSceneCandidatesForPosition(CampaignVec2 position, bool includeAdjacent)
	{
		List<string> ordered = new List<string>();
		try
		{
			GameSceneDataManager instance = GameSceneDataManager.Instance;
			if (instance?.SingleplayerBattleScenes == null)
			{
				return ordered;
			}
			AddDistinct(ScenesForExactPosition(instance, position));
			if (includeAdjacent)
			{
				float[] homesteadSceneSampleRadii = HomesteadSceneSampleRadii;
				foreach (float num in homesteadSceneSampleRadii)
				{
					if (ordered.Count < 12)
					{
						float[] homesteadSceneSampleAngles = HomesteadSceneSampleAngles;
						for (int j = 0; j < homesteadSceneSampleAngles.Length; j++)
						{
							float x = homesteadSceneSampleAngles[j] * TaleWorlds.Library.MathF.PI / 180f;
							CampaignVec2 position2 = position + new Vec2(TaleWorlds.Library.MathF.Cos(x) * num, TaleWorlds.Library.MathF.Sin(x) * num);
							AddDistinct(ScenesForExactPosition(instance, position2));
						}
						continue;
					}
					break;
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "GetBattleSceneCandidatesForPosition failed: " + ex.Message);
		}
		return ordered;
		void AddDistinct(List<string> ids)
		{
			foreach (string id in ids)
			{
				if (!string.IsNullOrEmpty(id) && !ordered.Contains(id) && ordered.Count < 12)
				{
					ordered.Add(id);
				}
			}
		}
	}

	private static List<string> ScenesForExactPosition(GameSceneDataManager mgr, CampaignVec2 position)
	{
		try
		{
			MapPatchData patch = Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(in position);
			List<string> list = (from s in mgr.SingleplayerBattleScenes
				where !s.IsNaval && s.MapIndices != null && s.MapIndices.Contains(patch.sceneIndex)
				select s.SceneID).ToList();
			if (list.Count > 0)
			{
				return list;
			}
			Campaign.Current.MapSceneWrapper.GetEnvironmentTerrainTypesCount(in position, out var terrain);
			return (from s in mgr.SingleplayerBattleScenes
				where !s.IsNaval && s.Terrain == terrain
				select s.SceneID).ToList();
		}
		catch
		{
			return new List<string>();
		}
	}

	public bool TryChooseScene(string sceneName)
	{
		if (string.IsNullOrEmpty(sceneName) || !CanRepickScene)
		{
			return false;
		}
		homesteadScene = new HomesteadScene(sceneName, this);
		TraceLogger.Write("Homestead", $"Player re-picked map '{sceneName}' for '{Name}'.");
		return true;
	}

	private List<string> GetNearbyUsedHomesteadSceneNames(CampaignVec2 position)
	{
		List<string> list = new List<string>();
		if (HomesteadBehavior.Instance == null)
		{
			return list;
		}
		foreach (Homestead item in HomesteadBehavior.Instance.HomesteadMobileParties.Values.ToList())
		{
			if (item == null || item == this || item.IsRetiredOrDestroyed || item.homesteadScene == null)
			{
				continue;
			}
			MobileParty mobileParty = item.MobileParty;
			if (mobileParty != null && !mobileParty.IsDisbanding && !(mobileParty.GetPosition2D.Distance(position.ToVec2()) > 75f))
			{
				string sceneName = item.homesteadScene.SceneName;
				if (!string.IsNullOrEmpty(sceneName) && !list.Contains(sceneName))
				{
					list.Add(sceneName);
				}
			}
		}
		return list;
	}

	private IEnumerable<CampaignVec2> GetDeterministicNearbySceneSamplePositions(CampaignVec2 origin)
	{
		int deterministicSceneSampleSeed = GetDeterministicSceneSampleSeed(origin);
		int angleOffset = ((HomesteadSceneSampleAngles.Length != 0) ? (deterministicSceneSampleSeed % HomesteadSceneSampleAngles.Length) : 0);
		float[] homesteadSceneSampleRadii = HomesteadSceneSampleRadii;
		foreach (float radius in homesteadSceneSampleRadii)
		{
			for (int j = 0; j < HomesteadSceneSampleAngles.Length; j++)
			{
				float x = HomesteadSceneSampleAngles[(j + angleOffset) % HomesteadSceneSampleAngles.Length] * TaleWorlds.Library.MathF.PI / 180f;
				Vec2 vec = new Vec2(TaleWorlds.Library.MathF.Cos(x) * radius, TaleWorlds.Library.MathF.Sin(x) * radius);
				yield return origin + vec;
			}
		}
	}

	private int GetDeterministicSceneSampleSeed(CampaignVec2 origin)
	{
		int num = 17;
		string text = name ?? "";
		foreach (char c in text)
		{
			num = num * 31 + c;
		}
		num = num * 31 + (int)(origin.X * 100f);
		num = num * 31 + (int)(origin.Y * 100f);
		if (num != int.MinValue)
		{
			return Math.Abs(num);
		}
		return 0;
	}

	public void PartyLeaderDied()
	{
		Utils.ShowMessageBox(Utils.GetLocalizedString("{=homestead_courier_arrives}A courier arrives..."), Utils.GetLocalizedString("{=homestead_leader_died_msg}They bring you a message that bears bad news. {LEADER_NAME} has died and {HOMESTEAD_NAME} needs a new leader assigned to it.", ("LEADER_NAME", leader.Name.ToString()), ("HOMESTEAD_NAME", name)));
		if (HomesteadBehavior.Instance.CurrentHomestead == this)
		{
			if (PlayerEncounter.Current.IsPlayerWaiting)
			{
				GameMenu.ActivateGameMenu("homestead_menu_wait_waiting");
			}
			else
			{
				GameMenu.ActivateGameMenu("homestead_menu_main");
			}
		}
		leader = null;
	}

	public void ChangeName(string newName)
	{
		name = newName;
		base.Party.SetCustomName(Name);
		SetGameTextsForMenus();
	}

	public bool PlayerChangeGoldStored(int amountToChange, out string failReason)
	{
		int num = TaleWorlds.Library.MathF.Abs(amountToChange);
		GameTexts.SetVariable("GOLD_AMOUNT", num);
		string text = "";
		string text2 = "";
		if (amountToChange < 0)
		{
			if (GoldStored < num)
			{
				failReason = new TextObject("{=homestead_withdraw_gold_failreason}This homestead does not have that much gold stored!").ToString();
				return false;
			}
			Hero.MainHero.ChangeHeroGold(num);
			GoldStored -= num;
			text = "str_you_received_gold_with_icon";
			text2 = "event:/ui/notification/coins_positive";
		}
		else
		{
			if (Hero.MainHero.Gold < amountToChange)
			{
				failReason = new TextObject("{=homestead_deposit_gold_failreason}You do not have that much gold!").ToString();
				return false;
			}
			Hero.MainHero.ChangeHeroGold(-amountToChange);
			GoldStored += amountToChange;
			text = "str_gold_removed_with_icon";
			text2 = "event:/ui/notification/coins_negative";
		}
		InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText(text).ToString(), text2));
		SetGameTextsForMenus();
		failReason = "";
		return true;
	}

	public void StartMoving(Vec2 target)
	{
		IsMoving = true;
		TargetPosition = target;
		SyncStandaloneMapIcon("Started moving");
		base.MobileParty.SetMoveGoToPoint(new CampaignVec2(target, isOnLand: true), MobileParty.NavigationType.Default);
		if (HasActivePatrol && PatrolParty != null)
		{
			PatrolParty.SetMoveEscortParty(base.MobileParty, MobileParty.NavigationType.Default, isTargetingPort: false);
		}
		foreach (MobileParty item in MobileParty.All.Where((MobileParty p) => p.PartyComponent is HomesteadRecruiterComponent homesteadRecruiterComponent && homesteadRecruiterComponent.HomeHomestead == this))
		{
			item.SetMoveEscortParty(base.MobileParty, MobileParty.NavigationType.Default, isTargetingPort: false);
		}
	}

	public void FinishMoving()
	{
		base.MobileParty.SetMoveModeHold();
		IsMoving = false;
		CaptureCurrentMapAnchor();
		base.MobileParty.Party.SetVisualAsDirty();
		SyncStandaloneMapIcon("Finished moving");
		if (HasActivePatrol && PatrolParty != null)
		{
			_patrolEngageTarget = null;
			_patrolTargetIndex = 0;
			ApplyPatrolMovement(PatrolParty);
		}
		InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_relocation_unpacked}Your homestead has unpacked at its new location."), Colors.Green));
	}

	public void HourlyTick()
	{
		NotifySmithUpgradeReadyIfDue();
	}

	public void ApplyPatrolMovement(MobileParty party)
	{
		if (party == null || !HasActivePatrol)
		{
			return;
		}
		if (IsMoving)
		{
			party.SetMoveGoToPoint(new CampaignVec2(base.MobileParty.GetPosition2D, isOnLand: true), MobileParty.NavigationType.Default);
			return;
		}
		if (party.GetPosition2D.Distance(base.MobileParty.GetPosition2D) < 3f)
		{
			TransferPatrolPrisoners(party);
			RefillPatrolTroops(party);
		}
		if (_patrolFollowingPlayer && MobileParty.MainParty != null)
		{
			party.ShouldJoinPlayerBattles = true;
			party.SetMoveEscortParty(MobileParty.MainParty, MobileParty.NavigationType.Default, isTargetingPort: false);
			return;
		}
		if (_patrolEngageTarget != null)
		{
			_patrolChaseHours++;
			bool flag = base.MobileParty.GetPosition2D.Distance(_patrolEngageTarget.GetPosition2D) > 42f;
			if (!_patrolEngageTarget.IsActive || _patrolEngageTarget.CurrentSettlement != null || _patrolChaseHours > 10 || !MobilePartyEngageIsAllowed(_patrolEngageTarget, party) || flag)
			{
				_patrolEngageTarget = null;
				_patrolChaseHours = 0;
			}
		}
		if (_patrolEngageTarget == null)
		{
			MobileParty bestNearestHostileParty = GetBestNearestHostileParty(party);
			if (bestNearestHostileParty != null)
			{
				_patrolEngageTarget = bestNearestHostileParty;
				_patrolChaseHours = 0;
			}
		}
		if (_patrolEngageTarget != null && _patrolEngageTarget.IsActive)
		{
			party.Aggressiveness = 1f;
			party.SetMoveEngageParty(_patrolEngageTarget, party.NavigationCapability);
			return;
		}
		party.Aggressiveness = 0.9f;
		List<Settlement> list = GetAutoRecruitVillageCandidates().ToList();
		Vec2 getPosition2D = base.MobileParty.GetPosition2D;
		int num = list.Count + 1;
		if (num > 1)
		{
			if (_patrolTargetIndex >= num)
			{
				_patrolTargetIndex = 0;
			}
			Vec2 vec = ((_patrolTargetIndex != 0) ? (list[_patrolTargetIndex - 1]?.GetPosition2D ?? getPosition2D) : getPosition2D);
			if (party.GetPosition2D.Distance(vec) < 3f)
			{
				_patrolTargetIndex = (_patrolTargetIndex + 1) % num;
				vec = ((_patrolTargetIndex != 0) ? (list[_patrolTargetIndex - 1]?.GetPosition2D ?? getPosition2D) : getPosition2D);
			}
			party.SetMoveGoToPoint(new CampaignVec2(vec, isOnLand: true), party.NavigationCapability);
			return;
		}
		Vec2 getPosition2D2 = base.MobileParty.GetPosition2D;
		float num2 = 3f;
		CampaignVec2[] array = new CampaignVec2[4]
		{
			new CampaignVec2(new Vec2(getPosition2D2.x - num2, getPosition2D2.y - num2), isOnLand: true),
			new CampaignVec2(new Vec2(getPosition2D2.x + num2, getPosition2D2.y - num2), isOnLand: true),
			new CampaignVec2(new Vec2(getPosition2D2.x + num2, getPosition2D2.y + num2), isOnLand: true),
			new CampaignVec2(new Vec2(getPosition2D2.x - num2, getPosition2D2.y + num2), isOnLand: true)
		};
		int num3 = _patrolTargetIndex % 4;
		CampaignVec2 point = array[num3];
		if (party.GetPosition2D.Distance(point.ToVec2()) < 3f)
		{
			_patrolTargetIndex = (_patrolTargetIndex + 1) % 4;
			point = array[_patrolTargetIndex % 4];
		}
		party.SetMoveGoToPoint(point, MobileParty.NavigationType.Default);
	}

	private void TransferPatrolPrisoners(MobileParty patrol)
	{
		if (patrol == null || patrol.PrisonRoster.TotalRegulars <= 0)
		{
			return;
		}
		int num = GetPrisonerLimit() - (Prisoners?.TotalRegulars ?? 0);
		if (num <= 0)
		{
			return;
		}
		int num2 = 0;
		int num3 = patrol.PrisonRoster.Count - 1;
		while (num3 >= 0 && num > 0)
		{
			TroopRosterElement elementCopyAtIndex = patrol.PrisonRoster.GetElementCopyAtIndex(num3);
			if (elementCopyAtIndex.Character != null && elementCopyAtIndex.Number > 0 && !elementCopyAtIndex.Character.IsHero)
			{
				int num4 = Math.Min(elementCopyAtIndex.Number, num);
				Prisoners?.AddToCounts(elementCopyAtIndex.Character, num4);
				patrol.PrisonRoster.RemoveTroop(elementCopyAtIndex.Character, num4);
				num2 += num4;
				num -= num4;
			}
			num3--;
		}
		if (num2 > 0)
		{
			SetGameTextsForMenus();
			TraceLogger.Write("Homestead", $"Patrol '{patrol.StringId}' transferred {num2} prisoners to '{Name}'.");
			if (GlobalSettings<MCMSettings>.Instance.ShowPatrolNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_patrol_prisoners_transferred", "The patrol returned to {HOMESTEAD_NAME} and transferred {TRANSFERRED_COUNT} prisoners.", 80f, 200f, 255f, ("HOMESTEAD_NAME", name), ("TRANSFERRED_COUNT", num2.ToString()));
			}
		}
	}

	private void RefillPatrolTroops(MobileParty patrol)
	{
		if (patrol == null || Troops == null)
		{
			return;
		}
		int num = 0;
		foreach (TroopRosterElement item in Troops.GetTroopRoster())
		{
			if (item.Character != null && !item.Character.IsHero && item.Number > 0)
			{
				num += item.Number;
			}
		}
		int totalRegulars = patrol.MemberRoster.TotalRegulars;
		int num2 = num + totalRegulars;
		int num3 = Math.Min(GetTroopLimit() / 2, num2 / 2) - totalRegulars;
		if (num3 <= 0)
		{
			return;
		}
		int num4 = 0;
		foreach (TroopRosterElement item2 in Troops.GetTroopRoster().ToList())
		{
			if (num3 <= 0)
			{
				break;
			}
			if (item2.Character != null && !item2.Character.IsHero && item2.Number > 0)
			{
				int num5 = Math.Min(item2.Number, num3);
				patrol.MemberRoster.AddToCounts(item2.Character, num5);
				Troops.RemoveTroop(item2.Character, num5);
				num4 += num5;
				num3 -= num5;
			}
		}
		if (num4 > 0)
		{
			SetGameTextsForMenus();
			TraceLogger.Write("Homestead", $"Patrol '{patrol.StringId}' refilled {num4} troops from '{Name}'.");
			if (GlobalSettings<MCMSettings>.Instance.ShowPatrolNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_patrol_troops_refilled", "The patrol returned to {HOMESTEAD_NAME} and restocked {TRANSFERRED_COUNT} troops.", 80f, 200f, 255f, ("HOMESTEAD_NAME", name), ("TRANSFERRED_COUNT", num4.ToString()));
			}
		}
	}

	private MobileParty? GetBestNearestHostileParty(MobileParty patrol)
	{
		float seeingRange = patrol.SeeingRange;
		float num = seeingRange * seeingRange;
		float estimatedStrength = patrol.Party.EstimatedStrength;
		MobileParty result = null;
		float num2 = -1f;
		Vec2 getPosition2D = patrol.GetPosition2D;
		Vec2 getPosition2D2 = base.MobileParty.GetPosition2D;
		float num3 = 1764f;
		foreach (MobileParty item in MobileParty.All)
		{
			if (!MobilePartyEngageIsAllowed(item, patrol))
			{
				continue;
			}
			Vec2 getPosition2D3 = item.GetPosition2D;
			if (!(getPosition2D.DistanceSquared(getPosition2D3) > num) && !(getPosition2D2.DistanceSquared(getPosition2D3) > num3))
			{
				float estimatedStrength2 = item.Party.EstimatedStrength;
				if (!(estimatedStrength2 >= estimatedStrength * 0.8f) && estimatedStrength2 > num2)
				{
					num2 = estimatedStrength2;
					result = item;
				}
			}
		}
		return result;
	}

	private bool MobilePartyEngageIsAllowed(MobileParty candidate, MobileParty patrol)
	{
		if (candidate == null || !candidate.IsActive)
		{
			return false;
		}
		if (candidate.CurrentSettlement != null)
		{
			return false;
		}
		if (candidate.IsVillager || candidate.IsCaravan)
		{
			return false;
		}
		if (candidate.IsMainParty)
		{
			return false;
		}
		if (!FactionManager.IsAtWarAgainstFaction(candidate.MapFaction, patrol.MapFaction))
		{
			return false;
		}
		if (candidate.Speed > patrol.Speed * 1.2f)
		{
			return false;
		}
		return true;
	}

	private void DailyTickFeedPatrol()
	{
		if (!HasActivePatrol || patrolParty == null)
		{
			return;
		}
		int numberOfAllMembers = patrolParty.Party.NumberOfAllMembers;
		if (numberOfAllMembers <= 0)
		{
			return;
		}
		ItemObject itemObject = Campaign.Current.ObjectManager.GetObject<ItemObject>("grain");
		if (itemObject != null)
		{
			int num = Math.Max(10, numberOfAllMembers * 2);
			int num2 = 0;
			foreach (ItemRosterElement item in patrolParty.ItemRoster)
			{
				if (item.EquipmentElement.Item?.StringId == "grain")
				{
					num2 += Math.Max(0, item.Amount);
				}
			}
			int num3 = num - num2;
			if (num3 > 0)
			{
				patrolParty.ItemRoster.AddToCounts(itemObject, num3);
			}
		}
		if (patrolParty.Morale < 60f)
		{
			patrolParty.RecentEventsMorale += 15f;
		}
	}

	public void OnPatrolBattleEnded(MapEvent mapEvent)
	{
		if (!HasActivePatrol || patrolParty == null || mapEvent == null)
		{
			return;
		}
		BattleSideEnum battleSideEnum = BattleSideEnum.NumSides;
		BattleSideEnum[] array = new BattleSideEnum[2]
		{
			BattleSideEnum.Attacker,
			BattleSideEnum.Defender
		};
		foreach (BattleSideEnum battleSideEnum2 in array)
		{
			foreach (MapEventParty item in mapEvent.PartiesOnSide(battleSideEnum2))
			{
				if (item?.Party?.MobileParty == patrolParty)
				{
					battleSideEnum = battleSideEnum2;
					break;
				}
			}
			if (battleSideEnum != BattleSideEnum.NumSides)
			{
				break;
			}
		}
		int num;
		switch (battleSideEnum)
		{
		case BattleSideEnum.NumSides:
			return;
		default:
			num = 1;
			break;
		case BattleSideEnum.Attacker:
			num = 0;
			break;
		}
		BattleSideEnum side = (BattleSideEnum)num;
		int num2 = 0;
		foreach (MapEventParty item2 in mapEvent.PartiesOnSide(side))
		{
			num2 += (item2?.Party?.NumberOfAllMembers).GetValueOrDefault();
		}
		int num3 = Math.Min(150, Math.Max(30, num2 * 3));
		int num4 = 0;
		int num5 = 0;
		PartyBase party = patrolParty.Party;
		foreach (TroopRosterElement item3 in patrolParty.MemberRoster.GetTroopRoster().ToList())
		{
			CharacterObject character = item3.Character;
			int number = item3.Number;
			if (character == null || character.IsHero || number <= 0)
			{
				continue;
			}
			int num6 = patrolParty.MemberRoster.FindIndexOfTroop(character);
			if (num6 < 0)
			{
				continue;
			}
			int num7 = num3 * number;
			int num8 = Math.Max(0, patrolParty.MemberRoster.GetElementXp(num6)) + num7;
			num4 += num7;
			MCMSettings? instance = GlobalSettings<MCMSettings>.Instance;
			if (instance != null && !instance.AutoTroopUpgradesEnabled)
			{
				patrolParty.MemberRoster.SetElementXp(num6, num8);
				continue;
			}
			if (!TryChooseTrainingFieldUpgradeTarget(party, character, out CharacterObject upgradeTarget, out int xpCost, out int goldCost) || upgradeTarget == null)
			{
				patrolParty.MemberRoster.SetElementXp(num6, num8);
				continue;
			}
			int val = ((xpCost > 0) ? (num8 / xpCost) : 0);
			int val2 = ((goldCost > 0) ? (GoldStored / goldCost) : int.MaxValue);
			ItemCategory upgradeRequiresItemFromCategory = upgradeTarget.UpgradeRequiresItemFromCategory;
			int val3 = ((upgradeRequiresItemFromCategory != null) ? CountUpgradeItems(upgradeRequiresItemFromCategory) : int.MaxValue);
			int num9 = Math.Min(number, Math.Min(val, Math.Min(val2, val3)));
			if (num9 <= 0)
			{
				patrolParty.MemberRoster.SetElementXp(num6, num8);
				continue;
			}
			int num10 = ConsumeUpgradeItems(upgradeRequiresItemFromCategory, num9);
			if (upgradeRequiresItemFromCategory != null && num10 < num9)
			{
				num9 = num10;
			}
			if (num9 <= 0)
			{
				patrolParty.MemberRoster.SetElementXp(num6, num8);
				continue;
			}
			int num11 = goldCost * num9;
			GoldStored = Math.Max(0, GoldStored - num11);
			int number2 = Math.Max(0, num8 - xpCost * num9);
			patrolParty.MemberRoster.AddToCounts(upgradeTarget, num9, insertAtFront: false, 0, 0, removeDepleted: false);
			patrolParty.MemberRoster.RemoveTroop(character, num9);
			int num12 = patrolParty.MemberRoster.FindIndexOfTroop(character);
			if (num12 >= 0)
			{
				patrolParty.MemberRoster.SetElementXp(num12, number2);
			}
			num5 += num9;
			DispatchTrainingFieldUpgradeEvent(party, character, upgradeTarget, num9);
			TraceLogger.Write("Homestead", $"Patrol battle: upgraded {num9} '{character.StringId}' → '{upgradeTarget.StringId}' at '{Name}' xpSpent={xpCost * num9} goldSpent={num11} itemsConsumed={num10}.");
		}
		if (num4 > 0 || num5 > 0)
		{
			TraceLogger.Write("Homestead", $"Patrol battle XP for '{Name}': xpGranted={num4} troopsUpgraded={num5} enemyCount={num2}.");
		}
	}

	public void SetPatrolFollowPlayer(bool follow)
	{
		_patrolFollowingPlayer = follow;
		if (patrolParty != null)
		{
			patrolParty.ShouldJoinPlayerBattles = follow;
		}
	}

	public bool DisbandPatrolToGarrisonOrPlayerParty()
	{
		if (!HasActivePatrol || patrolParty == null)
		{
			return false;
		}
		MobileParty mobileParty = patrolParty;
		try
		{
			if (MobileParty.MainParty?.MemberRoster != null)
			{
				MobileParty.MainParty.MemberRoster.Add(mobileParty.MemberRoster);
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("Homestead", $"Failed transferring patrol troops to garrison or player party for '{Name}': {arg}");
		}
		patrolParty = null;
		_patrolFollowingPlayer = false;
		_patrolEngageTarget = null;
		_patrolChaseHours = 0;
		HomesteadBehavior.Instance?.PatrolMobileParties.Remove(mobileParty);
		try
		{
			if (mobileParty.IsActive && !mobileParty.IsDisbanding)
			{
				DestroyPartyAction.Apply(null, mobileParty);
			}
		}
		catch (Exception arg2)
		{
			TraceLogger.Write("Homestead", $"Failed destroying patrol party for '{Name}': {arg2}");
		}
		SetGameTextsForMenus();
		TraceLogger.Write("Homestead", $"Disbanded patrol from '{Name}' into player party.");
		return false;
	}

	public void DailyTick()
	{
		if (!IsRetiredOrDestroyed && base.MobileParty != null && !base.MobileParty.IsDisbanding && base.MobileParty.IsActive)
		{
			if (!IsMoving)
			{
				UpdateTierProgress();
				RefreshMercenaries();
			}
			DailyTickPrisonerEscape();
			DailyTickMoraleChange();
			DailyTickNotableRelationMorale();
			int goldChange = 0;
			Dictionary<string, int> producedItems = new Dictionary<string, int>();
			TrainingFieldDailyResult trainingResult = TrainingFieldDailyResult.None;
			if (!IsMoving)
			{
				goldChange = DailyTickChangeGoldStored();
				DailyTickNoGoldPenalty();
				DailyTickHeadmanTrust();
				DailyTickLandPatent();
				DailyTickSettlementCharter();
				producedItems = DailyTickProduceItems();
				trainingResult = DailyTickTrainingField();
				DailyTickAutoRecruitSettlers();
			}
			DailyTickLeaderSkillXp(trainingResult.TroopsUpgraded);
			DailyTickAutoFoodBuy();
			DailyTickCheckPatrol();
			DailyTickFeedPatrol();
			DailyTickEnsureHoundMasterHero();
			DailyTickEnsureMarketLadyHero();
			DailyTickEnsureAmbassadorHero();
			DailyTickEnsureArmsMasterHero();
			DailyTickEnsureTavernKeeperHero();
			DailyTickEnsureMasterSmithHero();
			DailyTickEnsureStableMasterHero();
			DailyTickEnsureTroubadourHero();
			DailyTickArmsMasterDrillSergeantXp();
			DailyTickAmbassadorRelations();
			DailyTickAmbassadorCharmXp();
			DailyTickMarketTradeXp();
			NotifyDailyTickResults(goldChange, producedItems, trainingResult);
		}
	}

	private void DailyTickArmsMasterDrillSergeantXp()
	{
		if (ArmsMasterHero == null || !ArmsMasterHero.IsAlive)
		{
			return;
		}
		HomesteadBehavior instance = HomesteadBehavior.Instance;
		if (instance == null || !instance.HasArmsMasterMasteryUnlocked || base.MobileParty?.MemberRoster == null)
		{
			return;
		}
		foreach (TroopRosterElement item in base.MobileParty.MemberRoster.GetTroopRoster().ToList())
		{
			CharacterObject character = item.Character;
			if (character == null)
			{
				continue;
			}
			if (character.IsHero)
			{
				Hero heroObject = character.HeroObject;
				if (heroObject != null && heroObject.IsAlive && !heroObject.IsHumanPlayerCharacter)
				{
					heroObject.AddSkillXp(DefaultSkills.Leadership, 15f);
				}
				continue;
			}
			int number = item.Number;
			if (number > 0)
			{
				int num = base.MobileParty.MemberRoster.FindIndexOfTroop(character);
				if (num >= 0)
				{
					int num2 = Math.Max(0, base.MobileParty.MemberRoster.GetElementXp(num));
					base.MobileParty.MemberRoster.SetElementXp(num, num2 + 20 * number);
				}
			}
		}
	}

	public void OnGarrisonDefensiveBattleEnded(MapEvent mapEvent)
	{
		if (base.MobileParty?.MemberRoster == null || mapEvent == null)
		{
			return;
		}
		int num = 0;
		foreach (MapEventParty item in mapEvent.PartiesOnSide(BattleSideEnum.Attacker))
		{
			num += (item?.Party?.NumberOfAllMembers).GetValueOrDefault();
		}
		int num2 = (int)Math.Ceiling((float)Math.Min(150, Math.Max(30, num * 3)) * 0.15f);
		if (num2 <= 0)
		{
			return;
		}
		foreach (TroopRosterElement item2 in base.MobileParty.MemberRoster.GetTroopRoster().ToList())
		{
			CharacterObject character = item2.Character;
			int number = item2.Number;
			if (character != null && !character.IsHero && number > 0)
			{
				int num3 = base.MobileParty.MemberRoster.FindIndexOfTroop(character);
				if (num3 >= 0)
				{
					int num4 = Math.Max(0, base.MobileParty.MemberRoster.GetElementXp(num3));
					base.MobileParty.MemberRoster.SetElementXp(num3, num4 + num2 * number);
				}
			}
		}
	}

	private void DailyTickEnsureHoundMasterHero()
	{
		MigrateNotableHeroName(_houndMasterHero, "Hound Master");
		TryEnsureHoundMasterHero();
	}

	internal void TryEnsureHoundMasterHero()
	{
		if (_houndMasterHero != null && _houndMasterHero.IsAlive)
		{
			return;
		}
		HomesteadScene? obj = homesteadScene;
		if (obj == null || obj.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_dog_kennel") != true)
		{
			return;
		}
		try
		{
			CultureObject playerCulture = Hero.MainHero?.Culture;
			CharacterObject template = null;
			if (playerCulture != null)
			{
				template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Culture == playerCulture && c.Occupation == Occupation.RuralNotable);
			}
			if (template == null)
			{
				template = Game.Current?.ObjectManager?.GetObject<CharacterObject>("homestead_hound_master_template");
			}
			if (template == null)
			{
				template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Occupation == Occupation.RuralNotable);
			}
			TraceLogger.Write("Homestead", "TryEnsureHoundMasterHero: template → " + ((template == null) ? "NOT FOUND" : $"'{template.StringId}' culture={template.Culture?.StringId} occupation={template.Occupation}"));
			if (template == null)
			{
				return;
			}
			Settlement settlement = Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage && s.Culture == template.Culture) ?? Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage);
			TraceLogger.Write("Homestead", $"TryEnsureHoundMasterHero: dummy settlement '{settlement?.Name}' (culture '{settlement?.Culture?.StringId}')");
			if (settlement == null)
			{
				TraceLogger.Write("Homestead", "TryEnsureHoundMasterHero: no village found — skipping hero creation.");
				return;
			}
			Hero hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 30 + MBRandom.RandomInt(15));
			Utils.ApplyRandomPersonalityTraits(hero);
			hero.SetNewOccupation(Occupation.RuralNotable);
			ForceDetachHeroFromSettlement(hero, settlement);
			string text = hero.FirstName?.ToString() ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "the Hound Master";
			}
			TextObject firstName = new TextObject(text);
			TextObject textObject = new TextObject(text + " the Hound Master of " + name);
			hero.SetName(textObject, firstName);
			PatchCharacterObjectName(hero.CharacterObject, textObject);
			Equipment randomBattleEquipment = template.RandomBattleEquipment;
			if (randomBattleEquipment != null)
			{
				EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, randomBattleEquipment);
			}
			Equipment randomCivilianEquipment = template.RandomCivilianEquipment;
			if (randomCivilianEquipment != null)
			{
				hero.CivilianEquipment.FillFrom(randomCivilianEquipment, useSourceEquipmentType: false);
			}
			hero.AddPower(200f);
			hero.ChangeState(Hero.CharacterStates.Active);
			_houndMasterHero = hero;
			TraceLogger.Write("Homestead", $"TryEnsureHoundMasterHero: created hero '{hero.Name}' charObjName='{hero.CharacterObject?.Name}' ({hero.StringId}) for '{name}'.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "TryEnsureHoundMasterHero: FAILED — " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	private void CleanupOrphanedNotable(ref Hero? heroRef, string title)
	{
		if (heroRef != null)
		{
			try
			{
				TraceLogger.Write("Homestead", $"CleanupOrphanedNotable: retiring '{heroRef.Name}' ({title}) — building removed from '{name}'.");
				HomesteadBehavior.Instance?.CancelNotableFavor(heroRef);
				HomesteadBehavior.Instance?.GetActiveDeliveryQuest(heroRef)?.CancelDelivery();
				HomesteadBehavior.Instance?.GetActiveApparelQuest(heroRef)?.CancelApparel();
				HomesteadBehavior.Instance?.GetActiveBuildingQuest(heroRef)?.CancelBuilding();
				HomesteadBehavior.Instance?.GetActiveApprenticeQuest(heroRef)?.CancelApprentice();
				heroRef.ChangeState(Hero.CharacterStates.Dead);
			}
			catch (Exception ex)
			{
				TraceLogger.Write("Homestead", "CleanupOrphanedNotable: threw " + ex.GetType().Name + " for '" + title + "': " + ex.Message);
			}
			heroRef = null;
		}
	}

	public int ConvertNotablesToTown(Settlement town, Settlement castle)
	{
		if (town == null)
		{
			return 0;
		}
		int num = 0;
		num += MoveNotableToTown(ref _masterSmithHero, Occupation.Artisan, town, "MasterSmith");
		num += MoveNotableToTown(ref _marketLadyHero, Occupation.Merchant, town, "MarketLady");
		num += MoveNotableToTown(ref _houndMasterHero, Occupation.Artisan, town, "HoundMaster");
		num += MoveNotableToTown(ref _troubadourHero, Occupation.GangLeader, town, "Troubadour");
		num += MoveNotableToTown(ref _tavernKeeperHero, Occupation.GangLeader, town, "TavernKeeper");
		num += MoveNotableToTown(ref _stableMasterHero, Occupation.ArenaMaster, town, "StableMaster");
		num += MoveNotableToTown(ref _armsMasterHero, Occupation.Artisan, town, "ArmsMaster");
		num += MoveNotableAsGovernor(ref _ambassadorHero, castle ?? town, "Ambassador");
		int num2 = 6;
		int num3 = town.Notables?.Count ?? 0;
		if (num3 < num2)
		{
			Occupation[] array = new Occupation[3]
			{
				Occupation.Artisan,
				Occupation.Merchant,
				Occupation.GangLeader
			};
			for (int i = 0; i < num2 - num3; i++)
			{
				Occupation occ = array[i % array.Length];
				try
				{
					CharacterObject characterObject = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Occupation == occ && c.Culture == town.Culture) ?? CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Occupation == occ);
					if (characterObject != null)
					{
						Hero hero = HeroCreator.CreateSpecialHero(characterObject, town);
						Utils.ApplyRandomPersonalityTraits(hero);
						EnterSettlementAction.ApplyForCharacterOnly(hero, town);
						TraceLogger.Write("Homestead", $"ConvertNotablesToTown: spawned native notable '{hero.Name}' ({occ}).");
					}
				}
				catch (Exception ex)
				{
					TraceLogger.Write("Homestead", "ConvertNotablesToTown: failed spawning native notable: " + ex.Message);
				}
			}
		}
		return num;
	}

	private int MoveNotableAsGovernor(ref Hero hero, Settlement settlement, string role)
	{
		if (hero == null || !hero.IsAlive || settlement?.Town == null)
		{
			hero = null;
			return 0;
		}
		try
		{
			if (HomesteadBehavior.Instance != null && !string.IsNullOrEmpty(role))
			{
				HomesteadBehavior.Instance.ConvertedNotableRoles[hero.StringId] = role;
			}
			if (hero.PartyBelongedTo?.MemberRoster != null && hero.CharacterObject != null)
			{
				hero.PartyBelongedTo.MemberRoster.RemoveTroop(hero.CharacterObject);
			}
			hero.SetNewOccupation(Occupation.Wanderer);
			AddCompanionAction.Apply(Clan.PlayerClan, hero);
			HomesteadBehavior.Instance?.AddAmbassadorCompanionSlot();
			EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
			hero.BornSettlement = settlement;
			hero.UpdateHomeSettlement();
			ChangeGovernorAction.Apply(settlement.Town, hero);
			TraceLogger.Write("Homestead", $"ConvertNotables: '{hero.Name}' → Wanderer companion + governor of '{settlement.Name}'.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "MoveNotableAsGovernor failed for '" + hero?.StringId + "': " + ex.Message);
		}
		hero = null;
		return 1;
	}

	public List<Hero> TakeGraduatedApprenticesForConversion()
	{
		List<Hero> list = new List<Hero>();
		if (_residentHeroes != null)
		{
			foreach (Hero item in _residentHeroes.ToList())
			{
				if (item != null && item.IsAlive)
				{
					list.Add(item);
				}
			}
			_residentHeroes.Clear();
		}
		return list;
	}

	private int MoveNotableToTown(ref Hero hero, Occupation occupation, Settlement town, string role)
	{
		if (hero == null || !hero.IsAlive)
		{
			hero = null;
			return 0;
		}
		try
		{
			if (HomesteadBehavior.Instance != null && !string.IsNullOrEmpty(role))
			{
				HomesteadBehavior.Instance.ConvertedNotableRoles[hero.StringId] = role;
			}
			if (hero.PartyBelongedTo?.MemberRoster != null && hero.CharacterObject != null)
			{
				hero.PartyBelongedTo.MemberRoster.RemoveTroop(hero.CharacterObject);
			}
			hero.SetNewOccupation(occupation);
			hero.BornSettlement = town;
			try
			{
				if (hero.CharacterObject != null)
				{
					Settlement bornSettlement = HomesteadSettlementBuilder.FindUnrelatedVanillaVillage(hero.CharacterObject.Culture) ?? town;
					Hero hero2 = HeroCreator.CreateSpecialHero(hero.CharacterObject, bornSettlement, null, null, 30);
					if (hero2 != null && hero2.Clan != null)
					{
						hero2.Clan.SetLeader(hero);
						KillCharacterAction.ApplyByRemove(hero2);
						try
						{
							AccessTools.Field(typeof(Clan), "_home")?.SetValue(hero2.Clan, town);
							hero.UpdateHomeSettlement();
						}
						catch (Exception ex)
						{
							TraceLogger.Write("Homestead", "MoveNotableToTown: clan home set failed: " + ex.Message);
						}
					}
				}
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("Homestead", "MoveNotableToTown: clan setup failed for '" + hero?.StringId + "': " + ex2.Message);
			}
			EnterSettlementAction.ApplyForCharacterOnly(hero, town);
			ForceSetHomeSettlement(hero, town);
			TraceLogger.Write("Homestead", $"ConvertNotablesToTown: moved '{hero.Name}' ({occupation}) → '{town.Name}' notables={town.Notables?.Count ?? (-1)}.");
		}
		catch (Exception ex3)
		{
			TraceLogger.Write("Homestead", $"ConvertNotablesToTown: failed moving '{hero?.StringId}' ({occupation}): {ex3.Message}\n{ex3.StackTrace}");
		}
		hero = null;
		return 1;
	}

	public void RetireNotablesAndResidents()
	{
		CleanupOrphanedNotable(ref _houndMasterHero, "Hound Master");
		CleanupOrphanedNotable(ref _marketLadyHero, "Market Lady");
		CleanupOrphanedNotable(ref _ambassadorHero, "Ambassador");
		CleanupOrphanedNotable(ref _armsMasterHero, "Arms Master");
		CleanupOrphanedNotable(ref _tavernKeeperHero, "Tavern Keeper");
		CleanupOrphanedNotable(ref _masterSmithHero, "Master Smith");
		CleanupOrphanedNotable(ref _troubadourHero, "Trobairitz");
		if (_residentHeroes != null)
		{
			foreach (Hero item in _residentHeroes.ToList())
			{
				try
				{
					if (item != null && item.IsAlive)
					{
						if (item.CharacterObject != null && item.PartyBelongedTo?.MemberRoster != null)
						{
							item.PartyBelongedTo.MemberRoster.RemoveTroop(item.CharacterObject);
						}
						KillCharacterAction.ApplyByRemove(item, showNotification: false, isForced: false);
					}
				}
				catch (Exception ex)
				{
					TraceLogger.Write("Homestead", "RetireNotablesAndResidents: failed disposing resident '" + item?.StringId + "': " + ex.Message);
				}
			}
			_residentHeroes.Clear();
		}
		HomesteadBehavior.Instance?.GetActiveRaidQuestForHomestead(this)?.CancelRaid();
		HomesteadBehavior.Instance?.GetActiveArmsMasterRecruitQuest(this)?.CancelRecruitment();
		HomesteadBehavior.Instance?.GetActiveMasterSmithRecruitQuest(this)?.CancelRecruitment();
		HomesteadBehavior.Instance?.GetActiveHeadmanTrustQuest(this)?.CancelForTeardown();
		HomesteadBehavior.Instance?.GetActiveAngryVillagersQuest(this)?.CancelForTeardown();
		HomesteadBehavior.Instance?.GetActiveLandPatentQuest(this)?.CancelForTeardown();
		HomesteadBehavior.Instance?.GetActiveSettlementCharterQuest(this)?.CancelForTeardown();
	}

	private void MigrateNotableHeroName(Hero? hero, string title)
	{
		if (hero == null || !hero.IsAlive)
		{
			return;
		}
		string text = hero.Name?.ToString() ?? "";
		if (!text.Contains(" the " + title + " of "))
		{
			string text2 = hero.FirstName?.ToString() ?? "";
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "the " + title;
			}
			TextObject firstName = new TextObject(text2);
			TextObject textObject = new TextObject(text2 + " the " + title + " of " + name);
			hero.SetName(textObject, firstName);
			PatchCharacterObjectName(hero.CharacterObject, textObject);
			TraceLogger.Write("Homestead", $"MigrateNotableHeroName: renamed '{text}' → '{textObject}' for '{name}'.");
		}
	}

	internal static void PatchCharacterObjectName(CharacterObject? charObj, TextObject newName)
	{
		if (charObj == null)
		{
			return;
		}
		try
		{
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			FieldInfo fieldInfo = null;
			string[] array = new string[5] { "_basicName", "_name", "<Name>k__BackingField", "name", "_nameText" };
			foreach (string text in array)
			{
				fieldInfo = typeof(CharacterObject).GetField(text, bindingAttr) ?? typeof(BasicCharacterObject).GetField(text, bindingAttr);
				if (fieldInfo?.FieldType == typeof(TextObject))
				{
					break;
				}
				fieldInfo = null;
			}
			if (fieldInfo == null)
			{
				fieldInfo = typeof(CharacterObject).GetFields(bindingAttr).Concat(typeof(BasicCharacterObject).GetFields(bindingAttr)).FirstOrDefault((FieldInfo f) => f.FieldType == typeof(TextObject));
			}
			if (fieldInfo != null)
			{
				fieldInfo.SetValue(charObj, newName);
				TraceLogger.Write("Homestead", $"PatchCharacterObjectName: set '{fieldInfo.DeclaringType?.Name}.{fieldInfo.Name}' → '{newName}'");
			}
			else
			{
				TraceLogger.Write("Homestead", "PatchCharacterObjectName: could not find TextObject name backing field — agent will show NameGenerator title.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "PatchCharacterObjectName: threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	internal static void ForceSetHomeSettlement(Hero? hero, Settlement? settlement)
	{
		if (hero == null || settlement == null)
		{
			return;
		}
		try
		{
			typeof(Hero).GetField("_homeSettlement", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(hero, settlement);
			settlement.GetType().GetMethod("CollectNotablesToCache", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(settlement, null);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "ForceSetHomeSettlement: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void ForceDetachHeroFromSettlement(Hero? hero, Settlement? settlement)
	{
		if (hero == null)
		{
			return;
		}
		try
		{
			if (hero.CurrentSettlement != null)
			{
				LeaveSettlementAction.ApplyForCharacterOnly(hero);
			}
			typeof(Hero).GetField("_homeSettlement", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(hero, null);
			settlement?.GetType().GetMethod("CollectNotablesToCache", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(settlement, null);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "ForceDetachHeroFromSettlement: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void DailyTickEnsureMarketLadyHero()
	{
		MigrateNotableHeroName(_marketLadyHero, "Market Lady");
		TryEnsureMarketLadyHero();
	}

	internal void TryEnsureMarketLadyHero()
	{
		if ((_marketLadyHero != null && _marketLadyHero.IsAlive) || !HasMarket)
		{
			return;
		}
		try
		{
			CultureObject playerCulture = Hero.MainHero?.Culture;
			CharacterObject characterObject = null;
			if (playerCulture != null)
			{
				characterObject = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.IsFemale && c.Culture == playerCulture && c.Occupation == Occupation.RuralNotable);
			}
			if (characterObject == null)
			{
				characterObject = Game.Current?.ObjectManager?.GetObject<CharacterObject>("homestead_market_lady_template");
			}
			if (characterObject == null)
			{
				characterObject = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.IsFemale && c.Occupation == Occupation.RuralNotable);
			}
			if (characterObject == null)
			{
				characterObject = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Occupation == Occupation.RuralNotable);
			}
			TraceLogger.Write("Homestead", "TryEnsureMarketLadyHero: template → " + ((characterObject == null) ? "NOT FOUND" : $"'{characterObject.StringId}' culture={characterObject.Culture?.StringId} occupation={characterObject.Occupation}"));
			if (characterObject == null)
			{
				return;
			}
			Settlement settlement = HomesteadSettlementBuilder.FindUnrelatedVanillaVillage(characterObject.Culture) ?? Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage);
			TraceLogger.Write("Homestead", $"TryEnsureMarketLadyHero: dummy settlement '{settlement?.Name}' (culture '{settlement?.Culture?.StringId}')");
			if (settlement == null)
			{
				TraceLogger.Write("Homestead", "TryEnsureMarketLadyHero: no village found — skipping hero creation.");
				return;
			}
			Hero hero = HeroCreator.CreateSpecialHero(characterObject, settlement, null, null, 28 + MBRandom.RandomInt(15));
			Utils.ApplyRandomPersonalityTraits(hero);
			ForceDetachHeroFromSettlement(hero, settlement);
			if (!characterObject.IsFemale)
			{
				try
				{
					hero.IsFemale = true;
					BodyProperties randomBodyProperties = BodyProperties.GetRandomBodyProperties(characterObject.Race, isFemale: true, characterObject.GetBodyPropertiesMin(returnBaseValue: true), characterObject.GetBodyPropertiesMax(returnBaseValue: true), 0, MBRandom.RandomInt(), characterObject.BodyPropertyRange.HairTags, characterObject.BodyPropertyRange.BeardTags, characterObject.BodyPropertyRange.TattooTags);
					hero.StaticBodyProperties = randomBodyProperties.StaticProperties;
					hero.Weight = randomBodyProperties.DynamicProperties.Weight;
					hero.Build = randomBodyProperties.DynamicProperties.Build;
					TraceLogger.Write("Homestead", "TryEnsureMarketLadyHero: template was male — forced hero.IsFemale=true and regenerated body properties.");
				}
				catch (Exception ex)
				{
					TraceLogger.Write("Homestead", "TryEnsureMarketLadyHero: gender-force failed — " + ex.GetType().Name + ": " + ex.Message);
				}
			}
			string text = hero.FirstName?.ToString() ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "the Market Lady";
			}
			TextObject firstName = new TextObject(text);
			TextObject textObject = new TextObject(text + " the Market Lady of " + name);
			hero.SetName(textObject, firstName);
			PatchCharacterObjectName(hero.CharacterObject, textObject);
			Equipment randomBattleEquipment = characterObject.RandomBattleEquipment;
			if (randomBattleEquipment != null)
			{
				EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, randomBattleEquipment);
			}
			Equipment randomCivilianEquipment = characterObject.RandomCivilianEquipment;
			if (randomCivilianEquipment != null)
			{
				hero.CivilianEquipment.FillFrom(randomCivilianEquipment, useSourceEquipmentType: false);
			}
			hero.AddPower(200f);
			hero.ChangeState(Hero.CharacterStates.Active);
			_marketLadyHero = hero;
			TraceLogger.Write("Homestead", $"TryEnsureMarketLadyHero: created hero '{hero.Name}' charObjName='{hero.CharacterObject?.Name}' ({hero.StringId}) for '{name}'.");
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("Homestead", "TryEnsureMarketLadyHero: FAILED — " + ex2.GetType().Name + ": " + ex2.Message + "\n" + ex2.StackTrace);
		}
	}

	private void DailyTickEnsureAmbassadorHero()
	{
		MigrateNotableHeroName(_ambassadorHero, "Ambassador");
		TryEnsureAmbassadorHero();
	}

	internal void TryEnsureAmbassadorHero()
	{
		if ((_ambassadorHero != null && _ambassadorHero.IsAlive) || !HasAmbassadorHall)
		{
			return;
		}
		try
		{
			CultureObject playerCulture = Hero.MainHero?.Culture;
			CharacterObject template = null;
			if (playerCulture != null)
			{
				template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Culture == playerCulture && c.Occupation == Occupation.Merchant);
			}
			if (template == null)
			{
				template = Game.Current?.ObjectManager?.GetObject<CharacterObject>("homestead_ambassador_template");
			}
			if (template == null)
			{
				template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Occupation == Occupation.RuralNotable);
			}
			TraceLogger.Write("Homestead", "TryEnsureAmbassadorHero: template → " + ((template == null) ? "NOT FOUND" : $"'{template.StringId}' culture={template.Culture?.StringId} occupation={template.Occupation}"));
			if (template == null)
			{
				return;
			}
			Settlement settlement = Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage && s.Culture == template.Culture) ?? Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage);
			if (settlement == null)
			{
				TraceLogger.Write("Homestead", "TryEnsureAmbassadorHero: no village found — skipping.");
				return;
			}
			Hero hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 35 + MBRandom.RandomInt(15));
			Utils.ApplyRandomPersonalityTraits(hero);
			ForceDetachHeroFromSettlement(hero, settlement);
			string value = hero.FirstName?.ToString() ?? "";
			TextObject textObject = new TextObject(value);
			TextObject textObject2;
			if (string.IsNullOrWhiteSpace(value))
			{
				textObject2 = new TextObject("{=homestead_ambassador_name_no_first}Ambassador of {HOMESTEAD_NAME}");
				textObject2.SetTextVariable("HOMESTEAD_NAME", new TextObject(name));
				textObject = new TextObject("{=homestead_ambassador_fallback_first}the Ambassador");
			}
			else
			{
				textObject2 = new TextObject("{=homestead_ambassador_name}{FIRST_NAME} the Ambassador of {HOMESTEAD_NAME}");
				textObject2.SetTextVariable("FIRST_NAME", textObject);
				textObject2.SetTextVariable("HOMESTEAD_NAME", new TextObject(name));
			}
			hero.SetName(textObject2, textObject);
			PatchCharacterObjectName(hero.CharacterObject, textObject2);
			Equipment randomBattleEquipment = template.RandomBattleEquipment;
			if (randomBattleEquipment != null)
			{
				EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, randomBattleEquipment);
			}
			Equipment randomCivilianEquipment = template.RandomCivilianEquipment;
			if (randomCivilianEquipment != null)
			{
				hero.CivilianEquipment.FillFrom(randomCivilianEquipment, useSourceEquipmentType: false);
			}
			hero.AddPower(200f);
			hero.ChangeState(Hero.CharacterStates.Active);
			_ambassadorHero = hero;
			TraceLogger.Write("Homestead", $"TryEnsureAmbassadorHero: created hero '{hero.Name}' ({hero.StringId}) for '{name}'.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "TryEnsureAmbassadorHero: FAILED — " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	private void DailyTickEnsureArmsMasterHero()
	{
		MigrateNotableHeroName(_armsMasterHero, "Arms Master");
	}

	public void CreateArmsMasterHero(BodyProperties? templateBody = null, int templateAge = -1)
	{
		if (_armsMasterHero != null && _armsMasterHero.IsAlive)
		{
			TraceLogger.Write("Homestead", "CreateArmsMasterHero: Arms Master already alive — skipping.");
			return;
		}
		if (!HasTrainingFieldBuilding)
		{
			TraceLogger.Write("Homestead", "CreateArmsMasterHero: no Training Field — skipping.");
			return;
		}
		try
		{
			CultureObject playerCulture = Hero.MainHero?.Culture;
			CharacterObject template = null;
			if (playerCulture != null)
			{
				template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Culture == playerCulture && c.Occupation == Occupation.Wanderer);
			}
			if (template == null)
			{
				template = Game.Current?.ObjectManager?.GetObject<CharacterObject>("homestead_arms_master_template");
			}
			if (template == null)
			{
				template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Occupation == Occupation.Wanderer);
			}
			if (template == null)
			{
				template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Occupation == Occupation.RuralNotable);
			}
			TraceLogger.Write("Homestead", "CreateArmsMasterHero: template → " + ((template == null) ? "NOT FOUND" : $"'{template.StringId}' culture={template.Culture?.StringId} occupation={template.Occupation}"));
			if (template == null)
			{
				return;
			}
			Settlement settlement = Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage && s.Culture == template.Culture) ?? Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage);
			TraceLogger.Write("Homestead", $"CreateArmsMasterHero: dummy settlement '{settlement?.Name}' (culture '{settlement?.Culture?.StringId}')");
			if (settlement == null)
			{
				TraceLogger.Write("Homestead", "CreateArmsMasterHero: no village found — skipping.");
				return;
			}
			int age = ((templateAge > 0) ? templateAge : (35 + MBRandom.RandomInt(15)));
			Hero hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, age);
			Utils.ApplyRandomPersonalityTraits(hero);
			ForceDetachHeroFromSettlement(hero, settlement);
			if (templateBody.HasValue)
			{
				hero.StaticBodyProperties = templateBody.Value.StaticProperties;
				hero.Weight = templateBody.Value.Weight;
				hero.Build = templateBody.Value.Build;
			}
			string text = hero.FirstName?.ToString() ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "the Arms Master";
			}
			TextObject firstName = new TextObject(text);
			TextObject textObject = new TextObject(text + " the Arms Master of " + name);
			hero.SetName(textObject, firstName);
			PatchCharacterObjectName(hero.CharacterObject, textObject);
			Equipment randomBattleEquipment = template.RandomBattleEquipment;
			if (randomBattleEquipment != null)
			{
				EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, randomBattleEquipment);
				hero.CivilianEquipment.FillFrom(randomBattleEquipment, useSourceEquipmentType: false);
			}
			hero.AddPower(200f);
			hero.ChangeState(Hero.CharacterStates.Active);
			_armsMasterHero = hero;
			TraceLogger.Write("Homestead", $"CreateArmsMasterHero: created hero '{hero.Name}' ({hero.StringId}) for '{name}'.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "CreateArmsMasterHero: FAILED — " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	private void DailyTickEnsureTavernKeeperHero()
	{
		MigrateNotableHeroName(_tavernKeeperHero, "Tavern Keeper");
		TryEnsureTavernKeeperHero();
	}

	internal void TryEnsureTavernKeeperHero()
	{
		if ((_tavernKeeperHero != null && _tavernKeeperHero.IsAlive) || !HasTavernBuilding)
		{
			return;
		}
		try
		{
			CultureObject playerCulture = Hero.MainHero?.Culture;
			CharacterObject template = null;
			if (playerCulture != null)
			{
				template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Culture == playerCulture && c.Occupation == Occupation.Merchant);
			}
			if (template == null)
			{
				template = Game.Current?.ObjectManager?.GetObject<CharacterObject>("homestead_tavern_keeper_template");
			}
			if (template == null)
			{
				template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Occupation == Occupation.Merchant);
			}
			if (template == null)
			{
				template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Occupation == Occupation.RuralNotable);
			}
			if (template == null)
			{
				TraceLogger.Write("Homestead", "TryEnsureTavernKeeperHero: no template found — aborting.");
				return;
			}
			Settlement settlement = Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage && s.Culture == template.Culture) ?? Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage);
			if (settlement != null)
			{
				Hero hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 30 + MBRandom.RandomInt(20));
				Utils.ApplyRandomPersonalityTraits(hero);
				ForceDetachHeroFromSettlement(hero, settlement);
				string text = hero.FirstName?.ToString() ?? "the Tavern Keeper";
				hero.SetName(new TextObject(text + " the Tavern Keeper of " + name), new TextObject(text));
				PatchCharacterObjectName(hero.CharacterObject, new TextObject(text + " the Tavern Keeper of " + name));
				Equipment randomBattleEquipment = template.RandomBattleEquipment;
				if (randomBattleEquipment != null)
				{
					EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, randomBattleEquipment);
				}
				Equipment randomCivilianEquipment = template.RandomCivilianEquipment;
				if (randomCivilianEquipment != null)
				{
					hero.CivilianEquipment.FillFrom(randomCivilianEquipment, useSourceEquipmentType: false);
				}
				hero.AddPower(200f);
				hero.ChangeState(Hero.CharacterStates.Active);
				_tavernKeeperHero = hero;
				TraceLogger.Write("Homestead", $"TryEnsureTavernKeeperHero: created '{hero.Name}' for '{name}'.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "TryEnsureTavernKeeperHero: FAILED — " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void DailyTickEnsureMasterSmithHero()
	{
		MigrateNotableHeroName(_masterSmithHero, "Master Smith");
		TryEnsureMasterSmithHero();
	}

	internal void TryEnsureMasterSmithHero(BodyProperties? templateBody = null, int templateAge = -1)
	{
		if ((_masterSmithHero != null && _masterSmithHero.IsAlive) || !HasSmithy || !MasterSmithRecruited)
		{
			return;
		}
		try
		{
			CultureObject playerCulture = Hero.MainHero?.Culture;
			CharacterObject template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Culture == playerCulture && c.Occupation == Occupation.Blacksmith) ?? playerCulture?.Blacksmith ?? CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Occupation == Occupation.Blacksmith) ?? CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Occupation == Occupation.Merchant);
			if (template == null)
			{
				TraceLogger.Write("Homestead", "TryEnsureMasterSmithHero: no blacksmith template found — aborting.");
				return;
			}
			Settlement settlement = Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage && s.Culture == template.Culture) ?? Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage);
			if (settlement != null)
			{
				int age = ((templateAge > 0) ? templateAge : (30 + MBRandom.RandomInt(20)));
				Hero hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, age);
				Utils.ApplyRandomPersonalityTraits(hero);
				hero.SetNewOccupation(Occupation.RuralNotable);
				ForceDetachHeroFromSettlement(hero, settlement);
				if (templateBody.HasValue)
				{
					hero.StaticBodyProperties = templateBody.Value.StaticProperties;
					hero.Weight = templateBody.Value.Weight;
					hero.Build = templateBody.Value.Build;
				}
				TextObject textObject = hero.FirstName ?? new TextObject("{=homestead_master_smith_fallback}the Master Smith");
				TextObject textObject2 = new TextObject("{=homestead_master_smith_name}{FIRST_NAME} the Master Smith of {HOMESTEAD_NAME}");
				textObject2.SetTextVariable("FIRST_NAME", textObject);
				textObject2.SetTextVariable("HOMESTEAD_NAME", name);
				hero.SetName(textObject2, textObject);
				PatchCharacterObjectName(hero.CharacterObject, textObject2);
				Equipment equipment = template.RandomCivilianEquipment ?? template.FirstCivilianEquipment;
				if (equipment != null)
				{
					EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, equipment);
					hero.CivilianEquipment.FillFrom(equipment, useSourceEquipmentType: false);
				}
				hero.AddPower(200f);
				hero.ChangeState(Hero.CharacterStates.Active);
				_masterSmithHero = hero;
				TraceLogger.Write("Homestead", $"TryEnsureMasterSmithHero: created '{hero.Name}' for '{name}'.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "TryEnsureMasterSmithHero: FAILED — " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void DailyTickEnsureTroubadourHero()
	{
		MigrateNotableHeroName(_troubadourHero, "Trobairitz");
		TryEnsureTroubadourHero();
	}

	internal void TryEnsureTroubadourHero()
	{
		if ((_troubadourHero != null && _troubadourHero.IsAlive) || !HasTavernBuilding)
		{
			return;
		}
		try
		{
			CultureObject playerCulture = Hero.MainHero?.Culture;
			CharacterObject template = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.IsFemale && c.Culture == playerCulture && c.Occupation == Occupation.Wanderer) ?? CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.IsFemale && c.Culture == playerCulture) ?? CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.IsFemale) ?? CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Culture == playerCulture && c.Occupation == Occupation.Wanderer) ?? CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Culture == playerCulture) ?? CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Occupation == Occupation.Wanderer);
			if (template == null)
			{
				TraceLogger.Write("Homestead", "TryEnsureTroubadourHero: no usable template found at all — aborting.");
				return;
			}
			bool isFemale = template.IsFemale;
			Settlement settlement = Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage && s.Culture == template.Culture) ?? Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage);
			if (settlement != null)
			{
				Hero hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 20 + MBRandom.RandomInt(15));
				Utils.ApplyRandomPersonalityTraits(hero);
				hero.SetNewOccupation(Occupation.RuralNotable);
				ForceDetachHeroFromSettlement(hero, settlement);
				string text = (isFemale ? "Trobairitz" : "Troubadour");
				string text2 = (isFemale ? "the Trobairitz" : "the Troubadour");
				string text3 = hero.FirstName?.ToString() ?? text2;
				hero.SetName(new TextObject(text3 + " the " + text + " of " + name), new TextObject(text3));
				PatchCharacterObjectName(hero.CharacterObject, new TextObject(text3 + " the " + text + " of " + name));
				CultureObject cultureObject = playerCulture ?? template.Culture;
				Equipment equipment = ((!isFemale) ? template.RandomCivilianEquipment : (cultureObject?.FemaleDancer?.RandomCivilianEquipment ?? template.RandomCivilianEquipment));
				if (equipment != null)
				{
					hero.CivilianEquipment.FillFrom(equipment, useSourceEquipmentType: false);
				}
				hero.ChangeState(Hero.CharacterStates.Active);
				_troubadourHero = hero;
				TraceLogger.Write("Homestead", $"TryEnsureTroubadourHero: created '{hero.Name}' for '{name}'.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", "TryEnsureTroubadourHero: FAILED — " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	internal void TryEnsureStableMasterHero()
	{
		if ((_stableMasterHero != null && _stableMasterHero.IsAlive) || !HasStable || !StableMasterRecruited)
		{
			return;
		}
		try
		{
			CultureObject playerCulture = Hero.MainHero?.Culture;
			CharacterObject characterObject = null;
			if (playerCulture != null)
			{
				characterObject = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.IsFemale && c.Culture == playerCulture && c.Occupation == Occupation.RuralNotable);
			}
			if (characterObject == null)
			{
				characterObject = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.IsFemale && c.Occupation == Occupation.RuralNotable);
			}
			if (characterObject == null)
			{
				characterObject = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Occupation == Occupation.RuralNotable);
			}
			if (characterObject == null)
			{
				return;
			}
			Settlement settlement = HomesteadSettlementBuilder.FindUnrelatedVanillaVillage(characterObject.Culture) ?? Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage);
			if (settlement == null)
			{
				return;
			}
			Hero hero = HeroCreator.CreateSpecialHero(characterObject, settlement, null, null, 28 + MBRandom.RandomInt(15));
			Utils.ApplyRandomPersonalityTraits(hero);
			ForceDetachHeroFromSettlement(hero, settlement);
			if (!characterObject.IsFemale)
			{
				try
				{
					hero.IsFemale = true;
					BodyProperties randomBodyProperties = BodyProperties.GetRandomBodyProperties(characterObject.Race, isFemale: true, characterObject.GetBodyPropertiesMin(returnBaseValue: true), characterObject.GetBodyPropertiesMax(returnBaseValue: true), 0, MBRandom.RandomInt(), characterObject.BodyPropertyRange.HairTags, characterObject.BodyPropertyRange.BeardTags, characterObject.BodyPropertyRange.TattooTags);
					hero.StaticBodyProperties = randomBodyProperties.StaticProperties;
					hero.Weight = randomBodyProperties.DynamicProperties.Weight;
					hero.Build = randomBodyProperties.DynamicProperties.Build;
					TraceLogger.Write("Homestead", "TryEnsureStableMasterHero: template was male — forced hero.IsFemale=true and regenerated body properties.");
				}
				catch (Exception ex)
				{
					TraceLogger.Write("Homestead", "TryEnsureStableMasterHero: gender-force failed — " + ex.GetType().Name + ": " + ex.Message);
				}
			}
			string text = hero.FirstName?.ToString() ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "the Stablemistress";
			}
			TextObject firstName = new TextObject(text);
			TextObject textObject = new TextObject(text + " the Stablemistress of " + name);
			hero.SetName(textObject, firstName);
			PatchCharacterObjectName(hero.CharacterObject, textObject);
			Equipment equipment = characterObject.RandomCivilianEquipment ?? characterObject.FirstCivilianEquipment;
			if (equipment != null)
			{
				EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, equipment);
				hero.CivilianEquipment.FillFrom(equipment, useSourceEquipmentType: false);
			}
			hero.HeroDeveloper.AddSkillXp(DefaultSkills.Riding, 50000f);
			hero.AddPower(200f);
			hero.ChangeState(Hero.CharacterStates.Active);
			_stableMasterHero = hero;
			TraceLogger.Write("Homestead", $"TryEnsureStableMasterHero: created '{hero.Name}' for '{name}'.");
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("Homestead", "TryEnsureStableMasterHero: FAILED - " + ex2.GetType().Name + ": " + ex2.Message);
		}
	}

	private void DailyTickEnsureStableMasterHero()
	{
		MigrateNotableHeroName(_stableMasterHero, "Stablemistress");
		TryEnsureStableMasterHero();
	}

	private static StableHorseKind? ClassifyStableHorse(ItemObject item)
	{
		if (item == null || !item.HasHorseComponent)
		{
			return null;
		}
		ItemCategory itemCategory = item.ItemCategory;
		if (itemCategory == DefaultItemCategories.NobleHorse)
		{
			return StableHorseKind.Noble;
		}
		if (itemCategory == DefaultItemCategories.WarHorse)
		{
			return StableHorseKind.War;
		}
		if (itemCategory == DefaultItemCategories.PackAnimal)
		{
			return StableHorseKind.Pack;
		}
		if (itemCategory == DefaultItemCategories.Horse)
		{
			return StableHorseKind.Riding;
		}
		if (!item.HorseComponent.IsRideable)
		{
			return null;
		}
		return StableHorseKind.Riding;
	}

	private void AddRandomHorseOfKind(StableHorseKind kind, Dictionary<string, int> producedItems)
	{
		try
		{
			if (Stash != null && GetStashTotalItemCount() < GetStashCapacity())
			{
				List<ItemObject> list = (from it in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>()
					where it != null && !it.NotMerchandise && !it.StringId.Contains("_load_") && ClassifyStableHorse(it) == kind
					select it).ToList();
				if (list.Count != 0)
				{
					ItemObject randomElementInefficiently = list.GetRandomElementInefficiently();
					Stash.AddToCounts(randomElementInefficiently, 1);
					string key = randomElementInefficiently.Name?.ToString() ?? randomElementInefficiently.StringId;
					producedItems[key] = (producedItems.TryGetValue(key, out var value) ? value : 0) + 1;
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", $"AddRandomHorseOfKind({kind}) failed: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private void ProduceStableMasterHorses(Dictionary<string, int> producedItems)
	{
		if (HasStable && StableMasterRecruited)
		{
			if (MBRandom.RandomFloat < 0.5f)
			{
				AddRandomHorseOfKind(StableHorseKind.Riding, producedItems);
			}
			if (MBRandom.RandomFloat < 0.3f)
			{
				AddRandomHorseOfKind(StableHorseKind.War, producedItems);
			}
			HomesteadBehavior instance = HomesteadBehavior.Instance;
			if (((instance != null && instance.HasStableMasterMasteryUnlocked) || StableMasterMasteryUnlocked) && MBRandom.RandomFloat < 0.1f)
			{
				AddRandomHorseOfKind(StableHorseKind.Noble, producedItems);
			}
		}
	}

	public void HourlyTickTavern()
	{
		if (!HasTavernBuilding || _tavernKeeperHero == null || !_tavernKeeperHero.IsAlive)
		{
			return;
		}
		MobileParty mobileParty = base.MobileParty;
		if (mobileParty == null)
		{
			return;
		}
		Vec2 getPosition2D = mobileParty.GetPosition2D;
		if (_tavernNearbyVillagerIds == null)
		{
			_tavernNearbyVillagerIds = new HashSet<string>();
		}
		HashSet<string> hashSet = new HashSet<string>();
		int num = 0;
		foreach (MobileParty item in MobileParty.All)
		{
			if (item.IsVillager && item.IsActive && !((item.GetPosition2D - getPosition2D).Length > 15f))
			{
				hashSet.Add(item.StringId);
				if (!_tavernNearbyVillagerIds.Contains(item.StringId))
				{
					int num2 = Math.Max(5, Math.Min(item.MemberRoster.TotalManCount * 3, 40));
					num += num2;
				}
			}
		}
		if (num > 0)
		{
			GoldStored += num;
			_tavernTariffAccruedToday += num;
			TraceLogger.Write("Homestead", $"Tavern tariff: +{num}g from {hashSet.Except(_tavernNearbyVillagerIds).Count()} new villager parties at '{Name}'.");
			if (GlobalSettings<MCMSettings>.Instance.ShowDailyProductionNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_tavern_tariff_collected", "Tavern at {HOMESTEAD_NAME} collected {TARIFF} gold from passing villagers.", 255f, 215f, 0f, ("HOMESTEAD_NAME", name), ("TARIFF", num.ToString()));
			}
		}
		_tavernNearbyVillagerIds = hashSet;
	}

	private void DailyTickAmbassadorRelations()
	{
		if (!HasAmbassadorHall || _ambassadorHero == null || !_ambassadorHero.IsAlive)
		{
			return;
		}
		Hero mainHero = Hero.MainHero;
		MobileParty mobileParty = base.MobileParty;
		if (mainHero == null || mobileParty == null)
		{
			return;
		}
		Vec2 getPosition2D = mobileParty.GetPosition2D;
		Clan clan = mainHero.Clan;
		List<Hero> list = new List<Hero>();
		foreach (Settlement settlement in Campaign.Current.Settlements)
		{
			if (settlement == null || settlement.GetPosition2D.Distance(getPosition2D) > 20f)
			{
				continue;
			}
			foreach (Hero notable in settlement.Notables)
			{
				if (notable != null && notable.IsAlive && notable != mainHero && (notable.Clan == null || notable.Clan != clan))
				{
					list.Add(notable);
				}
			}
		}
		if (list.Count != 0)
		{
			Hero target = list[MBRandom.RandomInt(list.Count)];
			ApplyAmbassadorRelationImprovement(mainHero, target, "daily (notable)");
		}
	}

	public void HourlyTickAmbassador()
	{
		if (!HasAmbassadorHall || _ambassadorHero == null || !_ambassadorHero.IsAlive)
		{
			return;
		}
		Hero mainHero = Hero.MainHero;
		MobileParty mobileParty = base.MobileParty;
		if (mainHero == null || mobileParty == null)
		{
			return;
		}
		Vec2 getPosition2D = mobileParty.GetPosition2D;
		Clan clan = mainHero.Clan;
		if (_ambassadorNearbyPartyIds == null)
		{
			_ambassadorNearbyPartyIds = new HashSet<string>();
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (MobileParty mobileParty2 in Campaign.Current.MobileParties)
		{
			if (mobileParty2 == null || !mobileParty2.IsActive || mobileParty2.IsMainParty || mobileParty2.GetPosition2D.Distance(getPosition2D) > 20f)
			{
				continue;
			}
			Hero hero = null;
			Hero leaderHero = mobileParty2.LeaderHero;
			if (leaderHero != null && leaderHero.IsAlive && leaderHero != mainHero && leaderHero.IsLord && leaderHero.Clan != clan)
			{
				hero = leaderHero;
			}
			if (mobileParty2.PartyComponent is CaravanPartyComponent { Owner: { IsAlive: not false } owner } && owner != mainHero && owner.Clan != clan)
			{
				hero = owner;
			}
			if (hero != null)
			{
				hashSet.Add(mobileParty2.StringId);
				if (!_ambassadorNearbyPartyIds.Contains(mobileParty2.StringId) && !(MBRandom.RandomFloat >= 0.25f))
				{
					ApplyAmbassadorRelationImprovement(mainHero, hero, "hourly (lord/caravan)");
				}
			}
		}
		_ambassadorNearbyPartyIds = hashSet;
	}

	private void ApplyAmbassadorRelationImprovement(Hero player, Hero target, string source)
	{
		ChangeRelationAction.ApplyRelationChangeBetweenHeroes(player, target, 2, showQuickNotification: false);
		int num = 25 * (Tier + 1);
		player.AddSkillXp(DefaultSkills.Charm, num);
		TraceLogger.Write("Homestead", $"AmbassadorRelations ({source}): '{name}' improved '{player.Name}' ↔ '{target.Name}' by +2, +{num} Charm XP.");
		MCMSettings? instance = GlobalSettings<MCMSettings>.Instance;
		if (instance != null && instance.ShowNpcXpNotifications)
		{
			Utils.PrintLocalizedMessage("homestead_ambassador_relations_improved", "The Ambassador at {HOMESTEAD_NAME} improved your relations with {TARGET_NAME} (+2).", 160f, 200f, 240f, ("HOMESTEAD_NAME", name), ("TARGET_NAME", target.Name?.ToString() ?? "?"));
		}
	}

	private void DailyTickAmbassadorCharmXp()
	{
		if (!HasAmbassadorHall || leader == null || leader.IsHumanPlayerCharacter)
		{
			return;
		}
		int num = 25 * (Tier + 1);
		leader.AddSkillXp(DefaultSkills.Charm, num);
		int num2 = Math.Max(1, (int)Math.Round((float)num * (1f / 3f)));
		int num3 = 0;
		if (base.MobileParty?.MemberRoster != null)
		{
			foreach (TroopRosterElement item in base.MobileParty.MemberRoster.GetTroopRoster().ToList())
			{
				CharacterObject character = item.Character;
				if (character == null || !character.IsHero)
				{
					continue;
				}
				Hero heroObject = character.HeroObject;
				if (heroObject != null && heroObject != leader && !heroObject.IsHumanPlayerCharacter && heroObject.PartyBelongedTo == base.MobileParty)
				{
					HeroDeveloper heroDeveloper = heroObject.HeroDeveloper;
					if (heroDeveloper != null)
					{
						heroDeveloper.AddSkillXp(DefaultSkills.Charm, num2, isAffectedByFocusFactor: true, shouldNotify: false);
						num3++;
					}
				}
			}
		}
		TraceLogger.Write("Homestead", $"Ambassador Charm XP for '{Name}': leader +{num} Charm, supportingHeroes={num3} each +{num2}.");
		if (num3 > 0 && GlobalSettings<MCMSettings>.Instance.ShowNpcXpNotifications)
		{
			Utils.PrintLocalizedMessage("homestead_ambassador_charm_xp", "{SUPPORTING_HERO_COUNT} companion(s) gained {CHARM_XP} Charm XP from the Ambassador's Hall at {HOMESTEAD_NAME}.", 100f, 180f, 220f, ("SUPPORTING_HERO_COUNT", num3.ToString()), ("CHARM_XP", num2.ToString()), ("HOMESTEAD_NAME", Name.ToString()));
		}
	}

	private void DailyTickMarketTradeXp()
	{
		if (!HasMarket || leader == null)
		{
			return;
		}
		int num = 25 * (Tier + 1);
		leader.AddSkillXp(DefaultSkills.Trade, num);
		int num2 = Math.Max(1, (int)Math.Round((float)num * (1f / 3f)));
		int num3 = 0;
		if (base.MobileParty?.MemberRoster != null)
		{
			foreach (TroopRosterElement item in base.MobileParty.MemberRoster.GetTroopRoster().ToList())
			{
				CharacterObject character = item.Character;
				if (character == null || !character.IsHero)
				{
					continue;
				}
				Hero heroObject = character.HeroObject;
				if (heroObject != null && heroObject != leader && !heroObject.IsHumanPlayerCharacter && heroObject.PartyBelongedTo == base.MobileParty)
				{
					HeroDeveloper heroDeveloper = heroObject.HeroDeveloper;
					if (heroDeveloper != null)
					{
						heroDeveloper.AddSkillXp(DefaultSkills.Trade, num2, isAffectedByFocusFactor: true, shouldNotify: false);
						num3++;
					}
				}
			}
		}
		TraceLogger.Write("Homestead", $"Market Trade XP for '{Name}': leader +{num} Trade, supportingHeroes={num3} each +{num2}.");
		if (num3 > 0 && GlobalSettings<MCMSettings>.Instance.ShowNpcXpNotifications)
		{
			Utils.PrintLocalizedMessage("homestead_market_trade_xp", "{SUPPORTING_HERO_COUNT} companion(s) gained {TRADE_XP} Trade XP from the market at {HOMESTEAD_NAME}.", 100f, 200f, 180f, ("SUPPORTING_HERO_COUNT", num3.ToString()), ("TRADE_XP", num2.ToString()), ("HOMESTEAD_NAME", Name.ToString()));
		}
	}

	private void RefreshMercenaries()
	{
		if (HasTavernBuilding)
		{
			string[] array = new string[9] { "mercenary_1", "mercenary_2", "mercenary_3", "mercenary_4", "mercenary_5", "mercenary_6", "mercenary_7", "mercenary_8", "mercenary_9" };
			if (MBRandom.RandomFloat < 0.25f)
			{
				AvailableMercenaryCount = 0;
				AvailableMercenaryTypeId = "";
			}
			else
			{
				AvailableMercenaryTypeId = array[MBRandom.RandomInt(array.Length)];
				AvailableMercenaryCount = MBRandom.RandomInt(2, 8);
			}
		}
		else
		{
			AvailableMercenaryCount = 0;
			AvailableMercenaryTypeId = "";
		}
	}

	private void UpdateTierProgress()
	{
		if (leader != null && (Tier < 3 || !SettlementUpgradeReady))
		{
			int skillValue = leader.GetSkillValue(DefaultSkills.Steward);
			int skillValue2 = leader.GetSkillValue(DefaultSkills.Engineering);
			float num = (float)(skillValue + skillValue2) / 2f;
			float num2 = Math.Max(0f, Math.Min(1f, num / 100f));
			float num3;
			float num4;
			switch (Tier)
			{
			case 0:
				num3 = 0.002f;
				num4 = 1f;
				break;
			case 1:
				num3 = 0.0006f;
				num4 = 0.25f;
				break;
			default:
				num3 = 0.0005f;
				num4 = 0f;
				break;
			}
			float num5 = 1f + num4 * num2;
			TierProgress += num3 * (float)Troops.TotalRegulars * num5;
			if (TierProgress >= 1f)
			{
				OnTierProgressComplete();
			}
		}
	}

	internal bool FieldKitchenAffectsVillage(Settlement village)
	{
		if (village == null || !village.IsVillage || base.MobileParty == null || !HasFieldKitchen)
		{
			return false;
		}
		return village.GetPosition2D.Distance(base.MobileParty.GetPosition2D) <= 35f;
	}

	public void ApplyCustomMapIcon(string reason)
	{
		SyncStandaloneMapIcon(reason);
	}

	public void SyncStandaloneMapIcon(string reason)
	{
		MapScreen instance = MapScreen.Instance;
		if (((instance != null) ? instance.MapScene : null) == null || base.Party == null || IsMoving)
		{
			DestroyStandaloneMapIcon();
			return;
		}
		EnsureHomesteadPartyClickProxyVisible();
		if (_standaloneMapIcon != null)
		{
			if (_standaloneMapIconTier == Tier)
			{
				MatrixFrame frame = _standaloneMapIcon.GetFrame();
				float height = 0f;
				CampaignVec2 point = new CampaignVec2(base.MobileParty.GetPosition2D, isOnLand: true);
				Campaign.Current.MapSceneWrapper.GetHeightAtPoint(in point, ref height);
				frame.origin = new Vec3(base.MobileParty.GetPosition2D.X, base.MobileParty.GetPosition2D.Y, height) + MapVisualOffset;
				_standaloneMapIcon.SetFrame(ref frame);
				RegisterStandaloneMapIconClickTarget();
				// Re-assert every visual tick: agent visuals can be recreated by the
				// party visual refresh and would pop back in.
				SetHomesteadRiderAgentVisualsVisibility(visible: false);
				return;
			}
			UnregisterStandaloneMapIconClickTarget();
			_standaloneMapIcon.Remove(115);
			_standaloneMapIcon = null;
		}
		_standaloneMapIcon = TryCreateStandaloneMapIconEntity();
		_standaloneMapIconTier = Tier;
		if (_standaloneMapIcon != null)
		{
			_standaloneMapIcon.SetVisibilityExcludeParents(visible: true);
			MatrixFrame frame2 = _standaloneMapIcon.GetFrame();
			float height2 = 0f;
			CampaignVec2 point2 = new CampaignVec2(base.MobileParty.GetPosition2D, isOnLand: true);
			Campaign.Current.MapSceneWrapper.GetHeightAtPoint(in point2, ref height2);
			frame2.origin = new Vec3(base.MobileParty.GetPosition2D.X, base.MobileParty.GetPosition2D.Y, height2) + MapVisualOffset;
			_standaloneMapIcon.SetFrame(ref frame2);
			RegisterStandaloneMapIconClickTarget();
			SetHomesteadRiderAgentVisualsVisibility(visible: false);
			TraceLogger.Write("Homestead", $"Created new standalone map icon for '{Name}' tier {Tier} during {reason}");
		}
	}

	/// <summary>
	/// Restores the party's raycast/click proxy. The StrategicEntity is NOT the
	/// rider's render body (agent visuals are separate entities) — it is what the
	/// map mouse ray hits to resolve the party. Hiding it does not hide the rider;
	/// it only makes the party unclickable. This runs every visual tick so any
	/// save/session that was affected by the old hiding heals itself.
	/// </summary>
	private void EnsureHomesteadPartyClickProxyVisible()
	{
		try
		{
			MobilePartyVisualManager.Current?.GetPartyVisual(base.Party)?.StrategicEntity?.SetVisibilityExcludeParents(visible: true);
		}
		catch
		{
		}
	}

	private static readonly FieldInfo? PartyVisualBannerEntityField =
		AccessTools.Field(typeof(MobilePartyVisual), "_cachedBannerEntity");

	/// <summary>
	/// While the camp model is on the map it IS the homestead's icon, so the
	/// rider render (leader + mount + banner flag) is hidden. This touches ONLY
	/// the agent visual entities and the cached banner entity — never the
	/// StrategicEntity, which is the party's raycast/click proxy and must stay
	/// visible (hiding it was the earlier mistake that killed all clicking).
	/// </summary>
	private void SetHomesteadRiderAgentVisualsVisibility(bool visible)
	{
		try
		{
			MobilePartyVisual? partyVisual = MobilePartyVisualManager.Current?.GetPartyVisual(base.Party);
			if (partyVisual == null)
			{
				return;
			}
			partyVisual.HumanAgentVisuals?.GetEntity()?.SetVisibilityExcludeParents(visible);
			partyVisual.MountAgentVisuals?.GetEntity()?.SetVisibilityExcludeParents(visible);
			partyVisual.CaravanMountAgentVisuals?.GetEntity()?.SetVisibilityExcludeParents(visible);
			if (PartyVisualBannerEntityField?.GetValue(partyVisual) is ValueTuple<string, GameEntity> bannerEntity)
			{
				bannerEntity.Item2?.SetVisibilityExcludeParents(visible);
			}
		}
		catch
		{
		}
	}

	/// <summary>
	/// The standalone icon is a raw scene entity, invisible to the campaign UI:
	/// hovering or clicking the camp model does nothing, so players who read the
	/// camp as "the homestead" cannot open its menu (only the party rider works).
	/// The map resolves hover/click by looking the hit entity's pointer up in
	/// MapScreen.VisualsOfEntities, so registering the icon entity against the
	/// homestead party's own visual makes clicking the camp behave exactly like
	/// clicking the party.
	/// </summary>
	private void RegisterStandaloneMapIconClickTarget()
	{
		if (_standaloneMapIcon == null)
		{
			return;
		}
		try
		{
			if (MapScreen.VisualsOfEntities.ContainsKey(_standaloneMapIcon.Pointer))
			{
				return;
			}
			MapEntityVisual? partyVisual = null;
			foreach (MapEntityVisual visual in MapScreen.VisualsOfEntities.Values)
			{
				if (visual is MapEntityVisual<PartyBase> partyEntityVisual && partyEntityVisual.MapEntity == base.Party)
				{
					partyVisual = visual;
					break;
				}
			}
			if (partyVisual != null)
			{
				MapScreen.VisualsOfEntities[_standaloneMapIcon.Pointer] = partyVisual;
				TraceLogger.Write("Homestead", $"Registered standalone map icon of '{Name}' as a click target for the homestead party.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.WriteOnce("IconClickReg_" + (base.MobileParty?.StringId ?? Name?.ToString() ?? "unknown"), "Homestead", $"Could not register map icon click target for '{Name}': {ex.GetType().Name}: {ex.Message}");
		}
	}

	private void UnregisterStandaloneMapIconClickTarget()
	{
		if (_standaloneMapIcon == null)
		{
			return;
		}
		try
		{
			MapScreen.VisualsOfEntities.Remove(_standaloneMapIcon.Pointer);
		}
		catch
		{
		}
	}

	private GameEntity? TryCreateStandaloneMapIconEntity()
	{
		Scene mapScene = MapScreen.Instance.MapScene;
		GameEntity? gameEntity2 = TryCreateTentClusterIcon(mapScene);
		if (gameEntity2 != null)
		{
			return gameEntity2;
		}
		foreach (MapVisualCandidate mapIconCandidate in GetMapIconCandidates())
		{
			try
			{
				GameEntity gameEntity = GameEntity.Instantiate(mapScene, mapIconCandidate.MeshName, callScriptCallbacks: false);
				if (gameEntity == null)
				{
					MetaMesh copy = MetaMesh.GetCopy(mapIconCandidate.MeshName, showErrors: false, mayReturnNull: true);
					if (copy != null)
					{
						gameEntity = GameEntity.CreateEmpty(mapScene);
						gameEntity.AddMultiMesh(copy);
					}
				}
				if (gameEntity != null)
				{
					MatrixFrame frame = MatrixFrame.Identity;
					frame.rotation.ApplyScaleLocal(mapIconCandidate.Scale);
					gameEntity.SetFrame(ref frame);
					// A bare decorative mesh is invisible to the map's mouse ray, so
					// the camp could never be hovered or clicked. Give it a
					// raycast-only sphere body — the exact pattern WarSails uses for
					// its clickable anchor icon — so the ray hits it and the
					// VisualsOfEntities registration resolves it to the homestead
					// party (tooltip + click -> homestead menu).
					gameEntity.AddSphereAsBody(new Vec3(0f, 0f, 0f, -1f), 2f, BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
					return gameEntity;
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Write("Homestead", $"Standalone map icon entity '{mapIconCandidate.MeshName}' failed for '{Name}' tier {Tier} ({ex.GetType().Name}: {ex.Message})");
			}
		}
		TraceLogger.Write("Homestead", $"No map icon mesh could be loaded for '{Name}' tier {Tier}");
		return null;
	}

	/// <summary>
	/// Camp icon built from the vanilla siege-camp tent map mesh: one tent at
	/// tier 0, growing to a five-tent camp at tier 4 — the homestead reads as a
	/// camp that expands with its tier, like a siege camp does. The raycast
	/// sphere sits on the cluster parent, which is the entity registered as the
	/// click target. Returns null (caller falls back to the old house meshes) if
	/// the tent mesh is unavailable.
	/// </summary>
	private GameEntity? TryCreateTentClusterIcon(Scene mapScene)
	{
		try
		{
			int num = 1 + Math.Max(0, Math.Min(Tier, 4));
			Vec2[] array = new Vec2[5]
			{
				new Vec2(0f, 0f),
				new Vec2(1.2f, 0.5f),
				new Vec2(-1.1f, 0.7f),
				new Vec2(0.8f, -1f),
				new Vec2(-1f, -0.9f)
			};
			float[] array2 = new float[5] { 0f, 2.4f, 4.1f, 1.2f, 5.3f };
			GameEntity gameEntity = GameEntity.CreateEmpty(mapScene);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				MetaMesh copy = MetaMesh.GetCopy("map_icon_siege_camp_tent", showErrors: false, mayReturnNull: true);
				if (copy == null)
				{
					break;
				}
				GameEntity gameEntity3 = GameEntity.CreateEmpty(mapScene);
				gameEntity3.AddMultiMesh(copy);
				MatrixFrame frame = MatrixFrame.Identity;
				frame.rotation.RotateAboutUp(array2[i]);
				frame.origin = new Vec3(array[i].x, array[i].y, 0f);
				gameEntity3.SetFrame(ref frame);
				gameEntity.AddChild(gameEntity3);
				num2++;
			}
			if (num2 == 0)
			{
				gameEntity.Remove(115);
				return null;
			}
			gameEntity.AddSphereAsBody(new Vec3(0f, 0f, 0f, -1f), 2.5f, BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
			TraceLogger.Write("Homestead", $"Created tent-cluster map icon for '{Name}': {num2} tents (tier {Tier}).");
			return gameEntity;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("Homestead", $"Tent-cluster icon failed for '{Name}' ({ex.GetType().Name}: {ex.Message}) — falling back to house meshes.");
			return null;
		}
	}

	public void DestroyStandaloneMapIcon()
	{
		if (_standaloneMapIcon != null)
		{
			UnregisterStandaloneMapIconClickTarget();
			_standaloneMapIcon.Remove(115);
			EnsureHomesteadPartyClickProxyVisible();
			// No camp model anymore (e.g. the homestead is moving) — the rider is
			// the homestead's marker again, so bring its render back.
			SetHomesteadRiderAgentVisualsVisibility(visible: true);
		}
		_standaloneMapIcon = null;
	}

	private void DailyTickPrisonerEscape()
	{
		if (Prisoners == null || Prisoners.TotalRegulars <= 0)
		{
			return;
		}
		float num = 0.05f;
		for (int num2 = Prisoners.Count - 1; num2 >= 0; num2--)
		{
			TroopRosterElement elementCopyAtIndex = Prisoners.GetElementCopyAtIndex(num2);
			if (elementCopyAtIndex.Character != null && !elementCopyAtIndex.Character.IsHero)
			{
				int num3 = 0;
				for (int i = 0; i < elementCopyAtIndex.Number; i++)
				{
					if (MBRandom.RandomFloat < num)
					{
						num3++;
					}
				}
				if (num3 > 0)
				{
					Prisoners.AddToCounts(elementCopyAtIndex.Character, -num3);
				}
			}
		}
	}

	public int GetTroopLimit()
	{
		return ((homesteadScene == null) ? GetBaseTroopLimitForTier() : (GetBaseTroopLimitForTier() + homesteadScene.TotalSpace)) + _apprenticeGraduationGarrisonBonus;
	}

	private int GetBaseTroopLimitForTier()
	{
		return 10 + Math.Max(0, Tier) * 10;
	}

	public int GetPrisonerLimit()
	{
		if (homesteadScene == null)
		{
			return 0;
		}
		return homesteadScene.PrisonerCapacity;
	}

	public float GetMedicalMoraleBonus()
	{
		return (float)MedicalCare * 1.5f;
	}

	public float GetAiInfluenceDiseaseCareBonus()
	{
		if (!IsAiInfluenceLoaded())
		{
			return 0f;
		}
		return (float)MedicalCare * 1.5f;
	}

	public float GetRegularHealingBonus()
	{
		return (float)MedicalCare * 2f;
	}

	public float GetHeroHealingBonus()
	{
		return (float)MedicalCare * 3f;
	}

	public static bool IsAiInfluenceLoaded()
	{
		return AppDomain.CurrentDomain.GetAssemblies().Any(delegate(Assembly assembly)
		{
			string text = assembly.GetName().Name ?? "";
			return text.IndexOf("AIInfluence", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("AI Influence", StringComparison.OrdinalIgnoreCase) >= 0;
		});
	}

	private void DailyTickAutoRecruitSettlers()
	{
		if (!AutoRecruitEnabled || base.MobileParty?.Party == null)
		{
			return;
		}
		HashSet<Settlement> hashSet = new HashSet<Settlement>();
		int activeRecruiterSummaryFor = HomesteadRecruiterComponent.GetActiveRecruiterSummaryFor(this, hashSet);
		int num = Math.Max(0, GetAutoRecruitSlotsRemaining() - activeRecruiterSummaryFor);
		if (num <= 0 || GoldStored <= 0)
		{
			return;
		}
		int num2 = activeRecruiterSummaryFor;
		int num3 = 0;
		foreach (Settlement autoRecruitVillageCandidate in GetAutoRecruitVillageCandidates())
		{
			if (num <= 0 || GoldStored <= 0)
			{
				break;
			}
			if (hashSet.Contains(autoRecruitVillageCandidate))
			{
				continue;
			}
			if (!HomesteadRecruiterComponent.TryCreateAndDispatch(this, autoRecruitVillageCandidate, num2, out MobileParty recruiterParty, out int recruitedCount))
			{
				TraceLogger.Write("Homestead", $"Failed to create visible recruit group for '{Name}' targeting village='{autoRecruitVillageCandidate.StringId}'.");
				continue;
			}
			num2 += Math.Max(0, recruitedCount);
			num = Math.Max(0, GetAutoRecruitSlotsRemaining() - num2);
			hashSet.Add(autoRecruitVillageCandidate);
			num3++;
			TraceLogger.Write("Homestead", string.Format("Dispatched visible recruit group '{0}' for '{1}' from village='{2}' diplomacy='{3}' recruits={4} inFlightCommitted={5} capacityRemaining={6} goldStored={7}.", recruiterParty?.StringId ?? "null", Name, autoRecruitVillageCandidate.StringId, GetRecruitmentDiplomacyState(autoRecruitVillageCandidate), recruitedCount, num2, num, GoldStored));
			if (GlobalSettings<MCMSettings>.Instance.ShowRecruitNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_recruiter_departed", "A group of recruits has left {VILLAGE_NAME} for your homestead of {HOMESTEAD_NAME}.", 0f, 201f, 0f, ("HOMESTEAD_NAME", name), ("VILLAGE_NAME", autoRecruitVillageCandidate.Name.ToString()));
			}
		}
		if (num3 > 0)
		{
			TraceLogger.Write("Homestead", $"Dispatched {num3} visible recruit groups for '{Name}' across nearby villages; recruitsInFlight={num2} capacityRemaining={num} goldStored={GoldStored}.");
		}
	}

	private void DailyTickAutoFoodBuy()
	{
		if (!AutoFoodBuyEnabled || base.MobileParty?.Party == null || GoldStored <= 0 || Stash == null)
		{
			return;
		}
		int numberOfAllMembers = base.MobileParty.Party.NumberOfAllMembers;
		if (numberOfAllMembers == 0)
		{
			return;
		}
		int num = 0;
		HashSet<ItemObject> hashSet = new HashSet<ItemObject>();
		foreach (ItemRosterElement item2 in Stash)
		{
			ItemObject item = item2.EquipmentElement.Item;
			if (item != null && item.IsFood)
			{
				num += item2.Amount;
				hashSet.Add(item2.EquipmentElement.Item);
			}
		}
		int num2 = numberOfAllMembers * 2;
		bool flag = num < num2;
		bool flag2 = hashSet.Count < 4;
		if (!flag && !flag2)
		{
			return;
		}
		int num3 = (int)((float)GoldStored * 0.25f);
		if (num3 <= 0)
		{
			return;
		}
		int num4 = 0;
		int num5 = 0;
		List<string> list = new List<string>();
		foreach (Settlement foodVillageCandidate in GetFoodVillageCandidates())
		{
			if (!flag && !flag2)
			{
				break;
			}
			int num6 = num3 - num4;
			int num7 = GetStashCapacity() - GetStashTotalItemCount() - num5;
			if (num6 <= 0 || num7 <= 0)
			{
				break;
			}
			ItemObject itemObject = foodVillageCandidate.Village?.VillageType?.PrimaryProduction;
			if (itemObject == null || !itemObject.IsFood || itemObject.HasHorseComponent)
			{
				continue;
			}
			bool flag3 = hashSet.Contains(itemObject);
			int val;
			if (flag)
			{
				val = Math.Max(0, num2 - num - num5);
			}
			else
			{
				if (flag3)
				{
					continue;
				}
				val = 10;
			}
			int villageFoodPrice = GetVillageFoodPrice(foodVillageCandidate, itemObject);
			if (villageFoodPrice > 0)
			{
				float marketPriceDiscount = MarketPriceDiscount;
				int num8 = ((marketPriceDiscount > 0f) ? Math.Max(1, (int)Math.Round((float)villageFoodPrice * (1f - marketPriceDiscount))) : villageFoodPrice);
				int val2 = num6 / num8;
				val = Math.Min(val, Math.Min(val2, num7));
				if (val > 0)
				{
					int num9 = val * num8;
					GoldStored -= num9;
					Stash.AddToCounts(itemObject, val);
					hashSet.Add(itemObject);
					num4 += num9;
					num5 += val;
					list.Add($"{val}x {itemObject.Name}");
					flag = num + num5 < num2;
					flag2 = hashSet.Count < 4;
				}
			}
		}
		if (num5 > 0)
		{
			TraceLogger.Write("Homestead", string.Format("Auto food buy for '{0}': bought {1} food ({2}) for {3} gold. goldStored={4}", Name, num5, string.Join(", ", list), num4, GoldStored));
			if (GlobalSettings<MCMSettings>.Instance.ShowDailyProductionNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_autofoodbuy_message", "Homestead {HOMESTEAD_NAME} restocked food: {FOOD_LIST} (-{GOLD_SPENT} gold).", 0f, 200f, 100f, ("HOMESTEAD_NAME", name), ("FOOD_LIST", string.Join(", ", list)), ("GOLD_SPENT", num4.ToString()));
			}
		}
	}

	private IEnumerable<Settlement> GetFoodVillageCandidates()
	{
		if (base.MobileParty == null)
		{
			yield break;
		}
		foreach (Settlement item in from s in Campaign.Current.Settlements.Where(delegate(Settlement s)
			{
				if (s != null && s.IsVillage)
				{
					Village village = s.Village;
					if (village != null && village.VillageType?.PrimaryProduction?.IsFood == true && !s.IsRaided && !s.IsUnderRaid)
					{
						return s.GetPosition2D.Distance(base.MobileParty.GetPosition2D) <= 35f;
					}
				}
				return false;
			})
			orderby s.GetPosition2D.Distance(base.MobileParty.GetPosition2D)
			select s)
		{
			yield return item;
		}
	}

	private static int GetVillageFoodPrice(Settlement village, ItemObject food)
	{
		try
		{
			Town town = village.Village?.Bound?.Town;
			if (town != null)
			{
				return Math.Max(1, town.GetItemPrice(food));
			}
		}
		catch
		{
		}
		return Math.Max(1, food.Value);
	}

	internal IEnumerable<(string CategoryId, int TroopCount)> GetHorseUpgradeNeeds()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		if (Troops != null)
		{
			foreach (TroopRosterElement item in Troops.GetTroopRoster())
			{
				if (!(item.Character?.IsHero ?? false) && item.Number > 0)
				{
					AccumulateHorseNeedForTroop(item.Character, item.Number, dictionary);
				}
			}
		}
		if (HasActivePatrol && patrolParty != null)
		{
			foreach (TroopRosterElement item2 in patrolParty.MemberRoster.GetTroopRoster())
			{
				if (!(item2.Character?.IsHero ?? false) && item2.Number > 0)
				{
					AccumulateHorseNeedForTroop(item2.Character, item2.Number, dictionary);
				}
			}
		}
		foreach (KeyValuePair<string, int> item3 in dictionary)
		{
			yield return (CategoryId: item3.Key, TroopCount: item3.Value);
		}
	}

	private static void AccumulateHorseNeedForTroop(CharacterObject character, int count, Dictionary<string, int> needs)
	{
		CharacterObject[] upgradeTargets = character.UpgradeTargets;
		if (upgradeTargets == null || upgradeTargets.Length == 0)
		{
			return;
		}
		CharacterObject[] array = upgradeTargets;
		for (int i = 0; i < array.Length; i++)
		{
			ItemCategory itemCategory = array[i]?.UpgradeRequiresItemFromCategory;
			if (itemCategory != null && (!(itemCategory.StringId != "horse") || !(itemCategory.StringId != "war_horse")))
			{
				needs.TryGetValue(itemCategory.StringId, out var value);
				needs[itemCategory.StringId] = value + count;
				break;
			}
		}
	}

	internal int CountStashItemsByCategory(ItemCategory category)
	{
		if (Stash == null || category == null)
		{
			return 0;
		}
		int num = 0;
		foreach (ItemRosterElement item in Stash)
		{
			if (item.EquipmentElement.Item?.ItemCategory == category)
			{
				num += Math.Max(0, item.Amount);
			}
		}
		return num;
	}

	internal int GetAutoRecruitSlotsRemaining()
	{
		if (base.MobileParty?.Party == null)
		{
			return 0;
		}
		return Math.Max(0, GetTroopLimit() - base.MobileParty.Party.NumberOfAllMembers);
	}

	internal IEnumerable<Settlement> GetAutoRecruitVillageCandidates()
	{
		if (base.MobileParty == null)
		{
			yield break;
		}
		foreach (Settlement item in from settlement in Campaign.Current.Settlements.Where(IsValidAutoRecruitVillage)
			orderby settlement.GetPosition2D.Distance(base.MobileParty.GetPosition2D)
			select settlement)
		{
			yield return item;
		}
	}

	internal bool IsValidAutoRecruitVillage(Settlement settlement)
	{
		if (settlement == null || !settlement.IsVillage)
		{
			return false;
		}
		if (settlement.IsRaided || settlement.IsUnderRaid)
		{
			return false;
		}
		if (settlement.GetPosition2D.Distance(base.MobileParty.GetPosition2D) > 35f)
		{
			return false;
		}
		return settlement.Notables.Any(HasRecruitableVolunteerSlot);
	}

	internal static bool CanUseNotableForAutoRecruit(Hero notable)
	{
		if (notable != null && notable.IsAlive)
		{
			return notable.CanHaveRecruits;
		}
		return false;
	}

	private static bool HasRecruitableVolunteerSlot(Hero notable)
	{
		if (!CanUseNotableForAutoRecruit(notable))
		{
			return false;
		}
		CharacterObject[] volunteerTypes = notable.VolunteerTypes;
		if (volunteerTypes == null)
		{
			return false;
		}
		int num = Math.Min(6, volunteerTypes.Length);
		for (int i = 0; i < num; i++)
		{
			if (volunteerTypes[i] != null && HeroHelper.HeroCanRecruitFromHero(Hero.MainHero, notable, i))
			{
				return true;
			}
		}
		return false;
	}

	internal (int Recruits, int Cost) RecruitVolunteersFromVillageToParty(MobileParty receivingParty, Settlement village, int alreadyCollected)
	{
		if (receivingParty?.MemberRoster == null)
		{
			return (Recruits: 0, Cost: 0);
		}
		return RecruitVolunteersFromVillageToRoster(receivingParty.MemberRoster, village, alreadyCollected);
	}

	internal (int Recruits, int Cost) RecruitVolunteersFromVillageToRoster(TroopRoster receivingRoster, Settlement village, int alreadyCollected)
	{
		if (receivingRoster == null || village == null)
		{
			return (Recruits: 0, Cost: 0);
		}
		int num = Math.Max(0, GetAutoRecruitSlotsRemaining() - Math.Max(0, alreadyCollected));
		if (num <= 0 || GoldStored <= 0)
		{
			return (Recruits: 0, Cost: 0);
		}
		int num2 = 0;
		int num3 = 0;
		foreach (Hero notable in village.Notables)
		{
			if (num <= 0 || GoldStored <= 0)
			{
				break;
			}
			if (!CanUseNotableForAutoRecruit(notable))
			{
				continue;
			}
			CharacterObject[] volunteerTypes = notable.VolunteerTypes;
			if (volunteerTypes == null)
			{
				continue;
			}
			int num4 = Math.Min(6, volunteerTypes.Length);
			for (int i = 0; i < num4; i++)
			{
				if (num <= 0)
				{
					break;
				}
				if (GoldStored <= 0)
				{
					break;
				}
				CharacterObject characterObject = volunteerTypes[i];
				if (characterObject != null && HeroHelper.HeroCanRecruitFromHero(Hero.MainHero, notable, i))
				{
					int num5 = Math.Max(0, Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(characterObject, Hero.MainHero).RoundedResultNumber);
					if (num5 <= GoldStored)
					{
						volunteerTypes[i] = null;
						GoldStored -= num5;
						receivingRoster.AddToCounts(characterObject, 1);
						NotifyAutoRecruitedTroop(village, notable, characterObject);
						num2++;
						num3 += num5;
						num--;
						TraceLogger.Write("Homestead", $"Visible recruit group collected '{characterObject.StringId}' for '{Name}' from village='{village.StringId}' notable='{notable.StringId}' slot={i} cost={num5} diplomacy='{GetRecruitmentDiplomacyState(village)}' remainingCapacity={num} goldStored={GoldStored}.");
					}
				}
			}
		}
		if (num2 > 0)
		{
			SetGameTextsForMenus();
		}
		return (Recruits: num2, Cost: num3);
	}

	internal int DepositRecruiterTroops(MobileParty recruiterParty, CharacterObject? escortTroop)
	{
		if (recruiterParty?.MemberRoster == null || base.MobileParty?.Party == null)
		{
			return 0;
		}
		int num = GetAutoRecruitSlotsRemaining();
		if (num <= 0)
		{
			return 0;
		}
		int num2 = 0;
		bool flag = false;
		foreach (TroopRosterElement item in recruiterParty.MemberRoster.GetTroopRoster().ToList())
		{
			if (num <= 0)
			{
				break;
			}
			if (item.Character != null && item.Number > 0)
			{
				int num3 = item.Number;
				if (!flag && escortTroop != null && item.Character == escortTroop)
				{
					num3--;
					flag = true;
				}
				if (num3 > 0)
				{
					int num4 = Math.Min(num3, num);
					Troops.AddToCounts(item.Character, num4);
					recruiterParty.MemberRoster.RemoveTroop(item.Character, num4);
					num2 += num4;
					num -= num4;
				}
			}
		}
		if (num2 <= 0)
		{
			return 0;
		}
		SetGameTextsForMenus();
		TraceLogger.Write("Homestead", $"Visible recruit group deposited {num2} volunteers into '{Name}'.");
		if (GlobalSettings<MCMSettings>.Instance.ShowRecruitNotifications)
		{
			Utils.PrintLocalizedMessage("homestead_recruiter_returned", "A group of recruits arrived at your homestead of {HOMESTEAD_NAME} with {NUMBER_OF_RECRUITS} volunteers.", 0f, 201f, 0f, ("HOMESTEAD_NAME", name), ("NUMBER_OF_RECRUITS", num2.ToString()));
		}
		return num2;
	}

	internal void NotifyAutoRecruitedTroop(Settlement village, Hero notable, CharacterObject recruitTroop)
	{
		try
		{
			CampaignEventDispatcher.Instance.OnTroopRecruited(leader ?? Hero.MainHero, village, notable, recruitTroop, 1);
		}
		catch (Exception arg)
		{
			TraceLogger.Write("Homestead", string.Format("Failed to dispatch auto-recruit event for '{0}': {1}", recruitTroop?.StringId ?? "null", arg));
		}
	}

	private TrainingFieldDailyResult DailyTickTrainingField()
	{
		if (!HasTrainingField() || base.MobileParty?.Party == null || Troops == null || Troops.TotalRegulars <= 0)
		{
			return TrainingFieldDailyResult.None;
		}
		PartyBase party = base.MobileParty.Party;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		foreach (TroopRosterElement item in Troops.GetTroopRoster().ToList())
		{
			CharacterObject character = item.Character;
			int number = item.Number;
			if (character == null || character.IsHero || number <= 0)
			{
				continue;
			}
			int num5 = Troops.FindIndexOfTroop(character);
			if (num5 < 0)
			{
				continue;
			}
			int num6 = 20 * number;
			int num7 = ((_armsMasterHero != null && _armsMasterHero.IsAlive) ? ((int)Math.Round((float)num6 * 1.15f)) : num6);
			int num8 = Math.Max(0, Troops.GetElementXp(num5)) + num7;
			num += num7;
			MCMSettings? instance = GlobalSettings<MCMSettings>.Instance;
			if (instance != null && !instance.AutoTroopUpgradesEnabled)
			{
				Troops.SetElementXp(num5, num8);
				continue;
			}
			if (!TryChooseTrainingFieldUpgradeTarget(party, character, out CharacterObject upgradeTarget, out int xpCost, out int goldCost) || upgradeTarget == null)
			{
				Troops.SetElementXp(num5, num8);
				continue;
			}
			int val = ((xpCost > 0) ? (num8 / xpCost) : 0);
			int val2 = ((goldCost > 0) ? (GoldStored / goldCost) : int.MaxValue);
			ItemCategory upgradeRequiresItemFromCategory = upgradeTarget.UpgradeRequiresItemFromCategory;
			int val3 = ((upgradeRequiresItemFromCategory != null) ? CountUpgradeItems(upgradeRequiresItemFromCategory) : int.MaxValue);
			int num9 = Math.Min(number, Math.Min(val, Math.Min(val2, val3)));
			if (num9 <= 0)
			{
				Troops.SetElementXp(num5, num8);
				continue;
			}
			int num10 = ConsumeUpgradeItems(upgradeRequiresItemFromCategory, num9);
			if (upgradeRequiresItemFromCategory != null && num10 < num9)
			{
				num9 = num10;
			}
			if (num9 <= 0)
			{
				Troops.SetElementXp(num5, num8);
				continue;
			}
			int num11 = goldCost * num9;
			GoldStored = Math.Max(0, GoldStored - num11);
			int number2 = Math.Max(0, num8 - xpCost * num9);
			Troops.AddToCounts(upgradeTarget, num9, insertAtFront: false, 0, 0, removeDepleted: false);
			Troops.RemoveTroop(character, num9);
			int num12 = Troops.FindIndexOfTroop(character);
			if (num12 >= 0)
			{
				Troops.SetElementXp(num12, number2);
			}
			num2 += num9;
			num3 += num11;
			num4 += num10;
			DispatchTrainingFieldUpgradeEvent(party, character, upgradeTarget, num9);
			TraceLogger.Write("Homestead", $"Training field upgraded {num9} '{character.StringId}' to '{upgradeTarget.StringId}' at '{Name}' xpSpent={xpCost * num9} goldSpent={num11} itemsSpent={num10}.");
		}
		if (num > 0 || num2 > 0)
		{
			SetGameTextsForMenus();
			base.MobileParty.Party.SetVisualAsDirty();
			TraceLogger.Write("Homestead", $"Training field daily result for '{Name}': xpAdded={num} troopsUpgraded={num2} goldSpent={num3} itemsSpent={num4}.");
		}
		return new TrainingFieldDailyResult(num, num2, num3, num4);
	}

	private bool HasTrainingField()
	{
		HomesteadScene? obj = homesteadScene;
		if (obj == null)
		{
			return false;
		}
		return obj.SavedEntities?.Any((HomesteadSceneSavedEntity x) => x?.Placeable?.PrefabName == "homestead_training_field") == true;
	}

	private static bool TryChooseTrainingFieldUpgradeTarget(PartyBase party, CharacterObject character, out CharacterObject? upgradeTarget, out int xpCost, out int goldCost)
	{
		upgradeTarget = null;
		xpCost = 0;
		goldCost = 0;
		CharacterObject[] upgradeTargets = character.UpgradeTargets;
		if (upgradeTargets == null || upgradeTargets.Length == 0)
		{
			return false;
		}
		PartyTroopUpgradeModel partyTroopUpgradeModel = Campaign.Current.Models.PartyTroopUpgradeModel;
		List<(CharacterObject, int, int)> list = new List<(CharacterObject, int, int)>();
		CharacterObject[] array = upgradeTargets;
		foreach (CharacterObject characterObject in array)
		{
			if (characterObject != null && partyTroopUpgradeModel.CanPartyUpgradeTroopToTarget(party, character, characterObject))
			{
				int xpCostForUpgrade = partyTroopUpgradeModel.GetXpCostForUpgrade(party, character, characterObject);
				if (xpCostForUpgrade > 0)
				{
					int item = Math.Max(0, partyTroopUpgradeModel.GetGoldCostForUpgrade(party, character, characterObject).RoundedResultNumber);
					list.Add((characterObject, xpCostForUpgrade, item));
				}
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		(upgradeTarget, xpCost, goldCost) = list[(list.Count != 1) ? MBRandom.RandomInt(0, list.Count) : 0];
		return true;
	}

	private int CountUpgradeItems(ItemCategory? itemCategory)
	{
		if (itemCategory == null || Stash == null)
		{
			return 0;
		}
		int num = 0;
		foreach (ItemRosterElement item in Stash)
		{
			if (item.EquipmentElement.Item?.ItemCategory == itemCategory)
			{
				num += Math.Max(0, item.Amount);
			}
		}
		return num;
	}

	private int ConsumeUpgradeItems(ItemCategory? itemCategory, int amount)
	{
		if (itemCategory == null || amount <= 0 || Stash == null)
		{
			return 0;
		}
		int num = 0;
		int num2 = amount;
		foreach (ItemRosterElement item in Stash.ToList())
		{
			if (num2 <= 0)
			{
				break;
			}
			if (item.EquipmentElement.Item?.ItemCategory == itemCategory)
			{
				int num3 = Math.Min(num2, Math.Max(0, item.Amount));
				if (num3 > 0)
				{
					Stash.AddToCounts(item.EquipmentElement, -num3);
					num2 -= num3;
					num += num3;
				}
			}
		}
		return num;
	}

	private static void DispatchTrainingFieldUpgradeEvent(PartyBase party, CharacterObject sourceTroop, CharacterObject upgradeTroop, int amount)
	{
		try
		{
			SkillLevelingManager.OnUpgradeTroops(party, sourceTroop, upgradeTroop, amount);
		}
		catch (Exception arg)
		{
			TraceLogger.Write("Homestead", string.Format("Failed to dispatch training field upgrade event from '{0}' to '{1}': {2}", sourceTroop?.StringId ?? "null", upgradeTroop?.StringId ?? "null", arg));
		}
	}

	internal static string GetRecruitmentDiplomacyState(Settlement village)
	{
		IFaction faction = Hero.MainHero?.MapFaction;
		IFaction faction2 = village?.MapFaction;
		if (faction == null || faction2 == null)
		{
			return "unknown";
		}
		if (faction.IsAtWarWith(faction2))
		{
			return "enemy";
		}
		if (faction == faction2)
		{
			return "same-faction";
		}
		return "neutral";
	}

	private float GetEffectiveDailyChance(HomesteadScenePlaceableProducedItem pi)
	{
		if (HasStable && StableMasterRecruited && pi.ItemProducedID == "mule|sumpter_horse")
		{
			return 1f;
		}
		return pi.DailyChance;
	}

	private Dictionary<string, int> DailyTickProduceItems()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		if (homesteadScene == null)
		{
			return dictionary;
		}
		bool flag = false;
		try
		{
			foreach (HomesteadScenePlaceableProducedItem dailyProduceItem in GetDailyProduceItems())
			{
				if (GetStashTotalItemCount() >= GetStashCapacity())
				{
					if (!flag && GlobalSettings<MCMSettings>.Instance.ShowDailyProductionNotifications)
					{
						Utils.PrintLocalizedMessage("homestead_stash_full_production", "Homestead stash is full ({CURRENT}/{CAPACITY} items). Production halted until space is freed.", 255f, 200f, 80f, ("CURRENT", GetStashTotalItemCount().ToString()), ("CAPACITY", GetStashCapacity().ToString()));
						flag = true;
					}
					break;
				}
				if (MBRandom.RandomFloat > GetEffectiveDailyChance(dailyProduceItem))
				{
					continue;
				}
				string randomElementInefficiently = dailyProduceItem.ItemProducedID.Split(new char[1] { '|' }).GetRandomElementInefficiently();
				ItemObject itemObject = Campaign.Current.ObjectManager.GetObject<ItemObject>(randomElementInefficiently);
				if (itemObject != null && (dailyProduceItem.RequiredItemsToProduce.Count <= 0 || Utils.DoesItemRosterHaveItems(Stash, dailyProduceItem.RequiredItemsToProduce, takeItems: true)))
				{
					Stash.AddToCounts(itemObject, dailyProduceItem.AmountToProduce);
					string key = itemObject.Name?.ToString() ?? itemObject.StringId;
					if (!dictionary.ContainsKey(key))
					{
						dictionary[key] = 0;
					}
					dictionary[key] += dailyProduceItem.AmountToProduce;
				}
			}
		}
		catch (NullReferenceException)
		{
			if (homesteadScene.ProduceItems == null)
			{
				homesteadScene.ProduceItems = new List<HomesteadScenePlaceableProducedItem>();
			}
		}
		ProduceStableMasterHorses(dictionary);
		return dictionary;
	}

	private IEnumerable<HomesteadScenePlaceableProducedItem> GetDailyProduceItems()
	{
		if (homesteadScene == null)
		{
			yield break;
		}
		foreach (HomesteadSceneSavedEntity savedEntity in homesteadScene.SavedEntities)
		{
			foreach (HomesteadScenePlaceableProducedItem item in GetDailyProduceItemsForPlaceable(savedEntity.Placeable))
			{
				yield return item;
			}
		}
	}

	private static IEnumerable<HomesteadScenePlaceableProducedItem> GetDailyProduceItemsForPlaceable(HomesteadScenePlaceable placeable)
	{
		if (placeable.ProduceItems != null && placeable.ProduceItems.Count > 0)
		{
			foreach (HomesteadScenePlaceableProducedItem produceItem in placeable.ProduceItems)
			{
				yield return produceItem;
			}
			yield break;
		}
		string text = placeable.DisplayName ?? "";
		string prefabName = placeable.PrefabName;
		if (prefabName == null)
		{
			yield break;
		}
		switch (prefabName.Length)
		{
		case 23:
			switch (prefabName[10])
			{
			case 'b':
				if (prefabName == "homestead_butchers_tent")
				{
					yield return new HomesteadScenePlaceableProducedItem("meat|hides", 4, 1f, new Dictionary<string, int> { { "cow", 1 } });
					yield return new HomesteadScenePlaceableProducedItem("meat|hides", 3, 1f, new Dictionary<string, int> { { "hog", 1 } });
					yield return new HomesteadScenePlaceableProducedItem("meat|hides", 3, 1f, new Dictionary<string, int> { { "sheep", 1 } });
					yield return new HomesteadScenePlaceableProducedItem("meat", 1, 1f, new Dictionary<string, int> { { "chicken", 1 } });
				}
				break;
			case 'c':
				if (prefabName == "homestead_clay_gatherer")
				{
					yield return new HomesteadScenePlaceableProducedItem("clay", 3, 1f, new Dictionary<string, int>());
				}
				break;
			}
			break;
		case 20:
			switch (prefabName[10])
			{
			default:
				yield break;
			case 's':
				if (prefabName == "homestead_sheep_farm")
				{
					yield return new HomesteadScenePlaceableProducedItem("wool", 1, 1f, new Dictionary<string, int>());
					yield return new HomesteadScenePlaceableProducedItem("sheep", 1, 0.33f, new Dictionary<string, int>());
				}
				yield break;
			case 'g':
				if (prefabName == "homestead_grain_farm")
				{
					yield return new HomesteadScenePlaceableProducedItem("grain", 3, 1f, new Dictionary<string, int>());
				}
				yield break;
			case 'c':
				break;
			}
			if (!(prefabName == "homestead_cattle_pen"))
			{
				break;
			}
			goto IL_05d1;
		case 18:
			if (prefabName == "homestead_hog_farm")
			{
				yield return new HomesteadScenePlaceableProducedItem("meat", 1, 1f, new Dictionary<string, int>());
				yield return new HomesteadScenePlaceableProducedItem("hog", 1, 0.33f, new Dictionary<string, int>());
			}
			break;
		case 22:
			if (prefabName == "homestead_chicken_coop")
			{
				yield return new HomesteadScenePlaceableProducedItem("meat", 1, 1f, new Dictionary<string, int>());
				yield return new HomesteadScenePlaceableProducedItem("chicken", 1, 0.33f, new Dictionary<string, int>());
			}
			break;
		case 19:
			if (prefabName == "homestead_flax_farm")
			{
				yield return new HomesteadScenePlaceableProducedItem("flax", 3, 1f, new Dictionary<string, int>());
			}
			break;
		case 25:
			if (prefabName == "homestead_lumberjack_camp")
			{
				yield return new HomesteadScenePlaceableProducedItem("hardwood", 3, 1f, new Dictionary<string, int>());
			}
			break;
		case 16:
			if (prefabName == "homestead_smithy")
			{
				yield return new HomesteadScenePlaceableProducedItem("tools", 1, 1f, new Dictionary<string, int>());
			}
			break;
		case 30:
			if (!(prefabName == "homestead_native_horse_paddock") || text.IndexOf("Cattle", StringComparison.OrdinalIgnoreCase) < 0)
			{
				break;
			}
			goto IL_05d1;
		case 21:
			{
				if (prefabName == "homestead_flax_weaver")
				{
					yield return new HomesteadScenePlaceableProducedItem("linen", 3, 1f, new Dictionary<string, int> { { "flax", 3 } });
				}
				break;
			}
			IL_05d1:
			yield return new HomesteadScenePlaceableProducedItem("hides", 1, 1f, new Dictionary<string, int>());
			yield return new HomesteadScenePlaceableProducedItem("cow", 1, 0.33f, new Dictionary<string, int>());
			break;
		}
	}

	private float GetDailyGoldTierMultiplier()
	{
		if (Tier <= 0)
		{
			return 0.5f;
		}
		return Tier;
	}

	public int GetEffectiveProductivity()
	{
		int num = homesteadScene?.TotalProductivity ?? 0;
		int num2 = Prisoners?.TotalRegulars ?? 0;
		return num + num2 * 2;
	}

	private int CalculateDailyGoldChange(int leisureCostRoll)
	{
		float dailyGoldTierMultiplier = GetDailyGoldTierMultiplier();
		int num = -(int)Math.Round(20f * dailyGoldTierMultiplier);
		if (base.MobileParty?.Party != null)
		{
			try
			{
				int num2 = (int)Campaign.Current.Models.PartyWageModel.GetTotalWage(base.MobileParty, base.MobileParty.MemberRoster).ResultNumber;
				num -= num2;
			}
			catch (Exception ex)
			{
				TraceLogger.Write("Homestead", $"Failed to calculate garrison wages for '{Name}': {ex.Message}");
				int numberOfAllMembers = base.MobileParty.Party.NumberOfAllMembers;
				num -= numberOfAllMembers * 2;
			}
		}
		if (HasActivePatrol && patrolParty?.Party != null)
		{
			try
			{
				int num3 = (int)Campaign.Current.Models.PartyWageModel.GetTotalWage(patrolParty, patrolParty.MemberRoster).ResultNumber;
				num -= num3;
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("Homestead", $"Failed to calculate patrol wages for '{Name}': {ex2.Message}");
				int numberOfAllMembers2 = patrolParty.Party.NumberOfAllMembers;
				num -= numberOfAllMembers2 * 2;
			}
		}
		num += 100 * GetEffectiveProductivity();
		if (homesteadScene != null)
		{
			num -= (int)Math.Round((float)leisureCostRoll * dailyGoldTierMultiplier * (float)homesteadScene.TotalLeisure);
		}
		return num;
	}

	private int DailyTickChangeGoldStored()
	{
		int num = CalculateDailyGoldChange(MBRandom.RandomInt(7, 15));
		int goldStored = GoldStored;
		GoldStored += num;
		if (GoldStored < 0)
		{
			GoldStored = 0;
		}
		return GoldStored - goldStored;
	}

	private (int Min, int Max, float Average) GetDailyGoldForecast()
	{
		int num = CalculateDailyGoldChange(7);
		int num2 = CalculateDailyGoldChange(14);
		return (Min: num2, Max: num, Average: (float)(num2 + num) / 2f);
	}

	private float GetDailyMoraleDriftForecast()
	{
		if (homesteadScene == null)
		{
			return 0f;
		}
		int num = Prisoners?.TotalRegulars ?? 0;
		return (float)(homesteadScene.TotalLeisure - (homesteadScene.TotalProductivity - 2 * num)) / 10f + GetNotableRelationMoraleDelta();
	}

	private float GetNotableRelationMoraleDelta()
	{
		if (base.MobileParty?.MemberRoster == null)
		{
			return 0f;
		}
		float num = 0f;
		foreach (TroopRosterElement item in base.MobileParty.MemberRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character != null && character.IsHero)
			{
				Hero heroObject = character.HeroObject;
				if (heroObject != null && heroObject.IsAlive && heroObject != leader && !heroObject.IsHumanPlayerCharacter && heroObject.Clan != Campaign.Current.MainParty.ActualClan && heroObject.Clan != Hero.MainHero.Clan)
				{
					num += heroObject.GetRelationWithPlayer() / 50f;
				}
			}
		}
		return num;
	}

	private static string FormatSignedInt(int value)
	{
		if (value <= 0)
		{
			return value.ToString();
		}
		return "+" + value;
	}

	private static string FormatSignedFloat(float value)
	{
		if (!(value > 0f))
		{
			return value.ToString("0.##");
		}
		return "+" + value.ToString("0.##");
	}

	private void NotifyDailyTickResults(int goldChange, Dictionary<string, int> producedItems, TrainingFieldDailyResult trainingResult)
	{
		int tavernTariffAccruedToday = _tavernTariffAccruedToday;
		_tavernTariffAccruedToday = 0;
		if (goldChange == 0 && producedItems.Count == 0 && !trainingResult.HasResults && tavernTariffAccruedToday == 0)
		{
			return;
		}
		List<string> list = new List<string>();
		if (goldChange != 0)
		{
			list.Add(FormatSignedInt(goldChange) + Utils.GetLocalizedString("{=homestead_gold_label} gold"));
		}
		if (tavernTariffAccruedToday > 0)
		{
			list.Add("+" + tavernTariffAccruedToday + Utils.GetLocalizedString("{=homestead_tavern_tolls_label} gold (tavern road tolls)"));
		}
		foreach (KeyValuePair<string, int> item in producedItems.OrderBy<KeyValuePair<string, int>, string>((KeyValuePair<string, int> x) => x.Key))
		{
			string text = MBObjectManager.Instance.GetObject<ItemObject>(item.Key)?.Name?.ToString() ?? item.Key;
			list.Add(item.Value + " " + text);
		}
		if (trainingResult.XpAdded > 0)
		{
			int xpAdded = trainingResult.XpAdded;
			list.Add("+" + xpAdded + Utils.GetLocalizedString("{=homestead_training_xp_label} training XP"));
		}
		if (trainingResult.TroopsUpgraded > 0)
		{
			int xpAdded = trainingResult.TroopsUpgraded;
			list.Add("+" + xpAdded + Utils.GetLocalizedString("{=homestead_troop_upgrades_label} troop upgrades"));
		}
		if (trainingResult.GoldSpent > 0)
		{
			int xpAdded = trainingResult.GoldSpent;
			list.Add("-" + xpAdded + Utils.GetLocalizedString("{=homestead_training_gold_label} training gold"));
		}
		if (trainingResult.UpgradeItemsSpent > 0)
		{
			int xpAdded = trainingResult.UpgradeItemsSpent;
			list.Add("-" + xpAdded + Utils.GetLocalizedString("{=homestead_upgrade_items_used_label} upgrade items used"));
		}
		if (GlobalSettings<MCMSettings>.Instance.ShowDailyProductionNotifications)
		{
			Utils.PrintLocalizedMessage("homestead_daily_tick_earnings", "Homestead of {HOMESTEAD_NAME} daily report: {EARNINGS}.", 120f, 220f, 255f, ("HOMESTEAD_NAME", name), ("EARNINGS", string.Join(", ", list)));
		}
	}

	private void DailyTickMoraleChange()
	{
		if (homesteadScene != null && leader != null)
		{
			int num = Prisoners?.TotalRegulars ?? 0;
			float num2 = (float)(homesteadScene.TotalProductivity - 2 * num - homesteadScene.TotalLeisure) / 10f;
			base.MobileParty.RecentEventsMorale -= num2;
			if (base.MobileParty.Morale <= 30f)
			{
				TextObject textObject = new TextObject("{=homestead_low_morale_warning}Your homestead of {HOMESTEAD_NAME} has low morale!");
				textObject.SetTextVariable("HOMESTEAD_NAME", name);
				MBInformationManager.AddQuickInformation(textObject, 0, leader.CharacterObject);
			}
		}
	}

	private void DailyTickNotableRelationMorale()
	{
		if (leader != null)
		{
			float notableRelationMoraleDelta = GetNotableRelationMoraleDelta();
			if (notableRelationMoraleDelta != 0f)
			{
				base.MobileParty.RecentEventsMorale += notableRelationMoraleDelta;
				TraceLogger.Write("Homestead", string.Format("Notable-relation morale for '{0}': → morale {1}{2:0.##}/day.", Name, (notableRelationMoraleDelta >= 0f) ? "+" : "", notableRelationMoraleDelta));
			}
		}
	}

	private void DailyTickNoGoldPenalty()
	{
		if (GoldStored == 0 && leader != null)
		{
			base.MobileParty.RecentEventsMorale -= 5f;
			TextObject textObject = new TextObject("{=homestead_no_gold_stored_warning}Your homestead of {HOMESTEAD_NAME} is lacking gold!");
			textObject.SetTextVariable("HOMESTEAD_NAME", name);
			MBInformationManager.AddQuickInformation(textObject, 0, leader.CharacterObject);
		}
	}

	private void DailyTickLeaderSkillXp(int troopsUpgraded)
	{
		if (leader != null)
		{
			int num = homesteadScene?.TotalSpace ?? 0;
			int num2 = homesteadScene?.CurrentlyUsedBuildPoints ?? 0;
			int num3 = 25 + Math.Max(0, Tier) * 10;
			int num4 = num3 + Troops.TotalRegulars * 2 + num * 3;
			int num5 = num3 + num2 * 2;
			int num6 = num3 + troopsUpgraded * 3;
			leader.AddSkillXp(DefaultSkills.Steward, num4);
			leader.AddSkillXp(DefaultSkills.Engineering, num5);
			leader.AddSkillXp(DefaultSkills.Leadership, num6);
			var (num7, num8, num9, num10) = DailyTickSupportingHeroSkillXp(num4, num5, num6);
			TraceLogger.Write("Homestead", $"Daily leader XP for '{Name}' leader='{leader.StringId}': Steward +{num4}, Engineering +{num5}, Leadership +{num6}, supportingHeroes={num7}.");
			if (num7 > 0 && GlobalSettings<MCMSettings>.Instance.ShowNpcXpNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_supporting_hero_xp", "{SUPPORTING_HERO_COUNT} companion(s) gained {STEWARD_XP} Steward XP, {ENGINEERING_XP} Engineering XP, and {LEADERSHIP_XP} Leadership XP at {HOMESTEAD_NAME}.", 100f, 180f, 255f, ("SUPPORTING_HERO_COUNT", num7.ToString()), ("STEWARD_XP", num8.ToString()), ("ENGINEERING_XP", num9.ToString()), ("LEADERSHIP_XP", num10.ToString()), ("HOMESTEAD_NAME", Name.ToString()));
			}
		}
	}

	private (int count, int stewardXp, int engineeringXp, int leadershipXp) DailyTickSupportingHeroSkillXp(int stewardXp, int engineeringXp, int leadershipXp)
	{
		if (base.MobileParty?.MemberRoster == null)
		{
			return (count: 0, stewardXp: 0, engineeringXp: 0, leadershipXp: 0);
		}
		int num = Math.Max(1, (int)Math.Round((float)stewardXp * (1f / 3f)));
		int num2 = Math.Max(1, (int)Math.Round((float)engineeringXp * (1f / 3f)));
		int num3 = Math.Max(1, (int)Math.Round((float)leadershipXp * (1f / 3f)));
		int num4 = 0;
		foreach (TroopRosterElement item in base.MobileParty.MemberRoster.GetTroopRoster().ToList())
		{
			CharacterObject character = item.Character;
			if (character == null || !character.IsHero)
			{
				continue;
			}
			Hero heroObject = character.HeroObject;
			if (heroObject != null && heroObject != leader && !heroObject.IsHumanPlayerCharacter && heroObject.PartyBelongedTo == base.MobileParty)
			{
				HeroDeveloper heroDeveloper = heroObject.HeroDeveloper;
				if (heroDeveloper != null)
				{
					heroDeveloper.AddSkillXp(DefaultSkills.Steward, num, isAffectedByFocusFactor: true, shouldNotify: false);
					heroDeveloper.AddSkillXp(DefaultSkills.Engineering, num2, isAffectedByFocusFactor: true, shouldNotify: false);
					heroDeveloper.AddSkillXp(DefaultSkills.Leadership, num3, isAffectedByFocusFactor: true, shouldNotify: false);
					num4++;
				}
			}
		}
		return (count: num4, stewardXp: num, engineeringXp: num2, leadershipXp: num3);
	}

	public void AddTradeXpToLeaderAndSupporters(int profit)
	{
		if (leader == null || profit <= 0)
		{
			return;
		}
		int num = Math.Max(1, profit / 10);
		leader.AddSkillXp(DefaultSkills.Trade, num);
		int num2 = Math.Max(1, (int)Math.Round((float)num * (1f / 3f)));
		int num3 = 0;
		if (base.MobileParty?.MemberRoster != null)
		{
			foreach (TroopRosterElement item in base.MobileParty.MemberRoster.GetTroopRoster().ToList())
			{
				CharacterObject character = item.Character;
				if (character == null || !character.IsHero)
				{
					continue;
				}
				Hero heroObject = character.HeroObject;
				if (heroObject != null && heroObject != leader && !heroObject.IsHumanPlayerCharacter && heroObject.PartyBelongedTo == base.MobileParty)
				{
					HeroDeveloper heroDeveloper = heroObject.HeroDeveloper;
					if (heroDeveloper != null)
					{
						heroDeveloper.AddSkillXp(DefaultSkills.Trade, num2, isAffectedByFocusFactor: true, shouldNotify: false);
						num3++;
					}
				}
			}
		}
		TraceLogger.Write("Homestead", $"Trade XP for '{Name}': profit={profit}, leader +{num} Trade XP, supportingHeroes={num3} each +{num2}.");
		if (num3 > 0 && GlobalSettings<MCMSettings>.Instance.ShowNpcXpNotifications)
		{
			Utils.PrintLocalizedMessage("homestead_supporting_trade_xp", "{SUPPORTING_HERO_COUNT} companion(s) gained {TRADE_XP} Trade XP at {HOMESTEAD_NAME}.", 100f, 180f, 255f, ("SUPPORTING_HERO_COUNT", num3.ToString()), ("TRADE_XP", num2.ToString()), ("HOMESTEAD_NAME", Name.ToString()));
		}
	}

	private void OnTierProgressComplete()
	{
		if (Tier == 1 && !Tier1ApprovalGranted)
		{
			TierProgress = 1f;
			Tier1GrowthReady = true;
		}
		else if (Tier == 2 && !Tier2ApprovalGranted)
		{
			TierProgress = 1f;
			Tier2GrowthReady = true;
		}
		else if (Tier >= 3)
		{
			TierProgress = 1f;
			if (!SettlementUpgradeReady)
			{
				SettlementUpgradeReady = true;
				base.MobileParty?.Party?.SetVisualAsDirty();
				SetGameTextsForMenus();
			}
		}
		else
		{
			Tier++;
			TierProgress = 0f;
			base.MobileParty?.Party?.SetVisualAsDirty();
			HomesteadChronicle.Record($"The homestead of {Name} has grown into a larger camp (tier {Tier}).");
		}
	}

	public void OnHeadmanApprovalGranted()
	{
		Tier1ApprovalGranted = true;
		TryApplyHeldTier2Upgrade();
	}

	public void TryApplyHeldTier2Upgrade()
	{
		if (Tier == 1 && Tier1GrowthReady && Tier1ApprovalGranted)
		{
			Tier = 2;
			TierProgress = 0f;
			Tier1GrowthReady = false;
			Tier1ApprovalGranted = false;
			base.MobileParty?.Party?.SetVisualAsDirty();
			SetGameTextsForMenus();
		}
	}

	public void OnLandPatentGranted()
	{
		Tier2ApprovalGranted = true;
		TryApplyHeldTier3Upgrade();
	}

	public void TryApplyHeldTier3Upgrade()
	{
		if (Tier == 2 && Tier2GrowthReady && Tier2ApprovalGranted)
		{
			Tier = 3;
			TierProgress = 0f;
			Tier2GrowthReady = false;
			Tier2ApprovalGranted = false;
			base.MobileParty?.Party?.SetVisualAsDirty();
			SetGameTextsForMenus();
		}
	}

	public static Hero? FindHeadmanOfVillage(Settlement? village)
	{
		return village?.Notables?.FirstOrDefault((Hero n) => n != null && n.IsAlive && n.IsHeadman);
	}

	private void DailyTickHeadmanTrust()
	{
		if (Tier != 1 || !Tier1GrowthReady || Tier1ApprovalGranted)
		{
			return;
		}
		HomesteadBehavior instance = HomesteadBehavior.Instance;
		if (instance == null)
		{
			return;
		}
		HomesteadHeadmanTrustQuest activeHeadmanTrustQuest = instance.GetActiveHeadmanTrustQuest(this);
		if (activeHeadmanTrustQuest == null)
		{
			return;
		}
		if (activeHeadmanTrustQuest.Village != FindNearestVillage())
		{
			activeHeadmanTrustQuest.AbandonForRelocation();
			return;
		}
		instance.EnsureHeadmanHasIssue(activeHeadmanTrustQuest.Headman);
		if (AmbassadorAidingHeadman && HasAmbassadorHall && AmbassadorHero != null && activeHeadmanTrustQuest.Headman != null && activeHeadmanTrustQuest.Headman.IsAlive && activeHeadmanTrustQuest.Headman.GetRelationWithPlayer() < 40f)
		{
			ChangeRelationAction.ApplyPlayerRelation(activeHeadmanTrustQuest.Headman, 1, affectRelatives: false, showQuickNotification: false);
		}
	}

	public Settlement? FindNearestVillage()
	{
		if (base.MobileParty == null)
		{
			return null;
		}
		Vec2 pos = base.MobileParty.GetPosition2D;
		return (from s in Campaign.Current?.Settlements?.Where((Settlement s) => s?.IsVillage ?? false)
			orderby s.GetPosition2D.DistanceSquared(pos)
			select s).FirstOrDefault();
	}

	public void OnSettlementCharterGranted()
	{
		SettlementCharterGranted = true;
		SetGameTextsForMenus();
	}

	public Settlement? FindNearestKingdomSettlement()
	{
		if (base.MobileParty == null)
		{
			return null;
		}
		Vec2 pos = base.MobileParty.GetPosition2D;
		return (from s in Campaign.Current?.Settlements?.Where((Settlement s) => s != null && (s.IsTown || s.IsCastle || s.IsVillage) && s.OwnerClan?.Kingdom != null)
			orderby s.GetPosition2D.DistanceSquared(pos)
			select s).FirstOrDefault();
	}

	public Settlement? FindNearestSettlementAny()
	{
		if (base.MobileParty == null)
		{
			return null;
		}
		Vec2 pos = base.MobileParty.GetPosition2D;
		return (from s in Campaign.Current?.Settlements?.Where((Settlement s) => s != null && (s.IsTown || s.IsCastle || s.IsVillage))
			orderby s.GetPosition2D.DistanceSquared(pos)
			select s).FirstOrDefault();
	}

	public bool CanPlayerSelfGrantCharter()
	{
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null)
		{
			return false;
		}
		Kingdom kingdom = FindNearestKingdomSettlement()?.OwnerClan?.Kingdom;
		if (kingdom != null && kingdom.RulingClan == playerClan)
		{
			return true;
		}
		if (playerClan.Kingdom == null && FindNearestSettlementAny()?.OwnerClan == playerClan)
		{
			return true;
		}
		return false;
	}

	private void DailyTickSettlementCharter()
	{
		if (!SettlementUpgradeReady || SettlementCharterGranted)
		{
			return;
		}
		HomesteadBehavior instance = HomesteadBehavior.Instance;
		if (instance == null)
		{
			return;
		}
		HomesteadSettlementCharterQuest activeSettlementCharterQuest = instance.GetActiveSettlementCharterQuest(this);
		if (activeSettlementCharterQuest == null)
		{
			return;
		}
		if (activeSettlementCharterQuest.Settlement != FindNearestKingdomSettlement())
		{
			activeSettlementCharterQuest.AbandonForRelocation();
			return;
		}
		Hero ruler = activeSettlementCharterQuest.Ruler;
		if (!activeSettlementCharterQuest.PlayerIsRuler)
		{
			instance.EnsureRulerHasIssue(ruler);
		}
		if (AmbassadorAidingHeadman && HasAmbassadorHall && AmbassadorHero != null && !activeSettlementCharterQuest.PlayerIsRuler && ruler != null && ruler.IsAlive && ruler.GetRelationWithPlayer() < (float)activeSettlementCharterQuest.RequiredRelation && MBRandom.RandomFloat < 0.5f)
		{
			ChangeRelationAction.ApplyPlayerRelation(ruler, 1, affectRelatives: false, showQuickNotification: false);
		}
	}

	public Settlement? FindNearestTown()
	{
		if (base.MobileParty == null)
		{
			return null;
		}
		Vec2 pos = base.MobileParty.GetPosition2D;
		return (from s in Campaign.Current?.Settlements?.Where((Settlement s) => s?.IsTown ?? false)
			orderby s.GetPosition2D.DistanceSquared(pos)
			select s).FirstOrDefault();
	}

	private void DailyTickLandPatent()
	{
		if (Tier != 2 || !Tier2GrowthReady || Tier2ApprovalGranted)
		{
			return;
		}
		HomesteadBehavior instance = HomesteadBehavior.Instance;
		if (instance == null)
		{
			return;
		}
		HomesteadLandPatentQuest activeLandPatentQuest = instance.GetActiveLandPatentQuest(this);
		if (activeLandPatentQuest == null)
		{
			return;
		}
		if (activeLandPatentQuest.Town != FindNearestTown())
		{
			activeLandPatentQuest.AbandonForRelocation();
		}
		else if (AmbassadorAidingHeadman && HasAmbassadorHall && AmbassadorHero != null && activeLandPatentQuest.OwningClan != null && activeLandPatentQuest.ClanRelation < 40)
		{
			List<Hero> list = activeLandPatentQuest.OwningClan.Heroes?.Where((Hero h) => h?.IsAlive ?? false).ToList();
			if (list != null && list.Count > 0)
			{
				ChangeRelationAction.ApplyPlayerRelation(list[MBRandom.RandomInt(list.Count)], 1, affectRelatives: false, showQuickNotification: false);
			}
		}
	}

	private string GetMapVisualTag()
	{
		return string.Format("{0}_tier_{1}", "homestead_custom_map_visual", Math.Max(0, Math.Min(Tier, 4)));
	}

	public void OnPatrolDestroyed()
	{
		if (patrolParty != null)
		{
			patrolParty = null;
			_patrolFollowingPlayer = false;
			_patrolEngageTarget = null;
			_patrolChaseHours = 0;
			SetGameTextsForMenus();
			if (!isRetiredOrDestroyed && GlobalSettings<MCMSettings>.Instance.ShowPatrolNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_patrol_destroyed", "The patrol from your homestead of {HOMESTEAD_NAME} has been destroyed!", 255f, 80f, 80f, ("HOMESTEAD_NAME", name));
			}
		}
	}

	private void DailyTickCheckPatrol()
	{
		if (!AutoPatrolEnabled || HasActivePatrol)
		{
			return;
		}
		patrolParty = null;
		if (GetAutoRecruitSlotsRemaining() > 0)
		{
			return;
		}
		int num = 0;
		foreach (TroopRosterElement item in Troops.GetTroopRoster())
		{
			if (item.Character != null && !item.Character.IsHero && item.Number > 0)
			{
				num += item.Number;
			}
		}
		int num2 = num / 2;
		if (num2 >= 15)
		{
			CreatePatrol(num2);
		}
	}

	private void CreatePatrol(int patrolSize)
	{
		if (HasActivePatrol || base.MobileParty == null)
		{
			return;
		}
		List<(CharacterObject, int)> list = new List<(CharacterObject, int)>();
		int num = 0;
		foreach (TroopRosterElement item in Troops.GetTroopRoster().ToList())
		{
			if (num >= patrolSize)
			{
				break;
			}
			if (item.Character != null && !item.Character.IsHero && item.Number > 0)
			{
				int num2 = Math.Min(item.Number, patrolSize - num);
				list.Add((item.Character, num2));
				num += num2;
			}
		}
		if (num == 0)
		{
			return;
		}
		HomesteadPatrolPartyComponent component = new HomesteadPatrolPartyComponent(this);
		MobileParty mobileParty = MobileParty.CreateParty("homestead_patrol_" + base.MobileParty.StringId, component);
		mobileParty.InitializeMobilePartyAroundPosition(new TroopRoster(mobileParty.Party), new TroopRoster(mobileParty.Party), base.MobileParty.Position, 1f);
		mobileParty.ActualClan = Hero.MainHero.Clan;
		mobileParty.ShouldJoinPlayerBattles = false;
		mobileParty.Aggressiveness = 1f;
		int num3 = 0;
		foreach (var (characterObject, num4) in list)
		{
			mobileParty.MemberRoster.AddToCounts(characterObject, num4);
			Troops.RemoveTroop(characterObject, num4);
			num3 += num4;
		}
		if (num3 == 0)
		{
			try
			{
				DestroyPartyAction.Apply(null, mobileParty);
				return;
			}
			catch
			{
				return;
			}
		}
		List<Settlement> list2 = GetAutoRecruitVillageCandidates().ToList();
		if (list2.Count > 0)
		{
			mobileParty.SetMoveGoToPoint(new CampaignVec2(list2[0].GetPosition2D, isOnLand: true), MobileParty.NavigationType.Default);
			_patrolTargetIndex = 1 % list2.Count;
		}
		else
		{
			mobileParty.SetMoveGoToPoint(base.MobileParty.Position, MobileParty.NavigationType.Default);
			_patrolTargetIndex = 0;
		}
		patrolParty = mobileParty;
		if (HomesteadBehavior.Instance != null)
		{
			HomesteadBehavior.Instance.PatrolMobileParties[mobileParty] = this;
		}
		SetGameTextsForMenus();
		TraceLogger.Write("Homestead", $"Created patrol party '{mobileParty.StringId}' with {num3} troops for '{Name}'.");
		Utils.PrintLocalizedMessage("homestead_patrol_created", "A patrol of {PATROL_SIZE} troops has been sent out from your homestead of {HOMESTEAD_NAME}.", 80f, 200f, 255f, ("PATROL_SIZE", num3.ToString()), ("HOMESTEAD_NAME", name));
	}

	private TextObject BuildInformationTextObject()
	{
		TextObject variable = new TextObject("{=homestead_menu_info_title}Homestead of ");
		string value = ((Tier < 3) ? "{=homestead_menu_info_tier}Tier: {TIER_LEVEL}\n({TIER_PROGRESS_PERCENT}% to next tier!)" : (SettlementUpgradeReady ? "{=homestead_menu_info_tier_settlement_ready}Tier: {TIER_LEVEL} (ready to become a settlement)" : "{=homestead_menu_info_tier_settlement}Tier: {TIER_LEVEL}\n({TIER_PROGRESS_PERCENT}% to settlement upgrade)"));
		TextObject textObject = new TextObject(value);
		textObject.SetTextVariable("TIER_LEVEL", Tier);
		textObject.SetTextVariable("TIER_PROGRESS_PERCENT", (float)decimal.Round((decimal)TierProgress * 100.0m, 1));
		TextObject textObject2 = new TextObject("{=homestead_menu_info_gold_stored}Gold Stored: {GOLD_STORED}");
		textObject2.SetTextVariable("GOLD_STORED", GoldStored);
		TextObject textObject3 = new TextObject("{=homestead_menu_info_productivity}Total Productivity: {TOTAL_PRODUCTIVITY}");
		textObject3.SetTextVariable("TOTAL_PRODUCTIVITY", GetEffectiveProductivity());
		TextObject textObject4 = new TextObject("{=homestead_menu_info_space}Extra Space: {EXTRA_SPACE}");
		textObject4.SetTextVariable("EXTRA_SPACE", (homesteadScene != null) ? homesteadScene.TotalSpace : 0);
		TextObject textObject5 = new TextObject("{=homestead_menu_info_garrison_limit}Garrison Limit: {GARRISON_LIMIT}");
		textObject5.SetTextVariable("GARRISON_LIMIT", GetTroopLimit());
		TextObject textObject6 = new TextObject("{=homestead_menu_info_leisure}Total Leisure: {TOTAL_LEISURE}");
		textObject6.SetTextVariable("TOTAL_LEISURE", (homesteadScene != null) ? homesteadScene.TotalLeisure : 0);
		TextObject textObject7 = new TextObject("{=homestead_menu_info_medical_care}Medical Care: {TOTAL_MEDICAL_CARE}");
		textObject7.SetTextVariable("TOTAL_MEDICAL_CARE", MedicalCare);
		(int Min, int Max, float Average) dailyGoldForecast = GetDailyGoldForecast();
		int item = dailyGoldForecast.Min;
		int item2 = dailyGoldForecast.Max;
		float item3 = dailyGoldForecast.Average;
		TextObject textObject8 = new TextObject("{=homestead_menu_info_daily_gold_forecast}Expected Daily Gold: {DAILY_GOLD_MIN} to {DAILY_GOLD_MAX} (avg {DAILY_GOLD_AVG})");
		textObject8.SetTextVariable("DAILY_GOLD_MIN", FormatSignedInt(item));
		textObject8.SetTextVariable("DAILY_GOLD_MAX", FormatSignedInt(item2));
		textObject8.SetTextVariable("DAILY_GOLD_AVG", FormatSignedFloat(item3));
		TextObject textObject9 = new TextObject("{=homestead_menu_info_morale_drift}Expected Morale Drift: {MORALE_DRIFT}/day");
		textObject9.SetTextVariable("MORALE_DRIFT", FormatSignedFloat(GetDailyMoraleDriftForecast()));
		TextObject textObject10 = new TextObject("{=homestead_menu_info_morale}Total Morale: {TOTAL_MORALE}\n" + base.MobileParty.MoraleExplained.GetExplanations());
		textObject10.SetTextVariable("TOTAL_MORALE", base.MobileParty.Morale);
		int stashTotalItemCount = GetStashTotalItemCount();
		int stashCapacity = GetStashCapacity();
		TextObject textObject11 = new TextObject("{=homestead_menu_info_stash}Stash: {STASH_USED}/{STASH_CAP} items");
		textObject11.SetTextVariable("STASH_USED", stashTotalItemCount);
		textObject11.SetTextVariable("STASH_CAP", stashCapacity);
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		foreach (HomesteadScenePlaceableProducedItem dailyProduceItem in GetDailyProduceItems())
		{
			if (!dictionary.ContainsKey(dailyProduceItem.ItemProducedID))
			{
				dictionary[dailyProduceItem.ItemProducedID] = 0f;
			}
			dictionary[dailyProduceItem.ItemProducedID] += (float)dailyProduceItem.AmountToProduce * GetEffectiveDailyChance(dailyProduceItem);
		}
		StringBuilder lines = new StringBuilder();
		foreach (KeyValuePair<string, float> item4 in dictionary)
		{
			string[] source = item4.Key.Split(new char[1] { '|' });
			string variable2 = string.Join("/", source.Select((string id) => MBObjectManager.Instance.GetObject<ItemObject>(id)?.Name?.ToString() ?? id));
			TextObject textObject12 = new TextObject("{=homestead_production_line_format}  {DISPLAY_NAME}: ~{EXPECTED_YIELD}/day");
			textObject12.SetTextVariable("DISPLAY_NAME", variable2);
			textObject12.SetTextVariable("EXPECTED_YIELD", item4.Value.ToString("F1"));
			lines.Append(textObject12.ToString() + "\n");
		}
		if (HasStable && StableMasterRecruited)
		{
			AddTypeLine("{=homestead_prod_riding_horse}Riding Horse", 0.5f);
			AddTypeLine("{=homestead_prod_war_horse}War Horse", 0.3f);
			HomesteadBehavior instance = HomesteadBehavior.Instance;
			if ((instance != null && instance.HasStableMasterMasteryUnlocked) || StableMasterMasteryUnlocked)
			{
				AddTypeLine("{=homestead_prod_noble_horse}Noble Horse", 0.1f);
			}
		}
		string text = ((lines.Length == 0) ? Utils.GetLocalizedString("{=homestead_menu_info_none}  (none)") : lines.ToString().TrimEnd(new char[1] { '\n' }));
		TextObject variable3 = new TextObject("{=homestead_menu_info_production}Daily Production:\n" + text);
		TextObject textObject13 = new TextObject("{=homestead_menu_info_master}{TITLE}{NAME} - {TIER}\n{SEPARATOR}\n{GOLD}\n{STASH}\n{PRODUCTION}\n{SEPARATOR}\n{PRODUCTIVITY}\n{SPACE}\n{LIMITS}\n{LEISURE}\n{MEDICAL}\n{FORECAST_GOLD}\n{FORECAST_MORALE}\n{MORALE}");
		textObject13.SetTextVariable("TITLE", variable);
		textObject13.SetTextVariable("NAME", Name);
		textObject13.SetTextVariable("TIER", textObject);
		textObject13.SetTextVariable("SEPARATOR", "--------------------------------------------");
		textObject13.SetTextVariable("GOLD", textObject2);
		textObject13.SetTextVariable("STASH", textObject11);
		textObject13.SetTextVariable("PRODUCTION", variable3);
		textObject13.SetTextVariable("PRODUCTIVITY", textObject3);
		textObject13.SetTextVariable("SPACE", textObject4);
		textObject13.SetTextVariable("LIMITS", textObject5);
		textObject13.SetTextVariable("LEISURE", textObject6);
		textObject13.SetTextVariable("MEDICAL", textObject7);
		textObject13.SetTextVariable("FORECAST_GOLD", textObject8);
		textObject13.SetTextVariable("FORECAST_MORALE", textObject9);
		textObject13.SetTextVariable("MORALE", textObject10);
		return textObject13;
		void AddTypeLine(string label, float perDay)
		{
			TextObject textObject14 = new TextObject("{=homestead_production_line_format}  {DISPLAY_NAME}: ~{EXPECTED_YIELD}/day");
			textObject14.SetTextVariable("DISPLAY_NAME", Utils.GetLocalizedString(label));
			textObject14.SetTextVariable("EXPECTED_YIELD", perDay.ToString("F1"));
			lines.Append(textObject14.ToString() + "\n");
		}
	}

	public static Homestead? GetFor(MobileParty mobileParty)
	{
		if (mobileParty == null || HomesteadBehavior.Instance == null)
		{
			return null;
		}
		try
		{
			if (HomesteadBehavior.Instance.HomesteadMobileParties.TryGetValue(mobileParty, out Homestead value))
			{
				if (value.IsRetiredOrDestroyed)
				{
					HomesteadBehavior.Instance.HomesteadMobileParties.Remove(mobileParty);
					return null;
				}
				return value;
			}
		}
		catch (Exception ex)
		{
			TraceLogger.WriteOnce("GetForRace", "Homestead", "GetFor: HomesteadMobileParties lookup threw (rare read/write race) — falling back to PartyComponent check: " + ex.GetType().Name + ": " + ex.Message);
		}
		if (mobileParty.PartyComponent is Homestead homestead)
		{
			if (homestead.IsRetiredOrDestroyed)
			{
				return null;
			}
			try
			{
				HomesteadBehavior.Instance.HomesteadMobileParties[mobileParty] = homestead;
			}
			catch
			{
			}
			TraceLogger.Write("Homestead", "Recovered missing homestead registry entry for '" + mobileParty.StringId + "' from PartyComponent.");
			return homestead;
		}
		return null;
	}

	public static Homestead? GetNearby(Vec2 position, float proximityRadius = 5f)
	{
		if (HomesteadBehavior.Instance == null)
		{
			return null;
		}
		foreach (KeyValuePair<MobileParty, Homestead> homesteadMobileParty in HomesteadBehavior.Instance.HomesteadMobileParties)
		{
			Homestead value = homesteadMobileParty.Value;
			if (!value.IsRetiredOrDestroyed && value.MedicalCare > 0)
			{
				MobileParty mobileParty = value.MobileParty;
				if (mobileParty != null && mobileParty.GetPosition2D.Distance(position) <= proximityRadius)
				{
					return value;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Everything worth stealing: stored gold plus the market value of the stash.
	/// Drives the wealth-attracts-raiders pressure.
	/// </summary>
	public int GetWealthValue()
	{
		int num = Math.Max(0, GoldStored);
		try
		{
			foreach (ItemRosterElement item in Stash)
			{
				ItemObject item2 = item.EquipmentElement.Item;
				if (item2 != null && item.Amount > 0)
				{
					num += item2.Value * item.Amount;
				}
			}
		}
		catch
		{
		}
		return num;
	}

	public int GetStashCapacity()
	{
		return Tier switch
		{
			0 => 200, 
			1 => 500, 
			2 => 1000, 
			3 => 1500, 
			4 => 2000, 
			_ => 200, 
		} + _apprenticeGraduationInventoryBonus;
	}

	public int GetStashTotalItemCount()
	{
		int num = 0;
		if (Stash == null)
		{
			return 0;
		}
		foreach (ItemRosterElement item in Stash)
		{
			num += item.Amount;
		}
		return num;
	}

	public static int GetPaidUpgradeCostForTier(int tier)
	{
		return tier switch
		{
			0 => 10000, 
			1 => 100000, 
			2 => 1000000, 
			3 => 10000000, 
			_ => 0, 
		};
	}

	public int GetCurrentPaidUpgradeCost()
	{
		int paidUpgradeCostForTier = GetPaidUpgradeCostForTier(Tier);
		if (paidUpgradeCostForTier <= 0)
		{
			return 0;
		}
		float num = 1f - Math.Max(0f, Math.Min(1f, TierProgress));
		return Math.Max(1, (int)Math.Round((float)paidUpgradeCostForTier * num));
	}

	public static void SetGameTextsForMenus()
	{
		if (HomesteadBehavior.Instance != null && HomesteadBehavior.Instance.CurrentHomestead != null)
		{
			Homestead currentHomestead = HomesteadBehavior.Instance.CurrentHomestead;
			GameTexts.SetVariable("CURRENT_HOMESTEAD_INFORMATION", currentHomestead.HomesteadInformation);
			GameTexts.SetVariable("CURRENT_HOMESTEAD_NAME", currentHomestead.Name);
			GameTexts.SetVariable("CURRENT_HOMESTEAD_PAID_UPGRADE_COST", currentHomestead.GetCurrentPaidUpgradeCost().ToString("N0"));
			if (currentHomestead.Tier == 3)
			{
				GameTexts.SetVariable("CURRENT_HOMESTEAD_UPGRADE_TARGET", Utils.GetLocalizedString("{=homestead_gamemenu_pay_upgrade_eligible}to become eligible for a settlement charter"));
			}
			else
			{
				GameTexts.SetVariable("CURRENT_HOMESTEAD_NEXT_TIER", Math.Min(currentHomestead.Tier + 1, 4));
				GameTexts.SetVariable("CURRENT_HOMESTEAD_UPGRADE_TARGET", Utils.GetLocalizedString("{=homestead_gamemenu_pay_upgrade_tier}to upgrade to tier {CURRENT_HOMESTEAD_NEXT_TIER}"));
			}
			GameTexts.SetVariable("CURRENT_HOMESTEAD_LEADER_NAME", (currentHomestead.Leader == null) ? "" : currentHomestead.Leader.Name.ToString());
			GameTexts.SetVariable("CURRENT_HOMESTEAD_AUTO_RECRUIT_STATE", currentHomestead.AutoRecruitEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
			GameTexts.SetVariable("CURRENT_HOMESTEAD_AUTO_PATROL_STATE", currentHomestead.AutoPatrolEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
			GameTexts.SetVariable("CURRENT_HOMESTEAD_CARAVAN_TRADE_STATE", currentHomestead.CaravanTradingEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
			GameTexts.SetVariable("CURRENT_HOMESTEAD_AUTO_FOOD_BUY_STATE", currentHomestead.AutoFoodBuyEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
		}
	}
}
