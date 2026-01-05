using HarmonyLib;
using RealmsForgotten.Quest;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace RealmsForgotten.Patches
{
    internal class GameMenuPatches
    {
        [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_hideout_place_on_init")]
        [HarmonyPostfix]
        public static void HideoutInitPostfix(MenuCallbackArgs args)
        {
            if(Settlement.CurrentSettlement == null) return;
            if (MiscellaneousConfig.EnterHideoutGameMenuText.TryGetValue(Settlement.CurrentSettlement.Culture.StringId, out var text))
                GameTexts.SetVariable("HIDEOUT_DESCRIPTION", text);
            QuestHelperCampaignBehavior.TryAddQuest2HideoutText();
        }
        [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_send_troops_hideout_on_condition")]
        [HarmonyPostfix]
        public static void SendTroopsPostfix(bool __result, MenuCallbackArgs args)
        {
            if (QuestHelperCampaignBehavior.IsInHideoutForQuest2())
            {
                args.Tooltip = new("{=rf_quest2_storm}You have to storm this hideout and retrieve the map yourself!");
                args.IsEnabled = false;
                __result = false;
            }
        }
        [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_hideout_sneak_in_on_condition")]
        [HarmonyPostfix]
        public static void SneakInPostfix(bool __result, MenuCallbackArgs args)
        {
            if (QuestHelperCampaignBehavior.IsInHideoutForQuest2())
            {
                args.Tooltip = new("{=rf_quest2_storm}You have to storm this hideout and retrieve the map yourself!");
                args.IsEnabled = false;
                __result = false;
            }
        }
        [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_assault_hideout_parties_on_condition")]
        [HarmonyPostfix]
        public static void AssaultHideoutPostfix(bool __result, MenuCallbackArgs args)
        {
            if (QuestHelperCampaignBehavior.IsInHideoutForQuest2())
            {
                args.Tooltip = new("{=rf_quest2_storm}You have to storm this hideout and retrieve the map yourself!");
                args.IsEnabled = false;
                __result = false;
            }
        }

    }
}
