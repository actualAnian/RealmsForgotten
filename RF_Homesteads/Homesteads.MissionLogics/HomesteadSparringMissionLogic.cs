using System;
using System.Collections.Generic;
using System.Linq;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Homesteads.MissionLogics;

public class HomesteadSparringMissionLogic : MissionLogic
{
	private readonly Homestead? _homestead;

	private readonly Settlement? _settlement;

	private readonly Hero? _armsMaster;

	private readonly int _teamSize;

	private readonly List<Agent> _audienceAgents = new List<Agent>();

	private static readonly string[] _cheerAnimations = new string[3] { "act_cheer_1", "act_cheer_2", "act_cheer_3" };

	private const uint SparTeamBlue = 4280179400u;

	private const uint SparTeamRed = 4291312682u;

	private float _timeSinceStart;

	private bool _weaponsWielded;

	private bool _matchFinished;

	private bool _playerWon;

	private int _playerSideTotal;

	private int _enemySideTotal;

	private Vec3 _sceneCenter = new Vec3(200f, 200f);

	private const string PlayerSpawnTagPrefix = "sp_skirmish_player";

	private const string EnemySpawnTagPrefix = "sp_skirmish_enemy";

	private const string AudienceTag = "sp_skirmish_audience";

	private const string PropTag = "homestead_skirmish_prop";

	private const int MaxSpawnPoints = 5;

	private readonly SkirmishMap _map = SkirmishMaps.Default;

	private GameEntity? _mapEntity;

	private bool _mapPlaced;

	private int _settleFrames;

	private bool _spawned;

	private readonly Dictionary<Agent, int> _damageDealt = new Dictionary<Agent, int>();

	private readonly Dictionary<Agent, int> _damageTaken = new Dictionary<Agent, int>();

	private readonly Dictionary<Hero, MobileParty?> _heroPartyBackup = new Dictionary<Hero, MobileParty>();

	private Hero? _homesteadLeaderAtStart;

	private readonly TroopRoster? _playerRoster;

	private readonly TroopRoster? _enemyRoster;

	private float _endMatchTimer = -1f;

	public int GetDamageDealtBy(Agent agent)
	{
		if (agent == null || !_damageDealt.TryGetValue(agent, out var value))
		{
			return 0;
		}
		return value;
	}

	public int GetDamageTakenBy(Agent agent)
	{
		if (agent == null || !_damageTaken.TryGetValue(agent, out var value))
		{
			return 0;
		}
		return value;
	}

	public HomesteadSparringMissionLogic(Homestead homestead, int teamSize, TroopRoster? playerRoster = null, TroopRoster? enemyRoster = null)
	{
		_homestead = homestead;
		_teamSize = Math.Max(1, teamSize);
		_playerRoster = playerRoster;
		_enemyRoster = enemyRoster;
	}

	public HomesteadSparringMissionLogic(Settlement settlement, Hero armsMaster, int teamSize, TroopRoster? playerRoster = null, TroopRoster? enemyRoster = null)
	{
		_settlement = settlement;
		_armsMaster = armsMaster;
		_teamSize = Math.Max(1, teamSize);
		_playerRoster = playerRoster;
		_enemyRoster = enemyRoster;
	}

	public override void AfterStart()
	{
		base.Mission.SetMissionMode(MissionMode.Tournament, atStart: true);
		_homesteadLeaderAtStart = _homestead?.Leader ?? _settlement?.Town?.Governor;
		InitializeTeams();
	}

	private void TickLoading(float dt)
	{
		if (!_mapPlaced)
		{
			try
			{
				TraceLogger.Write("HomesteadSparringMissionLogic", $"TickLoading: instantiating skirmish map '{_map.PrefabName}' at ({_map.Anchor.x:0.#},{_map.Anchor.y:0.#},{_map.Anchor.z:0.#}).");
				_mapEntity = Utils.CreateGameEntityWithPrefab(_map.PrefabName, _map.Anchor, Mat3.Identity);
				_sceneCenter = _map.Anchor;
				_mapPlaced = true;
				return;
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSparringMissionLogic", "Failed to instantiate skirmish map '" + _map.PrefabName + "': " + ex.Message);
				AbortSparring("The sparring ground could not be loaded.");
				return;
			}
		}
		if (_settleFrames++ >= 5)
		{
			SpawnSkirmishProps();
			SpawnCombatants();
			SpawnAudience();
			_spawned = true;
		}
	}

