using HarmonyLib;
using RealmsForgotten.RFCustomSettlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.ObjectSystem;

namespace RFCustomSettlements.Patches
{

    [HarmonyPatch(typeof(Campaign), "OnRegisterTypes")]
    public class CEKRegisterTypes
    {
        // Token: 0x06000116 RID: 278 RVA: 0x00006BC8 File Offset: 0x00004DC8
        private static void Postfix(MBObjectManager objectManager)
        {
            objectManager.RegisterType<RFCustomSettlement>("RFCustomSettlement", "Components", 100U, true, false);
        }
    }

    //[HarmonyPrefix]
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
    public static class Helper
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
    }
    [HarmonyPatch(typeof(DefaultEncounterGameMenuModel), "GetEncounterMenu")]
    public class RFEncounterMenu
    {
    private static void Postfix(PartyBase attackerParty, PartyBase defenderParty, bool startBattle, bool joinBattle, DefaultEncounterGameMenuModel __instance, ref string __result)
    {
        PartyBase encounteredPartyBase = Helper.GetEncounteredPartyBase(attackerParty, defenderParty);
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
}
