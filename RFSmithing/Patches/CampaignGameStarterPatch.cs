using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.Smithing.Patches
{
    [HarmonyPatch(typeof(CampaignGameStarter), "AddModel", new Type[] { typeof(GameModel) })]
    internal static class CampaignGameStarterPatch
    {
        // Skip Banner Kings' smithing model.
        public static bool Prefix(GameModel gameModel) => gameModel.GetType() != AccessTools.TypeByName("BKSmithingModel");
    }
}