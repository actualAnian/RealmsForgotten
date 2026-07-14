using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Homesteads.Models;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

internal class HomesteadBattleSceneMissionLogic : MissionLogic
{
	private sealed class AgentStuckState
	{
		public Vec3 LastSampledPosition;

		public int SlowSampleCount;

		public List<Vec3>? WaypointChain;

		public int ChainIndex;

		public float ChainElapsed;

		public float CooldownRemaining;

		public bool HasActiveChain
		{
			get
			{
				if (WaypointChain != null)
				{
					return ChainIndex < WaypointChain.Count;
				}
				return false;
			}
		}

		public Vec3 CurrentWaypoint => WaypointChain[ChainIndex];
	}

	private const string WarBannerPrefabName = "homestead_war_banner_big";

	private const string LegacyWarBannerPrefabName = "flagpole_b_ground";

	private const float WarBannerAuraRadius = 15f;

	private const float WarBannerAuraTickSeconds = 5f;

	private const float WarBannerHealFraction = 0.03f;

	private const float WarBannerMoraleBoost = 3f;

	private readonly Homestead homestead;

	private bool hasLoadedSavedEntities;

	private readonly LoadingFadeOverlay _loadingFade = new LoadingFadeOverlay(0.5f);

	private readonly List<Vec3> warBannerPositions = new List<Vec3>();

	private float warBannerAuraTimer;

	private readonly List<Vec3> _playerNavHints = new List<Vec3>();

	private const float StuckSampleInterval = 3f;

	private const float StuckMovedThreshold = 0.4f;

	private const int StuckSampleCount = 3;

	private const float WaypointClearanceRadius = 2f;

	private const float WaypointArrivalRadius = 2.5f;

	private const float ChainWaypointTimeout = 6f;

	private const float RedirectCooldown = 5f;

	private const float WallClusterGap = 2f;

	private float _stuckSampleTimer;

	private const float StuckDetectionMaxRadiusSq = 3600f;

	private List<(Vec3 center, float radius)> _cachedBuildingExclusions = new List<(Vec3, float)>();

	private readonly List<Agent> _stuckAgentsToRemove = new List<Agent>();

	private readonly Dictionary<Agent, AgentStuckState> _stuckStates = new Dictionary<Agent, AgentStuckState>();

	private float _placementDelayRemaining = 0.5f;

	private readonly HomesteadBallistaTurretController _ballistaTurrets = new HomesteadBallistaTurretController();

	private Vec3 _homesteadBattleCenter = Vec3.Invalid;

	private bool _stagingRepositioned;

	private bool _playerRepositioned;

	private const float FlagSupportRaycastUp = 0.4f;

	private const float FlagSupportRaycastDown = 3f;

	private const float FlagSupportMaxGap = 1.2f;

	internal Homestead BattleHomestead => homestead;

	public bool HasRepositionedPlayers => _playerRepositioned;

	public HomesteadBattleSceneMissionLogic(Homestead homestead)
	{
		this.homestead = homestead;
	}

	private static string SpawnTagForSide(BattleSideEnum side)
	{
		if (side != BattleSideEnum.Defender)
		{
			return "spawnpoint_side_1";
		}
		return "spawnpoint_side_0";
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		HomesteadScene homesteadScene = homestead.GetHomesteadScene();
		_homesteadBattleCenter = GetHomesteadBattleCenter(homesteadScene);
		RepositionDefenderSpawnPoints();
	}

