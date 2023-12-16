using HarmonyLib;
using RealmsForgotten.Quest.SecondUpdate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;

namespace RealmsForgotten.Quest
{
    public static class QuestPatches
    {
        public static bool AvoidDisbanding = false;

        public static void PatchAll()
        {

            SubModule.harmony.Patch(AccessTools.Method(typeof(Hero), "CanHaveQuestsOrIssues"), postfix: new HarmonyMethod(typeof(HeroPatches), nameof(HeroPatches.CanHaveQuestsOrIssuesPostfix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(PrisonerReleaseCampaignBehavior), "DailyHeroTick"), prefix: new HarmonyMethod(typeof(PrisonerReleaseCampaignBehaviorPatches), nameof(PrisonerReleaseCampaignBehaviorPatches.Prefix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(DisbandArmyAction), "ApplyInternal"), prefix: new HarmonyMethod(typeof(AvoidArmyDispersePatch), nameof(AvoidArmyDispersePatch.Prefix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(PlayerArmyWaitBehavior), "wait_menu_army_leave_on_condition"), postfix: new HarmonyMethod(typeof(PlayerArmyWaitBehaviorPatches), nameof(PlayerArmyWaitBehaviorPatches.Postfix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(EncounterGameMenuBehavior), "game_menu_encounter_order_attack_on_condition"), postfix: new HarmonyMethod(typeof(AvoidPlayerDontFightingPatch), nameof(AvoidPlayerDontFightingPatch.game_menu_encounter_order_attack_on_condition_postfix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(EncounterGameMenuBehavior), "game_menu_encounter_leave_your_soldiers_behind_on_condition"), postfix: new HarmonyMethod(typeof(AvoidPlayerDontFightingPatch), nameof(AvoidPlayerDontFightingPatch.game_menu_encounter_leave_your_soldiers_behind_on_condition_postfix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(EncounterGameMenuBehavior), "game_menu_encounter_surrender_on_condition"), postfix: new HarmonyMethod(typeof(AvoidPlayerDontFightingPatch), nameof(AvoidPlayerDontFightingPatch.game_menu_encounter_surrender_on_condition_postfix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(BanditsCampaignBehavior), "bandit_start_barter_condition"), postfix: new HarmonyMethod(typeof(AvoidBarterPatch), nameof(AvoidBarterPatch.Postfix)));

        }

        public static class AvoidPlayerDontFightingPatch
        {
            public static void game_menu_encounter_order_attack_on_condition_postfix(MenuCallbackArgs args, ref bool __result)
            {
                if (FourthQuest.DisableSendTroops)
                {
                    args.IsEnabled = false;
                    __result = false;
                }
            }
            public static void game_menu_encounter_leave_your_soldiers_behind_on_condition_postfix(MenuCallbackArgs args, ref bool __result)
            {
                if (FourthQuest.DisableSendTroops)
                {
                    args.IsEnabled = false;
                    __result = false;
                }
            }
            public static void game_menu_encounter_surrender_on_condition_postfix(MenuCallbackArgs args, ref bool __result)
            {
                if (FourthQuest.DisableSendTroops)
                {
                    args.IsEnabled = false;
                    __result = false;
                }
            }
        }

        public static class AvoidBarterPatch
        {
            public static void Postfix(ref bool __result)
            {
                if (FourthQuest.DisableSendTroops)
                    __result = false;
            }
        }
        private static class HeroPatches
        {
            public static void CanHaveQuestsOrIssuesPostfix(ref bool __result, Hero __instance)
            {
                if (Kingdom.All.First(x => x.StringId == "empire").Leader.Spouse.HomeSettlement.Notables[0].StringId == __instance.StringId)
                    __result = false;
            }
        }

        public static class PrisonerReleaseCampaignBehaviorPatches
        {
            public static bool Prefix(Hero hero)
            {
                if (ThirdQuest.MustAvoidPrisonerEscape && hero.CharacterObject == ThirdQuest.PrisonerCharacter)
                {
                    return false;
                }
                return true;
            }
        }

        public static class AvoidArmyDispersePatch
        {
            public static bool Prefix(Army army, Army.ArmyDispersionReason reason)
            {
                if (AvoidDisbanding && army.Parties.Any(x => x == MobileParty.MainParty))
                {
                    return false;
                }

                return true;
            }
        }

        public static class PlayerArmyWaitBehaviorPatches
        {
            public static void Postfix(MenuCallbackArgs args, ref bool __result)
            {
                if (AvoidDisbanding)
                {
                    __result = false;
                }
            }
        }
    }
}
