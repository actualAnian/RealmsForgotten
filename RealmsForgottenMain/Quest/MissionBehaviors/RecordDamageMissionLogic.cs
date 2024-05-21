using System;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.MissionBehaviors;

public class RecordDamageMissionLogic : MissionLogic
{
            
    public RecordDamageMissionLogic(Action<Agent, Agent, int> agentHitAction)
    {
        OnAgentHitAction = agentHitAction;
    }
    public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
    {
        if (OnAgentHitAction == null)
        {
            return;
        }
        OnAgentHitAction(affectedAgent, affectorAgent, blow.InflictedDamage);
    }

    private Action<Agent, Agent, int> OnAgentHitAction;
}