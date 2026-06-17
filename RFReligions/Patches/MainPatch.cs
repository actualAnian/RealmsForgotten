using HarmonyLib;
using RealmsForgotten.RFReligions.Behavior;
using RealmsForgotten.RFReligions.Core;
using RealmsForgotten.RFReligions.Helper;
using RealmsForgotten.RFReligions.Overlay;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using SandBox.View.Overlay;

namespace RealmsForgotten.RFReligions.Patches;
internal class MainPatch
{
    [HarmonyPatch(typeof(DefaultGameMenuOverlayProvider), "GetOverlay")]
    public static class DefaultGameMenuOverlayProviderPatch
    {
        internal static GameMenu.MenuOverlayType currentMenuOverlayType;

        public static bool Prefix(GameMenu.MenuOverlayType menuOverlayType, ref GameMenuOverlay __result)
        {
            currentMenuOverlayType = menuOverlayType;
            try
            {
                if (menuOverlayType == GameMenu.MenuOverlayType.Encounter)
                    __result = new EncounterMenuOverlayVM();
                else
                    __result = new ReligionsSettlementMenuOverlayVM(menuOverlayType);
                return false;
            }
            catch (Exception)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR INITIALIZING SETTLEMENT RELIGION VIEW",
                    Colors.Red));
            }

            return true;
        }
    }

    //moved to ReplaceUIPatch
    [HarmonyPatch(typeof(GauntletLayer), "LoadMovie", new Type[] { typeof(string), typeof(ViewModel) })]
    public static class LoadMoviePatch
    {
        public static void Prefix(ref string movieName, ref ViewModel dataSource)
        {
            if (movieName == "SettlementOverlay" && dataSource is SettlementMenuOverlayVM settlementMenuOverlayVm)
            {
                /*var religionsSettlementMenuOverlayVm =
                    new ReligionsSettlementMenuOverlayVM(GetOverlayPatch.currentMenuOverlayType);
                dataSource = religionsSettlementMenuOverlayVm;*/
                movieName = "ReligionSettlementOverlay";
            }
        }
    }

    public static class EncyclopediaHeroPageVMPatch
    {
        public static void Postfix(ref Hero ____hero, ref MBBindingList<StringPairItemVM> ____stats)
        {
            try
            {
                var heroReligion = ReligionUIHelper.GetHeroReligion(____hero);
                var heroReligionDevotion = ReligionUIHelper.GetHeroReligionDevotion(____hero);
                if (heroReligion != null && heroReligionDevotion != null)
                {
                    ____stats.Add(heroReligion);
                    ____stats.Add(heroReligionDevotion);
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(ChangeRelationAction), "ApplyInternal")]
    public static class ChangeRelationActionPatch
    {
        public static void Prefix(
            Hero originalHero,
            Hero originalGainedRelationWith,
            ref int relationChange,
            bool showQuickNotification,
            ChangeRelationAction.ChangeRelationDetail detail)
        {
            if (originalHero == null || originalGainedRelationWith == null)
            {
                Debug.PrintError("ChangeRelationActionPatch: one of the Hero parameters is null!");
                return;
            }

            if (originalHero.IsPlayerCompanion || originalGainedRelationWith.IsPlayerCompanion)
                return;

            if (ReligionBehavior.Instance != null
                && ReligionBehavior.Instance._heroes.TryGetValue(originalHero, out HeroReligionModel heroReligionModel1)
                && ReligionBehavior.Instance._heroes.TryGetValue(originalGainedRelationWith, out HeroReligionModel heroReligionModel2))
            {
                if (heroReligionModel1.Religion != heroReligionModel2.Religion &&
                    ReligionLogicHelper.TolerableReligions.TryGetValue(heroReligionModel1.Religion, out Core.RFReligions compatibleReligion) &&
                    compatibleReligion != Core.RFReligions.All &&
                    heroReligionModel2.Religion != compatibleReligion)
                {
                    int religionPenalty = (int)(relationChange * 0.1f);
                    relationChange -= religionPenalty;

                    if (originalHero == Hero.MainHero)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"-{religionPenalty} relation penalty with {originalGainedRelationWith.Name} due to religious intolerance.",
                            Colors.Yellow));
                    }
                }
            }
        }
    }
}