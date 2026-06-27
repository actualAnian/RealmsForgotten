using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace RealmsForgotten.AiMade.ArmyCommand;

[PrefabExtension("ClanPartiesLeftPanel", "//NavigatableListPanel[@Id='ClanElementsListPanel']/Children")]
internal sealed class RFClanPartyCommandsPatch : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Child;

    public override int Index => 99;

    [PrefabExtensionFileName]
    public string Id => "RFClanPartyCommandsWidgets";
}
