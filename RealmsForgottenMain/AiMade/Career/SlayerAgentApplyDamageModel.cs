using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Career
{
    public class SlayerAgentApplyDamageModel : SandboxAgentApplyDamageModel
    {
        public override float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
        {
            float damage = base.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);

            if (attackInformation.AttackerAgent != null && attackInformation.AttackerAgent.IsMainAgent && weapon.CurrentUsageItem != null)
            {
                var weaponClass = weapon.CurrentUsageItem.WeaponClass;
                if (weaponClass == WeaponClass.OneHandedSword ||
                    weaponClass == WeaponClass.TwoHandedSword ||
                    weaponClass == WeaponClass.OneHandedAxe ||
                    weaponClass == WeaponClass.TwoHandedAxe ||
                    weaponClass == WeaponClass.Mace ||
                    weaponClass == WeaponClass.TwoHandedMace)
                {
                    // Apply 10% damage bonus
                    damage *= 1.10f;
                }
            }

            return damage;
        }
        private bool IsSlayer(Agent agent)
        {
            // Verificar se o agente é o herói do jogador e possui o benefício Slayer
            if (agent.IsHero && agent.Character is CharacterObject character && character.HeroObject != null)
            {
                Hero hero = character.HeroObject;
                return Campaign.Current?.GetCampaignBehavior<CareerProgressionBehavior>()?.IsAgentSlayer(hero) ?? false;
            }
            return false;
        }
    }
}