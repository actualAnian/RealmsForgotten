using System;
using System.Collections.Generic;
using System.Linq;
using Homesteads.Models;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

public class HomesteadDogCombatLogic : MissionLogic
{
	private readonly Homestead homestead;

	private bool hasSpawnedDogs;

	private readonly List<Agent> combatDogs = new List<Agent>();

	internal IReadOnlyList<Agent> CombatDogs => combatDogs;

	public HomesteadDogCombatLogic(Homestead homestead)
	{
		this.homestead = homestead;
	}

	public override void OnMissionTick(float dt)
	{
		if (Mission.Current.Mode == MissionMode.Battle && !hasSpawnedDogs && Mission.Current.MainAgent != null)
		{
			HomesteadBattleSceneMissionLogic missionBehavior = Mission.Current.GetMissionBehavior<HomesteadBattleSceneMissionLogic>();
			if (missionBehavior == null || missionBehavior.HasRepositionedPlayers)
			{
				SpawnDogs();
				hasSpawnedDogs = true;
			}
		}
	}

	private void SpawnDogs()
	{
		HomesteadSpawningMissionLogic.EnsureDogItemUsesCorrectMonster();
		HomesteadScene homesteadScene = homestead.GetHomesteadScene();
		if (homesteadScene == null || homesteadScene.SavedEntities?.Any((HomesteadSceneSavedEntity e) => e?.Placeable?.PrefabName == "homestead_dog_kennel") != true)
		{
			TraceLogger.Write("HomesteadDogCombatLogic", "SpawnDogs: no dog kennel placed in homestead — skipping battle dog spawn.");
			return;
		}
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>("dog");
		int num = ((homestead.Stash != null && itemObject != null) ? homestead.Stash.GetItemNumber(itemObject) : 0);
		int num2 = GlobalSettings<MCMSettings>.Instance?.MaxBattleDogs ?? 5;
		TraceLogger.Write("HomesteadDogCombatLogic", $"SpawnDogs: dogsInStash={num}, maxBattleDogs={num2}, kennelFound=true");
		if (num <= 0 || num2 <= 0)
		{
			return;
		}
		int num3 = Math.Min(num, num2);
		ItemObject item = itemObject;
		ItemRosterElement rosterElement = new ItemRosterElement(item);
		Vec3 position = Mission.Current.MainAgent.Position;
		for (int num4 = 0; num4 < num3; num4++)
		{
			Vec3 vec = new Vec3(MBRandom.RandomFloat * 4f - 2f, MBRandom.RandomFloat * 4f - 2f);
			Vec3 initialPosition = position + vec;
			initialPosition.z = Mission.Current.Scene.GetGroundHeightAtPosition(initialPosition);
			Agent agent;
			try
			{
				agent = Mission.Current.SpawnMonster(rosterElement, default(ItemRosterElement), in initialPosition, in Vec2.Forward);
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadDogCombatLogic", "SpawnDogs: Mission.SpawnMonster threw " + ex.GetType().Name + ": " + ex.Message + " — aborting battle-dog spawns this mission.");
				break;
			}
			if (agent != null && agent.Monster?.StringId != "dog")
			{
				TraceLogger.Write("HomesteadDogCombatLogic", "SpawnDogs: dog spawned with Monster '" + agent.Monster?.StringId + "' instead of 'dog' — EnsureDogItemUsesCorrectMonster may have failed; dog may render incorrectly.");
			}
			if (agent != null)
			{
				agent.Controller = AgentControllerType.None;
				for (int num5 = 0; num5 < 3; num5++)
				{
					agent.AgentVisuals?.GetSkeleton()?.TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), tickAnimsForChildren: true);
				}
				combatDogs.Add(agent);
				TraceLogger.Write("HomesteadDogCombatLogic", $"SpawnDogs: dog#{agent.Index} spawned at {initialPosition}");
			}
		}
	}
}
