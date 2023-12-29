using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Library;

namespace RealmsForgotten.Patches.CulturedStart.Patches
{
    [HarmonyPatch(typeof(CraftingCampaignBehavior), "CreateTownOrder")]
    public static class ErrorPatch
    {
        private static Hero hero;
        [HarmonyPrefix]
        public static void Prefix(Hero orderOwner, int orderSlot)
        {
            hero = orderOwner;
        }
        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
                InformationManager.DisplayMessage(new InformationMessage("CREATETOWNORDER ERROR: " + hero?.StringId));

            return null;
        }
    }

    // This transpiler does not appear to be called. Copied from original version so I left it in.
    public class CSPatchGameManager
    {
        // Remove the instructions which play the campaign intro.
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            List<CodeInstruction> codesToInsert = new();
            CodeInstruction codeAtIndex = null;
            MethodInfo? method = null;
            int startIndex = 0;
            int endIndex = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Stloc_0)
                {
                    startIndex = i - 3;
                }
                if (codes[i].opcode == OpCodes.Ldloc_0)
                {
                    endIndex = i + 2;
                    codeAtIndex = codes[i + 3];
                    method = (MethodInfo)codes[i - 5].operand;
                }
            }
            codesToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
            codesToInsert.Add(new CodeInstruction(OpCodes.Call, method));
            codes.RemoveRange(startIndex, endIndex - startIndex + 1);
            codes.InsertRange(codes.IndexOf(codeAtIndex), codesToInsert);
            return codes;
        }
    }
}
