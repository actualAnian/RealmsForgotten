using HarmonyLib;
using RealmsForgotten.RFCustomSettlements;
using SandBox.Objects.Usables;
using SandBox.ViewModelCollection.Nameplate;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.ObjectSystem;
using static RealmsForgotten.RFCustomSettlements.Helper;

namespace RFCustomSettlements.Patches
{
#pragma warning disable IDE0051 // Remove unused private members

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

    //[HarmonyPatch(typeof(AgentInteractionInterfaceVM), MethodType.Constructor, typeof(Mission))]
    //public class AgentInteractionInterfaceVMPatch
    //{
    //    static void Postfix(Mission mission, AgentInteractionInterfaceVM __instance)
    //    {
    //        try
    //        {

    //            if (mission.MissionBehaviors.Where(m => (m is CustomSettlementMissionLogic)).ElementAt(0) != null)
    //            {
    //                CustomSettlementMissionLogic.interactionInterface = __instance;
    //            }
    //        }
    //        catch { }
    //    }
    //}

    [HarmonyPatch(typeof(AgentInteractionInterfaceVM), "SetUsableMachine")]
    public class AgentInteractionInterfaceVMOnFocusGainedPatch
    {
        static void Postfix(UsableMachine machine, AgentInteractionInterfaceVM __instance)
        {
            if (machine.GameEntity.Name.StartsWith("rf"))
            {
                GameKey key = HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(13);
                string button = $@"<img src=""General\InputKeys\{key.ToString().ToLower()}"" extend=""24"">";
                switch(RealmsForgotten.RFCustomSettlements.Helper.ChooseObjectType(machine.GameEntity.Name))
                { 
                    case RFUsableObjectType.Pickable:
                        string itemId = RealmsForgotten.RFCustomSettlements.Helper.GetRFPickableObjectName(machine.GameEntity.Name.Split('_'));
                        if (itemId == "gold")
                            __instance.PrimaryInteractionMessage = button + " Gold Pouch";
                        else
                            __instance.PrimaryInteractionMessage = button + " " + MBObjectManager.Instance.GetObject<ItemObject>(itemId).Name;
                        break;
                    case RFUsableObjectType.Passage:
                        __instance.PrimaryInteractionMessage = button + " Go Through";
                        __instance.IsFocusedOnExit = true;
                        break;
                    case RFUsableObjectType.Healing:
                        __instance.PrimaryInteractionMessage = button + "Heal";
                        break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MissionMainAgentInteractionComponent), "FocusStateCheckTick")]
    public class FocusStateCheckTickPatch
    {
        static readonly MethodInfo curMisScrInfo = AccessTools.PropertyGetter("MissionMainAgentInteractionComponent:CurrentMissionScreen");
        static readonly MethodInfo curMisInfo = AccessTools.PropertyGetter("MissionMainAgentInteractionComponent:CurrentMission");
        static void Postfix(MissionMainAgentInteractionComponent __instance)
        {
            UsablePlace usablePlace;
            if ((usablePlace = (UsablePlace)(__instance.CurrentFocusedMachine)) != null)
            {

                if(((MissionScreen)curMisScrInfo.Invoke(__instance, null)).SceneLayer.Input.IsGameKeyPressed(13) && usablePlace.GameEntity.Name.StartsWith("rf"))
                {
                    var c = (Mission)curMisInfo.Invoke(__instance, null);
                    ((CustomSettlementMissionLogic)c.MissionBehaviors.Where(m => m is CustomSettlementMissionLogic).ElementAt(0)).OnObjectUsed(usablePlace);
                }
            }
        }
        [HarmonyPatch(typeof(SettlementNameplateVM), "IsVisible")]
        public class IsVisiblePatch
        {
            private static void Postfix(in Vec3 cameraPosition, SettlementNameplateVM __instance, ref bool __result)
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
#pragma warning restore IDE0051 // Remove unused private members
    }
}
