using RealmsForgotten.Behaviors;
using SandBox.GameComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Models
{
    public class CustomBerserkerApplyDamageModel : SandboxAgentApplyDamageModel
    {
        private AgentApplyDamageModel _previousModel;

        public CustomBerserkerApplyDamageModel(AgentApplyDamageModel previousModel)
        {
            _previousModel = previousModel;
        }

       
        public override float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
        {
           
            if (CustomBerserkerBehavior.berserkerModeActive && IsCustomTroop(attackInformation.VictimAgent))
            {
                InformationManager.DisplayMessage(new InformationMessage("Beserker took no damage from hit!", Colors.Cyan));
                return 0f; 
                
            }

           
            return _previousModel.CalculateDamage(in attackInformation, in collisionData, in weapon, baseDamage);
        }

        private bool IsCustomTroop(Agent agent)
        {
            
            return agent.Character?.StringId == "dwarf_berzerker"; 
        }
    }
}