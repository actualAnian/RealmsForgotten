using System;
using System.Collections.Generic;
using System.Linq;
using Homesteads.Models;
using SandBox;
using SandBox.Objects;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

public class HomesteadTavernMissionLogic : MissionLogic
{
	private readonly Homestead _homestead;

	private Vec3 _doorPos = Vec3.Invalid;

	private bool _spawned;

	private float _floorZ = float.MinValue;

	public static Agent? PendingMercFadeAgent;

	private readonly List<UsableMachine> _tavernSeats = new List<UsableMachine>();

	private int _seatCursor;

	public static Agent? TavernKeeperAgent { get; private set; }

	public HomesteadTavernMissionLogic(Homestead homestead)
	{
		_homestead = homestead;
	}

	public override void AfterStart()
	{
		base.Mission.SetMissionMode(MissionMode.StartUp, atStart: true);
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		return null;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (!_spawned)
		{
			try
			{
				SpawnEveryone();
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadTavernMissionLogic", "SpawnEveryone failed: " + ex.GetType().Name + ": " + ex.Message);
			}
			_spawned = true;
		}
		if (PendingMercFadeAgent == null || base.Mission.Mode == MissionMode.Conversation)
		{
			return;
		}
		Agent pendingMercFadeAgent = PendingMercFadeAgent;
		PendingMercFadeAgent = null;
		try
		{
			if (pendingMercFadeAgent.IsActive())
			{
				pendingMercFadeAgent.FadeOut(hideInstantly: false, hideMount: true);
			}
		}
		catch
		{
		}
	}

