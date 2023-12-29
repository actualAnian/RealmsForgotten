using Helpers;
using RealmsForgotten.CustomSkills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten
{
    public static class Extensions
    {
        private static Random _random = new();
        public static float GetFaithPerkBonus(this CharacterObject hero, PerkObject perk)
        {
            ExplainedNumber explainedNumber = new ExplainedNumber(perk.PrimaryBonus);
            SkillHelper.AddSkillBonusForCharacter(RFSkills.Faith, RFSkillEffects.FaithPerkMultiplier, hero, ref explainedNumber);
            return explainedNumber.ResultNumber;
        }
        public static T RandomElementByWeight<T>(this Dictionary<T, int> dictionary, Func<KeyValuePair<T, int>, int> weightSelector)
        {
            int totalWeight = dictionary.Sum(weightSelector);

            int randomWeight = _random.Next(totalWeight) + 1;

            foreach (var kvp in dictionary)
            {
                if (randomWeight <= kvp.Value)
                    return kvp.Key;

                randomWeight -= kvp.Value;
            }

            throw new InvalidOperationException("Dictionary is empty or has negative weights.");
        }

        public static IEnumerable<ItemObject> GetUsingWeapons(this BasicCharacterObject basicCharacterObject)
        {
            for (EquipmentIndex equipmentIndex = EquipmentIndex.Weapon0;
                 equipmentIndex <= EquipmentIndex.Weapon3;
                 equipmentIndex++)
            {
                yield return basicCharacterObject.Equipment[equipmentIndex].Item;
            }
        }


        public static void SetWeaponAmount(this MissionEquipment equipment, EquipmentIndex index, short count)
        {
            MissionWeapon weapon = equipment[index];
            weapon.Amount = count;
            equipment[index] = weapon;
        }
    }
}
