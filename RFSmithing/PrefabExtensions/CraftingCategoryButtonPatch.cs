using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using System.Collections.Generic;

namespace RealmsForgotten.Smithing.PrefabExtensions
{
    [PrefabExtension("Crafting", "descendant::ButtonWidget[@Id='CraftingCategoryButton']")]
    internal class CraftingCategoryButtonPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new()
        {
            new Attribute("SuggestedWidth", "320")
        };
    }
}