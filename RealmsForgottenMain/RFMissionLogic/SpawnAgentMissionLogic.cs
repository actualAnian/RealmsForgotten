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
    public record SpawnAgentData(string CharacterId, bool PlayerSide, Vec3 AgentPosition, bool IsAlarmed, bool WithHorse = false, bool UseTeleport = false, Vec2 SpawnDirection = default, string? BehaviorTreeStringId = null, object[]? BehaviorTreeParams = null);
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
                if (data.Item3)
                    Teleport.TeleportToPosition(data.Item1, data.Item2);
                else
                    data.Item1.TeleportToPosition(data.Item2);
                _troopsSpawnedLastTick.RemoveAt(i);
            }

            for (int i = _toSpawn.Count - 1; i >= 0; i--)
            {
                var data = _toSpawn[i];
                SpawnAgent(data.CharacterId, data.PlayerSide, data.AgentPosition, data.IsAlarmed, data.WithHorse, data.UseTeleport, data.SpawnDirection, data.BehaviorTreeStringId, data.BehaviorTreeParams);
                _toSpawn.RemoveAt(i);
            }
        }
        List<Tuple<Agent, Vec3, bool>> _troopsSpawnedLastTick = new();
        private Agent SpawnAgent(string characterId, bool playerSide, Vec3 agentPosition, bool isAlarmed, bool withHorse = false, bool useTeleport = false, Vec2 spawnDirection = default, string? behaviorTreeStringId = null, object[]? behaviorTreeParams = null)
        {
            var character = MBObjectManager.Instance.GetObject<CharacterObject>(characterId);
            Agent npcAgent = Mission.Current.SpawnTroop(
                troopOrigin: new SimpleAgentOrigin(character),
                isPlayerSide: playerSide,
                hasFormation: false,
                spawnWithHorse: withHorse,
                isReinforcement: true,
                formationTroopCount: 1,
                formationTroopIndex: 0,
                isAlarmed: isAlarmed,
                wieldInitialWeapons: true,
                forceDismounted: false,
                initialPosition: agentPosition,
                initialDirection: spawnDirection,
                specialActionSetSuffix: null,
                bannerItem: null,
                formationIndex: FormationClass.Infantry,
                useTroopClassForSpawn: false
            );
            npcAgent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
            _troopsSpawnedLastTick.Add(new(npcAgent, agentPosition, useTeleport));

            if (behaviorTreeStringId != null)
            {
                behaviorTreeParams ??= Array.Empty<object>();
                npcAgent.AddComponent(new BehaviorTreeAgentComponent(npcAgent, behaviorTreeStringId, behaviorTreeParams));
            }
            return npcAgent;
        }
    }
}
