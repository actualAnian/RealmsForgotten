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

namespace RealmsForgotten.AiMade.Models
{
    public class CustomBerserkerApplyDamageModel : SandboxAgentApplyDamageModel
    {
        private AgentApplyDamageModel _previousModel;

        public CustomBerserkerApplyDamageModel(AgentApplyDamageModel previousModel)
        {
            _previousModel = previousModel;
        }

        // Correct method signature for CalculateDamage
        public override float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
        {
            // Custom logic for Berserker Mode
            if (CustomBerserkerBehavior.berserkerModeActive && IsCustomTroop(attackInformation.VictimAgent))
            {
                return 0f; // Immunity to damage
            }

            // Fall back to the default damage calculation
            return _previousModel.CalculateDamage(in attackInformation, in collisionData, in weapon, baseDamage);
        }

        private bool IsCustomTroop(Agent agent)
        {
            // Logic to determine if the agent is the custom troop
            return agent.Character?.StringId == "dwarf_berzerker"; // Example troop ID
        }
    }
}