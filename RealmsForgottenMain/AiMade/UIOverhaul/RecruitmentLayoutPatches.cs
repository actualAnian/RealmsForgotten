using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using System.Collections.Generic;
using PrefabAttribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace RealmsForgotten.AiMade.UIOverhaul;

[PrefabExtension("RecruitmentPopup", "//BrushWidget[@Brush='Recruitment.Frame']")]
internal sealed class RFRecruitmentFramePatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Brush", "RF.Recruitment.Frame")
    };
}

[PrefabExtension("RecruitmentPopup", "//ScrollbarWidget[@Id='CartListScrollbar']")]
internal sealed class RFRecruitmentCartScrollbarPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Sprite", "SPGeneral\\SPRecruitment\\rf_slider_thin_bed_vertical")
    };
}

[PrefabExtension("RecruitmentPopup", "//Widget[@Id='CartListScrollbarHandle']")]
internal sealed class RFRecruitmentCartScrollbarHandlePatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Sprite", "RFRecruitment_slider_thin_vertical_9")
    };
}

[PrefabExtension("RecruitVolunteerTuple", "//Widget[@Sprite='SPGeneral\\SPRecruitment\\portrait_patron']")]
internal sealed class RFRecruitmentOwnerPortraitPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Sprite", "SPGeneral\\SPRecruitment\\rf_portrait_patron")
    };
}

[PrefabExtension("RecruitTroopPanel", "//RecruitTroopPanelButtonWidget[@Brush='Recruitment.Troop.Brush']")]
internal sealed class RFRecruitmentTroopPanelPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Brush", "RF.Recruitment.Troop")
    };
}

[PrefabExtension("RecruitTroopPanelCart", "//RecruitTroopPanelButtonWidget[@Brush='Recruitment.Troop.Cart.Brush']")]
internal sealed class RFRecruitmentTroopCartPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("Brush", "RF.Recruitment.Troop.Cart")
    };
}
