using RealmsForgotten.CustomSkills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;

namespace RealmsForgotten.Models
{
    internal class RFCombatXpModel : DefaultCombatXpModel
    {
        public override SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeEngineHit)
        {
            SkillObject baseValue = base.GetSkillForWeapon(weapon, isSiegeEngineHit);
            if (weapon.WeaponClass == WeaponClass.Musket || weapon.WeaponClass == WeaponClass.Cartridge || weapon.WeaponClass == WeaponClass.Pistol)
                baseValue = RFSkills.Arcane;
            return baseValue;
        }
    }
}
