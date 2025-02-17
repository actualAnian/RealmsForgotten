using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Logic
{
    public class CareerPerkMissionBehavior : MissionLogic
    {
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            var choices = PlayerCareerExtension.GetAllCareerChoices();
            if (affectorAgent.IsMainAgent)
            {
                
                if (choices.Contains("SurvivalistKeystone"))
                {
                    int amountToIncrease = 10;
                    float health = affectorAgent.Health;
                    if (health + amountToIncrease > affectorAgent.HealthLimit) affectorAgent.Health = affectorAgent.HealthLimit;
                    else affectorAgent.Health += 10;
                }
            }
        }
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {

        }
    }
}
