using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Homesteads.Models;
using SandBox;
using SandBox.Objects;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

public class HomesteadSpawningMissionLogic : MissionLogic
{
	private sealed class FollowerState
	{
		public float Grace;

		public bool Active;

		public Vec2 LastPos;

		public float StuckTimer;
	}

	private sealed class PosedNpc
	{
		public Agent Agent;

		public UsablePlace Place;

		public string ActionSetSuffix = "";

		public StandingPoint? Point;

		public ActionIndexCache Action = ActionIndexCache.act_none;

		public int Frames;

		public bool LoggedResolve;

		public bool Arrived;

		public string SpawnName = "";
	}

	private sealed class AmbientNpc
	{
		public Agent Agent;

		public ActionIndexCache Action = ActionIndexCache.act_none;

		public float Timer;
	}

	private sealed class FlagNpc
	{
		public Agent Agent;

		public string ActionKey = "";

		public ActionIndexCache Action = ActionIndexCache.act_none;

		public bool IsWorker;

		public float Timer;
	}

	private sealed class PinnedAnimalAgent
	{
		public readonly Agent Agent;

		public readonly Vec3 Position;

		public readonly Vec2 Direction;

		public PinnedAnimalAgent(Agent agent, Vec3 position, Vec2 direction)
		{
			Agent = agent;
			Position = position;
			Direction = direction;
		}
	}

	private Homestead homestead;

	private HomesteadScene homesteadScene;

	private bool isPlanningMode;

	private const float PinnedAnimalMaximumSpeed = 0.2f;

	private List<SoundEvent> sounds = new List<SoundEvent>();

	private readonly List<PinnedAnimalAgent> pinnedAnimalAgents = new List<PinnedAnimalAgent>();

	private readonly Dictionary<Agent, HomesteadNpcRole> _agentRoles = new Dictionary<Agent, HomesteadNpcRole>();

	private float _entityLoadDelay = 0.5f;

	private bool _entitiesLoaded;

	private Vec3 _playerSpawnPos = Vec3.Invalid;

	private readonly List<Agent> _heroFollowers = new List<Agent>();

	private readonly List<Agent> _followBuffer = new List<Agent>();

	private Vec2 _followAnchorDir = Vec2.Zero;

	private Vec2 _lastPlayerPos2D = Vec2.Zero;

	private bool _lastPlayerPosValid;

	private bool _initialFormDone;

	private readonly Dictionary<Agent, FollowerState> _followStates = new Dictionary<Agent, FollowerState>();

	private readonly LoadingFadeOverlay _loadingFade = new LoadingFadeOverlay();

	private readonly HashSet<Hero> _heroNonFollowers = new HashSet<Hero>();

	private static readonly HashSet<Hero> _staticNonFollowers = new HashSet<Hero>();

	private readonly Hero? _talkTarget;

	private bool _spawned;

	private bool _troopsSpawned;

	private int _tagWaitFrames;

	private bool _exteriorDoorEnabled;

	private static readonly ActionIndexCache _greeterWelcomeAction = ActionIndexCache.Create("act_greeting_front_1");

	private readonly List<PosedNpc> _posedNpcs = new List<PosedNpc>();

	private const float PoseArriveDistSq = 2.25f;

	private readonly List<AmbientNpc> _ambientNpcs = new List<AmbientNpc>();

	private static readonly ActionIndexCache[] _ambientWorkLoops = new ActionIndexCache[5]
	{
		ActionIndexCache.Create("act_conversation_normal_loop"),
		ActionIndexCache.Create("act_conversation_confident_loop"),
		ActionIndexCache.Create("act_conversation_hip_loop"),
		ActionIndexCache.Create("act_conversation_closed_loop"),
		ActionIndexCache.Create("act_conversation_weary2_loop")
	};

	private static readonly ActionIndexCache[] _flagWorkerLoops = new ActionIndexCache[6]
	{
		ActionIndexCache.Create("act_npc_farmer_digging"),
		ActionIndexCache.Create("act_npc_farmer_bush_cutting"),
		ActionIndexCache.Create("act_npc_farmer_scythe_using"),
		ActionIndexCache.Create("act_npc_villager_cloth_washing"),
		ActionIndexCache.Create("act_npc_villager_shoveling"),
		ActionIndexCache.Create("act_npc_villager_leather_cleaning")
	};

	private readonly List<FlagNpc> _flagNpcs = new List<FlagNpc>();

	private bool _usableAuditDone;

	private float _usableAuditTimer = 3f;

	private bool _forcedAiTicking;

	private const float UsableApproachOffset = 3f;

	private const string DogMonsterXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Monsters>\n  <Monster\n    id=\"dog\"\n    action_set=\"as_dog\"\n    monster_usage=\"animals\"\n    weight=\"30\"\n    hit_points=\"80\"\n    num_paces=\"5\"\n    jump_acceleration=\"0\"\n    sound_and_collision_info_class=\"boar\"\n    standing_chest_height=\"0.35\"\n    standing_pelvis_height=\"0.30\"\n    standing_eye_height=\"0.40\"\n    eye_offset_wrt_head=\"0.07, -0.05, 0.0\"\n    jump_speed_limit=\"3.5\"\n    family_type=\"5\"\n    ragdoll_bone_to_check_for_corpses_0=\"dog_root_joint\"\n    ragdoll_bone_to_check_for_corpses_1=\"dog_spine_2_joint\"\n    ragdoll_bone_to_check_for_corpses_2=\"dog_spine_3_joint\"\n    ragdoll_bone_to_check_for_corpses_3=\"dog_neck_1_joint\"\n    ragdoll_bone_to_check_for_corpses_4=\"dog_head_joint\"\n    ragdoll_fall_sound_bone_0=\"dog_root_joint\"\n    ragdoll_fall_sound_bone_1=\"dog_spine_2_joint\"\n    ragdoll_fall_sound_bone_2=\"dog_neck_1_joint\"\n    ragdoll_fall_sound_bone_3=\"dog_head_joint\"\n    head_look_direction_bone=\"dog_head_joint\"\n    thorax_look_direction_bone=\"dog_spine_3_joint\"\n    neck_root_bone=\"dog_neck_1_joint\"\n    pelvis_bone=\"dog_root_joint\"\n    right_upper_arm_bone=\"dog_right_scapula_1_joint\"\n    left_upper_arm_bone=\"dog_left_scapula_1_joint\"\n    fall_blow_damage_bone=\"dog_left_scapula_1_joint\"\n    front_bone_to_detect_ground_slope_index=\"2\"\n    back_bone_to_detect_ground_slope_index=\"4\"\n    bones_to_modify_on_sloping_ground_0=\"dog_neck_1_joint\"\n    bones_to_modify_on_sloping_ground_1=\"dog_left_scapula_joint\"\n    bones_to_modify_on_sloping_ground_2=\"dog_right_scapula_joint\"\n    bones_to_modify_on_sloping_ground_3=\"dog_left_hip_joint\"\n    bones_to_modify_on_sloping_ground_4=\"dog_right_hip_joint\"\n    body_rotation_reference_bone=\"dog_spine_3_joint\">\n    <Capsules>\n      <body_capsule radius=\"0.20\" pos1=\"0, 0.30, 0.30\" pos2=\"0, -0.28, 0.30\" />\n    </Capsules>\n    <Flags CanWander=\"true\" MoveAsHerd=\"true\" />\n  </Monster>\n</Monsters>";

	private static bool _companionDogVariantsRegistered = false;

	private const string CompanionDogVariantsXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Items>\n  <Item id=\"homestead_companion_dog_0\" name=\"{=homestead_companion_dog_name}Loyal Hound\"\n        mesh=\"dog\" value=\"4\" item_category=\"horse\" type=\"Horse\">\n    <ItemComponent>\n      <Horse monster=\"Monster.hog\" maneuver=\"60\" speed=\"28\" charge_damage=\"0\" body_length=\"130\">\n        <Materials><Material name=\"dog_a\" /></Materials>\n      </Horse>\n    </ItemComponent>\n    <Flags Civilian=\"true\" />\n  </Item>\n  <Item id=\"homestead_companion_dog_1\" name=\"{=homestead_companion_dog_name}Loyal Hound\"\n        mesh=\"dog\" value=\"4\" item_category=\"horse\" type=\"Horse\">\n    <ItemComponent>\n      <Horse monster=\"Monster.hog\" maneuver=\"60\" speed=\"28\" charge_damage=\"0\" body_length=\"130\">\n        <Materials><Material name=\"dog_b\" /></Materials>\n      </Horse>\n    </ItemComponent>\n    <Flags Civilian=\"true\" />\n  </Item>\n  <Item id=\"homestead_companion_dog_2\" name=\"{=homestead_companion_dog_name}Loyal Hound\"\n        mesh=\"dog\" value=\"4\" item_category=\"horse\" type=\"Horse\">\n    <ItemComponent>\n      <Horse monster=\"Monster.hog\" maneuver=\"60\" speed=\"28\" charge_damage=\"0\" body_length=\"130\">\n        <Materials><Material name=\"dog_c\" /></Materials>\n      </Horse>\n    </ItemComponent>\n    <Flags Civilian=\"true\" />\n  </Item>\n</Items>";

	private const float FollowerStandoffDist = 2f;

	private const float FollowerRowSpacing = 1.3f;

	private const int FollowerPerRow = 4;

	private const float FollowerArcDegrees = 120f;

	private const float FollowerSlotDeadzone = 1.2f;

	private const float FollowerMoveSpeed = 0.5f;

	private const float FollowerStartDist = 3.5f;

	private const float FollowerGrace = 3f;

	private const float FollowerStuckSpeed = 0.3f;

	private const float FollowerStuckTime = 2.5f;

	public static HomesteadSpawningMissionLogic? Current { get; private set; }

	public bool IsSceneRevealed => _loadingFade.IsDone;

	public static Agent? HoundMasterAgent { get; private set; }

	public static Agent? MarketLadyAgent { get; private set; }

	public static Agent? AmbassadorAgent { get; private set; }

	public static Agent? ArmsMasterAgent { get; private set; }

	public static Agent? TavernKeeperAgent { get; private set; }

	public static Agent? TavernGreeterAgent { get; private set; }

	public bool TryGetAgentRole(Agent agent, out HomesteadNpcRole role)
	{
		return _agentRoles.TryGetValue(agent, out role);
	}

	public static bool IsHeroFollowingStatic(Hero? hero)
	{
		if (hero != null)
		{
			return !_staticNonFollowers.Contains(hero);
		}
		return true;
	}

	public HomesteadSpawningMissionLogic(Homestead homestead, bool isPlanningMode = false, Hero? talkTarget = null)
	{
		this.homestead = homestead;
		this.isPlanningMode = isPlanningMode;
		_talkTarget = talkTarget;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		HoundMasterAgent = null;
		MarketLadyAgent = null;
		AmbassadorAgent = null;
		ArmsMasterAgent = null;
		TavernKeeperAgent = null;
		TavernGreeterAgent = null;
	}

