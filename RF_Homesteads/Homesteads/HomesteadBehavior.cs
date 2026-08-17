using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using Helpers;
using Homesteads.MissionLogics;
using Homesteads.Models;
using Homesteads.Patches;
using Homesteads.Views;
using MCM.Abstractions.Base.Global;
using SandBox;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;
using TaleWorlds.ScreenSystem;

namespace Homesteads;

public class HomesteadBehavior : CampaignBehaviorBase
{
	private enum PendingOfferType
	{
		Fetch,
		Delivery,
		Raid,
		Apparel,
		Building,
		Apprentice
	}

	public static HomesteadBehavior Instance;

	private const float HomesteadWaitDurationHours = 9999f;

	private const float MapVisualSyncIntervalSeconds = 1f;

	private const float MapVisualSyncMovementThreshold = 0.05f;

	private Homestead? currentHomestead;

	private float mapVisualSyncTimer;

	private bool hasLastPlayerMapPosition;

	private Vec2 lastPlayerMapPosition;

	private bool pendingPlanningMode;

	private bool pendingPlacementMode;

	public Dictionary<MobileParty, Homestead> HomesteadMobileParties = new Dictionary<MobileParty, Homestead>();

	public Dictionary<MobileParty, Homestead> PatrolMobileParties = new Dictionary<MobileParty, Homestead>();

	public int TutorialStage;

	public Homestead? CurrentPatrolHomestead;

	public MobileParty? CurrentPatrolParty;

	private readonly HashSet<MobileParty> _escortingVillagers = new HashSet<MobileParty>();

	public MobileParty? CurrentVillagerParty;

	public MobileParty? CurrentRecruiterParty;

	public Homestead? CurrentRecruiterHomestead;

	private Dictionary<MobileParty, HomesteadCaravanVisit> _activeCaravanVisits = new Dictionary<MobileParty, HomesteadCaravanVisit>();

	private Dictionary<string, int> _caravanCooldowns = new Dictionary<string, int>();

	private float _waitMenuHostileCheckTimer;

	private const float WaitMenuHostileCheckIntervalSeconds = 2f;

	private const float WaitMenuHostileEncounterRadius = 1f;

	private const float WaitMenuHostileWarningRadius = 8f;

	private string? _lastWarnedAttackerStringId;

	private bool _hasAdoptedDog;

	private string _adoptedDogName = "";

	private string _adoptedDogHomesteadId = "";

	private bool _hasPendingSparringResult;

	private bool _lastSparringResultPlayerWon;

	private int _adoptedDogMaterialIndex;

	private Dictionary<string, int> _notableFavorsDone = new Dictionary<string, int>();

	private Dictionary<string, int> _notableApparelSlotsMask = new Dictionary<string, int>();

	private Dictionary<string, int> _clanTournamentEntries = new Dictionary<string, int>();

	private Dictionary<string, int> _clanTournamentWins = new Dictionary<string, int>();

	private Dictionary<string, string> _clanTournamentNames = new Dictionary<string, string>();

	private Dictionary<string, int> _clanSparringEntries = new Dictionary<string, int>();

	private Dictionary<string, int> _clanSparringWins = new Dictionary<string, int>();

	private Dictionary<string, string> _clanSparringNames = new Dictionary<string, string>();

	private Dictionary<string, HomesteadTrackStats> _trackStats = new Dictionary<string, HomesteadTrackStats>();

	private Dictionary<string, int> _lastRaidDayByHomestead = new Dictionary<string, int>();

	public Dictionary<string, int> _notableApprenticeCount = new Dictionary<string, int>();

	public Dictionary<string, string> ConvertedNotableRoles = new Dictionary<string, string>();

	private const int MaxApprenticesPerNotable = 1;

	private string _offerItem = "";

	private int _offerCount;

	private static readonly string[] FavorItemPool = new string[12]
	{
		"grain", "hides", "clay", "flax", "wool", "hardwood", "fish", "charcoal", "meat", "pottery",
		"butter", "beer"
	};

	private const int FavorMaxCount = 30;

	private const int FavorRelationReward = 4;

	private static readonly string[] DeliveryItemPool = new string[5] { "pottery", "spice", "velvet", "fur", "jewelry" };

	private const int DeliveryRelationRewardSender = 5;

	public Hero? PendingTalkHero;

	private const float CaravanDetourRadius = 15f;

	private const float CaravanArrivalRadius = 8f;

	private const float NavalTradeRadius = 15f;

	private const int CaravanCooldownDays = 7;

	private const int CaravanMaxApproachHours = 12;

	private const int CaravanTradeDurationHours = 4;

	public bool HasHoundmasterKnockdownUnlocked;

	public bool HasAmbassadorTactfulIntroductionUnlocked;

	public bool HasMarketLadyTradeDiscountUnlocked;

	public bool HasArmsMasterMasteryUnlocked;

	public bool HasStableMasterMasteryUnlocked;

	public bool HasTavernKeeperBonusActive;

	private int _lastSmithUpgradePickerTick;

	public bool HasMasterSmithUpgradeUnlocked;

	private Dictionary<string, SettlementSmithUpgradeRecord> _settlementSmithUpgrades = new Dictionary<string, SettlementSmithUpgradeRecord>();

	public bool RaceFlagsHidden;

	private int _ambassadorCompanionSlotBonus;

	private static bool? _aiInfluenceInstalled;

	private Hero? _pendingDeliveryRecipient;

	private ItemObject? _pendingDeliveryPackage;

	private PendingOfferType _pendingOfferType;

	private PendingOfferType? _forcedOfferType;

	private static readonly string[] TavernCultureChoiceIds = new string[6] { "empire", "vlandia", "sturgia", "aserai", "khuzait", "battania" };

	private static readonly (EquipmentIndex Index, ItemObject.ItemTypeEnum Type, string LabelKey)[] ApparelSlots = new(EquipmentIndex, ItemObject.ItemTypeEnum, string)[5]
	{
		(EquipmentIndex.NumAllWeaponSlots, ItemObject.ItemTypeEnum.HeadArmor, "homestead_apparel_slot_helmet"),
		(EquipmentIndex.Cape, ItemObject.ItemTypeEnum.Cape, "homestead_apparel_slot_shoulders"),
		(EquipmentIndex.Body, ItemObject.ItemTypeEnum.BodyArmor, "homestead_apparel_slot_torso"),
		(EquipmentIndex.Gloves, ItemObject.ItemTypeEnum.HandArmor, "homestead_apparel_slot_gloves"),
		(EquipmentIndex.Leg, ItemObject.ItemTypeEnum.LegArmor, "homestead_apparel_slot_shoes")
	};

	private Hero? _pendingApparelNotable;

	private ItemObject? _pendingApparelItem;

	private int _pendingApparelSlotIdx;

	private Hero? _pendingBuildingNotable;

	private string? _pendingBuildingPrefab;

	private string? _pendingBuildingDisplay;

	private static readonly string[] UsefulDefensePrefabs = new string[4] { "homestead_training_field", "ballista_b", "homestead_watchtower", "arrow_barrel" };

	private const int SettlementDogCost = 1000;

	private static bool _adoptedDogItemRegistered = false;

	private const string AdoptedDogItemXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Items>\n  <Item id=\"homestead_adopted_dog\"\n        name=\"{=homestead_adopted_dog_name}Loyal Hound\"\n        value=\"1\"\n        is_merchandise=\"false\"\n        item_category=\"goods\"\n        type=\"Goods\" />\n</Items>";

	private const float ConvAmbassadorRange = 20f;

	private const int ConvAmbassadorRelationGain = 1;

	private const float ConvAmbassadorDailyChance = 0.5f;

	private const float ConvAmbassadorHourlyChance = 0.15f;

	private Dictionary<string, HashSet<string>> _convAmbassadorNearbyByAnchor = new Dictionary<string, HashSet<string>>();

	private bool _pendingPlayerEncounterFinish;

	private static readonly string[] _npcLinesGuard = new string[6] { "{=hr_npc_guard_1}Quiet on the watch, {HONORIFIC}. Not so much as a stray fox.", "{=hr_npc_guard_2}The gate's secure. We'll raise the alarm the moment anything stirs.", "{=hr_npc_guard_3}I keep my eyes on the road, {HONORIFIC}. No one passes unannounced.", "{=hr_npc_guard_4}Cold night for sentry duty, but the homestead sleeps easy for it.", "{=hr_npc_guard_5}Rest easy, {HONORIFIC}. Nothing gets past this post while I'm standing it.", "{=hr_npc_guard_6}Spear's sharp and my eyes are open. Trouble would be a fool to try us." };

	private static readonly string[] _npcLinesSoldier = new string[6] { "{=hr_npc_soldier_1}Good to see you about, {HONORIFIC}. The lads are keeping sharp.", "{=hr_npc_soldier_2}We drill every morning. This place will be ready if trouble comes.", "{=hr_npc_soldier_3}An honour to serve here, {HONORIFIC}. Beats marching in the mud, that's certain.", "{=hr_npc_soldier_4}Give the word and we'll march, {HONORIFIC}. Until then, we hold the line here.", "{=hr_npc_soldier_5}My blade is yours, {HONORIFIC} — same as the day I took the oath.", "{=hr_npc_soldier_6}The garrison's in good order. You've little to fear from raiders." };

	private static readonly string[] _npcLinesWorker = new string[6] { "{=hr_npc_worker_1}Honest work today, {HONORIFIC} — the fields don't tend themselves.", "{=hr_npc_worker_2}Back's aching, but the harvest's looking fair this year.", "{=hr_npc_worker_3}Plenty to do before sundown, {HONORIFIC}. A homestead's never finished.", "{=hr_npc_worker_4}We'll have the stores full before the frost, the gods willing.", "{=hr_npc_worker_5}Good land, this. Treat it right and it feeds everyone, {HONORIFIC}.", "{=hr_npc_worker_6}Mind the fresh mud by the pens, {HONORIFIC} — been hauling all morning." };

	private static readonly string[] _npcLinesMusician = new string[6] { "{=hr_npc_musician_1}A tune for the master of the house? Just say the word, {HONORIFIC}.", "{=hr_npc_musician_2}Music keeps the spirits high and the work moving, {HONORIFIC}.", "{=hr_npc_musician_3}I know every song from here to the capital — name one and I'll play it.", "{=hr_npc_musician_4}Ah, {HONORIFIC}! Come to hear an old melody, have you?", "{=hr_npc_musician_5}A homestead with no song is just a heap of timber, I always say.", "{=hr_npc_musician_6}My fingers are cold, but the strings still sing. Care for a verse?" };

	private static readonly string[] _npcLinesVillager = new string[6] { "{=hr_npc_villager_1}{HONORIFIC}! Didn't expect to see you walking the grounds. It's an honour.", "{=hr_npc_villager_2}Bless you, {HONORIFIC}. This place has been good to us and ours.", "{=hr_npc_villager_3}We're settling in well, {HONORIFIC}. Never thought I'd have a roof this fine.", "{=hr_npc_villager_4}A fine day to you, {HONORIFIC}. The little ones are growing strong here.", "{=hr_npc_villager_5}Good of you to visit, {HONORIFIC}. We don't see the master often.", "{=hr_npc_villager_6}All's well with us, {HONORIFIC}. Hard work and good neighbours — what more is there?" };

	private Homestead? _pendingSparringHomestead;

	private Settlement? _pendingSparringSettlement;

	private Hero? _pendingSparringArmsMaster;

	private int _pendingSparringTeamSize;

	private int _sparSelectPhase;

	private int _sparSelectDelayTicks;

	private float _sparSelectIdleTime;

	private TroopRoster? _sparPlayerRoster;

	private TroopRoster? _sparEnemyRoster;

	private const float SparSelectAbortSeconds = 90f;

	private Homestead? _pendingReturnHomestead;

	private Homestead? _pendingTavernHomestead;

	private Homestead? _pendingRaceHomestead;

	private bool _raceNeedsSelection;

	private bool _raceAwaitingRivalPicker;

	private Homestead? _racePickHs;

	private RaceTrack? _racePickTrack;

	private Settlement? _racePickSettlement;

	private bool _pendingArenaRace;

	private Settlement? _arenaRaceSettlement;

	private bool _pendingForge;

	private bool _pendingPostConversionSaveReload;

	private int _postConversionSaveReloadDelayTicks;

	private static readonly Dictionary<int, HashSet<string>> _buildingMaterialIdsByTier = new Dictionary<int, HashSet<string>>();

	public Homestead? CurrentHomestead
	{
		get
		{
			return currentHomestead;
		}
		set
		{
			currentHomestead = value;
			Homestead.SetGameTextsForMenus();
		}
	}

	public bool HasAdoptedDog => _hasAdoptedDog;

	public string AdoptedDogName => _adoptedDogName;

	public int AdoptedDogMaterialIndex => _adoptedDogMaterialIndex;

	public int AmbassadorCompanionSlotBonus => _ambassadorCompanionSlotBonus;

	private bool SparringPending
	{
		get
		{
			if (_pendingSparringHomestead == null)
			{
				return _pendingSparringSettlement != null;
			}
			return true;
		}
	}

	internal static bool SparringMissionActive { get; set; }

	internal static bool TavernMissionActive { get; set; }

	public bool ReturnNearTavernEntrance { get; private set; }

	public static event Action<string, List<(string name, string id, int placeIndex, int totalCs, bool finished)>, Homestead?>? OnRaceFinished;

	public static event Action<Homestead, Settlement, Settlement?>? OnHomesteadConvertedToSettlement;

	public bool IsEscortingVillager(MobileParty party)
	{
		if (party != null)
		{
			return _escortingVillagers.Contains(party);
		}
		return false;
	}

	public void StartEscortingVillager(MobileParty party)
	{
		if (party == null)
		{
			return;
		}
		_escortingVillagers.Add(party);
		try
		{
			party.SetMoveEscortParty(MobileParty.MainParty, MobileParty.NavigationType.Default, isTargetingPort: false);
		}
		catch
		{
		}
	}

	public void StopEscortingVillager(MobileParty party)
	{
		if (party == null)
		{
			return;
		}
		_escortingVillagers.Remove(party);
		try
		{
			Settlement settlement = (party.PartyComponent as VillagerPartyComponent)?.Village?.Settlement;
			if (settlement != null)
			{
				party.SetMoveGoToSettlement(settlement, MobileParty.NavigationType.Default, isTargetingThePort: false);
			}
		}
		catch
		{
		}
	}

	public void SetLastSparringResult(bool playerWon)
	{
		_hasPendingSparringResult = true;
		_lastSparringResultPlayerWon = playerWon;
	}

	public void AddAmbassadorCompanionSlot()
	{
		_ambassadorCompanionSlotBonus++;
	}

	public HomesteadBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddGameMenusAndDialogs);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			RebuildHomesteadPartyRegistry("session launch");
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			HomesteadForgeContext.RemoveInvalidCraftingOrders();
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			RebuildPatrolPartyRegistry("session launch");
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			RepairInvalidHomesteadLeaders();
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			RemoveInvalidHomesteadParties("session launch");
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			RepairUnavailableHomesteadLeaders("session launch");
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			EnsureAllHomesteadsStayAnchored("session launch");
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			RecalculateAllHomesteadSceneValues();
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			EnsureMasteryTitles();
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			RecoverGraduatedApprentices();
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			RepairHeadmenAndApprentices();
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			RepairConvertedNotableClans();
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			RetroactivelyGrantApprenticeBonusesForAllHomesteads();
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			NormalizeGarrisonHeroCounts("session launch");
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			Homestead.SetGameTextsForMenus();
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate
		{
			_buildingMaterialIdsByTier.Clear();
		});
		CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, OnSaveOver);
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, CheckHomesteadHourlyTick);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, CheckHomesteadPassiveHourlyTick);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, CheckHomesteadDailyTick);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
		CampaignEvents.OnCraftingOrderCompletedEvent.AddNonSerializedListener(this, OnCraftingOrderCompleted);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, ConvertedAmbassadorDailyTick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, WealthRaidDailyTick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, ConvertedAmbassadorHourlyTick);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, delegate
		{
			if (_pendingPlayerEncounterFinish && Mission.Current == null)
			{
				_pendingPlayerEncounterFinish = false;
				try
				{
					PlayerEncounter.Finish();
				}
				catch
				{
				}
			}
		});
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		CampaignEvents.OnPlayerMetHeroEvent.AddNonSerializedListener(this, OnPlayerMetHero);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnHsrSettlementEntered);
		CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(this, delegate(Hero hero1, Hero hero2, KillCharacterAction.KillCharacterActionDetail detail, bool someBool)
		{
			foreach (Homestead value in HomesteadMobileParties.Values)
			{
				if (value.Leader == hero1)
				{
					value.PartyLeaderDied();
					break;
				}
			}
		});
		CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, delegate(MapEvent mapEvent)
		{
			CurrentHomestead = null;
			HomesteadBattleContext.ClearIfMatches(mapEvent, "player battle end");
			RepairUnavailableHomesteadLeaders("player battle end");
		});
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, delegate(MapEvent mapEvent)
		{
			HomesteadBattleContext.ClearIfMatches(mapEvent, "map event ended");
			foreach (Homestead item in PatrolMobileParties.Values.ToList())
			{
				try
				{
					item.OnPatrolBattleEnded(mapEvent);
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadBehavior", "OnPatrolBattleEnded failed for '" + (item?.Name?.ToString() ?? "null") + "': " + ex.Message);
				}
			}
			if (HasArmsMasterMasteryUnlocked && mapEvent.WinningSide == BattleSideEnum.Defender)
			{
				foreach (Homestead item2 in HomesteadMobileParties.Values.ToList())
				{
					if (item2?.MobileParty != null)
					{
						bool flag = false;
						foreach (MapEventParty item3 in mapEvent.PartiesOnSide(BattleSideEnum.Defender))
						{
							if (item3?.Party?.MobileParty == item2.MobileParty)
							{
								flag = true;
								break;
							}
						}
						if (flag)
						{
							try
							{
								item2.OnGarrisonDefensiveBattleEnded(mapEvent);
							}
							catch (Exception ex2)
							{
								TraceLogger.Write("HomesteadBehavior", "OnGarrisonDefensiveBattleEnded failed for '" + (item2?.Name?.ToString() ?? "null") + "': " + ex2.Message);
							}
						}
					}
				}
			}
		});
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, delegate(MapEvent mapEvent, PartyBase attacker, PartyBase defender)
		{
			HomesteadBattleContext.TryPrepare(mapEvent, attacker, defender);
			if (defender.MobileParty != null)
			{
				Homestead homestead = Homestead.GetFor(defender.MobileParty);
				bool flag = false;
				MobileParty mobileParty = null;
				if (homestead == null && CurrentHomestead != null)
				{
					MobileParty mobileParty2 = CurrentHomestead.MobileParty;
					if (defender.MobileParty == MobileParty.MainParty && mobileParty2 != null && mobileParty2.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D) < 5f)
					{
						homestead = CurrentHomestead;
						mobileParty = mobileParty2;
						flag = true;
						TraceLogger.Write("HomesteadBehavior", $"MapEventStarted: MainParty is defender — falling back to CurrentHomestead '{CurrentHomestead.Name}' for encounter join.");
					}
				}
				if (homestead != null)
				{
					TraceLogger.Write("HomesteadBehavior", string.Format("MapEventStarted: homestead '{0}' is involved — attacker='{1}' defender='{2}' CurrentHomestead='{3}' isColocatedFallback={4}", homestead.Name, attacker.MobileParty?.StringId ?? "null", defender.MobileParty?.StringId ?? "null", CurrentHomestead?.Name?.ToString() ?? "null", flag));
					TryRecallPatrolToDefendHomestead(homestead, defender);
					if (CurrentHomestead == homestead)
					{
						PartyBase.MainParty.MapEventSide = defender.MapEventSide;
						if (flag && mobileParty != null)
						{
							HomesteadBattleContext.TryPrepare(mapEvent, attacker, mobileParty.Party);
							if (mobileParty.MapEvent == null)
							{
								mobileParty.Party.MapEventSide = defender.MapEventSide;
								TraceLogger.Write("HomesteadBehavior", "MapEventStarted: Pulled homestead party '" + mobileParty.StringId + "' into player's MapEvent.");
							}
							else
							{
								TraceLogger.Write("HomesteadBehavior", "MapEventStarted: HomesteadBattleContext prepared for '" + mobileParty.StringId + "' (already in MapEvent — not re-assigning MapEventSide).");
							}
						}
						PartyBase partyBase = (flag ? mobileParty : homestead.MobileParty)?.Party ?? PartyBase.MainParty;
						if (PlayerEncounter.Current == null)
						{
							try
							{
								PlayerEncounter.Start();
								PlayerEncounter.Current.SetupFields(attacker, partyBase);
								TraceLogger.Write("HomesteadBehavior", "MapEventStarted: Created PlayerEncounter for homestead attack (attacker='" + (attacker.MobileParty?.StringId ?? "null") + "' defender='" + (partyBase.MobileParty?.StringId ?? "MainParty") + "').");
							}
							catch (Exception ex)
							{
								TraceLogger.Write("HomesteadBehavior", "MapEventStarted: Failed to create PlayerEncounter: " + ex.Message);
							}
						}
						else
						{
							try
							{
								PlayerEncounter.Current.SetupFields(attacker, partyBase);
								TraceLogger.Write("HomesteadBehavior", "MapEventStarted: Overrode existing PlayerEncounter defender to homestead party (attacker='" + (attacker.MobileParty?.StringId ?? "null") + "' defender='" + (partyBase.MobileParty?.StringId ?? "MainParty") + "').");
							}
							catch (Exception ex2)
							{
								TraceLogger.Write("HomesteadBehavior", "MapEventStarted: Failed to override PlayerEncounter defender: " + ex2.Message);
							}
						}
						if (PlayerEncounter.Current != null)
						{
							PlayerEncounter.Current.IsPlayerWaiting = true;
						}
						string text = Campaign.Current.CurrentMenuContext?.GameMenu?.StringId;
						if (text != "homestead_menu_wait_waiting" && text != "encounter" && text != "homestead_menu_encounter")
						{
							GameMenu.ActivateGameMenu("homestead_menu_encounter");
						}
						TraceLogger.Write("HomesteadBehavior", string.Format("MapEventStarted: Joining defense of homestead '{0}' (currentMenu='{1}').", homestead.Name, text ?? "null"));
					}
				}
			}
		});
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, delegate(MenuCallbackArgs args)
		{
			string stringId = args.MenuContext.GameMenu.StringId;
			if (SparringPending && Mission.Current == null)
			{
				if (_sparSelectPhase == 0)
				{
					LaunchSparringMission();
				}
			}
			else
			{
				switch (stringId)
				{
				case "homestead_menu_main":
				case "homestead_menu_manage_main":
				case "homestead_menu_wait_waiting":
					CurrentHomestead = ResolveCurrentHomesteadForMenu(stringId + " opened");
					if (CurrentHomestead == null)
					{
						if (PlayerEncounter.Current != null)
						{
							PlayerEncounter.Finish();
						}
						else
						{
							GameMenu.ExitToLast();
						}
					}
					else
					{
						Homestead.SetGameTextsForMenus();
						if (_pendingForge)
						{
							switch (stringId)
							{
							case "homestead_menu_main":
							case "town":
							case "homestead_menu_notables":
								_pendingForge = false;
								OpenHomesteadForge();
								break;
							}
						}
					}
					break;
				}
			}
		});
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("CurrentHomestead", ref currentHomestead);
		dataStore.SyncData("HomesteadMobileParties", ref HomesteadMobileParties);
		dataStore.SyncData("TutorialStage", ref TutorialStage);
		dataStore.SyncData("CaravanCooldowns", ref _caravanCooldowns);
		if (_caravanCooldowns == null)
		{
			_caravanCooldowns = new Dictionary<string, int>();
		}
		dataStore.SyncData("HasAdoptedDog", ref _hasAdoptedDog);
		dataStore.SyncData("AdoptedDogName", ref _adoptedDogName);
		dataStore.SyncData("AdoptedDogHomesteadId", ref _adoptedDogHomesteadId);
		dataStore.SyncData("AdoptedDogMaterialIndex", ref _adoptedDogMaterialIndex);
		dataStore.SyncData("NotableFavorsDone", ref _notableFavorsDone);
		if (_notableFavorsDone == null)
		{
			_notableFavorsDone = new Dictionary<string, int>();
		}
		if (_lastRaidDayByHomestead == null)
		{
			_lastRaidDayByHomestead = new Dictionary<string, int>();
		}
		dataStore.SyncData("NotableApprenticeCount", ref _notableApprenticeCount);
		if (_notableApprenticeCount == null)
		{
			_notableApprenticeCount = new Dictionary<string, int>();
		}
		dataStore.SyncData("ConvertedNotableRoles", ref ConvertedNotableRoles);
		if (ConvertedNotableRoles == null)
		{
			ConvertedNotableRoles = new Dictionary<string, string>();
		}
		dataStore.SyncData("HasHoundmasterKnockdownUnlocked", ref HasHoundmasterKnockdownUnlocked);
		dataStore.SyncData("HasAmbassadorTactfulIntroductionUnlocked", ref HasAmbassadorTactfulIntroductionUnlocked);
		dataStore.SyncData("HasMarketLadyTradeDiscountUnlocked", ref HasMarketLadyTradeDiscountUnlocked);
		dataStore.SyncData("HasArmsMasterMasteryUnlocked", ref HasArmsMasterMasteryUnlocked);
		dataStore.SyncData("HasStableMasterMasteryUnlocked", ref HasStableMasterMasteryUnlocked);
		dataStore.SyncData("HasTavernKeeperBonusActive", ref HasTavernKeeperBonusActive);
		dataStore.SyncData("HasMasterSmithUpgradeUnlocked", ref HasMasterSmithUpgradeUnlocked);
		dataStore.SyncData("RaceFlagsHidden", ref RaceFlagsHidden);
		dataStore.SyncData("AmbassadorCompanionSlotBonus", ref _ambassadorCompanionSlotBonus);
		dataStore.SyncData("SettlementSmithUpgrades", ref _settlementSmithUpgrades);
		if (_settlementSmithUpgrades == null)
		{
			_settlementSmithUpgrades = new Dictionary<string, SettlementSmithUpgradeRecord>();
		}
		dataStore.SyncData("ClanTournamentEntries", ref _clanTournamentEntries);
		dataStore.SyncData("ClanTournamentWins", ref _clanTournamentWins);
		dataStore.SyncData("ClanTournamentNames", ref _clanTournamentNames);
		if (_clanTournamentEntries == null)
		{
			_clanTournamentEntries = new Dictionary<string, int>();
		}
		if (_clanTournamentWins == null)
		{
			_clanTournamentWins = new Dictionary<string, int>();
		}
		if (_clanTournamentNames == null)
		{
			_clanTournamentNames = new Dictionary<string, string>();
		}
		dataStore.SyncData("ClanSparringEntries", ref _clanSparringEntries);
		dataStore.SyncData("ClanSparringWins", ref _clanSparringWins);
		dataStore.SyncData("ClanSparringNames", ref _clanSparringNames);
		dataStore.SyncData("RaceTrackStats", ref _trackStats);
		if (_clanSparringEntries == null)
		{
			_clanSparringEntries = new Dictionary<string, int>();
		}
		if (_clanSparringWins == null)
		{
			_clanSparringWins = new Dictionary<string, int>();
		}
		if (_clanSparringNames == null)
		{
			_clanSparringNames = new Dictionary<string, string>();
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		if (detail == QuestBase.QuestCompleteDetails.Success && quest is HomesteadApprenticeQuest && quest.QuestGiver != null)
		{
			string stringId = quest.QuestGiver.StringId;
			if (!_notableApprenticeCount.ContainsKey(stringId))
			{
				_notableApprenticeCount[stringId] = 0;
			}
			_notableApprenticeCount[stringId]++;
		}
	}

	private void OnPlayerMetHero(Hero metHero)
	{
		if (HasAmbassadorTactfulIntroductionUnlocked && (metHero.IsLord || metHero.IsNotable))
		{
			ChangeRelationAction.ApplyPlayerRelation(metHero, 5);
			Utils.PrintLocalizedMessage("homestead_ambassador_met_bonus", "Thanks to your Ambassador's tactful introductions, your relation with {HERO} has increased.", 100f, 255f, 100f, ("HERO", metHero.Name.ToString()));
		}
	}

	public static bool IsAIInfluenceInstalled()
	{
		if (_aiInfluenceInstalled.HasValue)
		{
			return _aiInfluenceInstalled.Value;
		}
		bool flag = false;
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			if (string.Equals(assemblies[i].GetName().Name, "AIInfluence", StringComparison.Ordinal))
			{
				flag = true;
				break;
			}
		}
		_aiInfluenceInstalled = flag;
		return flag;
	}

	private void StartHomesteadTalk(Homestead homestead, Hero notable)
	{
		if (homestead != null && notable != null)
		{
			PendingTalkHero = notable;
			CustomMissions.StartHomesteadMission(homestead);
		}
	}

	public Hero? GetConversationNotable(out string title)
	{
		title = "";
		Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
		Homestead homestead = CurrentHomestead;
		if (oneToOneConversationHero == null || homestead == null)
		{
			return null;
		}
		if (oneToOneConversationHero == homestead.HoundMasterHero)
		{
			title = Utils.GetLocalizedString("{=homestead_role_houndmaster}Hound Master");
			return oneToOneConversationHero;
		}
		if (oneToOneConversationHero == homestead.MarketLadyHero)
		{
			title = Utils.GetLocalizedString("{=homestead_role_marketlady}Market Lady");
			return oneToOneConversationHero;
		}
		if (oneToOneConversationHero == homestead.AmbassadorHero)
		{
			title = Utils.GetLocalizedString("{=homestead_role_ambassador}Ambassador");
			return oneToOneConversationHero;
		}
		if (oneToOneConversationHero == homestead.ArmsMasterHero)
		{
			title = Utils.GetLocalizedString("{=homestead_role_armsmaster}Arms Master");
			return oneToOneConversationHero;
		}
		if (oneToOneConversationHero == homestead.TavernKeeperHero)
		{
			title = Utils.GetLocalizedString("{=homestead_role_tavernkeeper}Tavern Keeper");
			return oneToOneConversationHero;
		}
		if (oneToOneConversationHero == homestead.MasterSmithHero)
		{
			title = Utils.GetLocalizedString("{=homestead_role_mastersmith}Master Smith");
			return oneToOneConversationHero;
		}
		if (oneToOneConversationHero == homestead.StableMasterHero)
		{
			title = Utils.GetLocalizedString("{=homestead_role_stablemaster}Stable Master");
			return oneToOneConversationHero;
		}
		return null;
	}

	private static List<Hero> GetStationableCompanions()
	{
		List<Hero> list = new List<Hero>();
		foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character != null && character.IsHero && !item.Character.IsPlayerCharacter && item.Character.HeroObject != null)
			{
				list.Add(item.Character.HeroObject);
			}
		}
		return list;
	}

	internal static IEnumerable<Hero> GetHomesteadRoleHeroes(Homestead hs)
	{
		if (hs.Leader != null)
		{
			yield return hs.Leader;
		}
		if (hs.HoundMasterHero != null)
		{
			yield return hs.HoundMasterHero;
		}
		if (hs.MarketLadyHero != null)
		{
			yield return hs.MarketLadyHero;
		}
		if (hs.AmbassadorHero != null)
		{
			yield return hs.AmbassadorHero;
		}
		if (hs.ArmsMasterHero != null)
		{
			yield return hs.ArmsMasterHero;
		}
		if (hs.TavernKeeperHero != null)
		{
			yield return hs.TavernKeeperHero;
		}
		if (hs.MasterSmithHero != null)
		{
			yield return hs.MasterSmithHero;
		}
		if (hs.StableMasterHero != null)
		{
			yield return hs.StableMasterHero;
		}
	}

	private static List<Hero> GetRetrievableHomesteadHeroes(Homestead hs)
	{
		HashSet<Hero> hashSet = new HashSet<Hero>(GetHomesteadRoleHeroes(hs));
		List<Hero> list = new List<Hero>();
		foreach (TroopRosterElement item in hs.MobileParty.MemberRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character != null && character.IsHero)
			{
				Hero heroObject = item.Character.HeroObject;
				if (heroObject != null && !hashSet.Contains(heroObject))
				{
					list.Add(heroObject);
				}
			}
		}
		return list;
	}

	private void ShowStationCompanionsInquiry(Homestead hs)
	{
		ShowHeroTransferInquiry(new TextObject("{=homestead_station_title}Station Companions").ToString(), new TextObject("{=homestead_station_desc}Select companions to leave at {HOMESTEAD}.").SetTextVariable("HOMESTEAD", hs.Name).ToString(), GetStationableCompanions(), hs.MobileParty);
	}

	private void ShowRetrieveCompanionsInquiry(Homestead hs)
	{
		ShowHeroTransferInquiry(new TextObject("{=homestead_retrieve_title}Retrieve Companions").ToString(), new TextObject("{=homestead_retrieve_desc}Select companions to take from {HOMESTEAD} into your party.").SetTextVariable("HOMESTEAD", hs.Name).ToString(), GetRetrievableHomesteadHeroes(hs), MobileParty.MainParty);
	}

	private void ShowHeroTransferInquiry(string title, string description, List<Hero> heroes, MobileParty destination)
	{
		if (heroes == null || heroes.Count == 0 || destination == null)
		{
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (Hero hero in heroes)
		{
			list.Add(new InquiryElement(hero.CharacterObject, hero.Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(hero.CharacterObject)), isEnabled: true, ""));
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, description, list, isExitShown: true, 0, heroes.Count, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate(List<InquiryElement> selected)
		{
			foreach (InquiryElement item in selected)
			{
				Hero heroObject = ((CharacterObject)item.Identifier).HeroObject;
				if (heroObject != null)
				{
					AddHeroToPartyAction.Apply(heroObject, destination);
				}
			}
		}, null), pauseGameActiveState: true);
	}

	public HomesteadNotableFavorQuest? GetActiveFavorQuest(Hero notable)
	{
		if (notable == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadNotableFavorQuest result && quest.IsOngoing && quest.QuestGiver == notable)
			{
				return result;
			}
		}
		return null;
	}

	public bool TryGetActiveFavor(Hero notable, out string item, out int count)
	{
		item = "";
		count = 0;
		HomesteadNotableFavorQuest activeFavorQuest = GetActiveFavorQuest(notable);
		if (activeFavorQuest == null || activeFavorQuest.Item == null)
		{
			return false;
		}
		activeFavorQuest.RefreshProgress();
		item = activeFavorQuest.Item.StringId;
		count = activeFavorQuest.RequiredCount;
		return true;
	}

	private ItemObject? GetFavorItemObject(string itemId)
	{
		if (!string.IsNullOrEmpty(itemId))
		{
			return Game.Current?.ObjectManager?.GetObject<ItemObject>(itemId);
		}
		return null;
	}

	public bool IsFavorReady(Hero notable)
	{
		return GetActiveFavorQuest(notable)?.IsReady() ?? false;
	}

	public bool HasFavorPartial(Hero notable)
	{
		return GetActiveFavorQuest(notable)?.HasPartialInInventory() ?? false;
	}

	public void PartialDeliverFavor(Hero notable)
	{
		HomesteadNotableFavorQuest activeFavorQuest = GetActiveFavorQuest(notable);
		if (activeFavorQuest != null)
		{
			int count = activeFavorQuest.PlayerCarryCount();
			activeFavorQuest.PartialDeliver();
			SetFavorTextVariables(activeFavorQuest.Item?.StringId ?? "", count);
			TextObject textObject = new TextObject("{=homestead_favor_partial_delivered}Delivered {FAVOR_COUNT} {FAVOR_ITEM} — {REMAINING} more needed.");
			textObject.SetTextVariable("REMAINING", activeFavorQuest.RemainingNeeded);
			Utils.PrintDebugMessage(textObject.ToString(), 180f, 200f, 240f);
		}
	}

	public void CancelNotableFavor(Hero notable)
	{
		GetActiveFavorQuest(notable)?.CancelFavor();
	}

	public string SetForcedOfferType(string? typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName) || typeName.Equals("off", StringComparison.OrdinalIgnoreCase) || typeName.Equals("random", StringComparison.OrdinalIgnoreCase) || typeName.Equals("clear", StringComparison.OrdinalIgnoreCase))
		{
			_forcedOfferType = null;
			return "Homestead quest offer override cleared — offers are random again.";
		}
		if (Enum.TryParse<PendingOfferType>(typeName, ignoreCase: true, out var result))
		{
			_forcedOfferType = result;
			return $"Next homestead quest offer forced to '{result}'. (Talk to a notable and pick \"Is there anything you need?\")";
		}
		return "Unknown type '" + typeName + "'. Valid: " + string.Join(", ", Enum.GetNames(typeof(PendingOfferType))) + ", off.";
	}

	public HomesteadPackageDeliveryQuest? GetActiveDeliveryQuest(Hero sender)
	{
		if (sender == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadPackageDeliveryQuest result && quest.IsOngoing && quest.QuestGiver == sender)
			{
				return result;
			}
		}
		return null;
	}

	public HomesteadPackageDeliveryQuest? GetDeliveryQuestForRecipient(Hero recipient)
	{
		if (recipient == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		string stringId = recipient.StringId;
		TraceLogger.Write("HomesteadBehavior", $"GetDeliveryQuestForRecipient: looking for '{recipient.Name}' (StringId={stringId})");
		HomesteadPackageDeliveryQuest homesteadPackageDeliveryQuest = null;
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (!(quest is HomesteadPackageDeliveryQuest homesteadPackageDeliveryQuest2))
			{
				continue;
			}
			bool flag = homesteadPackageDeliveryQuest2.Recipient == recipient;
			bool flag2 = stringId != null && homesteadPackageDeliveryQuest2.Recipient?.StringId == stringId;
			TraceLogger.Write("HomesteadBehavior", $"  Quest {homesteadPackageDeliveryQuest2.StringId}: IsOngoing={quest.IsOngoing} " + $"Recipient='{homesteadPackageDeliveryQuest2.Recipient?.Name}' (StringId={homesteadPackageDeliveryQuest2.Recipient?.StringId}) " + $"refMatch={flag} idMatch={flag2} HasPackage={homesteadPackageDeliveryQuest2.PlayerHasPackage()}");
			if (quest.IsOngoing)
			{
				if (flag)
				{
					return homesteadPackageDeliveryQuest2;
				}
				if (flag2)
				{
					homesteadPackageDeliveryQuest = homesteadPackageDeliveryQuest2;
				}
			}
		}
		if (homesteadPackageDeliveryQuest != null)
		{
			TraceLogger.Write("HomesteadBehavior", $"GetDeliveryQuestForRecipient: using StringId fallback for '{homesteadPackageDeliveryQuest.Recipient?.Name}'");
		}
		return homesteadPackageDeliveryQuest;
	}

	public bool GenerateDeliveryOffer(Hero sender)
	{
		Homestead homestead = CurrentHomestead;
		if (homestead?.MobileParty == null)
		{
			return false;
		}
		Vec2 getPosition2D = homestead.MobileParty.GetPosition2D;
		MBList<Hero> mBList = new MBList<Hero>();
		foreach (Settlement item in Settlement.All)
		{
			if (!item.IsVillage || item.IsUnderRaid || (item.Village != null && item.Village.VillageState == Village.VillageStates.Looted) || (item.GatePosition.ToVec2() - getPosition2D).Length > 50f || item.Notables == null)
			{
				continue;
			}
			foreach (Hero notable in item.Notables)
			{
				if (notable == null || !notable.IsAlive || notable.IsPrisoner || notable == sender)
				{
					continue;
				}
				bool flag = false;
				if (HomesteadMobileParties != null)
				{
					foreach (Homestead value in HomesteadMobileParties.Values)
					{
						if (notable == value.HoundMasterHero || notable == value.MarketLadyHero || notable == value.AmbassadorHero || notable == value.ArmsMasterHero || notable == value.TavernKeeperHero || notable == value.MasterSmithHero)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					mBList.Add(notable);
				}
			}
		}
		if (mBList.IsEmpty())
		{
			return false;
		}
		_pendingDeliveryRecipient = mBList[MBRandom.RandomInt(mBList.Count)];
		string objectName = DeliveryItemPool[MBRandom.RandomInt(DeliveryItemPool.Length)];
		_pendingDeliveryPackage = Game.Current?.ObjectManager?.GetObject<ItemObject>(objectName);
		if (_pendingDeliveryPackage == null)
		{
			_pendingDeliveryPackage = Game.Current?.ObjectManager?.GetObject<ItemObject>("pottery");
		}
		if (_pendingDeliveryPackage == null)
		{
			return false;
		}
		SetDeliveryTextVariables(sender, _pendingDeliveryRecipient, _pendingDeliveryPackage);
		return true;
	}

	private static void SetDeliveryTextVariables(Hero sender, Hero recipient, ItemObject pkg)
	{
		MBTextManager.SetTextVariable("PKG_ITEM", pkg.Name);
		MBTextManager.SetTextVariable("PKG_RECIPIENT", recipient.Name);
		TextObject text = ((recipient.HomeSettlement != null) ? recipient.HomeSettlement.Name : recipient.Name);
		MBTextManager.SetTextVariable("PKG_SETTLEMENT", text);
		MBTextManager.SetTextVariable("PKG_SENDER", sender.Name);
	}

	public HomesteadRaidEventQuest? GetActiveRaidQuestForHomestead(Homestead? homestead)
	{
		if (homestead == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadRaidEventQuest homesteadRaidEventQuest && quest.IsOngoing && homesteadRaidEventQuest.Homestead == homestead)
			{
				return homesteadRaidEventQuest;
			}
		}
		return null;
	}

	public HomesteadArmsMasterRecruitQuest? GetActiveArmsMasterRecruitQuest(Homestead? homestead)
	{
		if (homestead == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadArmsMasterRecruitQuest homesteadArmsMasterRecruitQuest && quest.IsOngoing && homesteadArmsMasterRecruitQuest.Homestead == homestead)
			{
				return homesteadArmsMasterRecruitQuest;
			}
		}
		return null;
	}

	public HomesteadArmsMasterRecruitQuest? GetWonArmsMasterQuestForSettlement(Settlement? settlement)
	{
		if (settlement == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadArmsMasterRecruitQuest homesteadArmsMasterRecruitQuest && quest.IsOngoing && homesteadArmsMasterRecruitQuest.TournamentWon && homesteadArmsMasterRecruitQuest.TournamentSettlement == settlement)
			{
				return homesteadArmsMasterRecruitQuest;
			}
		}
		return null;
	}

	public HomesteadMasterSmithRecruitQuest? GetActiveMasterSmithRecruitQuest(Homestead? homestead)
	{
		if (homestead == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadMasterSmithRecruitQuest homesteadMasterSmithRecruitQuest && quest.IsOngoing && homesteadMasterSmithRecruitQuest.Homestead == homestead)
			{
				return homesteadMasterSmithRecruitQuest;
			}
		}
		return null;
	}

	public HomesteadStableMasterRecruitQuest? GetActiveStableMasterRecruitQuest(Homestead? homestead)
	{
		if (homestead == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadStableMasterRecruitQuest homesteadStableMasterRecruitQuest && quest.IsOngoing && homesteadStableMasterRecruitQuest.Homestead == homestead)
			{
				return homesteadStableMasterRecruitQuest;
			}
		}
		return null;
	}

	public HomesteadHeadmanTrustQuest? GetActiveHeadmanTrustQuest(Homestead? homestead)
	{
		if (homestead == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadHeadmanTrustQuest homesteadHeadmanTrustQuest && quest.IsOngoing && homesteadHeadmanTrustQuest.Homestead == homestead)
			{
				return homesteadHeadmanTrustQuest;
			}
		}
		return null;
	}

	public HomesteadHeadmanTrustQuest? GetHeadmanTrustQuestForHeadman(Hero? hero)
	{
		if (hero == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadHeadmanTrustQuest homesteadHeadmanTrustQuest && quest.IsOngoing && homesteadHeadmanTrustQuest.Headman == hero)
			{
				return homesteadHeadmanTrustQuest;
			}
		}
		return null;
	}

	public void EnsureHeadmanHasIssue(Hero? headman)
	{
		EnsureHeroHasVanillaIssue(headman, "EnsureHeadmanHasIssue");
	}

	public void EnsureRulerHasIssue(Hero? ruler)
	{
		EnsureHeroHasVanillaIssue(ruler, "EnsureRulerHasIssue");
	}

	private void EnsureHeroHasVanillaIssue(Hero? hero, string logContext)
	{
		try
		{
			if (hero == null || !hero.IsAlive)
			{
				return;
			}
			IssueManager issueManager = Campaign.Current?.IssueManager;
			if (issueManager == null || hero.Issue != null)
			{
				return;
			}
			List<PotentialIssueData> list = issueManager.CheckForIssues(hero);
			if (list != null)
			{
				List<PotentialIssueData> list2 = list.Where((PotentialIssueData p) => p.IsValid).ToList();
				if (list2.Count != 0)
				{
					issueManager.CreateNewIssue(list2[MBRandom.RandomInt(list2.Count)], hero);
					TraceLogger.Write("HomesteadBehavior", string.Format("{0}: gave '{1}' a vanilla issue ('{2}') ", logContext, hero.Name, hero.Issue?.GetType().Name ?? "?") + $"from {list2.Count} eligible candidate(s) ({list.Count} checked).");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", logContext + " failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public HomesteadLandPatentQuest? GetActiveLandPatentQuest(Homestead? homestead)
	{
		if (homestead == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadLandPatentQuest homesteadLandPatentQuest && quest.IsOngoing && homesteadLandPatentQuest.Homestead == homestead)
			{
				return homesteadLandPatentQuest;
			}
		}
		return null;
	}

	public HomesteadLandPatentQuest? GetLandPatentQuestForClanMember(Hero? hero)
	{
		if (hero?.Clan == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadLandPatentQuest homesteadLandPatentQuest && quest.IsOngoing && homesteadLandPatentQuest.OwningClan == hero.Clan)
			{
				return homesteadLandPatentQuest;
			}
		}
		return null;
	}

	public void EnsureLandPatentQuest(Homestead? homestead)
	{
		try
		{
			if (homestead != null && !homestead.IsRetiredOrDestroyed && homestead.Tier == 2 && !homestead.Tier2ApprovalGranted && GetActiveLandPatentQuest(homestead) == null)
			{
				Settlement settlement = homestead.FindNearestTown();
				Clan clan = settlement?.OwnerClan;
				if (settlement != null && clan != null && clan.Leader != null && clan != Clan.PlayerClan)
				{
					new HomesteadLandPatentQuest("homestead_land_patent_" + (homestead.MobileParty?.StringId ?? Guid.NewGuid().ToString()), clan, settlement, homestead).StartQuest();
					TraceLogger.Write("HomesteadBehavior", $"EnsureLandPatentQuest: started for '{homestead.Name}' → clan '{clan.Name}' of town '{settlement.Name}'.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "EnsureLandPatentQuest failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public HomesteadSettlementCharterQuest? GetActiveSettlementCharterQuest(Homestead? homestead)
	{
		if (homestead == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadSettlementCharterQuest homesteadSettlementCharterQuest && quest.IsOngoing && homesteadSettlementCharterQuest.Homestead == homestead)
			{
				return homesteadSettlementCharterQuest;
			}
		}
		return null;
	}

	public HomesteadSettlementCharterQuest? GetSettlementCharterQuestForRuler(Hero? hero)
	{
		if (hero == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadSettlementCharterQuest homesteadSettlementCharterQuest && quest.IsOngoing && homesteadSettlementCharterQuest.Ruler == hero)
			{
				return homesteadSettlementCharterQuest;
			}
		}
		return null;
	}

	public void EnsureSettlementCharterQuest(Homestead? homestead)
	{
		try
		{
			if (homestead != null && !homestead.IsRetiredOrDestroyed && homestead.SettlementUpgradeReady && !homestead.SettlementCharterGranted && GetActiveSettlementCharterQuest(homestead) == null && !homestead.CanPlayerSelfGrantCharter())
			{
				Settlement settlement = homestead.FindNearestKingdomSettlement();
				Kingdom kingdom = settlement?.OwnerClan?.Kingdom;
				if (settlement != null && kingdom != null && kingdom.Leader != null && kingdom.RulingClan != Clan.PlayerClan)
				{
					new HomesteadSettlementCharterQuest("homestead_charter_" + (homestead.MobileParty?.StringId ?? Guid.NewGuid().ToString()), kingdom, settlement, homestead).StartQuest();
					TraceLogger.Write("HomesteadBehavior", $"EnsureSettlementCharterQuest: started for '{homestead.Name}' → ruler '{kingdom.Leader.Name}' of '{kingdom.Name}'.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "EnsureSettlementCharterQuest failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public HomesteadAngryVillagersQuest? GetActiveAngryVillagersQuest(Homestead? homestead)
	{
		if (homestead == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadAngryVillagersQuest homesteadAngryVillagersQuest && quest.IsOngoing && homesteadAngryVillagersQuest.Homestead == homestead)
			{
				return homesteadAngryVillagersQuest;
			}
		}
		return null;
	}

	public void StartAngryVillagersRaid(Homestead? homestead, Hero? headman)
	{
		try
		{
			if (homestead?.MobileParty != null && !homestead.IsRetiredOrDestroyed)
			{
				if (headman == null)
				{
					headman = Homestead.FindHeadmanOfVillage(homestead.FindNearestVillage());
				}
				if (headman != null && GetActiveAngryVillagersQuest(homestead) == null)
				{
					int num = homestead.MobileParty.MemberRoster?.TotalManCount ?? 5;
					int num2 = Math.Max(15, num * 3);
					Settlement settlement = homestead.FindNearestVillage();
					CultureObject troopCulture = settlement?.Culture;
					Settlement originVillage = ((headman != null && headman.HomeSettlement?.IsVillage == true) ? headman.HomeSettlement : settlement);
					new HomesteadAngryVillagersQuest("homestead_angry_villagers_" + (homestead.MobileParty?.StringId ?? Guid.NewGuid().ToString()) + "_" + CampaignTime.Now.ToMilliseconds, headman, homestead, num2, troopCulture, clanEnforcement: false, originVillage).StartQuest();
					TraceLogger.Write("HomesteadBehavior", $"StartAngryVillagersRaid: {num2} villagers on '{homestead.Name}' (headman '{headman.Name}').");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "StartAngryVillagersRaid failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public void StartClanEnforcementRaid(Homestead? homestead, Clan? clan)
	{
		try
		{
			if (homestead?.MobileParty == null || homestead.IsRetiredOrDestroyed)
			{
				return;
			}
			Hero hero = clan?.Leader;
			if (clan == null || hero == null || GetActiveAngryVillagersQuest(homestead) != null)
			{
				return;
			}
			int num = homestead.MobileParty.MemberRoster?.TotalManCount ?? 5;
			int num2 = Math.Max(15, num * 3);
			Settlement nearestTown = homestead.FindNearestTown();
			Settlement settlement = null;
			if (nearestTown != null)
			{
				settlement = (from s in Campaign.Current?.Settlements
					where s.IsVillage && s.Village?.Bound == nearestTown
					orderby s.GatePosition.ToVec2().Distance(homestead.MobileParty.GetPosition2D)
					select s).FirstOrDefault();
				if (settlement == null)
				{
					settlement = (from s in Campaign.Current?.Settlements
						where s.IsVillage && s.Culture == clan.Culture
						orderby s.GatePosition.ToVec2().Distance(homestead.MobileParty.GetPosition2D)
						select s).FirstOrDefault();
				}
			}
			new HomesteadAngryVillagersQuest("homestead_enforcers_" + (homestead.MobileParty?.StringId ?? Guid.NewGuid().ToString()) + "_" + CampaignTime.Now.ToMilliseconds, hero, homestead, num2, clan.Culture, clanEnforcement: true, settlement).StartQuest();
			TraceLogger.Write("HomesteadBehavior", $"StartClanEnforcementRaid: {num2} men-at-arms of '{clan.Name}' on '{homestead.Name}'.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "StartClanEnforcementRaid failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public void EnsureHeadmanTrustQuest(Homestead? homestead)
	{
		try
		{
			if (homestead != null && !homestead.IsRetiredOrDestroyed && homestead.Tier == 1 && !homestead.Tier1ApprovalGranted && GetActiveHeadmanTrustQuest(homestead) == null)
			{
				Settlement settlement = homestead.FindNearestVillage();
				Hero hero = Homestead.FindHeadmanOfVillage(settlement);
				if (settlement != null && hero != null)
				{
					new HomesteadHeadmanTrustQuest("homestead_headman_trust_" + (homestead.MobileParty?.StringId ?? Guid.NewGuid().ToString()), hero, settlement, homestead).StartQuest();
					TraceLogger.Write("HomesteadBehavior", $"EnsureHeadmanTrustQuest: started for '{homestead.Name}' → headman '{hero.Name}' of '{settlement.Name}'.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "EnsureHeadmanTrustQuest failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public HomesteadMasterSmithRecruitQuest? GetMasterSmithQuestAwaitingPitch()
	{
		if (Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadMasterSmithRecruitQuest homesteadMasterSmithRecruitQuest && quest.IsOngoing && !homesteadMasterSmithRecruitQuest.PitchHeard)
			{
				return homesteadMasterSmithRecruitQuest;
			}
		}
		return null;
	}

	public HomesteadMasterSmithRecruitQuest? GetMasterSmithQuestReadyToRecruit(Settlement? settlement)
	{
		if (settlement == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadMasterSmithRecruitQuest homesteadMasterSmithRecruitQuest && quest.IsOngoing && homesteadMasterSmithRecruitQuest.PitchHeard && homesteadMasterSmithRecruitQuest.OrdersDone && (homesteadMasterSmithRecruitQuest.PitchSettlement == settlement || homesteadMasterSmithRecruitQuest.PitchSettlement == null))
			{
				return homesteadMasterSmithRecruitQuest;
			}
		}
		return null;
	}

	private void OnCraftingOrderCompleted(Town town, CraftingOrder order, ItemObject item, Hero completerHero)
	{
		if (completerHero == null || completerHero.Clan != Clan.PlayerClan || Campaign.Current?.QuestManager == null || !WasOrderSatisfied(order, item))
		{
			return;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadMasterSmithRecruitQuest homesteadMasterSmithRecruitQuest && quest.IsOngoing && homesteadMasterSmithRecruitQuest.PitchHeard && !homesteadMasterSmithRecruitQuest.OrdersDone)
			{
				homesteadMasterSmithRecruitQuest.RegisterOrderCompleted();
			}
		}
	}

	private static bool WasOrderSatisfied(CraftingOrder order, ItemObject craftedItem)
	{
		try
		{
			ICraftingCampaignBehavior craftingCampaignBehavior = Campaign.Current?.GetCampaignBehavior<ICraftingCampaignBehavior>();
			if (craftingCampaignBehavior == null)
			{
				TraceLogger.Write("HomesteadBehavior", "WasOrderSatisfied: crafting behavior not found — counting order leniently.");
				return true;
			}
			craftingCampaignBehavior.GetOrderResult(order, craftedItem, out var isSucceed, out var _, out var _, out var _);
			TraceLogger.Write("HomesteadBehavior", $"WasOrderSatisfied: order '{order?.OrderOwner?.Name}' isSucceed={isSucceed}.");
			return isSucceed;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "WasOrderSatisfied check failed: " + ex.GetType().Name + ": " + ex.Message);
			return true;
		}
	}

	public bool SettlementSmithUpgradePending(Hero? smith)
	{
		if (smith != null)
		{
			return _settlementSmithUpgrades.ContainsKey(smith.StringId);
		}
		return false;
	}

	public bool SettlementSmithUpgradeReady(Hero? smith)
	{
		if (smith != null && _settlementSmithUpgrades.TryGetValue(smith.StringId, out SettlementSmithUpgradeRecord value))
		{
			return (float)CampaignTime.Now.ToDays >= value.ReadyDay;
		}
		return false;
	}

	public void SetPendingSettlementSmithUpgrade(Hero smith, ItemObject item, bool fromInventory, bool isCivilian, int slot, string modifierId, float readyDay)
	{
		_settlementSmithUpgrades[smith.StringId] = new SettlementSmithUpgradeRecord
		{
			Item = item,
			ItemId = item?.StringId,
			FromInventory = fromInventory,
			IsCivilian = isCivilian,
			Slot = slot,
			ModifierId = modifierId,
			ReadyDay = readyDay
		};
	}

	public void ClearPendingSettlementSmithUpgrade(Hero? smith)
	{
		if (smith != null)
		{
			_settlementSmithUpgrades.Remove(smith.StringId);
		}
	}

	public void OpenSmithUpgradePicker()
	{
		if (Environment.TickCount - _lastSmithUpgradePickerTick < 2000)
		{
			return;
		}
		_lastSmithUpgradePickerTick = Environment.TickCount;
		Homestead hs = CurrentHomestead;
		Hero settlementSmith = ((hs == null) ? Hero.OneToOneConversationHero : null);
		bool flag = ((hs != null) ? hs.SmithUpgradePending : SettlementSmithUpgradePending(settlementSmith));
		if (!HasMasterSmithUpgradeUnlocked || flag || (hs == null && settlementSmith == null))
		{
			return;
		}
		string localizedString = Utils.GetLocalizedString("{=homestead_smith_upgrade_title}Forge Upgrade");
		List<SmithUpgradeOption> list = SmithUpgradeHelper.CollectOptions(Hero.MainHero);
		if (list.Count == 0)
		{
			Utils.ShowMessageBox(localizedString, Utils.GetLocalizedString("{=homestead_smith_upgrade_none}You're carrying nothing the smith can improve."));
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		foreach (SmithUpgradeOption item in list)
		{
			TextObject name = item.Target.Name;
			name.SetTextVariable("ITEMNAME", item.Item.Name);
			string variable = item.Current.ItemModifier?.Name?.ToString() ?? Utils.GetLocalizedString("{=homestead_smith_upgrade_normal}Normal");
			TextObject textObject = new TextObject("{=homestead_smith_upgrade_row}{TARGET}  —  {COST}{GOLD_ICON}");
			textObject.SetTextVariable("TARGET", name.ToString());
			textObject.SetTextVariable("COST", item.Cost);
			textObject.SetTextVariable("GOLD_ICON", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			TextObject textObject2 = new TextObject("{=homestead_smith_upgrade_hint}Currently {CURRENT}. Cost {COST}{GOLD_ICON}.");
			textObject2.SetTextVariable("CURRENT", variable);
			textObject2.SetTextVariable("COST", item.Cost);
			textObject2.SetTextVariable("GOLD_ICON", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			list2.Add(new InquiryElement(item, textObject.ToString(), new ItemImageIdentifier(item.Item), isEnabled: true, textObject2.ToString()));
		}
		TextObject textObject3 = new TextObject("{=homestead_smith_upgrade_body}Choose one piece for the master to refine — it will be ready tomorrow.\nYou have {GOLD}{GOLD_ICON}.");
		textObject3.SetTextVariable("GOLD", Hero.MainHero.Gold);
		textObject3.SetTextVariable("GOLD_ICON", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(localizedString, textObject3.ToString(), list2, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate(List<InquiryElement> selected)
		{
			OnSmithUpgradeChosen(hs, settlementSmith, selected);
		}, null), pauseGameActiveState: true, prioritize: true);
	}

	private void OnSmithUpgradeChosen(Homestead? hs, Hero? settlementSmith, List<InquiryElement> selected)
	{
		if (hs == null && settlementSmith == null)
		{
			TraceLogger.Write("HomesteadBehavior", "Smith Upgrade failed: no homestead or settlement smith");
			return;
		}
		if (selected == null)
		{
			TraceLogger.Write("HomesteadBehavior", "Smith Upgrade failed: selected is null");
			return;
		}
		if (selected.Count == 0)
		{
			TraceLogger.Write("HomesteadBehavior", "Smith Upgrade failed: selected count is 0");
			return;
		}
		if (hs?.SmithUpgradePending ?? SettlementSmithUpgradePending(settlementSmith))
		{
			TraceLogger.Write("HomesteadBehavior", "Smith Upgrade failed: upgrade already pending");
			return;
		}
		if (!(selected[0].Identifier is SmithUpgradeOption smithUpgradeOption))
		{
			TraceLogger.Write("HomesteadBehavior", "Smith Upgrade failed: opt is null");
			return;
		}
		if (Hero.MainHero.Gold < smithUpgradeOption.Cost)
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=homestead_smith_upgrade_poor}You can't afford that upgrade."));
			return;
		}
		if (smithUpgradeOption.IsInventory)
		{
			ItemRoster itemRoster = MobileParty.MainParty?.ItemRoster;
			if (itemRoster == null || itemRoster.FindIndexOfElement(smithUpgradeOption.Current) < 0)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=homestead_smith_upgrade_moved}That piece is no longer where it was. Try again."));
				return;
			}
			itemRoster.AddToCounts(smithUpgradeOption.Current, -1);
		}
		else
		{
			Equipment equipment = (smithUpgradeOption.IsCivilian ? Hero.MainHero.CivilianEquipment : Hero.MainHero.BattleEquipment);
			if (equipment[smithUpgradeOption.Slot].Item != smithUpgradeOption.Item)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=homestead_smith_upgrade_moved}That piece is no longer where it was. Try again."));
				return;
			}
			equipment[smithUpgradeOption.Slot] = EquipmentElement.Invalid;
		}
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, smithUpgradeOption.Cost, disableNotification: true);
		float readyDay = (float)CampaignTime.Now.ToDays + 1f;
		Hero hero;
		if (hs != null)
		{
			hs.SetPendingSmithUpgrade(smithUpgradeOption.Item, smithUpgradeOption.IsInventory, smithUpgradeOption.IsCivilian, (int)smithUpgradeOption.Slot, smithUpgradeOption.Target.StringId, readyDay);
			hero = hs.MasterSmithHero;
		}
		else
		{
			SetPendingSettlementSmithUpgrade(settlementSmith, smithUpgradeOption.Item, smithUpgradeOption.IsInventory, smithUpgradeOption.IsCivilian, (int)smithUpgradeOption.Slot, smithUpgradeOption.Target.StringId, readyDay);
			hero = settlementSmith;
		}
		TextObject textObject = new TextObject("{=homestead_smith_upgrade_dropped}The master takes your {ITEM} to the anvil. Return tomorrow to collect it.");
		textObject.SetTextVariable("ITEM", smithUpgradeOption.Item.Name);
		MBInformationManager.AddQuickInformation(textObject, 0, hero?.CharacterObject);
	}

	public void CollectSmithUpgrade(Homestead? hs, Hero? settlementSmith = null)
	{
		if (!(hs?.SmithUpgradeReady ?? SettlementSmithUpgradeReady(settlementSmith)))
		{
			return;
		}
		ItemObject itemObject;
		ItemModifier itemModifier;
		bool flag;
		bool flag2;
		int num;
		Hero hero;
		if (hs != null)
		{
			itemObject = hs.SmithUpgradeItem ?? MBObjectManager.Instance?.GetObject<ItemObject>(hs.SmithUpgradeItemId ?? string.Empty);
			itemModifier = MBObjectManager.Instance?.GetObject<ItemModifier>(hs.SmithUpgradeModifierId ?? string.Empty);
			flag = hs.SmithUpgradeFromInventory;
			flag2 = hs.SmithUpgradeIsCivilian;
			num = hs.SmithUpgradeSlot;
			hero = hs.MasterSmithHero;
		}
		else
		{
			SettlementSmithUpgradeRecord settlementSmithUpgradeRecord = _settlementSmithUpgrades[settlementSmith.StringId];
			itemObject = settlementSmithUpgradeRecord.Item ?? MBObjectManager.Instance?.GetObject<ItemObject>(settlementSmithUpgradeRecord.ItemId ?? string.Empty);
			itemModifier = MBObjectManager.Instance?.GetObject<ItemModifier>(settlementSmithUpgradeRecord.ModifierId ?? string.Empty);
			flag = settlementSmithUpgradeRecord.FromInventory;
			flag2 = settlementSmithUpgradeRecord.IsCivilian;
			num = settlementSmithUpgradeRecord.Slot;
			hero = settlementSmith;
		}
		try
		{
			if (itemObject != null)
			{
				EquipmentElement equipmentElement = new EquipmentElement(itemObject, itemModifier);
				if (flag)
				{
					MobileParty.MainParty?.ItemRoster?.AddToCounts(equipmentElement, 1);
				}
				else
				{
					Equipment equipment = (flag2 ? Hero.MainHero.CivilianEquipment : Hero.MainHero.BattleEquipment);
					EquipmentIndex index = (EquipmentIndex)num;
					if (equipment[index].Item == null)
					{
						equipment[index] = equipmentElement;
					}
					else
					{
						MobileParty.MainParty?.ItemRoster?.AddToCounts(equipmentElement, 1);
					}
				}
				TextObject textObject = itemModifier?.Name ?? itemObject.Name;
				textObject.SetTextVariable("ITEMNAME", itemObject.Name);
				TextObject textObject2 = new TextObject("{=homestead_smith_upgrade_collected}Your {ITEM} returns from the forge as a {UPGRADED}.");
				textObject2.SetTextVariable("ITEM", itemObject.Name);
				textObject2.SetTextVariable("UPGRADED", textObject);
				MBInformationManager.AddQuickInformation(textObject2, 0, hero?.CharacterObject);
			}
			else
			{
				TraceLogger.Write("HomesteadBehavior", "CollectSmithUpgrade: stored item could not be resolved — upgrade lost.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "CollectSmithUpgrade failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		if (hs != null)
		{
			hs.ClearPendingSmithUpgrade();
		}
		else
		{
			ClearPendingSettlementSmithUpgrade(settlementSmith);
		}
	}

	public void OpenTavernCulturePicker()
	{
		Homestead hs = CurrentHomestead;
		if (hs == null)
		{
			return;
		}
		string localizedString = Utils.GetLocalizedString("{=homestead_tavern_culture_title}Tavern Style");
		string localizedString2 = Utils.GetLocalizedString("{=homestead_tavern_culture_body}Choose which culture's tavern I should lead you to.");
		List<InquiryElement> list = new List<InquiryElement>();
		string[] tavernCultureChoiceIds = TavernCultureChoiceIds;
		foreach (string text in tavernCultureChoiceIds)
		{
			CultureObject cultureObject = MBObjectManager.Instance?.GetObject<CultureObject>(text);
			if (cultureObject != null)
			{
				list.Add(new InquiryElement(text, cultureObject.Name.ToString(), null));
			}
		}
		list.Add(new InquiryElement(null, Utils.GetLocalizedString("{=homestead_tavern_culture_auto}Automatic (nearest match)"), null));
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(localizedString, localizedString2, list, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate(List<InquiryElement> selected)
		{
			OnTavernCultureChosen(hs, selected);
		}, null), pauseGameActiveState: true, prioritize: true);
	}

	private void OnTavernCultureChosen(Homestead hs, List<InquiryElement> selected)
	{
		if (hs != null && selected != null && selected.Count != 0)
		{
			string text = (hs.PreferredTavernCultureId = selected[0].Identifier as string);
			TextObject textObject;
			if (text == null)
			{
				textObject = new TextObject("{=homestead_tavern_culture_set_auto}From now on, I'll lead you to whichever tavern is nearest.");
			}
			else
			{
				CultureObject cultureObject = MBObjectManager.Instance?.GetObject<CultureObject>(text);
				textObject = new TextObject("{=homestead_tavern_culture_set}From now on, I'll lead you to a {CULTURE} tavern.");
				textObject.SetTextVariable("CULTURE", cultureObject?.Name ?? new TextObject(text));
			}
			MBInformationManager.AddQuickInformation(textObject, 0, hs.TroubadourHero?.CharacterObject);
			TraceLogger.Write("HomesteadBehavior", string.Format("OnTavernCultureChosen: '{0}' PreferredTavernCultureId -> '{1}'.", hs.Name, text ?? "(auto)"));
		}
	}

	private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
		RecordClanTournamentResults(winner, participants, town);
		if (winner == null || !winner.IsPlayerCharacter)
		{
			return;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadArmsMasterRecruitQuest homesteadArmsMasterRecruitQuest && quest.IsOngoing && !homesteadArmsMasterRecruitQuest.TournamentWon)
			{
				homesteadArmsMasterRecruitQuest.MarkTournamentWon(town);
			}
		}
	}

	private void RecordClanTournamentResults(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town)
	{
		if (participants == null)
		{
			return;
		}
		foreach (CharacterObject participant in participants)
		{
			Hero hero = participant?.HeroObject;
			if (hero != null && hero.Clan == Clan.PlayerClan)
			{
				string stringId = hero.StringId;
				_clanTournamentEntries[stringId] = (_clanTournamentEntries.TryGetValue(stringId, out var value) ? value : 0) + 1;
				_clanTournamentNames[stringId] = hero.Name?.ToString() ?? stringId;
				if (winner != null && winner.HeroObject == hero)
				{
					_clanTournamentWins[stringId] = (_clanTournamentWins.TryGetValue(stringId, out var value2) ? value2 : 0) + 1;
				}
			}
		}
	}

	public void ShowClanTournamentRecords()
	{
		bool flag = false;
		foreach (KeyValuePair<string, int> clanTournamentEntry in _clanTournamentEntries)
		{
			flag = true;
			int value;
			int variable = (_clanTournamentWins.TryGetValue(clanTournamentEntry.Key, out value) ? value : 0);
			string value2;
			string variable2 = (_clanTournamentNames.TryGetValue(clanTournamentEntry.Key, out value2) ? value2 : clanTournamentEntry.Key);
			TextObject textObject = new TextObject("{=homestead_clan_record_line}{HERO}: {WINS} wins in {ENTRIES} tournaments");
			textObject.SetTextVariable("HERO", variable2);
			textObject.SetTextVariable("WINS", variable);
			textObject.SetTextVariable("ENTRIES", clanTournamentEntry.Value);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
		}
		if (!flag)
		{
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=homestead_clan_record_none}None of your clan have yet competed in a tournament.").ToString()));
		}
	}

	public void RecordSparringMatch(IEnumerable<Hero> winners, IEnumerable<Hero> losers)
	{
		foreach (Hero winner in winners)
		{
			if (winner.Clan == Clan.PlayerClan)
			{
				string stringId = winner.StringId;
				_clanSparringNames[stringId] = winner.Name.ToString();
				_clanSparringEntries[stringId] = (_clanSparringEntries.TryGetValue(stringId, out var value) ? value : 0) + 1;
				_clanSparringWins[stringId] = (_clanSparringWins.TryGetValue(stringId, out var value2) ? value2 : 0) + 1;
			}
		}
		foreach (Hero loser in losers)
		{
			if (loser.Clan == Clan.PlayerClan)
			{
				string stringId2 = loser.StringId;
				_clanSparringNames[stringId2] = loser.Name.ToString();
				_clanSparringEntries[stringId2] = (_clanSparringEntries.TryGetValue(stringId2, out var value3) ? value3 : 0) + 1;
			}
		}
	}

	public void ShowClanSparringRecords()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, int> clanSparringEntry in _clanSparringEntries)
		{
			int value;
			int num = (_clanSparringWins.TryGetValue(clanSparringEntry.Key, out value) ? value : 0);
			int num2 = clanSparringEntry.Value - num;
			string value2;
			string text = (_clanSparringNames.TryGetValue(clanSparringEntry.Key, out value2) ? value2 : clanSparringEntry.Key);
			list.Add($"{text}: {num}W / {num2}L ({clanSparringEntry.Value} matches)");
		}
		string text2 = ((list.Count > 0) ? string.Join("\n", list) : new TextObject("{=homestead_clan_sparring_record_none}None of your clan have yet competed in a sparring match.").ToString());
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=homestead_clan_sparring_records_title}Sparring Records").ToString(), text2, isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=homestead_ok}OK").ToString(), null, delegate
		{
		}, null));
	}

	public void RecordRaceResults(string trackId, List<(string name, string id, int bestLapCs, int totalCs, bool finished)> results)
	{
		if (results == null || string.IsNullOrEmpty(trackId))
		{
			return;
		}
		if (_trackStats == null)
		{
			_trackStats = new Dictionary<string, HomesteadTrackStats>();
		}
		if (!_trackStats.TryGetValue(trackId, out HomesteadTrackStats value) || value == null)
		{
			value = new HomesteadTrackStats();
			_trackStats[trackId] = value;
		}
		foreach (var result in results)
		{
			if (!string.IsNullOrEmpty(result.id))
			{
				value.Counts[result.id] = (value.Counts.TryGetValue(result.id, out var value2) ? value2 : 0) + 1;
				value.CountNames[result.id] = result.name;
			}
			if (result.bestLapCs > 0)
			{
				InsertRaceRecord(value.LapNames, value.LapTimes, result.name, result.bestLapCs);
			}
			if (result.finished && result.totalCs > 0)
			{
				InsertRaceRecord(value.TotalNames, value.TotalTimes, result.name, result.totalCs);
			}
		}
	}

	internal static void RaiseRaceFinished(string trackId, List<(string name, string id, int placeIndex, int totalCs, bool finished)> ordered, Homestead? homestead)
	{
		Action<string, List<(string, string, int, int, bool)>, Homestead> onRaceFinished = HomesteadBehavior.OnRaceFinished;
		if (onRaceFinished == null)
		{
			return;
		}
		try
		{
			onRaceFinished(trackId, ordered, homestead);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "OnRaceFinished handler threw: " + ex.Message);
		}
	}

	public IReadOnlyDictionary<string, HomesteadTrackStats> GetTrackStatsSnapshot()
	{
		if (_trackStats != null)
		{
			return new Dictionary<string, HomesteadTrackStats>(_trackStats);
		}
		return new Dictionary<string, HomesteadTrackStats>();
	}

	private static void RaiseHomesteadConvertedToSettlement(Homestead homestead, Settlement town, Settlement? castle)
	{
		Action<Homestead, Settlement, Settlement> onHomesteadConvertedToSettlement = HomesteadBehavior.OnHomesteadConvertedToSettlement;
		if (onHomesteadConvertedToSettlement == null)
		{
			return;
		}
		try
		{
			onHomesteadConvertedToSettlement(homestead, town, castle);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "OnHomesteadConvertedToSettlement handler threw: " + ex.Message);
		}
	}

	private static void InsertRaceRecord(List<string> names, List<int> times, string name, int timeCs)
	{
		int index = times.Count;
		for (int i = 0; i < times.Count; i++)
		{
			if (timeCs < times[i])
			{
				index = i;
				break;
			}
		}
		names.Insert(index, name);
		times.Insert(index, timeCs);
		while (times.Count > 3)
		{
			names.RemoveAt(times.Count - 1);
			times.RemoveAt(times.Count - 1);
		}
	}

	public void ShowRaceRecords()
	{
		List<RaceTrack> all = RaceTracks.All;
		if (all.Count <= 1)
		{
			ShowRaceRecordsForTrack(RaceTracks.Default);
			return;
		}
		List<InquiryElement> inquiryElements = all.Select((RaceTrack t) => new InquiryElement(t, t.Name, null)).ToList();
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=homestead_race_records_pick_title}Which Track?").ToString(), new TextObject("{=homestead_race_records_pick_text}Whose records would you like to hear?").ToString(), inquiryElements, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate(List<InquiryElement> selected)
		{
			if (selected != null && selected.Count > 0 && selected[0].Identifier is RaceTrack track)
			{
				ShowRaceRecordsForTrack(track);
			}
		}, null));
	}

	private void ShowRaceRecordsForTrack(RaceTrack track)
	{
		if (_trackStats == null)
		{
			_trackStats = new Dictionary<string, HomesteadTrackStats>();
		}
		_trackStats.TryGetValue(track.Id, out HomesteadTrackStats value);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(new TextObject("{=homestead_race_rec_laps}Fastest Laps:").ToString());
		if (value == null || value.LapTimes.Count == 0)
		{
			stringBuilder.AppendLine("   —");
		}
		else
		{
			for (int i = 0; i < value.LapTimes.Count; i++)
			{
				stringBuilder.AppendLine($"   {i + 1}. {value.LapNames[i]} — {FormatRaceTime(value.LapTimes[i])}");
			}
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(new TextObject("{=homestead_race_rec_totals}Fastest Race Times:").ToString());
		if (value == null || value.TotalTimes.Count == 0)
		{
			stringBuilder.AppendLine("   —");
		}
		else
		{
			for (int j = 0; j < value.TotalTimes.Count; j++)
			{
				stringBuilder.AppendLine($"   {j + 1}. {value.TotalNames[j]} — {FormatRaceTime(value.TotalTimes[j])}");
			}
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(new TextObject("{=homestead_race_rec_counts}Most Races:").ToString());
		List<KeyValuePair<string, int>> list = value?.Counts.OrderByDescending<KeyValuePair<string, int>, int>((KeyValuePair<string, int> kv) => kv.Value).Take(3).ToList();
		if (list == null || list.Count == 0)
		{
			stringBuilder.AppendLine("   —");
		}
		else
		{
			foreach (KeyValuePair<string, int> item in list)
			{
				string value2;
				string arg = (value.CountNames.TryGetValue(item.Key, out value2) ? value2 : item.Key);
				stringBuilder.AppendLine($"   {arg}: {item.Value}");
			}
		}
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=homestead_race_records_title}{TRACK} — Records").SetTextVariable("TRACK", track.Name).ToString(), stringBuilder.ToString().TrimEnd(Array.Empty<char>()), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=homestead_ok}OK").ToString(), null, delegate
		{
		}, null));
	}

	private static string FormatRaceTime(int centiseconds)
	{
		float num = (float)centiseconds / 100f;
		int num2 = (int)(num / 60f);
		float num3 = num - (float)num2 * 60f;
		if (num2 <= 0)
		{
			return $"{num3:0.0}s";
		}
		return $"{num2}m {num3:0.0}s";
	}

	// In-memory only (resets on load): next campaign day a wealth raid may hit
	// each homestead. Keeps rich camps from being hit day after day.
	private readonly Dictionary<string, int> _wealthRaidNextDay = new Dictionary<string, int>();

	private const int WealthRaidMinWealth = 10000;

	private const int WealthRaidCooldownDays = 5;

	/// <summary>
	/// Wealth attracts greed: every day, each homestead rolls a raid chance that
	/// scales with the value of its hoard (gold + stash). 30k wealth ≈ 10%/day,
	/// capped at 30%. The mob size also scales with wealth. A hound master makes
	/// the raiders spawn farther out (early warning = more reaction time).
	/// </summary>
	private void WealthRaidDailyTick()
	{
		try
		{
			if (GlobalSettings<MCMSettings>.Instance?.WealthRaidsEnabled != true)
			{
				return;
			}
			int num = (int)CampaignTime.Now.ToDays;
			foreach (KeyValuePair<MobileParty, Homestead> item in HomesteadMobileParties.ToList())
			{
				MobileParty key = item.Key;
				Homestead value = item.Value;
				if (key == null || !key.IsActive || key.IsDisbanding || value == null || value.IsRetiredOrDestroyed || value.IsMoving || key.MapEvent != null || GetActiveRaidQuestForHomestead(value) != null)
				{
					continue;
				}
				if (_wealthRaidNextDay.TryGetValue(key.StringId, out var value2) && num < value2)
				{
					continue;
				}
				Hero hero = key.LeaderHero ?? value.HoundMasterHero ?? value.MarketLadyHero;
				if (hero == null || !hero.IsAlive)
				{
					continue;
				}
				int wealthValue = value.GetWealthValue();
				if (wealthValue < WealthRaidMinWealth)
				{
					continue;
				}
				float num2 = Math.Min(0.3f, (float)wealthValue / 300000f);
				if (MBRandom.RandomFloat >= num2)
				{
					continue;
				}
				int num3 = (int)Math.Min(90L, 12L + (long)wealthValue / 2500L);
				bool flag = value.HoundMasterHero != null && value.HoundMasterHero.IsAlive;
				new HomesteadRaidEventQuest($"homestead_wealth_raid_{key.StringId}_{CampaignTime.Now.ToMilliseconds}", hero, value, num3, Math.Max(1, value.Tier), flag).StartQuest();
				_wealthRaidNextDay[key.StringId] = num + WealthRaidCooldownDays;
				_lastRaidDayByHomestead[key.StringId] = num;
				string text = (flag ? $"Your hound master's dogs raised the alarm early: word of {value.Name}'s wealth has spread, and an angry mob of about {num3} is gathering in the distance!" : $"Word of {value.Name}'s wealth has spread — an angry mob of about {num3} is marching on your homestead!");
				InformationManager.DisplayMessage(new InformationMessage(text, new Color(1f, 0.35f, 0.25f)));
				HomesteadChronicle.Record($"Drawn by its wealth, an angry mob of {num3} marched on the homestead of {value.Name}.");
				TraceLogger.Write("HomesteadBehavior", $"WealthRaid: wealth={wealthValue} chance={num2:0.##} size={num3} houndWarning={flag} homestead='{value.Name}'.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "WealthRaidDailyTick failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public bool CanOfferRaid(Homestead? hs)
	{
		if (hs?.MobileParty == null)
		{
			return false;
		}
		if (GetActiveRaidQuestForHomestead(hs) != null)
		{
			return false;
		}
		if (_lastRaidDayByHomestead.TryGetValue(hs.MobileParty.StringId, out var value) && (int)CampaignTime.Now.ToDays <= value)
		{
			return false;
		}
		return true;
	}

	public void StartRaidEvent(Hero sender)
	{
		Homestead homestead = CurrentHomestead;
		if (homestead?.MobileParty != null && sender != null && GetActiveRaidQuestForHomestead(homestead) == null)
		{
			int num = homestead.MobileParty.MemberRoster?.TotalManCount ?? 10;
			int num2 = MobileParty.MainParty?.MemberRoster?.TotalManCount ?? 1;
			int num3 = Math.Max(10, num + num2);
			int tier = homestead.Tier;
			new HomesteadRaidEventQuest($"homestead_raid_{homestead.Name}_{CampaignTime.Now.ToMilliseconds}", sender, homestead, num3, tier).StartQuest();
			_lastRaidDayByHomestead[homestead.MobileParty.StringId] = (int)CampaignTime.Now.ToDays;
			TraceLogger.Write("HomesteadBehavior", $"StartRaidEvent: '{sender.Name}' triggered Angry Mob ({num3} troops, tier {tier}) on '{homestead.Name}'.");
		}
	}

	public HomesteadNotableApparelQuest? GetActiveApparelQuest(Hero notable)
	{
		if (notable == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadNotableApparelQuest result && quest.IsOngoing && quest.QuestGiver == notable)
			{
				return result;
			}
		}
		return null;
	}

	public bool CanOfferApparel(Hero notable)
	{
		if (notable == null || CurrentHomestead?.MobileParty == null)
		{
			return false;
		}
		if (GetActiveApparelQuest(notable) != null)
		{
			return false;
		}
		return true;
	}

	public void MarkApparelSlotAwarded(Hero notable, int slotBit)
	{
	}

	public bool GenerateApparelOffer(Hero notable)
	{
		Homestead homestead = CurrentHomestead;
		if (homestead?.MobileParty == null || notable == null)
		{
			return false;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < ApparelSlots.Length; i++)
		{
			list.Add(i);
		}
		Vec2 hsPos = homestead.MobileParty.GetPosition2D;
		List<Settlement> list2 = (from s in Settlement.All
			where s.IsTown
			orderby (s.GatePosition.ToVec2() - hsPos).LengthSquared
			select s).Take(3).ToList();
		TraceLogger.Write("HomesteadBehavior", "GenerateApparelOffer: scanning markets of [" + string.Join(", ", list2.Select((Settlement s) => s.Name?.ToString() ?? "?")) + "] " + string.Format("for '{0}' (slots: {1}).", notable.Name, string.Join("/", list.Select((int num) => ApparelSlots[num].LabelKey))));
		foreach (int item in list.OrderBy((int _) => MBRandom.RandomFloat))
		{
			int currentVal = notable.CivilianEquipment[ApparelSlots[item].Index].Item?.Value ?? 0;
			Settlement source;
			ItemObject itemObject = PickCivilianClothingFromMarkets(list2, ApparelSlots[item].Type, currentVal, notable.IsFemale, out source);
			if (itemObject != null)
			{
				_pendingApparelNotable = notable;
				_pendingApparelItem = itemObject;
				_pendingApparelSlotIdx = item;
				SetApparelTextVariables(notable, item, itemObject);
				TraceLogger.Write("HomesteadBehavior", $"GenerateApparelOffer: chose '{itemObject.StringId}' ({itemObject.Name}) for slot " + $"'{ApparelSlots[item].LabelKey}' — IN STOCK at '{source?.Name}'.");
				return true;
			}
		}
		foreach (int item2 in list.OrderBy((int _) => MBRandom.RandomFloat))
		{
			int currentVal2 = notable.CivilianEquipment[ApparelSlots[item2].Index].Item?.Value ?? 0;
			ItemObject itemObject2 = PickCivilianClothingGlobal(ApparelSlots[item2].Type, currentVal2, notable.IsFemale);
			if (itemObject2 != null)
			{
				_pendingApparelNotable = notable;
				_pendingApparelItem = itemObject2;
				_pendingApparelSlotIdx = item2;
				SetApparelTextVariables(notable, item2, itemObject2);
				TraceLogger.Write("HomesteadBehavior", "GenerateApparelOffer: WARNING — no '" + ApparelSlots[item2].LabelKey + "' in nearby markets; " + $"fell back to GLOBAL pick '{itemObject2.StringId}' ({itemObject2.Name}). May not be readily purchasable.");
				return true;
			}
		}
		return false;
	}

	private static ItemObject? PickCivilianClothingFromMarkets(List<Settlement> settlements, ItemObject.ItemTypeEnum type, int currentVal, bool isFemale, out Settlement? source)
	{
		source = null;
		List<(ItemObject, Settlement)> list = new List<(ItemObject, Settlement)>();
		foreach (Settlement settlement in settlements)
		{
			if (settlement.ItemRoster == null)
			{
				continue;
			}
			foreach (ItemRosterElement item2 in settlement.ItemRoster)
			{
				ItemObject item = item2.EquipmentElement.Item;
				if (item != null && item.ItemType == type && item.IsCivilian && item.Value > currentVal && item.Value <= 2000 && (isFemale || item.StringId.IndexOf("dress", StringComparison.OrdinalIgnoreCase) < 0))
				{
					list.Add((item, settlement));
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		(ItemObject, Settlement) tuple = list[MBRandom.RandomInt(list.Count)];
		source = tuple.Item2;
		return tuple.Item1;
	}

	private static ItemObject? PickCivilianClothingGlobal(ItemObject.ItemTypeEnum type, int currentVal, bool isFemale)
	{
		List<ItemObject> list = new List<ItemObject>();
		MBReadOnlyList<ItemObject> mBReadOnlyList = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>();
		if (mBReadOnlyList != null)
		{
			foreach (ItemObject item in mBReadOnlyList)
			{
				if (item.ItemType == type && item.IsCivilian && item.Value > currentVal && item.Value <= 2000 && (isFemale || item.StringId.IndexOf("dress", StringComparison.OrdinalIgnoreCase) < 0))
				{
					list.Add(item);
				}
			}
		}
		if (list.Count != 0)
		{
			return list[MBRandom.RandomInt(list.Count)];
		}
		return null;
	}

	private static void SetApparelTextVariables(Hero notable, int slotIdx, ItemObject item)
	{
		MBTextManager.SetTextVariable("APPAREL_ITEM", item.Name);
		MBTextManager.SetTextVariable("APPAREL_SLOT", new TextObject("{=" + ApparelSlots[slotIdx].LabelKey + "}" + ApparelSlots[slotIdx].LabelKey));
		MBTextManager.SetTextVariable("APPAREL_NOTABLE", notable.Name);
	}

	public void AcceptApparelOffer(Hero notable)
	{
		if (notable != null && _pendingApparelNotable == notable && _pendingApparelItem != null && GetActiveApparelQuest(notable) == null)
		{
			int pendingApparelSlotIdx = _pendingApparelSlotIdx;
			new HomesteadNotableApparelQuest($"homestead_apparel_{notable.StringId}_{CampaignTime.Now.ToMilliseconds}", notable, _pendingApparelItem, (int)ApparelSlots[pendingApparelSlotIdx].Index, pendingApparelSlotIdx, ApparelSlots[pendingApparelSlotIdx].LabelKey).StartQuest();
			TraceLogger.Write("HomesteadBehavior", $"AcceptApparelOffer: '{notable.Name}' wants {_pendingApparelItem.StringId} (slot {ApparelSlots[pendingApparelSlotIdx].Index}).");
			_pendingApparelNotable = null;
			_pendingApparelItem = null;
		}
	}

	public HomesteadBuildingRequestQuest? GetActiveBuildingQuest(Hero notable)
	{
		if (notable == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadBuildingRequestQuest result && quest.IsOngoing && quest.QuestGiver == notable)
			{
				return result;
			}
		}
		return null;
	}

	public bool CanOfferBuilding(Hero notable)
	{
		if (notable != null && CurrentHomestead != null)
		{
			return GetActiveBuildingQuest(notable) == null;
		}
		return false;
	}

	public bool HasActiveQuestForNotable(Hero notable)
	{
		if (notable == null || Campaign.Current?.QuestManager == null)
		{
			return false;
		}
		if (GetActiveRaidQuestForHomestead(CurrentHomestead) != null)
		{
			return true;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest.IsOngoing && quest is IHomesteadNotableQuest && quest.QuestGiver == notable)
			{
				return true;
			}
		}
		return false;
	}

	public bool GenerateBuildingOffer(Hero notable)
	{
		Homestead homestead = CurrentHomestead;
		if (homestead == null || notable == null)
		{
			return false;
		}
		homestead.GetHomesteadScene();
		int sceneBuildPointsLeft = homestead.SceneBuildPointsLeft;
		List<HomesteadScenePlaceable> tierGroup = HomesteadScenePlaceable.GetTierGroup(homestead.Tier);
		if (homestead.CountBuiltPlaceable("homestead_hero_hangout") == 0)
		{
			HomesteadScenePlaceable homesteadScenePlaceable = FindBuildable(tierGroup, homestead, "homestead_hero_hangout", sceneBuildPointsLeft);
			if (homesteadScenePlaceable != null)
			{
				return SetPendingBuilding(notable, homesteadScenePlaceable, "hero hangout (priority)");
			}
		}
		int leisureProductivityBalance = homestead.GetLeisureProductivityBalance();
		List<HomesteadScenePlaceable> list = new List<HomesteadScenePlaceable>();
		string text = ((leisureProductivityBalance < 0) ? "Leisure" : ((leisureProductivityBalance > 3) ? "Productivity" : null));
		if (text != null)
		{
			HomesteadScenePlaceable homesteadScenePlaceable2 = PickBuildingCandidate(tierGroup, homestead, text, sceneBuildPointsLeft);
			if (homesteadScenePlaceable2 != null)
			{
				list.Add(homesteadScenePlaceable2);
				list.Add(homesteadScenePlaceable2);
				list.Add(homesteadScenePlaceable2);
			}
		}
		if (!HasAnyPrisonerStructure(homestead))
		{
			HomesteadScenePlaceable homesteadScenePlaceable3 = FindBuildable(tierGroup, homestead, "homestead_cage_wooden", sceneBuildPointsLeft) ?? FindBuildable(tierGroup, homestead, "homestead_prison_guardhouse", sceneBuildPointsLeft);
			if (homesteadScenePlaceable3 != null)
			{
				list.Add(homesteadScenePlaceable3);
			}
		}
		HomesteadScenePlaceable homesteadScenePlaceable4 = PickDefenseCandidate(tierGroup, homestead, sceneBuildPointsLeft);
		if (homesteadScenePlaceable4 != null)
		{
			list.Add(homesteadScenePlaceable4);
		}
		HomesteadScenePlaceable homesteadScenePlaceable5 = ((list.Count > 0) ? list[MBRandom.RandomInt(list.Count)] : null);
		if (homesteadScenePlaceable5 == null)
		{
			homesteadScenePlaceable5 = PickBuildingCandidate(tierGroup, homestead, "Light", sceneBuildPointsLeft);
		}
		if (homesteadScenePlaceable5 == null)
		{
			return false;
		}
		return SetPendingBuilding(notable, homesteadScenePlaceable5, $"lvp={leisureProductivityBalance} bpLeft={sceneBuildPointsLeft}");
	}

	private bool SetPendingBuilding(Hero notable, HomesteadScenePlaceable pick, string reason)
	{
		_pendingBuildingNotable = notable;
		_pendingBuildingPrefab = pick.PrefabName;
		_pendingBuildingDisplay = pick.DisplayName;
		MBTextManager.SetTextVariable("BUILD_BUILDING", pick.DisplayName);
		MBTextManager.SetTextVariable("BUILD_NOTABLE", notable.Name);
		TraceLogger.Write("HomesteadBehavior", "GenerateBuildingOffer: " + reason + " → '" + pick.PrefabName + "' (" + pick.DisplayName + ").");
		return true;
	}

	private static HomesteadScenePlaceable? FindBuildable(List<HomesteadScenePlaceable> all, Homestead hs, string prefab, int bpLeft)
	{
		foreach (HomesteadScenePlaceable item in all)
		{
			if (!(item.PrefabName != prefab))
			{
				if (item.MaxBuildCount > 0 && hs.CountBuiltPlaceable(item.PrefabName) >= item.MaxBuildCount)
				{
					return null;
				}
				if (item.BuildPointsRequired > bpLeft)
				{
					return null;
				}
				return item;
			}
		}
		return null;
	}

	private static bool HasAnyPrisonerStructure(Homestead hs)
	{
		if (hs.CountBuiltPlaceable("homestead_cage_wooden") <= 0)
		{
			return hs.CountBuiltPlaceable("homestead_prison_guardhouse") > 0;
		}
		return true;
	}

	private static HomesteadScenePlaceable? PickDefenseCandidate(List<HomesteadScenePlaceable> all, Homestead hs, int bpLeft)
	{
		List<HomesteadScenePlaceable> list = new List<HomesteadScenePlaceable>();
		foreach (HomesteadScenePlaceable item in all)
		{
			if (!(item.BuilderMenuCategoryString != "Defense") && Array.IndexOf(UsefulDefensePrefabs, item.PrefabName) >= 0 && (item.MaxBuildCount <= 0 || hs.CountBuiltPlaceable(item.PrefabName) < item.MaxBuildCount) && item.BuildPointsRequired <= bpLeft)
			{
				list.Add(item);
			}
		}
		if (list.Count != 0)
		{
			return list[MBRandom.RandomInt(list.Count)];
		}
		return null;
	}

	private static HomesteadScenePlaceable? PickBuildingCandidate(List<HomesteadScenePlaceable> all, Homestead hs, string category, int bpLeft)
	{
		HomesteadScenePlaceable result = null;
		int num = int.MinValue;
		foreach (HomesteadScenePlaceable item in all)
		{
			if (item.BuilderMenuCategoryString != category || (item.MaxBuildCount > 0 && hs.CountBuiltPlaceable(item.PrefabName) >= item.MaxBuildCount) || (category != "Light" && item.BuildPointsRequired > bpLeft))
			{
				continue;
			}
			bool flag = hs.CountBuiltPlaceable(item.PrefabName) > 0;
			if (!flag || !(category != "Light"))
			{
				int num2 = ((category == "Leisure") ? (((item.MedicalCareIncrease > 0 || item.ProduceItems.Count > 0 || item.ProductivityIncrease > 0) ? 100 : 0) + item.LeisureIncrease + ((!flag) ? 10 : 0)) : ((!(category == "Productivity")) ? ((!flag) ? 10 : 0) : (((item.ProduceItems.Count > 0) ? 100 : 0) + item.ProductivityIncrease + ((!flag) ? 10 : 0))));
				num2 += MBRandom.RandomInt(3);
				if (num2 > num)
				{
					num = num2;
					result = item;
				}
			}
		}
		return result;
	}

	public string DebugBuildingSelection(Homestead hs)
	{
		if (hs == null)
		{
			return "No homestead.";
		}
		hs.GetHomesteadScene();
		int sceneBuildPointsLeft = hs.SceneBuildPointsLeft;
		int leisureProductivityBalance = hs.GetLeisureProductivityBalance();
		List<HomesteadScenePlaceable> tierGroup = HomesteadScenePlaceable.GetTierGroup(hs.Tier);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Building selection for '{hs.Name}': tier={hs.Tier} lvp={leisureProductivityBalance} bpLeft={sceneBuildPointsLeft} " + $"prod={hs.SceneTotalProductivity} leis={hs.SceneTotalLeisure} " + $"placeablesAtTier={tierGroup.Count}");
		stringBuilder.AppendLine(string.Format("  hero hangout built: {0}", hs.CountBuiltPlaceable("homestead_hero_hangout")));
		if (hs.CountBuiltPlaceable("homestead_hero_hangout") == 0)
		{
			HomesteadScenePlaceable homesteadScenePlaceable = FindBuildable(tierGroup, hs, "homestead_hero_hangout", sceneBuildPointsLeft);
			stringBuilder.AppendLine("  P1 hero-hangout buildable: " + ((homesteadScenePlaceable != null) ? homesteadScenePlaceable.DisplayName : "NO (not in tier list or unaffordable)"));
		}
		string text = ((leisureProductivityBalance < 0) ? "Leisure" : ((leisureProductivityBalance > 3) ? "Productivity" : null));
		stringBuilder.AppendLine("  balance category: " + (text ?? "(balanced → Light cosmetic)"));
		if (text != null)
		{
			HomesteadScenePlaceable homesteadScenePlaceable2 = PickBuildingCandidate(tierGroup, hs, text, sceneBuildPointsLeft);
			stringBuilder.AppendLine("  balance pick: " + ((homesteadScenePlaceable2 != null) ? homesteadScenePlaceable2.DisplayName : "none affordable"));
		}
		HomesteadScenePlaceable homesteadScenePlaceable3 = ((!HasAnyPrisonerStructure(hs)) ? (FindBuildable(tierGroup, hs, "homestead_cage_wooden", sceneBuildPointsLeft) ?? FindBuildable(tierGroup, hs, "homestead_prison_guardhouse", sceneBuildPointsLeft)) : null);
		stringBuilder.AppendLine("  prisoner option: " + ((homesteadScenePlaceable3 != null) ? homesteadScenePlaceable3.DisplayName : "(have one / none affordable)"));
		HomesteadScenePlaceable homesteadScenePlaceable4 = PickDefenseCandidate(tierGroup, hs, sceneBuildPointsLeft);
		stringBuilder.AppendLine("  defense option: " + ((homesteadScenePlaceable4 != null) ? homesteadScenePlaceable4.DisplayName : "none"));
		HomesteadScenePlaceable homesteadScenePlaceable5 = PickBuildingCandidate(tierGroup, hs, "Light", sceneBuildPointsLeft);
		stringBuilder.AppendLine("  light fallback: " + ((homesteadScenePlaceable5 != null) ? homesteadScenePlaceable5.DisplayName : "none — NO Light placeables found!"));
		return stringBuilder.ToString();
	}

	public void AcceptBuildingOffer(Hero notable)
	{
		if (notable != null && _pendingBuildingNotable == notable && !string.IsNullOrEmpty(_pendingBuildingPrefab) && GetActiveBuildingQuest(notable) == null)
		{
			Homestead homestead = CurrentHomestead;
			if (homestead != null)
			{
				int num = homestead.CountBuiltPlaceable(_pendingBuildingPrefab);
				new HomesteadBuildingRequestQuest($"homestead_build_{notable.StringId}_{CampaignTime.Now.ToMilliseconds}", notable, homestead, _pendingBuildingPrefab, _pendingBuildingDisplay ?? _pendingBuildingPrefab, num).StartQuest();
				TraceLogger.Write("HomesteadBehavior", $"AcceptBuildingOffer: '{notable.Name}' wants '{_pendingBuildingPrefab}' (baseline {num}).");
				_pendingBuildingNotable = null;
				_pendingBuildingPrefab = null;
				_pendingBuildingDisplay = null;
			}
		}
	}

	public HomesteadApprenticeQuest? GetActiveApprenticeQuest(Hero notable)
	{
		if (notable == null || Campaign.Current?.QuestManager == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadApprenticeQuest result && quest.IsOngoing && quest.QuestGiver == notable)
			{
				return result;
			}
		}
		return null;
	}

	public static HomesteadApprenticeQuest? GetApprenticeQuestForHero(Hero? hero)
	{
		if (hero == null || Campaign.Current?.QuestManager?.Quests == null)
		{
			return null;
		}
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is HomesteadApprenticeQuest homesteadApprenticeQuest && quest.IsOngoing && homesteadApprenticeQuest.Apprentice == hero)
			{
				return homesteadApprenticeQuest;
			}
		}
		return null;
	}

	public bool CanOfferApprentice(Hero notable)
	{
		if (notable == null || CurrentHomestead == null)
		{
			return false;
		}
		if (GetActiveApprenticeQuest(notable) != null)
		{
			return false;
		}
		int value;
		return (_notableApprenticeCount.TryGetValue(notable.StringId, out value) ? value : 0) < 1;
	}

	public void AcceptApprenticeOffer(Hero notable)
	{
		if (notable == null || GetActiveApprenticeQuest(notable) != null)
		{
			return;
		}
		Homestead homestead = CurrentHomestead;
		if (homestead != null)
		{
			Hero hero = CreateApprenticeHero(notable, homestead);
			if (hero == null)
			{
				TraceLogger.Write("HomesteadBehavior", "AcceptApprenticeOffer: failed to create apprentice hero.");
				return;
			}
			AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
			hero.CharacterObject?.SetTransferableInPartyScreen(isTransferable: false);
			new HomesteadApprenticeQuest($"homestead_apprentice_{notable.StringId}_{CampaignTime.Now.ToMilliseconds}", notable, homestead, hero).StartQuest();
			TextObject textObject = new TextObject("{=homestead_appr_accepted}{APPRENTICE} joins your party to learn the ways of war. Win battles with them along.");
			textObject.SetTextVariable("APPRENTICE", hero.Name);
			Utils.PrintDebugMessage(textObject.ToString(), 180f, 200f, 240f);
			TraceLogger.Write("HomesteadBehavior", $"AcceptApprenticeOffer: '{notable.Name}' apprentice '{hero.Name}' (lvl {hero.Level}) joined the party.");
		}
	}

	private static Hero? CreateApprenticeHero(Hero notable, Homestead hs)
	{
		bool isFemale = MBRandom.RandomFloat < 0.5f;
		CharacterObject template = null;
		List<CharacterObject> list = CharacterObject.All.Where((CharacterObject c) => c.IsTemplate && c.Occupation == Occupation.RuralNotable).ToList();
		List<CharacterObject> list2 = list.Where((CharacterObject c) => c.IsFemale == isFemale).ToList();
		if (list2.Count > 0)
		{
			template = list2[MBRandom.RandomInt(list2.Count)];
		}
		else if (list.Count > 0)
		{
			template = list[MBRandom.RandomInt(list.Count)];
		}
		if (template == null)
		{
			list = CharacterObject.All.Where((CharacterObject c) => c.IsTemplate && c.Occupation == Occupation.Wanderer).ToList();
			list2 = list.Where((CharacterObject c) => c.IsFemale == isFemale).ToList();
			if (list2.Count > 0)
			{
				template = list2[MBRandom.RandomInt(list2.Count)];
			}
			else if (list.Count > 0)
			{
				template = list[MBRandom.RandomInt(list.Count)];
			}
		}
		if (template == null)
		{
			return null;
		}
		Settlement settlement = Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage && s.Culture == template.Culture) ?? Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage);
		if (settlement == null)
		{
			return null;
		}
		int age = 18 + MBRandom.RandomInt(6);
		Hero hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, age);
		Utils.ApplyRandomPersonalityTraits(hero);
		if (hero.CurrentSettlement != null)
		{
			LeaveSettlementAction.ApplyForCharacterOnly(hero);
		}
		string text = hero.FirstName?.ToString() ?? "Apprentice";
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "Apprentice";
		}
		TextObject roleTitleFor = Homestead.GetRoleTitleFor(hs, notable);
		TextObject textObject = new TextObject("{=homestead_apprentice_title_v2}{BASE_NAME} the Apprentice to the {ROLE} of {HOMESTEAD_NAME}").SetTextVariable("BASE_NAME", text).SetTextVariable("ROLE", roleTitleFor).SetTextVariable("HOMESTEAD_NAME", hs.Name);
		hero.SetName(textObject, new TextObject(text));
		Homestead.PatchCharacterObjectName(hero.CharacterObject, textObject);
		HomesteadApprenticeQuest.EquipApprentice(hero, 2);
		try
		{
			SkillObject[] array;
			if (hero.HeroDeveloper != null)
			{
				array = new SkillObject[18]
				{
					DefaultSkills.OneHanded,
					DefaultSkills.TwoHanded,
					DefaultSkills.Polearm,
					DefaultSkills.Bow,
					DefaultSkills.Crossbow,
					DefaultSkills.Throwing,
					DefaultSkills.Riding,
					DefaultSkills.Athletics,
					DefaultSkills.Crafting,
					DefaultSkills.Scouting,
					DefaultSkills.Tactics,
					DefaultSkills.Roguery,
					DefaultSkills.Charm,
					DefaultSkills.Leadership,
					DefaultSkills.Trade,
					DefaultSkills.Steward,
					DefaultSkills.Medicine,
					DefaultSkills.Engineering
				};
				foreach (SkillObject skill in array)
				{
					hero.HeroDeveloper.SetInitialSkillLevel(skill, 5);
				}
				hero.HeroDeveloper.InitializeHeroDeveloper();
			}
			SkillObject[] array2 = new SkillObject[4]
			{
				DefaultSkills.OneHanded,
				DefaultSkills.Athletics,
				DefaultSkills.Polearm,
				DefaultSkills.Throwing
			};
			array = array2;
			foreach (SkillObject skill2 in array)
			{
				hero.HeroDeveloper?.AddFocus(skill2, 3, checkUnspentFocusPoints: false);
			}
			if (hero.HeroDeveloper != null)
			{
				int num2 = 0;
				while (hero.Level < 5 && num2 < 500)
				{
					SkillObject skill3 = array2[MBRandom.RandomInt(array2.Length)];
					int skillValue = hero.GetSkillValue(skill3);
					hero.HeroDeveloper.SetInitialSkillLevel(skill3, skillValue + 1);
					hero.HeroDeveloper.InitializeHeroDeveloper();
					num2++;
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "CreateApprenticeHero: Setup threw " + ex.GetType().Name + ": " + ex.Message);
		}
		try
		{
			hero.SetHasMet();
		}
		catch
		{
		}
		try
		{
			hero.SetPersonalRelation(Hero.MainHero, 75);
		}
		catch
		{
		}
		return hero;
	}

	public void AcceptDeliveryOffer(Hero sender)
	{
		if (sender != null && _pendingDeliveryRecipient != null && _pendingDeliveryPackage != null && GetActiveDeliveryQuest(sender) == null)
		{
			new HomesteadPackageDeliveryQuest($"homestead_delivery_{sender.StringId}_{CampaignTime.Now.ToMilliseconds}", sender, _pendingDeliveryPackage, _pendingDeliveryRecipient).StartQuest();
			SetDeliveryTextVariables(sender, _pendingDeliveryRecipient, _pendingDeliveryPackage);
			Utils.PrintDebugMessage(new TextObject("{=homestead_pkg_accepted}You carry {PKG_ITEM} for delivery to {PKG_RECIPIENT} in {PKG_SETTLEMENT}. (Added to Quest Log.)").ToString(), 180f, 200f, 240f);
			_pendingDeliveryRecipient = null;
			_pendingDeliveryPackage = null;
		}
	}

	public void CompleteDelivery(Hero recipient)
	{
		GetDeliveryQuestForRecipient(recipient)?.CompleteDelivery();
	}

	public void GenerateFavorOffer(Hero notable)
	{
		int num = CurrentHomestead?.Tier ?? 1;
		float val = ((notable != null) ? Math.Max(0f, notable.GetRelationWithPlayer()) : 0f);
		int num2 = Math.Min(num - 1, 2) * 6;
		int num3 = (int)(Math.Min(val, 75f) / 75f * 15f);
		_offerItem = FavorItemPool[MBRandom.RandomInt(FavorItemPool.Length)];
		_offerCount = Math.Max(3, Math.Min(3 + num2 + num3, 30));
		SetFavorTextVariables(_offerItem, _offerCount);
	}

	private void SetFavorTextVariables(string itemId, int count)
	{
		TextObject text = GetFavorItemObject(itemId)?.Name ?? new TextObject(itemId);
		MBTextManager.SetTextVariable("FAVOR_ITEM", text);
		MBTextManager.SetTextVariable("FAVOR_COUNT", count);
	}

	public void SetFavorTextVariablesPublic(string itemId, int count, int playerCount = 0)
	{
		SetFavorTextVariables(itemId, count);
		MBTextManager.SetTextVariable("PLAYER_COUNT", playerCount);
	}

	public void AcceptCurrentFavorOffer(Hero notable)
	{
		if (notable != null && !string.IsNullOrEmpty(_offerItem) && _offerCount > 0 && GetActiveFavorQuest(notable) == null)
		{
			Homestead homestead = CurrentHomestead;
			ItemObject favorItemObject = GetFavorItemObject(_offerItem);
			if (homestead?.MobileParty != null && favorItemObject != null)
			{
				string text = "homestead_favor_" + notable.StringId + "_" + CampaignTime.Now.ToMilliseconds;
				new HomesteadNotableFavorQuest(text, notable, homestead.MobileParty, favorItemObject, _offerCount).StartQuest();
				SetFavorTextVariables(_offerItem, _offerCount);
				TextObject textObject = new TextObject("{=homestead_favor_accepted}{NOTABLE_NAME} will remember this favor: bring {FAVOR_COUNT} {FAVOR_ITEM} to the homestead stores. (Added to your Quest Log.)");
				textObject.SetTextVariable("NOTABLE_NAME", notable.Name);
				Utils.PrintDebugMessage(textObject.ToString(), 180f, 200f, 240f);
				TraceLogger.Write("HomesteadBehavior", $"AcceptCurrentFavorOffer: created favor quest '{text}' for '{notable.Name}' ({_offerCount}x{_offerItem}).");
			}
		}
	}

	public void CompleteFavor(Hero notable)
	{
		if (notable == null)
		{
			return;
		}
		HomesteadNotableFavorQuest activeFavorQuest = GetActiveFavorQuest(notable);
		if (activeFavorQuest != null && activeFavorQuest.IsReady())
		{
			int remainingNeeded = activeFavorQuest.RemainingNeeded;
			string text = activeFavorQuest.Item?.StringId ?? "";
			activeFavorQuest.TurnInAndComplete();
			Hero mainHero = Hero.MainHero;
			if (mainHero != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, notable, 4, showQuickNotification: false);
			}
			_notableFavorsDone[notable.StringId] = (_notableFavorsDone.TryGetValue(notable.StringId, out var value) ? value : 0) + 1;
			SetFavorTextVariables(text, remainingNeeded);
			TextObject textObject = new TextObject("{=homestead_favor_completed}{NOTABLE_NAME}'s favor complete (+{REWARD} relation).");
			textObject.SetTextVariable("NOTABLE_NAME", notable.Name);
			textObject.SetTextVariable("REWARD", 4);
			Utils.PrintDebugMessage(textObject.ToString(), 100f, 220f, 120f);
			TraceLogger.Write("HomesteadBehavior", $"CompleteFavor: '{notable.Name}' favor {remainingNeeded}x{text} → +{4} relation (now {(int)notable.GetRelationWithPlayer()}).");
		}
	}

	public void AdoptDog(string dogName, Homestead fromHomestead, int materialIndex = 0)
	{
		_hasAdoptedDog = true;
		_adoptedDogName = dogName;
		_adoptedDogHomesteadId = fromHomestead?.MobileParty?.StringId ?? "Town";
		_adoptedDogMaterialIndex = ((materialIndex >= 0) ? ((materialIndex > 2) ? 2 : materialIndex) : 0);
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>("dog");
		if (itemObject != null && fromHomestead?.Stash != null)
		{
			fromHomestead.Stash.AddToCounts(itemObject, -1);
		}
		EnsureAdoptedDogItemRegistered();
		ItemObject itemObject2 = Game.Current.ObjectManager.GetObject<ItemObject>("homestead_adopted_dog");
		if (itemObject2 != null)
		{
			MobileParty.MainParty.ItemRoster.AddToCounts(itemObject2, 1);
		}
		else
		{
			TraceLogger.Write("HomesteadBehavior", "AdoptDog: homestead_adopted_dog item not found after registration attempt — token not added to inventory.");
		}
	}

	public void ReleaseDog()
	{
		EnsureAdoptedDogItemRegistered();
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>("dog");
		ItemObject itemObject2 = Game.Current.ObjectManager.GetObject<ItemObject>("homestead_adopted_dog");
		if (itemObject2 != null)
		{
			MobileParty.MainParty.ItemRoster.AddToCounts(itemObject2, -1);
		}
		Homestead homestead = HomesteadMobileParties.Values.FirstOrDefault((Homestead h) => h.MobileParty != null && h.MobileParty.StringId == _adoptedDogHomesteadId);
		if (itemObject != null)
		{
			homestead?.Stash.AddToCounts(itemObject, 1);
		}
		_hasAdoptedDog = false;
		_adoptedDogName = "";
		_adoptedDogHomesteadId = "";
		_adoptedDogMaterialIndex = 0;
	}

	internal static void EnsureAdoptedDogItemRegistered()
	{
		if (_adoptedDogItemRegistered)
		{
			return;
		}
		if (Game.Current?.ObjectManager?.GetObject<ItemObject>("homestead_adopted_dog") != null)
		{
			_adoptedDogItemRegistered = true;
			return;
		}
		string text = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "homestead_adopted_dog_" + Guid.NewGuid().ToString("N") + ".xml");
		try
		{
			File.WriteAllText(text, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Items>\n  <Item id=\"homestead_adopted_dog\"\n        name=\"{=homestead_adopted_dog_name}Loyal Hound\"\n        value=\"1\"\n        is_merchandise=\"false\"\n        item_category=\"goods\"\n        type=\"Goods\" />\n</Items>", Encoding.UTF8);
			Game.Current.ObjectManager.LoadOneXmlFromFile(text, "", skipValidation: true);
			TraceLogger.Write("HomesteadBehavior", string.Format("EnsureAdoptedDogItemRegistered: success={0}", _adoptedDogItemRegistered = Game.Current.ObjectManager.GetObject<ItemObject>("homestead_adopted_dog") != null));
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "EnsureAdoptedDogItemRegistered: failed — " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			try
			{
				File.Delete(text);
			}
			catch
			{
			}
		}
	}

	private void RepairHeadmenAndApprentices()
	{
		foreach (Settlement settlement in Campaign.Current.Settlements)
		{
			if (!settlement.IsVillage || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_settlement_"))
			{
				continue;
			}
			foreach (Hero item in settlement.Notables.ToList())
			{
				if (!item.IsAlive || item.Occupation != Occupation.Headman)
				{
					continue;
				}
				if (item.Clan == null || item.Clan.IsEliminated)
				{
					try
					{
						Settlement bornSettlement = HomesteadSettlementBuilder.FindUnrelatedVanillaVillage(item.CharacterObject?.Culture) ?? settlement;
						Hero hero = HeroCreator.CreateSpecialHero(item.CharacterObject, bornSettlement, null, null, 30);
						hero.Clan?.SetLeader(item);
						KillCharacterAction.ApplyByRemove(hero);
						TraceLogger.Write("HomesteadBehavior", $"RepairHeadmenAndApprentices: Fixed null clan for headman {item.Name}");
					}
					catch (Exception ex)
					{
						TraceLogger.Write("HomesteadBehavior", $"RepairHeadmenAndApprentices: Failed to fix headman {item.Name} - {ex.Message}");
					}
				}
				try
				{
					if (item.IsFugitive || item.HeroState != Hero.CharacterStates.Active)
					{
						item.ChangeState(Hero.CharacterStates.Active);
					}
				}
				catch
				{
				}
				try
				{
					if (item.GetRelationWithPlayer() < 75f)
					{
						item.SetPersonalRelation(Hero.MainHero, 75);
					}
				}
				catch
				{
				}
			}
		}
	}

	private void RepairConvertedNotableClans()
	{
		if (ConvertedNotableRoles == null || ConvertedNotableRoles.Count == 0)
		{
			return;
		}
		foreach (string heroId in ConvertedNotableRoles.Keys.ToList())
		{
			try
			{
				Hero hero = Hero.AllAliveHeroes.FirstOrDefault((Hero h) => h.StringId == heroId);
				if (hero == null)
				{
					continue;
				}
				try
				{
					Settlement bornSettlement = hero.BornSettlement;
					if (bornSettlement != null && bornSettlement.StringId?.StartsWith("hsr_settlement_") == true && hero.Clan != null && !hero.Clan.IsEliminated && hero.Clan.Leader == hero && hero.Clan.HomeSettlement != bornSettlement)
					{
						AccessTools.Field(typeof(Clan), "_home")?.SetValue(hero.Clan, bornSettlement);
						hero.UpdateHomeSettlement();
						TraceLogger.Write("HomesteadBehavior", $"RepairConvertedNotableClans: re-homed clan of '{hero.Name}' ({ConvertedNotableRoles[heroId]}) to '{bornSettlement.StringId}'.");
					}
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadBehavior", "RepairConvertedNotableClans: home repair failed for '" + heroId + "': " + ex.Message);
				}
				if ((hero.Clan != null && !hero.Clan.IsEliminated) || hero.CharacterObject == null)
				{
					continue;
				}
				Settlement settlement = HomesteadSettlementBuilder.FindUnrelatedVanillaVillage(hero.CharacterObject.Culture) ?? hero.CurrentSettlement ?? Campaign.Current?.Settlements.FirstOrDefault((Settlement s) => s.IsVillage);
				if (settlement != null)
				{
					Hero hero2 = HeroCreator.CreateSpecialHero(hero.CharacterObject, settlement, null, null, 30);
					if (hero2?.Clan != null)
					{
						hero2.Clan.SetLeader(hero);
						KillCharacterAction.ApplyByRemove(hero2);
						TraceLogger.Write("HomesteadBehavior", $"RepairConvertedNotableClans: fixed clan for '{hero.Name}' ({ConvertedNotableRoles[heroId]}).");
					}
				}
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("HomesteadBehavior", "RepairConvertedNotableClans: failed for '" + heroId + "': " + ex2.Message);
			}
		}
	}

	private List<(Hero Amb, string AnchorId, Vec2 Pos, string AnchorName)> GetConvertedAmbassadorAnchors()
	{
		List<(Hero, string, Vec2, string)> list = new List<(Hero, string, Vec2, string)>();
		HashSet<string> hashSet = new HashSet<string>();
		if (ConvertedNotableRoles == null)
		{
			return list;
		}
		foreach (KeyValuePair<string, string> kv in ConvertedNotableRoles)
		{
			if (kv.Value != "Ambassador")
			{
				continue;
			}
			Hero hero = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => x.StringId == kv.Key);
			if (hero == null || !hero.IsAlive)
			{
				continue;
			}
			string text = null;
			Vec2 item = default(Vec2);
			string item2 = "";
			if (MobileParty.MainParty != null && hero.PartyBelongedTo == MobileParty.MainParty)
			{
				text = "party:main";
				item = MobileParty.MainParty.GetPosition2D;
				item2 = Utils.GetLocalizedString("{=homestead_amb_anchor_party}your party");
			}
			else
			{
				Settlement settlement = hero.GovernorOf?.Settlement ?? hero.CurrentSettlement;
				if (settlement != null)
				{
					text = "sett:" + settlement.StringId;
					item = settlement.GetPosition2D;
					item2 = settlement.Name?.ToString() ?? "?";
				}
			}
			if (text != null && hashSet.Add(text))
			{
				list.Add((hero, text, item, item2));
			}
		}
		return list;
	}

	private void ConvertedAmbassadorDailyTick()
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero == null)
			{
				return;
			}
			Clan clan = mainHero.Clan;
			foreach (var (amb, _, v, anchorName) in GetConvertedAmbassadorAnchors())
			{
				if (MBRandom.RandomFloat >= 0.5f)
				{
					continue;
				}
				List<Hero> list = new List<Hero>();
				foreach (Settlement settlement in Campaign.Current.Settlements)
				{
					if (settlement == null || settlement.GetPosition2D.Distance(v) > 20f)
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
					ApplyConvertedAmbassadorImprovement(mainHero, list[MBRandom.RandomInt(list.Count)], amb, anchorName, "daily (notable)");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "ConvertedAmbassadorDailyTick: " + ex.Message);
		}
	}

	private void ConvertedAmbassadorHourlyTick()
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero == null)
			{
				return;
			}
			Clan clan = mainHero.Clan;
			Dictionary<string, HashSet<string>> dictionary = new Dictionary<string, HashSet<string>>();
			foreach (var (amb, key, v, anchorName) in GetConvertedAmbassadorAnchors())
			{
				_convAmbassadorNearbyByAnchor.TryGetValue(key, out HashSet<string> value);
				HashSet<string> hashSet = (dictionary[key] = new HashSet<string>());
				foreach (MobileParty mobileParty in Campaign.Current.MobileParties)
				{
					if (mobileParty == null || !mobileParty.IsActive || mobileParty.IsMainParty || mobileParty.GetPosition2D.Distance(v) > 20f)
					{
						continue;
					}
					Hero hero = null;
					Hero leaderHero = mobileParty.LeaderHero;
					if (leaderHero != null && leaderHero.IsAlive && leaderHero != mainHero && leaderHero.IsLord && leaderHero.Clan != clan)
					{
						hero = leaderHero;
					}
					if (mobileParty.PartyComponent is CaravanPartyComponent { Owner: { IsAlive: not false } owner } && owner != mainHero && owner.Clan != clan)
					{
						hero = owner;
					}
					if (hero != null)
					{
						hashSet.Add(mobileParty.StringId);
						if ((value == null || !value.Contains(mobileParty.StringId)) && !(MBRandom.RandomFloat >= 0.15f))
						{
							ApplyConvertedAmbassadorImprovement(mainHero, hero, amb, anchorName, "hourly (lord/caravan)");
						}
					}
				}
			}
			_convAmbassadorNearbyByAnchor = dictionary;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "ConvertedAmbassadorHourlyTick: " + ex.Message);
		}
	}

	private void ApplyConvertedAmbassadorImprovement(Hero player, Hero target, Hero amb, string anchorName, string source)
	{
		ChangeRelationAction.ApplyRelationChangeBetweenHeroes(player, target, 1, showQuickNotification: false);
		player.AddSkillXp(DefaultSkills.Charm, 25f);
		TraceLogger.Write("HomesteadBehavior", $"ConvertedAmbassador ({source}): '{amb.Name}' @ '{anchorName}' improved '{player.Name}' ↔ '{target.Name}' by +{1}, +{25} Charm XP.");
		MCMSettings? instance = GlobalSettings<MCMSettings>.Instance;
		if (instance != null && instance.ShowNpcXpNotifications)
		{
			Utils.PrintLocalizedMessage("homestead_ambassador_conv_relations_improved", "Your Ambassador at {ANCHOR_NAME} improved your relations with {TARGET_NAME} (+1).", 160f, 200f, 240f, ("ANCHOR_NAME", anchorName), ("TARGET_NAME", target.Name?.ToString() ?? "?"));
		}
	}

	private void RemoveHomesteadNotablesFromSettlements()
	{
		if (HomesteadMobileParties == null)
		{
			return;
		}
		foreach (KeyValuePair<MobileParty, Homestead> homesteadMobileParty in HomesteadMobileParties)
		{
			Homestead value = homesteadMobileParty.Value;
			if (value.ResidentHeroes != null)
			{
				foreach (Hero residentHero in value.ResidentHeroes)
				{
					ForceDetachHeroFromSettlement(residentHero);
				}
			}
			ForceDetachHeroFromSettlement(value.HoundMasterHero);
			ForceDetachHeroFromSettlement(value.MarketLadyHero);
			ForceDetachHeroFromSettlement(value.AmbassadorHero);
			ForceDetachHeroFromSettlement(value.ArmsMasterHero);
			ForceDetachHeroFromSettlement(value.TavernKeeperHero);
			ForceDetachHeroFromSettlement(value.MasterSmithHero);
		}
	}

	private static void ForceDetachHeroFromSettlement(Hero? hero)
	{
		if (hero == null)
		{
			return;
		}
		try
		{
			Settlement settlement = hero.CurrentSettlement ?? hero.HomeSettlement;
			if (hero.CurrentSettlement != null)
			{
				LeaveSettlementAction.ApplyForCharacterOnly(hero);
			}
			typeof(Hero).GetField("_homeSettlement", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(hero, null);
			settlement?.GetType().GetMethod("CollectNotablesToCache", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(settlement, null);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "ForceDetachHeroFromSettlement: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void RecoverGraduatedApprentices()
	{
		if (HomesteadMobileParties == null)
		{
			return;
		}
		foreach (KeyValuePair<MobileParty, Homestead> homesteadMobileParty in HomesteadMobileParties)
		{
			Homestead value = homesteadMobileParty.Value;
			if (value.MobileParty == null)
			{
				continue;
			}
			foreach (TroopRosterElement item in value.MobileParty.MemberRoster.GetTroopRoster())
			{
				if (item.Character.IsHero)
				{
					Hero heroObject = item.Character.HeroObject;
					if (heroObject != null && heroObject.Name != null && heroObject.Name.ToString().Contains("Apprentice"))
					{
						value.AddResidentHero(heroObject);
					}
				}
			}
		}
	}

	private void RetroactivelyGrantApprenticeBonusesForAllHomesteads()
	{
		if (HomesteadMobileParties == null)
		{
			return;
		}
		foreach (KeyValuePair<MobileParty, Homestead> homesteadMobileParty in HomesteadMobileParties)
		{
			homesteadMobileParty.Value?.RetroactivelyGrantApprenticeBonuses();
		}
	}

	private void OnSaveOver(bool isSuccessful, string saveName)
	{
		if (isSuccessful && !string.IsNullOrEmpty(saveName))
		{
			HomesteadSettlementBuilder.FlushToSaveFile(saveName);
		}
	}

	private void RecalculateAllHomesteadSceneValues()
	{
		if (HomesteadMobileParties == null)
		{
			return;
		}
		foreach (KeyValuePair<MobileParty, Homestead> homesteadMobileParty in HomesteadMobileParties)
		{
			if (homesteadMobileParty.Value != null)
			{
				homesteadMobileParty.Value.GetHomesteadScene().RecalculateValues();
				homesteadMobileParty.Key?.MemberRoster?.UpdateVersion();
				homesteadMobileParty.Key?.PrisonRoster?.UpdateVersion();
			}
		}
	}

	public void OnApplicationTick(float dt)
	{
		HomesteadMovingHUD.UpdateAll();
		if (_raceAwaitingRivalPicker && Mission.Current == null && GameStateManager.Current?.ActiveState is MapState && _racePickTrack != null)
		{
			_raceAwaitingRivalPicker = false;
			ShowRaceRivalSelector(_racePickHs, _racePickTrack, _racePickSettlement);
		}
		if (_pendingArenaRace && Mission.Current == null && GameStateManager.Current?.ActiveState is MapState)
		{
			_pendingArenaRace = false;
			Settlement arenaRaceSettlement = _arenaRaceSettlement;
			_arenaRaceSettlement = null;
			if (Campaign.Current != null)
			{
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			}
			BeginRaceSetup(null, arenaRaceSettlement);
		}
		if (HomesteadForgeContext.IsActive && !(GameStateManager.Current?.ActiveState is CraftingState))
		{
			HomesteadForgeContext.End();
		}
		if ((SparringPending || _pendingReturnHomestead != null || _pendingTavernHomestead != null || _pendingRaceHomestead != null || pendingPlanningMode || pendingPlacementMode) && Mission.Current == null && GameStateManager.Current?.ActiveState is MapState)
		{
			if (pendingPlanningMode)
			{
				pendingPlanningMode = false;
				Homestead homestead = CurrentHomestead;
				if (homestead != null)
				{
					CustomMissions.StartHomesteadPlanningMission(homestead);
				}
			}
			else if (pendingPlacementMode)
			{
				pendingPlacementMode = false;
				Homestead hs = CurrentHomestead;
				if (hs != null && MapScreen.Instance != null)
				{
					GameMenu.ExitToLast();
					if (PlayerEncounter.Current != null)
					{
						PlayerEncounter.Finish();
					}
					int graduatedApprenticeCount = hs.GraduatedApprenticeCount;
					CampaignTimeControlMode prior = Campaign.Current.TimeControlMode;
					Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
					((HomesteadSettlementPlacementMapView)(object)MapScreen.Instance.AddMapView<HomesteadSettlementPlacementMapView>(Array.Empty<object>())).Initialize(hs, graduatedApprenticeCount, delegate(HomesteadSettlementPlacementMapView.Result result)
					{
						Campaign.Current.TimeControlMode = prior;
						InformationManager.DisplayMessage(new InformationMessage(ConvertHomesteadToSettlement(hs, result), Colors.Green));
					}, delegate
					{
						Campaign.Current.TimeControlMode = prior;
					});
				}
			}
			else if (_pendingTavernHomestead != null)
			{
				Homestead pendingTavernHomestead = _pendingTavernHomestead;
				_pendingTavernHomestead = null;
				try
				{
					if (Campaign.Current != null)
					{
						Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
					}
					CurrentHomestead = pendingTavernHomestead;
					TavernMissionActive = true;
					if (CustomMissions.StartHomesteadTavernMission(pendingTavernHomestead) == null)
					{
						TavernMissionActive = false;
					}
					Utilities.DisableGlobalLoadingWindow();
				}
				catch (Exception ex)
				{
					TavernMissionActive = false;
					TraceLogger.Write("HomesteadBehavior", "enter-tavern failed: " + ex.Message);
				}
			}
			else if (_pendingReturnHomestead != null)
			{
				Homestead pendingReturnHomestead = _pendingReturnHomestead;
				_pendingReturnHomestead = null;
				try
				{
					if (Campaign.Current != null)
					{
						Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
					}
					CurrentHomestead = pendingReturnHomestead;
					PendingTalkHero = (ReturnNearTavernEntrance ? null : pendingReturnHomestead.ArmsMasterHero);
					SparringMissionActive = false;
					TavernMissionActive = false;
					CustomMissions.StartHomesteadMission(pendingReturnHomestead);
					Utilities.DisableGlobalLoadingWindow();
				}
				catch (Exception ex2)
				{
					TraceLogger.Write("HomesteadBehavior", "return-to-homestead failed: " + ex2.Message);
				}
			}
			else if (SparringPending)
			{
				if (_sparSelectPhase == 0)
				{
					LaunchSparringMission();
				}
				else
				{
					TickSparringSelection(dt);
				}
			}
			else if (_pendingRaceHomestead != null && _raceNeedsSelection)
			{
				_raceNeedsSelection = false;
				Homestead hs2 = (CurrentHomestead = _pendingRaceHomestead);
				if (Campaign.Current != null)
				{
					Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
				}
				BeginRaceSetup(hs2);
			}
		}
		if (_pendingPostConversionSaveReload && Mission.Current == null && GameStateManager.Current?.ActiveState is MapState)
		{
			if (_postConversionSaveReloadDelayTicks > 0)
			{
				_postConversionSaveReloadDelayTicks--;
				return;
			}
			_pendingPostConversionSaveReload = false;
			TriggerPostConversionSaveReload();
		}
	}

	private void OnCampaignTick(float dt)
	{
		if (_pendingPlayerEncounterFinish && Mission.Current == null)
		{
			_pendingPlayerEncounterFinish = false;
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		}
		if ((SparringPending || _pendingTavernHomestead != null || _pendingRaceHomestead != null) && Mission.Current != null && Mission.Current.Mode != MissionMode.Conversation)
		{
			try
			{
				Mission.Current.EndMission();
			}
			catch
			{
			}
		}
		if (HomesteadMobileParties.Count == 0 || MobileParty.MainParty == null)
		{
			return;
		}
		mapVisualSyncTimer += dt;
		if (!(mapVisualSyncTimer < 1f))
		{
			mapVisualSyncTimer = 0f;
			Vec2 getPosition2D = MobileParty.MainParty.GetPosition2D;
			if (!hasLastPlayerMapPosition || !(getPosition2D.Distance(lastPlayerMapPosition) < 0.05f))
			{
				lastPlayerMapPosition = getPosition2D;
				hasLastPlayerMapPosition = true;
				SyncHomesteadMapVisuals("campaign tick");
			}
		}
	}

	private void AddGameMenusAndDialogs(CampaignGameStarter starter)
	{
		AddDialogs(starter);
		AddGameMenus(starter);
	}

	private void AddDialogs(CampaignGameStarter starter)
	{
		starter.AddDialogLine("homestead_converted_notable_safety_greeting", "start", "hero_main_options", "{=homestead_converted_notable_greeting}Well met, {?PLAYER.GENDER}my lady{?}my lord{\\?}. What can I do for you?", () => IsAnyConvertedNotableConvo(), null, 1);
		starter.AddDialogLine("homestead_patrol_talk_intercept", "start", "homestead_patrol_options", new TextObject("{=homestead_patrol_report}On patrol, sir! Awaiting your orders.").ToString(), () => IsPatrolConversation(), delegate
		{
			try
			{
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty?.MobileParty != null && Instance != null)
				{
					MobileParty mobileParty = encounteredParty.MobileParty;
					Instance.PatrolMobileParties.TryGetValue(mobileParty, out Homestead value);
					Instance.CurrentPatrolHomestead = value;
					Instance.CurrentPatrolParty = mobileParty;
					TraceLogger.Write("HomesteadBehavior", "Patrol dialog opened for '" + mobileParty.StringId + "' homestead='" + (value?.Name?.ToString() ?? "null") + "'");
				}
			}
			catch
			{
			}
		}, 200);
		starter.AddPlayerLine("homestead_patrol_option_follow", "homestead_patrol_options", "homestead_patrol_ack_follow", new TextObject("{=homestead_patrol_follow}Follow me.").ToString(), delegate
		{
			Homestead homestead = Instance?.CurrentPatrolHomestead;
			MobileParty mobileParty = Instance?.CurrentPatrolParty;
			return homestead != null && mobileParty != null && mobileParty.IsActive && !mobileParty.IsDisbanding && !homestead.PatrolFollowingPlayer;
		}, delegate
		{
			Instance?.CurrentPatrolHomestead?.SetPatrolFollowPlayer(follow: true);
		});
		starter.AddPlayerLine("homestead_patrol_option_resume", "homestead_patrol_options", "homestead_patrol_ack_resume", new TextObject("{=homestead_patrol_resume}Resume patrolling.").ToString(), delegate
		{
			Homestead homestead = Instance?.CurrentPatrolHomestead;
			MobileParty mobileParty = Instance?.CurrentPatrolParty;
			return homestead != null && mobileParty != null && mobileParty.IsActive && !mobileParty.IsDisbanding && homestead.PatrolFollowingPlayer;
		}, delegate
		{
			Instance?.CurrentPatrolHomestead?.SetPatrolFollowPlayer(follow: false);
		});
		starter.AddPlayerLine("homestead_patrol_option_join", "homestead_patrol_options", "homestead_patrol_ack_join", new TextObject("{=homestead_patrol_join}Join my troops.").ToString(), delegate
		{
			MobileParty mobileParty = Instance?.CurrentPatrolParty;
			return mobileParty != null && mobileParty.IsActive && !mobileParty.IsDisbanding;
		}, null);
		starter.AddPlayerLine("homestead_patrol_option_leave", "homestead_patrol_options", "close_window", new TextObject("{=homestead_patrol_encounter_leave}Carry on.").ToString(), () => true, delegate
		{
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		}, 99);
		starter.AddDialogLine("homestead_patrol_ack_follow_line", "homestead_patrol_ack_follow", "close_window", new TextObject("{=homestead_patrol_ack_follow}Right away, sir! Staying close.").ToString(), () => true, delegate
		{
			if (GlobalSettings<MCMSettings>.Instance.ShowPatrolNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_patrol_now_following", "The patrol will escort you.", 80f, 200f, 255f);
			}
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		});
		starter.AddDialogLine("homestead_patrol_ack_garrison_line", "homestead_patrol_ack_garrison", "close_window", new TextObject("{=homestead_patrol_ack_garrison}Understood, sir! Returning to the garrison!").ToString(), () => true, delegate
		{
			Homestead homestead = Instance?.CurrentPatrolHomestead;
			if (homestead != null)
			{
				int valueOrDefault = (Instance?.CurrentPatrolParty?.Party.NumberOfAllMembers).GetValueOrDefault();
				homestead.DisbandPatrolToGarrisonOrPlayerParty();
				Instance.CurrentPatrolParty = null;
				if (GlobalSettings<MCMSettings>.Instance.ShowPatrolNotifications)
				{
					Utils.PrintLocalizedMessage("homestead_patrol_garrisoned", "{TROOP_COUNT} troops from the patrol have returned to the garrison.", 80f, 200f, 80f, ("TROOP_COUNT", valueOrDefault.ToString()));
				}
			}
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		});
		starter.AddDialogLine("homestead_patrol_ack_join_line", "homestead_patrol_ack_join", "close_window", new TextObject("{=homestead_patrol_ack_join}Understood, sir! Joining your ranks!").ToString(), () => true, delegate
		{
			Homestead homestead = Instance?.CurrentPatrolHomestead;
			if (homestead != null)
			{
				int valueOrDefault = (Instance?.CurrentPatrolParty?.Party.NumberOfAllMembers).GetValueOrDefault();
				homestead.DisbandPatrolToGarrisonOrPlayerParty();
				Instance.CurrentPatrolParty = null;
				if (GlobalSettings<MCMSettings>.Instance.ShowPatrolNotifications)
				{
					Utils.PrintLocalizedMessage("homestead_patrol_joined", "{TROOP_COUNT} troops from the patrol have joined your party.", 80f, 200f, 80f, ("TROOP_COUNT", valueOrDefault.ToString()));
				}
			}
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		});
		starter.AddDialogLine("homestead_patrol_ack_resume_line", "homestead_patrol_ack_resume", "close_window", new TextObject("{=homestead_patrol_ack_resume}Yes, sir! Back to patrol!").ToString(), () => true, delegate
		{
			if (GlobalSettings<MCMSettings>.Instance.ShowPatrolNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_patrol_resumed", "The patrol has resumed their route.", 80f, 200f, 80f);
			}
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		});
		starter.AddDialogLine("homestead_villager_talk_intercept", "start", "homestead_villager_options", new TextObject("{=homestead_villager_greeting}{?PLAYER.GENDER}My lady{?}My lord{\\?}! Just hauling goods for the estate.").ToString(), () => IsOwnedVillagerConversation(), delegate
		{
			try
			{
				MobileParty mobileParty = PlayerEncounter.EncounteredParty?.MobileParty;
				if (mobileParty != null && Instance != null)
				{
					Instance.CurrentVillagerParty = mobileParty;
				}
			}
			catch
			{
			}
		}, 200);
		starter.AddPlayerLine("homestead_villager_option_follow", "homestead_villager_options", "homestead_villager_ack_follow", new TextObject("{=homestead_villager_follow}Follow me.").ToString(), delegate
		{
			MobileParty mobileParty = Instance?.CurrentVillagerParty;
			if (mobileParty != null && mobileParty.IsActive && !mobileParty.IsDisbanding)
			{
				HomesteadBehavior instance = Instance;
				if (instance == null)
				{
					return true;
				}
				return !instance.IsEscortingVillager(mobileParty);
			}
			return false;
		}, delegate
		{
			MobileParty mobileParty = Instance?.CurrentVillagerParty;
			if (mobileParty != null)
			{
				Instance?.StartEscortingVillager(mobileParty);
			}
		});
		starter.AddPlayerLine("homestead_villager_option_stop", "homestead_villager_options", "homestead_villager_ack_stop", new TextObject("{=homestead_villager_stop}You can head home now.").ToString(), delegate
		{
			MobileParty mobileParty = Instance?.CurrentVillagerParty;
			return mobileParty != null && mobileParty.IsActive && !mobileParty.IsDisbanding && (Instance?.IsEscortingVillager(mobileParty) ?? false);
		}, delegate
		{
			MobileParty mobileParty = Instance?.CurrentVillagerParty;
			if (mobileParty != null)
			{
				Instance?.StopEscortingVillager(mobileParty);
			}
		});
		starter.AddPlayerLine("homestead_villager_option_leave", "homestead_villager_options", "close_window", new TextObject("{=homestead_villager_leave}Carry on.").ToString(), () => true, delegate
		{
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		}, 99);
		starter.AddDialogLine("homestead_villager_ack_follow_line", "homestead_villager_ack_follow", "close_window", new TextObject("{=homestead_villager_ack_follow}Right away — we'll keep close to you.").ToString(), () => true, delegate
		{
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		});
		starter.AddDialogLine("homestead_villager_ack_stop_line", "homestead_villager_ack_stop", "close_window", new TextObject("{=homestead_villager_ack_stop}As you say — back to the estate we go.").ToString(), () => true, delegate
		{
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		});
		starter.AddDialogLine("homestead_recruiter_talk_intercept", "start", "homestead_recruiter_options", new TextObject("{=homestead_recruiter_report}We are headed to {HOMESTEAD_NAME}. Nothing to report, sir.").SetTextVariable("HOMESTEAD_NAME", Instance?.CurrentRecruiterHomestead?.Name?.ToString() ?? "our camp").ToString(), () => IsRecruiterConversation(), delegate
		{
			try
			{
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty?.MobileParty != null && Instance != null)
				{
					MobileParty mobileParty = encounteredParty.MobileParty;
					Instance.CurrentRecruiterParty = mobileParty;
					HomesteadRecruiterComponent homesteadRecruiterComponent = HomesteadRecruiterComponent.GetFor(mobileParty);
					Instance.CurrentRecruiterHomestead = homesteadRecruiterComponent?.HomeHomestead;
					TraceLogger.Write("HomesteadBehavior", "Recruiter dialog opened for '" + mobileParty.StringId + "' homestead='" + (homesteadRecruiterComponent?.HomeHomestead?.Name?.ToString() ?? "null") + "'");
				}
			}
			catch
			{
			}
		}, 201);
		starter.AddPlayerLine("homestead_recruiter_option_leave", "homestead_recruiter_options", "close_window", new TextObject("{=homestead_recruiter_encounter_leave}Carry on then.").ToString(), () => true, delegate
		{
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		});
		TextObject textObject = new TextObject("{=homestead_setup_new_dialog}We should set up a homestead here.");
		starter.AddPlayerLine("homestead_setup_new", "hero_main_options", "homestead_setup_options", textObject.ToString(), delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			return CurrentHomestead == null && oneToOneConversationHero != null && oneToOneConversationHero.PartyBelongedTo == MobileParty.MainParty && oneToOneConversationHero.Clan == Clan.PlayerClan && !oneToOneConversationHero.IsHumanPlayerCharacter && Hero.MainHero.CurrentSettlement == null;
		}, null, 500);
		starter.AddDialogLine("homestead_setup_accept", "homestead_setup_options", "close_window", Utils.GetLocalizedString("{=homestead_ok}okie dokie"), () => true, delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null)
			{
				CreateNewHomesteadAtPlayerLocation(oneToOneConversationHero);
			}
		}, 500);
		TextObject textObject2 = new TextObject("{=homestead_teardown_homestead_dialog}Let's pack up the homestead and leave.");
		starter.AddPlayerLine("homestead_teardown_homestead", "hero_main_options", "homestead_teardown_approval", textObject2.ToString(), delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			return CurrentHomestead != null && oneToOneConversationHero != null && CurrentHomestead.Leader == oneToOneConversationHero;
		}, null, 500);
		starter.AddDialogLine("homestead_teardown_accept", "homestead_teardown_approval", "close_window", Utils.GetLocalizedString("{=homestead_ok}okie dokie"), () => true, delegate
		{
			PackUpCurrentHomestead();
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.LeaveEncounter = true;
			}
		}, 500);
		TextObject textObject3 = new TextObject("{=homestead_change_leader_homestead_dialog}I'd like someone else to lead this homestead.");
		starter.AddPlayerLine("homestead_change_leader_homestead", "hero_main_options", "homestead_change_leader_approval", textObject3.ToString(), delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			return CurrentHomestead != null && oneToOneConversationHero != null && CurrentHomestead.Leader == oneToOneConversationHero && MobileParty.MainParty.MemberRoster.TotalHeroes > 1;
		}, null, 499);
		starter.AddDialogLine("homestead_change_leader_accept", "homestead_change_leader_approval", "homestead_change_leader_wait", Utils.GetLocalizedString("{=homestead_ok}okie dokie"), () => true, delegate
		{
			Utils.ShowSelectNewHomesteadLeaderScreen(CurrentHomestead, fromHomesteadMenu: false, endConversation: true);
		}, 500);
		starter.AddPlayerLine("homestead_change_leader_wait_continue", "homestead_change_leader_wait", "close_window", "{=homestead_continue}Continue.", null, null, 500);
		starter.AddPlayerLine("homestead_station_companions", "hero_main_options", "homestead_transfer_done", new TextObject("{=homestead_station_companions}I'd like to station some companions here.").ToString(), delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			return CurrentHomestead != null && oneToOneConversationHero != null && CurrentHomestead.Leader == oneToOneConversationHero && GetStationableCompanions().Count > 0;
		}, delegate
		{
			ShowStationCompanionsInquiry(CurrentHomestead);
		}, 120);
		starter.AddPlayerLine("homestead_retrieve_companions", "hero_main_options", "homestead_transfer_done", new TextObject("{=homestead_retrieve_companions}I'd like to take some companions back with me.").ToString(), delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			return CurrentHomestead != null && oneToOneConversationHero != null && CurrentHomestead.Leader == oneToOneConversationHero && GetRetrievableHomesteadHeroes(CurrentHomestead).Count > 0;
		}, delegate
		{
			ShowRetrieveCompanionsInquiry(CurrentHomestead);
		}, 119);
		starter.AddDialogLine("homestead_transfer_done_line", "homestead_transfer_done", "hero_main_options", new TextObject("{=homestead_transfer_done}As you wish.").ToString(), () => true, null);
		starter.AddPlayerLine("homestead_plan_homestead", "hero_main_options", "homestead_plan_homestead_npc", "{=homestead_plan_ask}Let's plan out our homestead.", delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			return CurrentHomestead != null && oneToOneConversationHero != null && CurrentHomestead.Leader == oneToOneConversationHero;
		}, null, 145);
		starter.AddDialogLine("homestead_plan_homestead_npc_response", "homestead_plan_homestead_npc", "close_window", "{=homestead_plan_npc_response}Of course! Take your time planning — come back when you've designed something you like.", () => true, delegate
		{
			pendingPlanningMode = true;
		});
		starter.AddPlayerLine("homestead_save_template", "hero_main_options", "homestead_template_save_prompt", "{=homestead_save_template_ask}I want to save this homestead's layout as a template.", delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			return CurrentHomestead != null && oneToOneConversationHero != null && CurrentHomestead.Leader == oneToOneConversationHero;
		}, null, 140);
		starter.AddDialogLine("homestead_template_save_prompt_line", "homestead_template_save_prompt", "homestead_template_saved", "{=homestead_template_name_prompt}What would you like to name this template?", () => true, delegate
		{
			Homestead homestead = CurrentHomestead;
			if (homestead?.GetHomesteadScene() == null || homestead.GetHomesteadScene().SavedEntities == null || homestead.GetHomesteadScene().SavedEntities.Count == 0)
			{
				Utils.PrintLocalizedMessage("homestead_template_empty", "There are no buildings to save as a template.", 255f, 200f, 80f);
			}
			else
			{
				Utils.ShowTextInputMessage("Save Template", "Enter template name:", delegate(string templateName)
				{
					if (!string.IsNullOrWhiteSpace(templateName))
					{
						SaveCurrentHomesteadTemplate(templateName);
					}
				});
			}
		});
		starter.AddDialogLine("homestead_template_saved_line", "homestead_template_saved", "close_window", "{=homestead_template_saved}Template '{TEMPLATE_NAME}' saved successfully. You can load it when building in any homestead.", () => true, delegate
		{
			try
			{
				PlayerEncounter.Finish();
			}
			catch
			{
			}
		});
		string[] array = new string[3] { "companion_talk", "lord_pretalk", "hero_main_options" };
		foreach (string text in array)
		{
			starter.AddPlayerLine("homestead_companion_follow_" + text, text, "homestead_companion_follow_ack", new TextObject("{=companion_follow_option}Follow me.").ToString(), delegate
			{
				if (TavernMissionActive)
				{
					return false;
				}
				Hero hero = GetConversationHero();
				return hero != null && !HomesteadSpawningMissionLogic.IsHeroFollowingStatic(hero);
			}, delegate
			{
				Hero hero = GetConversationHero();
				if (hero != null)
				{
					GetSpawner()?.SetHeroFollowing(hero, follow: true);
				}
			}, 200);
			starter.AddPlayerLine("homestead_companion_unfollow_" + text, text, "homestead_companion_unfollow_ack", new TextObject("{=companion_unfollow_option}Stay here for now.").ToString(), delegate
			{
				if (TavernMissionActive)
				{
					return false;
				}
				Hero hero = GetConversationHero();
				return hero != null && HomesteadSpawningMissionLogic.IsHeroFollowingStatic(hero);
			}, delegate
			{
				Hero hero = GetConversationHero();
				if (hero != null)
				{
					GetSpawner()?.SetHeroFollowing(hero, follow: false);
				}
			}, 200);
		}
		starter.AddDialogLine("homestead_companion_follow_ack_line", "homestead_companion_follow_ack", "close_window", new TextObject("{=companion_follow_ack}Of course. I'll stay close.").ToString(), () => true, null);
		starter.AddDialogLine("homestead_companion_unfollow_ack_line", "homestead_companion_unfollow_ack", "close_window", new TextObject("{=companion_unfollow_ack}Understood. I'll get back to what I was doing.").ToString(), () => true, null);
		starter.AddDialogLine("homestead_houndmaster_mastery_offer", "start", "homestead_houndmaster_mastery_options", "{=homestead_houndmaster_mastery_offer}My lord, the hounds have grown strong and disciplined under my training. With your permission, I can teach them a new trick to knock your enemies off their feet in battle.", delegate
		{
			if (!IsHoundMasterHomesteadConvo() || Instance == null || Instance.HasHoundmasterKnockdownUnlocked)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			int value;
			return Instance._notableApprenticeCount.TryGetValue(stringId, out value) && value >= 1;
		}, null, 300);
		starter.AddPlayerLine("homestead_houndmaster_mastery_ask_settlement", "hero_main_options", "homestead_houndmaster_mastery_offer_settlement_state", "{=homestead_houndmaster_mastery_ask_settlement}The hounds seem sharper lately. Have you taught them anything new?", delegate
		{
			if (!IsHoundMasterConvo() || Instance == null || Instance.CurrentHomestead != null || Instance.HasHoundmasterKnockdownUnlocked)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			int value;
			return Instance._notableApprenticeCount.TryGetValue(stringId, out value) && value >= 1;
		}, null, 150);
		starter.AddDialogLine("homestead_houndmaster_mastery_offer_settlement", "homestead_houndmaster_mastery_offer_settlement_state", "homestead_houndmaster_mastery_options", "{=homestead_houndmaster_mastery_offer}My lord, the hounds have grown strong and disciplined under my training. With your permission, I can teach them a new trick to knock your enemies off their feet in battle.", () => true, null);
		starter.AddDialogLine("homestead_houndmaster_no_dogs", "start", "homestead_houndmaster_nodogs_options", "{=homestead_houndmaster_no_dogs}I am still trying to catch and tame some wild dogs, my lord. We need some in the kennel before I can offer you a companion.", delegate
		{
			ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>("dog");
			int num2 = ((Instance?.CurrentHomestead?.Stash != null && itemObject != null) ? Instance.CurrentHomestead.Stash.GetItemNumber(itemObject) : 0);
			return IsHoundMasterConvo() && Instance?.CurrentHomestead != null && num2 == 0 && (Instance == null || !Instance.HasAdoptedDog);
		}, null, 200);
		starter.AddPlayerLine("homestead_houndmaster_mastery_accept", "homestead_houndmaster_mastery_options", "homestead_houndmaster_mastery_accepted", "{=homestead_houndmaster_mastery_accept}That sounds excellent. Teach them everything you know.", null, delegate
		{
			if (Instance != null)
			{
				Instance.HasHoundmasterKnockdownUnlocked = true;
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero != null)
				{
					string text5 = oneToOneConversationHero.FirstName?.ToString() ?? oneToOneConversationHero.Name.ToString();
					oneToOneConversationHero.SetName(new TextObject("{=homestead_grand_houndmaster}Grand Houndmaster {BASE_NAME}").SetTextVariable("BASE_NAME", text5), new TextObject(text5));
				}
			}
		});
		starter.AddPlayerLine("homestead_houndmaster_mastery_decline", "homestead_houndmaster_mastery_options", "close_window", "{=homestead_houndmaster_mastery_decline}Not right now. Carry on.", null, null);
		starter.AddDialogLine("homestead_houndmaster_mastery_done", "homestead_houndmaster_mastery_accepted", "close_window", "{=homestead_houndmaster_mastery_done}Right away, my lord! The enemies of your house will fear our pack.", null, null);
		starter.AddPlayerLine("homestead_houndmaster_nodogs_leave", "homestead_houndmaster_nodogs_options", "close_window", "{=homestead_houndmaster_nodogs_leave}Understood. Carry on.", () => true, null);
		starter.AddDialogLine("homestead_houndmaster_offer_dog", "start", "homestead_houndmaster_offer_options", "{=homestead_houndmaster_offer_dog}We have {DOG_COUNT} dogs ready, my lord. Shall I set one aside for you?", delegate
		{
			ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>("dog");
			int num2 = ((Instance?.CurrentHomestead?.Stash != null && itemObject != null) ? Instance.CurrentHomestead.Stash.GetItemNumber(itemObject) : 0);
			MBTextManager.SetTextVariable("DOG_COUNT", num2);
			return IsHoundMasterConvo() && num2 > 0 && (Instance == null || !Instance.HasAdoptedDog);
		}, null, 200);
		starter.AddPlayerLine("homestead_houndmaster_accept", "homestead_houndmaster_offer_options", "close_window", "{=homestead_houndmaster_accept}Yes, I'll take a companion.", () => true, delegate
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(Utils.GetLocalizedString("{=homestead_dog_name_title}Name your dog"), Utils.GetLocalizedString("{=homestead_dog_name_prompt}What will you name your companion?"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Utils.GetLocalizedString("{=homestead_dog_name_next}Next"), GameTexts.FindText("str_cancel").ToString(), delegate(string enteredName)
			{
				if (!string.IsNullOrWhiteSpace(enteredName) && Instance?.CurrentHomestead != null)
				{
					Homestead hs = Instance.CurrentHomestead;
					InformationManager.ShowInquiry(new InquiryData(Utils.GetLocalizedString("{=homestead_dog_coat_title}Choose a Coat"), Utils.GetLocalizedString("{=homestead_dog_coat_desc}What coat does {DOG_NAME} have?\n\nTawny – golden-brown\nSable – dark black\nPiebald – white with patches", ("DOG_NAME", enteredName)), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Utils.GetLocalizedString("{=homestead_dog_coat_tawny}Tawny"), Utils.GetLocalizedString("{=homestead_dog_coat_sablepiebald}Sable / Piebald"), delegate
					{
						Instance?.AdoptDog(enteredName, hs);
						InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_dog_follows}{DOG_NAME} now follows you.", ("DOG_NAME", enteredName))));
					}, delegate
					{
						InformationManager.ShowInquiry(new InquiryData(Utils.GetLocalizedString("{=homestead_dog_coat_title}Choose a Coat"), Utils.GetLocalizedString("{=homestead_dog_coat_which}Which coat for {DOG_NAME}?", ("DOG_NAME", enteredName)), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Utils.GetLocalizedString("{=homestead_dog_coat_sable}Sable"), Utils.GetLocalizedString("{=homestead_dog_coat_piebald}Piebald"), delegate
						{
							Instance?.AdoptDog(enteredName, hs, 1);
							InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_dog_follows}{DOG_NAME} now follows you.", ("DOG_NAME", enteredName))));
						}, delegate
						{
							Instance?.AdoptDog(enteredName, hs, 2);
							InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_dog_follows}{DOG_NAME} now follows you.", ("DOG_NAME", enteredName))));
						}));
					}));
				}
			}, null));
		});
		starter.AddPlayerLine("homestead_houndmaster_decline", "homestead_houndmaster_offer_options", "close_window", "{=homestead_houndmaster_decline}Not today.", () => true, null);
		starter.AddDialogLine("homestead_houndmaster_settlement_offer_dog", "hero_main_options", "homestead_houndmaster_settlement_offer_options", "{=homestead_houndmaster_settlement_offer_dog}The kennels here are well stocked, my lord. For {DOG_COST}{GOLD_ICON}, I can set aside a fine hound for you.", delegate
		{
			if (!IsHoundMasterConvo() || Instance?.CurrentHomestead != null || (Instance != null && Instance.HasAdoptedDog))
			{
				return false;
			}
			MBTextManager.SetTextVariable("DOG_COST", 1000);
			MBTextManager.SetTextVariable("GOLD_ICON", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			return true;
		}, null, 200);
		starter.AddPlayerLine("homestead_houndmaster_settlement_accept", "homestead_houndmaster_settlement_offer_options", "close_window", "{=homestead_houndmaster_settlement_accept}Yes, I'll take a companion.", () => Hero.MainHero.Gold >= 1000, delegate
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(Utils.GetLocalizedString("{=homestead_dog_name_title}Name your dog"), Utils.GetLocalizedString("{=homestead_dog_name_prompt}What will you name your companion?"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Utils.GetLocalizedString("{=homestead_dog_name_next}Next"), GameTexts.FindText("str_cancel").ToString(), delegate(string enteredName)
			{
				if (!string.IsNullOrWhiteSpace(enteredName))
				{
					if (Hero.MainHero.Gold < 1000)
					{
						InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_dog_cant_afford}You can't afford that.")));
					}
					else
					{
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 1000, disableNotification: true);
						InformationManager.ShowInquiry(new InquiryData(Utils.GetLocalizedString("{=homestead_dog_coat_title}Choose a Coat"), Utils.GetLocalizedString("{=homestead_dog_coat_desc}What coat does {DOG_NAME} have?\n\nTawny – golden-brown\nSable – dark black\nPiebald – white with patches", ("DOG_NAME", enteredName)), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Utils.GetLocalizedString("{=homestead_dog_coat_tawny}Tawny"), Utils.GetLocalizedString("{=homestead_dog_coat_sablepiebald}Sable / Piebald"), delegate
						{
							Instance?.AdoptDog(enteredName, null);
							InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_dog_follows}{DOG_NAME} now follows you.", ("DOG_NAME", enteredName))));
						}, delegate
						{
							InformationManager.ShowInquiry(new InquiryData(Utils.GetLocalizedString("{=homestead_dog_coat_title}Choose a Coat"), Utils.GetLocalizedString("{=homestead_dog_coat_which}Which coat for {DOG_NAME}?", ("DOG_NAME", enteredName)), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Utils.GetLocalizedString("{=homestead_dog_coat_sable}Sable"), Utils.GetLocalizedString("{=homestead_dog_coat_piebald}Piebald"), delegate
							{
								Instance?.AdoptDog(enteredName, null, 1);
								InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_dog_follows}{DOG_NAME} now follows you.", ("DOG_NAME", enteredName))));
							}, delegate
							{
								Instance?.AdoptDog(enteredName, null, 2);
								InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_dog_follows}{DOG_NAME} now follows you.", ("DOG_NAME", enteredName))));
							}));
						}));
					}
				}
			}, null));
		});
		starter.AddPlayerLine("homestead_houndmaster_settlement_decline", "homestead_houndmaster_settlement_offer_options", "close_window", "{=homestead_houndmaster_settlement_decline}Not today.", () => true, null);
		starter.AddDialogLine("homestead_houndmaster_has_dog", "start", "homestead_houndmaster_has_dog_options", "{=homestead_houndmaster_has_dog}{DOG_NAME} is a fine beast. Loyal to you through and through.", delegate
		{
			if (IsHoundMasterHomesteadConvo() && Instance != null && Instance.HasAdoptedDog)
			{
				MBTextManager.SetTextVariable("DOG_NAME", Instance.AdoptedDogName);
				return true;
			}
			return false;
		}, null, 200);
		starter.AddPlayerLine("homestead_houndmaster_has_dog_ask_settlement", "hero_main_options", "homestead_houndmaster_has_dog_settlement_state", "{=homestead_houndmaster_has_dog_ask_settlement}How is {DOG_NAME} getting on?", delegate
		{
			if (!IsHoundMasterConvo() || Instance == null || Instance.CurrentHomestead != null || !Instance.HasAdoptedDog)
			{
				return false;
			}
			MBTextManager.SetTextVariable("DOG_NAME", Instance.AdoptedDogName);
			return true;
		}, null, 150);
		starter.AddDialogLine("homestead_houndmaster_has_dog_settlement", "homestead_houndmaster_has_dog_settlement_state", "homestead_houndmaster_has_dog_options", "{=homestead_houndmaster_has_dog}{DOG_NAME} is a fine beast. Loyal to you through and through.", () => true, null);
		starter.AddPlayerLine("homestead_houndmaster_has_dog_leave", "homestead_houndmaster_has_dog_options", "close_window", "{=homestead_houndmaster_has_dog_leave}Good to know.", () => true, null);
		starter.AddPlayerLine("homestead_houndmaster_release_dog", "homestead_houndmaster_has_dog_options", "close_window", "{=homestead_houndmaster_release_dog}I need to let {DOG_NAME} go.", () => true, delegate
		{
			if (Instance != null)
			{
				string adoptedDogName = Instance.AdoptedDogName;
				Instance.ReleaseDog();
				InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_dog_returned}{DOG_NAME} has returned to the kennel.", ("DOG_NAME", adoptedDogName))));
			}
		});
		starter.AddPlayerLine("homestead_marketlady_mastery_ask", "hero_main_options", "homestead_marketlady_mastery_offer_state", "{=homestead_marketlady_mastery_ask}Do you have a way to improve our trading position?", delegate
		{
			if (!IsMarketLadyConvo() || Instance == null || Instance.HasMarketLadyTradeDiscountUnlocked)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			int value;
			return Instance._notableApprenticeCount.TryGetValue(stringId, out value) && value >= 1;
		}, null);
		starter.AddDialogLine("homestead_marketlady_mastery_offer", "homestead_marketlady_mastery_offer_state", "homestead_marketlady_mastery_options", "{=homestead_marketlady_mastery_offer}My lord, with the connections we've made, I can now ensure our goods fetch the best prices, and our purchases cost us less, no matter where you trade.", () => true, null);
		starter.AddPlayerLine("homestead_marketlady_mastery_ask_homestead", "homestead_marketlady_options", "homestead_marketlady_mastery_offer_state", "{=homestead_marketlady_mastery_ask}Do you have a way to improve our trading position?", delegate
		{
			if (!IsMarketLadyHomesteadConvo() || Instance == null || Instance.HasMarketLadyTradeDiscountUnlocked)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			int value;
			return Instance._notableApprenticeCount.TryGetValue(stringId, out value) && value >= 1;
		}, null);
		starter.AddPlayerLine("homestead_marketlady_mastery_accept", "homestead_marketlady_mastery_options", "homestead_marketlady_mastery_accepted", "{=homestead_marketlady_mastery_accept}That is an excellent strategy. Proceed immediately.", null, delegate
		{
			if (Instance != null)
			{
				Instance.HasMarketLadyTradeDiscountUnlocked = true;
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero != null)
				{
					string text5 = oneToOneConversationHero.FirstName?.ToString() ?? oneToOneConversationHero.Name.ToString();
					oneToOneConversationHero.SetName(new TextObject("{=homestead_trade_guru}Trade Guru {BASE_NAME}").SetTextVariable("BASE_NAME", text5), new TextObject(text5));
				}
			}
		});
		starter.AddPlayerLine("homestead_marketlady_mastery_decline", "homestead_marketlady_mastery_options", "close_window", "{=homestead_marketlady_mastery_decline}Hold off on that for now.", null, null);
		starter.AddDialogLine("homestead_marketlady_mastery_done", "homestead_marketlady_mastery_accepted", "close_window", "{=homestead_marketlady_mastery_done}Right away, my lord! I will send word to our contacts.", null, null);
		starter.AddDialogLine("homestead_marketlady_greeting", "start", "homestead_marketlady_options", "{=homestead_marketlady_greeting}Welcome, my lord. The market stalls are busy today. Shall I tell you what the traders have been saying?", () => IsMarketLadyHomesteadConvo(), null, 200);
		starter.AddPlayerLine("homestead_marketlady_ask_rumors", "homestead_marketlady_options", "homestead_marketlady_rumors", "{=homestead_marketlady_ask_rumors}What are the prices looking like?", () => true, null);
		starter.AddPlayerLine("homestead_marketlady_goodbye", "homestead_marketlady_options", "close_window", "{=homestead_marketlady_goodbye}Not right now, thank you.", () => true, null);
		starter.AddDialogLine("homestead_stablemaster_greeting", "start", "homestead_stablemaster_options", "{=homestead_stablemaster_greeting}The horses are restless today, my lord. What would you have of me?", () => IsStableMasterHomesteadConvo(), null, 200);
		starter.AddPlayerLine("homestead_stablemaster_ask_settlement", "hero_main_options", "homestead_stablemaster_greet_settlement_state", "{=homestead_stablemaster_ask_settlement}How are the horses coming along?", () => IsStableMasterConvo() && Instance != null && Instance.CurrentHomestead == null, null, 150);
		starter.AddDialogLine("homestead_stablemaster_greet_settlement", "homestead_stablemaster_greet_settlement_state", "homestead_stablemaster_options", "{=homestead_stablemaster_greeting}The horses are restless today, my lord. What would you have of me?", () => true, null);
		starter.AddPlayerLine("homestead_stablemaster_race", "homestead_stablemaster_options", "homestead_stablemaster_race_ack", "{=homestead_stablemaster_race}Let's hold a horse race.", () => IsStableMasterConvo(), null);
		starter.AddDialogLine("homestead_stablemaster_race_ack_line", "homestead_stablemaster_race_ack", "close_window", "{=homestead_stablemaster_race_ack}A fine day for it! Ride out to the track and we'll line up.", () => true, delegate
		{
			if (Instance?.CurrentHomestead == null)
			{
				Instance?.QueueArenaRace();
			}
			else
			{
				Instance?.QueueRace();
			}
		});
		starter.AddPlayerLine("homestead_stablemaster_records", "homestead_stablemaster_options", "homestead_stablemaster_records_line", "{=homestead_stablemaster_records}How have the races been going?", () => IsStableMasterConvo(), null);
		starter.AddDialogLine("homestead_stablemaster_records_response", "homestead_stablemaster_records_line", "homestead_stablemaster_options", "{=homestead_stablemaster_records_response}Let me tell you who's been quickest, my lord.", () => true, delegate
		{
			Instance?.ShowRaceRecords();
		});
		starter.AddPlayerLine("homestead_stablemaster_goodbye", "homestead_stablemaster_options", "close_window", "{=homestead_stablemaster_goodbye}Carry on.", () => true, null);
		starter.AddDialogLine("homestead_stablemaster_mastery_offer", "start", "homestead_stablemaster_mastery_options", "{=homestead_stablemaster_mastery_offer}{HONORIFIC_CAP}, I've raised riders and bred mounts until I know horseflesh better than most know kin. Name me your Grand Stablemistress and the finest steeds in the land will come from these stables.", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (!IsStableMasterConvo() || Instance == null || homestead == null || Instance.HasStableMasterMasteryUnlocked)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			if (!Instance._notableApprenticeCount.TryGetValue(stringId, out var value) || value < 1)
			{
				return false;
			}
			GameTexts.SetVariable("HONORIFIC_CAP", GetPlayerHonorificCapitalized());
			return true;
		}, null, 300);
		starter.AddPlayerLine("homestead_stablemaster_mastery_accept", "homestead_stablemaster_mastery_options", "homestead_stablemaster_mastery_accepted", "{=homestead_stablemaster_mastery_accept}You've earned it. Rise, Grand Stablemistress.", null, delegate
		{
			if (Instance != null)
			{
				Instance.HasStableMasterMasteryUnlocked = true;
				Homestead homestead = Instance.CurrentHomestead;
				if (homestead != null)
				{
					homestead.StableMasterMasteryUnlocked = true;
				}
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero != null)
				{
					string text5 = oneToOneConversationHero.FirstName?.ToString() ?? oneToOneConversationHero.Name.ToString();
					oneToOneConversationHero.SetName(new TextObject("{=homestead_grand_stablemistress}Grand Stablemistress {BASE_NAME}").SetTextVariable("BASE_NAME", text5), new TextObject(text5));
				}
			}
		});
		starter.AddPlayerLine("homestead_stablemaster_mastery_decline", "homestead_stablemaster_mastery_options", "close_window", "{=homestead_stablemaster_mastery_decline}Maybe later.", null, null);
		starter.AddDialogLine("homestead_stablemaster_mastery_done", "homestead_stablemaster_mastery_accepted", "close_window", "{=homestead_stablemaster_mastery_done}You honour me, {HONORIFIC}. The stables will not disappoint you.", delegate
		{
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null);
		starter.AddDialogLine("homestead_marketlady_rumors_line", "homestead_marketlady_rumors", "homestead_marketlady_post_rumors", "{=homestead_marketlady_rumors}{TRADE_RUMOR_TEXT}", delegate
		{
			if (!IsMarketLadyConvo())
			{
				return false;
			}
			Homestead homestead = Instance?.CurrentHomestead;
			MBTextManager.SetTextVariable("TRADE_RUMOR_TEXT", (homestead != null) ? BuildTradeRumorText(homestead) : "The traders speak little these days, my lord.");
			return true;
		}, null, 200);
		starter.AddPlayerLine("homestead_marketlady_post_rumors_leave", "homestead_marketlady_post_rumors", "close_window", "{=homestead_marketlady_post_rumors_leave}Thank you. I'll keep that in mind.", () => true, null);
		starter.AddPlayerLine("homestead_marketlady_post_rumors_again", "homestead_marketlady_post_rumors", "homestead_marketlady_options", "{=homestead_marketlady_post_rumors_again}Tell me more.", () => true, null);
		starter.AddDialogLine("homestead_ambassador_mastery_offer", "start", "homestead_ambassador_mastery_options", "{=homestead_ambassador_mastery_offer}My lord, we have achieved much together. With your blessing, I will begin writing ahead to every notable figure in the realm. My tactful introductions will ensure they respect you before you even arrive.", delegate
		{
			if (!IsAmbassadorHomesteadConvo() || Instance == null || Instance.HasAmbassadorTactfulIntroductionUnlocked)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			int value;
			return Instance._notableApprenticeCount.TryGetValue(stringId, out value) && value >= 1;
		}, null, 300);
		starter.AddPlayerLine("homestead_ambassador_mastery_ask_settlement", "hero_main_options", "homestead_ambassador_mastery_offer_settlement_state", "{=homestead_ambassador_mastery_ask_settlement}Have you any new strategy for winning over the local nobility?", delegate
		{
			if (!IsAmbassadorConvo() || Instance == null || Instance.CurrentHomestead != null || Instance.HasAmbassadorTactfulIntroductionUnlocked)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			int value;
			return Instance._notableApprenticeCount.TryGetValue(stringId, out value) && value >= 1;
		}, null, 150);
		starter.AddDialogLine("homestead_ambassador_mastery_offer_settlement", "homestead_ambassador_mastery_offer_settlement_state", "homestead_ambassador_mastery_options", "{=homestead_ambassador_mastery_offer}My lord, we have achieved much together. With your blessing, I will begin writing ahead to every notable figure in the realm. My tactful introductions will ensure they respect you before you even arrive.", () => true, null);
		starter.AddPlayerLine("homestead_ambassador_mastery_accept", "homestead_ambassador_mastery_options", "homestead_ambassador_mastery_accepted", "{=homestead_ambassador_mastery_accept}That is an excellent strategy. Proceed immediately.", null, delegate
		{
			if (Instance != null)
			{
				Instance.HasAmbassadorTactfulIntroductionUnlocked = true;
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero != null)
				{
					string text5 = oneToOneConversationHero.FirstName?.ToString() ?? oneToOneConversationHero.Name.ToString();
					oneToOneConversationHero.SetName(new TextObject("{=homestead_lord_ambassador}Lord Ambassador {BASE_NAME}").SetTextVariable("BASE_NAME", text5), new TextObject(text5));
				}
			}
		});
		starter.AddPlayerLine("homestead_ambassador_mastery_decline", "homestead_ambassador_mastery_options", "close_window", "{=homestead_ambassador_mastery_decline}Hold off on that for now.", null, null);
		starter.AddDialogLine("homestead_ambassador_mastery_done", "homestead_ambassador_mastery_accepted", "close_window", "{=homestead_ambassador_mastery_done}At once, my lord. The ravens will fly this very night.", null, null);
		starter.AddDialogLine("homestead_ambassador_greeting", "start", "homestead_ambassador_options", "{=homestead_ambassador_greeting}My lord. The hall is always open. I have been making overtures to the nearby notables and passing lords on your behalf.", () => IsAmbassadorHomesteadConvo(), null, 200);
		starter.AddPlayerLine("homestead_ambassador_ask_settlement", "hero_main_options", "homestead_ambassador_greet_settlement_state", "{=homestead_ambassador_ask_settlement}How do our diplomatic efforts fare?", () => IsAmbassadorConvo() && Instance != null && Instance.CurrentHomestead == null, null, 150);
		starter.AddDialogLine("homestead_ambassador_greet_settlement", "homestead_ambassador_greet_settlement_state", "homestead_ambassador_options", "{=homestead_ambassador_greeting}My lord. The hall is always open. I have been making overtures to the nearby notables and passing lords on your behalf.", () => true, null);
		starter.AddPlayerLine("homestead_ambassador_ask_relations", "homestead_ambassador_options", "homestead_ambassador_relations", "{=homestead_ambassador_ask_relations}How go the diplomatic efforts?", () => true, null);
		starter.AddPlayerLine("homestead_ambassador_task_headman", "homestead_ambassador_options", "homestead_ambassador_task_headman_resp", "{=homestead_ambassador_task_headman}Turn your efforts to winning over {HEADMAN} of {VILLAGE} — we need their blessing to expand.", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (!IsAmbassadorConvo() || homestead == null || homestead.AmbassadorAidingHeadman)
			{
				return false;
			}
			HomesteadHeadmanTrustQuest activeHeadmanTrustQuest = Instance.GetActiveHeadmanTrustQuest(homestead);
			if (activeHeadmanTrustQuest == null)
			{
				return false;
			}
			MBTextManager.SetTextVariable("HEADMAN", activeHeadmanTrustQuest.Headman?.Name ?? new TextObject("{=homestead_the_headman}the headman"));
			MBTextManager.SetTextVariable("VILLAGE", activeHeadmanTrustQuest.Village?.Name ?? new TextObject("{=homestead_the_nearby_village}the nearby village"));
			return true;
		}, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead != null)
			{
				homestead.AmbassadorAidingHeadman = true;
			}
		});
		starter.AddDialogLine("homestead_ambassador_task_headman_done", "homestead_ambassador_task_headman_resp", "close_window", "{=homestead_ambassador_task_headman_reply}I will devote myself to it, my lord. Each day I will press your case with them until they come around.", () => true, null);
		starter.AddPlayerLine("homestead_ambassador_task_landpatent", "homestead_ambassador_options", "homestead_ambassador_task_landpatent_resp", "{=homestead_ambassador_task_landpatent}Turn your efforts to winning the favour of {CLAN} — we need their land patent to expand.", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (!IsAmbassadorConvo() || homestead == null || homestead.AmbassadorAidingHeadman)
			{
				return false;
			}
			HomesteadLandPatentQuest activeLandPatentQuest = Instance.GetActiveLandPatentQuest(homestead);
			if (activeLandPatentQuest == null)
			{
				return false;
			}
			MBTextManager.SetTextVariable("CLAN", activeLandPatentQuest.OwningClan?.Name ?? new TextObject("{=homestead_the_local_clan}the local ruling house"));
			return true;
		}, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead != null)
			{
				homestead.AmbassadorAidingHeadman = true;
			}
		});
		starter.AddDialogLine("homestead_ambassador_task_landpatent_done", "homestead_ambassador_task_landpatent_resp", "close_window", "{=homestead_ambassador_task_landpatent_reply}I will press your case with their house daily, my lord, until they relent.", () => true, null);
		starter.AddPlayerLine("homestead_ambassador_task_charter", "homestead_ambassador_options", "homestead_ambassador_task_charter_resp", "{=homestead_ambassador_task_charter}Turn your efforts to winning the favour of {RULER} — we need a charter to raise our settlement.", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (!IsAmbassadorConvo() || homestead == null || homestead.AmbassadorAidingHeadman)
			{
				return false;
			}
			HomesteadSettlementCharterQuest activeSettlementCharterQuest = Instance.GetActiveSettlementCharterQuest(homestead);
			if (activeSettlementCharterQuest?.Ruler == null)
			{
				return false;
			}
			MBTextManager.SetTextVariable("RULER", activeSettlementCharterQuest.Ruler.Name);
			return true;
		}, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead != null)
			{
				homestead.AmbassadorAidingHeadman = true;
			}
		});
		starter.AddDialogLine("homestead_ambassador_task_charter_done", "homestead_ambassador_task_charter_resp", "close_window", "{=homestead_ambassador_task_charter_reply}I will press your suit at court, my lord, until the crown relents.", () => true, null);
		starter.AddPlayerLine("homestead_ambassador_stop_headman", "homestead_ambassador_options", "homestead_ambassador_stop_headman_resp", "{=homestead_ambassador_stop_headman}You may set aside your efforts with the village headman for now.", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (!IsAmbassadorConvo() || homestead == null || !homestead.AmbassadorAidingHeadman)
			{
				return false;
			}
			if (Instance?.GetActiveHeadmanTrustQuest(homestead) != null)
			{
				return true;
			}
			homestead.AmbassadorAidingHeadman = false;
			return false;
		}, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead != null)
			{
				homestead.AmbassadorAidingHeadman = false;
			}
		});
		starter.AddDialogLine("homestead_ambassador_stop_headman_done", "homestead_ambassador_stop_headman_resp", "close_window", "{=homestead_ambassador_stop_headman_reply}As you wish, my lord. I will turn my attention elsewhere.", () => true, null);
		starter.AddPlayerLine("homestead_ambassador_goodbye", "homestead_ambassador_options", "close_window", "{=homestead_ambassador_goodbye}Keep up the good work.", () => true, null);
		starter.AddDialogLine("homestead_ambassador_relations_report", "homestead_ambassador_relations", "homestead_ambassador_post_relations", "{=homestead_ambassador_relations_report}{AMBASSADOR_REPORT_TEXT}", delegate
		{
			if (!IsAmbassadorConvo())
			{
				return false;
			}
			Homestead homestead = Instance?.CurrentHomestead;
			string text5;
			if (homestead != null)
			{
				text5 = BuildAmbassadorReportText(homestead);
			}
			else
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				text5 = ((currentSettlement != null) ? BuildAmbassadorReportText(currentSettlement.GetPosition2D) : new TextObject("{=homestead_ambassador_no_report}I have no report at this time, my lord.").ToString());
			}
			MBTextManager.SetTextVariable("AMBASSADOR_REPORT_TEXT", text5);
			return true;
		}, null, 200);
		starter.AddPlayerLine("homestead_ambassador_relations_leave", "homestead_ambassador_post_relations", "close_window", "{=homestead_ambassador_relations_leave}Well said.", () => true, null);
		array = new string[9] { "homestead_marketlady_options", "homestead_ambassador_options", "homestead_houndmaster_offer_options", "homestead_houndmaster_has_dog_options", "homestead_houndmaster_nodogs_options", "homestead_arms_master_options", "homestead_tavernkeeper_options", "homestead_mastersmith_options", "homestead_stablemaster_options" };
		foreach (string text2 in array)
		{
			starter.AddPlayerLine("homestead_task_ask_" + text2, text2, "homestead_task_offer_npc", "{=homestead_favor_ask}Is there anything you need from me?", delegate
			{
				string title;
				Hero hero = Instance?.GetConversationNotable(out title);
				return hero != null && !Instance.HasActiveQuestForNotable(hero);
			}, delegate
			{
				string title;
				Hero hero = Instance?.GetConversationNotable(out title);
				if (hero != null && Instance != null)
				{
					bool num2 = Instance.GetActiveFavorQuest(hero) == null;
					bool flag = Instance.GetActiveDeliveryQuest(hero) == null;
					bool flag2 = Instance.CanOfferRaid(Instance.CurrentHomestead);
					bool flag3 = Instance.CanOfferApparel(hero);
					bool flag4 = Instance.CanOfferBuilding(hero);
					bool flag5 = Instance.CanOfferApprentice(hero);
					List<PendingOfferType> list = new List<PendingOfferType>();
					if (num2)
					{
						list.Add(PendingOfferType.Fetch);
					}
					if (flag)
					{
						list.Add(PendingOfferType.Delivery);
					}
					if (flag2)
					{
						list.Add(PendingOfferType.Raid);
					}
					if (flag3)
					{
						list.Add(PendingOfferType.Apparel);
					}
					if (flag4)
					{
						list.Add(PendingOfferType.Building);
					}
					if (flag5)
					{
						list.Add(PendingOfferType.Apprentice);
					}
					if (Instance._forcedOfferType.HasValue)
					{
						Instance._pendingOfferType = Instance._forcedOfferType.Value;
					}
					else
					{
						Instance._pendingOfferType = ((list.Count > 0) ? list[MBRandom.RandomInt(list.Count)] : PendingOfferType.Fetch);
					}
				}
			}, 80);
			starter.AddPlayerLine("homestead_task_about_" + text2, text2, "homestead_task_status_npc", "{=homestead_favor_about}About that task you gave me...", delegate
			{
				string title;
				Hero hero = Instance?.GetConversationNotable(out title);
				return hero != null && Instance.HasActiveQuestForNotable(hero);
			}, null, 80);
		}
		starter.AddDialogLine("homestead_favor_offer", "homestead_task_offer_npc", "homestead_favor_offer_options", "{=homestead_favor_offer}As it happens, yes. If you could bring {FAVOR_COUNT} {FAVOR_ITEM} to me, it would help us greatly.", delegate
		{
			if (Instance._pendingOfferType != PendingOfferType.Fetch)
			{
				return false;
			}
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero == null || Instance.GetActiveFavorQuest(hero) != null)
			{
				return false;
			}
			Instance.GenerateFavorOffer(hero);
			return true;
		}, null, 200);
		starter.AddDialogLine("homestead_delivery_offer_yes", "homestead_task_offer_npc", "homestead_delivery_offer_options", "{=homestead_delivery_offer}Actually, yes. Could you take this {PKG_ITEM} to {PKG_RECIPIENT} in {PKG_SETTLEMENT}? I would greatly appreciate it.", delegate
		{
			if (Instance._pendingOfferType != PendingOfferType.Delivery)
			{
				return false;
			}
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			return hero != null && Instance.GetActiveDeliveryQuest(hero) == null && Instance.GenerateDeliveryOffer(hero);
		}, null, 190);
		starter.AddDialogLine("homestead_raid_offer", "homestead_task_offer_npc", "homestead_raid_offer_options", "{=homestead_raid_offer}My lord — we have had reports of an angry mob forming nearby. They may be on their way here. We need every sword ready!", () => Instance._pendingOfferType == PendingOfferType.Raid && Instance.GetActiveRaidQuestForHomestead(Instance.CurrentHomestead) == null, null, 185);
		starter.AddPlayerLine("homestead_raid_accept", "homestead_raid_offer_options", "close_window", "{=homestead_raid_accept}I'll deal with them — everyone to arms!", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				Instance.StartRaidEvent(hero);
			}
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
			{
				Mission.Current?.EndMission();
			};
		});
		starter.AddDialogLine("homestead_apparel_offer", "homestead_task_offer_npc", "homestead_apparel_offer_options", "{=homestead_apparel_offer}There is one thing, my lord. I have grown weary of these rags — I should dearly love a proper {APPAREL_ITEM} to wear. Would you find me one?", delegate
		{
			if (Instance._pendingOfferType != PendingOfferType.Apparel)
			{
				return false;
			}
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			return hero != null && Instance.GetActiveApparelQuest(hero) == null && Instance.GenerateApparelOffer(hero);
		}, null, 184);
		starter.AddPlayerLine("homestead_apparel_accept", "homestead_apparel_offer_options", "close_window", "{=homestead_apparel_accept}You shall have it.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				Instance.AcceptApparelOffer(hero);
			}
		});
		starter.AddDialogLine("homestead_build_offer", "homestead_task_offer_npc", "homestead_build_offer_options", "{=homestead_build_offer}There is, my lord. The homestead could really use a {BUILD_BUILDING}. Might you build one for us?", delegate
		{
			if (Instance._pendingOfferType != PendingOfferType.Building)
			{
				return false;
			}
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			return hero != null && Instance.GetActiveBuildingQuest(hero) == null && Instance.GenerateBuildingOffer(hero);
		}, null, 183);
		starter.AddPlayerLine("homestead_build_accept", "homestead_build_offer_options", "close_window", "{=homestead_build_accept}Consider it built.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				Instance.AcceptBuildingOffer(hero);
			}
		});
		starter.AddPlayerLine("homestead_build_decline", "homestead_build_offer_options", "close_window", "{=homestead_build_decline}Not right now.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				ChangeRelationAction.ApplyPlayerRelation(hero, -1);
			}
		}, 90);
		starter.AddDialogLine("homestead_appr_offer", "homestead_task_offer_npc", "homestead_appr_offer_options", "{=homestead_appr_offer}There is, my lord. I know of a promising youth, keen to learn the ways of war but green as spring grass. Would you take them into your company and blood them in a few battles?", delegate
		{
			if (Instance._pendingOfferType != PendingOfferType.Apprentice)
			{
				return false;
			}
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			return hero != null && Instance.CanOfferApprentice(hero);
		}, null, 182);
		starter.AddPlayerLine("homestead_appr_accept", "homestead_appr_offer_options", "close_window", "{=homestead_appr_accept}Send them along — I'll make a warrior of them.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				Instance.AcceptApprenticeOffer(hero);
			}
		});
		starter.AddPlayerLine("homestead_appr_decline", "homestead_appr_offer_options", "close_window", "{=homestead_appr_decline}I can't take on a recruit right now.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				ChangeRelationAction.ApplyPlayerRelation(hero, -1);
			}
		}, 90);
		starter.AddDialogLine("homestead_task_offer_none", "homestead_task_offer_npc", "close_window", "{=homestead_delivery_no_target}Not at the moment — I have nothing that needs attending to right now.", () => true, null);
		starter.AddDialogLine("homestead_favor_ready", "homestead_task_status_npc", "homestead_favor_ready_options", "{=homestead_favor_ready}Ah — you've brought the {FAVOR_ITEM}! You have my thanks.", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero == null || !Instance.TryGetActiveFavor(hero, out string item, out int count))
			{
				return false;
			}
			if (!Instance.IsFavorReady(hero))
			{
				return false;
			}
			Instance.SetFavorTextVariablesPublic(item, count);
			return true;
		}, null, 200);
		starter.AddDialogLine("homestead_favor_pending", "homestead_task_status_npc", "homestead_favor_pending_options", "{=homestead_favor_pending}Have you brought the {FAVOR_COUNT} {FAVOR_ITEM} yet, my lord?", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero == null || !Instance.TryGetActiveFavor(hero, out string item, out int count))
			{
				return false;
			}
			if (Instance.IsFavorReady(hero))
			{
				return false;
			}
			HomesteadNotableFavorQuest activeFavorQuest = Instance.GetActiveFavorQuest(hero);
			Instance.SetFavorTextVariablesPublic(item, count, activeFavorQuest?.PlayerCarryCount() ?? 0);
			return true;
		}, null, 190);
		starter.AddDialogLine("homestead_delivery_status", "homestead_task_status_npc", "homestead_delivery_status_options", "{=homestead_delivery_status}I sent the {PKG_ITEM} along with you. Have you managed to deliver it to {PKG_RECIPIENT} in {PKG_SETTLEMENT} yet?", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero == null)
			{
				return false;
			}
			HomesteadPackageDeliveryQuest activeDeliveryQuest = Instance.GetActiveDeliveryQuest(hero);
			if (activeDeliveryQuest == null)
			{
				return false;
			}
			MBTextManager.SetTextVariable("PKG_ITEM", activeDeliveryQuest.Package?.Name ?? new TextObject(string.Empty));
			MBTextManager.SetTextVariable("PKG_RECIPIENT", activeDeliveryQuest.Recipient?.Name ?? new TextObject(string.Empty));
			MBTextManager.SetTextVariable("PKG_SETTLEMENT", activeDeliveryQuest.RecipientSettlement?.Name ?? new TextObject(string.Empty));
			return true;
		}, null, 180);
		starter.AddPlayerLine("homestead_delivery_status_leave", "homestead_delivery_status_options", "close_window", "{=homestead_delivery_status_leave}Still making my way there — I'll see it done.", () => true, null);
		starter.AddDialogLine("homestead_raid_status", "homestead_task_status_npc", "homestead_raid_status_options", "{=homestead_raid_status}The Angry Mob is still out there, my lord. {RAID_COUNT} of them, moving this way. We are watching and ready.", delegate
		{
			HomesteadRaidEventQuest homesteadRaidEventQuest = Instance?.GetActiveRaidQuestForHomestead(Instance.CurrentHomestead);
			if (homesteadRaidEventQuest == null)
			{
				return false;
			}
			MBTextManager.SetTextVariable("RAID_COUNT", homesteadRaidEventQuest.RaiderCount);
			return true;
		}, null, 180);
		starter.AddPlayerLine("homestead_raid_status_leave", "homestead_raid_status_options", "close_window", "{=homestead_raid_status_leave}Stay sharp. I'll intercept them on the map if I can.", () => true, null);
		starter.AddDialogLine("homestead_apparel_ready", "homestead_task_status_npc", "homestead_apparel_ready_options", "{=homestead_apparel_ready}Is that the {APPAREL_ITEM} you found for me? Wonderful!", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			HomesteadNotableApparelQuest homesteadNotableApparelQuest = ((hero != null) ? Instance.GetActiveApparelQuest(hero) : null);
			if (homesteadNotableApparelQuest == null || !homesteadNotableApparelQuest.PlayerHasItem())
			{
				return false;
			}
			MBTextManager.SetTextVariable("APPAREL_ITEM", homesteadNotableApparelQuest.Item?.Name ?? new TextObject(string.Empty));
			return true;
		}, null, 178);
		starter.AddPlayerLine("homestead_apparel_handover", "homestead_apparel_ready_options", "close_window", "{=homestead_apparel_handover}Here — it should fit you well.", () => true, delegate
		{
			string title;
			Hero notable = Instance?.GetConversationNotable(out title);
			Instance?.GetActiveApparelQuest(notable)?.CompleteHandIn();
		});
		starter.AddDialogLine("homestead_apparel_pending", "homestead_task_status_npc", "homestead_apparel_pending_options", "{=homestead_apparel_pending}Have you found that {APPAREL_ITEM} for me yet, my lord?", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			HomesteadNotableApparelQuest homesteadNotableApparelQuest = ((hero != null) ? Instance.GetActiveApparelQuest(hero) : null);
			if (homesteadNotableApparelQuest == null || homesteadNotableApparelQuest.PlayerHasItem())
			{
				return false;
			}
			MBTextManager.SetTextVariable("APPAREL_ITEM", homesteadNotableApparelQuest.Item?.Name ?? new TextObject(string.Empty));
			return true;
		}, null, 176);
		starter.AddPlayerLine("homestead_apparel_pending_leave", "homestead_apparel_pending_options", "close_window", "{=homestead_apparel_pending_leave}Not yet — I'll keep an eye out.", () => true, null);
		starter.AddDialogLine("homestead_build_ready", "homestead_task_status_npc", "homestead_build_ready_options", "{=homestead_build_ready}You built the {BUILD_BUILDING}! Splendid work, my lord.", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			HomesteadBuildingRequestQuest homesteadBuildingRequestQuest = ((hero != null) ? Instance.GetActiveBuildingQuest(hero) : null);
			if (homesteadBuildingRequestQuest == null || !homesteadBuildingRequestQuest.IsTargetBuilt())
			{
				return false;
			}
			HomesteadScenePlaceable homesteadScenePlaceable = HomesteadScenePlaceable.FindByPrefabName(homesteadBuildingRequestQuest.PrefabName);
			MBTextManager.SetTextVariable("BUILD_BUILDING", (homesteadScenePlaceable != null) ? new TextObject(homesteadScenePlaceable.DisplayName) : new TextObject(homesteadBuildingRequestQuest.PrefabName));
			return true;
		}, null, 176);
		starter.AddPlayerLine("homestead_build_handover", "homestead_build_ready_options", "close_window", "{=homestead_build_handover}It was the least I could do.", () => true, delegate
		{
			string title;
			Hero notable = Instance?.GetConversationNotable(out title);
			Instance?.GetActiveBuildingQuest(notable)?.CompleteIfBuilt();
		});
		starter.AddDialogLine("homestead_build_status", "homestead_task_status_npc", "homestead_build_status_options", "{=homestead_build_status}Have you had a chance to build that {BUILD_BUILDING} yet, my lord?", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			HomesteadBuildingRequestQuest homesteadBuildingRequestQuest = ((hero != null) ? Instance.GetActiveBuildingQuest(hero) : null);
			if (homesteadBuildingRequestQuest == null || homesteadBuildingRequestQuest.IsTargetBuilt())
			{
				return false;
			}
			HomesteadScenePlaceable homesteadScenePlaceable = HomesteadScenePlaceable.FindByPrefabName(homesteadBuildingRequestQuest.PrefabName);
			MBTextManager.SetTextVariable("BUILD_BUILDING", (homesteadScenePlaceable != null) ? new TextObject(homesteadScenePlaceable.DisplayName) : new TextObject(homesteadBuildingRequestQuest.PrefabName));
			return true;
		}, null, 174);
		starter.AddPlayerLine("homestead_build_status_leave", "homestead_build_status_options", "close_window", "{=homestead_build_status_leave}I'll get it built soon.", () => true, null);
		starter.AddDialogLine("homestead_appr_ready", "homestead_task_status_npc", "homestead_appr_ready_options", "{=homestead_appr_ready}I hear {APPR_NAME} has grown strong fighting at your side! They should be ready to settle here. You ought to speak with them directly — I'm sure they'll want to say their piece.", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			HomesteadApprenticeQuest homesteadApprenticeQuest = ((hero != null) ? Instance.GetActiveApprenticeQuest(hero) : null);
			if (homesteadApprenticeQuest == null || !homesteadApprenticeQuest.IsTrainingComplete)
			{
				return false;
			}
			MBTextManager.SetTextVariable("APPR_NAME", homesteadApprenticeQuest.Apprentice?.Name ?? new TextObject(string.Empty));
			return true;
		}, null, 172);
		starter.AddPlayerLine("homestead_appr_redirect", "homestead_appr_ready_options", "close_window", "{=homestead_appr_redirect}I'll find them.", () => true, null);
		starter.AddDialogLine("homestead_appr_status", "homestead_task_status_npc", "homestead_appr_status_options", "{=homestead_appr_status}How fares {APPR_NAME}? ({APPR_POINTS} of {APPR_REQ} skill points gained so far.)", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			HomesteadApprenticeQuest homesteadApprenticeQuest = ((hero != null) ? Instance.GetActiveApprenticeQuest(hero) : null);
			if (homesteadApprenticeQuest == null || homesteadApprenticeQuest.IsTrainingComplete)
			{
				return false;
			}
			MBTextManager.SetTextVariable("APPR_NAME", homesteadApprenticeQuest.Apprentice?.Name ?? new TextObject(string.Empty));
			MBTextManager.SetTextVariable("APPR_POINTS", homesteadApprenticeQuest.SkillPointsGained);
			MBTextManager.SetTextVariable("APPR_REQ", 200);
			return true;
		}, null, 170);
		starter.AddPlayerLine("homestead_appr_status_leave", "homestead_appr_status_options", "close_window", "{=homestead_appr_status_leave}Coming along. I'll keep at it.", () => true, null);
		starter.AddDialogLine("homestead_appr_prisoner", "homestead_task_status_npc", "homestead_appr_prisoner_options", "{=homestead_appr_prisoner}Word reached me that {APPR_NAME} was taken captive after the battle, my lord. It is a bitter thing — but I still have friends who owe me favours. If you wish it, I can arrange their release and send them back to you.", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			HomesteadApprenticeQuest homesteadApprenticeQuest = ((hero != null) ? Instance.GetActiveApprenticeQuest(hero) : null);
			if (homesteadApprenticeQuest == null || !homesteadApprenticeQuest.IsApprenticePrisoner)
			{
				return false;
			}
			MBTextManager.SetTextVariable("APPR_NAME", homesteadApprenticeQuest.Apprentice?.Name ?? new TextObject(string.Empty));
			return true;
		}, null, 173);
		starter.AddPlayerLine("homestead_appr_prisoner_rescue", "homestead_appr_prisoner_options", "homestead_appr_prisoner_confirm", "{=homestead_appr_prisoner_rescue}Please, do what you can. I want them back.", () => true, null, 110);
		starter.AddPlayerLine("homestead_appr_prisoner_leave", "homestead_appr_prisoner_options", "close_window", "{=homestead_appr_prisoner_leave}I will handle it myself.", () => true, null);
		starter.AddDialogLine("homestead_appr_prisoner_confirm", "homestead_appr_prisoner_confirm", "close_window", "{=homestead_appr_prisoner_confirm}Leave it with me, my lord. {APPR_NAME} will find their way back to you — I will see to it personally.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			((hero != null) ? Instance.GetActiveApprenticeQuest(hero) : null)?.RescueApprentice();
		});
		starter.AddDialogLine("homestead_appr_farewell", "start", "homestead_appr_farewell_options", "{=homestead_appr_farewell_speech}My lord... I've fought hard at your side, and I'd like to think I've grown into something worthy. I honed myself most in {APPR_TOP_SKILL} through it all. I am grateful for the chance you gave me — and I am ready to call this homestead home.", delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero == null)
			{
				return false;
			}
			HomesteadApprenticeQuest apprenticeQuestForHero = GetApprenticeQuestForHero(oneToOneConversationHero);
			if (apprenticeQuestForHero == null || !apprenticeQuestForHero.IsTrainingComplete)
			{
				return false;
			}
			MBTextManager.SetTextVariable("APPR_TOP_SKILL", apprenticeQuestForHero.TopSkillName);
			return true;
		}, null, 300);
		starter.AddPlayerLine("homestead_appr_farewell_accept", "homestead_appr_farewell_options", "homestead_appr_farewell_end", "{=homestead_appr_farewell_accept}You've earned your place. Serve this homestead well.", () => true, null, 110);
		starter.AddPlayerLine("homestead_appr_farewell_accept_alt", "homestead_appr_farewell_options", "homestead_appr_farewell_end", "{=homestead_appr_farewell_accept_alt}The homestead is lucky to have you. Make the most of it.", () => true, null);
		starter.AddDialogLine("homestead_appr_farewell_npc_end", "homestead_appr_farewell_end", "close_window", "{=homestead_appr_farewell_npc_end}I will, my lord. You have my thanks.", () => true, delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null)
			{
				GetApprenticeQuestForHero(oneToOneConversationHero)?.Graduate();
			}
		});
		starter.AddPlayerLine("homestead_favor_offer_accept", "homestead_favor_offer_options", "close_window", "{=homestead_favor_offer_accept}Consider it done.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				Instance.AcceptCurrentFavorOffer(hero);
			}
		});
		starter.AddPlayerLine("homestead_favor_offer_decline", "homestead_favor_offer_options", "close_window", "{=homestead_favor_decline}Not right now.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				ChangeRelationAction.ApplyPlayerRelation(hero, -1);
			}
		}, 90);
		starter.AddPlayerLine("homestead_favor_partial_deliver", "homestead_favor_pending_options", "homestead_favor_partial_ack", "{=homestead_favor_partial_deliver}I have {PLAYER_COUNT} {FAVOR_ITEM} with me — take them.", delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			return hero != null && Instance.HasFavorPartial(hero);
		}, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				Instance.PartialDeliverFavor(hero);
			}
		}, 110);
		starter.AddDialogLine("homestead_favor_partial_ack_line", "homestead_favor_partial_ack", "close_window", "{=homestead_favor_partial_ack}Every bit helps. Bring the rest when you can.", () => true, null, 200);
		starter.AddPlayerLine("homestead_favor_pending_leave", "homestead_favor_pending_options", "close_window", "{=homestead_favor_pending_leave}Not yet — I'll see to it.", () => true, null);
		starter.AddPlayerLine("homestead_favor_turnin", "homestead_favor_ready_options", "homestead_favor_thanks", "{=homestead_favor_turnin}Here you are.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				Instance.CompleteFavor(hero);
			}
		});
		starter.AddDialogLine("homestead_favor_thanks_line", "homestead_favor_thanks", "close_window", "{=homestead_favor_thanks}You are too good to us, my lord. We won't forget it.", () => true, null, 200);
		starter.AddPlayerLine("homestead_delivery_accept", "homestead_delivery_offer_options", "close_window", "{=homestead_delivery_accept}Leave it to me.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				Instance.AcceptDeliveryOffer(hero);
			}
		});
		starter.AddPlayerLine("homestead_delivery_decline", "homestead_delivery_offer_options", "close_window", "{=homestead_delivery_decline}I cannot make that trip right now.", () => true, delegate
		{
			string title;
			Hero hero = Instance?.GetConversationNotable(out title);
			if (hero != null)
			{
				ChangeRelationAction.ApplyPlayerRelation(hero, -1);
			}
		}, 90);
		array = new string[3] { "hero_main_options", "lord_pretalk", "companion_talk" };
		foreach (string hub in array)
		{
			starter.AddPlayerLine("homestead_delivery_handover_" + hub, hub, "homestead_delivery_received_npc", "{=homestead_delivery_handover}I have a delivery for you from {PKG_SENDER}.", delegate
			{
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero == null)
				{
					return false;
				}
				TraceLogger.Write("HomesteadBehavior", $"DeliveryHandover condition: talking to '{oneToOneConversationHero.Name}' (StringId={oneToOneConversationHero.StringId}) hub={hub}");
				HomesteadPackageDeliveryQuest homesteadPackageDeliveryQuest = Instance?.GetDeliveryQuestForRecipient(oneToOneConversationHero);
				if (homesteadPackageDeliveryQuest == null)
				{
					TraceLogger.Write("HomesteadBehavior", "  → no matching quest found");
					return false;
				}
				if (!homesteadPackageDeliveryQuest.PlayerHasPackage())
				{
					TraceLogger.Write("HomesteadBehavior", "  → quest found but player lacks package");
					return false;
				}
				if (homesteadPackageDeliveryQuest.QuestGiver != null)
				{
					MBTextManager.SetTextVariable("PKG_SENDER", homesteadPackageDeliveryQuest.QuestGiver.Name);
				}
				return true;
			}, null, 120);
			starter.AddPlayerLine("homestead_delivery_lost_" + hub, hub, "homestead_delivery_lost_npc", "{=homestead_delivery_lost}I seem to have misplaced your delivery from {PKG_SENDER}. I will sort it out.", delegate
			{
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero == null)
				{
					return false;
				}
				HomesteadPackageDeliveryQuest homesteadPackageDeliveryQuest = Instance?.GetDeliveryQuestForRecipient(oneToOneConversationHero);
				if (homesteadPackageDeliveryQuest == null || homesteadPackageDeliveryQuest.PlayerHasPackage())
				{
					return false;
				}
				if (homesteadPackageDeliveryQuest.QuestGiver != null)
				{
					MBTextManager.SetTextVariable("PKG_SENDER", homesteadPackageDeliveryQuest.QuestGiver.Name);
				}
				return true;
			}, null, 119);
		}
		starter.AddDialogLine("homestead_delivery_lost_response", "homestead_delivery_lost_npc", "hero_main_options", "{=homestead_delivery_lost_npc}Oh... that is most unfortunate. I do hope you can recover it — {PKG_SENDER} will be relying on it.", delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			HomesteadPackageDeliveryQuest homesteadPackageDeliveryQuest = Instance?.GetDeliveryQuestForRecipient(oneToOneConversationHero);
			if (homesteadPackageDeliveryQuest?.QuestGiver != null)
			{
				MBTextManager.SetTextVariable("PKG_SENDER", homesteadPackageDeliveryQuest.QuestGiver.Name);
			}
			return true;
		}, null, 200);
		starter.AddDialogLine("homestead_delivery_received", "homestead_delivery_received_npc", "homestead_delivery_handover_options", "{=homestead_delivery_received}Ah, wonderful! I have been expecting this. You have my thanks — and please pass along my regards to {PKG_SENDER}.", delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			HomesteadPackageDeliveryQuest homesteadPackageDeliveryQuest = Instance?.GetDeliveryQuestForRecipient(oneToOneConversationHero);
			if (homesteadPackageDeliveryQuest?.QuestGiver != null)
			{
				MBTextManager.SetTextVariable("PKG_SENDER", homesteadPackageDeliveryQuest.QuestGiver.Name);
			}
			return true;
		}, null, 200);
		starter.AddPlayerLine("homestead_delivery_handover_done", "homestead_delivery_handover_options", "close_window", "{=homestead_delivery_done}I will pass that on. Safe travels.", () => true, delegate
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null)
			{
				Instance?.CompleteDelivery(oneToOneConversationHero);
			}
		});
		array = new string[1] { "hero_main_options" };
		foreach (string text3 in array)
		{
			string text4 = "homestead_dog_reaction_npc_" + text3;
			starter.AddPlayerLine("homestead_dog_reaction_ask_" + text3, text3, text4, "{=homestead_dog_ask}What do you think of my dog?", () => DogReactionCondition(), null, 50);
			starter.AddDialogLine("homestead_dog_reaction_female_" + text3, text4, text3, "{=homestead_dog_reaction_f}Ah, {DOG_NAME} is cute!", delegate
			{
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero != null && oneToOneConversationHero.IsFemale && Instance != null)
				{
					MBTextManager.SetTextVariable("DOG_NAME", Instance.AdoptedDogName);
					return true;
				}
				return false;
			}, null);
			starter.AddDialogLine("homestead_dog_reaction_male_" + text3, text4, text3, "{=homestead_dog_reaction_m}Wow, {DOG_NAME} looks like a loyal partner!", delegate
			{
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero != null && !oneToOneConversationHero.IsFemale && Instance != null)
				{
					MBTextManager.SetTextVariable("DOG_NAME", Instance.AdoptedDogName);
					return true;
				}
				return false;
			}, null);
		}
		starter.AddDialogLine("homestead_npc_flavour", "start", "close_window", "{HOMESTEAD_NPC_LINE}", delegate
		{
			HomesteadNpcRole? activeConversationNpcRole = HomesteadConversationMissionLogic.ActiveConversationNpcRole;
			if (!activeConversationNpcRole.HasValue)
			{
				return false;
			}
			if (IsHomesteadPrisonerConversation())
			{
				return false;
			}
			object obj = activeConversationNpcRole switch
			{
				HomesteadNpcRole.Guard => _npcLinesGuard, 
				HomesteadNpcRole.Soldier => _npcLinesSoldier, 
				HomesteadNpcRole.Worker => _npcLinesWorker, 
				HomesteadNpcRole.Musician => _npcLinesMusician, 
				_ => _npcLinesVillager, 
			};
			TextObject textObject4 = new TextObject((string)((object[])obj)[MBRandom.RandomInt(((Array)obj).Length)]);
			textObject4.SetTextVariable("HONORIFIC", GetPlayerHonorific());
			MBTextManager.SetTextVariable("HOMESTEAD_NPC_LINE", textObject4.ToString());
			return true;
		}, null, 200);
		starter.AddPlayerLine("homestead_leader_arms_master_ask", "hero_main_options", "homestead_leader_arms_master_offer_opts", "{=homestead_leader_arms_master_ask}Our guards need a true master-at-arms. How might we attract one?", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			return IsHomesteadLeaderConvo() && homestead != null && homestead.HasTrainingFieldBuilding && homestead.ArmsMasterHero == null && Instance.GetActiveArmsMasterRecruitQuest(homestead) == null;
		}, null, 150);
		starter.AddDialogLine("homestead_leader_arms_master_offer", "homestead_leader_arms_master_offer_opts", "homestead_leader_arms_master_offer_choice", "{=homestead_leader_arms_master_offer}Prove yourself, my lord — win renown in a tournament, and a seasoned Arms Master may be persuaded to settle here and school the guards.", () => true, null);
		starter.AddPlayerLine("homestead_leader_arms_master_accept", "homestead_leader_arms_master_offer_choice", "homestead_leader_arms_master_accepted", "{=homestead_leader_arms_master_accept}I'll prove myself in the arena and bring back a master-at-arms.", () => true, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			Hero hero = homestead?.Leader;
			if (homestead == null || hero == null)
			{
				return;
			}
			try
			{
				new HomesteadArmsMasterRecruitQuest($"homestead_arms_master_{homestead.MobileParty?.StringId ?? homestead.Name?.ToString()}_{CampaignTime.Now.ToMilliseconds}", hero, homestead).StartQuest();
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadBehavior", "Arms Master quest start failed: " + ex.Message);
			}
		}, 110);
		starter.AddPlayerLine("homestead_leader_arms_master_decline", "homestead_leader_arms_master_offer_choice", "close_window", "{=homestead_leader_arms_master_decline}Another time.", () => true, null);
		starter.AddDialogLine("homestead_leader_arms_master_accepted_line", "homestead_leader_arms_master_accepted", "close_window", "{=homestead_leader_arms_master_accepted}Splendid. Bring honour to our house in the lists, and word will spread.", () => true, null);
		starter.AddPlayerLine("homestead_leader_headman_trust_ask", "hero_main_options", "homestead_leader_headman_trust_offer_opts", "{=homestead_leader_headman_ask}Why has our expansion stalled? We've the means to grow.", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			return IsHomesteadLeaderConvo() && homestead != null && homestead.Tier == 1 && homestead.Tier1GrowthReady && !homestead.Tier1ApprovalGranted && Instance.GetActiveHeadmanTrustQuest(homestead) == null && Instance.GetActiveAngryVillagersQuest(homestead) == null;
		}, null, 150);
		starter.AddDialogLine("homestead_leader_headman_trust_offer", "homestead_leader_headman_trust_offer_opts", "homestead_leader_headman_trust_offer_choice", "{=homestead_leader_headman_offer}We've grown into something more permanent now, my lord — and we encroach on the lands of {VILLAGE}. Its headman, {HEADMAN}, must give his blessing before we raise another stone. Win his trust, then ask him plainly.", delegate
		{
			Settlement settlement = (Instance?.CurrentHomestead)?.FindNearestVillage();
			Hero hero = Homestead.FindHeadmanOfVillage(settlement);
			MBTextManager.SetTextVariable("VILLAGE", settlement?.Name ?? new TextObject("{=homestead_the_nearby_village}the nearby village"));
			MBTextManager.SetTextVariable("HEADMAN", hero?.Name ?? new TextObject("{=homestead_the_headman}the headman"));
			return true;
		}, null);
		starter.AddPlayerLine("homestead_leader_headman_trust_accept", "homestead_leader_headman_trust_offer_choice", "homestead_leader_headman_trust_accepted", "{=homestead_leader_headman_accept}I'll earn the headman's approval.", () => true, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead != null)
			{
				Instance.EnsureHeadmanTrustQuest(homestead);
			}
		}, 110);
		starter.AddPlayerLine("homestead_leader_headman_trust_decline", "homestead_leader_headman_trust_offer_choice", "close_window", "{=homestead_leader_headman_decline}That can wait.", () => true, null);
		starter.AddDialogLine("homestead_leader_headman_trust_accepted_line", "homestead_leader_headman_trust_accepted", "close_window", "{=homestead_leader_headman_accepted}Wise, my lord. Do his people some good, send our Ambassador to speak for us, or simply share his table — he'll come around.", () => true, null);
		starter.AddPlayerLine("homestead_leader_landpatent_self_ask", "hero_main_options", "homestead_leader_landpatent_self_resp", "{=homestead_leader_landpatent_self_ask}We're ready to expand again — and we rule the nearest town ourselves.", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (!IsHomesteadLeaderConvo() || homestead == null || homestead.Tier != 2 || !homestead.Tier2GrowthReady || homestead.Tier2ApprovalGranted)
			{
				return false;
			}
			Settlement settlement = homestead.FindNearestTown();
			return settlement?.OwnerClan != null && settlement.OwnerClan == Clan.PlayerClan;
		}, null, 150);
		starter.AddDialogLine("homestead_leader_landpatent_self_grant", "homestead_leader_landpatent_self_resp", "close_window", "{=homestead_leader_landpatent_self_grant}Indeed, my lord — the land is ours to grant. I'll draw up the patent at once.", () => true, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead != null)
			{
				HomesteadLandPatentQuest activeLandPatentQuest = Instance.GetActiveLandPatentQuest(homestead);
				if (activeLandPatentQuest != null)
				{
					activeLandPatentQuest.GrantApproval();
				}
				else
				{
					homestead.OnLandPatentGranted();
				}
			}
		});
		starter.AddPlayerLine("homestead_leader_landpatent_ask", "hero_main_options", "homestead_leader_landpatent_offer_opts", "{=homestead_leader_landpatent_ask}Why can't we expand the homestead any further?", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (!IsHomesteadLeaderConvo() || homestead == null || homestead.Tier != 2 || !homestead.Tier2GrowthReady || homestead.Tier2ApprovalGranted)
			{
				return false;
			}
			if (Instance.GetActiveLandPatentQuest(homestead) != null)
			{
				return false;
			}
			Settlement settlement = homestead.FindNearestTown();
			return settlement?.OwnerClan != null && settlement.OwnerClan != Clan.PlayerClan;
		}, null, 150);
		starter.AddDialogLine("homestead_leader_landpatent_offer", "homestead_leader_landpatent_offer_opts", "homestead_leader_landpatent_offer_choice", "{=homestead_leader_landpatent_offer}A holding this large needs a land patent, my lord — and these lands answer to {CLAN}, who hold {TOWN}. Win their house's favour, then ask any of their nobles to grant it.", delegate
		{
			Settlement settlement = (Instance?.CurrentHomestead)?.FindNearestTown();
			MBTextManager.SetTextVariable("CLAN", settlement?.OwnerClan?.Name ?? new TextObject("{=homestead_the_local_clan}the local ruling house"));
			MBTextManager.SetTextVariable("TOWN", settlement?.Name ?? new TextObject("{=homestead_the_nearest_town}the nearest town"));
			return true;
		}, null);
		starter.AddPlayerLine("homestead_leader_landpatent_accept", "homestead_leader_landpatent_offer_choice", "homestead_leader_landpatent_accepted", "{=homestead_leader_landpatent_accept}I'll secure their land patent.", () => true, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead != null)
			{
				Instance.EnsureLandPatentQuest(homestead);
			}
		}, 110);
		starter.AddPlayerLine("homestead_leader_landpatent_decline", "homestead_leader_landpatent_offer_choice", "close_window", "{=homestead_leader_landpatent_decline}That can wait.", () => true, null);
		starter.AddDialogLine("homestead_leader_landpatent_accepted_line", "homestead_leader_landpatent_accepted", "close_window", "{=homestead_leader_landpatent_accepted}Very good, my lord. Their favour — or our Ambassador's persuasion — will see it done.", () => true, null);
		starter.AddPlayerLine("homestead_leader_charter_self_ask", "hero_main_options", "homestead_leader_charter_self_resp", "{=homestead_leader_charter_self_ask}The homestead is ready to become a settlement — and these lands answer to us.", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (!IsHomesteadLeaderConvo() || homestead == null || !homestead.SettlementUpgradeReady || homestead.SettlementCharterGranted)
			{
				return false;
			}
			return homestead.FilledKeyNotableRoleCount >= 5 && homestead.CanPlayerSelfGrantCharter();
		}, null, 150);
		starter.AddPlayerLine("homestead_leader_charter_notables_ask", "hero_main_options", "homestead_leader_charter_notables_resp", "{=homestead_leader_charter_notables_ask}Is the homestead ready to become a true settlement?", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			return IsHomesteadLeaderConvo() && homestead != null && homestead.SettlementUpgradeReady && !homestead.SettlementCharterGranted && homestead.FilledKeyNotableRoleCount < 5;
		}, null, 150);
		starter.AddDialogLine("homestead_leader_charter_notables_resp", "homestead_leader_charter_notables_resp", "close_window", "{=homestead_leader_charter_notables_resp}Not yet, my lord. A town needs more hands of standing than these walls hold — we have {FILLED} of the {NEEDED} we'd need: a Master Smith, a Market Lady, an Ambassador, an Arms Master, a Tavern Keeper, a Hound Master, a Troubadour, a Stable Master. Recruit a few more before we raise a charter.", delegate
		{
			GameTexts.SetVariable("FILLED", (Instance?.CurrentHomestead)?.FilledKeyNotableRoleCount ?? 0);
			GameTexts.SetVariable("NEEDED", 5);
			return true;
		}, null);
		starter.AddDialogLine("homestead_leader_charter_self_grant", "homestead_leader_charter_self_resp", "close_window", "{=homestead_leader_charter_self_grant}Then the charter is yours to grant, my lord. I'll see it sealed at once.", () => true, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead != null)
			{
				HomesteadSettlementCharterQuest activeSettlementCharterQuest = Instance.GetActiveSettlementCharterQuest(homestead);
				if (activeSettlementCharterQuest != null)
				{
					activeSettlementCharterQuest.GrantApproval();
				}
				else
				{
					homestead.OnSettlementCharterGranted();
				}
			}
		});
		starter.AddPlayerLine("homestead_leader_charter_ask", "hero_main_options", "homestead_leader_charter_offer_opts", "{=homestead_leader_charter_ask}Can we make our homestead into a true settlement?", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (!IsHomesteadLeaderConvo() || homestead == null || !homestead.SettlementUpgradeReady || homestead.SettlementCharterGranted)
			{
				return false;
			}
			if (homestead.FilledKeyNotableRoleCount < 5)
			{
				return false;
			}
			if (Instance.GetActiveSettlementCharterQuest(homestead) != null)
			{
				return false;
			}
			if (homestead.CanPlayerSelfGrantCharter())
			{
				return false;
			}
			Kingdom kingdom = homestead.FindNearestKingdomSettlement()?.OwnerClan?.Kingdom;
			return kingdom != null && kingdom.RulingClan != Clan.PlayerClan;
		}, null, 150);
		starter.AddDialogLine("homestead_leader_charter_offer", "homestead_leader_charter_offer_opts", "homestead_leader_charter_offer_choice", "{=homestead_leader_charter_offer}It is ready, my lord — but raising a town and castle needs a royal charter. These lands lie within {KINGDOM}; win the leave of {RULER}, then ask them for it.", delegate
		{
			Kingdom kingdom = (Instance?.CurrentHomestead)?.FindNearestKingdomSettlement()?.OwnerClan?.Kingdom;
			MBTextManager.SetTextVariable("KINGDOM", kingdom?.Name ?? new TextObject("{=homestead_the_local_realm}the local realm"));
			MBTextManager.SetTextVariable("RULER", kingdom?.Leader?.Name ?? new TextObject("{=homestead_its_ruler}its ruler"));
			return true;
		}, null);
		starter.AddPlayerLine("homestead_leader_charter_accept", "homestead_leader_charter_offer_choice", "homestead_leader_charter_accepted", "{=homestead_leader_charter_accept}I'll secure a charter from the crown.", () => true, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead != null)
			{
				Instance.EnsureSettlementCharterQuest(homestead);
			}
		}, 110);
		starter.AddPlayerLine("homestead_leader_charter_decline", "homestead_leader_charter_offer_choice", "close_window", "{=homestead_leader_charter_decline}Not yet.", () => true, null);
		starter.AddDialogLine("homestead_leader_charter_accepted_line", "homestead_leader_charter_accepted", "close_window", "{=homestead_leader_charter_accepted}Very good, my lord. Their favour — or our Ambassador's word at court — will carry it.", () => true, null);
		starter.AddPlayerLine("homestead_leader_begin_placement", "hero_main_options", "homestead_leader_begin_placement_resp", "{=homestead_leader_begin_placement}The charter is in hand. Let us choose the site for our new settlement.", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			return IsHomesteadLeaderConvo() && homestead != null && homestead.SettlementCharterGranted && homestead.MobileParty != null && homestead.MobileParty.IsActive;
		}, null, 155);
		starter.AddDialogLine("homestead_leader_begin_placement_resp", "homestead_leader_begin_placement_resp", "close_window", "{=homestead_leader_begin_placement_resp}Aye, my lord. Mark where you will raise the town and castle — I'll have everything ready for the day you commit the site.", () => true, delegate
		{
			pendingPlacementMode = true;
		});
		starter.AddPlayerLine("homestead_headman_trust_turnin_ask", "hero_main_options", "homestead_headman_trust_turnin_resp", "{=homestead_headman_turnin_ask}My homestead nearby has grown. Will you give your blessing for it to expand?", () => Instance?.GetHeadmanTrustQuestForHeadman(Hero.OneToOneConversationHero) != null, null, 160);
		starter.AddDialogLine("homestead_headman_trust_turnin_grant", "homestead_headman_trust_turnin_resp", "close_window", "{=homestead_headman_turnin_grant}You've proven a good neighbour to my people. Aye — let your homestead grow. You have my blessing.", () => (Instance?.GetHeadmanTrustQuestForHeadman(Hero.OneToOneConversationHero))?.IsApprovalReady ?? false, delegate
		{
			(Instance?.GetHeadmanTrustQuestForHeadman(Hero.OneToOneConversationHero))?.GrantApproval();
			if (Instance != null)
			{
				Instance._pendingPlayerEncounterFinish = true;
			}
		}, 110);
		starter.AddDialogLine("homestead_headman_trust_turnin_deny", "homestead_headman_trust_turnin_resp", "close_window", "{=homestead_headman_turnin_deny}Your steading sprawls onto our lands, and I scarcely know you. Earn my trust first — do right by my people, and we'll speak of it again.", () => true, delegate
		{
			if (Instance != null)
			{
				Instance._pendingPlayerEncounterFinish = true;
			}
		});
		starter.AddPlayerLine("homestead_landpatent_turnin_ask", "hero_main_options", "homestead_landpatent_turnin_resp", "{=homestead_landpatent_turnin_ask}My homestead nearby has grown. Will your house grant it a land patent to expand?", () => Instance?.GetLandPatentQuestForClanMember(Hero.OneToOneConversationHero) != null, null, 160);
		starter.AddDialogLine("homestead_landpatent_turnin_grant", "homestead_landpatent_turnin_resp", "close_window", "{=homestead_landpatent_turnin_grant}You have proven a friend to our house. Very well — your homestead has our leave to grow. The patent is yours.", () => (Instance?.GetLandPatentQuestForClanMember(Hero.OneToOneConversationHero))?.IsApprovalReady ?? false, delegate
		{
			(Instance?.GetLandPatentQuestForClanMember(Hero.OneToOneConversationHero))?.GrantApproval();
			if (Instance != null)
			{
				Instance._pendingPlayerEncounterFinish = true;
			}
		}, 110);
		starter.AddDialogLine("homestead_landpatent_turnin_deny", "homestead_landpatent_turnin_resp", "close_window", "{=homestead_landpatent_turnin_deny}Your steading sprawls across lands my house holds, and we scarcely know you. Earn our favour first, and we'll consider it.", () => true, delegate
		{
			if (Instance != null)
			{
				Instance._pendingPlayerEncounterFinish = true;
			}
		});
		starter.AddPlayerLine("homestead_charter_turnin_ask", "hero_main_options", "homestead_charter_turnin_resp", "{=homestead_charter_turnin_ask}My homestead has grown into a true holding. Will you grant it a charter to become a settlement?", () => Instance?.GetSettlementCharterQuestForRuler(Hero.OneToOneConversationHero) != null, null, 160);
		starter.AddDialogLine("homestead_charter_turnin_grant", "homestead_charter_turnin_resp", "close_window", "{=homestead_charter_turnin_grant}You have served the realm well. Granted — raise your settlement, and may it strengthen our borders.", () => (Instance?.GetSettlementCharterQuestForRuler(Hero.OneToOneConversationHero))?.IsApprovalReady ?? false, delegate
		{
			(Instance?.GetSettlementCharterQuestForRuler(Hero.OneToOneConversationHero))?.GrantApproval();
			if (Instance != null)
			{
				Instance._pendingPlayerEncounterFinish = true;
			}
		}, 110);
		starter.AddDialogLine("homestead_charter_turnin_deny", "homestead_charter_turnin_resp", "close_window", "{=homestead_charter_turnin_deny}You ask much, and have not yet earned such trust from me. Prove your worth to the realm first, and we shall speak again.", () => true, delegate
		{
			if (Instance != null)
			{
				Instance._pendingPlayerEncounterFinish = true;
			}
		});
		starter.AddPlayerLine("homestead_leader_smith_ask", "hero_main_options", "homestead_leader_smith_offer_opts", "{=homestead_leader_smith_ask}Our smithy stands idle. How might we find a master to work it?", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			return IsHomesteadLeaderConvo() && homestead != null && homestead.HasSmithy && homestead.MasterSmithHero == null && !homestead.MasterSmithRecruited && Instance.GetActiveMasterSmithRecruitQuest(homestead) == null;
		}, null, 150);
		starter.AddDialogLine("homestead_leader_smith_offer", "homestead_leader_smith_offer_opts", "homestead_leader_smith_offer_choice", "{=homestead_leader_smith_offer}Smiths of true skill are proud folk, my lord. Seek one out in any town and win his respect — he'll not leave his forge for less.", () => true, null);
		starter.AddPlayerLine("homestead_leader_smith_accept", "homestead_leader_smith_offer_choice", "homestead_leader_smith_accepted", "{=homestead_leader_smith_accept}I'll find a worthy smith and bring him to our forge.", () => true, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			Hero hero = homestead?.Leader;
			if (homestead == null || hero == null)
			{
				return;
			}
			try
			{
				new HomesteadMasterSmithRecruitQuest($"homestead_master_smith_{homestead.MobileParty?.StringId ?? homestead.Name?.ToString()}_{CampaignTime.Now.ToMilliseconds}", hero, homestead).StartQuest();
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadBehavior", "Master Smith quest start failed: " + ex.Message);
			}
		}, 110);
		starter.AddPlayerLine("homestead_leader_smith_decline", "homestead_leader_smith_offer_choice", "close_window", "{=homestead_leader_smith_decline}Another time.", () => true, null);
		starter.AddDialogLine("homestead_leader_smith_accepted_line", "homestead_leader_smith_accepted", "close_window", "{=homestead_leader_smith_accepted}A fine notion. Earn a smith's respect, and our forge will ring at last.", () => true, null);
		starter.AddPlayerLine("homestead_leader_stable_ask", "hero_main_options", "homestead_leader_stable_offer_opts", "{=homestead_leader_stable_ask}Our stable stands empty. How might we draw a stable master here?", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			return IsHomesteadLeaderConvo() && homestead != null && homestead.HasStable && homestead.StableMasterHero == null && !homestead.StableMasterRecruited && Instance.GetActiveStableMasterRecruitQuest(homestead) == null;
		}, null, 150);
		starter.AddDialogLine("homestead_leader_stable_offer", "homestead_leader_stable_offer_opts", "homestead_leader_stable_offer_choice", "{=homestead_leader_stable_offer}Fine animals draw fine handlers, my lord. Gather a founding herd — two each of pack animals, riding horses and war horses, and a noble mount — bring them home, and a skilled stable master is sure to follow.", () => true, null);
		starter.AddPlayerLine("homestead_leader_stable_accept", "homestead_leader_stable_offer_choice", "homestead_leader_stable_accepted", "{=homestead_leader_stable_accept}I'll gather a founding herd and bring it home.", () => true, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			Hero hero = homestead?.Leader;
			if (homestead == null || hero == null)
			{
				return;
			}
			try
			{
				new HomesteadStableMasterRecruitQuest($"homestead_stable_master_{homestead.MobileParty?.StringId ?? homestead.Name?.ToString()}_{CampaignTime.Now.ToMilliseconds}", hero, homestead).StartQuest();
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadBehavior", "Stable Master quest start failed: " + ex.Message);
			}
		}, 110);
		starter.AddPlayerLine("homestead_leader_stable_decline", "homestead_leader_stable_offer_choice", "close_window", "{=homestead_leader_stable_decline}Another time.", () => true, null);
		starter.AddDialogLine("homestead_leader_stable_accepted_line", "homestead_leader_stable_accepted", "close_window", "{=homestead_leader_stable_accepted}Very good, my lord. Bring the herd, then tell me when it's gathered.", () => true, null);
		starter.AddPlayerLine("homestead_leader_stable_status", "hero_main_options", "homestead_leader_stable_status_reply", "{=homestead_leader_stable_status}How fares the search for a stable master?", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			return IsHomesteadLeaderConvo() && homestead != null && Instance.GetActiveStableMasterRecruitQuest(homestead) != null;
		}, null, 150);
		starter.AddDialogLine("homestead_leader_stable_status_ready", "homestead_leader_stable_status_reply", "homestead_leader_stable_deliver_choice", "{=homestead_leader_stable_status_ready}The herd is gathered, my lord! Shall I have them stabled and send word that a master is wanted?", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			return (Instance?.GetActiveStableMasterRecruitQuest(homestead))?.HasRequiredHorses ?? false;
		}, null, 110);
		starter.AddDialogLine("homestead_leader_stable_status_need", "homestead_leader_stable_status_reply", "close_window", "{=homestead_leader_stable_status_need}Not yet, my lord — we still want for more animals. ({STABLE_PROGRESS})", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			MBTextManager.SetTextVariable("STABLE_PROGRESS", (Instance?.GetActiveStableMasterRecruitQuest(homestead))?.HerdProgress ?? "");
			return true;
		}, null);
		starter.AddPlayerLine("homestead_leader_stable_deliver_yes", "homestead_leader_stable_deliver_choice", "homestead_leader_stable_delivered", "{=homestead_leader_stable_deliver_yes}Yes — have them stabled.", () => true, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			Instance?.GetActiveStableMasterRecruitQuest(homestead)?.CompleteByDelivery();
		}, 110);
		starter.AddPlayerLine("homestead_leader_stable_deliver_no", "homestead_leader_stable_deliver_choice", "close_window", "{=homestead_leader_stable_deliver_no}Not just yet.", () => true, null);
		starter.AddDialogLine("homestead_leader_stable_delivered_line", "homestead_leader_stable_delivered", "close_window", "{=homestead_leader_stable_delivered}At once, my lord. Word will go out that our stable wants a master.", () => true, null);
		starter.AddPlayerLine("homestead_smith_pitch", "blacksmith_player", "homestead_smith_pitch_reply", "{=homestead_smith_pitch}I'm building a smithy at my homestead. Would you come and run my forge?", () => IsTownBlacksmithConvo() && Instance?.GetMasterSmithQuestAwaitingPitch() != null, null, 200);
		starter.AddDialogLine("homestead_smith_pitch_reply_line", "homestead_smith_pitch_reply", "blacksmith_player", "{=homestead_smith_pitch_reply}Leave a town forge for some backwater? Prove you honour the craft first — fill ten smithing orders in the towns of Calradia. Do that, then come back to me here, and we'll talk.", () => true, delegate
		{
			Instance?.GetMasterSmithQuestAwaitingPitch()?.MarkPitchHeard(Settlement.CurrentSettlement);
		});
		starter.AddPlayerLine("homestead_smith_recruit", "blacksmith_player", "homestead_smith_recruit_reply", "{=homestead_smith_recruit}I've filled the ten orders you asked of me. Will you come to my homestead now?", () => IsTownBlacksmithConvo() && Instance?.GetMasterSmithQuestReadyToRecruit(Settlement.CurrentSettlement) != null, null, 200);
		starter.AddDialogLine("homestead_smith_recruit_accept_line", "homestead_smith_recruit_reply", "close_window", "{=homestead_smith_recruit_accept}You've the dedication of a true smith, my lord. I'll pack my tools and make for your homestead.", () => true, delegate
		{
			HomesteadMasterSmithRecruitQuest homesteadMasterSmithRecruitQuest = Instance?.GetMasterSmithQuestReadyToRecruit(Settlement.CurrentSettlement);
			if (homesteadMasterSmithRecruitQuest != null)
			{
				BodyProperties? templateBody = null;
				int templateAge = -1;
				try
				{
					CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
					if (oneToOneConversationCharacter != null)
					{
						templateBody = oneToOneConversationCharacter.GetBodyPropertiesMin();
						templateAge = (int)oneToOneConversationCharacter.Age;
					}
				}
				catch
				{
				}
				homesteadMasterSmithRecruitQuest.CompleteRecruitment(templateBody, templateAge);
			}
		});
		starter.AddDialogLine("homestead_arms_master_spar_result_win", "start", "homestead_arms_master_options", "{=homestead_arms_master_spar_result_win_line}Well fought, my lord! A crushing victory for your side. Ready to go again, or shall we call it a day?", delegate
		{
			if (!IsArmsMasterHomesteadConvo() || Instance == null || !Instance._hasPendingSparringResult)
			{
				return false;
			}
			if (!Instance._lastSparringResultPlayerWon)
			{
				return false;
			}
			Instance._hasPendingSparringResult = false;
			return true;
		}, null, 400);
		starter.AddDialogLine("homestead_arms_master_spar_result_loss", "start", "homestead_arms_master_options", "{=homestead_arms_master_spar_result_loss_line}A tough bout, my lord, but the homestead men took the day. Shall we run it again?", delegate
		{
			if (!IsArmsMasterHomesteadConvo() || Instance == null || !Instance._hasPendingSparringResult)
			{
				return false;
			}
			if (Instance._lastSparringResultPlayerWon)
			{
				return false;
			}
			Instance._hasPendingSparringResult = false;
			return true;
		}, null, 400);
		starter.AddDialogLine("homestead_arms_master_spar_result_win_settlement", "hero_main_options", "homestead_arms_master_options", "{=homestead_arms_master_spar_result_win_line}Well fought, my lord! A crushing victory for your side. Ready to go again, or shall we call it a day?", delegate
		{
			if (!IsArmsMasterConvo() || Instance == null || Instance.CurrentHomestead != null || !Instance._hasPendingSparringResult)
			{
				return false;
			}
			if (!Instance._lastSparringResultPlayerWon)
			{
				return false;
			}
			Instance._hasPendingSparringResult = false;
			return true;
		}, null, 400);
		starter.AddDialogLine("homestead_arms_master_spar_result_loss_settlement", "hero_main_options", "homestead_arms_master_options", "{=homestead_arms_master_spar_result_loss_line}A tough bout, my lord, but the homestead men took the day. Shall we run it again?", delegate
		{
			if (!IsArmsMasterConvo() || Instance == null || Instance.CurrentHomestead != null || !Instance._hasPendingSparringResult)
			{
				return false;
			}
			if (Instance._lastSparringResultPlayerWon)
			{
				return false;
			}
			Instance._hasPendingSparringResult = false;
			return true;
		}, null, 400);
		starter.AddDialogLine("homestead_arms_master_greet", "start", "homestead_arms_master_options", "{=homestead_arms_master_greet}My lord! What are your orders for the men?", () => IsArmsMasterHomesteadConvo(), null, 150);
		starter.AddPlayerLine("homestead_tournament_master_recruit", "arena_master_talk", "homestead_tournament_master_recruit_opts", "{=homestead_tournament_master_recruit}You fight as well as any I've faced. Come serve as Arms Master at my homestead.", delegate
		{
			CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
			return oneToOneConversationCharacter != null && oneToOneConversationCharacter.Occupation == Occupation.ArenaMaster && Instance?.GetWonArmsMasterQuestForSettlement(Settlement.CurrentSettlement) != null;
		}, null, 200);
		starter.AddDialogLine("homestead_tournament_master_recruit_accept_line", "homestead_tournament_master_recruit_opts", "close_window", "{=homestead_tournament_master_recruit_accept}You honour me. I have taught these fields long enough — I'll bring my craft to your homestead.", () => true, delegate
		{
			HomesteadArmsMasterRecruitQuest homesteadArmsMasterRecruitQuest = Instance?.GetWonArmsMasterQuestForSettlement(Settlement.CurrentSettlement);
			if (homesteadArmsMasterRecruitQuest != null)
			{
				BodyProperties? templateBody = null;
				int templateAge = -1;
				try
				{
					CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
					if (oneToOneConversationCharacter != null)
					{
						templateBody = oneToOneConversationCharacter.GetBodyPropertiesMin();
						templateAge = (int)oneToOneConversationCharacter.Age;
					}
				}
				catch
				{
				}
				homesteadArmsMasterRecruitQuest.CompleteOnTournamentWin(templateBody, templateAge);
			}
		});
		starter.AddPlayerLine("homestead_arena_horse_race", "arena_master_talk", "homestead_arena_horse_race_ack", "{=homestead_arena_horse_race}Care to organise a horse race?", delegate
		{
			CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
			return oneToOneConversationCharacter != null && oneToOneConversationCharacter.Occupation == Occupation.ArenaMaster && Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown;
		}, null, 150);
		starter.AddDialogLine("homestead_arena_horse_race_ack_line", "homestead_arena_horse_race_ack", "close_window", "{=homestead_arena_horse_race_ack}A race? The crowd does love a good gallop. Ride out and we'll line up the field.", () => true, delegate
		{
			Instance?.QueueArenaRace();
		});
		starter.AddDialogLine("homestead_arms_master_greeting", "start", "homestead_arms_master_options", "{=homestead_arms_master_greeting}The men are ready to train whenever you are, my lord. Care to test your steel?", () => IsArmsMasterHomesteadConvo(), null, 150);
		starter.AddPlayerLine("homestead_arms_master_spar_ask_settlement", "hero_main_options", "homestead_arms_master_greet_settlement_state", "{=homestead_arms_master_spar_ask_settlement}Care to test your steel? (Sparring)", () => IsArmsMasterConvo() && Instance != null && Instance.CurrentHomestead == null, null, 150);
		starter.AddDialogLine("homestead_arms_master_greet_settlement", "homestead_arms_master_greet_settlement_state", "homestead_arms_master_options", "{=homestead_arms_master_greeting}The men are ready to train whenever you are, my lord. Care to test your steel?", () => true, null);
		starter.AddPlayerLine("homestead_arms_master_spar_me", "homestead_arms_master_options", "homestead_arms_master_spar_ack_1v1", "{=homestead_arms_master_spar_me}Let me spar against your best man. (1v1)", () => CanStartPracticeFight(), delegate
		{
			Instance?.QueueSparringMatch(1, withTeamSelection: false);
		});
		starter.AddPlayerLine("homestead_arms_master_run_match", "homestead_arms_master_options", "homestead_arms_master_spar_ack_5v5", "{=homestead_arms_master_run_match}Let us hold a group match — five against five.", () => CanStartPracticeFight(), delegate
		{
			Instance?.QueueSparringMatch(5, withTeamSelection: true);
		});
		starter.AddPlayerLine("homestead_arms_master_sparring_records", "homestead_arms_master_options", "homestead_arms_master_sparring_records_line", "{=homestead_arms_master_sparring_records}How have our men fared in the sparring matches?", () => IsArmsMasterConvo(), delegate
		{
			Instance?.ShowClanSparringRecords();
		});
		starter.AddDialogLine("homestead_arms_master_sparring_records_response", "homestead_arms_master_sparring_records_line", "homestead_arms_master_options", "{=homestead_arms_master_sparring_records_response}Of course, my lord. The records are as follows.", () => IsArmsMasterConvo(), null);
		starter.AddPlayerLine("homestead_arms_master_leave", "homestead_arms_master_options", "close_window", "{=homestead_arms_master_leave}Not just now.", () => true, null);
		starter.AddDialogLine("homestead_arms_master_spar_ack_line_1v1", "homestead_arms_master_spar_ack_1v1", "close_window", "{=homestead_arms_master_spar_ack}To the field, then!", () => true, EndMissionAfterConversation);
		starter.AddDialogLine("homestead_arms_master_spar_ack_line_5v5", "homestead_arms_master_spar_ack_5v5", "close_window", "{=homestead_arms_master_spar_ack}To the field, then!", () => true, EndMissionAfterConversation);
		starter.AddDialogLine("homestead_arms_master_records_done", "homestead_arms_master_records_line", "homestead_arms_master_options", "{=homestead_arms_master_records_done}A proud tally, my lord — long may it grow.", () => true, null);
		starter.AddDialogLine("homestead_arms_master_mastery_offer", "start", "homestead_arms_master_mastery_options", "{=homestead_arms_master_mastery_offer}My lord, with the warriors we have tested and the drills I run every dawn, your men are ready to become something your enemies will truly dread. Give me the authority to push them past their limits.", delegate
		{
			if (!IsArmsMasterConvo() || Instance == null || Instance.HasArmsMasterMasteryUnlocked)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			int value;
			return Instance._notableApprenticeCount.TryGetValue(stringId, out value) && value >= 1;
		}, null, 300);
		starter.AddPlayerLine("homestead_arms_master_mastery_accept", "homestead_arms_master_mastery_options", "homestead_arms_master_mastery_accepted", "{=homestead_arms_master_mastery_accept}Push them until they break — then push harder. You have my leave.", null, delegate
		{
			if (Instance != null)
			{
				Instance.HasArmsMasterMasteryUnlocked = true;
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero != null)
				{
					string text5 = oneToOneConversationHero.FirstName?.ToString() ?? oneToOneConversationHero.Name.ToString();
					if (!oneToOneConversationHero.Name.ToString().Contains("Drill Sergeant"))
					{
						oneToOneConversationHero.SetName(new TextObject("{=homestead_drill_sergeant}Drill Sergeant {BASE_NAME}").SetTextVariable("BASE_NAME", text5), new TextObject(text5));
					}
				}
			}
		});
		starter.AddPlayerLine("homestead_arms_master_mastery_decline", "homestead_arms_master_mastery_options", "close_window", "{=homestead_arms_master_mastery_decline}Not yet. Carry on as before.", null, null);
		starter.AddDialogLine("homestead_arms_master_mastery_done", "homestead_arms_master_mastery_accepted", "close_window", "{=homestead_arms_master_mastery_done}Ha! Now that is an order I can respect. From this day forth — Drill Sergeant it is, my lord.", null, null);
		starter.AddDialogLine("homestead_tavern_keeper_network_offer", "start", "homestead_tavern_keeper_network_options", "{=homestead_tavern_keeper_network_offer}There is something I have been meaning to raise, {HONORIFIC}. The traders and travellers who pass through here — I know their faces, their routes, their whispers. With a little coin to grease the right palms, I can have every rumour worth hearing brought to your door. Your name would grow with every caravan that leaves.", delegate
		{
			if (!IsTavernKeeperHomesteadConvo() || Instance == null || Instance.HasTavernKeeperBonusActive)
			{
				return false;
			}
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null && oneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			if (!Instance._notableApprenticeCount.TryGetValue(stringId, out var value) || value < 1)
			{
				return false;
			}
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null, 300);
		starter.AddPlayerLine("homestead_tavern_keeper_network_accept", "homestead_tavern_keeper_network_options", "homestead_tavern_keeper_network_accepted", "{=homestead_tavern_keeper_network_accept}Build your network. Every voice that reaches me is worth paying for.", null, delegate
		{
			if (Instance != null)
			{
				Instance.HasTavernKeeperBonusActive = true;
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero != null)
				{
					string text5 = oneToOneConversationHero.FirstName?.ToString() ?? oneToOneConversationHero.Name.ToString();
					if (!oneToOneConversationHero.Name.ToString().Contains("World Famous"))
					{
						oneToOneConversationHero.SetName(new TextObject("{=homestead_world_famous}{BASE_NAME}, World Famous Innkeeper of {HOMESTEAD_NAME}").SetTextVariable("BASE_NAME", text5).SetTextVariable("HOMESTEAD_NAME", Instance.CurrentHomestead?.Name ?? oneToOneConversationHero.CurrentSettlement?.Name ?? new TextObject(text5)), new TextObject(text5));
					}
				}
			}
		});
		starter.AddPlayerLine("homestead_tavern_keeper_network_decline", "homestead_tavern_keeper_network_options", "close_window", "{=homestead_tavern_keeper_network_decline}Not yet. Let things settle first.", null, null);
		starter.AddDialogLine("homestead_tavern_keeper_network_done", "homestead_tavern_keeper_network_accepted", "close_window", "{=homestead_tavern_keeper_network_done}Very good, {HONORIFIC}. I will begin reaching out tonight. Give it time — word travels faster than armies.", delegate
		{
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null);
		starter.AddDialogLine("homestead_tavernkeeper_greeting", "start", "homestead_tavernkeeper_options", "{=homestead_tavernkeeper_greeting}Welcome, {HONORIFIC}. The hearth is warm and the cups are full. What can I do for you?", delegate
		{
			if (!IsTavernKeeperHomesteadConvo())
			{
				return false;
			}
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null, 200);
		starter.AddDialogLine("homestead_mastersmith_upgrade_ready", "start", "homestead_mastersmith_options", "{=homestead_mastersmith_upgrade_ready}Good timing, {HONORIFIC} — your piece is finished. Tempered and true; you'll feel the difference. Here you are.", delegate
		{
			if (!IsMasterSmithHomesteadConvo() || Instance?.CurrentHomestead?.SmithUpgradeReady != true)
			{
				return false;
			}
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, delegate
		{
			Instance?.CollectSmithUpgrade(Instance.CurrentHomestead);
		}, 360);
		starter.AddDialogLine("homestead_mastersmith_mastery_offer", "start", "homestead_mastersmith_mastery_options", "{=homestead_mastersmith_mastery_offer}{HONORIFIC}, my hands have learned this trade as well as any living. Give me leave and I'll temper and refine the very gear you carry — make good steel better.", delegate
		{
			if (!IsMasterSmithHomesteadConvo() || Instance == null || Instance.HasMasterSmithUpgradeUnlocked)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			if (!Instance._notableApprenticeCount.TryGetValue(stringId, out var value) || value < 1)
			{
				return false;
			}
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null, 300);
		starter.AddPlayerLine("homestead_mastersmith_mastery_accept", "homestead_mastersmith_mastery_options", "homestead_mastersmith_mastery_accepted", "{=homestead_mastersmith_mastery_accept}Show me what your hammer can do.", null, delegate
		{
			if (Instance != null)
			{
				Instance.HasMasterSmithUpgradeUnlocked = true;
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (oneToOneConversationHero != null)
				{
					string text5 = oneToOneConversationHero.FirstName?.ToString() ?? oneToOneConversationHero.Name.ToString();
					oneToOneConversationHero.SetName(new TextObject("{=homestead_grandmaster_smith}Grandmaster Smith {BASE_NAME}").SetTextVariable("BASE_NAME", text5), new TextObject(text5));
				}
			}
		});
		starter.AddPlayerLine("homestead_mastersmith_mastery_decline", "homestead_mastersmith_mastery_options", "close_window", "{=homestead_mastersmith_mastery_decline}Maybe later.", null, null);
		starter.AddDialogLine("homestead_mastersmith_mastery_done", "homestead_mastersmith_mastery_accepted", "close_window", "{=homestead_mastersmith_mastery_done}Then bring me a piece whenever you like, and I'll see it improved.", null, null);
		starter.AddDialogLine("homestead_mastersmith_greeting", "start", "homestead_mastersmith_options", "{=homestead_mastersmith_greeting}The forge is hot, {HONORIFIC}. Need steel worked, or something off my anvil?", delegate
		{
			if (!IsMasterSmithHomesteadConvo())
			{
				return false;
			}
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null, 200);
		starter.AddPlayerLine("homestead_mastersmith_upgrade_ask", "homestead_mastersmith_options", "homestead_mastersmith_upgrade_ack", "{=homestead_mastersmith_upgrade_ask}I have a piece I'd like you to improve.", delegate
		{
			bool flag = (Instance?.CurrentHomestead)?.SmithUpgradePending ?? Instance?.SettlementSmithUpgradePending(Hero.OneToOneConversationHero) ?? false;
			return IsMasterSmithConvo() && Instance != null && Instance.HasMasterSmithUpgradeUnlocked && !flag;
		}, null, 130);
		starter.AddDialogLine("homestead_mastersmith_upgrade_ack", "homestead_mastersmith_upgrade_ack", "close_window", "{=homestead_mastersmith_upgrade_ack}Let's see what you're carrying. Pick the piece, and I'll have it ready by tomorrow.", () => true, delegate
		{
			try
			{
				Instance?.OpenSmithUpgradePicker();
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadBehavior", "OpenSmithUpgradePicker failed: " + ex.Message);
			}
		}, 130);
		starter.AddPlayerLine("homestead_mastersmith_upgrade_status_ask", "homestead_mastersmith_options", "homestead_mastersmith_upgrade_status", "{=homestead_mastersmith_upgrade_status_ask}How's my piece coming along?", delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			Hero smith = ((homestead == null) ? Hero.OneToOneConversationHero : null);
			bool flag = homestead?.SmithUpgradePending ?? Instance?.SettlementSmithUpgradePending(smith) ?? false;
			bool flag2 = homestead?.SmithUpgradeReady ?? Instance?.SettlementSmithUpgradeReady(smith) ?? false;
			return IsMasterSmithConvo() && flag && !flag2;
		}, null, 130);
		starter.AddDialogLine("homestead_mastersmith_upgrade_status", "homestead_mastersmith_upgrade_status", "homestead_mastersmith_options", "{=homestead_mastersmith_upgrade_status}Still at the anvil, {HONORIFIC}. Good work takes time — come back tomorrow and it'll be ready.", delegate
		{
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null, 130);
		starter.AddPlayerLine("homestead_mastersmith_use_forge", "homestead_mastersmith_options", "homestead_mastersmith_forge_ack", "{=homestead_mastersmith_use_forge}May I use your forge?", () => IsMasterSmithConvo(), null, 120);
		starter.AddDialogLine("homestead_mastersmith_forge_ack", "homestead_mastersmith_forge_ack", "close_window", "{=homestead_mastersmith_forge_ack}Aye — it's yours. Mind the heat.", () => true, delegate
		{
			Instance?.ScheduleHomesteadForge();
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
			{
				try
				{
					Mission.Current?.EndMission();
				}
				catch
				{
				}
			};
		}, 120);
		starter.AddPlayerLine("homestead_mastersmith_leave", "homestead_mastersmith_options", "close_window", "{=homestead_mastersmith_leave}Nothing for now.", () => true, null, 1);
		starter.AddDialogLine("homestead_mastersmith_upgrade_ready_settlement", "hero_main_options", "homestead_mastersmith_upgrade_ready_settlement_ack", "{=homestead_mastersmith_upgrade_ready}Good timing, {HONORIFIC} — your piece is finished. Tempered and true; you'll feel the difference. Here you are.", delegate
		{
			if (!IsMasterSmithConvo() || Instance == null || Instance.CurrentHomestead != null)
			{
				return false;
			}
			if (!Instance.SettlementSmithUpgradeReady(Hero.OneToOneConversationHero))
			{
				return false;
			}
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, delegate
		{
			Instance?.CollectSmithUpgrade(null, Hero.OneToOneConversationHero);
		}, 360);
		starter.AddPlayerLine("homestead_mastersmith_upgrade_ready_settlement_leave", "homestead_mastersmith_upgrade_ready_settlement_ack", "close_window", "{=homestead_mastersmith_leave}Nothing for now.", () => true, null);
		starter.AddPlayerLine("homestead_mastersmith_mastery_ask_settlement", "hero_main_options", "homestead_mastersmith_mastery_offer_settlement_state", "{=homestead_mastersmith_mastery_ask}Have you learned anything new about your trade?", delegate
		{
			if (!IsMasterSmithConvo() || Instance == null || Instance.CurrentHomestead != null || Instance.HasMasterSmithUpgradeUnlocked)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero == null || Hero.OneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			int value;
			return Instance._notableApprenticeCount.TryGetValue(stringId, out value) && value >= 1;
		}, null);
		starter.AddDialogLine("homestead_mastersmith_mastery_offer_settlement", "homestead_mastersmith_mastery_offer_settlement_state", "homestead_mastersmith_mastery_options", "{=homestead_mastersmith_mastery_offer}{HONORIFIC}, my hands have learned this trade as well as any living. Give me leave and I'll temper and refine the very gear you carry — make good steel better.", delegate
		{
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null);
		starter.AddPlayerLine("homestead_mastersmith_upgrade_ask_settlement", "hero_main_options", "homestead_mastersmith_upgrade_ack", "{=homestead_mastersmith_upgrade_ask}I have a piece I'd like you to improve.", () => IsMasterSmithConvo() && Instance != null && Instance.CurrentHomestead == null && Instance.HasMasterSmithUpgradeUnlocked && !Instance.SettlementSmithUpgradePending(Hero.OneToOneConversationHero), null);
		starter.AddPlayerLine("homestead_mastersmith_upgrade_status_ask_settlement", "hero_main_options", "homestead_mastersmith_upgrade_status_settlement", "{=homestead_mastersmith_upgrade_status_ask}How's my piece coming along?", delegate
		{
			if (!IsMasterSmithConvo() || Instance == null || Instance.CurrentHomestead != null)
			{
				return false;
			}
			return Instance.SettlementSmithUpgradePending(Hero.OneToOneConversationHero) && !Instance.SettlementSmithUpgradeReady(Hero.OneToOneConversationHero);
		}, null);
		starter.AddDialogLine("homestead_mastersmith_upgrade_status_settlement_line", "homestead_mastersmith_upgrade_status_settlement", "hero_main_options", "{=homestead_mastersmith_upgrade_status}Still at the anvil, {HONORIFIC}. Good work takes time — come back tomorrow and it'll be ready.", delegate
		{
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null);
		starter.AddPlayerLine("homestead_tavernkeeper_ask_news", "homestead_tavernkeeper_options", "homestead_tavernkeeper_news", "{=homestead_tavernkeeper_ask_news}What's the talk around here?", () => true, null);
		starter.AddDialogLine("homestead_tavernkeeper_news_report", "homestead_tavernkeeper_news", "homestead_tavernkeeper_options", "{=homestead_tavernkeeper_news_report}Travellers come and go, {HONORIFIC}, and every one of them leaves a little gossip behind. Keep this place busy and you'll always hear the realm's pulse first.", delegate
		{
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null, 200);
		starter.AddPlayerLine("homestead_tavernkeeper_goodbye", "homestead_tavernkeeper_options", "close_window", "{=homestead_tavernkeeper_goodbye}Keep the cups full.", () => true, null);
		starter.AddPlayerLine("homestead_tavern_keeper_network_ask_settlement", "hero_main_options", "homestead_tavern_keeper_network_offer_settlement_state", "{=homestead_tavern_keeper_network_ask}There's something I've been meaning to raise with you.", delegate
		{
			if (!IsTavernKeeperConvo() || Instance == null || Instance.CurrentHomestead != null || Instance.HasTavernKeeperBonusActive)
			{
				return false;
			}
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null && oneToOneConversationHero.GetRelationWithPlayer() < 90f)
			{
				return false;
			}
			string stringId = Hero.OneToOneConversationHero.StringId;
			int value;
			return Instance._notableApprenticeCount.TryGetValue(stringId, out value) && value >= 1;
		}, null);
		starter.AddDialogLine("homestead_tavern_keeper_network_offer_settlement", "homestead_tavern_keeper_network_offer_settlement_state", "homestead_tavern_keeper_network_options", "{=homestead_tavern_keeper_network_offer}There is something I have been meaning to raise, {HONORIFIC}. The traders and travellers who pass through here — I know their faces, their routes, their whispers. With a little coin to grease the right palms, I can have every rumour worth hearing brought to your door. Your name would grow with every caravan that leaves.", delegate
		{
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null);
		starter.AddDialogLine("homestead_tavern_greeter_greeting", "start", "homestead_tavern_greeter_options", "{=homestead_tavern_greeter_greeting}Welcome, {HONORIFIC}! The hearth's warm and the ale's poured. Shall I show you inside?", delegate
		{
			if (!IsTavernGreeterConvo())
			{
				return false;
			}
			GameTexts.SetVariable("HONORIFIC", GetPlayerHonorific());
			return true;
		}, null, 300);
		starter.AddPlayerLine("homestead_tavern_greeter_enter", "homestead_tavern_greeter_options", "close_window", "{=homestead_tavern_greeter_enter}Lead the way.", () => true, delegate
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead != null)
			{
				Instance.ScheduleTavernVisit(homestead);
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
				{
					Mission.Current?.EndMission();
				};
			}
		}, 110);
		starter.AddPlayerLine("homestead_tavern_greeter_change_culture", "homestead_tavern_greeter_options", "homestead_tavern_greeter_change_culture_ack", "{=homestead_tavern_greeter_change_culture}I'd like to choose which tavern you borrow.", () => IsTavernGreeterConvo(), null, 105);
		starter.AddDialogLine("homestead_tavern_greeter_change_culture_ack", "homestead_tavern_greeter_change_culture_ack", "close_window", "{=homestead_tavern_greeter_change_culture_ack}As you wish. Pick a style, and I'll lead you there next time.", () => true, delegate
		{
			try
			{
				Instance?.OpenTavernCulturePicker();
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadBehavior", "OpenTavernCulturePicker failed: " + ex.Message);
			}
		}, 105);
		starter.AddPlayerLine("homestead_tavern_greeter_leave", "homestead_tavern_greeter_options", "close_window", "{=homestead_tavern_greeter_leave}Not just now.", () => true, null);
		starter.AddDialogLine("homestead_prisoner_chat_start", "start", "homestead_prisoner_talk", "{=homestead_prisoner_greet}What do you want? I'm not going anywhere.", IsHomesteadPrisonerConversation, null, 400);
		starter.AddPlayerLine("homestead_prisoner_chat_leave", "homestead_prisoner_talk", "close_window", "{=homestead_prisoner_leave}Just making sure.", null, null);
		starter.AddDialogLine("homestead_ransom_broker_start", "start", "homestead_ransom_broker_talk", "{=homestead_ransom_broker_greet}Greetings, {?PLAYER.GENDER}madam{?}sir{\\?}. If you have any prisoners you'd like to be rid of, I can take them off your hands for a fair price.", delegate
		{
			if (IsHomesteadTavernInterior())
			{
				CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
				if (oneToOneConversationCharacter == null)
				{
					return false;
				}
				return oneToOneConversationCharacter.Occupation == Occupation.RansomBroker;
			}
			return false;
		}, null, 200);
		starter.AddPlayerLine("homestead_ransom_broker_sell", "homestead_ransom_broker_talk", "homestead_ransom_broker_sell_screen", "{=homestead_ransom_broker_sell}I have prisoners to sell.", delegate
		{
			MobileParty mainParty = MobileParty.MainParty;
			return mainParty != null && mainParty.Party?.NumberOfPrisoners > 0;
		}, null);
		starter.AddDialogLine("homestead_ransom_broker_sell_screen", "homestead_ransom_broker_sell_screen", "homestead_ransom_broker_pretalk", "{=homestead_ransom_broker_sell_screen}Let me see what you have...", null, delegate
		{
			Helpers.PartyScreenHelper.OpenScreenAsRansom();
		});
		starter.AddDialogLine("homestead_ransom_broker_pretalk", "homestead_ransom_broker_pretalk", "homestead_ransom_broker_talk", "{=homestead_ransom_broker_pretalk}Anything else?", null, null);
		starter.AddPlayerLine("homestead_ransom_broker_leave", "homestead_ransom_broker_talk", "close_window", "{=homestead_ransom_broker_leave}Not right now. Good day.", () => true, null);
		starter.AddDialogLine("homestead_mercenary_recruit_start", "start", "homestead_mercenary_tavern_talk", "{=homestead_mercenary_recruit_start}Do you have a need for fighters, {?PLAYER.GENDER}madam{?}sir{\\?}? Me and {?PLURAL}{MERCENARY_COUNT} of my mates{?}one of my mates{\\?} are looking for a master. You might call us mercenaries, like. We'll join you for {GOLD_AMOUNT}{GOLD_ICON}", delegate
		{
			if (!IsHomesteadTavernInterior())
			{
				return false;
			}
			CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
			if (oneToOneConversationCharacter == null || CurrentHomestead == null)
			{
				return false;
			}
			if (oneToOneConversationCharacter.StringId == CurrentHomestead.AvailableMercenaryTypeId && CurrentHomestead.AvailableMercenaryCount > 0)
			{
				MBTextManager.SetTextVariable("MERCENARY_COUNT", CurrentHomestead.AvailableMercenaryCount - 1);
				int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(oneToOneConversationCharacter, Hero.MainHero).RoundedResultNumber;
				MBTextManager.SetTextVariable("GOLD_AMOUNT", roundedResultNumber * CurrentHomestead.AvailableMercenaryCount);
				MBTextManager.SetTextVariable("PLURAL", (CurrentHomestead.AvailableMercenaryCount > 1) ? 1 : 0);
				return true;
			}
			return false;
		}, null, 200);
		starter.AddPlayerLine("homestead_mercenary_recruit_accept", "homestead_mercenary_tavern_talk", "homestead_mercenary_tavern_talk_hire", "{=homestead_mercenary_recruit_accept}All right. I will hire {?PLURAL}all of you{?}you{\\?}. Here is {GOLD_AMOUNT}{GOLD_ICON}", delegate
		{
			if (CurrentHomestead == null || CharacterObject.OneToOneConversationCharacter == null)
			{
				return false;
			}
			int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(CharacterObject.OneToOneConversationCharacter, Hero.MainHero).RoundedResultNumber;
			return Hero.MainHero.Gold >= roundedResultNumber * CurrentHomestead.AvailableMercenaryCount && MobileParty.MainParty.Party.PartySizeLimit >= MobileParty.MainParty.MemberRoster.TotalManCount + CurrentHomestead.AvailableMercenaryCount;
		}, delegate
		{
			if (CurrentHomestead != null && CharacterObject.OneToOneConversationCharacter != null)
			{
				int availableMercenaryCount = CurrentHomestead.AvailableMercenaryCount;
				int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(CharacterObject.OneToOneConversationCharacter, Hero.MainHero).RoundedResultNumber;
				MobileParty.MainParty.MemberRoster.AddToCounts(CharacterObject.OneToOneConversationCharacter, availableMercenaryCount);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -(availableMercenaryCount * roundedResultNumber));
				CurrentHomestead.AvailableMercenaryCount = 0;
				CurrentHomestead.AvailableMercenaryTypeId = "";
				if (Campaign.Current.ConversationManager.OneToOneConversationAgent is Agent pendingMercFadeAgent)
				{
					HomesteadTavernMissionLogic.PendingMercFadeAgent = pendingMercFadeAgent;
				}
			}
		}, 200);
		starter.AddPlayerLine("homestead_mercenary_recruit_reject_gold", "homestead_mercenary_tavern_talk", "close_window", "{=homestead_mercenary_recruit_reject_gold}That sounds good. But I can't afford any more men right now.", delegate
		{
			if (CurrentHomestead == null || CharacterObject.OneToOneConversationCharacter == null)
			{
				return false;
			}
			int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(CharacterObject.OneToOneConversationCharacter, Hero.MainHero).RoundedResultNumber;
			return Hero.MainHero.Gold < roundedResultNumber * CurrentHomestead.AvailableMercenaryCount;
		}, null, 200);
		starter.AddPlayerLine("homestead_mercenary_recruit_reject", "homestead_mercenary_tavern_talk", "close_window", "{=homestead_mercenary_recruit_reject}Sorry, I can't take on any more troops right now.", () => true, null, 200);
		starter.AddDialogLine("homestead_mercenary_recruit_end", "homestead_mercenary_tavern_talk_hire", "close_window", "{=homestead_mercenary_recruit_end}Right away, {?PLAYER.GENDER}madam{?}sir{\\?}. We'll grab our things and meet you outside.", () => true, null, 200);
		starter.AddDialogLine("homestead_musician_start", "start", "homestead_musician_response", "{=homestead_musician_lyric}*The musician hums a familiar tavern tune and glances your way with a nod.*", delegate
		{
			if (IsHomesteadTavernInterior())
			{
				CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
				if (oneToOneConversationCharacter == null)
				{
					return false;
				}
				return oneToOneConversationCharacter.Occupation == Occupation.Musician;
			}
			return false;
		}, null, 200);
		starter.AddPlayerLine("homestead_musician_leave", "homestead_musician_response", "close_window", "{=homestead_musician_leave}Play on, good man.", () => true, null);
		starter.AddDialogLine("homestead_barmaid_start", "start", "homestead_barmaid_talk", "{=homestead_barmaid_greet}What can I bring you, {?PLAYER.GENDER}madam{?}sir{\\?}?", delegate
		{
			if (IsHomesteadTavernInterior())
			{
				CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
				if (oneToOneConversationCharacter == null)
				{
					return false;
				}
				return oneToOneConversationCharacter.Occupation == Occupation.TavernWench;
			}
			return false;
		}, null, 200);
		starter.AddPlayerLine("homestead_barmaid_order", "homestead_barmaid_talk", "homestead_barmaid_ack", "{=homestead_barmaid_order}I'll have whatever you've got.", () => true, null);
		starter.AddDialogLine("homestead_barmaid_ack", "homestead_barmaid_ack", "close_window", "{=homestead_barmaid_ack}It'll be right up, {?PLAYER.GENDER}ma'am{?}sir{\\?}.", null, null);
		starter.AddPlayerLine("homestead_barmaid_leave", "homestead_barmaid_talk", "close_window", "{=homestead_barmaid_leave}I'm fine, thank you.", () => true, null);
		starter.AddDialogLine("homestead_tavern_generic_npc", "start", "homestead_tavern_generic_leave", "{=homestead_tavern_generic}*nods respectfully*", delegate
		{
			if (!IsHomesteadTavernInterior())
			{
				return false;
			}
			if (IsHomesteadPrisonerConversation())
			{
				return false;
			}
			CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
			if (oneToOneConversationCharacter == null || oneToOneConversationCharacter.IsHero)
			{
				return false;
			}
			return oneToOneConversationCharacter.Occupation != Occupation.RansomBroker && oneToOneConversationCharacter.Occupation != Occupation.Musician && oneToOneConversationCharacter.Occupation != Occupation.TavernWench;
		}, null, 200);
		starter.AddPlayerLine("homestead_tavern_generic_leave", "homestead_tavern_generic_leave", "close_window", "{=homestead_tavern_generic_leave}Carry on.", () => true, null);
		static bool CanStartPracticeFight()
		{
			if (!IsArmsMasterConvo())
			{
				return false;
			}
			HomesteadBehavior instance = Instance;
			if (instance != null && instance.CurrentHomestead?.HasTrainingFieldBuilding == true)
			{
				return true;
			}
			if (Hero.OneToOneConversationHero != null && Instance != null && Instance.ConvertedNotableRoles.TryGetValue(Hero.OneToOneConversationHero.StringId, out string value) && value == "ArmsMaster")
			{
				return true;
			}
			return false;
		}
		static bool DogReactionCondition()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			HomesteadBehavior instance = Instance;
			if (instance != null && instance.HasAdoptedDog && oneToOneConversationHero != null && oneToOneConversationHero != Hero.MainHero && oneToOneConversationHero.IsNotable)
			{
				return !oneToOneConversationHero.IsLord;
			}
			return false;
		}
		static void EndMissionAfterConversation()
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
			{
				try
				{
					Mission.Current?.EndMission();
				}
				catch
				{
				}
			};
		}
		static Hero? GetConversationHero()
		{
			try
			{
				Hero activeConversationHero = HomesteadConversationMissionLogic.ActiveConversationHero;
				if (activeConversationHero != null && activeConversationHero != Hero.MainHero)
				{
					return activeConversationHero;
				}
				if (GetSpawner() == null)
				{
					return null;
				}
				Hero hero = null;
				try
				{
					hero = Hero.OneToOneConversationHero;
				}
				catch
				{
				}
				return (hero != null && hero != Hero.MainHero) ? hero : null;
			}
			catch
			{
				return null;
			}
		}
		static HomesteadSpawningMissionLogic? GetSpawner()
		{
			return HomesteadSpawningMissionLogic.Current;
		}
		static bool IsAmbassadorConvo()
		{
			if (Instance?.CurrentHomestead?.AmbassadorHero != null && Hero.OneToOneConversationHero == Instance.CurrentHomestead.AmbassadorHero)
			{
				return true;
			}
			if (Hero.OneToOneConversationHero != null && Instance != null && Instance.ConvertedNotableRoles.TryGetValue(Hero.OneToOneConversationHero.StringId, out string value) && value == "Ambassador")
			{
				return true;
			}
			return false;
		}
		static bool IsAmbassadorHomesteadConvo()
		{
			if (Instance?.CurrentHomestead?.AmbassadorHero != null)
			{
				return Hero.OneToOneConversationHero == Instance.CurrentHomestead.AmbassadorHero;
			}
			return false;
		}
		static bool IsAnyConvertedNotableConvo()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null && Instance != null && Instance.CurrentHomestead == null)
			{
				return Instance.ConvertedNotableRoles.ContainsKey(oneToOneConversationHero.StringId);
			}
			return false;
		}
		static bool IsArmsMasterConvo()
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead?.ArmsMasterHero != null && Hero.OneToOneConversationHero == homestead.ArmsMasterHero)
			{
				return true;
			}
			if (Hero.OneToOneConversationHero != null && Instance != null && Instance.ConvertedNotableRoles.TryGetValue(Hero.OneToOneConversationHero.StringId, out string value) && value == "ArmsMaster")
			{
				return true;
			}
			return false;
		}
		static bool IsArmsMasterHomesteadConvo()
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead?.ArmsMasterHero != null)
			{
				return Hero.OneToOneConversationHero == homestead.ArmsMasterHero;
			}
			return false;
		}
		static bool IsHomesteadLeaderConvo()
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead?.Leader != null)
			{
				return Hero.OneToOneConversationHero == homestead.Leader;
			}
			return false;
		}
		static bool IsHomesteadPrisonerConversation()
		{
			if (HomesteadConversationMissionLogic.ActiveConversationNpcRole == HomesteadNpcRole.Prisoner)
			{
				return true;
			}
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead == null)
			{
				return false;
			}
			CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
			if (oneToOneConversationCharacter == null)
			{
				return false;
			}
			MobileParty mobileParty = homestead.MobileParty;
			if (mobileParty == null)
			{
				return false;
			}
			return mobileParty.PrisonRoster?.Contains(oneToOneConversationCharacter) == true;
		}
		static bool IsHomesteadTavernInterior()
		{
			return Mission.Current?.GetMissionBehavior<HomesteadTavernMissionLogic>() != null;
		}
		static bool IsHoundMasterConvo()
		{
			if (HomesteadConversationMissionLogic.IsHoundMasterConversation)
			{
				return true;
			}
			if (Instance?.CurrentHomestead?.HoundMasterHero != null && Hero.OneToOneConversationHero == Instance.CurrentHomestead.HoundMasterHero)
			{
				return true;
			}
			if (Hero.OneToOneConversationHero != null && Instance != null && Instance.ConvertedNotableRoles.TryGetValue(Hero.OneToOneConversationHero.StringId, out string value) && value == "HoundMaster")
			{
				return true;
			}
			return false;
		}
		static bool IsHoundMasterHomesteadConvo()
		{
			if (HomesteadConversationMissionLogic.IsHoundMasterConversation)
			{
				return true;
			}
			if (Instance?.CurrentHomestead?.HoundMasterHero != null && Hero.OneToOneConversationHero == Instance.CurrentHomestead.HoundMasterHero)
			{
				return true;
			}
			return false;
		}
		static bool IsMarketLadyConvo()
		{
			if (HomesteadConversationMissionLogic.IsMarketLadyConversation)
			{
				return true;
			}
			if (Instance?.CurrentHomestead?.MarketLadyHero != null && Hero.OneToOneConversationHero == Instance.CurrentHomestead.MarketLadyHero)
			{
				return true;
			}
			if (Hero.OneToOneConversationHero != null && Instance != null && Instance.ConvertedNotableRoles.TryGetValue(Hero.OneToOneConversationHero.StringId, out string value) && value == "MarketLady")
			{
				return true;
			}
			return false;
		}
		static bool IsMarketLadyHomesteadConvo()
		{
			if (HomesteadConversationMissionLogic.IsMarketLadyConversation)
			{
				return true;
			}
			if (Instance?.CurrentHomestead?.MarketLadyHero != null && Hero.OneToOneConversationHero == Instance.CurrentHomestead.MarketLadyHero)
			{
				return true;
			}
			return false;
		}
		static bool IsMasterSmithConvo()
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead?.MasterSmithHero != null && Hero.OneToOneConversationHero == homestead.MasterSmithHero)
			{
				return true;
			}
			if (Hero.OneToOneConversationHero != null && Instance != null && Instance.ConvertedNotableRoles.TryGetValue(Hero.OneToOneConversationHero.StringId, out string value) && value == "MasterSmith")
			{
				return true;
			}
			return false;
		}
		static bool IsMasterSmithHomesteadConvo()
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead?.MasterSmithHero != null)
			{
				return Hero.OneToOneConversationHero == homestead.MasterSmithHero;
			}
			return false;
		}
		static bool IsOwnedVillagerConversation()
		{
			try
			{
				if (PlayerEncounter.Current == null)
				{
					return false;
				}
				MobileParty mobileParty = PlayerEncounter.EncounteredParty?.MobileParty;
				if (mobileParty == null || !mobileParty.IsVillager)
				{
					return false;
				}
				if (Campaign.Current.CurrentConversationContext != ConversationContext.PartyEncounter)
				{
					return false;
				}
				Settlement settlement = (mobileParty.PartyComponent as VillagerPartyComponent)?.Village?.Settlement;
				return settlement != null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_settlement_") && settlement.OwnerClan == Clan.PlayerClan;
			}
			catch
			{
				return false;
			}
		}
		static bool IsPatrolConversation()
		{
			try
			{
				if (PlayerEncounter.Current == null)
				{
					return false;
				}
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty == null || !encounteredParty.IsMobile)
				{
					return false;
				}
				MobileParty mobileParty = encounteredParty.MobileParty;
				return mobileParty != null && Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && (Instance?.PatrolMobileParties.ContainsKey(mobileParty) ?? false);
			}
			catch
			{
				return false;
			}
		}
		static bool IsRecruiterConversation()
		{
			try
			{
				if (PlayerEncounter.Current == null)
				{
					return false;
				}
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty == null || !encounteredParty.IsMobile)
				{
					return false;
				}
				MobileParty mobileParty = encounteredParty.MobileParty;
				return mobileParty != null && Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && HomesteadRecruiterComponent.GetFor(mobileParty) != null;
			}
			catch
			{
				return false;
			}
		}
		static bool IsStableMasterConvo()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			Homestead homestead = Instance?.CurrentHomestead;
			if (oneToOneConversationHero != null && homestead != null && oneToOneConversationHero == homestead.StableMasterHero)
			{
				return true;
			}
			if (oneToOneConversationHero != null && Instance != null && Instance.ConvertedNotableRoles.TryGetValue(oneToOneConversationHero.StringId, out string value) && value == "StableMaster")
			{
				return true;
			}
			return false;
		}
		static bool IsStableMasterHomesteadConvo()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			Homestead homestead = Instance?.CurrentHomestead;
			if (oneToOneConversationHero != null && homestead != null)
			{
				return oneToOneConversationHero == homestead.StableMasterHero;
			}
			return false;
		}
		static bool IsTavernGreeterConvo()
		{
			if (HomesteadConversationMissionLogic.IsTavernGreeterConversation && Instance?.CurrentHomestead != null)
			{
				return Instance.CurrentHomestead.HasTavernBuilding;
			}
			return false;
		}
		static bool IsTavernKeeperConvo()
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead?.TavernKeeperHero != null && Hero.OneToOneConversationHero == homestead.TavernKeeperHero)
			{
				return true;
			}
			if (Hero.OneToOneConversationHero != null && Instance != null && Instance.ConvertedNotableRoles.TryGetValue(Hero.OneToOneConversationHero.StringId, out string value) && value == "TavernKeeper")
			{
				return true;
			}
			return false;
		}
		static bool IsTavernKeeperHomesteadConvo()
		{
			Homestead homestead = Instance?.CurrentHomestead;
			if (homestead?.TavernKeeperHero != null)
			{
				return Hero.OneToOneConversationHero == homestead.TavernKeeperHero;
			}
			return false;
		}
		static bool IsTownBlacksmithConvo()
		{
			CharacterObject oneToOneConversationCharacter = CharacterObject.OneToOneConversationCharacter;
			if (oneToOneConversationCharacter != null)
			{
				return oneToOneConversationCharacter.Occupation == Occupation.Blacksmith;
			}
			return false;
		}
	}

	private static string GetPlayerHonorific()
	{
		Hero mainHero = Hero.MainHero;
		return new TextObject((mainHero != null && mainHero.IsFemale) ? "{=hr_honorific_lady}my lady" : "{=hr_honorific_lord}my lord").ToString();
	}

	private static string GetPlayerHonorificCapitalized()
	{
		Hero mainHero = Hero.MainHero;
		return new TextObject((mainHero != null && mainHero.IsFemale) ? "{=hr_honorific_cap_lady}My lady" : "{=hr_honorific_cap_lord}My lord").ToString();
	}

	internal void TryReapplyHomesteadDefenderSetup()
	{
		if (PlayerEncounter.Current == null)
		{
			return;
		}
		MapEvent mapEvent = MobileParty.MainParty.MapEvent;
		if (mapEvent == null)
		{
			return;
		}
		MobileParty mobileParty = null;
		foreach (MapEventParty party2 in mapEvent.DefenderSide.Parties)
		{
			PartyBase party = party2.Party;
			if (party.MobileParty != null && party.MobileParty != MobileParty.MainParty && HomesteadMobileParties.ContainsKey(party.MobileParty))
			{
				mobileParty = party.MobileParty;
				break;
			}
		}
		if (mobileParty == null)
		{
			return;
		}
		PartyBase partyBase = null;
		using (List<MapEventParty>.Enumerator enumerator = mapEvent.AttackerSide.Parties.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				partyBase = enumerator.Current.Party;
			}
		}
		if (partyBase == null)
		{
			return;
		}
		try
		{
			PlayerEncounter.Current.SetupFields(partyBase, mobileParty.Party);
			PlayerEncounter.Current.IsPlayerWaiting = true;
			TraceLogger.Write("HomesteadBehavior", "TryReapplyHomesteadDefenderSetup: SetupFields(attacker='" + (partyBase.MobileParty?.StringId ?? "null") + "', defender='" + mobileParty.StringId + "') applied — join-mode active.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "TryReapplyHomesteadDefenderSetup: SetupFields threw: " + ex.Message);
		}
	}

	public void ScheduleTavernVisit(Homestead hs)
	{
		if (hs != null)
		{
			_pendingTavernHomestead = hs;
		}
	}

	public void QueueRace()
	{
		_pendingRaceHomestead = CurrentHomestead;
		_raceNeedsSelection = true;
		try
		{
			if (Campaign.Current?.ConversationManager != null)
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
				{
					Mission.Current?.EndMission();
				};
			}
		}
		catch
		{
		}
	}

	public void QueueArenaRace()
	{
		_pendingArenaRace = true;
		_arenaRaceSettlement = Settlement.CurrentSettlement;
		try
		{
			if (Campaign.Current?.ConversationManager != null)
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
				{
					Mission.Current?.EndMission();
				};
			}
		}
		catch
		{
		}
	}

	public void StartRaceFromConsole(RaceTrack? track)
	{
		if (track != null)
		{
			ShowRaceRivalSelector(null, track);
		}
		else
		{
			BeginRaceSetup(null);
		}
	}

	private void BeginRaceSetup(Homestead? hs, Settlement? nobleSource = null)
	{
		List<RaceTrack> all = RaceTracks.All;
		if (all.Count <= 1)
		{
			ShowRaceRivalSelector(hs, RaceTracks.Default, nobleSource);
			return;
		}
		List<InquiryElement> inquiryElements = all.Select((RaceTrack t) => new InquiryElement(t, t.Name, null)).ToList();
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(Utils.GetLocalizedString("{=homestead_race_track_pick_title}Choose a Track"), Utils.GetLocalizedString("{=homestead_race_track_pick_text}Which track shall we race?"), inquiryElements, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate(List<InquiryElement> selected)
		{
			if (selected != null && selected.Count > 0 && selected[0].Identifier is RaceTrack racePickTrack)
			{
				_racePickHs = hs;
				_racePickTrack = racePickTrack;
				_racePickSettlement = nobleSource;
				_raceAwaitingRivalPicker = true;
			}
			else
			{
				_pendingRaceHomestead = null;
			}
		}, delegate
		{
			_pendingRaceHomestead = null;
		}), pauseGameActiveState: true, prioritize: true);
	}

	private void ShowRaceRivalSelector(Homestead? hs, RaceTrack track, Settlement? nobleSource = null)
	{
		try
		{
			List<Hero> candidates = new List<Hero>();
			if (Clan.PlayerClan?.Heroes != null)
			{
				foreach (Hero hero in Clan.PlayerClan.Heroes)
				{
					Add(hero);
				}
			}
			if (hs != null)
			{
				Add(hs.Leader);
				Add(hs.HoundMasterHero);
				Add(hs.MarketLadyHero);
				Add(hs.AmbassadorHero);
				Add(hs.ArmsMasterHero);
				Add(hs.TavernKeeperHero);
				Add(hs.MasterSmithHero);
				Add(hs.StableMasterHero);
				if (hs.ResidentHeroes != null)
				{
					foreach (Hero residentHero in hs.ResidentHeroes)
					{
						Add(residentHero);
					}
				}
			}
			if (nobleSource != null)
			{
				if (nobleSource.Parties != null)
				{
					foreach (MobileParty party in nobleSource.Parties)
					{
						if (party?.LeaderHero != null && party.LeaderHero.IsLord)
						{
							Add(party.LeaderHero);
						}
					}
				}
				if (nobleSource.HeroesWithoutParty != null)
				{
					foreach (Hero item in nobleSource.HeroesWithoutParty)
					{
						if (item.IsLord)
						{
							Add(item);
						}
					}
				}
			}
			if (candidates.Count == 0)
			{
				_pendingRaceHomestead = null;
				CustomMissions.StartHorseRaceMission(hs, track, new List<Hero>());
				Utilities.DisableGlobalLoadingWindow();
				return;
			}
			int maxSelectableOptionCount = Math.Min(2, candidates.Count);
			List<InquiryElement> inquiryElements = candidates.Select((Hero h) => new InquiryElement(h, h.Name.ToString() + "  (" + new TextObject("{=homestead_race_riding}Riding {N}").SetTextVariable("N", h.GetSkillValue(DefaultSkills.Riding)).ToString() + ")", new CharacterImageIdentifier(CharacterCode.CreateFrom(h.CharacterObject)), isEnabled: true, null)).ToList();
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(Utils.GetLocalizedString("{=homestead_race_pick_title}Choose Racers"), Utils.GetLocalizedString("{=homestead_race_pick_text}Select up to two riders to race against."), inquiryElements, isExitShown: true, 1, maxSelectableOptionCount, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate(List<InquiryElement> selected)
			{
				_pendingRaceHomestead = null;
				List<Hero> rivals = (from h in selected?.Select((InquiryElement e) => e.Identifier as Hero)
					where h != null
					select h).Cast<Hero>().ToList() ?? new List<Hero>();
				CustomMissions.StartHorseRaceMission(hs, track, rivals);
				Utilities.DisableGlobalLoadingWindow();
			}, delegate
			{
				_pendingRaceHomestead = null;
			}), pauseGameActiveState: true, prioritize: true);
			void Add(Hero? h)
			{
				if (h != null && h != Hero.MainHero && h.IsAlive && !h.IsChild && !h.IsPrisoner && !h.IsDisabled && !candidates.Contains(h))
				{
					candidates.Add(h);
				}
			}
		}
		catch (Exception ex)
		{
			_pendingRaceHomestead = null;
			TraceLogger.Write("HomesteadBehavior", "ShowRaceRivalSelector failed: " + ex.Message);
		}
	}

	public void ScheduleHomesteadForge()
	{
		_pendingForge = true;
	}

	private void OpenHomesteadForge()
	{
		try
		{
			CraftingTemplate craftingTemplate = CraftingTemplate.All.FirstOrDefault();
			if (craftingTemplate == null)
			{
				TraceLogger.Write("HomesteadBehavior", "OpenHomesteadForge: no crafting templates found.");
				return;
			}
			if (CurrentHomestead != null)
			{
				HomesteadForgeContext.Begin(CurrentHomestead);
			}
			CultureObject culture = Hero.MainHero?.Culture ?? Settlement.CurrentSettlement?.Culture ?? Settlement.All.FirstOrDefault()?.Culture ?? new CultureObject();
			TextObject textObject = new TextObject("{=homestead_forge_crafted}Homestead Crafted {CURR_TEMPLATE_NAME}");
			textObject.SetTextVariable("CURR_TEMPLATE_NAME", craftingTemplate.TemplateName);
			Crafting crafting = new Crafting(craftingTemplate, culture, textObject);
			crafting.Init();
			crafting.ReIndex();
			CraftingState craftingState = Game.Current.GameStateManager.CreateState<CraftingState>();
			craftingState.InitializeLogic(crafting);
			Game.Current.GameStateManager.PushState(craftingState);
			TraceLogger.Write("HomesteadBehavior", "OpenHomesteadForge: smithy opened.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "OpenHomesteadForge failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	private void TriggerPostConversionSaveReload()
	{
		try
		{
			if (Campaign.Current?.SaveHandler == null)
			{
				return;
			}
			SaveHandler saveHandler = Campaign.Current.SaveHandler;
			string saveName = (CampaignOptions.IsIronmanMode ? saveHandler.IronmanModSaveName : MakeUniquePostSettlementSaveName());
			InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_postconv_saving}Saving and reloading to finalize the new settlement..."), Colors.Yellow));
			Action<bool, string> action = null;
			action = delegate(bool isSuccessful, string savedName)
			{
				CampaignEvents.OnSaveOverEvent.ClearListeners(this);
				if (!isSuccessful)
				{
					TraceLogger.Write("HomesteadBehavior", "Post-conversion auto-save failed.");
					InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_postconv_save_failed}Auto-save failed — please save and reload manually to finalize the new settlement."), Colors.Red));
				}
				else
				{
					SaveGameFileInfo saveFileWithName = MBSaveLoad.GetSaveFileWithName(savedName);
					if (saveFileWithName == null || saveFileWithName.IsCorrupted)
					{
						TraceLogger.Write("HomesteadBehavior", "Post-conversion save '" + savedName + "' missing or corrupted — aborting reload.");
					}
					else
					{
						SandBoxSaveHelper.TryLoadSave(saveFileWithName, (Action<LoadResult>)delegate(LoadResult loadResult)
						{
							//IL_002d: Unknown result type (might be due to invalid IL or missing references)
							//IL_0037: Expected O, but got Unknown
							if (Game.Current != null)
							{
								ScreenManager.PopScreen();
								GameStateManager.Current.CleanStates();
								GameStateManager.Current = TaleWorlds.MountAndBlade.Module.CurrentModule.GlobalGameStateManager;
							}
							MBSaveLoad.OnStartGame(loadResult);
							MBGameManager.StartNewGame((MBGameManager)new SandBoxGameManager(loadResult));
						}, (Action)delegate
						{
							TraceLogger.Write("HomesteadBehavior", "Post-conversion reload was cancelled.");
						});
					}
				}
			};
			CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, action);
			saveHandler.SaveAs(saveName);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "TriggerPostConversionSaveReload failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	private static string MakeUniquePostSettlementSaveName()
	{
		string text = MBSaveLoad.ActiveSaveSlotName;
		if (string.IsNullOrEmpty(text))
		{
			text = "homestead";
		}
		string text2 = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		string text3 = text + "_settlement_" + text2;
		int num = 1;
		while (MBSaveLoad.IsSaveGameFileExists(text3))
		{
			text3 = $"{text}_settlement_{text2}_{num++}";
		}
		return text3;
	}

	public void RequestReturnNearTavernEntrance()
	{
		ReturnNearTavernEntrance = true;
	}

	public bool ConsumeReturnNearTavernEntrance()
	{
		bool returnNearTavernEntrance = ReturnNearTavernEntrance;
		ReturnNearTavernEntrance = false;
		return returnNearTavernEntrance;
	}

	private void QueueSparringMatch(int teamSize, bool withTeamSelection)
	{
		Homestead homestead = Instance?.CurrentHomestead;
		if (homestead != null)
		{
			ScheduleSparringMatch(homestead, teamSize, withTeamSelection);
			return;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
		if (currentSettlement?.Town == null || oneToOneConversationHero == null)
		{
			Utils.PrintLocalizedMessage("homestead_sparring_town", "The Arms Master has no private training field here. Sparring is unavailable.", 255f, 200f, 80f);
		}
		else
		{
			ScheduleSparringMatchAtSettlement(currentSettlement, oneToOneConversationHero, teamSize, withTeamSelection);
		}
	}

	public void ScheduleReturnToHomestead(Homestead hs)
	{
		_pendingReturnHomestead = hs;
	}

	public void ScheduleSparringMatch(Homestead hs, int teamSize, bool withTeamSelection)
	{
		_pendingSparringHomestead = hs;
		_pendingSparringSettlement = null;
		_pendingSparringArmsMaster = null;
		_pendingSparringTeamSize = teamSize;
		_sparSelectPhase = (withTeamSelection ? 1 : 0);
		_sparSelectDelayTicks = 2;
		_sparSelectIdleTime = 0f;
		_sparPlayerRoster = null;
		_sparEnemyRoster = null;
	}

	public void ScheduleSparringMatchAtSettlement(Settlement settlement, Hero armsMaster, int teamSize, bool withTeamSelection)
	{
		_pendingSparringHomestead = null;
		_pendingSparringSettlement = settlement;
		_pendingSparringArmsMaster = armsMaster;
		_pendingSparringTeamSize = teamSize;
		_sparSelectPhase = (withTeamSelection ? 1 : 0);
		_sparSelectDelayTicks = 2;
		_sparSelectIdleTime = 0f;
		_sparPlayerRoster = null;
		_sparEnemyRoster = null;
	}

	private void LaunchSparringMission()
	{
		Homestead pendingSparringHomestead = _pendingSparringHomestead;
		Settlement pendingSparringSettlement = _pendingSparringSettlement;
		Hero pendingSparringArmsMaster = _pendingSparringArmsMaster;
		int pendingSparringTeamSize = _pendingSparringTeamSize;
		TroopRoster sparPlayerRoster = _sparPlayerRoster;
		TroopRoster sparEnemyRoster = _sparEnemyRoster;
		_pendingSparringHomestead = null;
		_pendingSparringSettlement = null;
		_pendingSparringArmsMaster = null;
		_sparSelectPhase = 0;
		_sparPlayerRoster = null;
		_sparEnemyRoster = null;
		if (pendingSparringHomestead == null && pendingSparringSettlement == null)
		{
			return;
		}
		try
		{
			SparringMissionActive = true;
			if (pendingSparringHomestead != null)
			{
				CustomMissions.StartHomesteadSparringMission(pendingSparringHomestead, pendingSparringTeamSize, sparPlayerRoster, sparEnemyRoster);
			}
			else
			{
				CustomMissions.StartHomesteadSparringMission(pendingSparringSettlement, pendingSparringArmsMaster, pendingSparringTeamSize, sparPlayerRoster, sparEnemyRoster);
			}
			Utilities.DisableGlobalLoadingWindow();
		}
		catch (Exception ex)
		{
			SparringMissionActive = false;
			TraceLogger.Write("HomesteadBehavior", "StartHomesteadSparringMission failed: " + ex.Message);
		}
	}

	private TroopRoster BuildSparringPool(TroopRoster? secondaryRoster, bool includePlayer, TroopRoster? exclude = null)
	{
		TroopRoster pool = TroopRoster.CreateDummyTroopRoster();
		HashSet<CharacterObject> seen = new HashSet<CharacterObject>();
		AddRoster(MobileParty.MainParty.MemberRoster);
		try
		{
			AddRoster(secondaryRoster);
		}
		catch
		{
		}
		if (exclude != null)
		{
			foreach (TroopRosterElement item in exclude.GetTroopRoster())
			{
				if (item.Character != null)
				{
					try
					{
						pool.AddToCounts(item.Character, -item.Number);
					}
					catch
					{
					}
				}
			}
		}
		return pool;
		void AddRoster(TroopRoster? src)
		{
			if (src == null)
			{
				return;
			}
			foreach (TroopRosterElement item2 in src.GetTroopRoster())
			{
				CharacterObject character = item2.Character;
				if (character != null && (includePlayer || character != CharacterObject.PlayerCharacter) && (!character.IsHero || seen.Add(character)))
				{
					int num = item2.Number - item2.WoundedNumber;
					if (num > 0)
					{
						pool.AddToCounts(character, num);
					}
				}
			}
		}
	}

	private TroopRoster? SparringSecondaryRoster()
	{
		if (_pendingSparringHomestead != null)
		{
			return _pendingSparringHomestead.Troops;
		}
		try
		{
			return _pendingSparringSettlement?.Town?.GarrisonParty?.MemberRoster;
		}
		catch
		{
			return null;
		}
	}

	private void TickSparringSelection(float dt)
	{
		if (_sparSelectPhase == 10 || _sparSelectPhase == 20)
		{
			_sparSelectIdleTime += dt;
			if (_sparSelectIdleTime > 90f)
			{
				TraceLogger.Write("HomesteadBehavior", "Sparring selection abandoned.");
				_pendingSparringHomestead = null;
				_pendingSparringSettlement = null;
				_pendingSparringArmsMaster = null;
				_sparSelectPhase = 0;
				_sparPlayerRoster = null;
				_sparEnemyRoster = null;
			}
			return;
		}
		if (Campaign.Current.CurrentMenuContext == null)
		{
			GameMenu.ActivateGameMenu((_pendingSparringHomestead != null) ? "homestead_menu_main" : "town");
			return;
		}
		if (_sparSelectDelayTicks > 0)
		{
			_sparSelectDelayTicks--;
			return;
		}
		int maxSelectableTroopCount = Math.Max(1, _pendingSparringTeamSize);
		if (_sparSelectPhase == 1)
		{
			if (!SparringPending)
			{
				_sparSelectPhase = 0;
				return;
			}
			TroopRoster fullRoster = BuildSparringPool(SparringSecondaryRoster(), includePlayer: true);
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			troopRoster.AddToCounts(CharacterObject.PlayerCharacter, 1);
			_sparSelectPhase = 10;
			_sparSelectIdleTime = 0f;
			InvokeOpenTroopSelection(Campaign.Current.CurrentMenuContext, fullRoster, troopRoster, (CharacterObject character) => character != CharacterObject.PlayerCharacter, delegate(TroopRoster selectedRoster)
			{
				_sparPlayerRoster = selectedRoster;
				_sparSelectPhase = 2;
				_sparSelectDelayTicks = 2;
			}, maxSelectableTroopCount, 1);
		}
		else
		{
			if (_sparSelectPhase != 2)
			{
				return;
			}
			if (!SparringPending)
			{
				_sparSelectPhase = 0;
				return;
			}
			TroopRoster troopRoster2 = BuildSparringPool(SparringSecondaryRoster(), includePlayer: false, _sparPlayerRoster);
			if (troopRoster2.TotalManCount <= 0)
			{
				_sparEnemyRoster = null;
				_sparSelectPhase = 0;
				return;
			}
			TroopRoster initialSelections = TroopRoster.CreateDummyTroopRoster();
			_sparSelectPhase = 20;
			_sparSelectIdleTime = 0f;
			InvokeOpenTroopSelection(Campaign.Current.CurrentMenuContext, troopRoster2, initialSelections, (CharacterObject character) => true, delegate(TroopRoster selectedRoster)
			{
				_sparEnemyRoster = selectedRoster;
				_sparSelectPhase = 0;
			}, maxSelectableTroopCount, 1);
		}
	}

	private void InvokeOpenTroopSelection(MenuContext menuContext, TroopRoster fullRoster, TroopRoster initialSelections, Func<CharacterObject, bool> canChangeStatusOfTroop, Action<TroopRoster> onDone, int maxSelectableTroopCount, int minSelectableTroopCount)
	{
		MethodInfo method = menuContext.GetType().GetMethod("OpenTroopSelection");
		if (method != null)
		{
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length == 6)
			{
				method.Invoke(menuContext, new object[6] { fullRoster, initialSelections, canChangeStatusOfTroop, onDone, maxSelectableTroopCount, minSelectableTroopCount });
			}
			else if (parameters.Length == 8)
			{
				method.Invoke(menuContext, new object[8] { fullRoster, initialSelections, null, canChangeStatusOfTroop, onDone, maxSelectableTroopCount, minSelectableTroopCount, false });
			}
			else
			{
				TraceLogger.Write("HomesteadBehavior", $"InvokeOpenTroopSelection: Unrecognized OpenTroopSelection signature with {parameters.Length} parameters.");
			}
		}
		else
		{
			TraceLogger.Write("HomesteadBehavior", "InvokeOpenTroopSelection: OpenTroopSelection method not found on MenuContext.");
		}
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		starter.AddGameMenu("homestead_menu_main", "{=homestead_gamemenu_main_fmt}You arrive at your homestead of {CURRENT_HOMESTEAD_NAME}. What would you like to do?", SetHomesteadMenuBackground);
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_talk_leader", "{=homestead_gamemenu_talk_leader_fmt}Talk to {CURRENT_HOMESTEAD_LEADER_NAME}", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			return CurrentHomestead != null && CurrentHomestead.Leader != null;
		}, delegate
		{
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(CurrentHomestead.Leader.CharacterObject, CurrentHomestead.Party);
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
		});
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_assign_leader", "{=homestead_gamemenu_assign_leader}Assign leader", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Manage;
			return CurrentHomestead != null && CurrentHomestead.Leader == null;
		}, delegate
		{
			Utils.ShowSelectNewHomesteadLeaderScreen(CurrentHomestead, fromHomesteadMenu: true);
		});
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_visit_notable", "{=homestead_gamemenu_visit_notable}Visit a notable", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			Homestead homestead = CurrentHomestead;
			if (homestead != null)
			{
				Hero? marketLadyHero = homestead.MarketLadyHero;
				if (marketLadyHero == null || !marketLadyHero.IsAlive || !homestead.HasMarket)
				{
					Hero? houndMasterHero = homestead.HoundMasterHero;
					if (houndMasterHero == null || !houndMasterHero.IsAlive || !homestead.HasDogKennel)
					{
						Hero? ambassadorHero = homestead.AmbassadorHero;
						if (ambassadorHero == null || !ambassadorHero.IsAlive || !homestead.HasAmbassadorHall)
						{
							Hero? armsMasterHero = homestead.ArmsMasterHero;
							if (armsMasterHero == null || !armsMasterHero.IsAlive || !homestead.HasTrainingFieldBuilding)
							{
								Hero? masterSmithHero = homestead.MasterSmithHero;
								if (masterSmithHero == null || !masterSmithHero.IsAlive || !homestead.HasSmithy)
								{
									Hero? stableMasterHero = homestead.StableMasterHero;
									if (stableMasterHero != null && stableMasterHero.IsAlive)
									{
										return homestead.HasStable;
									}
									return false;
								}
							}
						}
					}
				}
				return true;
			}
			return false;
		}, delegate
		{
			Homestead.SetGameTextsForMenus();
			GameMenu.SwitchToMenu("homestead_menu_notables");
		});
		starter.AddGameMenu("homestead_menu_notables", "{=homestead_notables_menu_text}Who would you like to speak with?", SetHomesteadMenuBackground);
		starter.AddGameMenuOption("homestead_menu_notables", "homestead_menu_talk_marketlady", "{=homestead_gamemenu_talk_marketlady}Talk to Market Lady", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = CurrentHomestead?.MarketLadyHero;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			return hero != null && hero.IsAlive && hero.CharacterObject != null && (CurrentHomestead?.HasMarket ?? false);
		}, delegate
		{
			StartHomesteadTalk(CurrentHomestead, CurrentHomestead.MarketLadyHero);
		});
		starter.AddGameMenuOption("homestead_menu_notables", "homestead_menu_talk_houndmaster", "{=homestead_gamemenu_talk_houndmaster}Talk to Hound Master", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = CurrentHomestead?.HoundMasterHero;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			return hero != null && hero.IsAlive && hero.CharacterObject != null && (CurrentHomestead?.HasDogKennel ?? false);
		}, delegate
		{
			StartHomesteadTalk(CurrentHomestead, CurrentHomestead.HoundMasterHero);
		});
		starter.AddGameMenuOption("homestead_menu_notables", "homestead_menu_talk_ambassador", "{=homestead_gamemenu_talk_ambassador}Talk to Ambassador", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = CurrentHomestead?.AmbassadorHero;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			return hero != null && hero.IsAlive && hero.CharacterObject != null && (CurrentHomestead?.HasAmbassadorHall ?? false);
		}, delegate
		{
			StartHomesteadTalk(CurrentHomestead, CurrentHomestead.AmbassadorHero);
		});
		starter.AddGameMenuOption("homestead_menu_notables", "homestead_menu_talk_armsmaster", "{=homestead_gamemenu_talk_armsmaster}Talk to Arms Master", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = CurrentHomestead?.ArmsMasterHero;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			return hero != null && hero.IsAlive && hero.CharacterObject != null && (CurrentHomestead?.HasTrainingFieldBuilding ?? false);
		}, delegate
		{
			StartHomesteadTalk(CurrentHomestead, CurrentHomestead.ArmsMasterHero);
		});
		starter.AddGameMenuOption("homestead_menu_notables", "homestead_menu_talk_mastersmith", "{=homestead_gamemenu_talk_mastersmith}Talk to the Master Smith", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = CurrentHomestead?.MasterSmithHero;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			return hero != null && hero.IsAlive && hero.CharacterObject != null && (CurrentHomestead?.HasSmithy ?? false);
		}, delegate
		{
			StartHomesteadTalk(CurrentHomestead, CurrentHomestead.MasterSmithHero);
		});
		starter.AddGameMenuOption("homestead_menu_notables", "homestead_menu_talk_stablemaster", "{=homestead_gamemenu_talk_stablemaster}Talk to the Stable Master", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = CurrentHomestead?.StableMasterHero;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			return hero != null && hero.IsAlive && hero.CharacterObject != null && (CurrentHomestead?.HasStable ?? false);
		}, delegate
		{
			StartHomesteadTalk(CurrentHomestead, CurrentHomestead.StableMasterHero);
		});
		starter.AddGameMenuOption("homestead_menu_notables", "homestead_menu_notables_back", "{=homestead_back}Back", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, delegate
		{
			GameMenu.SwitchToMenu("homestead_menu_main");
		});
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_smith_refine", "{=homestead_gamemenu_smith_refine}Have the Master Smith refine a piece", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Manage;
			Hero hero = CurrentHomestead?.MasterSmithHero;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			if (hero != null && hero.IsAlive)
			{
				Homestead? homestead = CurrentHomestead;
				if (homestead != null && homestead.HasSmithy && HasMasterSmithUpgradeUnlocked)
				{
					return !(CurrentHomestead?.SmithUpgradePending ?? false);
				}
			}
			return false;
		}, delegate
		{
			OpenSmithUpgradePicker();
		});
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_smith_collect", "{=homestead_gamemenu_smith_collect}Collect your refined piece from the Master Smith", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Manage;
			Hero hero = CurrentHomestead?.MasterSmithHero;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			if (hero != null && hero.IsAlive)
			{
				Homestead? homestead = CurrentHomestead;
				if (homestead != null && homestead.HasSmithy)
				{
					return CurrentHomestead?.SmithUpgradeReady ?? false;
				}
			}
			return false;
		}, delegate
		{
			CollectSmithUpgrade(CurrentHomestead);
			GameMenu.SwitchToMenu("homestead_menu_main");
		});
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_visit_tavern", "{=homestead_gamemenu_visit_tavern}Visit the tavern", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			return CurrentHomestead?.HasTavernBuilding ?? false;
		}, delegate
		{
			Homestead homestead = ResolveCurrentHomesteadForMenu("visit tavern");
			if (homestead == null)
			{
				TraceLogger.Write("HomesteadBehavior", "Visit tavern requested but no current homestead could be resolved.");
			}
			else
			{
				CurrentHomestead = homestead;
				TavernMissionActive = true;
				if (CustomMissions.StartHomesteadTavernMission(homestead) == null)
				{
					TavernMissionActive = false;
				}
			}
		});
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_enter_smithy", "{=homestead_gamemenu_enter_smithy}Enter the smithy", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Craft;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			Homestead? homestead = CurrentHomestead;
			return homestead != null && homestead.HasSmithy && CurrentHomestead?.MasterSmithHero != null && CurrentHomestead.MasterSmithHero.IsAlive;
		}, delegate
		{
			OpenHomesteadForge();
		});
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_choose_map", "{=homestead_gamemenu_choose_map}Choose homestead map", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return CurrentHomestead != null && CurrentHomestead.CanRepickScene;
		}, delegate
		{
			Homestead hs = CurrentHomestead;
			if (hs != null)
			{
				List<string> candidateSceneNames = hs.GetCandidateSceneNames();
				if (candidateSceneNames.Count <= 1)
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=homestead_map_only_one}This spot has only one battlefield map available, so there's nothing to choose — your homestead uses it."));
				}
				else
				{
					string text = hs.GetHomesteadScene()?.SceneName;
					List<InquiryElement> list = new List<InquiryElement>();
					foreach (string item2 in candidateSceneNames)
					{
						string title = item2 + ((item2 == text) ? " (current)" : "");
						list.Add(new InquiryElement(item2, title, null));
					}
					MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=homestead_map_pick_title}Choose Homestead Map").ToString(), new TextObject("{=homestead_map_pick_text}Pick a map, then Walk around to preview it. You can change it freely until you build your first structure, after which it locks in.").ToString(), list, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate(List<InquiryElement> selected)
					{
						if (selected != null && selected.Count > 0 && selected[0].Identifier is string sceneName && hs.TryChooseScene(sceneName))
						{
							MBInformationManager.AddQuickInformation(new TextObject("{=homestead_map_chosen}Homestead map changed — Walk around to preview it."));
						}
					}, null));
				}
			}
		});
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_walk_around", "{=homestead_gamemenu_walk_around}Walk around", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			return true;
		}, delegate
		{
			Homestead homestead = ResolveCurrentHomesteadForMenu("walk around");
			if (homestead == null)
			{
				TraceLogger.Write("HomesteadBehavior", "Walk around requested but no current homestead could be resolved.");
				PlayerEncounter.Finish();
			}
			else
			{
				TraceLogger.Write("HomesteadBehavior", string.Format("Starting walk-around for '{0}' scene='{1}' party='{2}'.", homestead.Name, homestead.GetHomesteadScene().SceneName, homestead.MobileParty?.StringId ?? "null"));
				CustomMissions.StartHomesteadMission(homestead);
			}
		});
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_goto_manage", "{=homestead_gamemenu_manage_homestead}Manage homestead", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			if (ResolveCurrentHomesteadForMenu("manage homestead condition") == null)
			{
				return false;
			}
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			return true;
		}, delegate
		{
			Homestead.SetGameTextsForMenus();
			GameMenu.SwitchToMenu("homestead_menu_manage_main");
			HomesteadTutorial.ManagingHomestead();
		});
		starter.AddGameMenu("homestead_menu_manage_main", "{CURRENT_HOMESTEAD_INFORMATION}", SetHomesteadMenuBackground);
		starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_autorecruittoggle", "{=homestead_gamemenu_autorecruittoggle_fmt}Toggle auto recruit ({CURRENT_HOMESTEAD_AUTO_RECRUIT_STATE})", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leaderboard;
			Homestead homestead = ResolveCurrentHomesteadForMenu("auto recruit toggle condition");
			if (homestead == null)
			{
				return false;
			}
			GameTexts.SetVariable("CURRENT_HOMESTEAD_AUTO_RECRUIT_STATE", homestead.AutoRecruitEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
			return true;
		}, delegate
		{
			Homestead homestead = ResolveCurrentHomesteadForMenu("auto recruit toggle");
			if (homestead != null)
			{
				homestead.AutoRecruitEnabled = !homestead.AutoRecruitEnabled;
				string item = (homestead.AutoRecruitEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
				float r = (homestead.AutoRecruitEnabled ? 0f : 255f);
				float g = (homestead.AutoRecruitEnabled ? 255f : 0f);
				Homestead.SetGameTextsForMenus();
				Utils.PrintLocalizedMessage("homestead_autorecruittoggle_message", "Auto recruit is now {AUTO_RECRUIT_STATE}.", r, g, 0f, ("AUTO_RECRUIT_STATE", item));
				GameMenu.SwitchToMenu("homestead_menu_manage_main");
			}
		});
		starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_autopatrol_toggle", "{=homestead_gamemenu_autopatrol_toggle_fmt}Toggle auto patrol ({CURRENT_HOMESTEAD_AUTO_PATROL_STATE})", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leaderboard;
			Homestead homestead = ResolveCurrentHomesteadForMenu("auto patrol toggle condition");
			if (homestead == null)
			{
				return false;
			}
			GameTexts.SetVariable("CURRENT_HOMESTEAD_AUTO_PATROL_STATE", homestead.AutoPatrolEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
			return true;
		}, delegate
		{
			Homestead homestead = ResolveCurrentHomesteadForMenu("auto patrol toggle");
			if (homestead != null)
			{
				homestead.AutoPatrolEnabled = !homestead.AutoPatrolEnabled;
				string item = (homestead.AutoPatrolEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
				float r = (homestead.AutoPatrolEnabled ? 0f : 255f);
				float g = (homestead.AutoPatrolEnabled ? 255f : 0f);
				Homestead.SetGameTextsForMenus();
				Utils.PrintLocalizedMessage("homestead_autopatrol_toggle_message", "Auto patrol is now {AUTO_PATROL_STATE}.", r, g, 0f, ("AUTO_PATROL_STATE", item));
				GameMenu.SwitchToMenu("homestead_menu_manage_main");
			}
		});
		starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_caravantrade_toggle", "{=homestead_gamemenu_caravantrade_toggle_fmt}Toggle caravan trading ({CURRENT_HOMESTEAD_CARAVAN_TRADE_STATE})", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leaderboard;
			Homestead homestead = ResolveCurrentHomesteadForMenu("caravan trade toggle condition");
			if (homestead == null)
			{
				return false;
			}
			GameTexts.SetVariable("CURRENT_HOMESTEAD_CARAVAN_TRADE_STATE", homestead.CaravanTradingEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
			return true;
		}, delegate
		{
			Homestead homestead = ResolveCurrentHomesteadForMenu("caravan trade toggle");
			if (homestead != null)
			{
				homestead.CaravanTradingEnabled = !homestead.CaravanTradingEnabled;
				string item = (homestead.CaravanTradingEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
				float r = (homestead.CaravanTradingEnabled ? 0f : 255f);
				float g = (homestead.CaravanTradingEnabled ? 255f : 0f);
				Homestead.SetGameTextsForMenus();
				Utils.PrintLocalizedMessage("homestead_caravantrade_toggle_message", "Caravan trading is now {CARAVAN_TRADE_STATE}.", r, g, 0f, ("CARAVAN_TRADE_STATE", item));
				GameMenu.SwitchToMenu("homestead_menu_manage_main");
			}
		});
		starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_autofoodbuy_toggle", "{=homestead_gamemenu_autofoodbuy_toggle_fmt}Toggle auto food buy ({CURRENT_HOMESTEAD_AUTO_FOOD_BUY_STATE})", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leaderboard;
			Homestead homestead = ResolveCurrentHomesteadForMenu("auto food buy toggle condition");
			if (homestead == null)
			{
				return false;
			}
			GameTexts.SetVariable("CURRENT_HOMESTEAD_AUTO_FOOD_BUY_STATE", homestead.AutoFoodBuyEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
			return true;
		}, delegate
		{
			Homestead homestead = ResolveCurrentHomesteadForMenu("auto food buy toggle");
			if (homestead != null)
			{
				homestead.AutoFoodBuyEnabled = !homestead.AutoFoodBuyEnabled;
				string item = (homestead.AutoFoodBuyEnabled ? Utils.GetLocalizedString("{=homestead_state_enabled}Enabled") : Utils.GetLocalizedString("{=homestead_state_disabled}Disabled"));
				float r = (homestead.AutoFoodBuyEnabled ? 0f : 255f);
				float g = (homestead.AutoFoodBuyEnabled ? 255f : 0f);
				Homestead.SetGameTextsForMenus();
				Utils.PrintLocalizedMessage("homestead_autofoodbuy_toggle_message", "Auto food buy is now {AUTO_FOOD_BUY_STATE}.", r, g, 0f, ("AUTO_FOOD_BUY_STATE", item));
				GameMenu.SwitchToMenu("homestead_menu_manage_main");
			}
		});
		starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_rename", "{=homestead_gamemenu_rename}Rename homestead", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leaderboard;
			return ResolveCurrentHomesteadForMenu("rename condition") != null;
		}, delegate
		{
			Homestead homestead = ResolveCurrentHomesteadForMenu("rename");
			if (homestead != null)
			{
				Utils.ShowNameHomesteadScreen(homestead, delegate
				{
					GameMenu.SwitchToMenu("homestead_menu_manage_main");
				});
			}
		});
		starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_garrison", "{=homestead_gamemenu_manage_garrison}Manage garrison", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.ManageGarrison;
			return ResolveCurrentHomesteadForMenu("manage garrison condition")?.MobileParty != null;
		}, delegate
		{
			Homestead homestead = ResolveCurrentHomesteadForMenu("manage garrison");
			if (homestead?.MobileParty != null)
			{
				HomesteadPartyScreenTalkGuardPatch.BeginHomesteadPartyScreen(homestead.MobileParty.StringId, homestead.GetPrisonerLimit());
				TroopRoster leftMemberRoster = CloneMemberRosterForScreen(homestead.MobileParty.MemberRoster, homestead.MobileParty);
				TroopRoster leftPrisonerRoster = ClonePrisonRosterForScreen(homestead.MobileParty.PrisonRoster);
				PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
				partyScreenLogic.Initialize(new PartyScreenLogicInitializationData
				{
					LeftOwnerParty = null,
					RightOwnerParty = MobileParty.MainParty.Party,
					LeftMemberRoster = leftMemberRoster,
					LeftPrisonerRoster = leftPrisonerRoster,
					RightMemberRoster = MobileParty.MainParty.MemberRoster,
					RightPrisonerRoster = MobileParty.MainParty.PrisonRoster,
					LeftLeaderHero = null,
					RightLeaderHero = Hero.MainHero,
					LeftPartyName = homestead.Name,
					RightPartyName = MobileParty.MainParty.Name,
					LeftPartyMembersSizeLimit = homestead.MobileParty.Party.PartySizeLimit,
					LeftPartyPrisonersSizeLimit = homestead.GetPrisonerLimit(),
					RightPartyMembersSizeLimit = MobileParty.MainParty.Party.PartySizeLimit,
					RightPartyPrisonersSizeLimit = MobileParty.MainParty.Party.PrisonerSizeLimit,
					MemberTransferState = PartyScreenLogic.TransferState.Transferable,
					PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
					AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable,
					TroopTransferableDelegate = (CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty) => (!character.IsHero || type != PartyScreenLogic.TroopType.Member) ? true : false,
					PartyScreenMode = Helpers.PartyScreenHelper.PartyScreenMode.Normal,
					IsDismissMode = false,
					IsTroopUpgradesDisabled = false,
					Header = null,
					PartyPresentationDoneButtonDelegate = delegate(TroopRoster lMem, TroopRoster lPris, TroopRoster rMem, TroopRoster rPris, FlattenedTroopRoster taken, FlattenedTroopRoster released, bool isForced, PartyBase lOwner, PartyBase rOwner)
					{
						if (homestead?.MobileParty != null)
						{
							ApplyNonHeroRosterChanges(homestead.MobileParty.MemberRoster, lMem);
							ApplyNonHeroRosterChanges(homestead.MobileParty.PrisonRoster, lPris);
							TraceLogger.Write("HomesteadBehavior", $"Garrison Done for '{homestead.Name}' — applied cloned left-side changes.");
						}
						return true;
					},
					PartyScreenClosedDelegate = delegate(PartyBase lOwner, TroopRoster lMem, TroopRoster lPris, PartyBase rOwner, TroopRoster rMem, TroopRoster rPris, bool fromCancel)
					{
						HomesteadPartyScreenTalkGuardPatch.EndHomesteadPartyScreen();
						TraceLogger.Write("HomesteadBehavior", string.Format("Garrison screen closed (fromCancel={0}) for '{1}'.", fromCancel, homestead?.Name?.ToString() ?? "unknown"));
						GameMenu.SwitchToMenu("homestead_menu_manage_main");
					}
				});
				PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
				partyState.PartyScreenLogic = partyScreenLogic;
				partyState.IsDonating = false;
				partyState.PartyScreenMode = Helpers.PartyScreenHelper.PartyScreenMode.Normal;
				Game.Current.GameStateManager.PushState(partyState);
			}
		});
		starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_stash", "{=homestead_gamemenu_manage_stash}Manage food/stash", delegate(MenuCallbackArgs args)
		{
			Homestead homestead = ResolveCurrentHomesteadForMenu("manage stash condition");
			if (homestead != null)
			{
				TextObject textObject = new TextObject("{=homestead_gamemenu_manage_stash_fmt}{MENU_TEXT} ({COUNT}/{CAPACITY})");
				textObject.SetTextVariable("MENU_TEXT", new TextObject("{=homestead_gamemenu_manage_stash}Manage food/stash"));
				textObject.SetTextVariable("COUNT", homestead.GetStashTotalItemCount());
				textObject.SetTextVariable("CAPACITY", homestead.GetStashCapacity());
				args.Text = textObject;
			}
			args.optionLeaveType = GameMenuOption.LeaveType.OpenStash;
			return homestead?.MobileParty?.ItemRoster != null;
		}, delegate
		{
			Homestead homestead = ResolveCurrentHomesteadForMenu("manage stash");
			if (homestead?.MobileParty?.ItemRoster != null)
			{
				InventoryScreenHelper.OpenScreenAsStash(homestead.MobileParty.ItemRoster);
			}
		});
		starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_gold", "{=homestead_gamemenu_deposit_withdraw_gold}Deposit/withdraw gold", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Bribe;
			return ResolveCurrentHomesteadForMenu("gold change condition") != null;
		}, delegate
		{
			string localizedString = Utils.GetLocalizedString("{=homestead_goldchange_title}Deposit/Withdraw Gold");
			string localizedString2 = Utils.GetLocalizedString("{=homestead_goldchange_text}Enter an amount to deposit or withdraw. A negative number means you will withdraw.");
			Utils.ShowTextInputMessage(localizedString, localizedString2, delegate(string input)
			{
				int result = 0;
				int.TryParse(input, out result);
				if (result == 0)
				{
					Utils.PrintLocalizedMessage("homestead_amount_entered_not_valid", "Amount entered must be a valid number.", 255f, 255f, 255f);
				}
				else
				{
					Homestead homestead = ResolveCurrentHomesteadForMenu("gold change");
					string failReason;
					if (homestead == null)
					{
						Utils.PrintLocalizedMessage("homestead_no_active_homestead_for_gold", "No active homestead was found for this gold transfer.", 255f, 80f, 80f);
						PlayerEncounter.Finish();
					}
					else if (!homestead.PlayerChangeGoldStored(result, out failReason))
					{
						Utils.PrintDebugMessage(failReason);
					}
					else
					{
						GameMenu.SwitchToMenu("homestead_menu_manage_main");
					}
				}
			});
		});
		starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_pay_upgrade", "{=homestead_gamemenu_pay_upgrade_fmt}Pay {CURRENT_HOMESTEAD_PAID_UPGRADE_COST} gold {CURRENT_HOMESTEAD_UPGRADE_TARGET}", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Bribe;
			Homestead homestead = ResolveCurrentHomesteadForMenu("pay upgrade condition");
			if (homestead == null)
			{
				return false;
			}
			return (homestead.Tier != 3 || !homestead.SettlementUpgradeReady) && homestead.Tier < 4;
		}, delegate
		{
			PayForCurrentHomesteadUpgrade();
		});
		starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_back", "{=homestead_gamemenu_back}Back", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, delegate
		{
			GameMenu.SwitchToMenu("homestead_menu_main");
		}, isLeave: true);
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_wait", "{=homestead_gamemenu_wait}Wait here", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Wait;
			if (CurrentHomestead != null && CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_disabled_tooltip}Not available while the homestead is packed up and relocating.");
				args.IsEnabled = false;
			}
			return true;
		}, delegate
		{
			GameMenu.SwitchToMenu("homestead_menu_wait_waiting");
		});
		starter.AddWaitGameMenu("homestead_menu_wait_waiting", "{=homestead_gamemenu_waiting}Waiting at {CURRENT_HOMESTEAD_NAME}...", delegate(MenuCallbackArgs args)
		{
			SetHomesteadMenuBackground(args);
			Homestead.SetGameTextsForMenus();
			args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(9999f, 0f);
			MobileParty.MainParty.SetMoveModeHold();
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.IsPlayerWaiting = true;
			}
		}, delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Wait;
			return true;
		}, null, delegate(MenuCallbackArgs args, CampaignTime dt)
		{
			MobileParty.MainParty.SetMoveModeHold();
			CurrentHomestead?.EnsurePartyStaysAtAnchor("wait menu tick");
			if (MobileParty.MainParty?.MapEvent != null)
			{
				TraceLogger.Write("HomesteadBehavior", "Wait tick interrupt: MainParty is now in a MapEvent — switching to homestead encounter menu.");
				args.MenuContext.GameMenu.EndWait();
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Current.IsPlayerWaiting = false;
				}
				GameMenu.SwitchToMenu("homestead_menu_encounter");
			}
			else
			{
				string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
				if (genericStateMenu != null && genericStateMenu != "homestead_menu_wait_waiting")
				{
					string text = genericStateMenu;
					if (CurrentHomestead != null && (genericStateMenu == "encounter" || genericStateMenu == "encounter_meeting"))
					{
						text = "homestead_menu_encounter";
					}
					TraceLogger.Write("HomesteadBehavior", "Wait tick interrupt: GetGenericStateMenu() returned '" + genericStateMenu + "' — switching to '" + text + "'.");
					args.MenuContext.GameMenu.EndWait();
					if (PlayerEncounter.Current != null)
					{
						PlayerEncounter.Current.IsPlayerWaiting = false;
					}
					GameMenu.SwitchToMenu(text);
				}
				else
				{
					_waitMenuHostileCheckTimer += (float)dt.ToSeconds;
					if (_waitMenuHostileCheckTimer >= 2f)
					{
						_waitMenuHostileCheckTimer = 0f;
						MobileParty mobileParty = FindNearbyHostilePartyForHomestead(CurrentHomestead, 8f);
						if (mobileParty != null && mobileParty.StringId != _lastWarnedAttackerStringId)
						{
							_lastWarnedAttackerStringId = mobileParty.StringId;
							string text2 = mobileParty.MapFaction?.Name?.ToString() ?? "Unknown";
							string text3 = mobileParty.Name?.ToString() ?? mobileParty.StringId;
							Utils.PrintLocalizedMessage("homestead_enemy_approaching", "Enemy forces are approaching your homestead! (" + text3 + " — " + text2 + ")", 255f, 100f, 50f);
							TraceLogger.Write("HomesteadBehavior", $"Wait tick: Enemy '{mobileParty.StringId}' ({text2}) approaching homestead '{CurrentHomestead?.Name}' within {8f} map-units.");
						}
						MobileParty mobileParty2 = FindNearbyHostilePartyForHomestead(CurrentHomestead);
						if (mobileParty2 != null)
						{
							TraceLogger.Write("HomesteadBehavior", $"Wait tick: Hostile party '{mobileParty2.StringId}' ({mobileParty2.MapFaction?.Name}) within {1f} map-units of homestead '{CurrentHomestead.Name}' — forcing encounter.");
							try
							{
								MobileParty mobileParty3 = CurrentHomestead.MobileParty;
								EncounterManager.StartPartyEncounter(mobileParty2.Party, mobileParty3.Party);
							}
							catch (Exception ex)
							{
								TraceLogger.Write("HomesteadBehavior", "Wait tick: EncounterManager.StartPartyEncounter failed: " + ex.Message);
								return;
							}
							_lastWarnedAttackerStringId = null;
							args.MenuContext.GameMenu.EndWait();
							if (PlayerEncounter.Current != null)
							{
								PlayerEncounter.Current.IsPlayerWaiting = false;
							}
							GameMenu.SwitchToMenu("homestead_menu_encounter");
						}
					}
				}
			}
		}, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
		starter.AddGameMenuOption("homestead_menu_wait_waiting", "homestead_menu_wait_leave", "{=homestead_gamemenu_stop_waiting}Stop waiting", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, delegate(MenuCallbackArgs args)
		{
			args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(100f);
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			if (CurrentHomestead?.MobileParty != null && !CurrentHomestead.IsRetiredOrDestroyed)
			{
				GameMenu.SwitchToMenu("homestead_menu_main");
			}
			else
			{
				CurrentHomestead = null;
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Finish();
				}
			}
		}, isLeave: true);
		starter.AddGameMenu("homestead_menu_encounter", "{=homestead_gamemenu_encounter_text}Your homestead is under attack!", delegate(MenuCallbackArgs args)
		{
			HomesteadBattleContext.SuppressEncounterReinit = false;
			SetHomesteadMenuBackground(args);
			if (PlayerEncounter.Battle == null)
			{
				if (MobileParty.MainParty.MapEvent != null)
				{
					PlayerEncounter.Init();
					TryReapplyHomesteadDefenderSetup();
				}
				else
				{
					PlayerEncounter.StartBattle();
				}
			}
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Update();
			}
		}, GameMenu.MenuOverlayType.Encounter);
		starter.AddGameMenuOption("homestead_menu_encounter", "homestead_encounter_fight", "{=homestead_gamemenu_encounter_fight}Fight!", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			if (PlayerEncounter.Current == null)
			{
				return false;
			}
			if (Hero.MainHero.IsWounded)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=homestead_fight_wounded_tooltip}You are too wounded to lead the battle in person. Send your troops to fight without you.");
			}
			return true;
		}, delegate(MenuCallbackArgs args)
		{
			TryReapplyHomesteadDefenderSetup();
			if (PlayerEncounter.Battle == null)
			{
				PlayerEncounter.StartBattle();
			}
			PlayerEncounter.Update();
			MenuHelper.EncounterAttackConsequence(args);
		});
		starter.AddGameMenuOption("homestead_menu_encounter", "homestead_encounter_send_troops", "{=homestead_gamemenu_encounter_send_troops}Send troops (let them fight without you)", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
			return PlayerEncounter.Current != null && MobileParty.MainParty.MapEvent != null;
		}, delegate(MenuCallbackArgs args)
		{
			TryReapplyHomesteadDefenderSetup();
			if (PlayerEncounter.Battle == null)
			{
				PlayerEncounter.StartBattle();
			}
			MenuHelper.EncounterOrderAttackConsequence(args);
		});
		starter.AddGameMenuOption("homestead_menu_encounter", "homestead_encounter_parley", "{=homestead_gamemenu_encounter_parley}Parley", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			MapEvent mapEvent = MobileParty.MainParty.MapEvent;
			if (mapEvent == null)
			{
				return false;
			}
			foreach (MapEventParty party in mapEvent.AttackerSide.Parties)
			{
				if (party.Party.MobileParty?.LeaderHero != null)
				{
					return true;
				}
			}
			return false;
		}, delegate
		{
			MapEvent mapEvent = MobileParty.MainParty.MapEvent;
			PartyBase partyBase = null;
			if (mapEvent != null)
			{
				foreach (MapEventParty party2 in mapEvent.AttackerSide.Parties)
				{
					if (party2.Party.MobileParty?.LeaderHero != null)
					{
						partyBase = party2.Party;
						break;
					}
				}
			}
			if (partyBase == null)
			{
				TraceLogger.Write("HomesteadBehavior", "Parley: no hero leader found on attacker side — cannot open dialog.");
				PlayerEncounter.Update();
				return;
			}
			HomesteadBattleContext.SuppressEncounterReinit = true;
			try
			{
				PlayerEncounter.Current?.SetupFields(partyBase, PartyBase.MainParty);
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Current.IsPlayerWaiting = false;
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadBehavior", "Parley: SetupFields(enemy, MainParty) threw: " + ex.Message);
			}
			try
			{
				Hero hero = partyBase.MobileParty?.LeaderHero;
				if (hero == null)
				{
					TraceLogger.Write("HomesteadBehavior", "Parley: enemy party leader hero is null after party lookup — aborting.");
					HomesteadBattleContext.SuppressEncounterReinit = false;
					PlayerEncounter.Update();
				}
				else
				{
					ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
					ConversationCharacterData conversationPartnerData = new ConversationCharacterData(hero.CharacterObject, partyBase);
					TraceLogger.Write("HomesteadBehavior", $"Parley: opening conversation directly with '{hero.Name}' ('{partyBase.MobileParty?.StringId}') via CampaignMapConversation.OpenConversation.");
					CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
				}
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("HomesteadBehavior", "Parley: CampaignMapConversation.OpenConversation threw: " + ex2.Message);
				HomesteadBattleContext.SuppressEncounterReinit = false;
				PlayerEncounter.Update();
			}
		});
		starter.AddGameMenuOption("homestead_menu_encounter", "homestead_encounter_flee", "{=homestead_gamemenu_encounter_flee}Flee!", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, delegate
		{
			PlayerEncounter.LeaveEncounter = true;
			PlayerEncounter.Update();
		}, isLeave: true);
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_move", "{=homestead_gamemenu_move}Move Homestead", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			if (CurrentHomestead == null || CurrentHomestead.Leader == null)
			{
				return false;
			}
			if (CurrentHomestead.IsMoving)
			{
				args.Tooltip = new TextObject("{=homestead_moving_tooltip}Homestead is already moving to a new location.");
				args.IsEnabled = false;
			}
			return true;
		}, delegate
		{
			GameMenu.ExitToLast();
			if (MapScreen.Instance != null && CurrentHomestead != null)
			{
				CampaignTimeControlMode priorTimeMode = Campaign.Current.TimeControlMode;
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
				((HomesteadPlacementMapView)(object)MapScreen.Instance.AddMapView<HomesteadPlacementMapView>(Array.Empty<object>())).Initialize(CurrentHomestead, delegate(Vec2 target)
				{
					Campaign.Current.TimeControlMode = ((priorTimeMode == CampaignTimeControlMode.Stop) ? CampaignTimeControlMode.StoppablePlay : priorTimeMode);
					CurrentHomestead.StartMoving(target);
				}, delegate
				{
					Campaign.Current.TimeControlMode = priorTimeMode;
				});
			}
		});
		starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_leave", "{=homestead_gamemenu_leave}Leave", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, delegate
		{
			CurrentHomestead = null;
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Finish();
			}
			else
			{
				GameMenu.ExitToLast();
			}
		}, isLeave: true);
	}

	private void CheckHomesteadHourlyTick(MobileParty party, PartyThinkParams thinkParams)
	{
		if (_escortingVillagers.Count > 0 && _escortingVillagers.Contains(party))
		{
			if (party.IsActive && !party.IsDisbanding)
			{
				party.SetMoveEscortParty(MobileParty.MainParty, MobileParty.NavigationType.Default, isTargetingPort: false);
				thinkParams.DoNotChangeBehavior = true;
				return;
			}
			_escortingVillagers.Remove(party);
		}
		if (PatrolMobileParties.TryGetValue(party, out Homestead value))
		{
			if (party.Party.NumberOfAllMembers <= 0 && party.IsActive && !party.IsDisbanding)
			{
				TraceLogger.Write("HomesteadBehavior", "Patrol party '" + party.StringId + "' has 0 members — destroying ghost (AI hourly tick).");
				try
				{
					DestroyPartyAction.Apply(null, party);
					return;
				}
				catch
				{
					return;
				}
			}
			value?.ApplyPatrolMovement(party);
			return;
		}
		if (_activeCaravanVisits.TryGetValue(party, out HomesteadCaravanVisit value2))
		{
			ApplyCaravanRedirectMovement(party, value2);
			thinkParams.DoNotChangeBehavior = true;
			return;
		}
		Homestead homestead = Homestead.GetFor(party);
		if (homestead != null)
		{
			if (ShouldDestroyBecausePartyInvalid(homestead))
			{
				DestroyInvalidHomesteadParty(homestead, "AI hourly tick");
				return;
			}
			homestead.EnsurePartyStaysAtAnchor("AI hourly tick");
			homestead.HourlyTick();
		}
	}

	internal void SyncHomesteadMapVisuals(string reason)
	{
		if (HomesteadMobileParties.Count == 0 || MobilePartyVisualManager.Current == null)
		{
			return;
		}
		foreach (KeyValuePair<MobileParty, Homestead> item in HomesteadMobileParties.ToList())
		{
			MobileParty key = item.Key;
			Homestead value = item.Value;
			if (key != null && !key.IsDisbanding)
			{
				try
				{
					MobilePartyVisualManager.Current.GetPartyVisual(key.Party);
					value.ApplyCustomMapIcon(reason);
				}
				catch (Exception arg)
				{
					TraceLogger.Write("HomesteadBehavior", string.Format("Failed syncing map visual for '{0}' during {1}: {2}", key?.StringId ?? "null", reason, arg));
				}
			}
		}
	}

	private static TroopRoster CloneMemberRosterForScreen(TroopRoster source, MobileParty ownerParty)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (TroopRosterElement item in source.GetTroopRoster())
		{
			troopRoster.AddToCounts(item.Character, item.Number, insertAtFront: false, item.WoundedNumber, item.Xp);
		}
		return troopRoster;
	}

	private static TroopRoster ClonePrisonRosterForScreen(TroopRoster source)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (TroopRosterElement item in source.GetTroopRoster())
		{
			troopRoster.AddToCounts(item.Character, item.Number, insertAtFront: false, item.WoundedNumber, item.Xp);
		}
		return troopRoster;
	}

	private static void ApplyNonHeroRosterChanges(TroopRoster target, TroopRoster finalState)
	{
		target.RemoveIf((TroopRosterElement e) => !e.Character.IsHero);
		foreach (TroopRosterElement item in finalState.GetTroopRoster())
		{
			if (!item.Character.IsHero)
			{
				target.AddToCounts(item.Character, item.Number, insertAtFront: false, item.WoundedNumber, item.Xp);
			}
		}
	}

	private void SetHomesteadMenuBackground(MenuCallbackArgs args)
	{
		string backgroundMeshName = (Instance?.CurrentHomestead?.Leader?.Culture ?? Hero.MainHero.Culture)?.EncounterBackgroundMesh ?? "";
		args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
	}

	private Homestead? TryGetHomesteadFromActivePlayerEncounter(string reason)
	{
		MobileParty encounteredMobileParty;
		try
		{
			encounteredMobileParty = PlayerEncounter.EncounteredMobileParty;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "Could not resolve active player encounter during " + reason + ": " + ex.Message);
			return null;
		}
		if (encounteredMobileParty == null)
		{
			return null;
		}
		Homestead homestead = Homestead.GetFor(encounteredMobileParty);
		if (homestead != null)
		{
			TraceLogger.Write("HomesteadBehavior", string.Format("Resolved current homestead from active player encounter during {0}: '{1}' party='{2}'.", reason, homestead.Name, encounteredMobileParty.StringId ?? "null"));
		}
		return homestead;
	}

	private Homestead? ResolveCurrentHomesteadForMenu(string reason)
	{
		Homestead homestead = TryGetHomesteadFromActivePlayerEncounter(reason) ?? CurrentHomestead;
		if (homestead == null)
		{
			return null;
		}
		if (!TryEnsureHomesteadUsable(homestead, reason))
		{
			return null;
		}
		CurrentHomestead = homestead;
		return homestead;
	}

	private bool TryEnsureHomesteadUsable(Homestead homestead, string reason)
	{
		if (homestead == null)
		{
			return false;
		}
		if (ShouldDestroyBecausePartyInvalid(homestead) && !TryRecoverInvalidHomesteadParty(homestead, reason))
		{
			DestroyInvalidHomesteadParty(homestead, reason);
			return false;
		}
		TryRepairUnavailableHomesteadLeader(homestead, reason, notify: true);
		MobileParty mobileParty = homestead.MobileParty;
		if (!homestead.IsRetiredOrDestroyed && mobileParty != null && mobileParty.IsActive)
		{
			return !mobileParty.IsDisbanding;
		}
		return false;
	}

	private void CheckHomesteadPassiveHourlyTick(MobileParty party)
	{
		if (party.PartyComponent is HomesteadRaiderPartyComponent homesteadRaiderPartyComponent)
		{
			if (party.Party.NumberOfAllMembers <= 0 && party.IsActive && !party.IsDisbanding)
			{
				try
				{
					DestroyPartyAction.Apply(null, party);
					return;
				}
				catch
				{
					return;
				}
			}
			MobileParty mobileParty = homesteadRaiderPartyComponent.Homestead?.MobileParty;
			if (mobileParty != null && mobileParty.IsActive)
			{
				party.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
				party.Aggressiveness = 1f;
				party.SetMoveGoToPoint(new CampaignVec2(mobileParty.GetPosition2D, isOnLand: true), MobileParty.NavigationType.Default);
			}
			return;
		}
		if (party.PartyComponent is Homestead { IsMoving: not false } homestead)
		{
			if (party.GetPosition2D.DistanceSquared(homestead.TargetPosition) < 1f)
			{
				homestead.FinishMoving();
				return;
			}
			party.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			party.SetMoveGoToPoint(new CampaignVec2(homestead.TargetPosition, isOnLand: true), MobileParty.NavigationType.Default);
			return;
		}
		if (PatrolMobileParties.TryGetValue(party, out Homestead value))
		{
			if (party.Party.NumberOfAllMembers <= 0 && party.IsActive && !party.IsDisbanding)
			{
				TraceLogger.Write("HomesteadBehavior", "Patrol party '" + party.StringId + "' has 0 members — destroying ghost (passive hourly tick).");
				try
				{
					DestroyPartyAction.Apply(null, party);
					return;
				}
				catch
				{
					return;
				}
			}
			value?.ApplyPatrolMovement(party);
			return;
		}
		HomesteadRecruiterComponent homesteadRecruiterComponent = HomesteadRecruiterComponent.GetFor(party);
		if (homesteadRecruiterComponent != null)
		{
			homesteadRecruiterComponent.HourlyTick();
			return;
		}
		if (party.IsCaravan)
		{
			CheckCaravanHourlyTick(party);
		}
		Homestead homestead2 = Homestead.GetFor(party);
		if (homestead2 != null && ShouldDestroyBecausePartyInvalid(homestead2))
		{
			DestroyInvalidHomesteadParty(homestead2, "hourly tick");
			return;
		}
		homestead2?.EnsurePartyStaysAtAnchor("hourly tick");
		homestead2?.HourlyTickAmbassador();
		homestead2?.HourlyTickTavern();
	}

	private void CheckHomesteadDailyTick(MobileParty party)
	{
		Homestead homestead = Homestead.GetFor(party);
		if (homestead == null)
		{
			return;
		}
		if (ShouldDestroyBecausePartyInvalid(homestead))
		{
			DestroyInvalidHomesteadParty(homestead, "daily tick");
			return;
		}
		TryRepairUnavailableHomesteadLeader(homestead, "daily tick", notify: true);
		NormalizeGarrisonHeroCounts("daily tick");
		homestead.EnsurePartyStaysAtAnchor("daily tick");
		homestead.DailyTick();
		if (HasTavernKeeperBonusActive)
		{
			Hero? tavernKeeperHero = homestead.TavernKeeperHero;
			if (tavernKeeperHero != null && tavernKeeperHero.IsAlive && Clan.PlayerClan != null)
			{
				Clan.PlayerClan.Renown += 5f;
			}
		}
	}

	public bool IsCaravanRedirected(MobileParty party)
	{
		if (party != null)
		{
			return _activeCaravanVisits.ContainsKey(party);
		}
		return false;
	}

	private void CheckCaravanHourlyTick(MobileParty party)
	{
		HomesteadCaravanVisit value2;
		if (party.HasNavalNavigationCapability)
		{
			if (_activeCaravanVisits.TryGetValue(party, out HomesteadCaravanVisit value))
			{
				ReleaseCaravan(party, value);
			}
			TryPassiveCaravanTrade(party);
		}
		else if (_activeCaravanVisits.TryGetValue(party, out value2))
		{
			TickCaravanVisit(party, value2);
		}
		else
		{
			TryStartCaravanDetour(party);
		}
	}

	private void TryPassiveCaravanTrade(MobileParty party)
	{
		if (HomesteadMobileParties.Count == 0 || !party.IsActive || party.IsDisbanding || party.MapEvent != null)
		{
			return;
		}
		int num = (int)CampaignTime.Now.ToDays;
		if ((_caravanCooldowns.TryGetValue(party.StringId, out var value) && num < value) || (party.MapFaction != null && Hero.MainHero?.MapFaction != null && FactionManager.IsAtWarAgainstFaction(party.MapFaction, Hero.MainHero.MapFaction)))
		{
			return;
		}
		Homestead homestead = null;
		float num2 = 15f;
		Vec2 getPosition2D = party.GetPosition2D;
		foreach (KeyValuePair<MobileParty, Homestead> item in HomesteadMobileParties.ToList())
		{
			MobileParty key = item.Key;
			Homestead value2 = item.Value;
			if (!value2.IsRetiredOrDestroyed && key != null && key.IsActive && !key.IsDisbanding && value2.CaravanTradingEnabled && HomesteadHasTradeableGoods(value2))
			{
				float num3 = getPosition2D.Distance(key.GetPosition2D);
				if (num3 < num2)
				{
					num2 = num3;
					homestead = value2;
				}
			}
		}
		if (homestead != null)
		{
			ExecuteCaravanTrade(party, homestead);
			_caravanCooldowns[party.StringId] = num + 7;
			TraceLogger.Write("HomesteadBehavior", $"Passive (naval) trade: '{party.StringId}' traded with homestead '{homestead.Name}' in passing (dist={num2:F1}).");
		}
	}

	private void TryStartCaravanDetour(MobileParty party)
	{
		if (HomesteadMobileParties.Count == 0 || !party.IsActive || party.IsDisbanding || party.MapEvent != null)
		{
			return;
		}
		int num = (int)CampaignTime.Now.ToDays;
		if ((_caravanCooldowns.TryGetValue(party.StringId, out var value) && num < value) || (party.MapFaction != null && Hero.MainHero?.MapFaction != null && FactionManager.IsAtWarAgainstFaction(party.MapFaction, Hero.MainHero.MapFaction)))
		{
			return;
		}
		Homestead homestead = null;
		float num2 = 15f;
		Vec2 getPosition2D = party.GetPosition2D;
		foreach (KeyValuePair<MobileParty, Homestead> item in HomesteadMobileParties.ToList())
		{
			MobileParty key = item.Key;
			Homestead value2 = item.Value;
			if (!value2.IsRetiredOrDestroyed && key != null && key.IsActive && !key.IsDisbanding && value2.CaravanTradingEnabled && HomesteadHasTradeableGoods(value2))
			{
				float num3 = getPosition2D.Distance(key.GetPosition2D);
				if (num3 < num2)
				{
					num2 = num3;
					homestead = value2;
				}
			}
		}
		if (homestead != null)
		{
			_activeCaravanVisits[party] = new HomesteadCaravanVisit(homestead);
			TraceLogger.Write("HomesteadBehavior", $"Redirecting caravan '{party.StringId}' to homestead '{homestead.Name}' (dist={num2:F1}).");
		}
	}

	private void ApplyCaravanRedirectMovement(MobileParty party, HomesteadCaravanVisit visit)
	{
		if (!visit.Homestead.IsRetiredOrDestroyed && visit.Homestead.MobileParty != null)
		{
			if (visit.State == HomesteadCaravanState.Trading)
			{
				party.SetMoveModeHold();
			}
			else
			{
				party.SetMoveGoToPoint(new CampaignVec2(visit.Homestead.MobileParty.GetPosition2D, isOnLand: true), MobileParty.NavigationType.Default);
			}
		}
	}

	private void TickCaravanVisit(MobileParty party, HomesteadCaravanVisit visit)
	{
		if (!party.IsActive || party.IsDisbanding || visit.Homestead.IsRetiredOrDestroyed || visit.Homestead.MobileParty == null || !visit.Homestead.MobileParty.IsActive)
		{
			_activeCaravanVisits.Remove(party);
			TraceLogger.Write("HomesteadBehavior", "Dropped caravan visit for '" + party.StringId + "': party or homestead no longer valid.");
		}
		else if (party.MapEvent == null)
		{
			switch (visit.State)
			{
			case HomesteadCaravanState.Approaching:
				TickCaravanApproaching(party, visit);
				break;
			case HomesteadCaravanState.Trading:
				TickCaravanTrading(party, visit);
				break;
			}
		}
	}

	private void TickCaravanApproaching(MobileParty party, HomesteadCaravanVisit visit)
	{
		visit.HoursInState++;
		if (visit.HoursInState > 12)
		{
			TraceLogger.Write("HomesteadBehavior", $"Caravan '{party.StringId}' approach to '{visit.Homestead.Name}' timed out after {visit.HoursInState} hours; releasing.");
			_activeCaravanVisits.Remove(party);
			_caravanCooldowns[party.StringId] = (int)CampaignTime.Now.ToDays + 1;
			return;
		}
		if (party.GetPosition2D.Distance(visit.Homestead.MobileParty.GetPosition2D) <= 8f)
		{
			TraceLogger.Write("HomesteadBehavior", $"Caravan '{party.StringId}' arrived at homestead '{visit.Homestead.Name}'; executing trade.");
			ExecuteCaravanTrade(party, visit.Homestead);
			visit.State = HomesteadCaravanState.Trading;
			visit.HoursInState = 0;
			party.SetMoveModeHold();
			return;
		}
		if (party.CurrentSettlement != null)
		{
			try
			{
				LeaveSettlementAction.ApplyForParty(party);
				TraceLogger.Write("HomesteadBehavior", $"Ejected caravan '{party.StringId}' from settlement '{party.CurrentSettlement?.Name}' for homestead detour.");
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadBehavior", "LeaveSettlementAction failed for caravan '" + party.StringId + "': " + ex.Message);
			}
		}
		party.SetMoveGoToPoint(new CampaignVec2(visit.Homestead.MobileParty.GetPosition2D, isOnLand: true), MobileParty.NavigationType.Default);
	}

	private void TickCaravanTrading(MobileParty party, HomesteadCaravanVisit visit)
	{
		party.SetMoveModeHold();
		visit.HoursInState++;
		if (visit.HoursInState >= 4)
		{
			ReleaseCaravan(party, visit);
		}
	}

	private void ExecuteCaravanTrade(MobileParty party, Homestead homestead)
	{
		if (homestead.Stash == null)
		{
			return;
		}
		float marketPriceDiscount = homestead.MarketPriceDiscount;
		int num = 0;
		foreach (ItemRosterElement item6 in homestead.Stash.ToList())
		{
			ItemObject item = item6.EquipmentElement.Item;
			if (item == null)
			{
				continue;
			}
			int amount = item6.Amount;
			int num2 = -1;
			if (item.StringId == "dog")
			{
				num2 = 20;
			}
			else if (IsTradeableGood(item))
			{
				num2 = (item.IsFood ? GlobalSettings<MCMSettings>.Instance.TradeSellFoodMin : ((!IsBuildingMaterial(item, homestead)) ? GlobalSettings<MCMSettings>.Instance.TradeSellGeneralGoodsMin : GlobalSettings<MCMSettings>.Instance.TradeSellBuildingMaterialsMin));
			}
			if (num2 != -1)
			{
				int num3 = Math.Max(0, amount - num2);
				if (num3 > 0)
				{
					int num4 = ((marketPriceDiscount > 0f) ? ((int)Math.Round((float)item.Value * (1f + marketPriceDiscount))) : item.Value);
					int num5 = num3 * num4;
					num += num5;
					homestead.Stash.AddToCounts(item6.EquipmentElement, -num3);
					TraceLogger.Write("HomesteadBehavior", $"Caravan trade (sell): {num3}x '{item.StringId}' from '{homestead.Name}' at {num4}g (base {item.Value}g) = {num5}g.");
					Utils.PrintDebugMessage($"[Caravan sell] {num3}x {item.Name} @ {item.Value}g = {num5}g", 180f, 255f, 180f);
				}
			}
		}
		homestead.GoldStored += num;
		int num6 = 0;
		int num7 = 25 * Math.Max(1, homestead.Tier + 1);
		int num8 = Math.Max(0, homestead.GetStashCapacity() - homestead.GetStashTotalItemCount() - num7);
		foreach (ItemRosterElement item7 in party.ItemRoster.ToList())
		{
			if (num8 <= 0 || homestead.GoldStored - num6 <= 0)
			{
				break;
			}
			ItemObject item2 = item7.EquipmentElement.Item;
			if (item2 == null || item2.Value <= 0)
			{
				continue;
			}
			bool isFood = item2.IsFood;
			bool flag = IsBuildingMaterial(item2, homestead);
			if (!isFood && !flag)
			{
				continue;
			}
			int num9 = (item2.HasHorseComponent ? GlobalSettings<MCMSettings>.Instance.TradeBuyHorsesMax : ((!isFood) ? GlobalSettings<MCMSettings>.Instance.TradeBuyBuildingMaterialsMax : GlobalSettings<MCMSettings>.Instance.TradeBuyFoodMax));
			int itemNumber = homestead.Stash.GetItemNumber(item2);
			int num10 = num9 - itemNumber;
			if (num10 > 0)
			{
				int num11 = ((marketPriceDiscount > 0f) ? Math.Max(1, (int)Math.Round((float)item2.Value * (1f - marketPriceDiscount))) : item2.Value);
				int amount2 = item7.Amount;
				int val = (homestead.GoldStored - num6) / num11;
				int num12 = Math.Min(amount2, Math.Min(num8, Math.Min(val, num10)));
				if (num12 > 0)
				{
					int num13 = num12 * num11;
					num6 += num13;
					num8 -= num12;
					party.ItemRoster.AddToCounts(item7.EquipmentElement, -num12);
					homestead.Stash.AddToCounts(item7.EquipmentElement, num12);
					TraceLogger.Write("HomesteadBehavior", $"Caravan trade (buy): {num12}x '{item2.StringId}' for '{homestead.Name}' at {num11}g (base {item2.Value}g) = {num13}g.");
					Utils.PrintDebugMessage($"[Caravan buy] {num12}x {item2.Name} @ {item2.Value}g = {num13}g", 180f, 220f);
				}
			}
		}
		int tradeBuyHorsesMax = GlobalSettings<MCMSettings>.Instance.TradeBuyHorsesMax;
		if (tradeBuyHorsesMax > 0)
		{
			foreach (var horseUpgradeNeed in homestead.GetHorseUpgradeNeeds())
			{
				string item3 = horseUpgradeNeed.CategoryId;
				int item4 = horseUpgradeNeed.TroopCount;
				ItemCategory horseCategory = Campaign.Current.ObjectManager.GetObject<ItemCategory>(item3);
				if (horseCategory == null)
				{
					continue;
				}
				int num14 = homestead.CountStashItemsByCategory(horseCategory);
				int num15 = Math.Min(item4, tradeBuyHorsesMax) - num14;
				if (num15 <= 0)
				{
					continue;
				}
				foreach (ItemRosterElement item8 in (from e in party.ItemRoster
					where e.EquipmentElement.Item?.ItemCategory == horseCategory && e.Amount > 0
					orderby e.EquipmentElement.Item?.Value ?? int.MaxValue
					select e).ToList())
				{
					if (num15 <= 0 || num8 <= 0 || homestead.GoldStored - num6 <= 0)
					{
						break;
					}
					ItemObject item5 = item8.EquipmentElement.Item;
					int num16 = ((marketPriceDiscount > 0f) ? Math.Max(1, (int)Math.Round((float)item5.Value * (1f - marketPriceDiscount))) : item5.Value);
					int val2 = (homestead.GoldStored - num6) / Math.Max(1, num16);
					int num17 = Math.Min(num15, Math.Min(item8.Amount, Math.Min(val2, num8)));
					if (num17 > 0)
					{
						int num18 = num17 * num16;
						num6 += num18;
						num8 -= num17;
						num15 -= num17;
						party.ItemRoster.AddToCounts(item8.EquipmentElement, -num17);
						homestead.Stash.AddToCounts(item8.EquipmentElement, num17);
						TraceLogger.Write("HomesteadBehavior", $"Caravan trade (horse): {num17}x '{item5.StringId}' [{item3}] for '{homestead.Name}' at {num16}g (base {item5.Value}g) = {num18}g.");
						Utils.PrintDebugMessage($"[Caravan horses] {num17}x {item5.Name} [{item3}] @ {item5.Value}g = {num18}g", 200f, 200f);
					}
				}
			}
		}
		HashSet<string> hashSet = new HashSet<string>();
		Hero[] array = new Hero[5] { homestead.HoundMasterHero, homestead.MarketLadyHero, homestead.AmbassadorHero, homestead.ArmsMasterHero, homestead.TavernKeeperHero };
		foreach (Hero hero in array)
		{
			if (hero != null && hero.StringId != null)
			{
				hashSet.Add(hero.StringId);
			}
		}
		HashSet<string> hashSet2 = new HashSet<string>();
		List<Hero> list = new List<Hero>();
		foreach (TroopRosterElement item9 in homestead.Troops.GetTroopRoster())
		{
			if (item9.Character.IsHero)
			{
				Hero heroObject = item9.Character.HeroObject;
				if (heroObject != null && heroObject.IsAlive && !hashSet.Contains(heroObject.StringId) && hashSet2.Add(heroObject.StringId))
				{
					list.Add(heroObject);
				}
			}
		}
		int num20 = Math.Min(Math.Max(0, homestead.GoldStored - num6) / 2, 20000);
		foreach (Hero item10 in list)
		{
			if (num20 <= 0)
			{
				break;
			}
			int num21 = TryUpgradeHeroGearFromCaravan(party, item10, num20);
			num6 += num21;
			num20 -= num21;
		}
		homestead.GoldStored -= num6;
		bool flag2 = num > 0;
		bool flag3 = num6 > 0;
		if (flag2 || flag3)
		{
			TraceLogger.Write("HomesteadBehavior", $"Caravan '{party.StringId}' trade with '{homestead.Name}' complete: +{num}g earned, -{num6}g spent on supplies.");
			int num22 = num - num6;
			if (num22 > 0)
			{
				homestead.AddTradeXpToLeaderAndSupporters(num22);
			}
			string localizationString;
			string str;
			if (flag2 && flag3)
			{
				localizationString = "homestead_caravan_trade_both";
				str = "A caravan traded with {HOMESTEAD_NAME}: earned {GOLD_EARNED} gold from goods sold, spent {GOLD_SPENT} gold on supplies.";
			}
			else if (flag2)
			{
				localizationString = "homestead_caravan_trade_sell";
				str = "A caravan bought goods from {HOMESTEAD_NAME}, earning {GOLD_EARNED} gold.";
			}
			else
			{
				localizationString = "homestead_caravan_trade_buy";
				str = "A caravan sold supplies to {HOMESTEAD_NAME} for {GOLD_SPENT} gold.";
			}
			if (GlobalSettings<MCMSettings>.Instance.ShowCaravanTradeNotifications)
			{
				Utils.PrintLocalizedMessage(localizationString, str, 80f, 200f, 80f, ("HOMESTEAD_NAME", homestead.Name?.ToString() ?? "your homestead"), ("GOLD_EARNED", num.ToString("N0")), ("GOLD_SPENT", num6.ToString("N0")));
			}
		}
	}

	private static int TryUpgradeHeroGearFromCaravan(MobileParty caravan, Hero leader, int budget)
	{
		int num = 0;
		List<string> list = new List<string>();
		List<ItemRosterElement> items = caravan.ItemRoster.ToList();
		Dictionary<ItemObject, int> dictionary = new Dictionary<ItemObject, int>();
		(EquipmentIndex, ItemObject.ItemTypeEnum, string)[] array = new(EquipmentIndex, ItemObject.ItemTypeEnum, string)[5]
		{
			(EquipmentIndex.NumAllWeaponSlots, ItemObject.ItemTypeEnum.HeadArmor, "helm"),
			(EquipmentIndex.Cape, ItemObject.ItemTypeEnum.Cape, "shoulders"),
			(EquipmentIndex.Body, ItemObject.ItemTypeEnum.BodyArmor, "torso"),
			(EquipmentIndex.Gloves, ItemObject.ItemTypeEnum.HandArmor, "gloves"),
			(EquipmentIndex.Leg, ItemObject.ItemTypeEnum.LegArmor, "boots")
		};
		for (int i = 0; i < 2; i++)
		{
			Equipment equipment = ((i == 0) ? leader.BattleEquipment : leader.CivilianEquipment);
			string text = ((i == 0) ? "battle" : "civ");
			(EquipmentIndex, ItemObject.ItemTypeEnum, string)[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				var (index, reqType, text2) = array2[j];
				if (budget - num <= 0)
				{
					break;
				}
				ItemObject item = equipment[index].Item;
				ItemObject itemObject = FindBestUpgrade(items, dictionary, reqType, item?.Value ?? 0);
				if (itemObject == null)
				{
					continue;
				}
				int num2 = ((item != null) ? ((int)((float)item.Value * 0.5f)) : 0);
				int num3 = itemObject.Value - num2;
				if (num3 <= budget - num)
				{
					dictionary.TryGetValue(itemObject, out var value);
					dictionary[itemObject] = value + 1;
					caravan.ItemRoster.AddToCounts(new EquipmentElement(itemObject), -1);
					if (item != null)
					{
						caravan.ItemRoster.AddToCounts(new EquipmentElement(item), 1);
					}
					equipment[index] = new EquipmentElement(itemObject);
					num += Math.Max(0, num3);
					list.Add((item != null) ? $"{text2} ({text}): {item.Name} → {itemObject.Name}" : $"{text2} ({text}): {itemObject.Name}");
				}
			}
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
			{
				if (budget - num <= 0)
				{
					break;
				}
				ItemObject item2 = equipment[equipmentIndex].Item;
				if (item2 == null)
				{
					continue;
				}
				ItemObject itemObject2 = FindBestUpgrade(items, dictionary, item2.ItemType, item2.Value);
				if (itemObject2 != null)
				{
					int num4 = (int)((float)item2.Value * 0.5f);
					int num5 = itemObject2.Value - num4;
					if (num5 <= budget - num)
					{
						dictionary.TryGetValue(itemObject2, out var value2);
						dictionary[itemObject2] = value2 + 1;
						caravan.ItemRoster.AddToCounts(new EquipmentElement(itemObject2), -1);
						caravan.ItemRoster.AddToCounts(new EquipmentElement(item2), 1);
						equipment[equipmentIndex] = new EquipmentElement(itemObject2);
						num += Math.Max(0, num5);
						list.Add($"weapon{(int)(equipmentIndex + 1)} ({text}): {item2.Name} → {itemObject2.Name}");
					}
				}
			}
		}
		if (num > 0)
		{
			TraceLogger.Write("HomesteadBehavior", string.Format("Leader '{0}' gear upgraded from caravan: [{1}] (-{2}g).", leader.Name, string.Join(", ", list), num));
			if (GlobalSettings<MCMSettings>.Instance.ShowCaravanTradeNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_leader_gear_upgraded", "{LEADER_NAME} upgraded their gear from the passing caravan: {ITEMS} (-{GOLD}g)", 100f, 220f, 120f, ("LEADER_NAME", leader.Name?.ToString() ?? "?"), ("ITEMS", string.Join(", ", list)), ("GOLD", num.ToString("N0")));
			}
		}
		return num;
		static ItemObject? FindBestUpgrade(List<ItemRosterElement> list2, Dictionary<ItemObject, int> reserved, ItemObject.ItemTypeEnum itemTypeEnum, int currentValue)
		{
			ItemObject result = null;
			int num6 = currentValue;
			foreach (ItemRosterElement item4 in list2)
			{
				ItemObject item3 = item4.EquipmentElement.Item;
				if (item3 != null)
				{
					reserved.TryGetValue(item3, out var value3);
					if (item4.Amount - value3 > 0 && item3.ItemType == itemTypeEnum && item3.Value > num6)
					{
						result = item3;
						num6 = item3.Value;
					}
				}
			}
			return result;
		}
	}

	private void ReleaseCaravan(MobileParty party, HomesteadCaravanVisit visit)
	{
		_activeCaravanVisits.Remove(party);
		int num = (int)CampaignTime.Now.ToDays + 7;
		_caravanCooldowns[party.StringId] = num;
		TraceLogger.Write("HomesteadBehavior", $"Released caravan '{party.StringId}' from '{visit.Homestead.Name}'; cooldown until day {num}.");
	}

	private static string BuildAmbassadorReportText(Homestead homestead)
	{
		MobileParty mobileParty = homestead.MobileParty;
		if (mobileParty == null)
		{
			return new TextObject("{=homestead_ambassador_no_contacts}I have yet to establish contacts in the area, my lord.").ToString();
		}
		return BuildAmbassadorReportText(mobileParty.GetPosition2D);
	}

	private static string BuildAmbassadorReportText(Vec2 homePos)
	{
		Hero mainHero = Hero.MainHero;
		if (mainHero == null)
		{
			return new TextObject("{=homestead_ambassador_no_contacts}I have yet to establish contacts in the area, my lord.").ToString();
		}
		List<(string, string, int)> list = new List<(string, string, int)>();
		foreach (MobileParty mobileParty in Campaign.Current.MobileParties)
		{
			if (mobileParty == null || !mobileParty.IsActive || mobileParty.IsMainParty || mobileParty.GetPosition2D.Distance(homePos) > 20f)
			{
				continue;
			}
			Hero leaderHero = mobileParty.LeaderHero;
			if (leaderHero != null && leaderHero.IsAlive && leaderHero != mainHero && leaderHero.IsLord && leaderHero.Clan != Clan.PlayerClan)
			{
				int item = (int)leaderHero.GetRelationWithPlayer();
				string item2 = leaderHero.Clan?.Name?.ToString() ?? "";
				list.Add((leaderHero.Name?.ToString() ?? "?", item2, item));
			}
			if (mobileParty.PartyComponent is CaravanPartyComponent caravanPartyComponent)
			{
				Hero owner = caravanPartyComponent.Owner;
				if (owner != null && owner.IsAlive && owner != mainHero && owner.Clan != Clan.PlayerClan && !list.Any<(string, string, int)>(((string Name, string Context, int Relation) c) => c.Name == owner.Name?.ToString()))
				{
					int item3 = (int)owner.GetRelationWithPlayer();
					list.Add((owner.Name?.ToString() ?? "?", new TextObject("{=homestead_ambassador_caravan_owner}caravan owner").ToString(), item3));
				}
			}
		}
		foreach (Settlement settlement in Campaign.Current.Settlements)
		{
			if (settlement == null || settlement.GetPosition2D.Distance(homePos) > 20f)
			{
				continue;
			}
			foreach (Hero notable in settlement.Notables)
			{
				if (notable != null && notable.IsAlive && notable != mainHero && (notable.Clan == null || notable.Clan != Clan.PlayerClan))
				{
					int item4 = (int)notable.GetRelationWithPlayer();
					list.Add((notable.Name?.ToString() ?? "?", settlement.Name?.ToString() ?? "", item4));
				}
			}
		}
		if (list.Count == 0)
		{
			return new TextObject("{=homestead_ambassador_quiet_roads}The roads have been quiet, my lord. I have yet to make contact with any notable lords or settlement figures within reach of the homestead.").ToString();
		}
		list.Sort(((string Name, string Context, int Relation) a, (string Name, string Context, int Relation) b) => b.Relation.CompareTo(a.Relation));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(new TextObject("{=homestead_ambassador_assessed_standing}I have assessed our standing with nearby figures, my lord:").ToString());
		int num = 0;
		foreach (var (text, text2, num2) in list)
		{
			if (num >= 6)
			{
				break;
			}
			string text3 = AmbassadorRelationLabel(num2);
			string text4 = ((num2 >= 0) ? "+" : "");
			string text5 = (string.IsNullOrWhiteSpace(text2) ? "" : (", " + text2));
			stringBuilder.AppendLine($"  {text}{text5}: {text3} ({text4}{num2})");
			num++;
		}
		if (list.Count > 6)
		{
			TextObject textObject = new TextObject("{=homestead_ambassador_others_in_range}...and {COUNT} others within range.");
			textObject.SetTextVariable("COUNT", list.Count - 6);
			stringBuilder.Append(textObject.ToString());
		}
		return stringBuilder.ToString().TrimEnd(Array.Empty<char>());
	}

	private static string AmbassadorRelationLabel(int rel)
	{
		if (rel >= 50)
		{
			return new TextObject("{=homestead_ambassador_rel_50}Trusted ally").ToString();
		}
		if (rel >= 25)
		{
			return new TextObject("{=homestead_ambassador_rel_25}Friendly").ToString();
		}
		if (rel >= 10)
		{
			return new TextObject("{=homestead_ambassador_rel_10}Warm").ToString();
		}
		if (rel >= 0)
		{
			return new TextObject("{=homestead_ambassador_rel_0}Neutral").ToString();
		}
		if (rel >= -10)
		{
			return new TextObject("{=homestead_ambassador_rel_m10}Cool").ToString();
		}
		if (rel >= -25)
		{
			return new TextObject("{=homestead_ambassador_rel_m25}Unfriendly").ToString();
		}
		return new TextObject("{=homestead_ambassador_rel_m50}Hostile").ToString();
	}

	private static string BuildTradeRumorText(Homestead homestead)
	{
		if (homestead.MobileParty == null)
		{
			return "The traders speak little these days, my lord.";
		}
		string[] array = new string[10] { "grain", "butter", "cheese", "fish", "pottery", "tools", "wool", "cloth", "hides", "flax" };
		List<Settlement> list = (from s in Campaign.Current?.Settlements
			where s.IsTown && s.GetPosition2D.Distance(homestead.MobileParty.GetPosition2D) < 80f
			orderby s.GetPosition2D.Distance(homestead.MobileParty.GetPosition2D)
			select s).Take(6).ToList();
		if (list == null || list.Count == 0)
		{
			return "I haven't heard much from distant markets lately, my lord.";
		}
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		string[] array2 = array;
		foreach (string objectName in array2)
		{
			if (list2.Count + list3.Count >= 4)
			{
				break;
			}
			ItemObject itemObject = Game.Current?.ObjectManager?.GetObject<ItemObject>(objectName);
			if (itemObject == null || itemObject.Value <= 0)
			{
				continue;
			}
			foreach (Settlement item in list)
			{
				if (list2.Count + list3.Count >= 4)
				{
					break;
				}
				try
				{
					int itemPrice = item.Town.GetItemPrice(itemObject);
					float num2 = (float)itemPrice / (float)itemObject.Value;
					if (num2 >= 1.45f)
					{
						list2.Add($"{itemObject.Name} is fetching {itemPrice}g in {item.Name} — well above the usual price");
					}
					else if (num2 <= 0.65f)
					{
						list3.Add($"{itemObject.Name} is going for only {itemPrice}g in {item.Name} right now");
					}
				}
				catch
				{
				}
			}
		}
		List<string> list4 = new List<string>(list2.Concat(list3));
		if (list4.Count == 0)
		{
			return "The markets seem steady at the moment, my lord. No great bargains or shortages to speak of.";
		}
		for (int num3 = list4.Count - 1; num3 > 0; num3--)
		{
			int num4 = MBRandom.RandomInt(num3 + 1);
			List<string> list5 = list4;
			int num = num3;
			int index = num4;
			string value = list4[num4];
			string value2 = list4[num3];
			list5[num] = value;
			list4[index] = value2;
		}
		return "Word from the traders: " + string.Join(". Also, ", list4.Take(3)) + ".";
	}

	private bool HomesteadHasTradeableGoods(Homestead homestead)
	{
		if (homestead?.Stash == null)
		{
			return false;
		}
		foreach (ItemRosterElement item2 in homestead.Stash)
		{
			ItemObject item = item2.EquipmentElement.Item;
			if (item != null && IsTradeableGood(item))
			{
				int num = (item.IsFood ? GlobalSettings<MCMSettings>.Instance.TradeSellFoodMin : ((!IsBuildingMaterial(item, homestead)) ? GlobalSettings<MCMSettings>.Instance.TradeSellGeneralGoodsMin : GlobalSettings<MCMSettings>.Instance.TradeSellBuildingMaterialsMin));
				if (item2.Amount > num)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsTradeableGood(ItemObject item)
	{
		if (item != null && !item.HasArmorComponent && !item.HasWeaponComponent && !item.HasHorseComponent)
		{
			return item.Value > 0;
		}
		return false;
	}

	internal static HashSet<string> GetBuildingMaterialIds(Homestead homestead)
	{
		int tier = homestead.Tier;
		if (_buildingMaterialIdsByTier.TryGetValue(tier, out HashSet<string> value))
		{
			return value;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (HomesteadScenePlaceable item in HomesteadScenePlaceable.GetTierGroup(tier))
			{
				foreach (string key in item.ItemRequirements.Keys)
				{
					string[] array = key.Split(new char[1] { '|' });
					foreach (string text in array)
					{
						hashSet.Add(text.Trim());
					}
				}
			}
			TraceLogger.Write("HomesteadBehavior", string.Format("Building material IDs for tier {0}: [{1}]", tier, string.Join(", ", hashSet)));
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadBehavior", $"Failed to derive building material IDs for tier {tier}: {arg}");
		}
		_buildingMaterialIdsByTier[tier] = hashSet;
		return hashSet;
	}

	internal static bool IsBuildingMaterial(ItemObject item, Homestead homestead)
	{
		if (item != null)
		{
			return GetBuildingMaterialIds(homestead).Contains(item.StringId);
		}
		return false;
	}

	private void EnsureAllHomesteadsStayAnchored(string reason)
	{
		RemoveInvalidHomesteadParties(reason);
		RepairUnavailableHomesteadLeaders(reason);
		foreach (Homestead value in HomesteadMobileParties.Values)
		{
			value.EnsurePartyStaysAtAnchor(reason);
		}
	}

	private void PayForCurrentHomesteadUpgrade()
	{
		Homestead homestead = CurrentHomestead;
		if (homestead == null)
		{
			return;
		}
		int currentPaidUpgradeCost = homestead.GetCurrentPaidUpgradeCost();
		if (currentPaidUpgradeCost <= 0 || homestead.Tier >= 4)
		{
			return;
		}
		if (Hero.MainHero.Gold < currentPaidUpgradeCost)
		{
			Utils.PrintLocalizedMessage("homestead_paid_upgrade_not_enough_gold", "You need {GOLD_COST} gold to upgrade this homestead.", 255f, 80f, 80f, ("GOLD_COST", currentPaidUpgradeCost.ToString("N0")));
		}
		else if (homestead.Tier == 1 && !homestead.Tier1ApprovalGranted)
		{
			Hero.MainHero.ChangeHeroGold(-currentPaidUpgradeCost);
			homestead.TierProgress = 1f;
			homestead.Tier1GrowthReady = true;
			Settlement settlement = homestead.FindNearestVillage();
			Hero hero = Homestead.FindHeadmanOfVillage(settlement);
			Utils.PrintLocalizedMessage("homestead_headman_trust_paid", "Your homestead has grown enough to expand, but doing so encroaches on the village of {VILLAGE}. You must first earn the blessing of its headman, {HEADMAN}. Speak with your homestead leader to begin.", 255f, 210f, 80f, ("VILLAGE", settlement?.Name?.ToString() ?? "the nearby village"), ("HEADMAN", hero?.Name?.ToString() ?? "the local headman"));
			GameMenu.SwitchToMenu("homestead_menu_manage_main");
		}
		else if (homestead.Tier == 2 && !homestead.Tier2ApprovalGranted)
		{
			Hero.MainHero.ChangeHeroGold(-currentPaidUpgradeCost);
			homestead.TierProgress = 1f;
			homestead.Tier2GrowthReady = true;
			Settlement settlement2 = homestead.FindNearestTown();
			Clan clan = settlement2?.OwnerClan;
			Utils.PrintLocalizedMessage("homestead_land_patent_paid", "Your homestead is ready to expand, but a holding this large needs a land patent from {CLAN}, who rule {TOWN}. Speak with your homestead leader to begin.", 255f, 210f, 80f, ("CLAN", clan?.Name?.ToString() ?? "the local ruling house"), ("TOWN", settlement2?.Name?.ToString() ?? "the nearest town"));
			GameMenu.SwitchToMenu("homestead_menu_manage_main");
		}
		else if (homestead.Tier == 3)
		{
			Hero.MainHero.ChangeHeroGold(-currentPaidUpgradeCost);
			homestead.TierProgress = 1f;
			if (!homestead.SettlementUpgradeReady)
			{
				homestead.SettlementUpgradeReady = true;
			}
			homestead.MobileParty?.Party?.SetVisualAsDirty();
			Homestead.SetGameTextsForMenus();
			Utils.PrintLocalizedMessage("homestead_paid_upgrade_charter_ready", "Your homestead is fully expanded and ready to seek a settlement charter. Speak with your homestead leader to begin.", 80f, 255f, 80f);
			GameMenu.SwitchToMenu("homestead_menu_manage_main");
		}
		else
		{
			Hero.MainHero.ChangeHeroGold(-currentPaidUpgradeCost);
			homestead.Tier++;
			homestead.TierProgress = 0f;
			homestead.MobileParty?.Party?.SetVisualAsDirty();
			Homestead.SetGameTextsForMenus();
			Utils.PrintLocalizedMessage("homestead_paid_upgrade_success", "Your homestead has been upgraded to tier {TIER_LEVEL}.", 80f, 255f, 80f, ("TIER_LEVEL", homestead.Tier.ToString()));
			GameMenu.SwitchToMenu("homestead_menu_manage_main");
		}
	}

	private Hero? GetFallbackLeader(Homestead homestead)
	{
		if (PartyScreenHelper.IsHomesteadPartyScreenOpen)
		{
			return null;
		}
		if (homestead?.MobileParty?.MemberRoster != null)
		{
			TroopRoster memberRoster = homestead.MobileParty.MemberRoster;
			for (int i = 0; i < memberRoster.Count; i++)
			{
				Hero heroObject = memberRoster.GetElementCopyAtIndex(i).Character.HeroObject;
				if (IsValidHomesteadLeader(heroObject))
				{
					return heroObject;
				}
			}
		}
		if (MobileParty.MainParty?.MemberRoster != null)
		{
			TroopRoster memberRoster2 = MobileParty.MainParty.MemberRoster;
			for (int j = 0; j < memberRoster2.Count; j++)
			{
				Hero heroObject2 = memberRoster2.GetElementCopyAtIndex(j).Character.HeroObject;
				if (IsValidHomesteadLeader(heroObject2))
				{
					return heroObject2;
				}
			}
		}
		return null;
	}

	private static bool IsValidHomesteadLeader(Hero? hero)
	{
		if (hero != null && !hero.IsHumanPlayerCharacter && hero.IsAlive)
		{
			return !hero.IsPrisoner;
		}
		return false;
	}

	private void PackUpCurrentHomestead()
	{
		Homestead homestead = CurrentHomestead;
		if (homestead == null)
		{
			TraceLogger.Write("HomesteadBehavior", "Pack-up requested but CurrentHomestead was already null.");
			return;
		}
		MobileParty mobileParty = homestead.MobileParty;
		Hero leader = homestead.Leader;
		TraceLogger.Write("HomesteadBehavior", string.Format("Packing up homestead '{0}' party='{1}' active={2} disbanding={3} leader='{4}'", homestead.Name, mobileParty?.StringId ?? "null", mobileParty?.IsActive.ToString() ?? "n/a", mobileParty?.IsDisbanding.ToString() ?? "n/a", leader?.StringId ?? "null"));
		CurrentHomestead = null;
		homestead.MarkRetiredOrDestroyed("pack up");
		homestead.RetireNotablesAndResidents();
		CleanupPatrolForHomestead(homestead, returnTroopsToMainParty: true);
		if (mobileParty != null)
		{
			HomesteadMobileParties.Remove(mobileParty);
		}
		else
		{
			MobileParty key = HomesteadMobileParties.FirstOrDefault<KeyValuePair<MobileParty, Homestead>>((KeyValuePair<MobileParty, Homestead> x) => x.Value == homestead).Key;
			if (key != null)
			{
				HomesteadMobileParties.Remove(key);
			}
		}
		try
		{
			Hero.MainHero.ChangeHeroGold(homestead.GoldStored);
			MobileParty.MainParty.ItemRoster.Add(homestead.Stash);
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadBehavior", $"Failed transferring homestead stash/gold while packing up '{homestead.Name}': {arg}");
		}
		try
		{
			if (leader != null && leader.PartyBelongedTo != MobileParty.MainParty)
			{
				AddHeroToPartyAction.Apply(leader, MobileParty.MainParty);
			}
		}
		catch (Exception arg2)
		{
			TraceLogger.Write("HomesteadBehavior", $"Failed returning homestead leader while packing up '{homestead.Name}': {arg2}");
		}
		try
		{
			if (mobileParty != MobileParty.MainParty)
			{
				MobileParty.MainParty.MemberRoster.Add(homestead.Troops);
			}
		}
		catch (Exception arg3)
		{
			TraceLogger.Write("HomesteadBehavior", $"Failed transferring homestead troops while packing up '{homestead.Name}': {arg3}");
		}
		try
		{
			if (mobileParty != null && mobileParty.IsActive && !mobileParty.IsDisbanding)
			{
				DestroyPartyAction.Apply(null, mobileParty);
			}
		}
		catch (Exception arg4)
		{
			TraceLogger.Write("HomesteadBehavior", $"Failed destroying homestead party while packing up '{homestead.Name}': {arg4}");
		}
	}

	public string ConvertHomesteadToSettlement(Homestead homestead, HomesteadSettlementPlacementMapView.Result result)
	{
		if (homestead == null)
		{
			return "No homestead to convert.";
		}
		List<Hero> apprentices = homestead.TakeGraduatedApprenticesForConversion();
		Settlement castle;
		List<(Settlement, Hero)> villageHeadmen;
		Settlement settlement = HomesteadSettlementBuilder.CreatePlacedSettlements(result, apprentices, out castle, out villageHeadmen);
		if (settlement == null)
		{
			return "Conversion failed while founding the settlements — see HomesteadsReloaded.trace.log.";
		}
		int num = homestead.ConvertNotablesToTown(settlement, castle);
		AssignVillageHeadmen(villageHeadmen);
		PopulateVillageLandowners(villageHeadmen);
		AssignTownLeaderAndClan(homestead, settlement);
		TransferGoodsAndGoldToTown(homestead, settlement);
		try
		{
			if (settlement.Town != null && settlement.Town.GarrisonParty == null)
			{
				settlement.AddGarrisonParty();
				settlement.SetGarrisonWagePaymentLimit(Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "ConvertHomestead: AddGarrisonParty failed: " + ex.Message);
		}
		try
		{
			if (castle?.Town != null && castle.Town.GarrisonParty == null)
			{
				castle.AddGarrisonParty();
				castle.SetGarrisonWagePaymentLimit(Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadBehavior", "ConvertHomestead: castle AddGarrisonParty failed: " + ex2.Message);
		}
		TryRetireHomesteadForSettlementConversion(homestead, HomesteadConversionTroopDestination.TargetGarrison, settlement, castle, out string failReason);
		if (!string.IsNullOrEmpty(failReason))
		{
			TraceLogger.Write("HomesteadBehavior", "ConvertHomestead: retire reported '" + failReason + "'.");
		}
		if (CurrentHomestead == homestead)
		{
			CurrentHomestead = null;
		}
		HomesteadSettlementBuilder.RestoreNotablesToSettlements();
		if (settlement.Town != null)
		{
			HomesteadSettlementBuilder.SeedWorkshops(settlement.Town);
		}
		if (settlement.Town != null)
		{
			HomesteadSettlementBuilder.AssignAlleyOwners(settlement);
		}
		_pendingPostConversionSaveReload = true;
		_postConversionSaveReloadDelayTicks = 10;
		RaiseHomesteadConvertedToSettlement(homestead, settlement, castle);
		TraceLogger.Write("HomesteadBehavior", $"ConvertHomesteadToSettlement: '{homestead.Name}' → town '{settlement.StringId}', {num} notable(s) moved.");
		return $"\"{settlement.Name}\" has risen from your homestead — town, castle and villages founded, with " + $"{num} notable(s), garrison and goods moved in. Saving and reloading to finalize it...";
	}

	private void AssignVillageHeadmen(List<(Settlement Village, Hero Headman)> villageHeadmen)
	{
		if (villageHeadmen == null)
		{
			return;
		}
		foreach (var (settlement, hero) in villageHeadmen)
		{
			if (settlement == null || hero == null || !hero.IsAlive)
			{
				continue;
			}
			try
			{
				if (hero.PartyBelongedTo?.MemberRoster != null && hero.CharacterObject != null)
				{
					hero.PartyBelongedTo.MemberRoster.RemoveTroop(hero.CharacterObject);
				}
				RemoveCompanionAction.ApplyByFire(Clan.PlayerClan, hero);
				hero.SetNewOccupation(Occupation.Headman);
				Settlement bornSettlement = HomesteadSettlementBuilder.FindUnrelatedVanillaVillage(hero.CharacterObject?.Culture) ?? settlement;
				Hero hero2 = HeroCreator.CreateSpecialHero(hero.CharacterObject, bornSettlement, null, null, 30);
				hero2.Clan?.SetLeader(hero);
				KillCharacterAction.ApplyByRemove(hero2);
				hero.ChangeState(Hero.CharacterStates.Active);
				EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
				hero.BornSettlement = settlement;
				hero.UpdateHomeSettlement();
				try
				{
					hero.SetPersonalRelation(Hero.MainHero, 75);
				}
				catch
				{
				}
				try
				{
					string text = hero.FirstName?.ToString();
					if (string.IsNullOrWhiteSpace(text))
					{
						text = hero.Name?.ToString() ?? "the Founder";
					}
					TextObject textObject = new TextObject("{=homestead_headman_founder_title}{BASE_NAME} the Founder of {VILLAGE_NAME}").SetTextVariable("BASE_NAME", text).SetTextVariable("VILLAGE_NAME", settlement.Name);
					hero.SetName(textObject, new TextObject(text));
					Homestead.PatchCharacterObjectName(hero.CharacterObject, textObject);
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadBehavior", "AssignVillageHeadmen: rename to Founder failed for '" + hero?.StringId + "': " + ex.Message);
				}
				TraceLogger.Write("HomesteadBehavior", $"AssignVillageHeadmen: '{hero.Name}' → headman of '{settlement.Name}'.");
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("HomesteadBehavior", "AssignVillageHeadmen: failed for '" + hero?.StringId + "': " + ex2.Message);
			}
		}
	}

	private void AssignTownLeaderAndClan(Homestead homestead, Settlement town)
	{
		if (town?.Town == null)
		{
			return;
		}
		Hero leader = homestead.Leader;
		try
		{
			if (leader != null && leader.IsAlive && !leader.IsHumanPlayerCharacter && leader.Clan == Clan.PlayerClan)
			{
				if (leader.PartyBelongedTo?.MemberRoster != null && leader.CharacterObject != null)
				{
					leader.PartyBelongedTo.MemberRoster.RemoveTroop(leader.CharacterObject);
				}
				EnterSettlementAction.ApplyForCharacterOnly(leader, town);
				leader.BornSettlement = town;
				ChangeGovernorAction.Apply(town.Town, leader);
				TraceLogger.Write("HomesteadBehavior", $"AssignTownLeaderAndClan: '{leader.Name}' → governor of '{town.Name}'.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "AssignTownLeaderAndClan leader failed: " + ex.Message);
		}
		if (homestead.Troops == null)
		{
			return;
		}
		foreach (FlattenedTroopRosterElement item in homestead.Troops.ToFlattenedRoster().ToList())
		{
			Hero hero = item.Troop?.HeroObject;
			if (hero != null && hero.IsAlive && hero != leader && !hero.IsHumanPlayerCharacter && hero.Clan == Clan.PlayerClan)
			{
				try
				{
					homestead.Troops.RemoveTroop(hero.CharacterObject);
					hero.BornSettlement = town;
					EnterSettlementAction.ApplyForCharacterOnly(hero, town);
					TraceLogger.Write("HomesteadBehavior", $"AssignTownLeaderAndClan: '{hero.Name}' settled in '{town.Name}'.");
				}
				catch (Exception ex2)
				{
					TraceLogger.Write("HomesteadBehavior", "AssignTownLeaderAndClan member '" + hero?.StringId + "' failed: " + ex2.Message);
				}
			}
		}
	}

	private void PopulateVillageLandowners(List<(Settlement Village, Hero Headman)> villageHeadmen)
	{
		if (villageHeadmen == null)
		{
			return;
		}
		foreach (var (settlement, hero) in villageHeadmen)
		{
			HomesteadSettlementBuilder.PopulateVillageLandowners(settlement);
			HomesteadSettlementBuilder.ApplyFemaleVillageNotableCompat(settlement);
			if (settlement != null && hero != null && hero.IsAlive)
			{
				try
				{
					EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadBehavior", "PopulateVillageLandowners: re-entering headman '" + hero.StringId + "' failed: " + ex.Message);
				}
			}
		}
	}

	private void TransferGoodsAndGoldToTown(Homestead homestead, Settlement town)
	{
		if (town?.Town == null)
		{
			return;
		}
		try
		{
			if (homestead.Stash != null && town.ItemRoster != null)
			{
				foreach (ItemRosterElement item in homestead.Stash.ToList())
				{
					if (item.EquipmentElement.Item != null)
					{
						town.ItemRoster.AddToCounts(item.EquipmentElement, item.Amount);
					}
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "ConvertHomestead: goods transfer failed: " + ex.Message);
		}
		try
		{
			if (homestead.GoldStored > 0)
			{
				town.Town.ChangeGold(homestead.GoldStored);
				homestead.GoldStored = 0;
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadBehavior", "ConvertHomestead: gold transfer failed: " + ex2.Message);
		}
		try
		{
			town.Town.FoodStocks = town.Town.FoodStocksUpperLimit();
		}
		catch (Exception ex3)
		{
			TraceLogger.Write("HomesteadBehavior", "ConvertHomestead: food top-up failed: " + ex3.Message);
		}
	}

	public bool TryRetireHomesteadForSettlementConversion(Homestead? homestead, HomesteadConversionTroopDestination troopDestination, Settlement? targetSettlement, Settlement? patrolTargetSettlement, out string failReason)
	{
		failReason = "";
		if (homestead == null)
		{
			failReason = "No active homestead was found.";
			return false;
		}
		MobileParty mobileParty = homestead.MobileParty;
		MobileParty mobileParty2 = ((troopDestination != HomesteadConversionTroopDestination.TargetGarrison) ? MobileParty.MainParty : targetSettlement?.Town?.GarrisonParty);
		if (mobileParty2 == null)
		{
			failReason = "No valid troop destination was found for the converted homestead.";
			return false;
		}
		TraceLogger.Write("HomesteadBehavior", string.Format("Retiring homestead '{0}' for settlement conversion; party='{1}' troopDestination='{2}' target='{3}'", homestead.Name, mobileParty?.StringId ?? "null", troopDestination, targetSettlement?.StringId ?? "null"));
		CurrentHomestead = null;
		homestead.MarkRetiredOrDestroyed("settlement conversion");
		homestead.RetireNotablesAndResidents();
		try
		{
			int num = TransferPatrolTroopsToGarrisonOnConversion(homestead, patrolTargetSettlement, targetSettlement);
			if (num > 0)
			{
				TraceLogger.Write("HomesteadBehavior", $"Transferred {num} patrol troop(s) from converted homestead '{homestead.Name}' to the castle garrison.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", $"Failed transferring patrol troops while retiring '{homestead.Name}': {ex.Message}");
		}
		CleanupPatrolForHomestead(homestead, returnTroopsToMainParty: false);
		UnregisterHomesteadParty(homestead);
		try
		{
			ReturnHomesteadHeroesToPlayerParty(homestead);
			int num2 = TransferRegularTroops(homestead.Troops, mobileParty2.MemberRoster);
			TraceLogger.Write("HomesteadBehavior", $"Transferred {num2} regular troops from converted homestead '{homestead.Name}' to '{mobileParty2.StringId}'.");
		}
		catch (Exception ex2)
		{
			failReason = "Failed to transfer homestead troops: " + ex2.Message;
			TraceLogger.Write("HomesteadBehavior", $"Failed transferring troops while retiring '{homestead.Name}': {ex2}");
			return false;
		}
		try
		{
			MobileParty.MainParty.PrisonRoster.Add(homestead.Prisoners);
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadBehavior", $"Failed transferring prisoners while retiring '{homestead.Name}': {arg}");
		}
		try
		{
			if (mobileParty != null && mobileParty.IsActive && !mobileParty.IsDisbanding)
			{
				DestroyPartyAction.Apply(null, mobileParty);
			}
		}
		catch (Exception ex3)
		{
			failReason = "Failed to destroy the consumed homestead party: " + ex3.Message;
			TraceLogger.Write("HomesteadBehavior", $"Failed destroying homestead party while retiring '{homestead.Name}': {ex3}");
			return false;
		}
		return true;
	}

	private void CleanupPatrolForHomestead(Homestead homestead, bool returnTroopsToMainParty)
	{
		MobileParty patrolParty = homestead.PatrolParty;
		if (patrolParty == null)
		{
			return;
		}
		if (returnTroopsToMainParty)
		{
			homestead.DisbandPatrolToGarrisonOrPlayerParty();
			return;
		}
		PatrolMobileParties.Remove(patrolParty);
		try
		{
			if (patrolParty.IsActive && !patrolParty.IsDisbanding)
			{
				DestroyPartyAction.Apply(null, patrolParty);
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadBehavior", $"Failed destroying patrol party for '{homestead.Name}': {arg}");
		}
	}

	private void EnsureMasteryTitles()
	{
		if (HomesteadMobileParties == null)
		{
			return;
		}
		foreach (Homestead value in HomesteadMobileParties.Values)
		{
			if (value == null)
			{
				continue;
			}
			if (HasHoundmasterKnockdownUnlocked && value.HoundMasterHero != null)
			{
				string text = value.HoundMasterHero.Name.ToString();
				if (!text.Contains("Grand Houndmaster"))
				{
					string text2 = value.HoundMasterHero.FirstName?.ToString() ?? text;
					value.HoundMasterHero.SetName(new TextObject("{=homestead_grand_houndmaster}Grand Houndmaster {BASE_NAME}").SetTextVariable("BASE_NAME", text2), new TextObject(text2));
				}
			}
			if (HasAmbassadorTactfulIntroductionUnlocked && value.AmbassadorHero != null)
			{
				string text3 = value.AmbassadorHero.Name.ToString();
				if (!text3.Contains("Lord Ambassador"))
				{
					string text4 = value.AmbassadorHero.FirstName?.ToString() ?? text3;
					value.AmbassadorHero.SetName(new TextObject("{=homestead_lord_ambassador}Lord Ambassador {BASE_NAME}").SetTextVariable("BASE_NAME", text4), new TextObject(text4));
				}
			}
			if (HasMarketLadyTradeDiscountUnlocked && value.MarketLadyHero != null)
			{
				string text5 = value.MarketLadyHero.Name.ToString();
				if (!text5.Contains("Trade Guru"))
				{
					string text6 = value.MarketLadyHero.FirstName?.ToString() ?? text5;
					value.MarketLadyHero.SetName(new TextObject("{=homestead_trade_guru}Trade Guru {BASE_NAME}").SetTextVariable("BASE_NAME", text6), new TextObject(text6));
				}
			}
			if (HasArmsMasterMasteryUnlocked && value.ArmsMasterHero != null)
			{
				string text7 = value.ArmsMasterHero.Name.ToString();
				if (!text7.Contains("Drill Sergeant"))
				{
					string text8 = value.ArmsMasterHero.FirstName?.ToString() ?? text7;
					value.ArmsMasterHero.SetName(new TextObject("{=homestead_drill_sergeant}Drill Sergeant {BASE_NAME}").SetTextVariable("BASE_NAME", text8), new TextObject(text8));
				}
			}
			if (value.StableMasterMasteryUnlocked)
			{
				HasStableMasterMasteryUnlocked = true;
			}
			if (HasStableMasterMasteryUnlocked && value.StableMasterHero != null)
			{
				string text9 = value.StableMasterHero.Name.ToString();
				if (!text9.Contains("Grand Stablemistress"))
				{
					string text10 = value.StableMasterHero.FirstName?.ToString() ?? text9;
					value.StableMasterHero.SetName(new TextObject("{=homestead_grand_stablemistress}Grand Stablemistress {BASE_NAME}").SetTextVariable("BASE_NAME", text10), new TextObject(text10));
				}
			}
			if (HasMasterSmithUpgradeUnlocked && value.MasterSmithHero != null)
			{
				string text11 = value.MasterSmithHero.Name.ToString();
				if (!text11.Contains("Grandmaster Smith"))
				{
					string text12 = value.MasterSmithHero.FirstName?.ToString() ?? text11;
					value.MasterSmithHero.SetName(new TextObject("{=homestead_grandmaster_smith}Grandmaster Smith {BASE_NAME}").SetTextVariable("BASE_NAME", text12), new TextObject(text12));
				}
			}
			if (HasTavernKeeperBonusActive && value.TavernKeeperHero != null)
			{
				string text13 = value.TavernKeeperHero.Name.ToString();
				if (!text13.Contains("World Famous"))
				{
					string text14 = value.TavernKeeperHero.FirstName?.ToString() ?? text13;
					value.TavernKeeperHero.SetName(new TextObject("{=homestead_world_famous}{BASE_NAME}, World Famous Innkeeper of {HOMESTEAD_NAME}").SetTextVariable("BASE_NAME", text14).SetTextVariable("HOMESTEAD_NAME", value.Name), new TextObject(text14));
				}
			}
		}
	}

	private void UnregisterHomesteadParty(Homestead homestead)
	{
		MobileParty mobileParty = homestead.MobileParty;
		if (mobileParty != null)
		{
			HomesteadMobileParties.Remove(mobileParty);
			return;
		}
		MobileParty key = HomesteadMobileParties.FirstOrDefault<KeyValuePair<MobileParty, Homestead>>((KeyValuePair<MobileParty, Homestead> x) => x.Value == homestead).Key;
		if (key != null)
		{
			HomesteadMobileParties.Remove(key);
		}
	}

	private static void ReturnHomesteadHeroesToPlayerParty(Homestead homestead)
	{
		foreach (FlattenedTroopRosterElement item in homestead.Troops.ToFlattenedRoster())
		{
			Hero heroObject = item.Troop.HeroObject;
			if (heroObject != null && !heroObject.IsHumanPlayerCharacter && heroObject.IsAlive && heroObject.PartyBelongedTo != MobileParty.MainParty)
			{
				AddHeroToPartyAction.Apply(heroObject, MobileParty.MainParty);
			}
		}
	}

	private static int TransferRegularTroops(TroopRoster source, TroopRoster target)
	{
		int num = 0;
		foreach (FlattenedTroopRosterElement item in source.ToFlattenedRoster())
		{
			if (item.Troop != null && !item.Troop.IsHero)
			{
				target.AddToCounts(item.Troop, 1);
				num++;
			}
		}
		return num;
	}

	private static int TransferPatrolTroopsToGarrisonOnConversion(Homestead homestead, Settlement? castle, Settlement? town)
	{
		MobileParty patrolParty = homestead.PatrolParty;
		if (patrolParty?.MemberRoster == null)
		{
			return 0;
		}
		MobileParty mobileParty = castle?.Town?.GarrisonParty ?? town?.Town?.GarrisonParty;
		if (mobileParty == null)
		{
			return 0;
		}
		return TransferRegularTroops(patrolParty.MemberRoster, mobileParty.MemberRoster);
	}

	private void OnHsrSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		try
		{
			string stringId = settlement.StringId;
			if (stringId == null || !stringId.StartsWith("hsr_settlement_") || party?.LeaderHero != Hero.MainHero)
			{
				return;
			}
			LocationComplex locationComplex = settlement.LocationComplex;
			LocationComplex current = LocationComplex.Current;
			bool flag = settlement == MBObjectManager.Instance.GetObject<Settlement>(settlement.StringId);
			TraceLogger.Write("HomesteadBehavior", $"[ENTER] '{settlement.Name}' IsFort={settlement.IsFortification} IsTown={settlement.IsTown} " + $"IsCastle={settlement.IsCastle} Town={settlement.Town != null} " + $"LC={locationComplex != null} LCCurrent={current != null} LCMatch={locationComplex != null && current != null && locationComplex == current} " + $"HWP={settlement.HeroesWithoutParty.Count} Notables={settlement.Notables.Count} " + $"objId={RuntimeHelpers.GetHashCode(settlement)} sameAsRegistry={flag}");
			if (!flag || settlement.HeroesWithoutParty.Count == 0)
			{
				TraceLogger.Write("HomesteadBehavior", $"[ENTER] '{settlement.Name}' looks empty or stale (sameAsRegistry={flag}, HWP={settlement.HeroesWithoutParty.Count}) — re-running notable restoration against this instance.");
				HomesteadSettlementBuilder.RestoreNotablesToSettlements(new List<Settlement> { settlement });
			}
			foreach (Hero item in settlement.HeroesWithoutParty)
			{
				TraceLogger.Write("HomesteadBehavior", $"[ENTER] HWP '{item.Name}' ({item.Occupation}) currSett='{item.CurrentSettlement?.Name}' " + $"match={item.CurrentSettlement == settlement} isNotable={item.IsNotable} " + $"isLord={item.IsLord} isPlayerClan={item.Clan == Clan.PlayerClan} workshops={item.OwnedWorkshops.Count}");
			}
			foreach (Hero aliveLord in Clan.PlayerClan.AliveLords)
			{
				if (aliveLord != Hero.MainHero)
				{
					TraceLogger.Write("HomesteadBehavior", $"[ENTER] LORD '{aliveLord.Name}' ({aliveLord.Occupation}) currSett='{aliveLord.CurrentSettlement?.Name}' " + $"match={aliveLord.CurrentSettlement == settlement} isLord={aliveLord.IsLord} isPartyLeader={aliveLord.IsPartyLeader}");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "OnHsrSettlementEntered threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		foreach (Homestead item in HomesteadMobileParties.Values.ToList())
		{
			if (item.Leader == prisoner)
			{
				TryRepairUnavailableHomesteadLeader(item, "leader captured", notify: true);
				break;
			}
		}
	}

	private void RepairUnavailableHomesteadLeaders(string reason)
	{
		foreach (Homestead item in HomesteadMobileParties.Values.ToList())
		{
			TryRepairUnavailableHomesteadLeader(item, reason, notify: true);
		}
	}

	private void RemoveInvalidHomesteadParties(string reason)
	{
		foreach (Homestead item in HomesteadMobileParties.Values.ToList())
		{
			if (ShouldDestroyBecausePartyInvalid(item))
			{
				DestroyInvalidHomesteadParty(item, reason);
			}
		}
	}

	private static bool ShouldDestroyBecausePartyInvalid(Homestead homestead)
	{
		MobileParty mobileParty = homestead?.MobileParty;
		if (homestead == null || homestead.IsRetiredOrDestroyed || mobileParty == null || mobileParty.IsDisbanding || !mobileParty.IsActive)
		{
			return true;
		}
		if (mobileParty.Party.NumberOfAllMembers <= 0)
		{
			return true;
		}
		return false;
	}

	private void RebuildHomesteadPartyRegistry(string reason)
	{
		foreach (MobileParty mobileParty in Campaign.Current.MobileParties)
		{
			if (mobileParty?.PartyComponent is Homestead homestead && !mobileParty.IsDisbanding && (!HomesteadMobileParties.TryGetValue(mobileParty, out Homestead value) || value != homestead))
			{
				HomesteadMobileParties[mobileParty] = homestead;
				TraceLogger.Write("HomesteadBehavior", "Registered homestead party '" + mobileParty.StringId + "' from PartyComponent during " + reason + ".");
			}
		}
		foreach (KeyValuePair<MobileParty, Homestead> item in HomesteadMobileParties.ToList())
		{
			MobileParty key = item.Key;
			Homestead value2 = item.Value;
			if (key != null && key.IsActive && !key.IsDisbanding && key.PartyComponent == value2 && value2.IsRetiredOrDestroyed)
			{
				value2.RestoreForSaveCompatibility(reason);
			}
			if (value2.IsRetiredOrDestroyed || key == null || key.IsDisbanding || !key.IsActive || key.PartyComponent != value2)
			{
				HomesteadMobileParties.Remove(key);
				TraceLogger.Write("HomesteadBehavior", "Removed stale homestead registry entry during " + reason + "; party='" + (key?.StringId ?? "null") + "'.");
			}
		}
	}

	private void RebuildPatrolPartyRegistry(string reason)
	{
		PatrolMobileParties.Clear();
		foreach (KeyValuePair<MobileParty, Homestead> item in HomesteadMobileParties.ToList())
		{
			Homestead value = item.Value;
			MobileParty patrolParty = value.PatrolParty;
			if (patrolParty != null && patrolParty.IsActive && !patrolParty.IsDisbanding)
			{
				PatrolMobileParties[patrolParty] = value;
			}
		}
		TraceLogger.Write("HomesteadBehavior", $"Rebuilt patrol party registry during {reason}: {PatrolMobileParties.Count} entries.");
	}

	internal void DestroyInvalidHomesteadParty(Homestead homestead, string reason)
	{
		MobileParty mobileParty = homestead.MobileParty;
		if (TryRecoverInvalidHomesteadParty(homestead, reason))
		{
			return;
		}
		TraceLogger.Write("HomesteadBehavior", string.Format("Removing invalid homestead '{0}' during {1}; party='{2}' active={3} disbanding={4}", homestead.Name, reason, mobileParty?.StringId ?? "null", mobileParty?.IsActive.ToString() ?? "n/a", mobileParty?.IsDisbanding.ToString() ?? "n/a"));
		if (CurrentHomestead == homestead)
		{
			CurrentHomestead = null;
		}
		homestead.MarkRetiredOrDestroyed(reason);
		homestead.RetireNotablesAndResidents();
		CleanupPatrolForHomestead(homestead, returnTroopsToMainParty: false);
		if (mobileParty != null)
		{
			HomesteadMobileParties.Remove(mobileParty);
		}
		else
		{
			MobileParty key = HomesteadMobileParties.FirstOrDefault<KeyValuePair<MobileParty, Homestead>>((KeyValuePair<MobileParty, Homestead> x) => x.Value == homestead).Key;
			if (key != null)
			{
				HomesteadMobileParties.Remove(key);
			}
		}
		if (mobileParty != null && mobileParty.IsActive && !mobileParty.IsDisbanding)
		{
			try
			{
				DestroyPartyAction.Apply(null, mobileParty);
			}
			catch (Exception arg)
			{
				TraceLogger.Write("HomesteadBehavior", $"DestroyPartyAction failed while removing invalid homestead '{mobileParty.StringId}': {arg}");
			}
		}
		Utils.PrintLocalizedMessage("homestead_destroyed_party_invalid", "Your homestead of {HOMESTEAD_NAME} has been abandoned after its party was disbanded.", 255f, 120f, 80f, ("HOMESTEAD_NAME", homestead.Name.ToString()));
	}

	internal bool TryRecoverInvalidHomesteadParty(Homestead homestead, string reason)
	{
		if (homestead == null)
		{
			return false;
		}
		MobileParty mobileParty = homestead.MobileParty;
		if (mobileParty == null || mobileParty.IsDisbanding || !mobileParty.IsActive || mobileParty.Party == null)
		{
			return false;
		}
		if (homestead.IsRetiredOrDestroyed)
		{
			homestead.RestoreForSaveCompatibility(reason);
		}
		if (mobileParty.Party.MapEvent != null || mobileParty.Party.NumberOfAllMembers > 0)
		{
			return true;
		}
		TraceLogger.Write("HomesteadBehavior", $"[DIAG] TryRecoverInvalidHomesteadParty: garrison is EMPTY for '{homestead.Name}' during '{reason}'. " + string.Format("SparringActive={0} leader='{1}'", SparringMissionActive, homestead.Leader?.StringId ?? "NULL"));
		Hero hero = (IsValidHomesteadLeader(homestead.Leader) ? homestead.Leader : GetFallbackLeader(homestead));
		if (hero == null)
		{
			return false;
		}
		try
		{
			if (homestead.Leader != hero)
			{
				homestead.ChangePartyLeader(hero);
			}
			else if (hero.PartyBelongedTo != mobileParty)
			{
				AddHeroToPartyAction.Apply(hero, mobileParty);
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadBehavior", $"Failed to recover empty homestead party '{mobileParty.StringId}' during {reason}: {arg}");
			return false;
		}
		if (mobileParty.Party.NumberOfAllMembers <= 0)
		{
			return false;
		}
		HomesteadMobileParties[mobileParty] = homestead;
		mobileParty.ActualClan = Hero.MainHero?.Clan;
		mobileParty.ShouldJoinPlayerBattles = true;
		homestead.EnsurePartyStaysAtAnchor("save compatibility recovery");
		mobileParty.Party.SetVisualAsDirty();
		TraceLogger.Write("HomesteadBehavior", "Recovered empty homestead party '" + mobileParty.StringId + "' during " + reason + " with leader='" + hero.StringId + "'.");
		return true;
	}

	private void RepairInvalidHomesteadLeaders()
	{
		RepairUnavailableHomesteadLeaders("leader repair");
	}

	private void NormalizeGarrisonHeroCounts(string reason)
	{
		foreach (Homestead value in HomesteadMobileParties.Values)
		{
			TroopRoster troopRoster = value.MobileParty?.MemberRoster;
			if (troopRoster == null)
			{
				continue;
			}
			foreach (TroopRosterElement item in troopRoster.GetTroopRoster().ToList())
			{
				if (item.Character.IsHero)
				{
					int num = item.Number - 1;
					if (num > 0)
					{
						troopRoster.AddToCounts(item.Character, -num);
						TraceLogger.Write("HomesteadBehavior", $"NormalizeGarrisonHeroCounts: removed {num} duplicate(s) of '{item.Character.StringId}' from '{value.Name}' during {reason}.");
					}
				}
			}
		}
	}

	private bool TryRepairUnavailableHomesteadLeader(Homestead homestead, string reason, bool notify)
	{
		if (homestead == null || homestead.IsRetiredOrDestroyed)
		{
			return false;
		}
		if (SparringMissionActive || TavernMissionActive)
		{
			return true;
		}
		Hero leader = homestead.Leader;
		TraceLogger.Write("HomesteadBehavior", $"[DIAG] TryRepairUnavailableHomesteadLeader reason='{reason}' homestead='{homestead.Name}' " + "leader='" + (leader?.StringId ?? "NULL") + "' IsAlive=" + (leader?.IsAlive.ToString() ?? "n/a") + " IsPrisoner=" + (leader?.IsPrisoner.ToString() ?? "n/a") + " IsPlayer=" + (leader?.IsHumanPlayerCharacter.ToString() ?? "n/a") + " PartyBelongedTo='" + (leader?.PartyBelongedTo?.StringId ?? "NULL") + "' " + string.Format("HomesteadParty='{0}' SparringActive={1}", homestead.MobileParty?.StringId ?? "NULL", SparringMissionActive));
		if (IsValidHomesteadLeader(leader))
		{
			if (leader.PartyBelongedTo != homestead.MobileParty && homestead.MobileParty != null)
			{
				try
				{
					AddHeroToPartyAction.Apply(leader, homestead.MobileParty);
					TraceLogger.Write("HomesteadBehavior", $"Returned leader '{leader.StringId}' to homestead '{homestead.Name}' during {reason}.");
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadBehavior", $"Failed returning leader '{leader.StringId}' to homestead '{homestead.Name}' during {reason}: {ex}");
				}
			}
			return true;
		}
		Hero fallbackLeader = GetFallbackLeader(homestead);
		if (fallbackLeader != null)
		{
			try
			{
				homestead.ChangePartyLeader(fallbackLeader);
				TraceLogger.Write("HomesteadBehavior", $"Reassigned homestead '{homestead.Name}' to fallback leader '{fallbackLeader.StringId}' during {reason}.");
				if (notify)
				{
					Utils.PrintLocalizedMessage("homestead_leader_reassigned", "Your homestead of {HOMESTEAD_NAME} was reassigned to {NEW_LEADER_NAME} because its previous leader is unavailable.", 255f, 180f, 80f, ("HOMESTEAD_NAME", homestead.Name.ToString()), ("NEW_LEADER_NAME", fallbackLeader.Name.ToString()));
				}
				return true;
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("HomesteadBehavior", $"Failed reassigning homestead '{homestead.Name}' to fallback leader '{fallbackLeader.StringId}' during {reason}: {ex2}");
			}
		}
		if (leader != null)
		{
			homestead.ClearLeaderIfMatches(leader, reason);
			TraceLogger.Write("HomesteadBehavior", $"Cleared unavailable leader '{leader.StringId}' from homestead '{homestead.Name}' during {reason}; homestead will remain active without leader bonuses.");
			if (notify)
			{
				Utils.PrintLocalizedMessage("homestead_leader_unavailable", "Your homestead of {HOMESTEAD_NAME} has no active leader because its previous leader is unavailable. Assign a new leader to resume leader bonuses.", 255f, 180f, 80f, ("HOMESTEAD_NAME", homestead.Name.ToString()));
			}
		}
		return false;
	}

	private void CreateNewHomesteadAtPlayerLocation(Hero leaderHero)
	{
		if (leaderHero.IsHumanPlayerCharacter)
		{
			Utils.PrintLocalizedMessage("homestead_player_cannot_lead", "The player character cannot lead a homestead. Choose a companion instead.", 255f, 80f, 80f);
			return;
		}
		Homestead homestead = new Homestead(leaderHero);
		MobileParty mobileParty = MobileParty.CreateParty("homestead_" + leaderHero.StringId, homestead);
		mobileParty.InitializeMobilePartyAroundPosition(new TroopRoster(mobileParty.Party), new TroopRoster(mobileParty.Party), MobileParty.MainParty.Position, 1f);
		homestead.CaptureCurrentMapAnchor();
		homestead.EnsurePartyStaysAtAnchor("creation");
		homestead.InitializeHomesteadSceneAtPosition(MobileParty.MainParty.Position, "creation");
		Utils.ShowNameHomesteadScreen(homestead);
		AddHeroToPartyAction.Apply(leaderHero, mobileParty);
		PartyScreenHelper.GetActivePartyState()?.PartyScreenLogic.DoneLogic(isForced: true);
		mobileParty.ActualClan = Hero.MainHero.Clan;
		mobileParty.ShouldJoinPlayerBattles = true;
		mobileParty.Party.SetVisualAsDirty();
		HomesteadChronicle.Record($"{leaderHero.Name} raised a homestead for {Hero.MainHero.Name}'s clan.");
		HomesteadMobileParties[mobileParty] = homestead;
		float num = (float)(leaderHero.GetSkillValue(DefaultSkills.Steward) / 2 + leaderHero.GetSkillValue(DefaultSkills.Engineering) / 2) / 100f;
		int num2 = (int)Math.Floor(num);
		float tierProgress = num % 1f;
		if (num2 > 1)
		{
			homestead.Tier = 1;
			homestead.TierProgress = 1f;
			homestead.Tier1GrowthReady = true;
		}
		else
		{
			homestead.Tier = num2;
			homestead.TierProgress = tierProgress;
		}
		RepairInvalidHomesteadLeaders();
	}

	private MobileParty? FindNearbyHostilePartyForHomestead(Homestead? homestead, float radius = 1f)
	{
		MobileParty mobileParty = homestead?.MobileParty;
		if (mobileParty == null || mobileParty.MapEvent != null)
		{
			return null;
		}
		Vec2 getPosition2D = mobileParty.GetPosition2D;
		float num = radius * radius;
		foreach (MobileParty item in MobileParty.All)
		{
			if (item.IsActive && !item.IsDisbanding && item.MapEvent == null && item != mobileParty && item != MobileParty.MainParty && !item.IsGarrison && !item.IsVillager && !item.IsCaravan && item.MemberRoster.TotalManCount > 0 && (item.Army == null || item.Army.LeaderParty?.MapEvent == null) && mobileParty.MapFaction != null && item.MapFaction != null && FactionManager.IsAtWarAgainstFaction(item.MapFaction, mobileParty.MapFaction) && (getPosition2D - item.GetPosition2D).LengthSquared <= num)
			{
				return item;
			}
		}
		return null;
	}

	public void SaveCurrentHomesteadTemplate(string templateName)
	{
		Homestead homestead = CurrentHomestead;
		if (homestead?.GetHomesteadScene() == null)
		{
			TraceLogger.Write("HomesteadBehavior", "SaveCurrentHomesteadTemplate: No current homestead or scene");
			return;
		}
		List<HomesteadSceneSavedEntity> savedEntities = homestead.GetHomesteadScene().SavedEntities;
		if (savedEntities == null || savedEntities.Count == 0)
		{
			Utils.PrintLocalizedMessage("homestead_template_empty", "There are no buildings to save as a template.", 255f, 200f, 80f);
			return;
		}
		string targetMap = ((Mission.Current != null) ? Mission.Current.SceneName : "");
		HomesteadTemplate homesteadTemplate = HomesteadTemplate.CreateFromEntities(templateName, homestead.Name?.ToString() ?? "Unknown", targetMap, savedEntities);
		if (homesteadTemplate == null)
		{
			Utils.PrintLocalizedMessage("homestead_template_failed", "Failed to create template.", 255f, 80f, 80f);
		}
		else if (HomesteadTemplateManager.SaveTemplate(homesteadTemplate))
		{
			GameTexts.SetVariable("TEMPLATE_NAME", templateName);
			Utils.PrintLocalizedMessage("homestead_template_created", "Template '{TEMPLATE_NAME}' created with {COUNT} buildings.", 80f, 255f, 80f, ("COUNT", savedEntities.Count.ToString()));
			TraceLogger.Write("HomesteadBehavior", "Template '{templateName}' saved with {entities.Count} entities");
		}
		else
		{
			Utils.PrintLocalizedMessage("homestead_template_save_failed", "Failed to save template.", 255f, 80f, 80f);
		}
	}

	private static void TryRecallPatrolToDefendHomestead(Homestead homestead, PartyBase defender)
	{
		try
		{
			if (!homestead.HasActivePatrol)
			{
				return;
			}
			MobileParty patrolParty = homestead.PatrolParty;
			if (patrolParty == null)
			{
				return;
			}
			if (patrolParty.MapEvent != null)
			{
				TraceLogger.Write("HomesteadBehavior", "TryRecallPatrolToDefendHomestead: patrol '" + patrolParty.StringId + "' is already in a MapEvent — skipping.");
				return;
			}
			patrolParty.Party.MapEventSide = defender.MapEventSide;
			TraceLogger.Write("HomesteadBehavior", $"TryRecallPatrolToDefendHomestead: pulled patrol '{patrolParty.StringId}' into defender side for homestead '{homestead.Name}'.");
			if (GlobalSettings<MCMSettings>.Instance.ShowPatrolNotifications)
			{
				Utils.PrintLocalizedMessage("homestead_patrol_recalled_to_defend", "The patrol has rushed back to defend {HOMESTEAD_NAME}!", 255f, 200f, 80f, ("HOMESTEAD_NAME", homestead.Name.ToString()));
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBehavior", "TryRecallPatrolToDefendHomestead: exception — " + ex.Message);
		}
	}
}
