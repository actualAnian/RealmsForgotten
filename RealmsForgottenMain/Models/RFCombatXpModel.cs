using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem.GameComponents;
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
