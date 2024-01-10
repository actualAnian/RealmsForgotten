using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.LinQuick;
using System;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core;

namespace RealmsForgotten.Patches
{
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(MissionCombatMechanicsHelper), "DecideAgentShrugOffBlow")]
    static class MissionCombatMechanicsHelperPatch
    {
        // patch to prevent half-giants from being staggered
        public static void Postfix(Agent victimAgent, ref bool __result)
        {
            if (victimAgent.Character != null && victimAgent.Character.IsGiant())
            {
                __result = true;
            };
        }
    }
    [HarmonyPatch(typeof(TroopRoster), "TotalManCount", MethodType.Getter)]
    static class TotalManCountGetPatch
    {
        // makes giants count as more than 1 member
        public static void Postfix(TroopRoster __instance, ref int __result)
        {
            __result = 0;
            foreach (TroopRosterElement rosterElement in __instance.GetTroopRoster())
            {
                if (!rosterElement.Character.IsGiant()) __result += rosterElement.Number;
                else __result += rosterElement.Number * Globals.GiantCountsAs;
            }
        }
    }
    internal class PartyVMPatch
    {
        private static TextObject SetTextVariable(MBBindingList<PartyCharacterVM> partyList)
        {
            int troopNumber = 0;
            int giantNumber = 0;
            foreach (PartyCharacterVM p in partyList)
            {
                
                troopNumber += MathF.Max(0, p.Number - p.WoundedCount);
                if(p.Character != null && p.Character.IsGiant()) giantNumber += p.Number;
            }
            int a = partyList.Sum((PartyCharacterVM item) => MathF.Max(0, item.Number - item.WoundedCount));
            string str;
            if (giantNumber != 0) str = $"{troopNumber + giantNumber * (Globals.GiantCountsAs - 1)}: {troopNumber - giantNumber} + {giantNumber} giants";
            else str = $"{troopNumber}";
            return new TextObject(str);
        }
        public static IEnumerable<CodeInstruction> PartyVMPopulatePartyListLabelPatch(IEnumerable<CodeInstruction> instructions, ILGenerator ilgenerator)
        {
            bool isRepeatingCodeInsertion = false;
            bool isRepeatingLabelAdd = false;
            Label jumpLabel = ilgenerator.DefineLabel();

            int insertion = 0;
            List<CodeInstruction> codes = instructions.ToListQ<CodeInstruction>();
            for (int index = 0; index < codes.Count; index++)
            {
                if (codes[index].opcode == OpCodes.Ldloc_0 && codes[index + 1].opcode == OpCodes.Call && codes[index + 2].opcode == OpCodes.Ldstr && codes[index + 3].opcode == OpCodes.Ldloc_1 && codes[index -1].opcode == OpCodes.Ldstr)
                {
                    if (isRepeatingCodeInsertion)
                    {
                        throw new InvalidProgramException("invalid patch at RealmsForgotten.Patches.PartyVMPopulatePartyListLabelPatch during insertion add");
                    }
                    isRepeatingCodeInsertion = true;
                    insertion = index;
                }

                if (codes[index].opcode == OpCodes.Call  && codes[index+1].opcode == OpCodes.Ldstr && codes[index + 2].opcode == OpCodes.Ldloc_1 && codes[index + 3].opcode == OpCodes.Call && codes[index + 4].opcode == OpCodes.Ldarg_1)
                {
                    if (isRepeatingLabelAdd)
                    {
                        throw new InvalidProgramException("invalid patch at RealmsForgotten.Patches.PartyVMPopulatePartyListLabelPatch, during label add");
                    }
                    isRepeatingLabelAdd = true;
                    codes[index + 1].labels.Add(jumpLabel);
                }
            }
            List<CodeInstruction> stack = new()
            {
                new CodeInstruction(OpCodes.Ldarg_0, null),
                new CodeInstruction(OpCodes.Call, AccessTools.Method("RealmsForgotten.Patches.PartyVMPatch:SetTextVariable")),
                new CodeInstruction(OpCodes.Ldc_I4_0, null),
                new CodeInstruction(OpCodes.Call, AccessTools.Method("TaleWorlds.Localization.MBTextManager:SetTextVariable", new Type[] { typeof(string), typeof(TextObject), typeof(bool)})),
                new CodeInstruction(OpCodes.Br, jumpLabel)
            };
            codes.InsertRange(insertion, stack);
            return codes.AsEnumerable<CodeInstruction>();
        }
    }
    [HarmonyPatch(typeof(RecruitmentVM), "CurrentPartySize", MethodType.Getter)]
    static class RecruitmentVMRefreshPartyPropertiesPatch
    {
        // giants count as more troops when adding a troop to cart during recruitment
        public static void Postfix(RecruitmentVM __instance, ref int __result)
        {
            int noGiants = 0;
            foreach(RecruitVolunteerTroopVM troop in __instance.TroopsInCart)
            {
                if(troop.Character.IsGiant()) noGiants++;
            }
            __result += noGiants * (Globals.GiantCountsAs -1);
        }
    }
    //[HarmonyPatch(typeof(RecruitVolunteerTroopVM), "RefreshValues")]
    //static class RecruitVolunteerTroopVMRefreshValuesPatch
    //{
    //    // adds the "costs 2 to the viewmodel while adding a troop"
    //    public static void Postfix(RecruitVolunteerTroopVM __instance)
    //    {
//            if(__instance.Character != null)
//            {
//                if(__instance.Character.IsGiant())
//                {
//                    __instance.Wage = __instance.Wage * Globals.GiantsCostMult;
//                }
//                //__instance.Wage = 10000;
//                //__instance.NameText += "aaaaaaaaaaaa";
//                __instance.NameText += "\n lololo";
//                //__instance.Level += "\n lololo";
//            }
            //int noGiants = 0;
            //foreach (RecruitVolunteerTroopVM troop in __instance.TroopsInCart)
            //{
            //    if (troop.Character.IsGiant()) noGiants++;
            //}
            //__result += noGiants * (Globals.GiantCountsAs - 1);
    //    }
    //}
        // adds the "costs 2 to the viewmodel while adding a troop"
        //used for information screen when hovering over a troop
    [HarmonyPatch(typeof(TooltipRefresherCollection), "RefreshCharacterTooltip")]
    internal class UpdateTooltipPatch
    {
        public static void Postfix(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
        {
            CharacterObject? characterObject = args[0] as CharacterObject;

            if (characterObject !=null && characterObject.TroopWage > 0)
            {
                //                GameTexts.SetVariable("STR2", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                propertyBasedTooltipVM.AddProperty("1111", $"Troop Limit: {(characterObject.IsGiant()? Globals.GiantCountsAs : 1)}", 0, TooltipProperty.TooltipPropertyFlags.MultiLine);
            }
        }
    }
#pragma warning restore IDE0051 // Remove unused private members
}
