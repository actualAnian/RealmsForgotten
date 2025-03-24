using Helpers;
using RealmsForgotten.Career.Logic;
using RealmsForgotten.Career;
using SandBox.GameComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace RealmsForgotten.Models
{
    public class RFStrikeMagnitudeModel : SandboxStrikeMagnitudeModel
    {
        public RFStrikeMagnitudeModel()
        {
        }
        public override float CalculateAdjustedArmorForBlow(float baseArmor, BasicCharacterObject attackerCharacter, BasicCharacterObject attackerCaptainCharacter, BasicCharacterObject victimCharacter, BasicCharacterObject victimCaptainCharacter, WeaponComponentData weaponComponent)
        {
            var result = base.CalculateAdjustedArmorForBlow(baseArmor, attackerCharacter, attackerCaptainCharacter, victimCharacter, victimCaptainCharacter, weaponComponent);
            ExplainedNumber resultArmor = new ExplainedNumber(result);
            var attackerCaptain = attackerCharacter as CharacterObject;
            if (weaponComponent != null && attackerCharacter is CharacterObject attacker)
            {

                if (attacker.IsPlayerCharacter && attacker.HeroObject == Hero.MainHero)
                {
                    //AttackTypeMask attackMask = AttackTypeMask.Melee;
                    //if (weaponComponent.IsRangedWeapon) attackMask = AttackTypeMask.Ranged;
                    CareerHelper.ApplyBasicCareerPassives(ref resultArmor, PassiveEffectType.ArmorPenetration, true);
                }
            }
            return resultArmor.ResultNumber;
        }
    }
}