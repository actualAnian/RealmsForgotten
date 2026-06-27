using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Localization;

namespace RF_Enlistment;

internal static class RFLordConversationsCampaignBehaviorPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_lord_join_army_on_condition")]
    private static bool ConversationLordJoinArmyOnConditionPrefix(ref bool __result)
    {
        if (RFEnlistmentCampaignBehavior.Instance?.IsEnlistedForConversationOverrides() != true)
        {
            return true;
        }

        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_lord_join_army_on_clickable_condition")]
    private static bool ConversationLordJoinArmyOnClickableConditionPrefix(ref bool __result, ref TextObject hint)
    {
        if (RFEnlistmentCampaignBehavior.Instance?.IsEnlistedForConversationOverrides() != true)
        {
            return true;
        }

        hint = new TextObject("{=rf_enlistment_already_serving_hint}You are already bound to military service.");
        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_ally_thanks_meet_after_helping_in_battle_on_condition")]
    private static bool ConversationAllyThanksMeetAfterHelpingInBattleOnConditionPrefix(ref bool __result)
    {
        if (RFEnlistmentCampaignBehavior.Instance?.IsEnlistedForConversationOverrides() != true)
        {
            return true;
        }

        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_ally_thanks_after_helping_in_battle_on_condition")]
    private static bool ConversationAllyThanksAfterHelpingInBattleOnConditionPrefix(ref bool __result)
    {
        if (RFEnlistmentCampaignBehavior.Instance?.IsEnlistedForConversationOverrides() != true)
        {
            return true;
        }

        __result = false;
        return false;
    }
}