	public override void AfterStart()
	{
		Current = this;
		homesteadScene = homestead.GetHomesteadScene();
		base.Mission.SetMissionMode(MissionMode.StartUp, atStart: true);
		if (!isPlanningMode)
		{
			_loadingFade.Show();
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (!_spawned && _talkTarget == null)
		{
			SpawnPlayer();
			_spawned = true;
		}
		if (!isPlanningMode)
		{
			_loadingFade.Tick(dt, _troopsSpawned);
		}
		if (!_entitiesLoaded && !isPlanningMode)
		{
			_entityLoadDelay -= dt;
			if (_entityLoadDelay <= 0f)
			{
				int num = homesteadScene.AddAllSavedEntitiesToCurrentScene();
				_entitiesLoaded = true;
				TraceLogger.Write("HomesteadSpawningMissionLogic", $"Deferred load: placed {num}/{homesteadScene.SavedEntities.Count} saved buildables into walk-around scene for '{homestead.Name}'.");
				Mission.Current?.GetMissionBehavior<HomesteadSceneEditingMissionLogic>()?.NotifyEntitiesLoaded();
			}
		}
		else if (_entitiesLoaded && !_troopsSpawned && !isPlanningMode)
		{
			bool flag = homesteadScene.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_dog_kennel") ?? false;
			bool flag2 = homesteadScene.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_training_field") ?? false;
			bool flag3 = homesteadScene.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_ambasador_hall") ?? false;
			bool flag4 = homesteadScene.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_market") ?? false;
			homesteadScene.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_tavern");
			bool num2 = homesteadScene.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_stables") ?? false;
			bool flag5 = !flag || base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_hound_master").Any();
			bool flag6 = !flag2 || base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_arms_master").Any();
			bool flag7 = !flag3 || base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_ambassador").Any();
			bool flag8 = !flag4 || base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_market_lady").Any();
			bool flag9 = !num2 || base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_stable_master").Any();
			bool flag10 = true;
			bool flag11 = true;
			if (_talkTarget != null && !_spawned)
			{
				string heroSpawnTag = GetHeroSpawnTag(_talkTarget);
				flag11 = heroSpawnTag == null || base.Mission.Scene.FindEntitiesWithTag(heroSpawnTag).Any();
			}
			if ((!flag5 || !flag6 || !flag7 || !flag8 || !flag9 || !flag10 || !flag11) && _tagWaitFrames < 300)
			{
				_tagWaitFrames++;
				return;
			}
			if (!_spawned)
			{
				SpawnPlayer();
				_spawned = true;
			}
			SpawnTroops();
			SpawnPrisoners();
			SpawnAnimals("hog");
			SpawnAnimals("goose");
			SpawnAnimals("chicken");
			SpawnAnimals("cow");
			SpawnAnimals("sheep");
			SpawnAnimals("cat");
			ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>("dog");
			int maxCount = Math.Min((homestead.Stash != null && itemObject != null) ? homestead.Stash.GetItemNumber(itemObject) : 0, 5);
			SpawnAnimals("dog", maxCount);
			SpawnHoundMaster();
			SpawnMarketLady();
			SpawnAmbassador();
			SpawnArmsMaster();
			SpawnMasterSmith();
			SpawnStableMaster();
			SpawnAnimals("sumpter_horse");
			SpawnTavernKeeper();
			SpawnTavernGreeter();
			SpawnResidents();
			SpawnNpcActionFlagAgents();
			HomesteadTutorial.WalkAround();
			RepositionPlayerAtTavernEntranceIfReturning();
			_troopsSpawned = true;
		}
		if (base.Mission.Mode != MissionMode.Conversation && !base.Mission.AllowAiTicking)
		{
			base.Mission.AllowAiTicking = true;
			if (!_forcedAiTicking)
			{
				_forcedAiTicking = true;
				TraceLogger.Write("HomesteadSpawningMissionLogic", "Forced Mission.AllowAiTicking = true (it was FALSE) — enabling AgentNavigator ticking for NPC sit/work/follow.");
			}
		}
		KeepPinnedAnimalsInPlace();
		UpdateHeroFollowers(dt);
		UpdateGreeterDance();
		UpdatePosedNpcs();
		UpdateAmbientNpcs(dt);
		UpdateFlagNpcs(dt);
		if (_troopsSpawned && !_usableAuditDone)
		{
			_usableAuditTimer -= dt;
			if (_usableAuditTimer <= 0f)
			{
				AuditUsablePoints();
			}
		}
		if (!_exteriorDoorEnabled)
		{
			_exteriorDoorEnabled = TryEnableExteriorTavernDoor();
		}
	}

	private bool TryEnableExteriorTavernDoor()
	{
		int num = 0;
		try
		{
			foreach (PassageUsePoint item in base.Mission.ActiveMissionObjects.OfType<PassageUsePoint>())
			{
				item.IsMissionExit = true;
				num++;
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "TryEnableExteriorTavernDoor failed: " + ex.GetType().Name + ": " + ex.Message);
			return false;
		}
		if (num > 0)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", $"TryEnableExteriorTavernDoor: enabled {num} PassageUsePoint(s) in the walk-around.");
			return true;
		}
		return false;
	}

