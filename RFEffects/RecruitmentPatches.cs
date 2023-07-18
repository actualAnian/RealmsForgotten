﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RFEffects
{
        [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "GetRecruitVolunteerFromIndividual")]
        static class GetRecruitVolunteerFromIndividualPatch
        {
            [HarmonyPostfix]
            static void Postfix(MobileParty side1Party, CharacterObject subject, Hero individual, int bitCode)
            {
                if (subject.Culture != side1Party.LeaderHero.Culture && !side1Party.ActualClan.IsMinorFaction && !side1Party.ActualClan.IsClanTypeMercenary)
                {
                    ChangeClanInfluenceAction.Apply(side1Party.ActualClan, -(subject.Tier * (10 - (side1Party.ActualClan.Renown / 10000))));

                }
            }
        }
        [HarmonyPatch(typeof(RecruitmentVM), "OnDone")]
        static class OnDonePatch
        {
            private static MethodInfo originalMethod = AccessTools.Method(typeof(RecruitmentVM), "OnDone");
            static bool isOriginalMethod;
            [HarmonyPrefix]
            static bool Prefix(MBBindingList<RecruitVolunteerTroopVM> ____troopsInCart, RecruitmentVM __instance)
            {
                if (isOriginalMethod)
                {
                    isOriginalMethod = false;
                    return true;
                }
                if (Settlement.CurrentSettlement.Culture == Hero.MainHero.Culture || ____troopsInCart.Count == 0)
                    return true;
                float total = 0;
                foreach (RecruitVolunteerTroopVM recruitVolunteerTroopVM in ____troopsInCart)
                {
                    total -= recruitVolunteerTroopVM.Character.Tier * (10 - (Hero.MainHero.Clan.Renown / 10000));
                }
                if (total > 0)
                    return true;
                total = (float)Math.Round(total, 2);
                string title = new TextObject("{=acculturarion_title}Acculturation").ToString();
                TextObject descriptionTO = new TextObject("{=acculturarion_description}Due to the volunteers being from a different culture from yours you will invest {INFLUENCE_COST} of influence by recruiting them.");
                descriptionTO.SetTextVariable("INFLUENCE_COST", -total);
                InformationManager.ShowInquiry(new InquiryData(title, descriptionTO.ToString(), true, true,
                    "Accept", "Cancel", () =>
                    {
                        isOriginalMethod = true;
                        ChangeClanInfluenceAction.Apply(Hero.MainHero.Clan, total);
                        originalMethod.Invoke(__instance, null);

                    }, null));
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_recruit_volunteers_on_condition")]
        static class game_menu_town_recruit_troops_on_conditionPatch
        {
            [HarmonyPostfix]
            static void Postfix(ref MenuCallbackArgs args)
            {
                if (Settlement.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && Settlement.CurrentSettlement.Culture != Hero.MainHero.Culture)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=recruitment_war_condition_tooltip}You are in war with this faction.");
                }

            }
        }

        [HarmonyPatch(typeof(AiVisitSettlementBehavior), "ApproximateNumberOfVolunteersCanBeRecruitedFromSettlement")]
        static class ApproximateNumberOfVolunteersCanBeRecruitedFromSettlementPatch
        {
            [HarmonyPostfix]
            static void Postfix(Hero hero, Settlement settlement, ref int __result)
            {
                if (hero.Clan != null && !hero.Clan.IsClanTypeMercenary && !hero.Clan.IsMinorFaction && settlement.MapFaction.IsAtWarWith(hero.MapFaction))
                    __result = 0;
            }
        }
}
