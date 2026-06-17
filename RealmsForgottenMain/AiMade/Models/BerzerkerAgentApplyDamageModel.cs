using SandBox.GameComponents;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Models
{
    public class CustomBerserkerApplyDamageModel : SandboxAgentApplyDamageModel
    {
        private readonly AgentApplyDamageModel _baseModel;
        private readonly CustomBerserkerBehavior _berserkerBehavior;

        public CustomBerserkerApplyDamageModel(AgentApplyDamageModel previousModel, CustomBerserkerBehavior berserkerBehavior)
        {
            _baseModel = previousModel;
            _berserkerBehavior = berserkerBehavior;
        }
        public override bool IsDamageIgnored(in AttackInformation attackInformation, in AttackCollisionData collisionData)
        {
            var victim = attackInformation.VictimAgent;

            // Check if Berserker mode is active and the victim is a custom troop
            if (_berserkerBehavior.berserkerModeActive && IsCustomTroop(victim))
            {
                InformationManager.DisplayMessage(new InformationMessage("Berserker took no damage from hit!", Colors.Cyan));
                return true;
            }

            return _baseModel.IsDamageIgnored(in attackInformation, in collisionData);
        }

        private bool IsCustomTroop(Agent agent)
        {
            return agent?.Character?.StringId == "dwarf_berzerker"; // Replace with your troop ID
        }
    }
}