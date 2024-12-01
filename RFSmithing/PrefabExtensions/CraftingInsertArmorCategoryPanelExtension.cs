using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace RealmsForgotten.Smithing.PrefabExtensions
{
    [PrefabExtension("Crafting", "descendant::Widget[@Id='RightPanel']/Children")]
    internal class CraftingInsertArmorCategoryPanelExtension : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string Id => "CraftingInsertArmorCategoryPanelExtension";
        public override InsertType Type => InsertType.Child;
        public override int Index => 3;
    }
}