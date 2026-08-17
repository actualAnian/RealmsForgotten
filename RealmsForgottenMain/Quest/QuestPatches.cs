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
        private static bool _athasScholarLoadRepairApplied;

        public static void PatchAthasScholarLoadRepair()
        {
            if (_athasScholarLoadRepairApplied)
            {
                return;
            }

            MethodInfo heroAfterLoad = AccessTools.Method(typeof(Hero), "AfterLoad");
            if (heroAfterLoad == null)
            {
                RealmsForgotten.AiMade.RFLogger.Log("[ThirdQuest] Could not install Athas Scholar load repair: Hero.AfterLoad was not found.");
                return;
            }

            SubModule.harmony.Patch(heroAfterLoad,
                prefix: new HarmonyMethod(typeof(AthasScholarHeroLoadRepairPatch), nameof(AthasScholarHeroLoadRepairPatch.Prefix)));
            _athasScholarLoadRepairApplied = true;
        }

        public static void PatchAll()
        {
            PatchAthasScholarLoadRepair();
            SubModule.harmony.Patch(AccessTools.Method(typeof(Hero), "CanHaveCampaignIssues"), postfix: new HarmonyMethod(typeof(HeroPatches), nameof(HeroPatches.CanHaveCampaignIssuesPostfix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(PrisonerReleaseCampaignBehavior), "DailyHeroTick"), prefix: new HarmonyMethod(typeof(PrisonerReleaseCampaignBehaviorPatches), nameof(PrisonerReleaseCampaignBehaviorPatches.Prefix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(DisbandArmyAction), "ApplyInternal"), prefix: new HarmonyMethod(typeof(AvoidArmyDispersePatch), nameof(AvoidArmyDispersePatch.Prefix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(PlayerArmyWaitBehavior), "wait_menu_army_leave_on_condition"), postfix: new HarmonyMethod(typeof(PlayerArmyWaitBehaviorPatches), nameof(PlayerArmyWaitBehaviorPatches.Postfix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(EncounterGameMenuBehavior), "game_menu_encounter_order_attack_on_condition"), postfix: new HarmonyMethod(typeof(AvoidPlayerDontFightingPatch), nameof(AvoidPlayerDontFightingPatch.game_menu_encounter_order_attack_on_condition_postfix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(EncounterGameMenuBehavior), "game_menu_encounter_leave_your_soldiers_behind_on_condition"), postfix: new HarmonyMethod(typeof(AvoidPlayerDontFightingPatch), nameof(AvoidPlayerDontFightingPatch.game_menu_encounter_leave_your_soldiers_behind_on_condition_postfix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(EncounterGameMenuBehavior), "game_menu_encounter_surrender_on_condition"), postfix: new HarmonyMethod(typeof(AvoidPlayerDontFightingPatch), nameof(AvoidPlayerDontFightingPatch.game_menu_encounter_surrender_on_condition_postfix)));
            SubModule.harmony.Patch(AccessTools.Method(typeof(BanditInteractionsCampaignBehavior), "bandit_start_barter_condition"), postfix: new HarmonyMethod(typeof(AvoidBarterPatch), nameof(AvoidBarterPatch.Postfix)));
        }

        public static class AthasScholarHeroLoadRepairPatch
        {
            private static readonly FieldInfo HeroPerksField = AccessTools.Field(typeof(Hero), "_heroPerks");

            public static void Prefix(Hero __instance)
            {
                if (__instance == null || HeroPerksField == null)
                {
                    return;
                }

                bool isAthasScholar = string.Equals(__instance.StringId, "rf_athas_scholar", StringComparison.Ordinal)
                    || string.Equals(__instance.CharacterObject?.StringId, "rf_athas_scholar", StringComparison.Ordinal);
                if (!isAthasScholar || HeroPerksField.GetValue(__instance) != null)
                {
                    return;
                }

                object emptyPerkState = Activator.CreateInstance(HeroPerksField.FieldType);
                if (emptyPerkState != null)
                {
                    HeroPerksField.SetValue(__instance, emptyPerkState);
                    RealmsForgotten.AiMade.RFLogger.Log($"[ThirdQuest] Repaired missing perk state while loading Athas Scholar | hero={__instance.StringId}");
                }
            }
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

        // Make this class public so it can be accessed in the PatchAll method
        public static class HeroPatches
        {
            public static void CanHaveCampaignIssuesPostfix(ref bool __result, Hero __instance)
            {
                var empireKingdom = Kingdom.All?.FirstOrDefault(x => x?.StringId == "empire");

                if (empireKingdom?.Leader?.Spouse?.HomeSettlement?.Notables != null
                    && empireKingdom.Leader.Spouse.HomeSettlement.Notables.Count > 0
                    && empireKingdom.Leader.Spouse.HomeSettlement.Notables[0].StringId == __instance.StringId)
                {
                    __result = false;
                }
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
                    RealmsForgotten.AiMade.RFLogger.Log($"[QuestPatches] Army disperse blocked | leader={army.LeaderParty?.StringId ?? "none"} | reason={reason} | avoidDisbanding={AvoidDisbanding}");
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
