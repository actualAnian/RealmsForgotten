using BehaviorTreeWrapper;
using RealmsForgotten.Utility.Magic;
using SandBox;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.RFMissionLogic
{
    public record SpawnAgentData(string characterId, Team agentTeam, Vec3 agentPosition, bool useTeleport, Vec2 spawnDirection = default, string? behaviorTreeStringId = null, object[]? behaviorTreeParams = null);
    public class SpawnAgentMissionLogic : MissionLogic
    {
        public static void AddAgentToSpawn(SpawnAgentData data)
        {
            var logic = Mission.Current.MissionLogics.FirstOrDefault(l => l is SpawnAgentMissionLogic);
            if (logic == null) InformationManager.DisplayMessage(new("Can not spawn agent, SpawnAgentMissionLogic not present in the mission"));
            _toSpawn.Add(data);
        }
        private static readonly List<SpawnAgentData> _toSpawn = new();
        public override void OnMissionTick(float dt)
        {
            //if (_toSpawn.Count == 0) return;
            for (int i = _troopsSpawnedLastTick.Count - 1; i >= 0; i--)
            {
                var data = _troopsSpawnedLastTick[i];
                Teleport.TeleportToPosition(data.Item1, data.Item2);
                _troopsSpawnedLastTick.RemoveAt(i);
            }

            for (int i = _toSpawn.Count - 1; i >= 0; i--)
            {
                var data = _toSpawn[i];
                SpawnAgent(data.characterId, data.agentTeam, data.agentPosition, data.useTeleport, data.spawnDirection, data.behaviorTreeStringId, data.behaviorTreeParams);
                _toSpawn.RemoveAt(i);
            }
        }
        List<Tuple<Agent, Vec3>> _troopsSpawnedLastTick = new();
        private Agent SpawnAgent(string characterId, Team agentTeam, Vec3 agentPosition, bool useTeleport = false, Vec2 spawnDirection = default, string? behaviorTreeStringId = null, object[]? behaviorTreeParams = null)
        {
            var character = MBObjectManager.Instance.GetObject<CharacterObject>(characterId);
            //var npcBuildData = new AgentBuildData(character)
            //    .Team(agentTeam)
            //    .InitialPosition(agentPosition)
            //    .InitialDirection(spawnDirection);
            //var npcAgent = Mission.Current.SpawnAgent(npcBuildData);

            //Agent bandit = Mission.Current.SpawnTroop(agentToSpawn,
            //    false, false, false, false, 0, 0,
            //    false, false, false, 
            //    new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), "_hideout_bandit", null, FormationClass.NumberOfAllFormations, false);

            Agent spawned = Mission.Current.SpawnTroop(
                troopOrigin: new SimpleAgentOrigin(character),
                false,
                hasFormation: false,
                spawnWithHorse: false,
                isReinforcement: true,
                formationTroopCount: 1,
                formationTroopIndex: 0,
                isAlarmed: true,
                wieldInitialWeapons: true,
                forceDismounted: false,
                initialPosition: agentPosition,
                initialDirection: spawnDirection,
                specialActionSetSuffix: null,
                bannerItem: null,
                formationIndex: FormationClass.Infantry,
                useTroopClassForSpawn: false
            );
            spawned.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
            _troopsSpawnedLastTick.Add(new(spawned, agentPosition));

            //Teleport.TeleportToPosition(npcAgent, agentPosition);
            //if (behaviorTreeStringId != null)
            //{
            //    behaviorTreeParams ??= Array.Empty<object>();
            //    npcAgent.AddComponent(new BehaviorTreeAgentComponent(npcAgent, behaviorTreeStringId, behaviorTreeParams));
            //}
            //return npcAgent;
            return spawned;
        }
    }
}
