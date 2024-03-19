using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.UI;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Quest.UI
{
    internal static class QuestUIManager
    {
        public static void ShowNotification(string text, Action onDone, bool haveImage, string spriteId = "")
        {
            GameStateManager.Current.PushState(GameStateManager.Current.CreateState<QuestNotificationState>(text, 40, onDone, haveImage, spriteId));
        }
    }

    sealed class Cheats
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("show_notification", "rfui")]
        [UsedImplicitly]
        private static string ShowNot(List<string> strings)
        {


            QuestUIManager.ShowNotification("What was that? Not only are these lands plagued by rising undead, but now demons threaten the world of the living! We are damned...", ()=>{}, false);


            return "Done!";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("show_notification_image", "rfui")]
        [UsedImplicitly]
        private static string ShowImg(List<string> strings)
        {


            QuestUIManager.ShowNotification("What was that? Not only are these lands plagued by rising undead, but now demons threaten the world of the living! We are damned...", () => { }, true, "prisoner_image");


            return "Done!";
        }
    }
}
