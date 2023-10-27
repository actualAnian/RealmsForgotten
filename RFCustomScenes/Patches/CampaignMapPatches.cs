using HarmonyLib;
using RealmsForgotten.RFCustomSettlements;
using SandBox.CampaignBehaviors;
using SandBox.ViewModelCollection.Nameplate;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;
using static RealmsForgotten.RFCustomSettlements.Helper;

namespace RFCustomSettlements
{
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(Campaign), "OnRegisterTypes")]
    public class RFRegisterTypes
    {
        private static void Postfix(MBObjectManager objectManager)
        {
            objectManager.RegisterType<RFCustomSettlement>("RFCustomSettlement", "Components", 100U, true, false);
        }
    }
    [HarmonyPatch(typeof(MBObjectManager), "GetMergedXmlForManaged")]
    public class SkipValidation { 
        public static bool Prefix(string id, ref bool skipValidation)
        {
            if(id == "Settlements")
            {
                skipValidation = true;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(DefaultEncounterGameMenuModel), "GetEncounterMenu")]
    public class RFEncounterMenu
    {
        public static PartyBase GetEncounteredPartyBase(PartyBase attackerParty, PartyBase defenderParty)
        {
            if (attackerParty == PartyBase.MainParty || defenderParty == PartyBase.MainParty)
            {
                if (attackerParty != PartyBase.MainParty)
                {
                    return attackerParty;
                }
                return defenderParty;
            }
            else
            {
                if (defenderParty.MapEvent == null)
                {
                    return attackerParty;
                }
                return defenderParty;
            }
        }
        private static void Postfix(PartyBase attackerParty, PartyBase defenderParty, ref string __result)
        {
            PartyBase encounteredPartyBase = GetEncounteredPartyBase(attackerParty, defenderParty);
            bool isSettlement = encounteredPartyBase.IsSettlement;
            if (isSettlement)
            {
                Settlement settlement = encounteredPartyBase.Settlement;
                bool flag = settlement.SettlementComponent != null && settlement.SettlementComponent is RFCustomSettlement;
                if (flag)
                {
                    __result = "rf_settlement_start";
                }
            }
        }
    }
    [HarmonyPatch(typeof(SettlementNameplateVM), "IsVisible")]
    public class IsVisiblePatch
    {
        private static void Postfix(SettlementNameplateVM __instance, ref bool __result)
        {
            SettlementComponent settlementComponent = __instance.Settlement.SettlementComponent;
            RFCustomSettlement? rfSettlement;
            if (!(settlementComponent == null) && (rfSettlement = settlementComponent as RFCustomSettlement) is not null)
            {
                __result = rfSettlement.IsVisible;
            }
        }
    }
    [HarmonyPatch(typeof(PartyBase), "UpdateVisibilityAndInspected")]
    public class UpdateVisibilityAndInspectedPatch
    {
        private static void Postfix(float mainPartySeeingRange, PartyBase __instance)
        {
            RFCustomSettlement? rFCustomSettlement;
            if (__instance.IsSettlement && __instance.Settlement.SettlementComponent != null && (rFCustomSettlement = __instance.Settlement.SettlementComponent as RFCustomSettlement) != null)
            {
                if (MobileParty.MainParty.Position2D.Distance(__instance.Settlement.Position2D) > mainPartySeeingRange)
                {
                    __instance.Settlement.IsVisible = rFCustomSettlement.IsVisible = false;
                }
                else
                {
                    __instance.Settlement.IsVisible = rFCustomSettlement.IsVisible = true;
                }
            }
        }
    }
    [HarmonyPatch(typeof(HideoutConversationsCampaignBehavior), "bandit_hideout_start_defender_on_condition")]
    internal class HideoutConversationsCampaignBehaviorPatch
    {
        internal static IEnumerable<CodeInstruction> StartOnConditionPatch(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var codes = instructions.ToList();
            var insertion = 0;
            var startVanillaRecruitjumpLabel = ilGenerator.DefineLabel();
            var instr_list = new List<CodeInstruction>
            {
                new(OpCodes.Call, AccessTools.Method(typeof(Helper), nameof(IsInRFSettlement))),
                new(OpCodes.Brfalse, startVanillaRecruitjumpLabel),
                new(OpCodes.Ldc_I4_0, null),
                new(OpCodes.Ret, null)
            };
            codes[0].labels.Add(startVanillaRecruitjumpLabel);
            codes.InsertRange(insertion, instr_list);
            return codes.AsEnumerable();
        }
    }
#pragma warning restore IDE0051 // Remove unused private members
}