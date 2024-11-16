using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using RealmsForgotten.HuntableHerds.Extensions;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Source.Objects;
using SandBox.Objects.Usables;
using System.Collections.Generic;
using System.Linq;
using SandBox.Objects.AreaMarkers;
using SandBox;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.ObjectSystem;
using RealmsForgotten.HuntableHerds.AgentComponents;
using RealmsForgotten.HuntableHerds.Models;
using System.Text;
using static RealmsForgotten.RFCustomSettlements.ExploreSettlementStateHandler;
using static RealmsForgotten.RFCustomSettlements.CustomSettlementBuildData;
using System.Threading.Tasks;
using RFCustomSettlements;
using HuntableHerds.Models;
using SandBox.AI;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class CustomSettlementMissionLogic : MissionBehavior
    {
        private class UsedObject
        {
            public UsedObject(UsableMachine machine, bool isMachineAITicked)
            {
                Machine = machine;
                MachineAI = machine.CreateAIBehaviorObject();
                IsMachineAITicked = isMachineAITicked;
            }
            public readonly UsableMachine Machine;
            public readonly UsableMachineAIBase MachineAI;
            public bool IsMachineAITicked;
        }

        private static int _disabledFaceId;
        private static int _disabledFaceIdForAnimals;
        private bool isMissionInitialized;
        private readonly List<PatrolArea> patrolAreas;
        private readonly List<CommonAreaMarker> areaMarkers;
        private readonly List<GameEntity> animalSpawnPositions = new();
        private Dictionary<int, GameEntity> NpcSpawnPositions = new();
        private readonly Dictionary<Agent, CustomSettlementMissionLogic.UsedObject> defenderAgentObjects;
        private readonly ItemRoster loot;
        private int goldLooted = 0;
        private readonly MobileParty banditsInSettlement;
        private readonly CustomSettlementBuildData BanditsData;
        private readonly Action? OnBattleEnd;
        private readonly Dictionary<int, NpcData> NpcsInSettlement = new();
        public Dictionary<Agent, Vec3> LootableAgents { get; } = new ();

        //private  onStateChangeListeners

        public CustomSettlementMissionLogic(CustomSettlementBuildData buildData, Action? onBattleEnd = null)
        {
            defenderAgentObjects = new Dictionary<Agent, CustomSettlementMissionLogic.UsedObject>();
            patrolAreas = new();
            areaMarkers = new();
            loot = new();
            banditsInSettlement = CreateBanditData(buildData);
            foreach(NpcData data in buildData.allNpcs)
            {
                if (!NpcsInSettlement.ContainsKey(data.TagId))
                    NpcsInSettlement[data.TagId] = data;
                else
                    HuntableHerds.SubModule.PrintDebugMessage($"Error, multiple Npcs with same tag: {data.TagId}", 255, 0, 0);
            }
            BanditsData = buildData;
            NextSceneData.Instance.shouldSwitchScenes = false;
            OnBattleEnd = onBattleEnd;
        }
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Agent.Main == null)
                return;

            this.UsedObjectTick(dt);

            if (!isMissionInitialized)
            {
                InitializeMission();
                isMissionInitialized = true;
                Globals.IsMissionInitialized = true;
                return;
            }
        }
        private async Task AddBodyToLootableList(Agent agent)
        {
            await Task.Delay(1000);
            LootableAgents.Add(agent, agent.GetChestGlobalPosition());
        }
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent.Components.Any(c => c is LootableAgentComponent))
            {
                AddBodyToLootableList(affectedAgent);
            }
        }
        private void UsedObjectTick(float dt)
        {
            foreach (KeyValuePair<Agent, UsedObject> keyValuePair in defenderAgentObjects)
            {
                if (keyValuePair.Value.IsMachineAITicked)
                {
                    keyValuePair.Value.MachineAI.Tick(keyValuePair.Key, null, null, dt);
                }
            }
        }
        private void InitializeMission()
        {
            areaMarkers.AddRange(from area in base.Mission.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>()
                                       orderby area.AreaIndex
                                       select area);
            
            patrolAreas.AddRange(from area in base.Mission.ActiveMissionObjects.FindAllWithType<PatrolArea>()
                                 orderby area.AreaIndex
                                 select area);
            animalSpawnPositions.AddRange(Mission.Current.Scene.FindEntitiesWithTag("spawnpoint_herdanimal"));
            Mission.MakeDefaultDeploymentPlans();
            NpcSpawnPositions = new();
            int i = 1;
            GameEntity gameEntity;
            do
            {
                gameEntity = base.Mission.Scene.FindEntityWithTag("rf_Npc_" + i);
                if (gameEntity != null) NpcSpawnPositions.Add(i, gameEntity);
                ++i;
            } while (gameEntity != null);

            SpawnPatrollingTroops(patrolAreas);
            SpawnStandingTroops(areaMarkers);
            SpawnHuntableHerdsAnimals();
            SpawnNpcs();
            SpawnPlayerTroops();
        }

        private void SpawnNpcs()
        {

            Dictionary<string, List<UsableMachine>> _usablePoints = new();
            foreach (UsableMachine usableMachine in base.Mission.MissionObjects.FindAllWithType<UsableMachine>())
            {
                foreach (string key in usableMachine.GameEntity.Tags)
                {
                    if (!_usablePoints.ContainsKey(key))
                    {
                        _usablePoints.Add(key, new List<UsableMachine>());
                    }
                    _usablePoints[key].Add(usableMachine);
                }
            }
            if (NpcSpawnPositions.Count == 0) return;
            foreach (KeyValuePair<int, GameEntity>  pair in NpcSpawnPositions)
            {
                var NpcSpawnPoint = pair.Value.GetChildren().FirstOrDefault();
                Team team = base.Mission.PlayerAllyTeam;
                try
                {
                    NpcData currentNpcData = NpcsInSettlement[pair.Key];
                    string characterId = currentNpcData.Id;
                    //string? characterId = Helper.GetCharacterIdfromEntityName(pair.Value.Name);
                    Vec3 position = NpcSpawnPoint.GetGlobalFrame().origin;
                    CharacterObject troop = MBObjectManager.Instance.GetObject<CharacterObject>(characterId);
                    AgentBuildData agentBuildData = new AgentBuildData(troop).InitialPosition(position);
                    Vec2 vec = new(NpcSpawnPoint.GetGlobalFrame().rotation.f.AsVec2.x, NpcSpawnPoint.GetGlobalFrame().rotation.f.AsVec2.y);
                    AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).TroopOrigin(new SimpleAgentOrigin(troop, -1, null, default)).Team(team);
                    Agent agent = Mission.Current.SpawnAgent(agentBuildData2, false);

                    AnimationSystemData animationSystemData = agentBuildData.AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSetWithSuffix(agentBuildData.AgentMonster, agentBuildData.AgentIsFemale, currentNpcData.ActionSet), agent.Character.GetStepSize(), false);
                    agent.SetActionSet(ref animationSystemData);

                    agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
                    StandingPoint animationPoint = NpcSpawnPoint.GetFirstScriptOfType<StandingPoint>();
                    agent.UseGameObject(animationPoint);
                    SimulateTick(agent);
                }
                catch { HuntableHerds.SubModule.PrintDebugMessage($"ERROR, could not spawn Npc from game entity of name: {pair.Value.Name}"); }
            }
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
                    ItemRosterElement rosterElement = new(spawnObject);
                    Vec2 initialDirection = frame.rotation.f.AsVec2;
                    Agent agent = base.Mission.SpawnMonster(rosterElement, default, in position, in initialDirection);

                    HerdBuildData herdBuildData = (from buildData in HerdBuildData.allHuntableAgentBuildDatas where buildData.SpawnId == entity.Name select buildData).ElementAt(0);
                    HerdBuildData.CurrentHerdBuildData = herdBuildData;
                    HerdAgentComponent huntAgentComponent = herdBuildData.IsPassive ? new PassiveHerdAgentComponent(agent) : new AggressiveHerdAgentComponent(agent);

                    agent.AddComponent(huntAgentComponent);

                    //animals.Add(agent, huntAgentComponent);

                    for (int i = 0; i < 3; i++)
                    {
                        agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), true);
                    }
                }
                catch { }
            }
        }
        private MobileParty CreateBanditData(CustomSettlementBuildData bd)
        {
            List<string> banditIDs = new();

            MobileParty mparty = new();

            foreach (KeyValuePair<int, CustomSettlementBuildData.RFBanditData> pair in bd.patrolAreasBandits)
            {
                try
                {

                    if (!banditIDs.Contains(pair.Value.Id))
                    {
                        banditIDs.Add(pair.Value.Id);
                        var troop = MBObjectManager.Instance.GetObject<CharacterObject>(pair.Value.Id);
                        mparty.AddElementToMemberRoster(troop, 1);
                    }
                }
                catch
                {
                    HuntableHerds.SubModule.PrintDebugMessage($"Error, there is no character with id \"{pair.Value.Id}\"", 255, 0, 0);
                }
            }
            foreach(KeyValuePair<int, List<CustomSettlementBuildData.RFBanditData>> pair in bd.stationaryAreasBandits)
            {
                foreach(CustomSettlementBuildData.RFBanditData banditData in pair.Value)
                {
                    try
                    {
                        banditIDs.Add(banditData.Id);
                        CharacterObject troop = MBObjectManager.Instance.GetObject<CharacterObject>(banditData.Id);
                        mparty.AddElementToMemberRoster(troop, 1);
                    }
                    catch
                    {
                        HuntableHerds.SubModule.PrintDebugMessage($"Error, there is no character with id \"{banditData.Id}\"", 255, 0, 0);
                    }
                }
            }
            mparty.ActualClan = Clan.All.Where(c => c.StringId == "looters").ElementAt(0);
            return mparty;
        }
        private void SpawnStandingTroops(List<CommonAreaMarker> areaMarkers)
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
                Queue<StandingPoint> usableMachinesQueue = new(usableMachinesInArea);

                for(int i = 0; i < allBandits; i++)
                {
                    try
                    {
                        RFBanditData currentBanditData = BanditsData.patrolAreasBandits[areaIndex];
                        RFAgentOrigin agentToSpawn = PrepareAgentToSpawn(ChooseBanditToSpawn(banditsInArea));
                        standingPoint = usableMachinesQueue.Dequeue();
                        globalFrame = standingPoint.GameEntity.GetGlobalFrame();
                        globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                        Agent agent = Mission.Current.SpawnTroop(agentToSpawn, false, false, false, false, 0, 0, false, false, false, new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);
                        AddLootableComponent(currentBanditData.ItemDropsData, agent);
                        InitializeBanditAgent(agent, standingPoint, false, defenderAgentObjects);
                    }
                    catch(InvalidOperationException)
                    {
                        HuntableHerds.SubModule.PrintDebugMessage($"error spawning the bandits in common area {areaIndex}, not enough animation points. found: {commonAreaMarker.GetUsableMachinesInRange(null).Count}, needed: {allBandits}");
                    }
                    catch
                    {
                        HuntableHerds.SubModule.PrintDebugMessage($"error spawning the bandits in common area {areaIndex}");
                    }
                }
            }
        }
        private void SpawnPatrollingTroops(List<PatrolArea> patrolAreas)
        {
            IEnumerable<PatrolArea> source = from area in patrolAreas
                                             where area.StandingPoints.All((StandingPoint point) => !point.HasUser && !point.HasAIMovingTo)
                                             select area;
            foreach(PatrolArea area in source)
            {
                int areaIndex = area.AreaIndex;
                try
                {
                    if (!BanditsData.patrolAreasBandits.ContainsKey(areaIndex)) continue;
                    RFBanditData currentBanditData = BanditsData.patrolAreasBandits[areaIndex];

                    MatrixFrame globalFrame = area.GameEntity.GetGlobalFrame();
                    globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                    RFAgentOrigin troopToSpawn = PrepareAgentToSpawn(currentBanditData.Id);
                    Agent agent = Mission.Current.SpawnTroop(troopToSpawn, false, false, false, false, 0, 0, false, false, false, new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);
                    AddLootableComponent(currentBanditData.ItemDropsData, agent);
                    InitializeBanditAgent(agent, area.StandingPoints[0], false, defenderAgentObjects);
                }
                catch (Exception)
                {
                    HuntableHerds.SubModule.PrintDebugMessage($"error spawning the bandits in patrol area {areaIndex}");
                }
            }
        }
        private void AddLootableComponent(ItemDropsData? data, Agent agent)
        {
            if (data != null)
                agent.AddComponent(new LootableAgentComponent(agent, data));
        }
        private void SpawnPlayerTroops()
        {
            TroopRoster? troopRoster;
            if ((troopRoster = NextSceneData.Instance.playerTroopRoster) == null) return;
            FlattenedTroopRoster flattenedTR = troopRoster.ToFlattenedRoster();
            foreach(TroopRosterElement troop in  troopRoster.GetTroopRoster())
            {
                CharacterObject? character;
                if ((character = troop.Character) == Hero.MainHero.CharacterObject) continue;
                for(int i = 0; i < troop.Number; i++)
                {
                    UniqueTroopDescriptor descriptor = flattenedTR.FindIndexOfCharacter(character);
                    RFAgentOrigin troopToSpawn = new(Hero.MainHero.PartyBelongedTo.Party, descriptor, character.Tier, character, true);
                    _ = Mission.Current.SpawnTroop(troopToSpawn, true, true, false, false, 0, 0, true, true, true, null, null, null, null, FormationClass.NumberOfAllFormations, false);
                }
            }
            foreach (Formation formation in Mission.Current.AttackerTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(formation.QuerySystem.MedianPosition));
                }
                formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
                if (Mission.Current.AttackerTeam == Mission.Current.PlayerTeam)
                {
                    formation.PlayerOwner = Mission.Current.MainAgent;
                }
            }
        }
        private RFAgentOrigin PrepareAgentToSpawn(string banditId)
        {
            FlattenedTroopRoster flattenedTR = banditsInSettlement.MemberRoster.ToFlattenedRoster();
            CharacterObject troop = MBObjectManager.Instance.GetObject<CharacterObject>(banditId);
            UniqueTroopDescriptor descriptor = flattenedTR.FindIndexOfCharacter(troop);
            RFAgentOrigin rFAgentOrigin = new(new PartyBase(banditsInSettlement), descriptor, troop.Tier, troop);
            return rFAgentOrigin;
        }
        private string ChooseBanditToSpawn(Dictionary<string, int> banditsInArea)
        {
            KeyValuePair<string, int> banditPair = banditsInArea.GetRandomElementInefficiently();
            banditsInArea[banditPair.Key] -= 1;
            if (banditsInArea[banditPair.Key] < 1) banditsInArea.Remove(banditPair.Key);
            return banditPair.Key;
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
            NextSceneData.Instance.goldLoot = goldLooted;
            NextSceneData.Instance.itemLoot = loot;

            if (NextSceneData.Instance.shouldSwitchScenes == false)
                NextSceneData.Instance.currentState = NextSceneData.RFExploreState.Finished;
            if (OnBattleEnd != null) this.OnBattleEnd();
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

            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).CivilianEquipment(false).NoHorses(false).NoWeapons(false).ClothingColor1(base.Mission.PlayerTeam.Color).ClothingColor2(base.Mission.PlayerTeam.Color2).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, playerCharacter, -1, default, false)).MountKey(MountCreationKey.GetRandomMountKeyString(playerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, playerCharacter.GetMountKeySeed())).Controller(Agent.ControllerType.Player);

            Hero heroObject = playerCharacter.HeroObject;

            if ((heroObject?.ClanBanner) != null)
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
            //BehaviorTree.Visit
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
                        formation?.Team.DetachmentManager.RemoveScoresOfAgentFromDetachments(agent);
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
                ItemRosterElement itemRosterElement = new(Game.Current.ObjectManager.GetObject<ItemObject>("chicken"), 0, null);
                globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                Mission mission = Mission.Current;
                ItemRosterElement rosterElement = itemRosterElement;
                ItemRosterElement harnessRosterElement = default;
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
        internal void OnAgentLooted(Agent agent)
        {
            if (Helper.IsLootableDeadAgent(agent))
            {
                LootableAgentComponent component = agent.GetComponent<LootableAgentComponent>();
                bool playSound = component.GetItemDrops().Count != 0 || component.GoldDrop != 0;
                foreach (ItemRosterElement item in component.GetItemDrops())
                {
                    EquipmentElement element = item.EquipmentElement;
                    loot.AddToCounts(element, item.Amount);
                    HuntableHerds.SubModule.PrintDebugMessage("You looted " + item.Amount + " " + element.Item.Name + "!");
                }
                if (component.GoldDrop != 0)
                {
                    goldLooted += component.GoldDrop;
                    HuntableHerds.SubModule.PrintDebugMessage("You found " + goldLooted + "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                }

                if (playSound) 
                    Mission.MakeSoundOnlyOnRelatedPeer(SoundEvent.GetEventIdFromString("event:/mission/combat/pickup_arrows"), agent.Position, Mission.MainAgent.Index);
                LootableAgents.Remove(agent);
            }
        }
        internal void OnObjectUsed(UsablePlace usablePlace)
        {
            switch (Helper.ChooseObjectType(usablePlace.GameEntity.Name))
            {
                case Helper.RFUsableObjectType.Pickable:
                    LootItem(usablePlace);
                    break;
                case Helper.RFUsableObjectType.Passage:
                    StartNewMission(usablePlace);
                    break;
                case Helper.RFUsableObjectType.Healing:
                    DoHealing(usablePlace);
                    break;
                default:
                    break;
            }
        }
        private void LootItem(UsablePlace usablePlace)
        {
            try
            {
                string[] itemData = usablePlace.GameEntity.Name.Split('_');
                string itemId = Helper.GetRFPickableObjectName(itemData);
                int amount = Helper.GetGoldAmount(itemData);
                string soundEventId = "";
                if (itemId == "gold")
                {
                    goldLooted += amount;
                    soundEventId = "event:/ui/notification/coins_positive";
                    HuntableHerds.SubModule.PrintDebugMessage("You found " + goldLooted + "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                }
                else
                {
                    ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                    loot.AddToCounts(item, amount);
                    HuntableHerds.SubModule.PrintDebugMessage("You found " + item.Name + "!");
                    soundEventId = "event:/mission/combat/pickup_arrows";
                }
                Mission.MakeSoundOnlyOnRelatedPeer(SoundEvent.GetEventIdFromString(soundEventId), usablePlace.GameEntity.GlobalPosition, Mission.MainAgent.Index);
                usablePlace.GameEntity.ClearComponents();
            }
            catch
            {
                string str = "Error in game entity name " + usablePlace.GameEntity.Name;
                HuntableHerds.SubModule.PrintDebugMessage(str, 255, 0, 0);
            }
        }
        private void StartNewMission(UsablePlace usablePlace)
        {
            StringBuilder sb = new();
            string[] data = usablePlace.GameEntity.Name.Split('_');
            foreach (string str in data.Skip(2))
                sb.Append(str + "_");
            sb.Remove(sb.Length - 1, 1);
            string newSceneId = sb.ToString();
            Mission.Current.EndMission();
            NextSceneData.Instance.shouldSwitchScenes = true;
            NextSceneData.Instance.newSceneId = newSceneId;
            NextSceneData.Instance.currentState = NextSceneData.RFExploreState.SwitchScene;
        }
        private void DoHealing(UsablePlace usablePlace)
        {
            Agent.Main.Health = Agent.Main.HealthLimit;
            usablePlace.GameEntity.ClearComponents();
        }
    }
}