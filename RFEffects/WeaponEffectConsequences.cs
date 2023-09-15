using HarmonyLib;
using RealmsForgotten.RFEffects;
using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Mission;

namespace RFEffects
{
    public static class WeaponEffectConsequences
    {
        public static void Fire(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            RFMissionBehaviour missionBehavior = Mission.Current.GetMissionBehavior<RFMissionBehaviour>();
            missionBehavior.toBeAdded.Add(affectedAgent);
            if (!missionBehavior.agentsUnderFire.ContainsKey(affectedAgent.Index))
            {
                missionBehavior.agentsUnderFire.Add(affectedAgent.Index, affectedAgent.Index);
                return;
            }
            missionBehavior.agentsUnderFire[affectedAgent.Index] = affectedAgent.Index;
        }

        public static void Terror(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            affectedAgent.ChangeMorale(-25);
        }
        public static void Ice(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            affectedAgent.SetMaximumSpeedLimit(5, false);
        }
        public static void Heal(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            float searchAreaRadius = 5f;
            List<Agent> list = new List<Agent>();
            AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, blow.GlobalPosition.AsVec2, searchAreaRadius, extendRangeByBiggestAgentCollisionPadding: true);
            while (searchStruct.LastFoundAgent != null)
            {
                Agent lastFoundAgent = searchStruct.LastFoundAgent;
                if (lastFoundAgent.CurrentMortalityState != Agent.MortalityState.Invulnerable && lastFoundAgent != affectorAgent && lastFoundAgent != affectedAgent && !lastFoundAgent.IsEnemyOf(affectorAgent))
                {
                    list.Add(lastFoundAgent);
                }

                AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
            }

            foreach (Agent agent in list)
            {
                agent.Health += 20f;
            }
        }
    }
}
