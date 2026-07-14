using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace RealmsForgotten.Models
{
    internal class RFCombatXpModel : DefaultCombatXpModel
    {
        private CombatXpModel _previousModel;
        
        public RFCombatXpModel(CombatXpModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeEngineHit)
        {
            SkillObject baseValue = _previousModel.GetSkillForWeapon(weapon, isSiegeEngineHit);
            // weapon is null for unarmed/siege hits (vanilla handles it) — guard
            // before dereferencing WeaponClass.
            if (weapon != null && (weapon.WeaponClass == WeaponClass.Musket || weapon.WeaponClass == WeaponClass.Cartridge || weapon.WeaponClass == WeaponClass.Pistol))
                baseValue = RFSkills.Arcane;
            return baseValue;
        }
    }
}
