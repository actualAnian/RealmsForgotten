using System;
using System.Runtime.Remoting.Messaging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using RealmsForgotten;
using RealmsForgotten.HuntableHerds.Extensions;
using SandBox.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Source.Objects;
using SandBox.Objects.Usables;
using System.Collections.Generic;
using System.Linq;
using SandBox.Objects.AreaMarkers;
using SandBox;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using RealmsForgotten.RFCustomSettlements.AgentOrigins;
using TaleWorlds.InputSystem;
using RealmsForgotten.HuntableHerds.AgentComponents;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.Localization;
using System.Text;
using RealmsForgotten.RFCustomSettlements;
using RealmsForgotten.HuntableHerds.Models;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class CustomSettlementMissionLogic : MissionBehavior
    {
        private class UsedObject
        {
            public UsedObject(UsableMachine machine, bool isMachineAITicked)
            {
                this.Machine = machine;
                this.MachineAI = machine.CreateAIBehaviorObject();
                this.IsMachineAITicked = isMachineAITicked;
            }

            public readonly UsableMachine Machine;

            public readonly UsableMachineAIBase MachineAI;


            public bool IsMachineAITicked;
        }



        private static int _disabledFaceId;
        private static int _disabledFaceIdForAnimals;
        private bool isMissionInitialized;
        private List<PatrolArea> patrolAreas;
        private List<CommonAreaMarker> areaMarkers;
        private IMissionTroopSupplier troopSupplier;
        private List<GameEntity> animalSpawnPositions = new();
        private readonly Dictionary<Agent, CustomSettlementMissionLogic.UsedObject> defenderAgentObjects;
        private List<MissionObject> pickableItems;
        private ItemRoster loot;
        private int goldLooted = 0;
        private MobileParty banditsInSettlement;
        private CustomSettlementBuildData BanditsData;
        public CustomSettlementMissionLogic(CustomSettlementBuildData buildData)
        {
            defenderAgentObjects = new Dictionary<Agent, CustomSettlementMissionLogic.UsedObject>();
            patrolAreas = new();
            areaMarkers = new();
            loot = new();
            banditsInSettlement = CreateBanditData(buildData);
            BanditsData = buildData;
        }
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Agent.Main == null)
                return;

            if (Input.IsKeyPressed(InputKey.Q))
                LootArea();

            if (!isMissionInitialized)
            {
                InitializeMission();
                isMissionInitialized = true;
                return;
            }
        }

        private void LootArea()
        {
            bool foundItems = false;
            List<MissionObject> objectsToRemove = new List<MissionObject>();
            foreach (MissionObject missionObject in pickableItems)
            {
                if(missionObject.GameEntity.GlobalPosition.Distance(Agent.Main.Position) < Config.maxPickableDistance)
                {
                    try
                    {
                        string itemId;
                        int amount;
                        ParseEditorItemData(missionObject.GameEntity.Name.Split('_'), out itemId, out amount);
                        string soundEventId = "";
                        if (itemId == "gold")
                        {
                            goldLooted += amount;
                            soundEventId = "event:/ui/notification/coins_positive";
                            HuntableHerds.SubModule.PrintDebugMessage("You found " + goldLooted + " gold coins!");
                        }
                        else
                        {
                            ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                            loot.AddToCounts(item, amount);
                            HuntableHerds.SubModule.PrintDebugMessage("You found " + item.Name + "!");
                            soundEventId = "event:/mission/combat/pickup_arrows";
                        }
                        foundItems = true;
                        Mission.MakeSoundOnlyOnRelatedPeer(SoundEvent.GetEventIdFromString(soundEventId), missionObject.GameEntity.GlobalPosition, Mission.MainAgent.Index);
                        missionObject.GameEntity.ClearComponents();
                        objectsToRemove.Add(missionObject);
                    }
                    catch
                    {
                        string str = "Error in game entity name" + missionObject.GameEntity.Name;
                        HuntableHerds.SubModule.PrintDebugMessage(str, 255, 0, 0);
                    }
                }
            }

            pickableItems.RemoveAll(m => objectsToRemove.Contains(m));
            if (!foundItems)
            {
                HuntableHerds.SubModule.PrintDebugMessage("There's nothing to loot nearby...");
                return;
            }
        }

        private static void ParseEditorItemData(string[] EditorItemString, out string itemId, out int amount)
        {
            string[] data = EditorItemString;
            StringBuilder itemIdBuilder = new();
            foreach (string str in data.Skip(2).Take(data.Length - 3))
                itemIdBuilder.Append(str + "_");
            itemIdBuilder.Remove(itemIdBuilder.Length - 1, 1);
            itemId = itemIdBuilder.ToString();
            amount = int.Parse(data.Last());
        }

        private void InitializeMission()
        {
            pickableItems = base.Mission.MissionObjects.Where(m => (m.GameEntity.Name.Contains("rf_pickable"))).ToList();

            areaMarkers.AddRange(from area in base.Mission.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>()
                                       orderby area.AreaIndex
                                       select area);
            
            patrolAreas.AddRange(from area in base.Mission.ActiveMissionObjects.FindAllWithType<PatrolArea>()
                                 orderby area.AreaIndex
                                 select area);
            animalSpawnPositions.AddRange(Mission.Current.Scene.FindEntitiesWithTag("spawnpoint_herdanimal"));

            SpawnPatrollingTroops(patrolAreas, defenderAgentObjects);
            SpawnStandingTroops(areaMarkers, defenderAgentObjects);
            SpawnHuntableHerdsAnimals();
        }

        private void SpawnHuntableHerdsAnimals()
        {
            foreach(GameEntity entity in animalSpawnPositions)
            {
                try
                {
                    MatrixFrame frame = MatrixFrame.Identity;
                    Vec3 position = entity.GetGlobalFrame().origin;
                    ItemObject spawnObject = Game.Current.ObjectManager.GetObject<ItemObject>(entity.Name);
                    ItemRosterElement rosterElement = new ItemRosterElement(spawnObject);
                    Vec2 initialDirection = frame.rotation.f.AsVec2;
                    Agent agent = base.Mission.SpawnMonster(rosterElement, default(ItemRosterElement), in position, in initialDirection);

                    HerdBuildData herdBuildData = (from buildData in HerdBuildData.allHuntableAgentBuildDatas where buildData.SpawnId == entity.Name select buildData).ElementAt(0);
                    HerdAgentComponent huntAgentComponent = herdBuildData.IsPassive ? new PassiveHerdAgentComponent(agent) : new AggressiveHerdAgentComponent(agent);

                    agent.AddComponent(huntAgentComponent);

                    //animals.Add(agent, huntAgentComponent);

                    for (int i = 0; i < 3; i++)
                    {
                        agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), true);
                    }
                }
                catch 
                { }
            }
        }
        private void SpawnAnimalToHunt(Vec3 position)
        {
            MatrixFrame frame = MatrixFrame.Identity;

            ItemObject spawnObject = Game.Current.ObjectManager.GetObject<ItemObject>(HerdBuildData.CurrentHerdBuildData.SpawnId);
            ItemRosterElement rosterElement = new ItemRosterElement(spawnObject);
            Vec2 initialDirection = frame.rotation.f.AsVec2;
            Agent agent = base.Mission.SpawnMonster(rosterElement, default(ItemRosterElement), in position, in initialDirection);

            HerdAgentComponent huntAgentComponent = HerdBuildData.CurrentHerdBuildData.IsPassive ? new PassiveHerdAgentComponent(agent) : new AggressiveHerdAgentComponent(agent);

            agent.AddComponent(huntAgentComponent);

            //animals.Add(agent, huntAgentComponent);

            for (int i = 0; i < 3; i++)
            {
                agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), true);
            }
        }
        private MobileParty CreateBanditData(CustomSettlementBuildData bd)
        {
            List<string> banditIDs = new();

            MobileParty mparty = new MobileParty();

            foreach (KeyValuePair<int, CustomSettlementBuildData.RFBanditData> pair in bd.patrolAreasBandits)
            {
                if(!banditIDs.Contains(pair.Value.Id))
                {
                    banditIDs.Add(pair.Value.Id);
                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>(pair.Value.Id);
                    mparty.AddElementToMemberRoster(troop, 1);
                }
            }
            foreach(KeyValuePair<int, List<CustomSettlementBuildData.RFBanditData>> pair in bd.stationaryAreasBandits) 
            {
                foreach(CustomSettlementBuildData.RFBanditData banditData in pair.Value)
                {
                    banditIDs.Add(banditData.Id);
                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>(banditData.Id);
                    mparty.AddElementToMemberRoster(troop, 1);
                }
            }

            return mparty;            
        }

        private void SpawnStandingTroops(List<CommonAreaMarker> areaMarkers, Dictionary<Agent, CustomSettlementMissionLogic.UsedObject> defenderAgentObjects)
        {
            foreach (CommonAreaMarker commonAreaMarker in areaMarkers)
            {
                int areaIndex = commonAreaMarker.AreaIndex;
                if (!BanditsData.stationaryAreasBandits.ContainsKey(areaIndex)) continue;
                List<StandingPoint> usableMachinesInArea = new();
                StandingPoint standingPoint;
                MatrixFrame globalFrame;

                Dictionary<string, int> banditsInArea = GetTroopsInArea(BanditsData.stationaryAreasBandits[areaIndex], out int allBandits);

                foreach (UsableMachine usableMachine in commonAreaMarker.GetUsableMachinesInRange(null))
                {
                    usableMachinesInArea.AddRange(usableMachine.StandingPoints);
                }
                usableMachinesInArea.Shuffle();
                Queue<StandingPoint> usableMachinesQueue = new Queue<StandingPoint>((IEnumerable<StandingPoint>)usableMachinesInArea);

                for(int i = 0; i < allBandits; i++)
                {
                    try 
                    {
                        RFAgentOrigin agentToSpawn = PrepareAgentToSpawn(ChooseBanditToSpawn(banditsInArea));
                        standingPoint = usableMachinesQueue.Dequeue();
                        globalFrame = standingPoint.GameEntity.GetGlobalFrame();
                        globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                        Agent agent = Mission.Current.SpawnTroop(agentToSpawn, false, false, false, false, 0, 0, false, false, false, new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);

                        InitializeBanditAgent(agent, standingPoint, false, defenderAgentObjects);
                    }
                    catch
                    {
                        HuntableHerds.SubModule.PrintDebugMessage($"error spawning the bandits in area {areaIndex}");
                    }
                }

            }
        }


        private RFAgentOrigin PrepareAgentToSpawn(string banditId)
        {
            FlattenedTroopRoster flattenedTR = banditsInSettlement.MemberRoster.ToFlattenedRoster();
            UniqueTroopDescriptor descriptor = flattenedTR.FindIndexOfCharacter(MBObjectManager.Instance.GetObject<CharacterObject>(banditId));

            RFAgentOrigin rFAgentOrigin = new RFAgentOrigin(new PartyBase(banditsInSettlement), descriptor, 1, flattenedTR[descriptor]);
            return rFAgentOrigin;
        }
        private string ChooseBanditToSpawn(Dictionary<string, int> banditsInArea)
        {
            KeyValuePair<string, int> banditPair = banditsInArea.GetRandomElementInefficiently();
            banditsInArea[banditPair.Key] -= 1;
            if (banditsInArea[banditPair.Key] < 1) banditsInArea.Remove(banditPair.Key);
            return banditPair.Key;
        }

        private void SpawnPatrollingTroops(List<PatrolArea> patrolAreas, Dictionary<Agent, CustomSettlementMissionLogic.UsedObject> defenderAgentObjects)
        {
            IEnumerable<PatrolArea> source = from area in patrolAreas
                                             where area.StandingPoints.All((StandingPoint point) => !point.HasUser && !point.HasAIMovingTo)
                                             select area;
            foreach(PatrolArea area in source) 
            {
                try
                {
                    int areaIndex = area.AreaIndex;
                    if (!BanditsData.patrolAreasBandits.ContainsKey(areaIndex)) continue;

                    MatrixFrame globalFrame = area.GameEntity.GetGlobalFrame();
                    globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();

                    RFAgentOrigin troopToSpawn = PrepareAgentToSpawn(BanditsData.patrolAreasBandits[areaIndex].Id);
                    Agent agent = Mission.Current.SpawnTroop(troopToSpawn, false, false, false, false, 0, 0, false, false, false, new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);
                    InitializeBanditAgent(agent, area.StandingPoints[0], false, defenderAgentObjects);
                }
                catch (Exception) { }

            }
        }
        private Dictionary<string, int> GetTroopsInArea(List<CustomSettlementBuildData.RFBanditData> rFBanditData, out int allBandits)
        {
            Dictionary<string, int> bandits = new();
            allBandits = 0;
            foreach (CustomSettlementBuildData.RFBanditData data in rFBanditData)
            {
                bandits[data.Id] = data.Amount;
                allBandits += data.Amount;
            }
            return bandits;
        }
        private void InitializeBanditAgent(Agent agent, StandingPoint spawnPoint, bool isPatrolling, Dictionary<Agent, CustomSettlementMissionLogic.UsedObject> defenderAgentObjects)
        {
            UsableMachine usableMachine = isPatrolling ? spawnPoint.GameEntity.Parent.GetScriptComponents<PatrolArea>().FirstOrDefault<PatrolArea>() : spawnPoint.GameEntity.Parent.GetScriptComponents<UsableMachine>().FirstOrDefault<UsableMachine>();
            if (isPatrolling)
            {
                ((IDetachment)usableMachine).AddAgent(agent, -1);
                agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
            }
            else
            {
                agent.UseGameObject(spawnPoint, -1);
            }
            defenderAgentObjects.Add(agent, new CustomSettlementMissionLogic.UsedObject(usableMachine, isPatrolling));
            AgentFlag agentFlags = agent.GetAgentFlags();
            agent.SetAgentFlags((agentFlags | AgentFlag.CanGetAlarmed) & ~AgentFlag.CanRetreat);
            agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
            this.SimulateTick(agent);
        }
        protected override void OnEndMission()
        {
            CustomSettlementsCampaignBehavior.goldLoot = goldLooted;
            CustomSettlementsCampaignBehavior.itemLoot = loot;
            CustomSettlementsCampaignBehavior.finishedMission = true;
            base.OnEndMission();
        }
        private void SimulateTick(Agent agent)
        {
            int num = MBRandom.RandomInt(1, 20);
            for (int i = 0; i < num; i++)
            {
                if (agent.IsUsingGameObject)
                {
                    agent.CurrentlyUsedGameObject.SimulateTick(0.1f);
                }
            }
        }
        public override MissionBehaviorType BehaviorType 
		{
			get
			{
				return MissionBehaviorType.Other;
			}
		}

        public override void AfterStart()
        {
            SpawnPlayer();
            SpawnChicken();
            SpawnItems();
            HuntableHerds.SubModule.PrintDebugMessage("Press Q nearby interesting items to loot them!");
        }

        private void SpawnItems()
        {
            ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("relic_map_arrow");
            MissionWeapon missionWeapon = new MissionWeapon(item, new ItemModifier(), Banner.CreateOneColoredEmptyBanner(1));
            Vec3 pos = Vec3.Invalid;
            Vec3 rot = Vec3.Invalid;
            pos = new Vec3(423.71f, 326.38f, 50.81f);
            rot = new Vec3(20f, 15f, 0f);
            if (pos != Vec3.Invalid)
                this.Mission.SpawnWeaponWithNewEntityAux(missionWeapon, Mission.WeaponSpawnFlags.WithStaticPhysics, new MatrixFrame(Mat3.CreateMat3WithForward(rot),
                    pos), 0, null, false);

            this.Mission.OnItemPickUp += OnItemPickup;

        }

        private void OnItemPickup(Agent agent, SpawnedItemEntity entity)
        {
            PartyBase.MainParty.ItemRoster.AddToCounts(
                    MBObjectManager.Instance.GetObject<ItemObject>("relic_map_arrow"), 1);
        }

        private Agent SpawnPlayer()
        {
            MatrixFrame matrixFrame = MatrixFrame.Identity;
            GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("spawnpoint_player");
            CharacterObject playerCharacter = CharacterObject.PlayerCharacter;

            Vec3 playerSpawnFallback = Mission.Current.Scene.FindEntityWithName("sp_player").GlobalPosition;
            Mission.Current.GetTrueRandomPositionAroundPoint(playerSpawnFallback, 20, 500, false);

            AgentBuildData agentBuildData = new AgentBuildData(playerCharacter).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity.GetGlobalFrame().origin);

            Vec2 vec = matrixFrame.rotation.f.AsVec2;
            vec = vec.Normalized();

            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).CivilianEquipment(false).NoHorses(false).NoWeapons(false).ClothingColor1(base.Mission.PlayerTeam.Color).ClothingColor2(base.Mission.PlayerTeam.Color2).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, playerCharacter, -1, default(UniqueTroopDescriptor), false)).MountKey(MountCreationKey.GetRandomMountKeyString(playerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, playerCharacter.GetMountKeySeed())).Controller(Agent.ControllerType.Player);

            Hero heroObject = playerCharacter.HeroObject;

            if (((heroObject != null) ? heroObject.ClanBanner : null) != null)
            {
                agentBuildData2.Banner(playerCharacter.HeroObject.ClanBanner);
            }

            Agent agent = base.Mission.SpawnAgent(agentBuildData2);

            for (int i = 0; i < 3; i++)
            {
                Agent.Main.AgentVisuals.GetSkeleton().TickAnimations(0.1f, Agent.Main.AgentVisuals.GetGlobalFrame(), true);
            }

            return agent;
        }
        public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
        {
            bool flag2 = flag == Agent.AIStateFlag.Alarmed;
            if (flag2 || flag == Agent.AIStateFlag.Cautious)
            {
                if (agent.IsUsingGameObject)
                {
                    agent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
                }
                else
                {
                    agent.DisableScriptedMovement();
                    if (agent.IsAIControlled && agent.AIMoveToGameObjectIsEnabled())
                    {
                        agent.AIMoveToGameObjectDisable();
                        Formation formation = agent.Formation;
                        if (formation != null)
                        {
                            formation.Team.DetachmentManager.RemoveScoresOfAgentFromDetachments(agent);
                        }
                    }
                }
                defenderAgentObjects[agent].IsMachineAITicked = false;
            }
            else if (flag == Agent.AIStateFlag.None)
            {
                defenderAgentObjects[agent].IsMachineAITicked = true;
                agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
                ((IDetachment)defenderAgentObjects[agent].Machine).AddAgent(agent, -1);
            }
            if (flag2)
            {
                agent.SetWantsToYell();
            }
        }

        public void SpawnChicken()
        {
            GameEntity gameEntity2 = base.Mission.Scene.FindEntityWithTag("navigation_mesh_deactivator");
            if (gameEntity2 != null)
            {
                NavigationMeshDeactivator firstScriptOfType = gameEntity2.GetFirstScriptOfType<NavigationMeshDeactivator>();
                _disabledFaceId = firstScriptOfType.DisableFaceWithId;
                _disabledFaceIdForAnimals = firstScriptOfType.DisableFaceWithIdForAnimals;
            }
            foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("sp_chicken"))
            {
                MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
                ItemRosterElement itemRosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("chicken"), 0, null);
                globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                Mission mission = Mission.Current;
                ItemRosterElement rosterElement = itemRosterElement;
                ItemRosterElement harnessRosterElement = default(ItemRosterElement);
                Vec2 asVec = globalFrame.rotation.f.AsVec2;
                Agent agent = mission.SpawnMonster(rosterElement, harnessRosterElement, globalFrame.origin, asVec, -1);

                if (_disabledFaceId != -1)
                {
                    agent.SetAgentExcludeStateForFaceGroupId(_disabledFaceId, true);
                }
                if (_disabledFaceIdForAnimals != -1)
                {
                    agent.SetAgentExcludeStateForFaceGroupId(_disabledFaceId, true);
                }
                AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(gameEntity, agent);
                SimulateAnimalAnimations(agent);
            }
        }
        private static void SimulateAnimalAnimations(Agent agent)
        {
            int num = 10 + MBRandom.RandomInt(90);
            for (int i = 0; i < num; i++)
            {
                agent.TickActionChannels(0.1f);
                Vec3 v = agent.ComputeAnimationDisplacement(0.1f);
                if (v.LengthSquared > 0f)
                {
                    agent.TeleportToPosition(agent.Position + v);
                }
                agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), true);
            }
        }
    }
}