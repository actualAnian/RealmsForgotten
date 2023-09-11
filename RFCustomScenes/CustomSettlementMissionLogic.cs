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

namespace RFCustomSettlements
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

            // Token: 0x0400055A RID: 1370
            public readonly UsableMachineAIBase MachineAI;


            public bool IsMachineAITicked;
        }

        private bool isRandomScene;
        private static int _disabledFaceId;
        private static int _disabledFaceIdForAnimals;
        private bool isMissionInitialized;
        private List<PatrolArea> patrolAreas;
        private List<CommonAreaMarker> areaMarkers;
        private IMissionTroopSupplier troopSupplier;
        private readonly Dictionary<Agent, CustomSettlementMissionLogic.UsedObject> defenderAgentObjects;

        public CustomSettlementMissionLogic(bool isRandomScene)
        {
            this.isRandomScene = isRandomScene;
            defenderAgentObjects = new Dictionary<Agent, CustomSettlementMissionLogic.UsedObject>();
            patrolAreas = new();
            areaMarkers = new();
            Temp();
        }
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (!isMissionInitialized)
            {
                InitializeMission();
                isMissionInitialized = true;
                return;
            }
        }

        private void InitializeMission()
        {
            GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("No Prefab");
            var aga = base.Mission.MissionObjects;
            var aa = aga.Where(m => (m.GameEntity.Name == "arrow_new_icon"));

            try {
            areaMarkers.AddRange(from area in base.Mission.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>()
                                       orderby area.AreaIndex
                                       select area);
            }
            catch (Exception ex) { areaMarkers = new(); }
            patrolAreas.AddRange(from area in base.Mission.ActiveMissionObjects.FindAllWithType<PatrolArea>()
                                 orderby area.AreaIndex
                                 select area);
            //Temp();
            SpawnPatrollingTroops(patrolAreas, defenderAgentObjects);
            SpawnStandingTroops(areaMarkers, defenderAgentObjects);
        }

        private FlattenedTroopRoster Temp()
        {
            var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");

            TroopRoster temp = new TroopRoster(Hero.MainHero.PartyBelongedTo.Party);
            temp.AddToCounts(looter, 1);
            
            FlattenedTroopRoster troopRoster = new();
            troopRoster.Add(temp.GetElementCopyAtIndex(0));
            temp = TroopRoster.CreateDummyTroopRoster();
            return troopRoster;
            //troopSupplier = new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, troopRoster, null);
        }

        private void SpawnStandingTroops(List<CommonAreaMarker> areaMarkers, Dictionary<Agent, CustomSettlementMissionLogic.UsedObject> defenderAgentObjects)
        {
            foreach (CommonAreaMarker commonAreaMarker in areaMarkers)
            {
                List<StandingPoint> usableMachinesInArea = new();
                StandingPoint standingPoint = new();
                MatrixFrame globalFrame;

                CharacterObject looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");

                MobileParty mparty = new MobileParty();
                mparty.AddElementToMemberRoster(looter, 1);
                FlattenedTroopRoster flattenedTroopRosterElements = mparty.MemberRoster.ToFlattenedRoster();
                UniqueTroopDescriptor descriptor = flattenedTroopRosterElements.ElementAt(0).Descriptor;
                PartyBase party = new PartyBase(mparty);

                RFAgentOrigin rFAgentOrigin = new RFAgentOrigin(party, descriptor, 1, flattenedTroopRosterElements[descriptor]);

                foreach (UsableMachine usableMachine in commonAreaMarker.GetUsableMachinesInRange(null))
                {
                    usableMachinesInArea.AddRange(usableMachine.StandingPoints);
                }
                usableMachinesInArea.Shuffle();
                Queue<StandingPoint> usableMachinesQueue = new Queue<StandingPoint>((IEnumerable<StandingPoint>)usableMachinesInArea);

                for(int i = 0; i < 2; i++)
                {
                    try 
                    {
                        standingPoint = usableMachinesQueue.Dequeue();
                        globalFrame = standingPoint.GameEntity.GetGlobalFrame();
                        globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                        Agent agent = Mission.Current.SpawnTroop(rFAgentOrigin, false, false, false, false, 0, 0, false, false, false, new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);

                        InitializeBanditAgent(agent, standingPoint, false, defenderAgentObjects);
                    }
                    catch(InvalidOperationException)
                    {
                        break;
                    }
                }

            }
        }

        private void SpawnPatrollingTroops(List<PatrolArea> patrolAreas, Dictionary<Agent, CustomSettlementMissionLogic.UsedObject> defenderAgentObjects)
        {
            StandingPoint standingPoint = null;
            IEnumerable<PatrolArea> source = from area in patrolAreas
                                             where area.StandingPoints.All((StandingPoint point) => !point.HasUser && !point.HasAIMovingTo)
                                             select area;
            if (!source.IsEmpty<PatrolArea>())
            {
                standingPoint = source.First<PatrolArea>().StandingPoints[0];
            }
//            List<IAgentOriginBase> list2 = troopSupplier.SupplyTroops(1).ToList<IAgentOriginBase>();
            MatrixFrame globalFrame = standingPoint.GameEntity.GetGlobalFrame();
            globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();

            CharacterObject looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");

            MobileParty mparty = new MobileParty();
            mparty.AddElementToMemberRoster(looter, 1);
            FlattenedTroopRoster flattenedTroopRosterElements = mparty.MemberRoster.ToFlattenedRoster();
            UniqueTroopDescriptor descriptor = flattenedTroopRosterElements.ElementAt(0).Descriptor;
            PartyBase party = new PartyBase(mparty);

            RFAgentOrigin rFAgentOrigin = new RFAgentOrigin(party, descriptor, 1, flattenedTroopRosterElements[descriptor]);
            Agent agent = Mission.Current.SpawnTroop(rFAgentOrigin, false, false, false, false, 0, 0, false, false, false, new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);
            InitializeBanditAgent(agent, standingPoint, false, defenderAgentObjects);
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

            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).CivilianEquipment(false).NoHorses(false).NoWeapons(false).ClothingColor1(base.Mission.PlayerTeam.Color).ClothingColor2(base.Mission.PlayerTeam.Color2).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, playerCharacter, -1, default(UniqueTroopDescriptor), false)).MountKey(MountCreationKey.GetRandomMountKeyString(playerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, playerCharacter.GetMountKeySeed())).Controller(Agent.ControllerType.Player);


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