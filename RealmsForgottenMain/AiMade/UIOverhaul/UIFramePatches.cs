using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using System.Collections.Generic;
using PrefabAttribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace RealmsForgotten.AiMade.UIOverhaul;

[PrefabExtension("EscapeMenu", "//Widget[@Id='EscapeMenu' and @Sprite='SPGeneral\\EscapeMenu\\escape_panel']")]
internal sealed class RFEscapeMenuPanelPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Sprite", "SPGeneral\\EscapeMenu\\rf_escape_panel")
    };
}

[PrefabExtension("EncyclopediaBar", "//BrushWidget[@Brush='Encyclopedia.Frame']")]
internal sealed class RFEncyclopediaFramePatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Brush", "RF.Encyclopedia.Frame")
    };
}

[PrefabExtension("EncyclopediaBar", "//Widget[@Sprite='StdAssets\\tabbar_popup' and @SuggestedWidth='576' and @SuggestedHeight='150' and @PositionYOffset='-17']")]
internal sealed class RFEncyclopediaCenterHeaderPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Sprite", "StdAssets\\rf_header_encyclopedia")
    };
}

[PrefabExtension("PartyScreen", "//Widget[@Sprite='StdAssets\\tabbar_standart']")]
internal sealed class RFPartyCenterHeaderPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Sprite", "StdAssets\\rf_header_party")
    };
}

[PrefabExtension("Inventory", "//Widget[@Id='CharacterSelection' and @Sprite='StdAssets\\tabbar_long']")]
internal sealed class RFInventoryCenterHeaderPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Sprite", "StdAssets\\rf_header_inventory")
    };
}

[PrefabExtension("PartyScreen", "//TextWidget[@Text='@TitleLbl' and @Brush='Party.Text.Title']")]
internal sealed class RFPartyTitleCenterPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("VerticalAlignment", "Top"),
        new PrefabAttribute("MarginTop", "34")
    };
}

[PrefabExtension("Inventory", "//AnimatedDropdownWidget[@Id='DropdownParent']")]
internal sealed class RFInventoryNameBoxCenterPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("VerticalAlignment", "Top"),
        new PrefabAttribute("MarginTop", "19")
    };
}
