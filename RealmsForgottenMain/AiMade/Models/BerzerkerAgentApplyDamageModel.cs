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
        private readonly AgentApplyDamageModel _previousModel;
        private readonly CustomBerserkerBehavior _berserkerBehavior;

        public CustomBerserkerApplyDamageModel(AgentApplyDamageModel previousModel, CustomBerserkerBehavior berserkerBehavior)
        {
            _previousModel = previousModel;
            _berserkerBehavior = berserkerBehavior;
        }

        public override float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
        {
            var victim = attackInformation.VictimAgent;

            // Check if Berserker mode is active and the victim is a custom troop
            if (_berserkerBehavior.berserkerModeActive && IsCustomTroop(victim))
            {
                InformationManager.DisplayMessage(new InformationMessage("Berserker took no damage from hit!", Colors.Cyan));
                return 0f; // Nullify damage
            }

            return _previousModel.CalculateDamage(in attackInformation, in collisionData, in weapon, baseDamage);
        }

        private bool IsCustomTroop(Agent agent)
        {
            return agent?.Character?.StringId == "dwarf_berzerker"; // Replace with your troop ID
        }
    }
}