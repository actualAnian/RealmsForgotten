using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Logic
{
    public static class CareerLogic
    {
        public static void ApplyExtraAmmo()
        {
            MissionEquipment equipment = Agent.Main.Equipment;
            for (int i = 0; i < 5; i++)
            {
                EquipmentIndex equipmentIndex = (EquipmentIndex)i;
                MissionWeapon missionWeapon = equipment[equipmentIndex];
                if (missionWeapon.IsEmpty) continue;
                WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
                if (currentUsageItem != null && currentUsageItem.IsAmmo && currentUsageItem.RelevantSkill != null)
                {
                    ExplainedNumber ammoCount = new ExplainedNumber(missionWeapon.Amount);
                    switch (missionWeapon.Item.Type)
                    {
                        case ItemObject.ItemTypeEnum.Arrows or ItemObject.ItemTypeEnum.Bolts:
                            CareerHelper.ApplyBasicCareerPassives(ref ammoCount, PassiveEffectType.Ammo, false);
                            break;
                        case ItemObject.ItemTypeEnum.Bullets:
                            CareerHelper.ApplyBasicCareerPassives(ref ammoCount, PassiveEffectType.SpellAmmo, false);
                            break;
                        default:
                            break;
                    }
                    short result = (short)MathF.Round(ammoCount.ResultNumber);
                    equipment.SetAmountOfSlot(equipmentIndex, result, true);
                }
            }
        }
        public static void ApplyExtraShieldDamage(MissionWeapon weapon)
        {
            List<string> choices = PlayerCareerExtension.GetAllCareerChoices();
            if (choices.Contains("NightRiderPassive4"))
            {
                WeaponComponentData weaponComponentData = weapon.CurrentUsageItem;
                weaponComponentData.WeaponFlags |= WeaponFlags.BonusAgainstShield;
            }
        }
    }
}
