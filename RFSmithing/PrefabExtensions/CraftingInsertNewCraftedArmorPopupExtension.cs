using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace RealmsForgotten.Smithing.PrefabExtensions
{
    [PrefabExtension("Crafting", "descendant::NewCraftedWeaponPopup[@Id='NewCraftedWeaponPopupWidget']")]
    internal class CraftingInsertNewCraftedArmorPopupExtension : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string Id => "CraftingInsertNewCraftedArmorPopupExtension";
        public override InsertType Type => InsertType.Append;
    }
}