	private void SpawnEveryone()
	{
		GameEntity gameEntity = FindFirstTaggedEntity("spawnpoint_player", "spawnpoint_player_outside", "sp_player_conversation");
		Mat3 mat = Mat3.Identity;
		Vec3 vec;
		if (gameEntity != null)
		{
			MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
			vec = globalFrame.origin;
			mat = globalFrame.rotation;
			TraceLogger.Write("HomesteadTavernMissionLogic", "Player spawn from entity '" + gameEntity.Name + "'.");
		}
		else
		{
			Vec3 centerPosition = Vec3.Invalid;
			base.Mission.Scene.GetNavMeshCenterPosition(0, ref centerPosition);
			vec = (centerPosition.IsValid ? centerPosition : new Vec3(0f, 0f, 0f, -1f));
			TraceLogger.Write("HomesteadTavernMissionLogic", "No tavern player-spawn tag found — using navmesh centre.");
		}
		_doorPos = vec;
		_floorZ = vec.z;
		Agent agent = SpawnAgentFor(CharacterObject.PlayerCharacter, vec, mat.f.AsVec2, isPlayer: true);
		base.Mission.MainAgent = agent;
		agent.Controller = AgentControllerType.Player;
		List<GameEntity> npcSpots = CollectTaggedEntities("npc_common", "npc_common_limited", "sp_npc").ToList();
		int spotCursor = 0;
		Hero tavernKeeperHero = _homestead.TavernKeeperHero;
		if (tavernKeeperHero == null)
		{
			_homestead.TryEnsureTavernKeeperHero();
			tavernKeeperHero = _homestead.TavernKeeperHero;
		}
		if (tavernKeeperHero != null && tavernKeeperHero.IsAlive && tavernKeeperHero.CharacterObject != null)
		{
			GameEntity gameEntity2 = FindFirstTaggedEntity("spawnpoint_tavernkeeper");
			Vec3 vec2 = ((gameEntity2 != null) ? gameEntity2.GetGlobalFrame().origin : NextSpot());
			Agent agent2 = SpawnAgentFor(tavernKeeperHero.CharacterObject, vec2, FaceToward(vec2, vec), isPlayer: false);
			ApplyActionSet(agent2, "_tavern_keeper");
			TavernKeeperAgent = agent2;
		}
		CollectTavernSeats();
		int num = 0;
		foreach (Hero accompanyingPartyHero in GetAccompanyingPartyHeroes())
		{
			if (num >= 10)
			{
				break;
			}
			Vec3 vec3 = NextSpot();
			Agent agent3 = SpawnAgentFor(accompanyingPartyHero.CharacterObject, vec3, FaceToward(vec3, vec), isPlayer: false);
			ApplyActionSet(agent3, "_villager_in_tavern");
			SeatPatron(agent3);
			num++;
		}
		IReadOnlyList<Hero> residentHeroes = _homestead.ResidentHeroes;
		if (residentHeroes != null)
		{
			foreach (Hero item in residentHeroes)
			{
				if (num >= 10)
				{
					break;
				}
				if (item != null && item.IsAlive && item.CharacterObject != null)
				{
					Vec3 vec4 = NextSpot();
					Agent agent4 = SpawnAgentFor(item.CharacterObject, vec4, FaceToward(vec4, vec), isPlayer: false);
					ApplyActionSet(agent4, "_villager_in_tavern");
					SeatPatron(agent4);
					num++;
				}
			}
		}
		CultureObject cultureObject = _homestead.Leader?.Culture ?? Hero.MainHero?.Culture;
		CharacterObject characterObject = ResolveMusicianCharacter();
		if (characterObject != null)
		{
			GameEntity gameEntity3 = FindFirstTaggedEntity("musician");
			Vec3 vec5 = ((gameEntity3 != null) ? gameEntity3.GetGlobalFrame().origin : NextSpot());
			Agent agent5 = SpawnAgentFor(characterObject, vec5, FaceToward(vec5, vec), isPlayer: false);
			ApplyActionSet(agent5, "_musician");
			PoseAgent(agent5, "act_musician_idle_stand_active");
		}
		CharacterObject characterObject2 = cultureObject?.TavernWench;
		if (characterObject2 != null)
		{
			GameEntity gameEntity4 = FindFirstTaggedEntity("sp_tavern_wench");
			Vec3 vec6 = ((gameEntity4 != null) ? gameEntity4.GetGlobalFrame().origin : NextSpot());
			Agent agent6 = SpawnAgentFor(characterObject2, vec6, FaceToward(vec6, vec), isPlayer: false);
			ApplyActionSet(agent6, "_barmaid");
		}
		CharacterObject characterObject3 = cultureObject?.RansomBroker;
		if (characterObject3 != null)
		{
			GameEntity gameEntity5 = FindFirstTaggedEntity("sp_ransom_broker");
			Vec3 vec7 = ((gameEntity5 != null) ? gameEntity5.GetGlobalFrame().origin : NextSpot());
			Agent agent7 = SpawnAgentFor(characterObject3, vec7, FaceToward(vec7, vec), isPlayer: false);
			ApplyActionSet(agent7, "_villager_in_tavern");
			SeatPatron(agent7);
		}
		if (_homestead.AvailableMercenaryCount > 0 && !string.IsNullOrEmpty(_homestead.AvailableMercenaryTypeId))
		{
			CharacterObject characterObject4 = Game.Current.ObjectManager.GetObject<CharacterObject>(_homestead.AvailableMercenaryTypeId);
			if (characterObject4 != null)
			{
				GameEntity gameEntity6 = FindFirstTaggedEntity("sp_mercenary");
				Vec3 vec8 = ((gameEntity6 != null) ? gameEntity6.GetGlobalFrame().origin : NextSpot());
				Agent agent8 = SpawnAgentFor(characterObject4, vec8, FaceToward(vec8, vec), isPlayer: false);
				ApplyActionSet(agent8, "_warrior_in_tavern");
				SeatPatron(agent8);
			}
		}
		EnableNativeExitDoors();
		TraceLogger.Write("HomesteadTavernMissionLogic", $"Tavern populated for '{_homestead.Name}': keeper={tavernKeeperHero != null} patrons={num} seats={_tavernSeats.Count}.");
		Vec3 NextSpot()
		{
			if (npcSpots.Count == 0)
			{
				return ScatterNearDoor(spotCursor++);
			}
			GameEntity gameEntity7 = npcSpots[spotCursor % npcSpots.Count];
			spotCursor++;
			return gameEntity7.GetGlobalFrame().origin;
		}
	}

