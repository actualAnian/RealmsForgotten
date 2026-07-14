using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

namespace RealmsForgotten.Patches
{
    public static class CharacterDeveloperAttributeOrderPatch
    {
        public static void ReorderCustomAttributesPostfix(CharacterDeveloperHeroItemVM __instance)
        {
            if (__instance?.Attributes == null)
            {
                return;
            }

            List<CharacterAttributeItemVM> attributes = __instance.Attributes.ToList();
            CharacterAttributeItemVM naval = attributes.FirstOrDefault(attribute => attribute?.AttributeType?.StringId == "naval");
            CharacterAttributeItemVM discipline = attributes.FirstOrDefault(attribute => attribute?.AttributeType?.StringId == "discipline");
            if (naval == null || discipline == null)
            {
                return;
            }

            int navalIndex = attributes.IndexOf(naval);
            int disciplineIndex = attributes.IndexOf(discipline);
            if (navalIndex >= 0 && disciplineIndex >= 0 && disciplineIndex < navalIndex)
            {
                return;
            }

            attributes.Remove(naval);
            disciplineIndex = attributes.IndexOf(discipline);
            attributes.Insert(disciplineIndex + 1, naval);

            __instance.Attributes.Clear();
            foreach (CharacterAttributeItemVM attribute in attributes)
            {
                __instance.Attributes.Add(attribute);
            }
        }
    }
}