	private void AbortSparring(string reason)
	{
		MBInformationManager.AddQuickInformation(new TextObject(reason));
		TraceLogger.Write("HomesteadSparringMissionLogic", "Sparring aborted: " + reason);
		Mission.Current.EndMission();
	}

	private static List<GameEntity> GetOrderedSpawnPoints(string tagPrefix, int max)
	{
		List<GameEntity> list = new List<GameEntity>();
		for (int i = 0; i < max; i++)
		{
			List<GameEntity> list2 = Mission.Current.Scene.FindEntitiesWithTag($"{tagPrefix}_{i:D3}").ToList();
			if (list2.Count == 0)
			{
				break;
			}
			list.Add(list2[0]);
		}
		return list;
	}

	private void SpawnSkirmishProps()
	{
		try
		{
			List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("homestead_skirmish_prop").ToList();
			int num = 0;
			foreach (GameEntity item in list)
			{
				try
				{
					string name = item.Name;
					if (!string.IsNullOrEmpty(name))
					{
						MatrixFrame globalFrame = item.GetGlobalFrame();
						Utils.CreateGameEntityWithPrefab(name, globalFrame.origin, globalFrame.rotation);
						num++;
					}
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadSparringMissionLogic", "Skirmish prop instantiate failed: " + ex.Message);
				}
			}
			TraceLogger.Write("HomesteadSparringMissionLogic", $"SpawnSkirmishProps: placed {num}/{list.Count} prop(s).");
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadSparringMissionLogic", "SpawnSkirmishProps failed: " + ex2.Message);
		}
	}