	private Agent SpawnAgentFor(CharacterObject character, Vec3 position, Vec2 facing, bool isPlayer)
	{
		Team team = base.Mission.PlayerTeam ?? base.Mission.Teams.Add(BattleSideEnum.Defender, 255u, 16777215u);
		float num = ((_floorZ > float.MinValue) ? _floorZ : Math.Max(position.z, base.Mission.Scene.GetGroundHeightAtPosition(position)));
		Vec3 position2 = new Vec3(position.x, position.y, num + 0.05f);
		Vec2 direction = ((facing == Vec2.Zero) ? Vec2.Forward : facing.Normalized());
		AgentBuildData agentBuildData = new AgentBuildData(character).Team(team).InitialPosition(in position2).InitialDirection(in direction)
			.CivilianEquipment(civilianEquipment: true)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: true)
			.Controller((!isPlayer) ? AgentControllerType.AI : AgentControllerType.Player);
		Hero heroObject = character.HeroObject;
		if (heroObject?.ClanBanner != null)
		{
			agentBuildData.Banner(heroObject.ClanBanner);
		}
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		agent.SetWatchState(Agent.WatchState.Patrolling);
		return agent;
	}

	private void ApplyActionSet(Agent agent, string suffix)
	{
		if (agent != null && agent.Character != null)
		{
			string actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agent.Monster, agent.Character.IsFemale, suffix);
			AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(MBGlobals.GetActionSet(actionSetCode), agent.Character.GetStepSize(), hasClippingPlane: false);
			agent.SetActionSet(ref animationSystemData);
		}
	}

	private void CollectTavernSeats()
	{
		_tavernSeats.Clear();
		_seatCursor = 0;
		try
		{
			List<UsableMachine> list = new List<UsableMachine>();
			List<UsableMachine> list2 = new List<UsableMachine>();
			List<string> list3 = new List<string>();
			foreach (UsableMachine item in base.Mission.ActiveMissionObjects.OfType<UsableMachine>())
			{
				if (item == null || item.IsDisabledForAI || item.StandingPoints == null || item.StandingPoints.Count == 0)
				{
					continue;
				}
				bool flag = item is Chair;
				if ((!flag && !(item is UsablePlace)) || IsSleepingFurniture(item))
				{
					continue;
				}
				(flag ? list : list2).Add(item);
				try
				{
					WeakGameEntity gameEntity = item.GameEntity;
					if (gameEntity.IsValid)
					{
						list3.Add((flag ? "[chair] " : "") + (gameEntity.Name ?? "?"));
					}
				}
				catch
				{
				}
			}
			_tavernSeats.AddRange(list);
			_tavernSeats.AddRange(list2);
			foreach (UsableMachine tavernSeat in _tavernSeats)
			{
				if (tavernSeat.StandingPoints == null)
				{
					continue;
				}
				foreach (StandingPoint standingPoint in tavernSeat.StandingPoints)
				{
					try
					{
						standingPoint?.SetIsDisabledForPlayersSynched(value: true);
					}
					catch
					{
					}
				}
			}
			TraceLogger.Write("HomesteadTavernMissionLogic", $"Tavern usable places ({list.Count} chairs, {list2.Count} standing): " + string.Join(", ", list3));
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTavernMissionLogic", "CollectTavernSeats failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		TraceLogger.Write("HomesteadTavernMissionLogic", $"CollectTavernSeats: found {_tavernSeats.Count} usable seats/spots.");
	}

	private void EnableNativeExitDoors()
	{
		int num = 0;
		try
		{
			foreach (PassageUsePoint item in base.Mission.ActiveMissionObjects.OfType<PassageUsePoint>())
			{
				if (item != null)
				{
					item.IsMissionExit = true;
					num++;
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTavernMissionLogic", "EnableNativeExitDoors failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		TraceLogger.Write("HomesteadTavernMissionLogic", $"EnableNativeExitDoors: enabled {num} native door(s) as mission exits.");
	}

	private static bool IsSleepingFurniture(UsableMachine place)
	{
		try
		{
			WeakGameEntity gameEntity = place.GameEntity;
			if (!gameEntity.IsValid)
			{
				return false;
			}
			string text = gameEntity.Name?.ToLowerInvariant() ?? "";
			return text.Contains("bed") || text.Contains("bunk") || text.Contains("sleep") || text.Contains("bedroll");
		}
		catch
		{
			return false;
		}
	}

	private void SeatPatron(Agent agent)
	{
		if (agent == null || !agent.IsActive() || _tavernSeats.Count == 0)
		{
			return;
		}
		for (int i = 0; i < _tavernSeats.Count; i++)
		{
			UsableMachine usableMachine = _tavernSeats[(_seatCursor + i) % _tavernSeats.Count];
			if (usableMachine.GetVacantStandingPointForAI(agent) != null)
			{
				_seatCursor = (_seatCursor + i + 1) % _tavernSeats.Count;
				try
				{
					agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator().SetTarget(usableMachine, false, Agent.AIScriptedFrameFlags.None);
					return;
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadTavernMissionLogic", "SeatPatron failed for '" + agent.Name + "': " + ex.GetType().Name + ": " + ex.Message);
					return;
				}
			}
		}
		TraceLogger.Write("HomesteadTavernMissionLogic", "SeatPatron: no vacant seat available for '" + agent.Name + "'.");
	}

	private static void PoseAgent(Agent agent, string actionName)
	{
		if (agent == null || string.IsNullOrEmpty(actionName))
		{
			return;
		}
		try
		{
			agent.Controller = AgentControllerType.None;
			agent.SetActionChannel(0, ActionIndexCache.Create(actionName), ignorePriority: true, (AnimFlags)0uL);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTavernMissionLogic", "PoseAgent '" + actionName + "' failed for '" + agent.Name + "': " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static IEnumerable<Hero> GetAccompanyingPartyHeroes()
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			yield break;
		}
		foreach (TroopRosterElement item in mainParty.MemberRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character != null && character.IsHero)
			{
				Hero heroObject = character.HeroObject;
				if (heroObject != null && heroObject != Hero.MainHero && heroObject.IsAlive)
				{
					yield return heroObject;
				}
			}
		}
	}

	private CharacterObject? ResolveMusicianCharacter()
	{
		CultureObject cultureObject = _homestead.Leader?.Culture ?? Hero.MainHero?.Culture;
		object obj = cultureObject?.Musician;
		if (obj == null)
		{
			if (cultureObject == null)
			{
				return null;
			}
			obj = cultureObject.Townsman;
		}
		return (CharacterObject?)obj;
	}

	public override void OnEndMissionInternal()
	{
		base.OnEndMissionInternal();
		TavernKeeperAgent = null;
		PendingMercFadeAgent = null;
		HomesteadBehavior instance = HomesteadBehavior.Instance;
		if (instance == null || !instance.ReturnNearTavernEntrance)
		{
			HomesteadBehavior.TavernMissionActive = false;
		}
	}

	private GameEntity? FindFirstTaggedEntity(params string[] tags)
	{
		foreach (string tag in tags)
		{
			GameEntity gameEntity = base.Mission.Scene.FindEntitiesWithTag(tag).FirstOrDefault();
			if (gameEntity != null)
			{
				return gameEntity;
			}
		}
		return null;
	}

	private IEnumerable<GameEntity> CollectTaggedEntities(params string[] tags)
	{
		List<GameEntity> list = new List<GameEntity>();
		foreach (string tag in tags)
		{
			list.AddRange(base.Mission.Scene.FindEntitiesWithTag(tag));
		}
		return list;
	}

	private static Vec2 FaceToward(Vec3 from, Vec3 to)
	{
		Vec3 vec = to - from;
		vec.z = 0f;
		if (!(vec.LengthSquared > 0.01f))
		{
			return Vec2.Forward;
		}
		return vec.AsVec2.Normalized();
	}

	private Vec3 ScatterNearDoor(int index)
	{
		float num = (float)((double)index * 2.39996);
		float num2 = 2.5f + (float)(index % 3) * 1.5f;
		return (_doorPos.IsValid ? _doorPos : new Vec3(0f, 0f, 0f, -1f)) + new Vec3((float)(Math.Cos(num) * (double)num2), (float)(Math.Sin(num) * (double)num2));
	}
}
