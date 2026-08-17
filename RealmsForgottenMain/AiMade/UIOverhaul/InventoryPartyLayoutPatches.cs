using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using System.Collections.Generic;
using PrefabAttribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace RealmsForgotten.AiMade.UIOverhaul;

[PrefabExtension("InventoryItemTuple", "descendant::Constant[@Name='NameText.Margin']")]
internal sealed class RFInventoryItemNameMarginPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("OnTrue", "135"),
        new PrefabAttribute("OnFalse", "175")
    };
}

[PrefabExtension("InventoryItemTuple", "descendant::Constant[@Name='NameText.Width']")]
internal sealed class RFInventoryItemNameWidthPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("OnTrue", "232"),
        new PrefabAttribute("OnFalse", "220")
    };
}

[PrefabExtension("InventoryItemTuple", "descendant::TextWidget[@Id='NameText']")]
internal sealed class RFInventoryItemNameClipPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("ClipContents", "true")
    };
}

[PrefabExtension("InventoryItemTuple", "descendant::Constant[@Name='Button.Transfer.Width']")]
internal sealed class RFInventoryTransferArrowWidthPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Additive", "-8")
    };
}

[PrefabExtension("InventoryItemTuple", "descendant::Constant[@Name='Button.Transfer.Height']")]
internal sealed class RFInventoryTransferArrowHeightPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Additive", "-11")
    };
}

[PrefabExtension("InventoryItemTuple", "descendant::Widget[@Sprite='Inventory\\tuple_shadow']")]
internal sealed class RFInventoryItemThumbnailShadowPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("AlphaFactor", "0.3")
    };
}

[PrefabExtension("InventoryEquippedItemSlot", "descendant::BrushWidget[@Id='Background']")]
internal sealed class RFInventoryEquippedSlotBackgroundPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Brush.ColorFactor", "1.3")
    };
}

[PrefabExtension("InventoryEquippedItemSlot", "descendant::Widget[@Sprite='Inventory\\portrait_cart']")]
internal sealed class RFInventoryEquippedSlotCanvasPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("ColorFactor", "1.3")
    };
}

[PrefabExtension("PartyTroopTupleLeft", "descendant::Constant[@Name='NameLeft']")]
internal sealed class RFPartyTroopLeftNameOffsetPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Value", "160")
    };
}

[PrefabExtension("PartyTroopTupleLeft", "descendant::Widget[@DataSource='{TierIconData}']")]
internal sealed class RFPartyTroopLeftTierPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("PositionXOffset", "-8"),
        new PrefabAttribute("PositionYOffset", "6")
    };
}

[PrefabExtension("PartyTroopTupleLeft", "descendant::TextWidget[@Text='@Name']")]
internal sealed class RFPartyTroopLeftNamePatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("SuggestedWidth", "335"),
        new PrefabAttribute("ClipContents", "true")
    };
}

[PrefabExtension("PartyTroopTuple", "descendant::Constant[@Name='NameLeft']")]
internal sealed class RFPartyTroopRightNameOffsetPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Value", "185")
    };
}

[PrefabExtension("PartyTroopTuple", "descendant::Widget[@DataSource='{TierIconData}']")]
internal sealed class RFPartyTroopRightTierPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("PositionXOffset", "-8"),
        new PrefabAttribute("PositionYOffset", "6")
    };
}

[PrefabExtension("PartyTroopTuple", "descendant::TextWidget[@Text='@Name']")]
internal sealed class RFPartyTroopRightNamePatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("SuggestedWidth", "250"),
        new PrefabAttribute("ClipContents", "true")
    };
}

[PrefabExtension("InventoryItemTuple", "descendant::ButtonWidget[@Id='SellButton']")]
internal sealed class RFInventorySellArrowInsetPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("PositionXOffset", "5")
    };
}

[PrefabExtension("InventoryItemTuple", "descendant::ButtonWidget[@Id='BuyButton']")]
internal sealed class RFInventoryBuyArrowInsetPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("PositionXOffset", "-5")
    };
}

[PrefabExtension("PartyTroopTupleLeft", "descendant::ButtonWidget[@Command.Click='ExecuteTransferSingle']")]
internal sealed class RFPartyLeftTransferArrowInsetPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("SuggestedWidth", "22"),
        new PrefabAttribute("SuggestedHeight", "30"),
        new PrefabAttribute("PositionXOffset", "-5")
    };
}

[PrefabExtension("PartyTroopTuple", "descendant::ButtonWidget[@Command.Click='ExecuteTransferSingle']")]
internal sealed class RFPartyRightTransferArrowInsetPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("SuggestedWidth", "22"),
        new PrefabAttribute("SuggestedHeight", "30"),
        new PrefabAttribute("PositionXOffset", "5")
    };
}