	protected override void OnEndMission()
	{
		RestoreHeroParties();
		if (_homestead != null)
		{
			HomesteadBehavior.Instance?.ScheduleReturnToHomestead(_homestead);
		}
		else
		{
			HomesteadBehavior.SparringMissionActive = false;
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (!(affectedAgent?.Character is CharacterObject { IsHero: not false, HeroObject: not null } characterObject) || characterObject.HeroObject == Hero.MainHero)
		{
			return;
		}
		Hero heroObject = characterObject.HeroObject;
		if (heroObject == _homesteadLeaderAtStart)
		{
			try
			{
				heroObject.HitPoints = Math.Max(1, heroObject.CharacterObject.MaxHitPoints());
				return;
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSparringMissionLogic", $"OnAgentRemoved HP-only heal failed for leader {heroObject.Name}: {ex.Message}");
				return;
			}
		}
		if (!_heroPartyBackup.TryGetValue(heroObject, out MobileParty value) || value == null || !value.IsActive || value.IsDisbanding)
		{
			return;
		}
		try
		{
			heroObject.HitPoints = Math.Max(1, heroObject.CharacterObject.MaxHitPoints());
			if (heroObject.PartyBelongedTo != value)
			{
				AddHeroToPartyAction.Apply(heroObject, value, showNotification: false);
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadSparringMissionLogic", $"OnAgentRemoved restore failed for {heroObject.Name}: {ex2.Message}");
		}
	}

	private void RestoreHeroParties()
	{
		Hero homesteadLeaderAtStart = _homesteadLeaderAtStart;
		MobileParty mobileParty = _homestead?.MobileParty ?? _settlement?.Town?.GarrisonParty;
		if (homesteadLeaderAtStart != null && mobileParty != null)
		{
			try
			{
				homesteadLeaderAtStart.HitPoints = homesteadLeaderAtStart.CharacterObject.MaxHitPoints();
				if (homesteadLeaderAtStart.PartyBelongedTo != mobileParty)
				{
					AddHeroToPartyAction.Apply(homesteadLeaderAtStart, mobileParty, showNotification: false);
				}
				TroopRoster memberRoster = mobileParty.MemberRoster;
				int num = 0;
				while (num++ < 20)
				{
					int num2 = 0;
					foreach (TroopRosterElement item in memberRoster.GetTroopRoster())
					{
						if (item.Character == homesteadLeaderAtStart.CharacterObject)
						{
							num2 += item.Number;
						}
					}
					if (num2 > 1)
					{
						memberRoster.AddToCounts(homesteadLeaderAtStart.CharacterObject, -1);
						continue;
					}
					break;
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSparringMissionLogic", $"RestoreHeroParties failed for leader {homesteadLeaderAtStart.Name}: {ex.Message}");
			}
		}
		foreach (KeyValuePair<Hero, MobileParty> item2 in _heroPartyBackup.ToList())
		{
			Hero key = item2.Key;
			MobileParty value = item2.Value;
			if (key == null || key == Hero.MainHero)
			{
				continue;
			}
			try
			{
				key.HitPoints = key.CharacterObject.MaxHitPoints();
				if (value != null && value.IsActive && !value.IsDisbanding && key.PartyBelongedTo != value)
				{
					AddHeroToPartyAction.Apply(key, value, showNotification: false);
				}
				if (value == null)
				{
					continue;
				}
				TroopRoster memberRoster2 = value.MemberRoster;
				int num3 = 0;
				while (num3++ < 20)
				{
					int num4 = 0;
					foreach (TroopRosterElement item3 in memberRoster2.GetTroopRoster())
					{
						if (item3.Character == key.CharacterObject)
						{
							num4 += item3.Number;
						}
					}
					if (num4 > 1)
					{
						memberRoster2.AddToCounts(key.CharacterObject, -1);
						continue;
					}
					break;
				}
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("HomesteadSparringMissionLogic", $"RestoreHeroParties failed for {key.Name}: {ex2.Message}");
			}
		}
		_heroPartyBackup.Clear();
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		if (!_matchFinished)
		{
			return false;
		}
		missionResult = (_playerWon ? MissionResult.CreateSuccessful(base.Mission) : MissionResult.CreateDefeated(base.Mission));
		return true;
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		return null;
	}

	private void InitializeTeams()
	{
		Banner banner = Hero.MainHero?.Clan?.Banner;
		uint color = Hero.MainHero?.Clan?.Color ?? 255;
		uint color2 = Hero.MainHero?.Clan?.Color2 ?? 16777215;
		Team team = base.Mission.Teams.Add(BattleSideEnum.Defender, color, color2, banner);
		Team team2 = base.Mission.Teams.Add(BattleSideEnum.Attacker, 4289344273u);
		team.SetIsEnemyOf(team2, isEnemyOf: true);
		team2.SetIsEnemyOf(team, isEnemyOf: true);
		base.Mission.PlayerTeam = team;
	}

	private void SpawnCombatants()
	{
		List<GameEntity> orderedSpawnPoints = GetOrderedSpawnPoints("sp_skirmish_player", 5);
		List<GameEntity> orderedSpawnPoints2 = GetOrderedSpawnPoints("sp_skirmish_enemy", 5);
		Vec3 vec = ((orderedSpawnPoints.Count > 0) ? orderedSpawnPoints[0].GlobalPosition : (_sceneCenter + new Vec3(8f)));
		Vec3 vec2 = ((orderedSpawnPoints2.Count > 0) ? orderedSpawnPoints2[0].GlobalPosition : (_sceneCenter + new Vec3(-8f)));
		Vec2 facing = ToFacing(vec2 - vec, new Vec2(-1f, 0f));
		Vec2 facing2 = ToFacing(vec - vec2, new Vec2(1f, 0f));
		Vec3 spawnPosition = GetSpawnPosition(orderedSpawnPoints, 0, vec);
		Agent agent = SpawnCombatAgent(CharacterObject.PlayerCharacter, spawnPosition, isPlayerSide: true, facing);
		base.Mission.MainAgent = agent;
		agent.Controller = AgentControllerType.Player;
		_playerSideTotal = 1;
		if (_teamSize > 1)
		{
			List<CharacterObject> list = ((_playerRoster != null) ? (from t in _playerRoster.GetTroopRoster()
				where t.Character != null && t.Character != CharacterObject.PlayerCharacter
				select t).SelectMany((TroopRosterElement t) => Enumerable.Repeat(t.Character, t.Number)).Take(_teamSize - 1).ToList() : GetBestTroops(MobileParty.MainParty?.MemberRoster, _teamSize - 1));
			for (int num = 0; num < list.Count; num++)
			{
				Vec3 spawnPosition2 = GetSpawnPosition(orderedSpawnPoints, num + 1, spawnPosition + new Vec3(0f, num + 1));
				SpawnCombatAgent(list[num], spawnPosition2, isPlayerSide: true, facing);
			}
			_playerSideTotal += list.Count;
		}
		List<CharacterObject> list2 = ((_enemyRoster != null) ? (from t in _enemyRoster.GetTroopRoster()
			where t.Character != null && t.Character != CharacterObject.PlayerCharacter
			select t).SelectMany((TroopRosterElement t) => Enumerable.Repeat(t.Character, t.Number)).Take(_teamSize).ToList() : GetBestTroops(SafeHomesteadRoster(), _teamSize));
		while (_enemyRoster == null && list2.Count < _teamSize)
		{
			CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("imperial_recruit");
			if (characterObject == null)
			{
				break;
			}
			list2.Add(characterObject);
		}
		for (int num2 = 0; num2 < list2.Count; num2++)
		{
			Vec3 spawnPosition3 = GetSpawnPosition(orderedSpawnPoints2, num2, vec2 + new Vec3(0f, num2));
			SpawnCombatAgent(list2[num2], spawnPosition3, isPlayerSide: false, facing2);
		}
		_enemySideTotal = list2.Count;
	}

	private static Vec2 ToFacing(Vec3 direction, Vec2 fallback)
	{
		Vec2 asVec = direction.AsVec2;
		if (!(asVec.LengthSquared > 0.0001f))
		{
			return fallback;
		}
		return asVec.Normalized();
	}

	private static Vec3 GetSpawnPosition(List<GameEntity> spawns, int index, Vec3 fallbackPos)
	{
		if (index >= spawns.Count)
		{
			return fallbackPos;
		}
		return spawns[index].GlobalPosition;
	}

	private static List<CharacterObject> GetBestTroops(TroopRoster? roster, int count)
	{
		List<CharacterObject> list = new List<CharacterObject>();
		if (roster == null || count <= 0)
		{
			return list;
		}
		List<TroopRosterElement> list2 = (from t in roster.GetTroopRoster()
			where t.Character != null && !t.Character.IsHero && t.Character.IsSoldier
			orderby t.Character.Level descending
			select t).ToList();
		int num = 0;
		foreach (TroopRosterElement item in list2)
		{
			int num2 = Math.Min(item.Number - item.WoundedNumber, count - num);
			for (int num3 = 0; num3 < num2; num3++)
			{
				list.Add(item.Character);
			}
			num += num2;
			if (num >= count)
			{
				break;
			}
		}
		return list;
	}

	private TroopRoster? SafeHomesteadRoster()
	{
		try
		{
			return _homestead?.Troops ?? _settlement?.Town?.GarrisonParty?.MemberRoster;
		}
		catch
		{
			return null;
		}
	}

	private void SpawnAudience()
	{
		List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("sp_skirmish_audience").ToList();
		if (list.Count == 0)
		{
			return;
		}
		List<CharacterObject> list2 = new List<CharacterObject>();
		TroopRoster troopRoster = SafeHomesteadRoster();
		if (troopRoster != null)
		{
			foreach (TroopRosterElement item in troopRoster.GetTroopRoster())
			{
				if (item.Character != null && !item.Character.IsHero && !list2.Contains(item.Character))
				{
					list2.Add(item.Character);
				}
			}
		}
		if (list2.Count == 0)
		{
			CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("imperial_recruit");
			if (characterObject != null)
			{
				list2.Add(characterObject);
			}
		}
		if (list2.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (GameEntity item2 in list)
		{
			CharacterObject character = list2[MBRandom.RandomInt(list2.Count)];
			MatrixFrame globalFrame = item2.GetGlobalFrame();
			Agent agent = SpawnAudienceAgent(character, globalFrame.origin);
			_audienceAgents.Add(agent);
			if (num < 80)
			{
				num++;
				TraceLogger.Write("HomesteadSparringMissionLogic", $"SpawnAudience: point baked=({globalFrame.origin.x:0.##},{globalFrame.origin.y:0.##},{globalFrame.origin.z:0.##}) " + $"agentPos=({agent.Position.x:0.##},{agent.Position.y:0.##},{agent.Position.z:0.##}).");
			}
			Vec3 vec = _sceneCenter - globalFrame.origin;
			if (vec.LengthSquared > 0.0001f)
			{
				agent.LookDirection = vec.NormalizedCopy();
			}
			agent.Controller = AgentControllerType.None;
		}
	}

	private static Equipment BuildPracticeEquipment(CharacterObject character)
	{
		Equipment equipment = character.HeroObject?.BattleEquipment ?? character.Equipment;
		Equipment equipment2 = new Equipment();
		if (equipment != null)
		{
			equipment2.FillFrom(equipment, useSourceEquipmentType: false);
		}
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex <= EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
		{
			equipment2[equipmentIndex] = default(EquipmentElement);
		}
		equipment2[EquipmentIndex.ArmorItemEndSlot] = default(EquipmentElement);
		equipment2[EquipmentIndex.HorseHarness] = default(EquipmentElement);
		ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>("empire_sword_1_t2_blunt");
		ItemObject itemObject2 = MBObjectManager.Instance.GetObject<ItemObject>("oval_shield");
		if (itemObject != null)
		{
			equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(itemObject));
		}
		if (itemObject2 != null)
		{
			equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, new EquipmentElement(itemObject2));
		}
		return equipment2;
	}

	private Agent SpawnCombatAgent(CharacterObject character, Vec3 position, bool isPlayerSide, Vec2 facing)
	{
		BattleSideEnum side = ((!isPlayerSide) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		Team team = base.Mission.Teams.FirstOrDefault((Team t) => t.Side == side) ?? base.Mission.Teams.Add(side, isPlayerSide ? 255u : 16711680u, 16777215u);
		Vec3 position2 = new Vec3(position.x, position.y, position.z + 0.1f);
		Vec2 direction = ((facing == Vec2.Zero) ? Vec2.Forward : facing);
		if (character.IsHero && character.HeroObject != null && character.HeroObject != Hero.MainHero && character.HeroObject != _homesteadLeaderAtStart && !_heroPartyBackup.ContainsKey(character.HeroObject))
		{
			_heroPartyBackup[character.HeroObject] = character.HeroObject.PartyBelongedTo;
		}
		uint color = (isPlayerSide ? 4280179400u : 4291312682u);
		AgentBuildData agentBuildData = new AgentBuildData(character).Team(team).InitialPosition(in position2).InitialDirection(in direction)
			.NoHorses(noHorses: true)
			.ClothingColor1(color)
			.ClothingColor2(color)
			.Equipment(BuildPracticeEquipment(character))
			.FixedEquipment(fixedEquipment: true);
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		agent.Controller = ((character.HeroObject == null || character.HeroObject != Hero.MainHero) ? AgentControllerType.AI : AgentControllerType.Player);
		agent.SetWatchState(Agent.WatchState.Alarmed);
		return agent;
	}

	private Agent SpawnAudienceAgent(CharacterObject character, Vec3 position)
	{
		Vec3 position2 = new Vec3(position.x, position.y, position.z + 0.1f);
		AgentBuildData agentBuildData = new AgentBuildData(character).InitialPosition(in position2).InitialDirection(in Vec2.Forward).NoHorses(noHorses: true);
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		agent.Controller = AgentControllerType.None;
		try
		{
			agent.TeleportToPosition(position2);
		}
		catch
		{
		}
		agent.SetMortalityState(Agent.MortalityState.Invulnerable);
		return agent;
	}

	public override void OnMissionTick(float dt)
	{
		if (!_spawned)
		{
			TickLoading(dt);
			return;
		}
		_timeSinceStart += dt;
		if (!_weaponsWielded && _timeSinceStart > 0.5f)
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent.IsHuman)
				{
					if (agent.Controller != AgentControllerType.None)
					{
						agent.WieldInitialWeapons(Agent.WeaponWieldActionType.Instant);
						continue;
					}
					agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
					agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
					string actName = _cheerAnimations[MBRandom.RandomInt(_cheerAnimations.Length)];
					agent.SetActionChannel(0, ActionIndexCache.Create(actName), ignorePriority: true, (AnimFlags)0uL);
				}
			}
			_weaponsWielded = true;
		}
		if (!_matchFinished)
		{
			CheckMatchEnd();
			return;
		}
		if (_endMatchTimer < 0f)
		{
			_endMatchTimer = 3.5f;
			return;
		}
		_endMatchTimer -= dt;
		if (_endMatchTimer <= 0f)
		{
			Mission.Current.EndMission();
			_endMatchTimer = 9999f;
		}
	}

