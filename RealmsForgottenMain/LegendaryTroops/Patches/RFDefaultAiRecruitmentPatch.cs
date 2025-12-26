using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.LinQuick;

namespace RealmsForgotten.LegendaryTroops.Patches
{
    [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "CheckRecruiting")]
    internal class CheckRecruitingPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilgenerator)
        {
            bool patchInsertionCheck = false;
            int insertion = 0;
            Label jumpLabel = ilgenerator.DefineLabel();
            
            List<CodeInstruction> codes = instructions.ToListQ();
            List<CodeInstruction> instructionsToAdd = new()
            {
                new CodeInstruction(OpCodes.Ldarg_1, null),
                new CodeInstruction(OpCodes.Ldarg_2, null),
                new CodeInstruction(OpCodes.Call, AccessTools.Method("RealmsForgotten.LegendaryTroops.Helper:CanRecruitIfInCastle")),
                new CodeInstruction(OpCodes.Brfalse, jumpLabel)
            };

            for (int index = 0; index < codes.Count; index++)
            {
                if (codes[Math.Abs(index - 1)].opcode == OpCodes.Brtrue_S && codes[index].opcode == OpCodes.Ldarg_0 && codes[index + 1].opcode == OpCodes.Ldarg_1 && codes[index + 2].opcode == OpCodes.Ldarg_2)
                {
                    if (patchInsertionCheck) throw new ArgumentException("patch RealmsForgotten.LegendaryTroops.CheckRecruitingPatch has been added multiple times!");
                    patchInsertionCheck = true;
                    insertion = index;
                }
                if (codes[index].opcode == OpCodes.Ret)
                {
                    codes[index].labels.Add(jumpLabel);
                }
            }
            codes.InsertRange(insertion, instructionsToAdd);
            return codes.AsEnumerable();
        }
    }
}