	private void TeleportForStagingTick()
	{
		if (_stagingRepositioned)
		{
			return;
		}
		Mission current = Mission.Current;
		if (current == null || current.Mode != MissionMode.Deployment)
		{
			return;
		}
		Team playerTeam = Mission.Current.PlayerTeam;
		if (playerTeam == null)
		{
			return;
		}
		_stagingRepositioned = true;
		Vec3 vec = (_homesteadBattleCenter.IsValid ? _homesteadBattleCenter : TryGetWalkAreaCentroid());
		if (!vec.IsValid)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"TeleportForStagingTick: no valid homestead centre — staging agents not repositioned for '{homestead.Name}'.");
			return;
		}
		if (!IsInsideBattleWalkArea(vec))
		{
			Vec3 vec2 = ClampToNearestWalkAreaEdge(vec);
			if (vec2.IsValid)
			{
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"TeleportForStagingTick: homestead centre ({vec.x:0.##},{vec.y:0.##}) outside walk_area — clamped to ({vec2.x:0.##},{vec2.y:0.##}).");
				vec = vec2;
			}
		}
		float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(vec);
		vec = new Vec3(vec.x, vec.y, groundHeightAtPosition);
		int num = 0;
		int num2 = 0;
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent != null && agent.IsActive() && !agent.IsMount && agent.Team == playerTeam)
			{
				try
				{
					Vec3 position = SpreadAround(vec, num2, 100);
					agent.TeleportToPosition(position);
					num2++;
					num++;
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"TeleportForStagingTick: agent[{num2}] relocation failed: {ex.Message}");
				}
			}
		}
		TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"TeleportForStagingTick: moved {num} friendly agents to homestead centre " + $"({vec.x:0.##},{vec.y:0.##}) for staging in '{homestead.Name}'.");
		TryFinishDeployment();
	}

	private void TryFinishDeployment()
	{
		try
		{
			foreach (MissionBehavior missionBehavior in Mission.Current.MissionBehaviors)
			{
				if (missionBehavior != null)
				{
					MethodInfo method = missionBehavior.GetType().GetMethod("FinishDeployment", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (!(method == null))
					{
						method.Invoke(missionBehavior, null);
						TraceLogger.Write("HomesteadBattleSceneMissionLogic", "TryFinishDeployment: called FinishDeployment() on " + missionBehavior.GetType().Name + " — deployment screen should dismiss.");
						return;
					}
				}
			}
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", "TryFinishDeployment: no behavior with FinishDeployment found — falling back to SetMissionMode(Battle).");
			Mission.Current.SetMissionMode(MissionMode.Battle, atStart: false);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", "TryFinishDeployment: threw " + ex.GetType().Name + ": " + ex.Message);
			try
			{
				Mission.Current.SetMissionMode(MissionMode.Battle, atStart: false);
			}
			catch
			{
			}
		}
	}

	private void TeleportPlayerTick()
	{
		if (_playerRepositioned)
		{
			return;
		}
		Mission current = Mission.Current;
		if (current == null || current.Mode != MissionMode.Battle || !hasLoadedSavedEntities)
		{
			return;
		}
		Agent mainAgent = Mission.Current.MainAgent;
		if (mainAgent == null)
		{
			return;
		}
		Vec3 vec = (_homesteadBattleCenter.IsValid ? _homesteadBattleCenter : TryGetWalkAreaCentroid());
		if (!vec.IsValid)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"TeleportPlayerTick: no homestead centre or walk_area centroid — agents not repositioned for '{homestead.Name}'.");
			_playerRepositioned = true;
			return;
		}
		if (!IsInsideBattleWalkArea(vec))
		{
			Vec3 vec2 = ClampToNearestWalkAreaEdge(vec);
			if (!vec2.IsValid)
			{
				vec2 = TryGetWalkAreaCentroid();
			}
			if (!vec2.IsValid)
			{
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"TeleportPlayerTick: homestead centre ({vec.x:0.##},{vec.y:0.##}) outside walk_area with no fallback.");
				_playerRepositioned = true;
				return;
			}
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"TeleportPlayerTick: homestead centre clamped ({vec.x:0.##},{vec.y:0.##}) → ({vec2.x:0.##},{vec2.y:0.##}).");
			vec = vec2;
		}
		float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(vec);
		vec = new Vec3(vec.x, vec.y, groundHeightAtPosition);
		Team team = mainAgent.Team;
		List<Agent> list = new List<Agent>();
		List<Agent> list2 = new List<Agent>();
		foreach (Agent agent2 in Mission.Current.Agents)
		{
			if (agent2 != null && agent2.IsActive() && !agent2.IsMount)
			{
				if (agent2.Team == team)
				{
					list.Add(agent2);
				}
				else if (agent2.Team != null)
				{
					list2.Add(agent2);
				}
			}
		}
		if (list.Remove(mainAgent))
		{
			list.Insert(0, mainAgent);
		}
		try
		{
			Vec3 position = SpreadAroundClear(vec, 0, list.Count, _cachedBuildingExclusions);
			mainAgent.TeleportToPosition(position);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", "TeleportPlayerTick: player relocation failed: " + ex.Message);
		}
		List<Agent> list3 = new List<Agent>(list);
		list3.Remove(mainAgent);
		if (!DistributeDefendersAcrossGuardFlags(list3, vec))
		{
			int num = 1;
			foreach (Agent item3 in list3)
			{
				try
				{
					Vec3 position2 = SpreadAroundClear(vec, num, list.Count, _cachedBuildingExclusions);
					item3.TeleportToPosition(position2);
					num++;
				}
				catch (Exception ex2)
				{
					TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"TeleportPlayerTick: friendly[{num}] relocation failed: {ex2.Message}");
				}
			}
		}
		Team playerTeam = Mission.Current.PlayerTeam;
		if (playerTeam != null)
		{
			foreach (Formation item4 in playerTeam.FormationsIncludingEmpty)
			{
				if (item4.CountOfUnits != 0)
				{
					try
					{
						WorldPosition position3 = new WorldPosition(Mission.Current.Scene, vec);
						item4.SetMovementOrder(MovementOrder.MovementOrderMove(position3));
					}
					catch (Exception ex3)
					{
						TraceLogger.Write("HomesteadBattleSceneMissionLogic", "TeleportPlayerTick: formation order reset failed: " + ex3.Message);
					}
				}
			}
		}
		Vec3 center = ComputeWalkAreaFarPoint(vec);
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < list2.Count; i++)
		{
			Agent agent = list2[i];
			bool flag;
			if (_cachedBuildingExclusions.Count > 0)
			{
				flag = false;
				foreach (var cachedBuildingExclusion in _cachedBuildingExclusions)
				{
					Vec3 item = cachedBuildingExclusion.center;
					float item2 = cachedBuildingExclusion.radius;
					float num4 = agent.Position.x - item.x;
					float num5 = agent.Position.y - item.y;
					float num6 = item2 + 6f;
					if (num4 * num4 + num5 * num5 <= num6 * num6)
					{
						flag = true;
						break;
					}
				}
			}
			else
			{
				float num7 = agent.Position.x - vec.x;
				float num8 = agent.Position.y - vec.y;
				flag = num7 * num7 + num8 * num8 <= 1600f;
			}
			if (flag)
			{
				try
				{
					Vec3 position4 = (center.IsValid ? SpreadAround(center, num3, list2.Count) : SpreadAround(new Vec3(vec.x + 100f, vec.y, vec.z), num3, list2.Count));
					agent.TeleportToPosition(position4);
					num3++;
					num2++;
				}
				catch (Exception ex4)
				{
					TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"TeleportPlayerTick: enemy[{i}] relocation failed: {ex4.Message}");
				}
			}
		}
		_playerRepositioned = true;
		string arg = (_homesteadBattleCenter.IsValid ? "homestead centre" : "walk_area centroid");
		TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"TeleportPlayerTick: repositioned {list.Count} friendly + {num2}/{list2.Count} enemy agents. " + $"friendlyTarget=({vec.x:0.##},{vec.y:0.##}) " + "enemyFarTarget=(" + (center.IsValid ? $"{center.x:0.##},{center.y:0.##}" : "none") + ") " + $"via {arg} for '{homestead.Name}'.");
	}

	private bool DistributeDefendersAcrossGuardFlags(List<Agent> troops, Vec3 friendlyTarget)
	{
		HomesteadScene homesteadScene;
		try
		{
			homesteadScene = homestead.GetHomesteadScene();
		}
		catch
		{
			return false;
		}
		IEnumerable<KeyValuePair<GameEntity, HomesteadSceneSavedEntity>> enumerable = homesteadScene?.LoadedSavedEntities;
		if (enumerable == null)
		{
			return false;
		}
		List<(Vec3 pos, Mat3 rot, bool groundLevel)> flagSpots = new List<(Vec3, Mat3, bool)>();
		int num = 0;
		foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> item3 in enumerable)
		{
			HomesteadScenePlaceable homesteadScenePlaceable = item3.Value?.Placeable;
			if (homesteadScenePlaceable != null && homesteadScenePlaceable.IsNpcActionFlag && string.Equals(homesteadScenePlaceable.NpcAction, "STAND", StringComparison.OrdinalIgnoreCase))
			{
				MatrixFrame globalFrame;
				try
				{
					globalFrame = item3.Key.GetGlobalFrame();
				}
				catch
				{
					continue;
				}
				if (!IsFlagSpotSupported(globalFrame.origin))
				{
					num++;
				}
				else
				{
					flagSpots.Add((globalFrame.origin, globalFrame.rotation, IsGroundLevelFlag(globalFrame.origin)));
				}
			}
		}
		if (flagSpots.Count == 0)
		{
			return false;
		}
		List<int> list = Enumerable.Range(0, flagSpots.Count).ToList();
		List<int> list2 = list.Where((int i) => flagSpots[i].groundLevel).ToList();
		int[] array = new int[troops.Count];
		int[] array2 = new int[flagSpots.Count];
		int num2 = 0;
		int num3 = 0;
		for (int num4 = 0; num4 < troops.Count; num4++)
		{
			bool flag = troops[num4]?.HasMount ?? false;
			List<int> list3 = (flag ? list2 : list);
			if (list3.Count == 0)
			{
				array[num4] = -1;
				continue;
			}
			int num5 = (flag ? list3[num3 % list3.Count] : list3[num2 % list3.Count]);
			if (flag)
			{
				num3++;
			}
			else
			{
				num2++;
			}
			array[num4] = num5;
			array2[num5]++;
		}
		int[] array3 = new int[flagSpots.Count];
		List<Agent> list4 = new List<Agent>();
		int num6 = 0;
		for (int num7 = 0; num7 < troops.Count; num7++)
		{
			Agent agent = troops[num7];
			if (agent == null || !agent.IsActive())
			{
				continue;
			}
			if (array[num7] < 0)
			{
				list4.Add(agent);
				continue;
			}
			(Vec3 pos, Mat3 rot, bool groundLevel) tuple = flagSpots[array[num7]];
			Vec3 item = tuple.pos;
			Mat3 item2 = tuple.rot;
			int num8 = array3[array[num7]]++;
			try
			{
				Vec3 position = SpreadAroundClear(item, num8, array2[array[num7]], _cachedBuildingExclusions);
				agent.TeleportToPosition(position);
				if (num8 == 0)
				{
					Vec2 asVec = item2.f.AsVec2;
					if (asVec.LengthSquared > 0.0001f)
					{
						agent.SetMovementDirection(asVec.Normalized());
					}
				}
				num6++;
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", "DistributeDefendersAcrossGuardFlags: relocation failed: " + ex.Message);
			}
		}
		int num9 = 0;
		foreach (Agent item4 in list4)
		{
			try
			{
				Vec3 position2 = SpreadAroundClear(friendlyTarget, num9, list4.Count, _cachedBuildingExclusions);
				item4.TeleportToPosition(position2);
				num9++;
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", "DistributeDefendersAcrossGuardFlags: centre-fallback relocation failed: " + ex2.Message);
			}
		}
		TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"DistributeDefendersAcrossGuardFlags: {num6} troop(s) placed across {flagSpots.Count} Guard/Stand flag(s) " + $"({list2.Count} ground-level), {list4.Count} cavalry fell back to the homestead centre " + $"(no ground-level flag available), {num} flag(s) skipped (no solid surface) for '{homestead.Name}'.");
		return true;
	}

	private static bool IsGroundLevelFlag(Vec3 flagPos)
	{
		try
		{
			float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(new Vec3(flagPos.x, flagPos.y));
			return Math.Abs(flagPos.z - groundHeightAtPosition) <= 1.2f;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsFlagSpotSupported(Vec3 flagPos)
	{
		try
		{
			Vec3 vec = flagPos + Vec3.Up * 0.4f;
			Vec3 vec2 = flagPos - Vec3.Up * 3f;
			if (Mission.Current.Scene.RayCastForClosestEntityOrTerrain(vec, vec2, out var collisionDistance, 0.05f, BodyFlags.CommonCollisionExcludeFlagsForMissile))
			{
				Vec3 vec3 = (vec2 - vec).NormalizedCopy();
				return Math.Abs((vec + vec3 * collisionDistance).z - flagPos.z) <= 1.2f;
			}
		}
		catch
		{
		}
		return false;
	}

	private static Vec3 SpreadAround(Vec3 center, int index, int total)
	{
		if (index == 0)
		{
			return center;
		}
		float x = (float)index * (TaleWorlds.Library.MathF.PI * 2f / (float)TaleWorlds.Library.MathF.Max(total, 1));
		float num = 1.5f + (float)(index / 6) * 2f;
		float x2 = center.x + TaleWorlds.Library.MathF.Cos(x) * num;
		float y = center.y + TaleWorlds.Library.MathF.Sin(x) * num;
		float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(new Vec3(x2, y));
		return new Vec3(x2, y, groundHeightAtPosition);
	}

	public override void OnMissionTick(float dt)
	{
		TeleportForStagingTick();
		TeleportPlayerTick();
		_loadingFade.Tick(dt, hasLoadedSavedEntities);
		if (!hasLoadedSavedEntities)
		{
			_placementDelayRemaining -= dt;
			if (_placementDelayRemaining <= 0f)
			{
				LoadSavedBuildables("OnMissionTick");
			}
		}
		UnstuckAgentsTick(dt);
		ApplyWarBannerAura(dt);
		if (hasLoadedSavedEntities)
		{
			Mission current = Mission.Current;
			if (current != null && current.Mode == MissionMode.Battle)
			{
				_ballistaTurrets.Tick(dt, base.Mission.PlayerTeam);
			}
		}
	}

	public override void OnEndMissionInternal()
	{
		_loadingFade.Remove();
	}

	private void LoadSavedBuildables(string source)
	{
		if (hasLoadedSavedEntities)
		{
			return;
		}
		try
		{
			HomesteadScene homesteadScene = homestead.GetHomesteadScene();
			int num = homesteadScene.AddAllSavedEntitiesToCurrentScene();
			CacheWarBannerPositions(homesteadScene);
			CacheNavHintPositions(homesteadScene);
			ApplyNavMarkerVisibility(homesteadScene);
			hasLoadedSavedEntities = true;
			_cachedBuildingExclusions = GetBuildingExclusions();
			_ballistaTurrets.Rebuild(homesteadScene.LoadedSavedEntities);
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"Loaded {num}/{homesteadScene.SavedEntities.Count} saved buildables into homestead battle scene for '{homestead.Name}' via {source}.");
			int num2 = 0;
			foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> loadedSavedEntity in homesteadScene.LoadedSavedEntities)
			{
				if (num2 < 5)
				{
					Vec3 globalPosition = loadedSavedEntity.Key.GlobalPosition;
					TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"  entity[{num2}] prefab='{loadedSavedEntity.Value?.Placeable?.PrefabName}' saved=({loadedSavedEntity.Value?.posX:0.##},{loadedSavedEntity.Value?.posY:0.##},{loadedSavedEntity.Value?.posZ:0.##}) actual=({globalPosition.x:0.##},{globalPosition.y:0.##},{globalPosition.z:0.##})");
					num2++;
					continue;
				}
				break;
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"Failed loading saved buildables for homestead battle '{homestead.Name}' via {source}: {arg}");
		}
	}

	private void CacheWarBannerPositions(HomesteadScene homesteadScene)
	{
		warBannerPositions.Clear();
		foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> loadedSavedEntity in homesteadScene.LoadedSavedEntities)
		{
			if (IsWarBannerPrefab(loadedSavedEntity.Value?.Placeable?.PrefabName))
			{
				warBannerPositions.Add(loadedSavedEntity.Key.GlobalPosition);
			}
		}
		if (warBannerPositions.Count > 0)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"Cached {warBannerPositions.Count} homestead war banner aura position(s) for '{homestead.Name}'.");
		}
	}

	private static bool IsWarBannerPrefab(string? prefabName)
	{
		if (!(prefabName == "homestead_war_banner_big"))
		{
			return prefabName == "flagpole_b_ground";
		}
		return true;
	}

	private static bool IsNavMarkerPrefab(string? prefabName)
	{
		if (!(prefabName == "homestead_nav_point") && !(prefabName == "homestead_race_start"))
		{
			return prefabName == "homestead_race_gate";
		}
		return true;
	}

	private static bool IsNavHintPrefab(string? prefabName)
	{
		return prefabName == "homestead_nav_point";
	}

	private void CacheNavHintPositions(HomesteadScene homesteadScene)
	{
		_playerNavHints.Clear();
		foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> loadedSavedEntity in homesteadScene.LoadedSavedEntities)
		{
			if (IsNavHintPrefab(loadedSavedEntity.Value?.Placeable?.PrefabName))
			{
				_playerNavHints.Add(loadedSavedEntity.Key.GlobalPosition);
			}
		}
		if (_playerNavHints.Count > 0)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"Cached {_playerNavHints.Count} player nav hint(s) for '{homestead.Name}'.");
		}
	}

	private static void ApplyNavMarkerVisibility(HomesteadScene homesteadScene)
	{
		bool flag = GlobalSettings<MCMSettings>.Instance?.ShowNavPointsInBattle ?? false;
		foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> loadedSavedEntity in homesteadScene.LoadedSavedEntities)
		{
			HomesteadScenePlaceable obj = loadedSavedEntity.Value?.Placeable;
			string prefabName = obj?.PrefabName;
			bool flag2 = obj?.IsNpcActionFlag ?? false;
			if (!IsNavMarkerPrefab(prefabName) && !flag2)
			{
				continue;
			}
			bool flag3 = !flag2 && flag;
			try
			{
				loadedSavedEntity.Key.SetVisibilityExcludeParents(flag3);
				if (flag3)
				{
					ApplyNavMarkerMaterialBattle(loadedSavedEntity.Key, "plain_green");
				}
			}
			catch
			{
			}
		}
	}

	private static void ApplyNavMarkerMaterialBattle(GameEntity entity, string material)
	{
		try
		{
			foreach (GameEntity entityAndChild in entity.GetEntityAndChildren())
			{
				MetaMesh metaMesh = entityAndChild.GetMetaMesh(0);
				if (!(metaMesh == null))
				{
					for (int i = 0; i < metaMesh.MeshCount; i++)
					{
						metaMesh.GetMeshAtIndex(i).SetMaterial(material);
					}
				}
			}
		}
		catch
		{
		}
	}

	private void UnstuckAgentsTick(float dt)
	{
		if (!hasLoadedSavedEntities)
		{
			return;
		}
		_stuckSampleTimer += dt;
		if (_stuckSampleTimer < 3f)
		{
			return;
		}
		_stuckSampleTimer = 0f;
		if (_cachedBuildingExclusions.Count == 0)
		{
			return;
		}
		Scene scene = Mission.Current.Scene;
		Vec3 homesteadBattleCenter = _homesteadBattleCenter;
		bool isValid = homesteadBattleCenter.IsValid;
		_stuckAgentsToRemove.Clear();
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent == null || !agent.IsActive() || agent.IsMount || agent.IsMainAgent)
			{
				continue;
			}
			if (!agent.IsHuman || agent.Health <= 0f)
			{
				_stuckAgentsToRemove.Add(agent);
				continue;
			}
			Vec3 position = agent.Position;
			if (isValid)
			{
				float num = position.x - homesteadBattleCenter.x;
				float num2 = position.y - homesteadBattleCenter.y;
				if (num * num + num2 * num2 > 3600f)
				{
					if (_stuckStates.ContainsKey(agent))
					{
						_stuckAgentsToRemove.Add(agent);
					}
					continue;
				}
			}
			if (!_stuckStates.TryGetValue(agent, out AgentStuckState value))
			{
				value = new AgentStuckState
				{
					LastSampledPosition = position
				};
				_stuckStates[agent] = value;
				continue;
			}
			if (value.CooldownRemaining > 0f)
			{
				value.CooldownRemaining -= 3f;
				value.LastSampledPosition = position;
				continue;
			}
			if (value.HasActiveChain)
			{
				value.ChainElapsed += 3f;
				Vec3 currentWaypoint = value.CurrentWaypoint;
				float num3 = position.x - currentWaypoint.x;
				float num4 = position.y - currentWaypoint.y;
				bool flag = num3 * num3 + num4 * num4 <= 6.25f;
				bool flag2 = value.ChainElapsed >= 6f;
				if (!(flag || flag2))
				{
					continue;
				}
				if (flag2 && !flag)
				{
					TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"UnstuckAgents: chain[{value.ChainIndex}] timed out for agent at ({position.x:0.#},{position.y:0.#}).");
				}
				value.ChainIndex++;
				value.ChainElapsed = 0f;
				if (value.HasActiveChain)
				{
					try
					{
						Vec3 currentWaypoint2 = value.CurrentWaypoint;
						WorldPosition position2 = new WorldPosition(scene, currentWaypoint2);
						agent.SetScriptedPosition(ref position2, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.GoToPosition);
						TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"UnstuckAgents: chain[{value.ChainIndex}/{value.WaypointChain.Count}] " + $"agent ({position.x:0.#},{position.y:0.#}) → ({currentWaypoint2.x:0.#},{currentWaypoint2.y:0.#}).");
					}
					catch (Exception ex)
					{
						TraceLogger.Write("HomesteadBattleSceneMissionLogic", "UnstuckAgents: chain SetScriptedPosition failed: " + ex.Message);
						value.WaypointChain = null;
						value.CooldownRemaining = 5f;
					}
				}
				else
				{
					try
					{
						agent.DisableScriptedMovement();
					}
					catch
					{
					}
					TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"UnstuckAgents: chain complete, agent now at ({position.x:0.#},{position.y:0.#}).");
					value.WaypointChain = null;
					value.CooldownRemaining = 5f;
				}
				value.LastSampledPosition = position;
				continue;
			}
			float length = (position - value.LastSampledPosition).Length;
			value.LastSampledPosition = position;
			(Vec3, float) tuple = default((Vec3, float));
			float num5 = float.MaxValue;
			foreach (var cachedBuildingExclusion in _cachedBuildingExclusions)
			{
				Vec3 item = cachedBuildingExclusion.center;
				float item2 = cachedBuildingExclusion.radius;
				float num6 = position.x - item.x;
				float num7 = position.y - item.y;
				float num8 = num6 * num6 + num7 * num7;
				float num9 = item2 + 1f;
				if (num8 <= num9 * num9 && num8 < num5)
				{
					num5 = num8;
					tuple = (item, item2);
				}
			}
			(Vec3, float) tuple2 = tuple;
			if (tuple2.Item1 == default(Vec3) && tuple2.Item2 == 0f)
			{
				value.SlowSampleCount = 0;
				continue;
			}
			if (length >= 0.4f)
			{
				value.SlowSampleCount = 0;
				continue;
			}
			value.SlowSampleCount++;
			if (value.SlowSampleCount < 3)
			{
				continue;
			}
			value.SlowSampleCount = 0;
			bool flag3 = false;
			try
			{
				flag3 = agent.CanBeAssignedForScriptedMovement();
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", "UnstuckAgents: CanBeAssignedForScriptedMovement threw " + ex2.GetType().Name + " for agent. Exception: " + ex2.Message);
				flag3 = false;
			}
			if (!flag3)
			{
				value.CooldownRemaining = 5f;
				continue;
			}
			List<Vec3> list = FindPathAroundWallCluster(position, agent.GetTargetPosition(), tuple, _cachedBuildingExclusions, scene, _playerNavHints);
			if (list.Count == 0)
			{
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"UnstuckAgents: no chain computed for agent at ({position.x:0.#},{position.y:0.#}).");
				value.CooldownRemaining = 5f;
				continue;
			}
			try
			{
				WorldPosition position3 = new WorldPosition(scene, list[0]);
				agent.SetScriptedPosition(ref position3, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.GoToPosition);
				value.WaypointChain = list;
				value.ChainIndex = 0;
				value.ChainElapsed = 0f;
				string text = string.Join(" → ", list.Select((Vec3 w) => $"({w.x:0.#},{w.y:0.#})"));
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"UnstuckAgents: started {list.Count}-WP chain for agent ({position.x:0.#},{position.y:0.#}): {text}.");
			}
			catch (Exception ex3)
			{
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"UnstuckAgents: SetScriptedPosition failed for agent ({position.x:0.#},{position.y:0.#}): {ex3.Message}");
				value.CooldownRemaining = 5f;
			}
		}
		foreach (Agent item3 in _stuckAgentsToRemove)
		{
			_stuckStates.Remove(item3);
		}
	}

	private static List<(Vec3 center, float radius)> FindWallCluster((Vec3 center, float radius) seed, IReadOnlyList<(Vec3 center, float radius)> allExclusions)
	{
		int count = allExclusions.Count;
		bool[] array = new bool[count];
		for (int i = 0; i < count; i++)
		{
			if (allExclusions[i].center.x == seed.center.x && allExclusions[i].center.y == seed.center.y)
			{
				array[i] = true;
				break;
			}
		}
		bool flag = true;
		while (flag)
		{
			flag = false;
			for (int j = 0; j < count; j++)
			{
				if (array[j])
				{
					continue;
				}
				(Vec3 center, float radius) tuple = allExclusions[j];
				Vec3 item = tuple.center;
				float item2 = tuple.radius;
				for (int k = 0; k < count; k++)
				{
					if (array[k])
					{
						(Vec3 center, float radius) tuple2 = allExclusions[k];
						Vec3 item3 = tuple2.center;
						float item4 = tuple2.radius;
						float num = item.x - item3.x;
						float num2 = item.y - item3.y;
						float num3 = item2 + item4 + 2f;
						if (num * num + num2 * num2 <= num3 * num3)
						{
							array[j] = true;
							flag = true;
							break;
						}
					}
				}
			}
		}
		List<(Vec3, float)> list = new List<(Vec3, float)>();
		for (int l = 0; l < count; l++)
		{
			if (array[l])
			{
				list.Add(allExclusions[l]);
			}
		}
		if (list.Count == 0)
		{
			list.Add(seed);
		}
		return list;
	}

	private static List<Vec3> FindPathAroundWallCluster(Vec3 agentPos, Vec2 agentTarget, (Vec3 center, float radius) nearBuilding, IReadOnlyList<(Vec3 center, float radius)> allExclusions, Scene scene, IReadOnlyList<Vec3>? navHints = null)
	{
		Vec2 agentPos2 = agentPos.AsVec2;
		Vec2 toTarget = agentTarget - agentPos2;
		toTarget.Normalize();
		List<(Vec3 center, float radius)> list = FindWallCluster(nearBuilding, allExclusions);
		List<(Vec3, float)> list2 = new List<(Vec3, float)>();
		foreach (var item5 in list)
		{
			Vec3 item = item5.center;
			float item2 = item5.radius;
			Vec2 vec = item.AsVec2 - agentPos2;
			float num = vec.x * toTarget.x + vec.y * toTarget.y;
			if (!(num < 0f - item2))
			{
				float num2 = ((num > 0f) ? num : 0f);
				float num3 = agentPos2.x + toTarget.x * num2;
				float num4 = agentPos2.y + toTarget.y * num2;
				float num5 = item.x - num3;
				float num6 = item.y - num4;
				float num7 = item2 + 2f;
				if (num5 * num5 + num6 * num6 <= num7 * num7)
				{
					list2.Add((item, item2));
				}
			}
		}
		if (list2.Count == 0)
		{
			list2.Add(nearBuilding);
		}
		float num8 = float.MaxValue;
		float num9 = float.MinValue;
		float num10 = float.MaxValue;
		float num11 = float.MinValue;
		foreach (var (vec2, num12) in list2)
		{
			if (vec2.x - num12 < num8)
			{
				num8 = vec2.x - num12;
			}
			if (vec2.x + num12 > num9)
			{
				num9 = vec2.x + num12;
			}
			if (vec2.y - num12 < num10)
			{
				num10 = vec2.y - num12;
			}
			if (vec2.y + num12 > num11)
			{
				num11 = vec2.y + num12;
			}
		}
		bool flag = num9 - num8 >= num11 - num10;
		float num13 = (flag ? ((num10 + num11) * 0.5f) : ((num8 + num9) * 0.5f));
		float num14 = (flag ? ((num11 - num10) * 0.5f) : ((num9 - num8) * 0.5f));
		float num15 = (flag ? agentPos.y : agentPos.x);
		float num16 = (flag ? agentPos.x : agentPos.y);
		float num17 = ((num15 < num13) ? 1f : (-1f));
		float num18 = num13 - num17 * (num14 + 2f);
		float num19 = num13 + num17 * (num14 + 2f);
		float num20 = (flag ? num8 : num10);
		float num21 = (flag ? num9 : num11);
		float num22 = 3f;
		float num23 = num20 - num22;
		float num24 = num21 + num22;
		Vec2 candidate = (flag ? new Vec2(num23, num18) : new Vec2(num18, num23));
		Vec2 candidate2 = (flag ? new Vec2(num24, num18) : new Vec2(num18, num24));
		float num25 = ((ScoreEnd(candidate) >= ScoreEnd(candidate2)) ? num23 : num24);
		Vec2 vec3 = (flag ? new Vec2(num16, num18) : new Vec2(num18, num16));
		Vec2 vec4 = (flag ? new Vec2(num25, num18) : new Vec2(num18, num25));
		Vec2 vec5 = (flag ? new Vec2(num25, num19) : new Vec2(num19, num25));
		List<Vec3> list3 = new List<Vec3>(4);
		Vec2[] array = new Vec2[3] { vec3, vec4, vec5 };
		for (int i = 0; i < array.Length; i++)
		{
			Vec2 vec6 = array[i];
			bool flag2 = false;
			foreach (var allExclusion in allExclusions)
			{
				Vec3 item3 = allExclusion.center;
				float item4 = allExclusion.radius;
				float num26 = vec6.x - item3.x;
				float num27 = vec6.y - item3.y;
				if (num26 * num26 + num27 * num27 < item4 * item4 * 0.81f)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				float groundHeightAtPosition = scene.GetGroundHeightAtPosition(new Vec3(vec6.x, vec6.y));
				list3.Add(new Vec3(vec6.x, vec6.y, groundHeightAtPosition));
			}
		}
		if (navHints != null && navHints.Count > 0)
		{
			float num28 = ScoreEnd(flag ? new Vec2(num25, num19) : new Vec2(num19, num25));
			Vec3? vec7 = null;
			foreach (Vec3 navHint in navHints)
			{
				float num29 = (flag ? navHint.y : navHint.x);
				if (!((num17 > 0f) ? (num29 < num13 - num14 * 0.5f) : (num29 > num13 + num14 * 0.5f)))
				{
					continue;
				}
				float num30 = (flag ? navHint.x : navHint.y);
				float num31 = (num21 - num20) * 0.5f;
				float num32 = (num20 + num21) * 0.5f;
				if (!(Math.Abs(num30 - num32) > num31 * 1.5f))
				{
					float num33 = ScoreEnd(navHint.AsVec2);
					if (num33 > num28)
					{
						num28 = num33;
						vec7 = navHint;
					}
				}
			}
			if (vec7.HasValue)
			{
				Vec3 value = vec7.Value;
				float groundHeightAtPosition2 = scene.GetGroundHeightAtPosition(new Vec3(value.x, value.y));
				list3.Add(new Vec3(value.x, value.y, groundHeightAtPosition2));
			}
		}
		return list3;
		float ScoreEnd(Vec2 vec8)
		{
			Vec2 va = vec8 - agentPos2;
			va.Normalize();
			return Vec2.DotProduct(va, toTarget);
		}
	}

	private void ApplyWarBannerAura(float dt)
	{
		if (!hasLoadedSavedEntities || warBannerPositions.Count == 0)
		{
			return;
		}
		warBannerAuraTimer += dt;
		if (warBannerAuraTimer < 5f)
		{
			return;
		}
		warBannerAuraTimer = 0f;
		float radiusSquared = 225f;
		foreach (Agent agent in base.Mission.Agents)
		{
			if (IsFriendlyLivingHuman(agent) && IsInsideAnyWarBannerAura(agent.Position, radiusSquared))
			{
				agent.ChangeMorale(3f);
				HealAgent(agent);
			}
		}
	}

	private bool IsFriendlyLivingHuman(Agent agent)
	{
		if (agent != null && agent.IsActive() && agent.IsHuman && !agent.IsMount && agent.Team == base.Mission.PlayerTeam)
		{
			return agent.Health > 0f;
		}
		return false;
	}

	private bool IsInsideAnyWarBannerAura(Vec3 position, float radiusSquared)
	{
		foreach (Vec3 warBannerPosition in warBannerPositions)
		{
			if ((position - warBannerPosition).LengthSquared <= radiusSquared)
			{
				return true;
			}
		}
		return false;
	}

	private static void HealAgent(Agent agent)
	{
		float num = agent.HealthLimit * 0.03f;
		if (!(num <= 0f) && !(agent.Health >= agent.HealthLimit))
		{
			agent.Health = TaleWorlds.Library.MathF.Min(agent.HealthLimit, agent.Health + num);
		}
	}

	private void RepositionDefenderSpawnPoints()
	{
		try
		{
			Vec3 vec = GetHomesteadBattleCenter(homestead.GetHomesteadScene());
			if (!vec.IsValid)
			{
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", "RepositionSpawnPoints: no valid homestead center (PlayerSpawnPosition not set and no saved buildings) — spawn points unchanged.");
				return;
			}
			BattleSideEnum playerBattleSide = GetPlayerBattleSide();
			bool side = playerBattleSide == BattleSideEnum.Defender;
			string text = SpawnTagForSide(playerBattleSide);
			string text2 = SpawnTagForSide(side ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"RepositionSpawnPoints: playerSide={playerBattleSide} playerTag='{text}' enemyTag='{text2}' for '{homestead.Name}'.");
			if (!IsInsideBattleWalkArea(vec))
			{
				Vec3 position = ClampToNearestWalkAreaEdge(vec);
				if (!position.IsValid)
				{
					position = TryGetWalkAreaCentroid();
				}
				if (!position.IsValid)
				{
					TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"RepositionSpawnPoints: homestead centre ({vec.x:0.##},{vec.y:0.##}) is outside walk_area with no fallback — spawn points unchanged.");
					return;
				}
				float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(position);
				vec = new Vec3(position.x, position.y, groundHeightAtPosition);
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"RepositionSpawnPoints: homestead centre was outside walk_area — clamped to ({vec.x:0.##},{vec.y:0.##}).");
			}
			float groundHeightAtPosition2 = Mission.Current.Scene.GetGroundHeightAtPosition(vec);
			vec = new Vec3(vec.x, vec.y, groundHeightAtPosition2);
			int num = 0;
			num += MoveEntitiesToCenter(text, vec);
			num += MoveEntitiesToCenter("sp_player_back", vec);
			foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("sp_battle_set"))
			{
				if (item.HasTag(text) || (playerBattleSide == BattleSideEnum.Defender && item.HasTag("defender")) || (playerBattleSide == BattleSideEnum.Attacker && item.HasTag("attacker")))
				{
					MatrixFrame frame = item.GetFrame();
					frame.origin = vec;
					item.SetGlobalFrame(in frame);
					num++;
				}
			}
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"RepositionSpawnPoints: moved {num} player-side spawn entities to homestead center=({vec.x:0.##},{vec.y:0.##},{vec.z:0.##}); enemy spawn '{text2}' left at natural position for '{homestead.Name}'.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", "RepositionSpawnPoints failed: " + ex.Message);
		}
	}

	private static BattleSideEnum GetPlayerBattleSide()
	{
		try
		{
			MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
			if (playerMapEvent != null)
			{
				BattleSideEnum playerSide = playerMapEvent.PlayerSide;
				TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"GetPlayerBattleSide: MapEvent.PlayerSide={playerSide}.");
				return playerSide;
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", "GetPlayerBattleSide: could not read MapEvent.PlayerSide (" + ex.GetType().Name + ": " + ex.Message + ") — defaulting to Defender.");
		}
		return BattleSideEnum.Defender;
	}

	private static Vec3 GetEntityCentroid(string tag)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 0;
		foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag(tag))
		{
			Vec3 globalPosition = item.GlobalPosition;
			num += globalPosition.x;
			num2 += globalPosition.y;
			num3 += globalPosition.z;
			num4++;
		}
		if (num4 <= 0)
		{
			return Vec3.Invalid;
		}
		return new Vec3(num / (float)num4, num2 / (float)num4, num3 / (float)num4);
	}

	private static int MoveEntitiesToCenter(string tag, Vec3 center)
	{
		List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag(tag).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			GameEntity gameEntity = list[i];
			MatrixFrame frame = gameEntity.GetFrame();
			float x = (float)i * (TaleWorlds.Library.MathF.PI * 2f / (float)TaleWorlds.Library.MathF.Max(list.Count, 1));
			float num = 3f + (float)(i / 8) * 4f;
			Vec3 vec = center + new Vec3(TaleWorlds.Library.MathF.Cos(x) * num, TaleWorlds.Library.MathF.Sin(x) * num);
			vec.z = Mission.Current.Scene.GetGroundHeightAtPosition(vec);
			frame.origin = vec;
			gameEntity.SetGlobalFrame(in frame);
		}
		return list.Count;
	}

	private static Vec3 TryGetWalkAreaCentroid()
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
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", "TryGetWalkAreaCentroid: failed (" + ex.GetType().Name + ": " + ex.Message + ").");
			return Vec3.Invalid;
		}
	}

	private static bool IsInsideBattleWalkArea(Vec3 position)
	{
		try
		{
			Mission.MBBoundaryCollection mBBoundaryCollection = Mission.Current?.Boundaries;
			if (mBBoundaryCollection == null)
			{
				return true;
			}
			if (!mBBoundaryCollection.TryGetValue("walk_area", out var points) || points == null || points.Count < 3)
			{
				return true;
			}
			return PointInPolygon(position.AsVec2, points);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", "IsInsideBattleWalkArea: boundary check failed (" + ex.GetType().Name + ": " + ex.Message + "); assuming inside.");
			return true;
		}
	}

	private static Vec3 ClampToNearestWalkAreaEdge(Vec3 desired)
	{
		try
		{
			Vec2 closestBoundaryPosition = Mission.Current.GetClosestBoundaryPosition(desired.AsVec2);
			Vec3 vec = TryGetWalkAreaCentroid();
			Vec2 vec3;
			if (vec.IsValid)
			{
				Vec2 vec2 = vec.AsVec2 - closestBoundaryPosition;
				vec3 = ((!(vec2.Normalize() > 0.001f)) ? closestBoundaryPosition : (closestBoundaryPosition + vec2 * 20f));
			}
			else
			{
				vec3 = closestBoundaryPosition;
			}
			float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(new Vec3(vec3.x, vec3.y));
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"ClampToNearestWalkAreaEdge: desired=({desired.x:0.##},{desired.y:0.##}) boundaryPt=({closestBoundaryPosition.x:0.##},{closestBoundaryPosition.y:0.##}) inset=({vec3.x:0.##},{vec3.y:0.##}).");
			return new Vec3(vec3.x, vec3.y, groundHeightAtPosition);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", "ClampToNearestWalkAreaEdge failed (" + ex.GetType().Name + ": " + ex.Message + ").");
			return Vec3.Invalid;
		}
	}

	private static Vec3 ComputeWalkAreaFarPoint(Vec3 homesteadCenter)
	{
		try
		{
			Mission.MBBoundaryCollection mBBoundaryCollection = Mission.Current?.Boundaries;
			if (mBBoundaryCollection == null || !mBBoundaryCollection.TryGetValue("walk_area", out var points) || points == null || points.Count < 3)
			{
				return Vec3.Invalid;
			}
			Vec2 vec = default(Vec2);
			float num = -1f;
			foreach (Vec2 item in points)
			{
				float num2 = item.x - homesteadCenter.x;
				float num3 = item.y - homesteadCenter.y;
				float num4 = num2 * num2 + num3 * num3;
				if (num4 > num)
				{
					num = num4;
					vec = item;
				}
			}
			if (num < 0f)
			{
				return Vec3.Invalid;
			}
			Vec3 vec2 = TryGetWalkAreaCentroid();
			Vec2 vec3 = vec;
			if (vec2.IsValid)
			{
				Vec2 vec4 = vec2.AsVec2 - vec;
				float num5 = vec4.Normalize();
				if (num5 > 0.001f)
				{
					vec3 = vec + vec4 * Math.Min(15f, num5 * 0.3f);
				}
			}
			float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(new Vec3(vec3.x, vec3.y));
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"ComputeWalkAreaFarPoint: homestead=({homesteadCenter.x:0.##},{homesteadCenter.y:0.##}) " + $"farthestVertex=({vec.x:0.##},{vec.y:0.##}) " + $"insetResult=({vec3.x:0.##},{vec3.y:0.##}).");
			return new Vec3(vec3.x, vec3.y, groundHeightAtPosition);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", "ComputeWalkAreaFarPoint failed (" + ex.GetType().Name + ": " + ex.Message + ").");
			return Vec3.Invalid;
		}
	}

	private static bool PointInPolygon(Vec2 point, IEnumerable<Vec2> polygon)
	{
		bool flag = false;
		Vec2 a = default(Vec2);
		bool flag2 = true;
		Vec2 b = default(Vec2);
		foreach (Vec2 item in polygon)
		{
			if (flag2)
			{
				a = item;
				b = item;
				flag2 = false;
				continue;
			}
			if (CrossesRay(point, a, item))
			{
				flag = !flag;
			}
			a = item;
		}
		if (!flag2 && CrossesRay(point, a, b))
		{
			flag = !flag;
		}
		return flag;
	}

	private static bool CrossesRay(Vec2 point, Vec2 a, Vec2 b)
	{
		if (a.y > point.y != b.y > point.y)
		{
			return point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
		}
		return false;
	}

	private List<(Vec3 center, float radius)> GetBuildingExclusions()
	{
		List<(Vec3, float)> list = new List<(Vec3, float)>();
		foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> loadedSavedEntity in homestead.GetHomesteadScene().LoadedSavedEntities)
		{
			string text = loadedSavedEntity.Value?.Placeable?.PrefabName;
			if (text != null)
			{
				float battleFootprintRadius = HomesteadScenePlaceable.GetBattleFootprintRadius(text);
				if (battleFootprintRadius > 0f)
				{
					list.Add((loadedSavedEntity.Key.GlobalPosition, battleFootprintRadius));
				}
			}
		}
		return list;
	}

	private static Vec3 SpreadAroundClear(Vec3 center, int index, int total, IReadOnlyList<(Vec3 center, float radius)> exclusions)
	{
		if (exclusions.Count == 0)
		{
			return SpreadAround(center, index, total);
		}
		int num = Math.Max(index, 1);
		float num2 = (float)num * (TaleWorlds.Library.MathF.PI * 2f / (float)TaleWorlds.Library.MathF.Max(total, 1));
		Vec3 result = Vec3.Invalid;
		float num3 = float.MinValue;
		for (int i = 0; i < 12; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				float x = num2 + (float)j * (TaleWorlds.Library.MathF.PI / 4f);
				float num4 = num / 6 + i;
				float num5 = 1.5f + num4 * 2f;
				float x2 = center.x + TaleWorlds.Library.MathF.Cos(x) * num5;
				float y = center.y + TaleWorlds.Library.MathF.Sin(x) * num5;
				float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(new Vec3(x2, y));
				Vec3 vec = new Vec3(x2, y, groundHeightAtPosition);
				float num6 = float.MaxValue;
				foreach (var exclusion in exclusions)
				{
					Vec3 item = exclusion.center;
					float item2 = exclusion.radius;
					float num7 = vec.x - item.x;
					float num8 = vec.y - item.y;
					float num9 = TaleWorlds.Library.MathF.Sqrt(num7 * num7 + num8 * num8) - item2;
					if (num9 < num6)
					{
						num6 = num9;
					}
				}
				if (num6 > 0f)
				{
					return vec;
				}
				if (num6 > num3)
				{
					num3 = num6;
					result = vec;
				}
			}
		}
		if (result.IsValid)
		{
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"SpreadAroundClear: no fully-clear position for agent index {index} ({96} candidates all overlap a footprint); using least-blocked spot (clearance {num3:0.##} m).");
			return result;
		}
		return SpreadAround(center, index, total);
	}

	private static Vec3 GetHomesteadBattleCenter(HomesteadScene homesteadScene)
	{
		if (homesteadScene.PlayerSpawnPosition.IsValid)
		{
			if (IsInsideBattleWalkArea(homesteadScene.PlayerSpawnPosition))
			{
				return homesteadScene.PlayerSpawnPosition;
			}
			TraceLogger.Write("HomesteadBattleSceneMissionLogic", $"GetHomesteadBattleCenter: PlayerSpawnPosition ({homesteadScene.PlayerSpawnPosition.x:0.##},{homesteadScene.PlayerSpawnPosition.y:0.##}) " + "is outside walk_area — falling back to building centroid.");
		}
		if (homesteadScene.SavedEntities == null || homesteadScene.SavedEntities.Count == 0)
		{
			return Vec3.Invalid;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 0;
		foreach (HomesteadSceneSavedEntity savedEntity in homesteadScene.SavedEntities)
		{
			num += savedEntity.posX;
			num2 += savedEntity.posY;
			num3 += savedEntity.posZ;
			num4++;
		}
		if (num4 == 0)
		{
			return Vec3.Invalid;
		}
		return new Vec3(num / (float)num4, num2 / (float)num4, num3 / (float)num4);
	}
}
