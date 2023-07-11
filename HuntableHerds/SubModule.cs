using HarmonyLib;
using HuntableHerds.Models;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HuntableHerds {
    public class SubModule : MBSubModuleBase {
        public static Random Random = new();

        protected override void OnSubModuleLoad() {
            new Harmony("HuntableHerds").PatchAll();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot() {
            HerdBuildData.BuildAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter) {
            if (game.GameType is Campaign) {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;

                campaignStarter.AddBehavior(new HerdSpottingBehavior());
            }
        }

        public static void PrintDebugMessage(string str, float r = 255, float g = 255, float b = 255) {
            float normR = r / 255;
            float normG = g / 255;
            float normB = b / 255;
            InformationManager.DisplayMessage(new InformationMessage(str, new Color(normR, normG, normB)));
        }

        public static void PrintMessageBox(string title, string str, Action yes, Action? no = null) {
            if (no == null)
                no = () => { };
            InquiryData inquiry = new(title, str, true, true, "Yes", "No", yes, no);
            InformationManager.ShowInquiry(inquiry, true, true);
        }
    }
}