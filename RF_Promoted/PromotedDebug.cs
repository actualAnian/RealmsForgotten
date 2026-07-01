using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RF_Promoted;

internal static class PromotedDebug
{
    public static void Message(string text)
    {
        if (!PromotedSettings.Current.DebugMessages)
        {
            return;
        }

        InformationManager.DisplayMessage(new InformationMessage("[RF Promoted] " + text, Colors.Yellow));
    }
}