	private static void InstallFlagActionSet(Agent agent, string actionSetSuffix)
	{
		if (agent == null)
		{
			return;
		}
		try
		{
			AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(MBGlobals.GetActionSetWithSuffix(agent.Monster, agent.IsFemale, actionSetSuffix), agent.Character.GetStepSize(), hasClippingPlane: false);
			agent.SetActionSet(ref animationSystemData);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "InstallFlagActionSet('" + actionSetSuffix + "') failed: " + ex.Message);
		}
	}

	private void UpdateAmbientNpcs(float dt)
	{
		if (_ambientNpcs.Count == 0 || base.Mission.Mode == MissionMode.Conversation)
		{
			return;
		}
		for (int num = _ambientNpcs.Count - 1; num >= 0; num--)
		{
			AmbientNpc ambientNpc = _ambientNpcs[num];
			if (ambientNpc.Agent == null || !ambientNpc.Agent.IsActive())
			{
				_ambientNpcs.RemoveAt(num);
			}
			else
			{
				ambientNpc.Timer -= dt;
				if (ambientNpc.Action.Index == ActionIndexCache.act_none.Index || ambientNpc.Timer <= 0f)
				{
					ambientNpc.Action = _ambientWorkLoops[MBRandom.RandomInt(_ambientWorkLoops.Length)];
					ambientNpc.Timer = 18f + MBRandom.RandomFloat * 12f;
				}
				if (ambientNpc.Agent.GetCurrentAction(0).Index != ambientNpc.Action.Index)
				{
					try
					{
						ambientNpc.Agent.SetActionChannel(0, in ambientNpc.Action, ignorePriority: true, (AnimFlags)0uL);
					}
					catch
					{
					}
				}
			}
		}
	}

	private void UpdateFlagNpcs(float dt)
	{
		if (_flagNpcs.Count == 0 || base.Mission.Mode == MissionMode.Conversation)
		{
			return;
		}
		for (int num = _flagNpcs.Count - 1; num >= 0; num--)
		{
			FlagNpc flagNpc = _flagNpcs[num];
			if (flagNpc.Agent == null || !flagNpc.Agent.IsActive())
			{
				_flagNpcs.RemoveAt(num);
			}
			else
			{
				if (flagNpc.IsWorker)
				{
					flagNpc.Timer -= dt;
					if (flagNpc.Action.Index == ActionIndexCache.act_none.Index || flagNpc.Timer <= 0f)
					{
						flagNpc.Action = _flagWorkerLoops[MBRandom.RandomInt(_flagWorkerLoops.Length)];
						flagNpc.Timer = 18f + MBRandom.RandomFloat * 12f;
					}
				}
				if (flagNpc.Action.Index != ActionIndexCache.act_none.Index && flagNpc.Agent.GetCurrentAction(0).Index != flagNpc.Action.Index)
				{
					try
					{
						flagNpc.Agent.SetActionChannel(0, in flagNpc.Action, ignorePriority: true, (AnimFlags)0uL);
					}
					catch
					{
					}
				}
			}
		}
	}

	private static CharacterObject? GetGenericFlagCharacter()
	{
		CultureObject cultureObject = Clan.PlayerClan?.Culture ?? Hero.MainHero?.Culture;
		bool flag = MBRandom.RandomFloat < 0.5f;
		CharacterObject characterObject = null;
		if (cultureObject != null)
		{
			characterObject = (flag ? cultureObject.Townswoman : cultureObject.Townsman);
			if (characterObject == null)
			{
				characterObject = cultureObject.Villager;
			}
			if (characterObject == null)
			{
				characterObject = cultureObject.Guard;
			}
		}
		if (characterObject == null)
		{
			characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>(flag ? "townswoman_empire" : "townsman_empire");
		}
		return characterObject;
	}

	private Dictionary<CharacterObject, int> BuildRemainingGarrisonPool()
	{
		Dictionary<CharacterObject, int> dictionary = new Dictionary<CharacterObject, int>();
		TroopRoster troopRoster = homestead?.Troops;
		if (troopRoster == null)
		{
			return dictionary;
		}
		foreach (TroopRosterElement item in troopRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character != null && !character.IsHero)
			{
				int num = item.Number - item.WoundedNumber;
				if (num > 0)
				{
					dictionary[character] = num;
				}
			}
		}
		return dictionary;
	}

	private static CharacterObject? TakeGarrisonCharacter(Dictionary<CharacterObject, int> pool)
	{
		if (pool.Count == 0)
		{
			return null;
		}
		CharacterObject characterObject = pool.Keys.ElementAt(MBRandom.RandomInt(pool.Count));
		int num = pool[characterObject] - 1;
		if (num > 0)
		{
			pool[characterObject] = num;
		}
		else
		{
			pool.Remove(characterObject);
		}
		return characterObject;
	}

	private void SpawnNpcActionFlagAgents()
	{
		IEnumerable<KeyValuePair<GameEntity, HomesteadSceneSavedEntity>> enumerable = homesteadScene?.LoadedSavedEntities;
		if (enumerable == null)
		{
			return;
		}
		Dictionary<CharacterObject, int> pool = BuildRemainingGarrisonPool();
		int num = 0;
		int num2 = 0;
		foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> item in enumerable)
		{
			HomesteadScenePlaceable homesteadScenePlaceable = item.Value?.Placeable;
			if (homesteadScenePlaceable == null || !homesteadScenePlaceable.IsNpcActionFlag)
			{
				continue;
			}
			CharacterObject characterObject = TakeGarrisonCharacter(pool);
			bool flag = characterObject != null;
			if (flag)
			{
				num2++;
			}
			else
			{
				characterObject = GetGenericFlagCharacter();
			}
			if (characterObject == null)
			{
				continue;
			}
			MatrixFrame globalFrame = item.Key.GetGlobalFrame();
			Vec3 origin = globalFrame.origin;
			Mat3 rotation = globalFrame.rotation;
			string text = homesteadScenePlaceable.NpcAction ?? "";
			bool flag2 = string.Equals(text, "act_musician_idle_stand_active", StringComparison.OrdinalIgnoreCase);
			string text2 = (flag2 ? "_musician" : "_villager");
			bool flag3 = flag && !flag2;
			Agent agent = SpawnHomesteadAgent(null, origin, rotation, homestead.Party, characterObject, AgentControllerType.AI, text2, !flag3, flag3, noHorses: true);
			if (agent == null)
			{
				continue;
			}
			for (int num3 = _ambientNpcs.Count - 1; num3 >= 0; num3--)
			{
				if (_ambientNpcs[num3].Agent == agent)
				{
					_ambientNpcs.RemoveAt(num3);
				}
			}
			FlagNpc flagNpc = new FlagNpc
			{
				Agent = agent,
				ActionKey = text
			};
			bool flag4 = string.Equals(text, "STAND", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(text);
			if (string.Equals(text, "WORKER", StringComparison.OrdinalIgnoreCase))
			{
				flagNpc.IsWorker = true;
			}
			else if (flag4)
			{
				flagNpc.Action = ActionIndexCache.act_none;
			}
			else
			{
				flagNpc.Action = ActionIndexCache.Create(text);
			}
			if (!flag4)
			{
				InstallFlagActionSet(agent, text2);
			}
			_flagNpcs.Add(flagNpc);
			num++;
		}
		if (num > 0)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", $"SpawnNpcActionFlagAgents: spawned {num} NPC(s) at action flags for '{homestead.Name}' " + $"({num2} from the garrison roster, {num - num2} generic).");
		}
	}

	private void AuditUsablePoints()
	{
		_usableAuditDone = true;
		try
		{
			List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_npc").ToList();
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			List<string> list2 = new List<string>();
			foreach (GameEntity item in list)
			{
				UsablePlace firstScriptOfType = item.GetFirstScriptOfType<UsablePlace>();
				AnimationPoint firstScriptOfType2 = item.GetFirstScriptOfType<AnimationPoint>();
				int num4 = ((UsableMachine)(object)firstScriptOfType)?.StandingPoints?.Count ?? (-1);
				if (firstScriptOfType != null)
				{
					num++;
				}
				if (num4 > 0)
				{
					num2++;
				}
				if (firstScriptOfType2 != null)
				{
					num3++;
				}
				if (list2.Count < 14)
				{
					list2.Add(string.Format("{0}[up={1},sp={2},anim={3}]", item.Name, firstScriptOfType != null, num4, (firstScriptOfType2 != null) ? firstScriptOfType2.LoopStartAction : "none"));
				}
			}
			TraceLogger.Write("HomesteadSpawningMissionLogic", string.Format("USABLE AUDIT: {0} spawnpoint_homestead_npc entities | {1} have UsablePlace | {2} have >0 StandingPoints | {3} have AnimationPoint(self/child) | posedNpcs={4}. Sample: {5}", list.Count, num, num2, num3, _posedNpcs.Count, string.Join(" ; ", list2)));
			int num5 = 0;
			List<string> list3 = new List<string>();
			foreach (Agent agent in base.Mission.Agents)
			{
				if (num5 >= 8)
				{
					break;
				}
				if (agent.IsHuman && agent.IsActive() && agent != base.Mission.MainAgent && agent.Controller == AgentControllerType.AI)
				{
					CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
					AgentNavigator val = ((component != null) ? component.AgentNavigator : null);
					string text = "null";
					try
					{
						text = ((val == null) ? null : val.TargetUsableMachine?.GameEntity.Name) ?? "null";
					}
					catch
					{
					}
					int num6 = -1;
					try
					{
						num6 = agent.GetCurrentAction(0).Index;
					}
					catch
					{
					}
					list3.Add(string.Format("{0}[aiCtrl={1},nav={2},navTarget={3},formation={4},action={5}]", agent.Character?.StringId ?? "?", agent.IsAIControlled, val != null, text, agent.Formation != null, num6));
					num5++;
				}
			}
			TraceLogger.Write("HomesteadSpawningMissionLogic", "AGENT AUDIT (AI npc sample): " + string.Join(" ; ", list3));
			TraceLogger.Write("HomesteadSpawningMissionLogic", $"MISSION STATE: Mode={base.Mission.Mode} AllowAiTicking={base.Mission.AllowAiTicking} forcedAiTicking={_forcedAiTicking}");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "AuditUsablePoints failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static StandingPoint? GetUsableStandingPoint(UsablePlace place, Agent agent)
	{
		try
		{
			if (((UsableMachine)(object)place)?.StandingPoints == null || ((UsableMachine)(object)place).StandingPoints.Count == 0)
			{
				return null;
			}
			StandingPoint vacantStandingPointForAI = ((UsableMachine)(object)place).GetVacantStandingPointForAI(agent);
			if (vacantStandingPointForAI != null)
			{
				return vacantStandingPointForAI;
			}
			foreach (StandingPoint standingPoint in ((UsableMachine)(object)place).StandingPoints)
			{
				if (standingPoint is AnimationPoint)
				{
					return standingPoint;
				}
			}
			return ((UsableMachine)(object)place).StandingPoints[0];
		}
		catch
		{
			return null;
		}
	}

	private void UpdatePosedNpcs()
	{
		if (_posedNpcs.Count == 0 || base.Mission.Mode == MissionMode.Conversation)
		{
			return;
		}
		for (int num = _posedNpcs.Count - 1; num >= 0; num--)
		{
			PosedNpc posedNpc = _posedNpcs[num];
			if (posedNpc.Agent == null || !posedNpc.Agent.IsActive())
			{
				_posedNpcs.RemoveAt(num);
				continue;
			}
			if (posedNpc.Point == null)
			{
				StandingPoint usableStandingPoint = GetUsableStandingPoint(posedNpc.Place, posedNpc.Agent);
				if (usableStandingPoint == null)
				{
					if (++posedNpc.Frames > 600)
					{
						_posedNpcs.RemoveAt(num);
					}
					continue;
				}
				posedNpc.Point = usableStandingPoint;
				AnimationPoint val = (AnimationPoint)(object)((usableStandingPoint is AnimationPoint) ? usableStandingPoint : null);
				if (val != null)
				{
					string text = ((!string.IsNullOrEmpty(val.LoopStartAction)) ? val.LoopStartAction : val.ArriveAction);
					if (!string.IsNullOrEmpty(text))
					{
						posedNpc.Action = ActionIndexCache.Create(text);
					}
				}
				if (!posedNpc.LoggedResolve)
				{
					posedNpc.LoggedResolve = true;
					TraceLogger.Write("HomesteadSpawningMissionLogic", "POSE resolve OK: '" + posedNpc.SpawnName + "' standingPoint='" + usableStandingPoint.GetType().Name + "' action='" + ((usableStandingPoint as AnimationPoint)?.LoopStartAction ?? "?") + "'.");
				}
			}
			if (posedNpc.Action.Index == ActionIndexCache.act_none.Index)
			{
				_posedNpcs.RemoveAt(num);
				continue;
			}
			float lengthSquared;
			try
			{
				lengthSquared = (posedNpc.Agent.Position - posedNpc.Point.GetUserFrameForAgent(posedNpc.Agent).Origin.GetGroundVec3()).LengthSquared;
			}
			catch
			{
				lengthSquared = (posedNpc.Agent.Position - posedNpc.Agent.Position).LengthSquared;
			}
			bool flag = posedNpc.Agent.MovementVelocity.LengthSquared < 0.04f;
			if (!(lengthSquared <= 2.25f && flag))
			{
				continue;
			}
			if (!posedNpc.Arrived)
			{
				posedNpc.Arrived = true;
				try
				{
					MatrixFrame globalFrame = posedNpc.Point.GameEntity.GetGlobalFrame();
					if (globalFrame.origin.z - posedNpc.Agent.Position.z > 0.5f)
					{
						posedNpc.Agent.TeleportToPosition(globalFrame.origin);
						Vec2 targetPosition = globalFrame.origin.AsVec2;
						Vec3 targetDirection = globalFrame.rotation.f;
						posedNpc.Agent.SetTargetPositionAndDirection(in targetPosition, in targetDirection);
					}
				}
				catch
				{
				}
				TraceLogger.Write("HomesteadSpawningMissionLogic", $"POSE arrived + posing: '{posedNpc.SpawnName}' action={posedNpc.Action.Index}.");
			}
			if (posedNpc.Agent.GetCurrentAction(0).Index != posedNpc.Action.Index)
			{
				try
				{
					posedNpc.Agent.SetActionChannel(0, in posedNpc.Action, ignorePriority: true, (AnimFlags)0uL);
				}
				catch
				{
				}
			}
		}
	}

	private void UpdateGreeterDance()
	{
		Agent tavernGreeterAgent = TavernGreeterAgent;
		if (tavernGreeterAgent == null || !tavernGreeterAgent.IsActive() || base.Mission.Mode == MissionMode.Conversation || tavernGreeterAgent.GetCurrentAction(0).Index == _greeterWelcomeAction.Index)
		{
			return;
		}
		try
		{
			tavernGreeterAgent.SetActionChannel(0, in _greeterWelcomeAction, ignorePriority: true, (AnimFlags)0uL);
		}
		catch
		{
		}
	}

	private void SpawnTavernGreeter()
	{
		if (!homestead.HasTavernBuilding)
		{
			return;
		}
		CultureObject cultureObject = homestead.Leader?.Culture ?? Hero.MainHero?.Culture;
		if (homestead.TroubadourHero == null || !homestead.TroubadourHero.IsAlive)
		{
			homestead.TryEnsureTroubadourHero();
		}
		CharacterObject characterObject = homestead.TroubadourHero?.CharacterObject ?? cultureObject?.Townswoman ?? cultureObject?.Villager ?? Game.Current.ObjectManager.GetObject<CharacterObject>("villager_woman_empire");
		if (characterObject == null)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnTavernGreeter: no greeter character found — aborting.");
			return;
		}
		GameEntity gameEntity = base.Mission.Scene.FindEntitiesWithTag("homestead_tavern_entrance").FirstOrDefault();
		Vec3 positionToSpawnAt;
		Mat3 rotationToSpawnWith;
		if (gameEntity != null)
		{
			MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
			Vec3 f = globalFrame.rotation.f;
			f.z = 0f;
			f = ((f.LengthSquared > 0.01f) ? f.NormalizedCopy() : new Vec3(0f, 1f));
			Vec3 vec = new Vec3(0f - f.y, f.x);
			positionToSpawnAt = SnapToGround(globalFrame.origin - f * 2.5f + vec * 2.5f);
			rotationToSpawnWith = globalFrame.rotation;
			rotationToSpawnWith.RotateAboutUp(TaleWorlds.Library.MathF.PI);
		}
		else
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnTavernGreeter: entrance tag not found — using overflow spawn.");
			positionToSpawnAt = SnapToGround(GetOverflowSpawnPosition(_playerSpawnPos, 94));
			rotationToSpawnWith = Mat3.Identity;
		}
		TavernGreeterAgent = SpawnHomesteadAgent(null, positionToSpawnAt, rotationToSpawnWith, homestead.Party, characterObject, AgentControllerType.AI, "_villager", civilianEquipment: true, shouldSheathWeapons: false, noHorses: true);
		if (TavernGreeterAgent != null)
		{
			_agentRoles.Remove(TavernGreeterAgent);
		}
		TraceLogger.Write("HomesteadSpawningMissionLogic", $"SpawnTavernGreeter: spawned greeter '{characterObject.Name}' at ({positionToSpawnAt.x:0.##},{positionToSpawnAt.y:0.##},{positionToSpawnAt.z:0.##}).");
	}

	private void RepositionPlayerAtTavernEntranceIfReturning()
	{
		HomesteadBehavior instance = HomesteadBehavior.Instance;
		if (instance == null || !instance.ConsumeReturnNearTavernEntrance())
		{
			return;
		}
		Agent mainAgent = base.Mission.MainAgent;
		if (mainAgent == null || !mainAgent.IsActive())
		{
			return;
		}
		GameEntity gameEntity = base.Mission.Scene.FindEntitiesWithTag("homestead_tavern_entrance").FirstOrDefault();
		if (gameEntity == null)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "return-from-tavern: entrance tag not found — leaving player at default spawn.");
			return;
		}
		Vec3 origin = gameEntity.GetGlobalFrame().origin;
		origin.z = base.Mission.Scene.GetGroundHeightAtPosition(origin) + 0.1f;
		try
		{
			mainAgent.TeleportToPosition(origin);
			TraceLogger.Write("HomesteadSpawningMissionLogic", "Returned from tavern: repositioned player at the tavern entrance.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "return-from-tavern reposition failed: " + ex.Message);
		}
	}

	public override void OnEndMissionInternal()
	{
		Current = null;
		_staticNonFollowers.Clear();
		_agentRoles.Clear();
		_followStates.Clear();
		_followAnchorDir = Vec2.Zero;
		_lastPlayerPosValid = false;
		_initialFormDone = false;
		_loadingFade.Remove();
		foreach (SoundEvent sound in sounds)
		{
			sound.Release();
		}
	}

	private void SpawnPlayer()
	{
		Vec3 vec = homesteadScene.PlayerSpawnPosition;
		Mat3 rotationToSpawnWith = homesteadScene.PlayerSpawnRotation;
		if (!vec.IsValid)
		{
			vec = GetDefaultEditingSpawnPosition();
		}
		if (_talkTarget != null)
		{
			string heroSpawnTag = GetHeroSpawnTag(_talkTarget);
			if (heroSpawnTag != null)
			{
				GameEntity gameEntity = Mission.Current.Scene.FindEntitiesWithTag(heroSpawnTag).FirstOrDefault();
				if (gameEntity != null)
				{
					Vec3 vec2 = gameEntity.GetGlobalFrame().rotation.f;
					vec2.z = 0f;
					if (vec2.LengthSquared > 0.01f)
					{
						vec2.Normalize();
					}
					else
					{
						vec2 = new Vec3(0f, 1f);
					}
					vec = gameEntity.GlobalPosition + vec2 * 1.5f;
					vec.z = gameEntity.GlobalPosition.z;
					Vec3 vec3 = gameEntity.GlobalPosition - vec;
					vec3.z = 0f;
					if (vec3.LengthSquared > 0.01f)
					{
						vec3.Normalize();
					}
					else
					{
						vec3 = -vec2;
					}
					float num = (float)Math.Cos(-0.7853981852531433);
					float num2 = (float)Math.Sin(-0.7853981852531433);
					Vec2 vec4 = new Vec2(vec3.x * num - vec3.y * num2, vec3.x * num2 + vec3.y * num);
					Vec3 f = new Vec3(vec4.x, vec4.y);
					f.Normalize();
					rotationToSpawnWith = new Mat3(new Vec3(f.y, 0f - f.x), in f, new Vec3(0f, 0f, 1f));
				}
			}
		}
		_playerSpawnPos = vec;
		SpawnHomesteadAgent(null, vec, rotationToSpawnWith, PartyBase.MainParty, CharacterObject.PlayerCharacter, AgentControllerType.Player, "", civilianEquipment: false);
	}

	private string? GetHeroSpawnTag(Hero target)
	{
		if (target == homestead.HoundMasterHero)
		{
			return "spawnpoint_homestead_hound_master";
		}
		if (target == homestead.MarketLadyHero)
		{
			return "spawnpoint_homestead_market_lady";
		}
		if (target == homestead.AmbassadorHero)
		{
			return "spawnpoint_homestead_ambassador";
		}
		if (target == homestead.ArmsMasterHero)
		{
			return "spawnpoint_homestead_arms_master";
		}
		if (target == homestead.MasterSmithHero)
		{
			return "spawnpoint_homestead_master_smith";
		}
		if (target == homestead.StableMasterHero)
		{
			return "spawnpoint_homestead_stable_master";
		}
		return null;
	}

	private Vec3 GetDefaultEditingSpawnPosition()
	{
		Vec3 result = TryGetWalkAreaCentroid();
		if (result.IsValid)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", $"No PlayerSpawnPosition — spawning at walk_area centroid ({result.x:0.##},{result.y:0.##},{result.z:0.##}) for '{homestead.Name}'.");
			return result;
		}
		Vec3 tagCentroid = GetTagCentroid("spawnpoint_side_0");
		Vec3 tagCentroid2 = GetTagCentroid("spawnpoint_side_1");
		if (tagCentroid.IsValid && tagCentroid2.IsValid)
		{
			Vec3 vec = new Vec3((tagCentroid.x + tagCentroid2.x) / 2f, (tagCentroid.y + tagCentroid2.y) / 2f);
			vec.z = Mission.Current.Scene.GetGroundHeightAtPosition(vec);
			TraceLogger.Write("HomesteadSpawningMissionLogic", $"No PlayerSpawnPosition — spawning at side midpoint ({vec.x:0.##},{vec.y:0.##},{vec.z:0.##}) [side0=({tagCentroid.x:0.##},{tagCentroid.y:0.##}) side1=({tagCentroid2.x:0.##},{tagCentroid2.y:0.##})] for '{homestead.Name}'.");
			return vec;
		}
		if (tagCentroid.IsValid)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", $"No PlayerSpawnPosition — spawning at side_0 centroid ({tagCentroid.x:0.##},{tagCentroid.y:0.##}) (side_1 absent) for '{homestead.Name}'.");
			return tagCentroid;
		}
		if (tagCentroid2.IsValid)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", $"No PlayerSpawnPosition — spawning at side_1 centroid ({tagCentroid2.x:0.##},{tagCentroid2.y:0.##}) (side_0 absent) for '{homestead.Name}'.");
			return tagCentroid2;
		}
		Vec3 centerPosition = Vec3.Invalid;
		base.Mission.Scene.GetNavMeshCenterPosition(0, ref centerPosition);
		TraceLogger.Write("HomesteadSpawningMissionLogic", $"No PlayerSpawnPosition and no spawn entities — using navmesh centre for '{homestead.Name}'.");
		return centerPosition;
	}

	private static Vec3 GetTagCentroid(string tag)
	{
		float num = 0f;
		float num2 = 0f;
		int num3 = 0;
		foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag(tag))
		{
			Vec3 globalPosition = item.GlobalPosition;
			num += globalPosition.x;
			num2 += globalPosition.y;
			num3++;
		}
		if (num3 == 0)
		{
			return Vec3.Invalid;
		}
		Vec3 vec = new Vec3(num / (float)num3, num2 / (float)num3);
		vec.z = Mission.Current.Scene.GetGroundHeightAtPosition(vec);
		return vec;
	}

	private Vec3 TryGetWalkAreaCentroid()
	{
		try
		{
			Mission.MBBoundaryCollection mBBoundaryCollection = Mission.Current?.Boundaries;
			if (mBBoundaryCollection == null || !mBBoundaryCollection.TryGetValue("walk_area", out var points) || points == null || points.Count < 3)
			{
				return Vec3.Invalid;
			}
			float num = 0f;
			float num2 = 0f;
			int num3 = 0;
			foreach (Vec2 item in points)
			{
				num += item.x;
				num2 += item.y;
				num3++;
			}
			if (num3 == 0)
			{
				return Vec3.Invalid;
			}
			Vec3 vec = new Vec3(num / (float)num3, num2 / (float)num3);
			vec.z = Mission.Current.Scene.GetGroundHeightAtPosition(vec);
			return vec;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "TryGetWalkAreaCentroid: failed (" + ex.GetType().Name + ": " + ex.Message + ").");
			return Vec3.Invalid;
		}
	}

	private static void AddFlattenedToRoster(TroopRoster roster, IEnumerable<FlattenedTroopRosterElement> elements)
	{
		foreach (FlattenedTroopRosterElement element in elements)
		{
			roster.AddToCounts(element.Troop, 1);
		}
	}

	private void SpawnTroops()
	{
		IReadOnlyList<Hero> residents = homestead.ResidentHeroes ?? new List<Hero>();
		try
		{
			foreach (TroopRosterElement item in homestead.Troops.GetTroopRoster().ToList())
			{
				CharacterObject character = item.Character;
				if (character != null && character.IsHero && item.Number > 1)
				{
					homestead.Troops.AddToCounts(item.Character, -(item.Number - 1));
					TraceLogger.Write("HomesteadSpawningMissionLogic", $"Normalized hero '{item.Character.StringId}' count in garrison from {item.Number} to 1.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "Hero roster normalization failed: " + ex.Message);
		}
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		AddFlattenedToRoster(roster, from x in homestead.Troops.ToFlattenedRoster()
			where x.Troop.HeroObject == homestead.Leader
			select x);
		bool flag = SpawnNPCs("spawnpoint_homestead_leader", roster, heroesFollow: true);
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		AddFlattenedToRoster(troopRoster, from x in MobileParty.MainParty.MemberRoster.ToFlattenedRoster()
			where x.Troop.HeroObject != null && x.Troop.HeroObject != homestead.Leader && x.Troop.HeroObject.Spouse == Hero.MainHero && !residents.Contains(x.Troop.HeroObject)
			select x);
		AddFlattenedToRoster(troopRoster, from x in homestead.Troops.ToFlattenedRoster()
			where x.Troop.HeroObject != null && x.Troop.HeroObject != homestead.Leader && x.Troop.HeroObject.Spouse == Hero.MainHero && !residents.Contains(x.Troop.HeroObject)
			select x);
		bool flag2 = SpawnNPCs("spawnpoint_homestead_spouse", troopRoster, heroesFollow: true);
		TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
		AddFlattenedToRoster(troopRoster2, from x in MobileParty.MainParty.MemberRoster.ToFlattenedRoster()
			where x.Troop.HeroObject != null && x.Troop.HeroObject != homestead.Leader && !x.Troop.IsPlayerCharacter && x.Troop.HeroObject.Spouse != Hero.MainHero && !residents.Contains(x.Troop.HeroObject)
			select x);
		AddFlattenedToRoster(troopRoster2, from x in homestead.Troops.ToFlattenedRoster()
			where x.Troop.HeroObject != null && x.Troop.HeroObject != homestead.Leader && !x.Troop.IsPlayerCharacter && x.Troop.HeroObject.Spouse != Hero.MainHero && !residents.Contains(x.Troop.HeroObject)
			select x);
		bool flag3 = SpawnNPCs("spawnpoint_homestead_companion", troopRoster2, heroesFollow: true);
		TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
		foreach (MobileParty item2 in MobileParty.All)
		{
			if (item2 != MobileParty.MainParty && item2.IsActive)
			{
				float num = item2.GetPosition2D.DistanceSquared(MobileParty.MainParty.GetPosition2D);
				if (num < 200f)
				{
					string text = item2.TargetParty?.Name?.ToString() ?? "null";
					string text2 = item2.ShortTermTargetParty?.Name?.ToString() ?? "null";
					string arg = item2.LeaderHero?.Name?.ToString() ?? "null";
					bool flag4 = item2.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction);
					TraceLogger.Write("HomesteadSpawningMissionLogic", $"Nearby party: '{item2.Name}' | Leader: {arg} | DistSq: {num:F2} | " + $"DefaultBehavior: {item2.DefaultBehavior} | ShortTermBehavior: {item2.ShortTermBehavior} | " + "TargetParty: " + text + " | ShortTermTargetParty: " + text2 + " | " + $"AtWar: {flag4}");
				}
			}
		}
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != null)
		{
			foreach (MobileParty attachedParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
			{
				if (attachedParty != MobileParty.MainParty && attachedParty.LeaderHero != null)
				{
					troopRoster3.AddToCounts(attachedParty.LeaderHero.CharacterObject, 1);
				}
			}
		}
		foreach (MobileParty item3 in MobileParty.All)
		{
			if (item3 != MobileParty.MainParty && item3 != homestead.MobileParty && item3.IsActive && item3.LeaderHero != null && item3.LeaderHero != homestead.Leader && item3.LeaderHero != Hero.MainHero && MobileParty.MainParty.MemberRoster.FindIndexOfTroop(item3.LeaderHero.CharacterObject) == -1 && item3.GetPosition2D.DistanceSquared(MobileParty.MainParty.GetPosition2D) < 100f && !item3.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction) && troopRoster3.FindIndexOfTroop(item3.LeaderHero.CharacterObject) == -1)
			{
				troopRoster3.AddToCounts(item3.LeaderHero.CharacterObject, 1);
			}
		}
		if (troopRoster3.TotalManCount > 0)
		{
			InformationManager.DisplayMessage(new InformationMessage($"Spawning {troopRoster3.TotalManCount} visiting heroes from accompanying parties."));
			TraceLogger.Write("HomesteadSpawningMissionLogic", $"Found {troopRoster3.TotalManCount} visiting heroes to spawn.");
		}
		if (troopRoster3.TotalManCount > 0 && !SpawnNPCs("spawnpoint_homestead_companion", troopRoster3, heroesFollow: true))
		{
			SpawnNPCsAtFallbackPosition(troopRoster3, _playerSpawnPos);
		}
		TroopRoster troopRoster4 = TroopRoster.CreateDummyTroopRoster();
		AddFlattenedToRoster(troopRoster4, from x in homestead.Troops.ToFlattenedRoster()
			where x.Troop.HeroObject == null || !residents.Contains(x.Troop.HeroObject)
			select x);
		if (flag)
		{
			troopRoster4.RemoveIf((TroopRosterElement x) => x.Character.HeroObject == homestead.Leader);
		}
		if (flag2)
		{
			troopRoster4.RemoveIf((TroopRosterElement x) => x.Character.HeroObject != null && x.Character.HeroObject != homestead.Leader && x.Character.HeroObject.Spouse == Hero.MainHero);
		}
		if (flag3)
		{
			troopRoster4.RemoveIf((TroopRosterElement x) => x.Character.HeroObject != null && x.Character.HeroObject != homestead.Leader && !x.Character.IsPlayerCharacter && x.Character.HeroObject.Spouse != Hero.MainHero);
		}
		HashSet<CharacterObject> hashSet = new HashSet<CharacterObject>(homestead.Troops.ToFlattenedRoster().Troops);
		if (!flag3)
		{
			foreach (CharacterObject troop in troopRoster2.ToFlattenedRoster().Troops)
			{
				if (!hashSet.Contains(troop))
				{
					troopRoster4.AddToCounts(troop, 1);
				}
			}
		}
		if (!flag2)
		{
			foreach (CharacterObject troop2 in troopRoster.ToFlattenedRoster().Troops)
			{
				if (!hashSet.Contains(troop2))
				{
					troopRoster4.AddToCounts(troop2, 1);
				}
			}
		}
		if (!SpawnNPCs("spawnpoint_homestead_npc", troopRoster4, heroesFollow: true))
		{
			SpawnNPCsAtFallbackPosition(troopRoster4, _playerSpawnPos);
		}
	}

	private void SpawnPrisoners()
	{
		List<GameEntity> entitiesWithNpcSpawnTag = base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_prisoner").OrderByDescending(GetPrisonSpawnPriority).ToList();
		SpawnNPCs(entitiesWithNpcSpawnTag, homestead.Prisoners, heroesFollow: false, isPrisoner: true);
	}

	private bool SpawnNPCs(string spawnpointTag, TroopRoster roster, bool heroesFollow = false)
	{
		List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag(spawnpointTag).ToList();
		if (spawnpointTag == "spawnpoint_homestead_npc")
		{
			list = list.Where((GameEntity e) => e.Name != "sp_training_spawn_1" && e.Name != "training_spawn_1" && e.Name != "sp_training_spawn_2" && e.Name != "training_spawn_2" && e.Name != "sp_training_spawn_3" && e.Name != "training_spawn_3" && e.Name != "sp_training_spawn_4" && e.Name != "training_spawn_4").ToList();
		}
		return SpawnNPCs(list, roster, heroesFollow);
	}

	private bool SpawnNPCs(List<GameEntity> entitiesWithNpcSpawnTag, TroopRoster roster, bool heroesFollow = false, bool isPrisoner = false)
	{
		if (entitiesWithNpcSpawnTag.Count == 0)
		{
			return false;
		}
		List<CharacterObject> list = roster.ToFlattenedRoster().Troops.ToList();
		if (list.Count == 0)
		{
			return false;
		}
		List<GameEntity> e = entitiesWithNpcSpawnTag.ToList();
		Queue<Vec3> campSpots = BuildTentCampSpots(isPrisoner);
		int num = 0;
		foreach (CharacterObject item in list)
		{
			bool flag = entitiesWithNpcSpawnTag.Count > 0;
			GameEntity gameEntity = (flag ? entitiesWithNpcSpawnTag.GetRandomElementInefficiently() : e.GetRandomElementInefficiently());
			string actionSetCodeSuffix = "_villager";
			bool num2 = !item.IsHero && !isPrisoner;
			bool shouldWearCivEquipment = !num2;
			bool shouldSheathWeapons = num2;
			HandleSpawnEntitySpecialTags(gameEntity, ref actionSetCodeSuffix, ref shouldWearCivEquipment, ref shouldSheathWeapons);
			Vec3 positionToSpawnAt;
			Mat3 rotation;
			bool usedCampSpot = false;
			if (!item.IsHero && !isPrisoner && campSpots.Count > 0)
			{
				// Regular garrison troops live around the camp's tents, not in a
				// block on the spawn point: each takes a spot ringed around a
				// placed tent and faces a random way (the villager action set
				// supplies the ambient idles).
				positionToSpawnAt = campSpots.Dequeue();
				rotation = Mat3.Identity;
				rotation.RotateAboutUp(MBRandom.RandomFloat * (TaleWorlds.Library.MathF.PI * 2f));
				usedCampSpot = true;
			}
			else
			{
				positionToSpawnAt = (flag ? gameEntity.GlobalPosition : GetOverflowSpawnPosition(gameEntity.GlobalPosition, num));
				rotation = gameEntity.GetFrame().rotation;
				if (!item.IsHero && !isPrisoner)
				{
					rotation = Mat3.Identity;
					rotation.RotateAboutUp(MBRandom.RandomFloat * (TaleWorlds.Library.MathF.PI * 2f));
				}
			}
			Agent spawned = SpawnHomesteadAgent((flag && !usedCampSpot) ? gameEntity : null, positionToSpawnAt, rotation, homestead.Party, item, AgentControllerType.AI, actionSetCodeSuffix, shouldWearCivEquipment, shouldSheathWeapons, noHorses: true, heroesFollow, isPrisoner);
			if (spawned != null && !item.IsHero && !isPrisoner)
			{
				_campTroopAgents.Add(spawned);
			}
			if (flag && !usedCampSpot)
			{
				entitiesWithNpcSpawnTag.Remove(gameEntity);
			}
			num++;
		}
		return true;
	}

	private readonly List<Agent> _campTroopAgents = new List<Agent>();

	/// <summary>
	/// Spots ringed around every placed tent, shuffled, so garrison troops idle
	/// around and between the tents like a lived-in camp instead of piling up on
	/// the location's spawn point.
	/// </summary>
	private Queue<Vec3> BuildTentCampSpots(bool isPrisoner)
	{
		Queue<Vec3> queue = new Queue<Vec3>();
		if (isPrisoner)
		{
			return queue;
		}
		try
		{
			List<Vec3> spots = new List<Vec3>();
			foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> kv in homestead.GetHomesteadScene().LoadedSavedEntities)
			{
				string prefab = kv.Value?.Placeable?.PrefabName ?? "";
				if (kv.Key == null || (prefab.IndexOf("tent", StringComparison.OrdinalIgnoreCase) < 0 && prefab.IndexOf("yurt", StringComparison.OrdinalIgnoreCase) < 0))
				{
					continue;
				}
				int points = 3 + MBRandom.RandomInt(3);
				for (int i = 0; i < points; i++)
				{
					spots.Add(GetRingSpotAround(kv.Key));
				}
			}
			while (spots.Count > 0)
			{
				int index = MBRandom.RandomInt(spots.Count);
				queue.Enqueue(spots[index]);
				spots.RemoveAt(index);
			}
			if (queue.Count > 0)
			{
				TraceLogger.Write("HomesteadSpawningMissionLogic", $"BuildTentCampSpots: {queue.Count} camp spots around placed tents.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "BuildTentCampSpots failed: " + ex.Message);
		}
		return queue;
	}

	/// <summary>
	/// A ring spot OUTSIDE the shelter's canvas: the minimum radius comes from
	/// the entity's bounding-box footprint plus clearance, so nobody spawns
	/// halfway inside the tent walls.
	/// </summary>
	private Vec3 GetRingSpotAround(GameEntity shelterEntity)
	{
		Vec3 center = shelterEntity.GlobalPosition;
		float footprint = 1.4f;
		try
		{
			Vec3 boundingBoxMin = shelterEntity.GetBoundingBoxMin();
			Vec3 boundingBoxMax = shelterEntity.GetBoundingBoxMax();
			footprint = Math.Max(Math.Abs(boundingBoxMax.X - boundingBoxMin.X), Math.Abs(boundingBoxMax.Y - boundingBoxMin.Y)) * 0.5f;
		}
		catch
		{
		}
		float radius = Math.Max(1.8f, footprint + 0.6f) + MBRandom.RandomFloat * 1.6f;
		float angle = MBRandom.RandomFloat * (TaleWorlds.Library.MathF.PI * 2f);
		return SnapToGround(center + new Vec3(TaleWorlds.Library.MathF.Cos(angle) * radius, TaleWorlds.Library.MathF.Sin(angle) * radius));
	}

	/// <summary>
	/// Called when a tent/yurt is built mid-visit: up to four garrison troops
	/// WALK over (scripted move, walking animation) and settle around the new
	/// shelter, so the camp reorganizes live instead of only on the next visit.
	/// </summary>
	public void SendTroopsToCampShelter(GameEntity shelterEntity, int maxTroops = 4)
	{
		try
		{
			if (shelterEntity == null)
			{
				return;
			}
			Vec3 center = shelterEntity.GlobalPosition;
			List<Agent> candidates = _campTroopAgents.Where((Agent a) => a != null && a.IsActive() && !a.IsPlayerControlled && center.Distance(a.Position) > 5f).OrderByDescending((Agent a) => center.Distance(a.Position)).ToList();
			int sent = 0;
			foreach (Agent agent in candidates)
			{
				if (sent >= maxTroops)
				{
					break;
				}
				Vec3 spot = GetRingSpotAround(shelterEntity);
				WorldPosition worldPosition = new WorldPosition(base.Mission.Scene, spot);
				agent.SetScriptedPosition(ref worldPosition, addHumanLikeDelay: true, Agent.AIScriptedFrameFlags.DoNotRun);
				sent++;
			}
			if (sent > 0)
			{
				TraceLogger.Write("HomesteadSpawningMissionLogic", $"SendTroopsToCampShelter: {sent} troops walking to the new shelter at ({center.X:0.#},{center.Y:0.#}).");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SendTroopsToCampShelter failed: " + ex.Message);
		}
	}

	private Vec3 SnapToGround(Vec3 p)
	{
		try
		{
			float groundHeightAtPosition = base.Mission.Scene.GetGroundHeightAtPosition(p);
			if (groundHeightAtPosition < 9999f)
			{
				p.z = groundHeightAtPosition;
			}
		}
		catch
		{
		}
		return p;
	}

	private static Vec3 GetOverflowSpawnPosition(Vec3 origin, int index)
	{
		// Wide, slightly jittered rings: overflow troops spread out like a camp
		// instead of packing shoulder-to-shoulder on the spawn point.
		float x = (float)(index % 12) * (TaleWorlds.Library.MathF.PI / 6f) + MBRandom.RandomFloat * 0.35f;
		int num = index / 12 + 1;
		float num2 = (2.6f + MBRandom.RandomFloat) * (float)num;
		return origin + new Vec3(TaleWorlds.Library.MathF.Cos(x) * num2, TaleWorlds.Library.MathF.Sin(x) * num2);
	}

	private static int GetPrisonSpawnPriority(GameEntity entity)
	{
		GameEntity gameEntity = entity;
		while (gameEntity != null)
		{
			if (gameEntity.Name == "homestead_prison_guardhouse")
			{
				return 2;
			}
			if (gameEntity.Name == "homestead_cage_wooden")
			{
				return 1;
			}
			gameEntity = gameEntity.Parent;
		}
		return 0;
	}

	private void HandleSpawnEntitySpecialTags(GameEntity entity, ref string actionSetCodeSuffix, ref bool shouldWearCivEquipment, ref bool shouldSheathWeapons)
	{
		if (entity.HasTag("homestead_flute_musician"))
		{
			SoundEvent soundEvent = SoundEvent.CreateEvent(SoundEvent.GetEventIdFromString("homestead/music/flute" + MBRandom.RandomInt(1, 3)), base.Mission.Scene);
			soundEvent.PlayInPosition(entity.GlobalPosition);
			sounds.Add(soundEvent);
			actionSetCodeSuffix = "_musician";
		}
		else if (entity.HasTag("homestead_guard"))
		{
			shouldWearCivEquipment = false;
			shouldSheathWeapons = true;
			actionSetCodeSuffix = "_guard";
		}
	}

	private Agent SpawnHomesteadAgent(GameEntity? spawnEntity, Vec3 positionToSpawnAt, Mat3 rotationToSpawnWith, PartyBase fromParty, CharacterObject characterObject, AgentControllerType controllerType, string actionSetCodeSuffix, bool civilianEquipment, bool shouldSheathWeapons = false, bool noHorses = false, bool addToFollowers = false, bool isPrisoner = false)
	{
		if (characterObject.HeroObject != null)
		{
			foreach (Agent agent2 in Mission.Current.Agents)
			{
				if (agent2.IsHuman && agent2.IsActive() && (agent2.Character as CharacterObject)?.HeroObject == characterObject.HeroObject)
				{
					TraceLogger.Write("HomesteadSpawningMissionLogic", $"SpawnHomesteadAgent: skipping duplicate spawn of hero '{characterObject.HeroObject.Name}'.");
					return agent2;
				}
			}
		}
		bool flag = addToFollowers && characterObject.HeroObject != null && controllerType == AgentControllerType.AI;
		UsablePlace val = ((flag || spawnEntity == null) ? null : spawnEntity.GetFirstScriptOfType<UsablePlace>());
		AgentBuildData agentBuildData = new AgentBuildData(characterObject).Team(base.Mission.PlayerTeam).InitialPosition(in positionToSpawnAt);
		Vec2 direction = rotationToSpawnWith.f.AsVec2.Normalized();
		bool flag2 = civilianEquipment;
		Equipment equipment = null;
		if (civilianEquipment && !HasUsableCivilianEquipment(characterObject))
		{
			equipment = GetFallbackCivilianEquipment(characterObject);
			if (equipment == null)
			{
				flag2 = false;
				TraceLogger.Write("HomesteadSpawningMissionLogic", "No safe civilian equipment found for '" + characterObject.StringId + "'. Spawning with battle equipment instead.");
			}
		}
		IAgentOriginBase troopOrigin = new PartyAgentOrigin(fromParty, characterObject);
		AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(in direction).CivilianEquipment(flag2).NoHorses(noHorses || spawnEntity != null)
			.NoWeapons(flag2)
			.ClothingColor1(base.Mission.PlayerTeam.Color)
			.ClothingColor2(base.Mission.PlayerTeam.Color2)
			.TroopOrigin(troopOrigin)
			.Controller(controllerType);
		if (equipment != null)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "Using civilian equipment fallback for '" + characterObject.StringId + "'");
			agentBuildData2.Equipment(equipment).FixedEquipment(fixedEquipment: true);
		}
		if (characterObject.HeroObject?.ClanBanner != null)
		{
			agentBuildData2.Banner(characterObject.HeroObject.ClanBanner);
		}
		Agent agent = base.Mission.SpawnAgent(agentBuildData2);
		if (controllerType != AgentControllerType.Player)
		{
			try
			{
				TickAgentAnimations(agent);
			}
			catch
			{
			}
		}
		if (val != null)
		{
			try
			{
				AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(MBGlobals.GetActionSetWithSuffix(agent.Monster, agent.IsFemale, actionSetCodeSuffix), agent.Character.GetStepSize(), hasClippingPlane: false);
				agent.SetActionSet(ref animationSystemData);
				agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator().SetTarget((UsableMachine)(object)val, false, Agent.AIScriptedFrameFlags.None);
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSpawningMissionLogic", "SetTarget failed for '" + characterObject.StringId + "' on '" + ((object)val).GetType().Name + "': " + ex.Message);
			}
			_posedNpcs.Add(new PosedNpc
			{
				Agent = agent,
				Place = val,
				ActionSetSuffix = actionSetCodeSuffix,
				SpawnName = (spawnEntity?.Name ?? "?")
			});
		}
		else if (flag)
		{
			_heroFollowers.Add(agent);
			Hero heroObject = characterObject.HeroObject;
			bool flag3 = homestead != null && heroObject == homestead.Leader;
			bool flag4 = HomesteadBehavior.GetApprenticeQuestForHero(heroObject) != null;
			SetHeroFollowing(heroObject, !flag3 && !flag4);
		}
		if (shouldSheathWeapons)
		{
			SheathAgentWeapons(agent);
		}
		if (controllerType == AgentControllerType.AI && characterObject.HeroObject == null)
		{
			HomesteadNpcRole homesteadNpcRole = (isPrisoner ? HomesteadNpcRole.Prisoner : ((actionSetCodeSuffix == "_guard") ? HomesteadNpcRole.Guard : ((actionSetCodeSuffix == "_musician") ? HomesteadNpcRole.Musician : ((spawnEntity != null && spawnEntity.Name.IndexOf("worker", StringComparison.OrdinalIgnoreCase) >= 0) ? HomesteadNpcRole.Worker : (characterObject.IsSoldier ? HomesteadNpcRole.Soldier : HomesteadNpcRole.Villager)))));
			_agentRoles[agent] = homesteadNpcRole;
			if (val == null && !flag && (homesteadNpcRole == HomesteadNpcRole.Villager || homesteadNpcRole == HomesteadNpcRole.Worker))
			{
				_ambientNpcs.Add(new AmbientNpc
				{
					Agent = agent
				});
			}
		}
		return agent;
	}

	private static void SheathAgentWeapons(Agent agent)
	{
		agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
		RemoveShieldIfPresent(agent);
	}

	private static void RemoveShieldIfPresent(Agent agent)
	{
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			ItemObject item = agent.Equipment[equipmentIndex].Item;
			if (item != null && item.ItemType == ItemObject.ItemTypeEnum.Shield)
			{
				agent.RemoveEquippedWeapon(equipmentIndex);
				break;
			}
		}
	}

	private static bool HasUsableCivilianEquipment(CharacterObject characterObject)
	{
		Equipment equipment;
		return TryGetFirstUsableCivilianEquipment(characterObject, out equipment);
	}

	private static Equipment? GetFallbackCivilianEquipment(CharacterObject characterObject)
	{
		CharacterObject fallbackCivilianTemplate = GetFallbackCivilianTemplate(characterObject);
		if (fallbackCivilianTemplate != null && TryGetFirstUsableCivilianEquipment(fallbackCivilianTemplate, out Equipment equipment))
		{
			return equipment.Clone();
		}
		Hero heroObject = characterObject.HeroObject;
		if (heroObject != null)
		{
			try
			{
				Equipment civilianEquipment = Campaign.Current.Models.HeroCreationModel.GetCivilianEquipment(heroObject);
				if (IsUsableEquipment(civilianEquipment))
				{
					return civilianEquipment.Clone();
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSpawningMissionLogic", "HeroCreationModel civilian equipment fallback failed for '" + characterObject.StringId + "': " + ex.GetType().Name + ": " + ex.Message);
			}
		}
		return null;
	}

	private static bool TryGetFirstUsableCivilianEquipment(CharacterObject characterObject, out Equipment equipment)
	{
		equipment = null;
		try
		{
			foreach (Equipment civilianEquipment in characterObject.CivilianEquipments)
			{
				if (IsUsableEquipment(civilianEquipment))
				{
					equipment = civilianEquipment;
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "Failed reading civilian equipment for '" + characterObject.StringId + "': " + ex.GetType().Name + ": " + ex.Message);
		}
		return false;
	}

	private static bool IsUsableEquipment(Equipment? equipment)
	{
		try
		{
			return equipment != null && !equipment.IsEmpty();
		}
		catch
		{
			return false;
		}
	}

	private static CharacterObject? GetFallbackCivilianTemplate(CharacterObject characterObject)
	{
		CultureObject culture = characterObject.Culture;
		if (culture == null)
		{
			return null;
		}
		if (characterObject.IsFemale)
		{
			if (culture.Townswoman != null)
			{
				return culture.Townswoman;
			}
		}
		else if (culture.Townsman != null)
		{
			return culture.Townsman;
		}
		if (culture.Villager != null)
		{
			return culture.Villager;
		}
		if (culture.Guard != null)
		{
			return culture.Guard;
		}
		return null;
	}

	private void SpawnAnimals(string animalNameId, int maxCount = int.MaxValue)
	{
		if (maxCount <= 0)
		{
			return;
		}
		if (animalNameId == "dog")
		{
			EnsureDogItemUsesCorrectMonster();
		}
		int num = 0;
		foreach (GameEntity item in base.Mission.Scene.FindEntitiesWithTag("sp_" + animalNameId))
		{
			if (num >= maxCount)
			{
				break;
			}
			MatrixFrame frame = item.GetFrame();
			ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(animalNameId);
			if (itemObject == null)
			{
				TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnAnimals: ItemObject '" + animalNameId + "' not found — skipping spawn point.");
				continue;
			}
			if (animalNameId == "dog")
			{
				string text = itemObject.HorseComponent?.Monster?.StringId ?? "null";
				TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnAnimals(dog): item.HorseComponent.Monster = '" + text + "'.");
			}
			Monster monster = itemObject.HorseComponent?.Monster;
			if (monster == null)
			{
				TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnAnimals: ItemObject '" + animalNameId + "' has no HorseComponent/Monster — skipping spawn point.");
				continue;
			}
			ItemRosterElement rosterElement = new ItemRosterElement(itemObject);
			Vec3 initialPosition = item.GlobalPosition;
			Vec2 initialDirection = frame.rotation.f.AsVec2;
			Agent agent;
			try
			{
				agent = base.Mission.SpawnMonster(rosterElement, default(ItemRosterElement), in initialPosition, in initialDirection);
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnAnimals: Mission.SpawnMonster threw for '" + animalNameId + "' (monster '" + monster.StringId + "'): " + ex.GetType().Name + " " + ex.Message + " — skipping spawn point.");
				continue;
			}
			if (agent != null && animalNameId == "dog" && agent.Monster?.StringId != "dog")
			{
				TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnAnimals(dog): spawned with Monster '" + agent.Monster?.StringId + "' instead of 'dog' — EnsureDogItemUsesCorrectMonster may have failed; dog may render incorrectly.");
			}
			if (agent != null)
			{
				if (animalNameId == "dog")
				{
					agent.Controller = AgentControllerType.AI;
				}
				else
				{
					agent.Controller = AgentControllerType.None;
					PinAnimalAgent(agent, initialPosition, initialDirection);
				}
				TickAgentAnimations(agent);
				num++;
			}
		}
	}

	private void SpawnHoundMaster()
	{
		List<HomesteadSceneSavedEntity> savedEntities = homesteadScene.SavedEntities;
		if (savedEntities == null || !savedEntities.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_dog_kennel"))
		{
			return;
		}
		if (homestead.HoundMasterHero == null)
		{
			homestead.TryEnsureHoundMasterHero();
		}
		CharacterObject characterObject = homestead.HoundMasterHero?.CharacterObject;
		if (characterObject == null)
		{
			characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("homestead_hound_master_template");
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnHoundMaster: hero null after TryEnsure, template lookup → " + ((characterObject == null) ? "NOT FOUND" : $"found '{characterObject.Name}'"));
		}
		if (characterObject == null)
		{
			characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("spc_notable_empire_21");
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnHoundMaster: template not found, using spc_notable_empire_21 → " + ((characterObject == null) ? "NOT FOUND" : "found"));
		}
		if (characterObject == null)
		{
			characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("townsman_empire");
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnHoundMaster: spc_notable_empire_21 not found, falling back to townsman_empire (last resort).");
		}
		if (characterObject == null)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnHoundMaster: no suitable character found — aborting.");
			return;
		}
		List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_hound_master").ToList();
		Vec3 positionToSpawnAt;
		Mat3 rotationToSpawnWith;
		if (list.Count > 0)
		{
			positionToSpawnAt = SnapToGround(list[0].GlobalPosition);
			rotationToSpawnWith = list[0].GetGlobalFrame().rotation;
		}
		else
		{
			positionToSpawnAt = GetOverflowSpawnPosition(_playerSpawnPos, 99);
			rotationToSpawnWith = Mat3.Identity;
		}
		HoundMasterAgent = SpawnHomesteadAgent(null, positionToSpawnAt, rotationToSpawnWith, homestead.Party, characterObject, AgentControllerType.AI, "_villager", civilianEquipment: true, shouldSheathWeapons: false, noHorses: true);
		TraceLogger.Write("HomesteadSpawningMissionLogic", $"SpawnHoundMaster: spawned '{characterObject.Name}' at ({positionToSpawnAt.x:0.##},{positionToSpawnAt.y:0.##},{positionToSpawnAt.z:0.##})");
	}

	private void SpawnMarketLady()
	{
		List<HomesteadSceneSavedEntity> savedEntities = homesteadScene.SavedEntities;
		if (savedEntities == null || !savedEntities.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_market"))
		{
			return;
		}
		if (homestead.MarketLadyHero == null)
		{
			homestead.TryEnsureMarketLadyHero();
		}
		CharacterObject characterObject = homestead.MarketLadyHero?.CharacterObject;
		if (characterObject == null)
		{
			characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("homestead_market_lady_template");
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnMarketLady: hero null after TryEnsure, template lookup → " + ((characterObject == null) ? "NOT FOUND" : $"found '{characterObject.Name}'"));
		}
		if (characterObject == null)
		{
			characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("villager_woman_empire");
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnMarketLady: template not found, using villager_woman_empire → " + ((characterObject == null) ? "NOT FOUND" : "found"));
		}
		if (characterObject == null)
		{
			characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("townsman_empire");
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnMarketLady: villager_woman_empire not found either, using townsman_empire → " + ((characterObject == null) ? "NOT FOUND" : "found"));
		}
		if (characterObject == null)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnMarketLady: no suitable character found — aborting.");
			return;
		}
		List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_market_lady").ToList();
		GameEntity gameEntity = null;
		Vec3 vec;
		Mat3 rotationToSpawnWith;
		if (list.Count > 0)
		{
			gameEntity = list[0];
			vec = SnapToGround(gameEntity.GlobalPosition);
			rotationToSpawnWith = gameEntity.GetGlobalFrame().rotation;
		}
		else
		{
			GameEntity gameEntity2 = base.Mission.Scene.FindEntitiesWithTag("homestead_market").FirstOrDefault();
			if (gameEntity2 != null)
			{
				vec = gameEntity2.GlobalPosition + new Vec3(1.5f);
				rotationToSpawnWith = Mat3.Identity;
			}
			else
			{
				vec = GetOverflowSpawnPosition(_playerSpawnPos, 98);
				rotationToSpawnWith = Mat3.Identity;
			}
			float groundHeightAtPosition = base.Mission.Scene.GetGroundHeightAtPosition(vec);
			if (groundHeightAtPosition < 9999f)
			{
				vec.z = groundHeightAtPosition;
			}
		}
		MarketLadyAgent = SpawnHomesteadAgent(gameEntity, vec, rotationToSpawnWith, homestead.Party, characterObject, AgentControllerType.AI, "_villager", civilianEquipment: true, shouldSheathWeapons: false, noHorses: true);
		TraceLogger.Write("HomesteadSpawningMissionLogic", $"SpawnMarketLady: spawned '{characterObject.Name}' at ({vec.x:0.##},{vec.y:0.##},{vec.z:0.##})");
	}

	private void SpawnAmbassador()
	{
		List<HomesteadSceneSavedEntity> savedEntities = homesteadScene.SavedEntities;
		if (savedEntities == null || !savedEntities.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_ambasador_hall"))
		{
			return;
		}
		if (homestead.AmbassadorHero == null)
		{
			homestead.TryEnsureAmbassadorHero();
		}
		CharacterObject characterObject = homestead.AmbassadorHero?.CharacterObject;
		if (characterObject == null)
		{
			characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("homestead_ambassador_template");
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnAmbassador: hero null after TryEnsure, template lookup → " + ((characterObject == null) ? "NOT FOUND" : $"found '{characterObject.Name}'"));
		}
		if (characterObject == null)
		{
			characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("spc_notable_empire_21");
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnAmbassador: template not found, falling back to spc_notable_empire_21 → " + ((characterObject == null) ? "NOT FOUND" : "found"));
		}
		if (characterObject == null)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnAmbassador: no suitable character found — aborting.");
			return;
		}
		List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_ambassador").ToList();
		Vec3 positionToSpawnAt;
		Mat3 rotationToSpawnWith;
		if (list.Count > 0)
		{
			positionToSpawnAt = SnapToGround(list[0].GlobalPosition);
			rotationToSpawnWith = list[0].GetGlobalFrame().rotation;
		}
		else
		{
			positionToSpawnAt = SnapToGround(GetOverflowSpawnPosition(_playerSpawnPos, 97));
			rotationToSpawnWith = Mat3.Identity;
		}
		AmbassadorAgent = SpawnHomesteadAgent(null, positionToSpawnAt, rotationToSpawnWith, homestead.Party, characterObject, AgentControllerType.AI, "_villager", civilianEquipment: true, shouldSheathWeapons: false, noHorses: true);
		TraceLogger.Write("HomesteadSpawningMissionLogic", $"SpawnAmbassador: spawned '{characterObject.Name}' at ({positionToSpawnAt.x:0.##},{positionToSpawnAt.y:0.##},{positionToSpawnAt.z:0.##})");
	}

	private void SpawnArmsMaster()
	{
		HomesteadScene obj = homesteadScene;
		if (obj == null || obj.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_training_field") != true)
		{
			return;
		}
		Hero armsMasterHero = homestead.ArmsMasterHero;
		if (armsMasterHero == null || !armsMasterHero.IsAlive)
		{
			return;
		}
		CharacterObject characterObject = armsMasterHero.CharacterObject;
		if (characterObject == null)
		{
			characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("homestead_arms_master_template");
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnArmsMaster: hero char null, template lookup → " + ((characterObject == null) ? "NOT FOUND" : "found"));
		}
		if (characterObject == null)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnArmsMaster: no character found — aborting.");
			return;
		}
		List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_arms_master").ToList();
		GameEntity gameEntity = null;
		if (list.Count > 0)
		{
			gameEntity = list[0];
		}
		else if (homesteadScene?.LoadedSavedEntities != null)
		{
			foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> loadedSavedEntity in homesteadScene.LoadedSavedEntities)
			{
				if (loadedSavedEntity.Value.Placeable?.PrefabName == "homestead_training_field")
				{
					gameEntity = FindChildWithTagRecursive(loadedSavedEntity.Key, "spawnpoint_homestead_arms_master");
					if (gameEntity != null)
					{
						InformationManager.DisplayMessage(new InformationMessage("[Homesteads] Found Arms Master via manual fallback.", Colors.Magenta));
						break;
					}
				}
			}
		}
		Vec3 positionToSpawnAt;
		Mat3 rotationToSpawnWith;
		if (gameEntity != null)
		{
			MatrixFrame matrixFrame = gameEntity.GetGlobalFrame();
			if (matrixFrame.origin.Length < 0.001f)
			{
				matrixFrame = CalculateGlobalFrame(gameEntity);
			}
			positionToSpawnAt = SnapToGround(matrixFrame.origin);
			rotationToSpawnWith = matrixFrame.rotation;
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("[Homesteads] Arms Master tag not found; using fallback spawn.", Colors.Red));
			positionToSpawnAt = SnapToGround(GetOverflowSpawnPosition(_playerSpawnPos, 95));
			rotationToSpawnWith = Mat3.Identity;
		}
		ArmsMasterAgent = SpawnHomesteadAgent(null, positionToSpawnAt, rotationToSpawnWith, homestead.Party, characterObject, AgentControllerType.AI, "_guard", civilianEquipment: false, shouldSheathWeapons: true, noHorses: true);
		TraceLogger.Write("HomesteadSpawningMissionLogic", $"SpawnArmsMaster: spawned '{characterObject.Name}' at ({positionToSpawnAt.x:0.##},{positionToSpawnAt.y:0.##},{positionToSpawnAt.z:0.##})");
	}

	private void SpawnMasterSmith()
	{
		if (!homestead.HasSmithy)
		{
			return;
		}
		Hero masterSmithHero = homestead.MasterSmithHero;
		if (masterSmithHero == null || !masterSmithHero.IsAlive)
		{
			return;
		}
		CharacterObject characterObject = masterSmithHero.CharacterObject;
		if (characterObject == null)
		{
			return;
		}
		GameEntity gameEntity = base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_master_smith").FirstOrDefault();
		Vec3 positionToSpawnAt;
		Mat3 rotationToSpawnWith;
		if (gameEntity != null)
		{
			MatrixFrame matrixFrame = gameEntity.GetGlobalFrame();
			if (matrixFrame.origin.Length < 0.001f)
			{
				matrixFrame = CalculateGlobalFrame(gameEntity);
			}
			positionToSpawnAt = SnapToGround(matrixFrame.origin);
			rotationToSpawnWith = matrixFrame.rotation;
		}
		else
		{
			positionToSpawnAt = SnapToGround(GetOverflowSpawnPosition(_playerSpawnPos, 96));
			rotationToSpawnWith = Mat3.Identity;
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnMasterSmith: master-smith tag not found — using overflow spawn.");
		}
		SpawnHomesteadAgent(null, positionToSpawnAt, rotationToSpawnWith, homestead.Party, characterObject, AgentControllerType.AI, "_villager", civilianEquipment: true, shouldSheathWeapons: true, noHorses: true);
		TraceLogger.Write("HomesteadSpawningMissionLogic", $"SpawnMasterSmith: spawned '{characterObject.Name}' at ({positionToSpawnAt.x:0.##},{positionToSpawnAt.y:0.##},{positionToSpawnAt.z:0.##}).");
	}

	private void SpawnStableMaster()
	{
		if (!homestead.HasStable)
		{
			return;
		}
		Hero stableMasterHero = homestead.StableMasterHero;
		if (stableMasterHero == null || !stableMasterHero.IsAlive)
		{
			return;
		}
		CharacterObject characterObject = stableMasterHero.CharacterObject;
		if (characterObject == null)
		{
			return;
		}
		GameEntity gameEntity = base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_stable_master").FirstOrDefault();
		Vec3 positionToSpawnAt;
		Mat3 rotationToSpawnWith;
		if (gameEntity != null)
		{
			MatrixFrame matrixFrame = gameEntity.GetGlobalFrame();
			if (matrixFrame.origin.Length < 0.001f)
			{
				matrixFrame = CalculateGlobalFrame(gameEntity);
			}
			positionToSpawnAt = SnapToGround(matrixFrame.origin);
			rotationToSpawnWith = matrixFrame.rotation;
		}
		else
		{
			positionToSpawnAt = SnapToGround(GetOverflowSpawnPosition(_playerSpawnPos, 100));
			rotationToSpawnWith = Mat3.Identity;
			TraceLogger.Write("HomesteadSpawningMissionLogic", "SpawnStableMaster: stable-master tag not found — using overflow spawn.");
		}
		SpawnHomesteadAgent(null, positionToSpawnAt, rotationToSpawnWith, homestead.Party, characterObject, AgentControllerType.AI, "_villager", civilianEquipment: true, shouldSheathWeapons: true, noHorses: true);
		TraceLogger.Write("HomesteadSpawningMissionLogic", $"SpawnStableMaster: spawned '{characterObject.Name}' at ({positionToSpawnAt.x:0.##},{positionToSpawnAt.y:0.##},{positionToSpawnAt.z:0.##}).");
	}

	private void SpawnTavernKeeper()
	{
		if (homestead.HasTavernBuilding && homestead.TavernKeeperHero == null)
		{
			homestead.TryEnsureTavernKeeperHero();
		}
	}

	private void SpawnResidents()
	{
		IReadOnlyList<Hero> residentHeroes = homestead.ResidentHeroes;
		if (residentHeroes == null || residentHeroes.Count == 0)
		{
			return;
		}
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (Hero item in residentHeroes)
		{
			if (item != null && item.IsAlive && item.CharacterObject != null)
			{
				troopRoster.AddToCounts(item.CharacterObject, 1);
			}
		}
		if (troopRoster.TotalManCount > 0 && !SpawnNPCs("spawnpoint_homestead_npc", troopRoster))
		{
			SpawnNPCsAtFallbackPosition(troopRoster, _playerSpawnPos, heroesFollow: false);
		}
		TraceLogger.Write("HomesteadSpawningMissionLogic", $"SpawnResidents: spawned {troopRoster.TotalManCount} resident(s) for '{homestead.Name}'.");
	}

	internal static void EnsureCompanionDogVariantItemsRegistered()
	{
		if (_companionDogVariantsRegistered)
		{
			return;
		}
		if (Game.Current?.ObjectManager?.GetObject<ItemObject>("homestead_companion_dog_0") != null)
		{
			_companionDogVariantsRegistered = true;
			return;
		}
		string text = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "homestead_companion_dogs_" + Guid.NewGuid().ToString("N") + ".xml");
		try
		{
			File.WriteAllText(text, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Items>\n  <Item id=\"homestead_companion_dog_0\" name=\"{=homestead_companion_dog_name}Loyal Hound\"\n        mesh=\"dog\" value=\"4\" item_category=\"horse\" type=\"Horse\">\n    <ItemComponent>\n      <Horse monster=\"Monster.hog\" maneuver=\"60\" speed=\"28\" charge_damage=\"0\" body_length=\"130\">\n        <Materials><Material name=\"dog_a\" /></Materials>\n      </Horse>\n    </ItemComponent>\n    <Flags Civilian=\"true\" />\n  </Item>\n  <Item id=\"homestead_companion_dog_1\" name=\"{=homestead_companion_dog_name}Loyal Hound\"\n        mesh=\"dog\" value=\"4\" item_category=\"horse\" type=\"Horse\">\n    <ItemComponent>\n      <Horse monster=\"Monster.hog\" maneuver=\"60\" speed=\"28\" charge_damage=\"0\" body_length=\"130\">\n        <Materials><Material name=\"dog_b\" /></Materials>\n      </Horse>\n    </ItemComponent>\n    <Flags Civilian=\"true\" />\n  </Item>\n  <Item id=\"homestead_companion_dog_2\" name=\"{=homestead_companion_dog_name}Loyal Hound\"\n        mesh=\"dog\" value=\"4\" item_category=\"horse\" type=\"Horse\">\n    <ItemComponent>\n      <Horse monster=\"Monster.hog\" maneuver=\"60\" speed=\"28\" charge_damage=\"0\" body_length=\"130\">\n        <Materials><Material name=\"dog_c\" /></Materials>\n      </Horse>\n    </ItemComponent>\n    <Flags Civilian=\"true\" />\n  </Item>\n</Items>", Encoding.UTF8);
			Game.Current.ObjectManager.LoadOneXmlFromFile(text, "", skipValidation: true);
			Monster monster = Game.Current.ObjectManager.GetObject<Monster>("dog");
			if (monster != null)
			{
				BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				string[] array = new string[5] { "<Monster>k__BackingField", "_monster", "monster", "_Monster", "Monster" };
				FieldInfo fieldInfo = null;
				Type type = typeof(HorseComponent);
				while (fieldInfo == null && type != null && type != typeof(object))
				{
					string[] array2 = array;
					foreach (string name in array2)
					{
						fieldInfo = type.GetField(name, bindingAttr);
						if (fieldInfo != null)
						{
							break;
						}
					}
					if (fieldInfo == null)
					{
						fieldInfo = type.GetFields(bindingAttr).FirstOrDefault((FieldInfo f) => f.FieldType == typeof(Monster));
					}
					type = type.BaseType;
				}
				for (int num = 0; num <= 2; num++)
				{
					ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>($"homestead_companion_dog_{num}");
					if (itemObject?.HorseComponent != null && fieldInfo != null)
					{
						fieldInfo.SetValue(itemObject.HorseComponent, monster);
					}
				}
			}
			_companionDogVariantsRegistered = true;
			TraceLogger.Write("HomesteadSpawningMissionLogic", "EnsureCompanionDogVariantItemsRegistered: success.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "EnsureCompanionDogVariantItemsRegistered: failed — " + ex.GetType().Name + ": " + ex.Message);
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

	internal static void EnsureDogItemUsesCorrectMonster()
	{
		try
		{
			ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>("dog");
			if (itemObject?.HorseComponent == null)
			{
				TraceLogger.Write("HomesteadSpawningMissionLogic", "EnsureDogItemUsesCorrectMonster: dog item or HorseComponent is null.");
				return;
			}
			Monster monster = Game.Current.ObjectManager.GetObject<Monster>("dog");
			if (monster == null)
			{
				string text = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "homestead_dog_" + Guid.NewGuid().ToString("N") + ".xml");
				try
				{
					File.WriteAllText(text, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Monsters>\n  <Monster\n    id=\"dog\"\n    action_set=\"as_dog\"\n    monster_usage=\"animals\"\n    weight=\"30\"\n    hit_points=\"80\"\n    num_paces=\"5\"\n    jump_acceleration=\"0\"\n    sound_and_collision_info_class=\"boar\"\n    standing_chest_height=\"0.35\"\n    standing_pelvis_height=\"0.30\"\n    standing_eye_height=\"0.40\"\n    eye_offset_wrt_head=\"0.07, -0.05, 0.0\"\n    jump_speed_limit=\"3.5\"\n    family_type=\"5\"\n    ragdoll_bone_to_check_for_corpses_0=\"dog_root_joint\"\n    ragdoll_bone_to_check_for_corpses_1=\"dog_spine_2_joint\"\n    ragdoll_bone_to_check_for_corpses_2=\"dog_spine_3_joint\"\n    ragdoll_bone_to_check_for_corpses_3=\"dog_neck_1_joint\"\n    ragdoll_bone_to_check_for_corpses_4=\"dog_head_joint\"\n    ragdoll_fall_sound_bone_0=\"dog_root_joint\"\n    ragdoll_fall_sound_bone_1=\"dog_spine_2_joint\"\n    ragdoll_fall_sound_bone_2=\"dog_neck_1_joint\"\n    ragdoll_fall_sound_bone_3=\"dog_head_joint\"\n    head_look_direction_bone=\"dog_head_joint\"\n    thorax_look_direction_bone=\"dog_spine_3_joint\"\n    neck_root_bone=\"dog_neck_1_joint\"\n    pelvis_bone=\"dog_root_joint\"\n    right_upper_arm_bone=\"dog_right_scapula_1_joint\"\n    left_upper_arm_bone=\"dog_left_scapula_1_joint\"\n    fall_blow_damage_bone=\"dog_left_scapula_1_joint\"\n    front_bone_to_detect_ground_slope_index=\"2\"\n    back_bone_to_detect_ground_slope_index=\"4\"\n    bones_to_modify_on_sloping_ground_0=\"dog_neck_1_joint\"\n    bones_to_modify_on_sloping_ground_1=\"dog_left_scapula_joint\"\n    bones_to_modify_on_sloping_ground_2=\"dog_right_scapula_joint\"\n    bones_to_modify_on_sloping_ground_3=\"dog_left_hip_joint\"\n    bones_to_modify_on_sloping_ground_4=\"dog_right_hip_joint\"\n    body_rotation_reference_bone=\"dog_spine_3_joint\">\n    <Capsules>\n      <body_capsule radius=\"0.20\" pos1=\"0, 0.30, 0.30\" pos2=\"0, -0.28, 0.30\" />\n    </Capsules>\n    <Flags CanWander=\"true\" MoveAsHerd=\"true\" />\n  </Monster>\n</Monsters>", Encoding.UTF8);
					TraceLogger.Write("HomesteadSpawningMissionLogic", "EnsureDogItemUsesCorrectMonster: registering Monster.dog from temp file '" + text + "'");
					Game.Current.ObjectManager.LoadOneXmlFromFile(text, "", skipValidation: true);
					monster = Game.Current.ObjectManager.GetObject<Monster>("dog");
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadSpawningMissionLogic", "EnsureDogItemUsesCorrectMonster: temp-file registration failed: " + ex.Message);
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
			if (monster == null)
			{
				TraceLogger.Write("HomesteadSpawningMissionLogic", "EnsureDogItemUsesCorrectMonster: Monster.dog could not be loaded or found.");
				return;
			}
			string text2 = itemObject.HorseComponent.Monster?.StringId ?? "null";
			BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			FieldInfo fieldInfo = null;
			Type type = typeof(HorseComponent);
			string[] array = new string[5] { "<Monster>k__BackingField", "_monster", "monster", "_Monster", "Monster" };
			while (fieldInfo == null && type != null && type != typeof(object))
			{
				string[] array2 = array;
				foreach (string name in array2)
				{
					fieldInfo = type.GetField(name, bindingAttr);
					if (fieldInfo != null)
					{
						break;
					}
				}
				if (fieldInfo == null)
				{
					fieldInfo = type.GetFields(bindingAttr).FirstOrDefault((FieldInfo f) => f.FieldType == typeof(Monster));
				}
				type = type.BaseType;
			}
			if (fieldInfo == null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				Type type2 = typeof(HorseComponent);
				while (type2 != null && type2 != typeof(object))
				{
					FieldInfo[] fields = type2.GetFields(bindingAttr);
					foreach (FieldInfo fieldInfo2 in fields)
					{
						stringBuilder.Append(type2.Name + "." + fieldInfo2.Name + "(" + fieldInfo2.FieldType.Name + ") ");
					}
					type2 = type2.BaseType;
				}
				TraceLogger.Write("HomesteadSpawningMissionLogic", "EnsureDogItemUsesCorrectMonster: could not find Monster field on HorseComponent hierarchy. " + $"Dog will use '{text2}'. All fields: {stringBuilder}");
			}
			else
			{
				fieldInfo.SetValue(itemObject.HorseComponent, monster);
				TraceLogger.Write("HomesteadSpawningMissionLogic", "EnsureDogItemUsesCorrectMonster: patched " + fieldInfo.DeclaringType?.Name + "." + fieldInfo.Name + " from '" + text2 + "' to 'dog' via reflection.");
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "EnsureDogItemUsesCorrectMonster: threw " + ex2.GetType().Name + ": " + ex2.Message);
		}
	}

	private void PinAnimalAgent(Agent agent, Vec3 position, Vec2 direction)
	{
		PinnedAnimalAgent pinnedAnimalAgent = new PinnedAnimalAgent(agent, position, direction);
		pinnedAnimalAgents.Add(pinnedAnimalAgent);
		if (base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Deployment)
		{
			ApplyPinnedAnimalMovement(pinnedAnimalAgent);
		}
	}

	private void KeepPinnedAnimalsInPlace()
	{
		if (base.Mission.Mode != MissionMode.Battle && base.Mission.Mode != MissionMode.Deployment)
		{
			return;
		}
		for (int num = pinnedAnimalAgents.Count - 1; num >= 0; num--)
		{
			PinnedAnimalAgent pinnedAnimalAgent = pinnedAnimalAgents[num];
			if (!pinnedAnimalAgent.Agent.IsActive())
			{
				pinnedAnimalAgents.RemoveAt(num);
			}
			else
			{
				ApplyPinnedAnimalMovement(pinnedAnimalAgent);
			}
		}
	}

	private static void ApplyPinnedAnimalMovement(PinnedAnimalAgent pinnedAnimal)
	{
		Agent agent = pinnedAnimal.Agent;
		agent.SetMaximumSpeedLimit(0.2f, isMultiplier: false);
		agent.SetMovementDirection(in Vec2.Zero);
		agent.SetTargetPositionAndDirection(pinnedAnimal.Position.AsVec2, new Vec3(pinnedAnimal.Direction.x, pinnedAnimal.Direction.y));
	}

	private void TickAgentAnimations(Agent agent)
	{
		// One call site wraps this in try/catch, the other doesn't — guard here
		// so a freshly spawned agent without visuals can't crash either path.
		var visuals = agent?.AgentVisuals;
		var skeleton = visuals?.GetSkeleton();
		if (visuals == null || skeleton == null)
		{
			return;
		}

		for (int i = 0; i < 3; i++)
		{
			skeleton.TickAnimations(0.1f, visuals.GetGlobalFrame(), tickAnimsForChildren: true);
		}
	}

	private void SpawnNPCsAtFallbackPosition(TroopRoster roster, Vec3 anchorPos, bool heroesFollow = true)
	{
		if (!anchorPos.IsValid)
		{
			return;
		}
		List<CharacterObject> list = roster.ToFlattenedRoster().Troops.ToList();
		if (list.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (CharacterObject item in list)
		{
			Vec3 overflowSpawnPosition = GetOverflowSpawnPosition(anchorPos, num + 1);
			float groundHeightAtPosition = base.Mission.Scene.GetGroundHeightAtPosition(overflowSpawnPosition);
			if (groundHeightAtPosition < 9999f)
			{
				overflowSpawnPosition.z = groundHeightAtPosition;
			}
			SpawnHomesteadAgent(null, overflowSpawnPosition, Mat3.Identity, homestead.Party, item, AgentControllerType.AI, "_villager", civilianEquipment: true, shouldSheathWeapons: false, noHorses: true, heroesFollow);
			num++;
		}
		TraceLogger.Write("HomesteadSpawningMissionLogic", $"No spawnpoint_homestead_npc in scene — spawned {num} NPC(s) at fallback position near player spawn for '{homestead.Name}'.");
	}

	public bool IsHeroFollowing(Hero? hero)
	{
		if (hero != null)
		{
			return !_heroNonFollowers.Contains(hero);
		}
		return true;
	}

	public void SetHeroFollowing(Hero? hero, bool follow)
	{
		if (hero == null)
		{
			return;
		}
		if (follow)
		{
			_heroNonFollowers.Remove(hero);
			_staticNonFollowers.Remove(hero);
			ReleaseHeroFromPose(hero);
			return;
		}
		_heroNonFollowers.Add(hero);
		_staticNonFollowers.Add(hero);
		Agent agent = FindAgentForHero(hero);
		if (agent != null && !TryRouteHeroToFreeUsable(agent))
		{
			StopAgentInPlace(agent);
		}
	}

	private bool TryRouteHeroToFreeUsable(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		try
		{
			UsablePlace val = null;
			float num = float.MaxValue;
			Vec3 position = agent.Position;
			foreach (UsablePlace up in base.Mission.ActiveMissionObjects.OfType<UsablePlace>())
			{
				if (up == null || ((UsableMachine)(object)up).IsDisabledForAI || ((UsableMachine)(object)up).StandingPoints == null || ((UsableMachine)(object)up).StandingPoints.Count == 0)
				{
					continue;
				}
				string name = ((ScriptComponentBehavior)(object)up).GameEntity.Name;
				if ((name == null || name.IndexOf("chair_sit_auto", StringComparison.OrdinalIgnoreCase) < 0) && !_posedNpcs.Any((PosedNpc p) => p.Place == up))
				{
					float lengthSquared = (((ScriptComponentBehavior)(object)up).GameEntity.GlobalPosition - position).LengthSquared;
					if (lengthSquared < num)
					{
						num = lengthSquared;
						val = up;
					}
				}
			}
			if (val == null)
			{
				return false;
			}
			AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(MBGlobals.GetActionSetWithSuffix(agent.Monster, agent.IsFemale, "_villager"), agent.Character.GetStepSize(), hasClippingPlane: false);
			agent.SetActionSet(ref animationSystemData);
			agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator().SetTarget((UsableMachine)(object)val, false, Agent.AIScriptedFrameFlags.None);
			_posedNpcs.Add(new PosedNpc
			{
				Agent = agent,
				Place = val,
				ActionSetSuffix = "_villager",
				SpawnName = "hero"
			});
			return true;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "TryRouteHeroToFreeUsable failed: " + ex.Message);
			return false;
		}
	}

	private void ReleaseHeroFromPose(Hero hero)
	{
		Agent agent = FindAgentForHero(hero);
		if (agent == null)
		{
			return;
		}
		for (int num = _posedNpcs.Count - 1; num >= 0; num--)
		{
			if (_posedNpcs[num].Agent == agent)
			{
				_posedNpcs.RemoveAt(num);
			}
		}
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			if (component != null)
			{
				AgentNavigator agentNavigator = component.AgentNavigator;
				if (agentNavigator != null)
				{
					agentNavigator.ClearTarget();
				}
			}
		}
		catch
		{
		}
	}

	private Agent? FindAgentForHero(Hero hero)
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent.IsHuman && agent.IsActive() && (agent.Character as CharacterObject)?.HeroObject == hero)
			{
				return agent;
			}
		}
		return null;
	}

	private static void StopAgentInPlace(Agent agent)
	{
		Vec2 targetPosition = agent.Position.AsVec2;
		Vec3 f = agent.Frame.rotation.f;
		agent.SetTargetPositionAndDirection(in targetPosition, new Vec3(f.x, f.y));
	}

	private void UpdateHeroFollowers(float dt)
	{
		Agent main = Agent.Main;
		if (main == null || !main.IsActive() || _heroFollowers.Count == 0)
		{
			return;
		}
		Vec2 asVec = main.Position.AsVec2;
		if (_lastPlayerPosValid && dt > 0.0001f)
		{
			Vec2 vec = asVec - _lastPlayerPos2D;
			if (vec.Length / dt > 0.5f && vec.LengthSquared > 1E-06f)
			{
				_followAnchorDir = vec.Normalized();
			}
		}
		_lastPlayerPos2D = asVec;
		_lastPlayerPosValid = true;
		if (!_initialFormDone && _followAnchorDir.LengthSquared < 1E-06f)
		{
			Vec2 vec2 = new Vec2(main.LookDirection.x, main.LookDirection.y);
			_followAnchorDir = ((vec2.LengthSquared > 0.01f) ? vec2.Normalized() : new Vec2(0f, 1f));
		}
		if (_followAnchorDir.LengthSquared < 1E-06f)
		{
			return;
		}
		for (int num = _heroFollowers.Count - 1; num >= 0; num--)
		{
			if (!_heroFollowers[num].IsActive())
			{
				_followStates.Remove(_heroFollowers[num]);
				_heroFollowers.RemoveAt(num);
			}
		}
		_followBuffer.Clear();
		foreach (Agent heroFollower in _heroFollowers)
		{
			Hero hero = (heroFollower.Character as CharacterObject)?.HeroObject;
			if (hero == null || !_heroNonFollowers.Contains(hero))
			{
				_followBuffer.Add(heroFollower);
			}
		}
		int count = _followBuffer.Count;
		if (count == 0)
		{
			return;
		}
		float num2 = TaleWorlds.Library.MathF.Atan2(0f - _followAnchorDir.y, 0f - _followAnchorDir.x);
		float num3 = TaleWorlds.Library.MathF.PI * 2f / 3f;
		for (int i = 0; i < count; i++)
		{
			Agent agent = _followBuffer[i];
			FollowerState followerState = GetFollowerState(agent);
			int num4 = i / 4;
			int num5 = i % 4;
			int num6 = Math.Min(4, count - num4 * 4);
			float num7 = 2f + (float)num4 * 1.3f;
			float num8 = ((num6 > 1) ? (((float)num5 / (float)(num6 - 1) - 0.5f) * num3) : 0f);
			float x = num2 + num8;
			Vec2 targetPosition = asVec + new Vec2(TaleWorlds.Library.MathF.Cos(x), TaleWorlds.Library.MathF.Sin(x)) * num7;
			Vec2 asVec2 = agent.Position.AsVec2;
			float length = (asVec2 - asVec).Length;
			float length2 = (asVec2 - targetPosition).Length;
			if (!followerState.Active)
			{
				if (!_initialFormDone && length2 > 1.2f)
				{
					followerState.Active = true;
					followerState.StuckTimer = 0f;
					followerState.LastPos = asVec2;
				}
				else if (length > 3.5f)
				{
					followerState.Grace += dt;
					if (followerState.Grace >= 3f)
					{
						followerState.Active = true;
						followerState.StuckTimer = 0f;
						followerState.LastPos = asVec2;
					}
				}
				else
				{
					followerState.Grace = 0f;
				}
				if (!followerState.Active)
				{
					continue;
				}
			}
			if (length2 <= 1.2f)
			{
				followerState.Active = false;
				followerState.Grace = 0f;
				followerState.StuckTimer = 0f;
				continue;
			}
			Vec2 vec3 = (targetPosition - asVec2).Normalized();
			agent.SetTargetPositionAndDirection(in targetPosition, new Vec3(vec3.x, vec3.y));
			if (((dt > 0.0001f) ? ((asVec2 - followerState.LastPos).Length / dt) : 0f) < 0.3f)
			{
				followerState.StuckTimer += dt;
				if (followerState.StuckTimer >= 2.5f)
				{
					TeleportFollowerToSlot(agent, targetPosition);
					followerState.Active = false;
					followerState.Grace = 0f;
					followerState.StuckTimer = 0f;
				}
			}
			else
			{
				followerState.StuckTimer = 0f;
			}
			followerState.LastPos = asVec2;
		}
		_initialFormDone = true;
	}

	private FollowerState GetFollowerState(Agent follower)
	{
		if (!_followStates.TryGetValue(follower, out FollowerState value))
		{
			value = new FollowerState
			{
				LastPos = follower.Position.AsVec2
			};
			_followStates[follower] = value;
		}
		return value;
	}

	private void TeleportFollowerToSlot(Agent follower, Vec2 slot)
	{
		try
		{
			WorldPosition worldPosition = follower.GetWorldPosition();
			worldPosition.SetVec2(slot);
			if (worldPosition.IsValid)
			{
				follower.TeleportToPosition(worldPosition.GetGroundVec3());
			}
			else
			{
				follower.TeleportToPosition(new Vec3(slot.x, slot.y, follower.Position.z));
			}
			TraceLogger.Write("HomesteadSpawningMissionLogic", "Unstuck follower '" + follower.Character?.StringId + "' → teleported to formation slot.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawningMissionLogic", "TeleportFollowerToSlot threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private GameEntity? FindChildWithTagRecursive(GameEntity parent, string tag)
	{
		if (parent.HasTag(tag))
		{
			return parent;
		}
		foreach (GameEntity child in parent.GetChildren())
		{
			GameEntity gameEntity = FindChildWithTagRecursive(child, tag);
			if (gameEntity != null)
			{
				return gameEntity;
			}
		}
		return null;
	}

	private MatrixFrame CalculateGlobalFrame(GameEntity entity)
	{
		if (entity.Parent == null)
		{
			return entity.GetFrame();
		}
		return CalculateGlobalFrame(entity.Parent) * entity.GetFrame();
	}
}
