using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using System.Collections.Generic;
using PrefabAttribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace RealmsForgotten.AiMade.UIOverhaul;

[PrefabExtension("RFCharacterDeveloper", "descendant::CharacterTableauWidget")]
internal sealed class RFCharacterDeveloperTableauPositionPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("PositionYOffset", "140")
    };
}

[PrefabExtension("RFCharacterDeveloper", "descendant::ButtonWidget[@Sprite='move_to_career_icon']")]
internal sealed class RFCharacterDeveloperClassButtonSizePatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("SuggestedWidth", "160"),
        new PrefabAttribute("SuggestedHeight", "52")
    };
}

[PrefabExtension("RFCharacterDeveloper", "descendant::ListPanel[@Id='PercentageIndicatorWidget']")]
internal sealed class RFCharacterDeveloperSkillIndicatorOrderPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("StackLayout.LayoutMethod", "VerticalTopToBottom")
    };
}
