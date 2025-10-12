using RealmsForgotten.Career;
using RealmsForgotten.Career.Logic;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Models
{
    public class RFStrikeMagnitudeModel : SandboxStrikeMagnitudeModel
    {
        public RFStrikeMagnitudeModel()
        {
        }
        public override float CalculateAdjustedArmorForBlow(in AttackInformation attackInformation, in AttackCollisionData collisionData, 
            float baseArmor, BasicCharacterObject attackerCharacter, BasicCharacterObject attackerCaptainCharacter, 
            BasicCharacterObject victimCharacter, BasicCharacterObject victimCaptainCharacter, WeaponComponentData weaponComponent)
        {
            var result = base.CalculateAdjustedArmorForBlow(attackInformation, collisionData, baseArmor, attackerCharacter, attackerCaptainCharacter, 
                victimCharacter, victimCaptainCharacter, weaponComponent);
            ExplainedNumber resultArmor = new ExplainedNumber(result);
            if (weaponComponent != null && attackerCharacter is CharacterObject attacker)
            {

                if (attacker.IsPlayerCharacter && attacker.HeroObject == Hero.MainHero)
                {
                    //AttackTypeMask attackMask = AttackTypeMask.Melee;
                    //if (weaponComponent.IsRangedWeapon) attackMask = AttackTypeMask.Ranged;
                    CareerHelper.ApplyBasicCareerPassives(ref resultArmor, PassiveEffectType.ArmorPenetration);
                }
            }
            return resultArmor.ResultNumber;
        }
    }
}