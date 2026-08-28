using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using System.Collections.Generic;
using PrefabAttribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace RealmsForgotten.AiMade.UIOverhaul;

[PrefabExtension("SaveLoadScreen", "//Widget[@Id='SaveLoadScreen']/Children/Widget[@Sprite='SaveLoadBackground']")]
internal sealed class RFSaveLoadBackgroundPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Sprite", "InventoryBackground"),
        new PrefabAttribute("PositionXOffset", "197")
    };
}

[PrefabExtension("SaveLoadScreen", "//Widget[@Id='InnerPanel']")]
internal sealed class RFSaveLoadInnerPanelPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("SuggestedWidth", "540"),
        new PrefabAttribute("PositionXOffset", "58")
    };
}

[PrefabExtension("SaveLoadScreen", "//ListPanel[@Id='ActionButtonContainer']")]
internal sealed class RFSaveLoadActionsPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("MarginRight", "0")
    };
}

[PrefabExtension("SavedGameGroup", "//NavigatableListPanel[@SuggestedWidth='!Save.Tuple.Width']")]
internal sealed class RFSavedGameGroupWidthPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("SuggestedWidth", "540")
    };
}

[PrefabExtension("SavedGameGroup", "//PartyHeaderToggleWidget[@Id='HeaderToggle']")]
internal sealed class RFSavedGameGroupHeaderPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("SuggestedWidth", "540")
    };
}

[PrefabExtension("SavedGameTuple", "//ButtonWidget[@SuggestedWidth='!Save.Tuple.Width']")]
internal sealed class RFSavedGameTupleWidthPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("SuggestedWidth", "540")
    };
}
