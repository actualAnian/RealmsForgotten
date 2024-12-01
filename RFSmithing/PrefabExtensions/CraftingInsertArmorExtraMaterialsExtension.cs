using System.Collections.Generic;
using System.Xml;
using RealmsForgotten.Smithing.ViewModels;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Library;

namespace RealmsForgotten.Smithing.PrefabExtensions;

[PrefabExtension("Crafting", "descendant::CraftingScreenWidget/Children")]
internal class CraftingInsertArmorExtraMaterialsExtension : PrefabExtensionInsertPatch
{
    /*
     * Should be InsertType.Append but the item we're after doesn't have an Id
     */
    [PrefabExtensionFileName] public string Id => "CraftingInsertArmorExtraMaterialsExtension";
    public override InsertType Type => InsertType.Child;
    public override int Index => 0;
}
    
[PrefabExtension("CraftingCategory", "descendant::Widget[@Id='CraftingCategoryParent']/Children/Widget[@Id='InnerPanel']/Children/ListPanel[@Id='PieceListParent']/Children/ListPanel/Children/Widget/Children/ScrollablePanel/Children/Widget")]
internal class KardrathiumToggleButton : PrefabExtensionInsertPatch
{
        
    [PrefabExtensionFileName] public string Id => "KardrathiumToggleButton";
        
    public override InsertType Type => InsertType.Append;
}
