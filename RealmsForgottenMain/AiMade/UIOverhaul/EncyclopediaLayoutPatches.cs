using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using System.Collections.Generic;
using PrefabAttribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace RealmsForgotten.AiMade.UIOverhaul;

internal static class RFEncyclopediaSafeArea
{
    internal const string ContentRootSelector =
        "//Window/BrushWidget/Children/Widget[@SuggestedHeight='!Encyclopedia.Height' and @SuggestedWidth='!Encyclopedia.Width']";

    internal static List<PrefabAttribute> CreateContentAttributes() => new()
    {
        new PrefabAttribute("SuggestedWidth", "1360"),
        new PrefabAttribute("SuggestedHeight", "652"),
        new PrefabAttribute("MarginTop", "132")
    };
}

[PrefabExtension("EncyclopediaHome", "//NavigatableListPanel[@Id='EncyclopediaHomeList']")]
internal sealed class RFEncyclopediaHomeSafeAreaPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("MarginTop", "220"),
        new Attribute("MarginBottom", "88"),
        new Attribute("MarginLeft", "70"),
        new Attribute("MarginRight", "70")
    };
}

[PrefabExtension("EncyclopediaItemList", RFEncyclopediaSafeArea.ContentRootSelector)]
internal sealed class RFEncyclopediaItemListSafeAreaPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => RFEncyclopediaSafeArea.CreateContentAttributes();
}

[PrefabExtension("EncyclopediaClanPage", RFEncyclopediaSafeArea.ContentRootSelector)]
internal sealed class RFEncyclopediaClanPageSafeAreaPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => RFEncyclopediaSafeArea.CreateContentAttributes();
}

[PrefabExtension("EncyclopediaConceptPage", RFEncyclopediaSafeArea.ContentRootSelector)]
internal sealed class RFEncyclopediaConceptPageSafeAreaPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => RFEncyclopediaSafeArea.CreateContentAttributes();
}

[PrefabExtension("EncyclopediaFactionPage", RFEncyclopediaSafeArea.ContentRootSelector)]
internal sealed class RFEncyclopediaFactionPageSafeAreaPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => RFEncyclopediaSafeArea.CreateContentAttributes();
}

[PrefabExtension("EncyclopediaHeroPage", RFEncyclopediaSafeArea.ContentRootSelector)]
internal sealed class RFEncyclopediaHeroPageSafeAreaPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => RFEncyclopediaSafeArea.CreateContentAttributes();
}

[PrefabExtension("EncyclopediaSettlementPage", RFEncyclopediaSafeArea.ContentRootSelector)]
internal sealed class RFEncyclopediaSettlementPageSafeAreaPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => RFEncyclopediaSafeArea.CreateContentAttributes();
}

[PrefabExtension("EncyclopediaShipPage", RFEncyclopediaSafeArea.ContentRootSelector)]
internal sealed class RFEncyclopediaShipPageSafeAreaPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => RFEncyclopediaSafeArea.CreateContentAttributes();
}

[PrefabExtension("EncyclopediaUnitPage", RFEncyclopediaSafeArea.ContentRootSelector)]
internal sealed class RFEncyclopediaUnitPageSafeAreaPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => RFEncyclopediaSafeArea.CreateContentAttributes();
}
