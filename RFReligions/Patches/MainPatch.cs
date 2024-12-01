using System;
using HarmonyLib;
using RealmsForgotten.RFReligions.Behavior;
using RealmsForgotten.RFReligions.Core;
using RealmsForgotten.RFReligions.Helper;
using RealmsForgotten.RFReligions.Overlay;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;

namespace RealmsForgotten.RFReligions.Patches;

internal class MainPatch
{
    [HarmonyPatch(typeof(GameMenuOverlay), "GetOverlay")]
    public static class GetOverlayPatch
    {
        internal static GameOverlays.MenuOverlayType currentMenuOverlayType;

        public static bool Prefix(GameOverlays.MenuOverlayType menuOverlayType, ref GameMenuOverlay __result)
        {
            currentMenuOverlayType = menuOverlayType;
            try
            {
                if (menuOverlayType - GameOverlays.MenuOverlayType.SettlementWithParties > 2)
                {
                    if (menuOverlayType == GameOverlays.MenuOverlayType.Encounter)
                        __result = new EncounterMenuOverlayVM();
                    else
                        __result = null;
                }
                else
                {
                    __result = new ReligionsSettlementMenuOverlayVM(menuOverlayType);
                }

                return false;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR INITIALIZING SETTLEMENT RELIGION VIEW",
                    Colors.Red));
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(GauntletLayer), "LoadMovie")]
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

    [HarmonyPatch(typeof(EncyclopediaHeroPageVM), "Refresh")]
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
            if (ReligionBehavior.Instance?._heroes.TryGetValue(originalHero, 
                    out HeroReligionModel heroReligionModel1) == true && ReligionBehavior.Instance?._heroes.TryGetValue(
                    originalGainedRelationWith, out HeroReligionModel heroReligionModel2) == true)
            {
                if (heroReligionModel1.Religion != heroReligionModel2.Religion && ReligionLogicHelper.TolerableReligions.TryGetValue(heroReligionModel1.Religion,
                        out Core.RFReligions compatibleReligion) && compatibleReligion != Core.RFReligions.All &&
                        heroReligionModel2.Religion != compatibleReligion)
                {
                    int religionPenalty = (int)(relationChange * 0.1f);
                    relationChange = relationChange - religionPenalty;
                    if(originalHero == Hero.MainHero)
                        InformationManager.DisplayMessage(new InformationMessage($"{relationChange} of penalty on relation with {originalGainedRelationWith.Name.ToString()} for being an intolerable religion.", 
                            Colors.Yellow));
                }
            }
        }
    }
}