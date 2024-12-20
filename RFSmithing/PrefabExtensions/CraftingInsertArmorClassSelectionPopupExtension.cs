﻿using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace RealmsForgotten.Smithing.PrefabExtensions
{
    [PrefabExtension("Crafting", "descendant::CraftingTemplateSelectionPopup[@Id='WeaponClassSelectionPopup']")]
    internal class CraftingInsertArmorClassSelectionPopupExtension : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string Id => "CraftingInsertArmorClassSelectionPopupExtension";
        public override InsertType Type => InsertType.Append;
    }
}