	private void CountStanding(out int playerCount, out int enemyCount)
	{
		playerCount = 0;
		enemyCount = 0;
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.IsHuman && agent.Health > 0f && agent.Team != null)
			{
				if (agent.Team.Side == BattleSideEnum.Defender)
				{
					playerCount++;
				}
				else if (agent.Team.Side == BattleSideEnum.Attacker)
				{
					enemyCount++;
				}
			}
		}
	}

	private void CheckMatchEnd()
	{
		if (!(_timeSinceStart < 3f))
		{
			CountStanding(out var playerCount, out var enemyCount);
			if (enemyCount == 0)
			{
				FinishMatch(playerWon: true);
			}
			else if (playerCount == 0)
			{
				FinishMatch(playerWon: false);
			}
		}
	}

	private void FinishMatch(bool playerWon)
	{
		_matchFinished = true;
		_playerWon = playerWon;
		BattleSideEnum battleSideEnum = ((!playerWon) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		base.Mission.GetMissionBehavior<AgentVictoryLogic>()?.SetTimersOfVictoryReactionsOnBattleEnd(battleSideEnum);
		MBInformationManager.AddQuickInformation(new TextObject(playerWon ? "Sparring Match: your side wins!" : "Sparring Match: the homestead side takes it."));
		HashSet<Agent> hashSet = new HashSet<Agent>(_damageDealt.Keys);
		foreach (Agent key in _damageTaken.Keys)
		{
			hashSet.Add(key);
		}
		foreach (Agent item in hashSet)
		{
			((item.Character as CharacterObject)?.HeroObject)?.AddSkillXp(DefaultSkills.Athletics, 250f);
		}
		if (_homestead != null)
		{
			if (_teamSize == 1)
			{
				if (playerWon)
				{
					_homestead.SparringWins1v1++;
				}
				else
				{
					_homestead.SparringLosses1v1++;
				}
			}
			else if (playerWon)
			{
				_homestead.SparringWins5v5++;
			}
			else
			{
				_homestead.SparringLosses5v5++;
			}
		}
		try
		{
			Hero hero = _homestead?.ArmsMasterHero ?? _armsMaster;
			if (hero != null && hero.IsAlive)
			{
				ChangeRelationAction.ApplyPlayerRelation(hero, 1);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSparringMissionLogic", "Relation award failed: " + ex.Message);
		}
		List<Hero> list = new List<Hero>();
		List<Hero> list2 = new List<Hero>();
		foreach (Agent item2 in hashSet)
		{
			Hero hero2 = (item2.Character as CharacterObject)?.HeroObject;
			if (hero2 != null && item2.Team != null)
			{
				if (item2.Team.Side == battleSideEnum)
				{
					list.Add(hero2);
				}
				else
				{
					list2.Add(hero2);
				}
			}
		}
		HomesteadBehavior.Instance?.SetLastSparringResult(playerWon);
		HomesteadBehavior.Instance?.RecordSparringMatch(list, list2);
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		if (isBlocked || isSiegeEngineHit || affectorAgent == null || affectedAgent == null || attackerWeapon == null)
		{
			return;
		}
		int inflictedDamage = blow.InflictedDamage;
		if (inflictedDamage > 0)
		{
			_damageDealt[affectorAgent] = (_damageDealt.TryGetValue(affectorAgent, out var value) ? value : 0) + inflictedDamage;
			_damageTaken[affectedAgent] = (_damageTaken.TryGetValue(affectedAgent, out var value2) ? value2 : 0) + inflictedDamage;
		}
		CharacterObject characterObject = affectorAgent.Character as CharacterObject;
		Hero hero = characterObject?.HeroObject;
		if (hero == null || !(affectedAgent.Character is CharacterObject attackedTroop))
		{
			return;
		}
		try
		{
			CombatXpModel combatXpModel = Campaign.Current.Models.CombatXpModel;
			bool isFatal = affectedAgent.Health <= 0f;
			int num = (int)combatXpModel.GetXpFromHit(characterObject, null, attackedTroop, hero.PartyBelongedTo?.Party ?? PartyBase.MainParty, blow.InflictedDamage, isFatal, CombatXpModel.MissionTypeEnum.Tournament).ResultNumber;
			if (num > 0)
			{
				SkillObject skillForWeapon = combatXpModel.GetSkillForWeapon(attackerWeapon, isSiegeEngineHit: false);
				if (skillForWeapon != null)
				{
					hero.AddSkillXp(skillForWeapon, num);
				}
			}
		}
		catch
		{
		}
	}
}
