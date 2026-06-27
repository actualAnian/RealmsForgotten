using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace RealmsForgotten.AiMade.ArmyCommand;

[PrefabExtension("ArmyManagementRightPanel", "//Window/BrushWidget/Children")]
internal sealed class RFArmyManagementPatch : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Child;
    public override int Index => 2;

    [PrefabExtensionFileName]
    public string Id => "RFArmyManagementWidgets";
}
