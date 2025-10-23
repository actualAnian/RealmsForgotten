using HarmonyLib;
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
        public static void Postfix(MenuCallbackArgs args)
        {
            if (MiscellaneousConfig.EnterHideoutGameMenuText.TryGetValue(Settlement.CurrentSettlement.Culture.StringId, out var text))
                GameTexts.SetVariable("HIDEOUT_DESCRIPTION", text);
        }
    }
}
