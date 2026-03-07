using BehaviorTrees;
using BehaviorTreeWrapper;
using HuntableHerds.Models;
using psai.net;
using RealmsForgotten.HuntableHerds.AgentComponents;
using RealmsForgotten.HuntableHerds.Extensions;
using RealmsForgotten.HuntableHerds.Models;
using RealmsForgotten.MusicSounds;
using RFCustomSettlements;
using RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree;
using RFCustomSettlements.Quests;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects;
using SandBox.Objects.AreaMarkers;
using SandBox.Objects.Usables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Source.Objects;
using TaleWorlds.ObjectSystem;
using static RealmsForgotten.RFCustomSettlements.CustomSettlementBuildData;
using static RealmsForgotten.RFCustomSettlements.ExploreSettlementStateHandler;

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
        private readonly MobileParty banditsInSettlement;
        private readonly CustomSettlementBuildData BanditsData;
        private readonly Action? OnBattleEnd;
        private readonly Dictionary<int, NpcData> NpcsInSettlement = new();
        public delegate void UnitKilledHandler(string id);
        public event UnitKilledHandler? UnitKilled;
        readonly CustomSettlementsCampaignBehavior _campaignBehavior = Campaign.Current.GetCampaignBehavior<CustomSettlementsCampaignBehavior>();
        IFocusable? _focusedRFObject;
        readonly string _sceneName;
        int _pickableItemsRemaining = 0;
        bool _resetEndMissionTimer = false;
        private BasicMissionTimer? _endTimer;
        float _timePassed = 0;
        bool _secondPassed = false;
        public Dictionary<Agent, Vec3> LootableAgents { get; } = new();
        public CustomSettlementMissionLogic(CustomSettlementBuildData buildData, string sceneName, RFMusicType musicTheme, Action? onBattleEnd = null)
        {
            defenderAgentObjects = new Dictionary<Agent, CustomSettlementMissionLogic.UsedObject>();
            patrolAreas = new();
            areaMarkers = new();
            banditsInSettlement = CreateBanditData(buildData);
            foreach (NpcData data in buildData.AllNpcs)
            {
                if (!NpcsInSettlement.ContainsKey(data.TagId))
                    NpcsInSettlement[data.TagId] = data;
                else
                    HuntableHerds.SubModule.PrintDebugMessage($"Error, multiple Npcs with same tag: {data.TagId}", 255, 0, 0);
            }
            BanditsData = buildData;
            NextSceneData.Instance.shouldSwitchScenes = false;
            OnBattleEnd = onBattleEnd;
            _sceneName = sceneName;
            CustomSettlementQuest.SubscribeEligibleQuests(this);
            RFMusicManager.Instance.AddRequest(musicTheme, 1);
        }
        private void AfterSecond()
        {
            RFMusicManager.Instance.PlayMusic();
        }
        public override void EarlyStart()
        {
            RegisterCustomSettlementsBehaviorTrees();
        }
        public override void OnMissionTick(float dt)
        {
            //Mission.Current.Agents[5].Components[3].OnTick(dt);
            _timePassed += dt;
            ResetLeaveMissionTimer();
            if (MissionEnded())
                EndMissionByPlayerDeath();
            base.OnMissionTick(dt);
            if (Agent.Main == null)
                return;
            UsedObjectTick(dt);
            if (!isMissionInitialized)
            {
                InitializeMission();
                isMissionInitialized = true;
                Globals.IsMissionInitialized = true;
                return;
            }
            if (!_secondPassed && _timePassed >= 1f)
            {
                _secondPassed = true;
                AfterSecond();
            }
            HandleRFFocusedObject();
            HandleLeaveMission();
        }
        private void HandleRFFocusedObject()
        {
            if (_focusedRFObject == null) return;
            Helper.IsCloseEnough(Agent.Main, _focusedRFObject);   
        }
        private void RegisterCustomSettlementsBehaviorTrees()
        {
            BTRegister.RegisterClass("HornBlowerBehaviorTree", objects => HornBlowerBehaviorTree.BuildTree(objects));
        }
        private async Task AddBodyToLootableList(Agent agent)
        {
            await Task.Delay(2000);
            Vec3 position;
            try { position = agent.GetChestGlobalPosition(); }
            catch (Exception) { position = agent.Position; }
            LootableAgents.Add(agent, position);
        }
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            int enemyAgents = Mission.Current.PlayerEnemyTeam.ActiveAgents.Count;
            if (enemyAgents < 5)
            {
                TextObject text = new("{rf_enemy_count}{ENEMY_AGENTS} enemies remaining");
                text.SetTextVariable("ENEMY_AGENTS", enemyAgents);
                InformationManager.DisplayMessage(new(text.ToString()));
            }
            string agentId = affectedAgent.Character == null ? affectedAgent.Monster.StringId : affectedAgent.Character.StringId;
            UnitKilled?.Invoke(agentId);
            if (affectedAgent.Components.Any(c => c is LootableAgentComponent))
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                AddBodyToLootableList(affectedAgent);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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
            areaMarkers.AddRange(from area in Mission.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>()
                                 orderby area.AreaIndex
                                 select area);

            patrolAreas.AddRange(from area in Mission.ActiveMissionObjects.FindAllWithType<PatrolArea>()
                                 orderby area.AreaIndex
                                 select area);
            animalSpawnPositions.AddRange(Mission.Current.Scene.FindEntitiesWithTag("spawnpoint_herdanimal"));
            Mission.DeploymentPlan.MakeDefaultDeploymentPlans();
            NpcSpawnPositions = new();
            int i = 1;
            GameEntity gameEntity;
            do
            {
                gameEntity = Mission.Scene.FindEntityWithTag("rf_Npc_" + i);
                if (gameEntity != null) NpcSpawnPositions.Add(i, gameEntity);
                ++i;
            } while (gameEntity != null);

            SpawnDynamicPatrollingTroops();
            SpawnPatrollingTroops(patrolAreas);
            SpawnStandingTroops(areaMarkers);
            SpawnHuntableHerdsAnimals();
            SpawnNpcs();
            SpawnPlayerTroops();
            RemovePreviouslyPickedItems();
            _pickableItemsRemaining = Mission.Current.ActiveMissionObjects.Where(o => o.GameEntity.Name.Contains("rf_pickable")).Count();
            Mission.Current.IsFriendlyMission = false;
            SpawnWeaponsOnTheGround();
        }
        private void SpawnWeaponsOnTheGround()
        {
            var keyword = "rf_weapon_";
            IEnumerable<MissionObject> spawnableWeapons = Mission.Current.ActiveMissionObjects.Where(o => o.GameEntity.Name.Contains(keyword));
            //possibleObjects.First().GameEntity.frame
            foreach (var weapon in spawnableWeapons)
            {
                var itemName = weapon.GameEntity.Name.Substring(keyword.Length);
                try
                {
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(itemName);
                    var entity = Mission.SpawnWeaponWithNewEntityAux(new MissionWeapon(item, null, null), Mission.WeaponSpawnFlags.WithPhysics, weapon.GameEntity.GetGlobalFrame(), 0, null, false);
                }
                catch
                {
                    InformationManager.DisplayMessage(new($"Error spawning an item with id {itemName}"));
                }
            }
        }
        private void ResetLeaveMissionTimer()
        {
            if (!_resetEndMissionTimer) return;
            _resetEndMissionTimer = false;
            Mission.Current.IsFriendlyMission = false;
        }
        private void HandleLeaveMission()
        {
            if (Mission.InputManager.IsGameKeyPressed(4) && !IsPlayerNearEnemies())
            {
                Mission.Current.IsFriendlyMission = true;
            }
            if (Mission.InputManager.IsGameKeyReleased(4))
                _resetEndMissionTimer = true;
        }
        private bool IsPlayerNearEnemies()
        {
            if (Agent.Main == null) return false;
            foreach (var enemy in Mission.PlayerEnemyTeam.ActiveAgents)
                if (Agent.Main.GetDistanceTo(enemy) < 10)
                {
                    MBInformationManager.AddQuickInformation(new("{rf_near_enemies}You are near enemies, you can't leave!"));
                    return true;
                }
            return false;
        }

        private void RemovePreviouslyPickedItems()
        {
            List<Vec3> pickedItemPos = _campaignBehavior.GetPickedObjectsFromScene(_sceneName);
            IEnumerable<MissionObject> possibleObjects = Mission.Current.ActiveMissionObjects.Where(o => o.GameEntity.Name.Contains("rf_pickable"));
            for (int i = possibleObjects.Count() -1; i >= 0; i--)
            {
                MissionObject rfObject = possibleObjects.ElementAt(i);
                if (!pickedItemPos.Contains(rfObject.GameEntity.GlobalPosition)) continue;
                rfObject.GameEntity.ClearComponents();
            }
        }
        private void SpawnNpcs()
        {
            Dictionary<string, List<UsableMachine>> _usablePoints = new();
            foreach (UsableMachine usableMachine in Mission.MissionObjects.FindAllWithType<UsableMachine>())
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
            foreach (KeyValuePair<int, GameEntity> pair in NpcSpawnPositions)
            {
                var NpcSpawnPoint = pair.Value.GetChildren().FirstOrDefault();
                Team team = base.Mission.PlayerAllyTeam;
                try
                {
                    NpcData currentNpcData = NpcsInSettlement[pair.Key];
                    string characterId = currentNpcData.Id;
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
            foreach (GameEntity entity in animalSpawnPositions)
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

            foreach (KeyValuePair<int, CustomSettlementBuildData.RFBanditData> pair in bd.PatrolAreasBandits)
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
            foreach (KeyValuePair<int, List<CustomSettlementBuildData.RFBanditData>> pair in bd.StationaryAreasBandits)
            {
                foreach (CustomSettlementBuildData.RFBanditData banditData in pair.Value)
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
                if (!BanditsData.StationaryAreasBandits.ContainsKey(areaIndex)) continue;
                List<StandingPoint> usableMachinesInArea = new();
                StandingPoint standingPoint;
                MatrixFrame globalFrame;

                Dictionary<RFBanditData, int> banditsInArea = GetTroopsInArea(BanditsData.StationaryAreasBandits[areaIndex], out int allBandits);

                foreach (UsableMachine usableMachine in commonAreaMarker.GetUsableMachinesInRange(null))
                {
                    usableMachinesInArea.AddRange(usableMachine.StandingPoints);
                }
                usableMachinesInArea.Shuffle();
                Queue<StandingPoint> usableMachinesQueue = new(usableMachinesInArea);

                for (int i = 0; i < allBandits; i++)
                {
                    try
                    {
                        RFBanditData currentBanditData = ChooseBanditToSpawn(banditsInArea);
                        standingPoint = usableMachinesQueue.Dequeue();
                        globalFrame = standingPoint.GameEntity.GetGlobalFrame();
                        globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                        Agent agent = SpawnBandit(currentBanditData, globalFrame);
                        if (currentBanditData.ItemDropsData != null)
                            AddLootableComponent(currentBanditData.ItemDropsData, agent);
                        InitializeBanditAgent(agent, standingPoint, false, defenderAgentObjects);
                    }
                    catch (InvalidOperationException)
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
        private void SpawnDynamicPatrollingTroops()
        {
            List<DynamicPatrolAreaParent> dynamicPatrolAreas = new();
            dynamicPatrolAreas.AddRange(from area in Mission.ActiveMissionObjects.FindAllWithType<DynamicPatrolAreaParent>()
                                        orderby area.UniqueId
                                        select area);
            foreach (DynamicPatrolAreaParent dynamicPatrolArea in dynamicPatrolAreas)
            {
                int areaIndex = dynamicPatrolArea.UniqueId;
                if (!BanditsData.DynamicPatrolAreasBandits.ContainsKey(areaIndex)) continue;
                try
                {
                    RFBanditData currentBanditData = BanditsData.DynamicPatrolAreasBandits[areaIndex];
                    for (int i = 0; i < currentBanditData.Amount; i++) 
                    {
                        MatrixFrame globalFrame = dynamicPatrolArea.GameEntity.GetGlobalFrame();
                        GameEntity gameEntity = GameEntity.CreateFromWeakEntity(dynamicPatrolArea.GameEntity);
                        Agent agent = SpawnBandit(currentBanditData, globalFrame);
                        agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanGetAlarmed);
                        AgentNavigator nav = agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
                        nav.AddBehaviorGroup<AlarmedBehaviorGroup>();
                        DailyBehaviorGroup dailyBehavior = nav.AddBehaviorGroup<DailyBehaviorGroup>();
                        dailyBehavior.AddBehavior<PatrolAgentBehavior>();
                        dailyBehavior.GetBehavior<PatrolAgentBehavior>().SetDynamicPatrolArea(gameEntity);
                    }
                }
                catch(Exception)
                {
                    HuntableHerds.SubModule.PrintDebugMessage($"error spawning the bandits in patrol area {areaIndex}");
                }
            }
        }

        private void SpawnPatrollingTroops(List<PatrolArea> patrolAreas)
        {
            IEnumerable<PatrolArea> source = from area in patrolAreas
                                             where area.StandingPoints.All((StandingPoint point) => !point.HasUser && !point.HasAIMovingTo)
                                             select area;
            foreach (PatrolArea area in source)
            {
                int areaIndex = area.AreaIndex;
                try
                {
                    if (!BanditsData.PatrolAreasBandits.ContainsKey(areaIndex)) continue;
                    RFBanditData currentBanditData = BanditsData.PatrolAreasBandits[areaIndex];

                    MatrixFrame globalFrame = area.GameEntity.GetGlobalFrame();
                    globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                    Agent agent = SpawnBandit(currentBanditData, globalFrame);
                    if (currentBanditData.ItemDropsData != null)
                        AddLootableComponent(currentBanditData.ItemDropsData, agent);
                    InitializeBanditAgent(agent, area.StandingPoints[0], false, defenderAgentObjects);
                }
                catch (Exception)
                {
                    HuntableHerds.SubModule.PrintDebugMessage($"error spawning the bandits in patrol area {areaIndex}");
                }
            }
        }
        private Agent SpawnBandit(RFBanditData currentBanditData, MatrixFrame globalFrame)
        {
            RFAgentOrigin agentToSpawn = PrepareAgentToSpawn(currentBanditData.Id);
            Agent bandit = Mission.Current.SpawnTroop(agentToSpawn, false, false, false, false, 0, 0, false, false, false, new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);
            if (currentBanditData.TreeData != null)
                bandit.AddComponent(new BehaviorTreeAgentComponent(bandit, currentBanditData.TreeData.Name, currentBanditData.TreeData.Params));
            return bandit;
        }

        private void AddLootableComponent(ItemDropsData data, Agent agent)
        {
            agent.AddComponent(new LootableAgentComponent(agent, data));
        }
        private void SpawnPlayerTroops()
        {
            TroopRoster? troopRoster;
            if ((troopRoster = NextSceneData.Instance.playerTroopRoster) == null) return;
            FlattenedTroopRoster flattenedTR = troopRoster.ToFlattenedRoster();
            foreach (TroopRosterElement troop in troopRoster.GetTroopRoster())
            {
                CharacterObject? character;
                if ((character = troop.Character) == Hero.MainHero.CharacterObject) continue;
                for (int i = 0; i < troop.Number; i++)
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
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(formation.CachedMedianPosition));
                }
                formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
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
        private RFBanditData ChooseBanditToSpawn(Dictionary<RFBanditData, int> banditsInArea)
        {
            KeyValuePair<RFBanditData, int> banditPair = banditsInArea.GetRandomElementInefficiently();
            banditsInArea[banditPair.Key] -= 1;
            if (banditsInArea[banditPair.Key] < 1) banditsInArea.Remove(banditPair.Key);
            return banditPair.Key;
        }
        private Dictionary<RFBanditData, int> GetTroopsInArea(List<RFBanditData> rFBanditData, out int allBandits)
        {
            allBandits = 0;
            Dictionary<RFBanditData, int> bandits = new();
            foreach (RFBanditData bandit in rFBanditData)
            {
                allBandits += bandit.Amount;
                bandits.Add(bandit, bandit.Amount);
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
            agent.GetComponent<CampaignAgentComponent>().AgentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
            SimulateTick(agent);
        }
        protected override void OnEndMission()
        {
            if (NextSceneData.Instance.shouldSwitchScenes == false)
                NextSceneData.Instance.currentState = NextSceneData.RFExploreState.Finished;
            OnBattleEnd?.Invoke();
            base.OnEndMission();
            RFMusicManager.Instance.StopMusicWithFadeout();
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
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
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

            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).CivilianEquipment(false).NoHorses(false).NoWeapons(false).ClothingColor1(base.Mission.PlayerTeam.Color).ClothingColor2(base.Mission.PlayerTeam.Color2).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, playerCharacter, -1, default, false)).MountKey(MountCreationKey.GetRandomMountKeyString(playerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, playerCharacter.GetMountKeySeed())).Controller(AgentControllerType.Player);

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
            if (Agent.Main == null || agent.Team == Agent.Main.Team) return;
            bool flag2 = (flag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.Alarmed;
            if (flag2 || (flag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.Cautious)
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
                if (defenderAgentObjects.TryGetValue(agent, out var obj)) 
                    obj.IsMachineAITicked = false;
            }
            else if ((flag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.None && defenderAgentObjects.TryGetValue(agent, out var value))
            {
                value.IsMachineAITicked = true;
                agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
                ((IDetachment)value.Machine).AddAgent(agent, -1, Agent.AIScriptedFrameFlags.None);
            }
            if (flag2)
                agent.SetWantsToYell();
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
                    MobileParty.MainParty.ItemRoster.AddToCounts(element, item.Amount);
                    HuntableHerds.SubModule.PrintDebugMessage("You looted " + item.Amount + " " + element.Item.Name + "!");
                }
                if (component.GoldDrop != 0)
                {
                    Hero.MainHero.ChangeHeroGold(component.GoldDrop);
                    HuntableHerds.SubModule.PrintDebugMessage("You found " + component.GoldDrop + "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                }
                if (playSound)
                    Mission.MakeSoundOnlyOnRelatedPeer(SoundEvent.GetEventIdFromString("event:/mission/combat/pickup_arrows"), agent.Position, Mission.MainAgent.Index);
                LootableAgents.Remove(agent);
            }
        }
        public override void OnFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
        {
            if (Helper.IsRFObject(focusableObject)) _focusedRFObject = focusableObject;
        }
        public override void OnFocusLost(Agent agent, IFocusable focusableObject)
        {
            _focusedRFObject = null;
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
                    Hero.MainHero.ChangeHeroGold(amount);
                    soundEventId = "event:/ui/notification/coins_positive";
                    TextObject text = new("{rf_gold_found}{ITEM_AMOUNT} gold added to the inventory");
                    text.SetTextVariable("ITEM_AMOUNT", amount);
                    InformationManager.DisplayMessage(new(text.ToString()));
                }
                else
                {
                    ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                    if (item.WeaponComponent != null)
                    {
                        var weapon = new MissionWeapon(item, null, null);
                        EquipNewItem(Agent.Main, weapon);
                    }
                    MobileParty.MainParty.ItemRoster.AddToCounts(item, amount);
                    TextObject text = new("{rf_item_found}{ITEM_AMOUNT} {ITEM_NAME} added to the inventory");
                    text.SetTextVariable("ITEM_NAME", item.Name);
                    if (amount > 1)
                        text.SetTextVariable("ITEM_AMOUNT", amount);
                    else
                        text.SetTextVariable("ITEM_AMOUNT", "");
                    InformationManager.DisplayMessage(new(text.ToString()));
                    soundEventId = "event:/mission/combat/pickup_arrows";
                }
                Mission.MakeSoundOnlyOnRelatedPeer(SoundEvent.GetEventIdFromString(soundEventId), usablePlace.GameEntity.GlobalPosition, Mission.MainAgent.Index);
                usablePlace.GameEntity.ClearComponents();
                _campaignBehavior.SetExplorationSceneObjectAsPicked(_sceneName, usablePlace.GameEntity.GlobalPosition);
                _pickableItemsRemaining -= 1;
                if (_pickableItemsRemaining < 5)
                {
                    TextObject text = new("{rf_item_count}{ITEM_COUNT} pickable items remaining");
                    text.SetTextVariable("ITEM_COUNT", _pickableItemsRemaining);
                    InformationManager.DisplayMessage(new(text.ToString()));
                }
            }
            catch
            {
                string str = "Error in game entity name " + usablePlace.GameEntity.Name;
                HuntableHerds.SubModule.PrintDebugMessage(str, 255, 0, 0);
            }
        }
        private void EquipNewItem(Agent agent, MissionWeapon weapon)
        {
            bool equippedNewItem = false;
            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
            {
                if (agent.Equipment[equipmentIndex].IsEmpty)
                {
                    agent.EquipWeaponWithNewEntity(equipmentIndex, ref weapon);
                    agent.TryToWieldWeaponInSlot(equipmentIndex, Agent.WeaponWieldActionType.WithAnimation, false);
                    equippedNewItem = true;
                    break;
                }
            }
            if (!equippedNewItem)
            {
                var eqIndex = Agent.Main.GetPrimaryWieldedItemIndex();
                agent.DropItem(eqIndex, WeaponClass.Undefined);
                agent.EquipWeaponWithNewEntity(eqIndex, ref weapon);
                agent.TryToWieldWeaponInSlot(eqIndex, Agent.WeaponWieldActionType.WithAnimation, false);
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
        private bool MissionEnded()
        {
            if (_endTimer != null && _endTimer.ElapsedTime > 6f) return true;
            else if (Agent.Main == null && _endTimer == null)
                _endTimer = new BasicMissionTimer();
            return false;
        }

        private void EndMissionByPlayerDeath()
        {
            Mission.Current.EndMission();
        }
    }
}