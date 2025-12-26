using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using TaleWorlds.Core;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch(typeof(Banner), "TryGetBannerDataFromCode")]
    public class BannerPatches
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                // Find the constant 32 used in the comparison
                if (codes[i].opcode == OpCodes.Ldc_I4_S &&
                    codes[i].operand is sbyte operandValue &&
                    operandValue == 32)
                {
                    // Replace 32 with int.MaxValue so the condition (count > int.MaxValue) is always false
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, int.MaxValue);
                    break;
                }
            }
            return codes;
        }
    }
}
