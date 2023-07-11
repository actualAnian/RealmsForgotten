using HarmonyLib;
using HuntableHerds.Models;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;

namespace HuntableHerds.Patches {
    [HarmonyPatch(typeof(MapNotificationVM), "PopulateTypeDictionary")]
    internal class PopulateMapNotificationTypesPatch {
        [HarmonyPostfix]
        private static void Postfix(MapNotificationVM __instance) {
            __instance.RegisterMapNotificationType(typeof(HerdMapNotification), typeof(HerdMapNotificationItemVM));
        }
    }
}
