using HarmonyLib;
using SandBox.View;
using StoryMode.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade.ViewModelCollection.InitialMenu;

namespace RealmsForgotten.Patches
{
    [HarmonyDebug]
    [HarmonyPatch(typeof(InitialMenuVM), "RefreshMenuOptions")]
    public static class RefreshMenuOptionsPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilgenerator)
        {
            int insertion = -1;
            Label jumpLabel = ilgenerator.DefineLabel();

            List<CodeInstruction> instructionsToAdd = new()
            {
                new CodeInstruction(OpCodes.Ldloc_1, null),
                new CodeInstruction(OpCodes.Call, AccessTools.Method("RealmsForgotten.Utility.Helper:ShouldLoadMainMenuOption")),
                new CodeInstruction(OpCodes.Brfalse, jumpLabel)
            };

            List<CodeInstruction> codes = instructions.ToListQ<CodeInstruction>();
            for (int index = 0; index < codes.Count; index++)
            {
                if (codes[index].opcode == OpCodes.Ldarg_0 &&
                    codes[index + 1].opcode == OpCodes.Call &&
                    codes[index + 2].opcode == OpCodes.Ldloc_1 &&
                    codes[index+ 3].opcode == OpCodes.Newobj)
                {
                    insertion = index;
                }
                if (codes[index].opcode == OpCodes.Ldloc_0 &&
                    codes[index + 1].opcode == OpCodes.Callvirt &&
                    codes[index + 2].opcode == OpCodes.Brtrue_S &&
                    codes[index + 3].opcode == OpCodes.Leave_S)
                {
                    codes[index].labels.Add(jumpLabel);
                }
            }
            codes.InsertRange(insertion, instructionsToAdd);
            return codes.AsEnumerable<CodeInstruction>();
        }

        //[HarmonyPatch(typeof(SandBoxViewSubModule), "OnSubModuleLoad")]
        //public static class SandBoxViewSubModulePatch
        //{
        //    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilgenerator)
        //    {
        //        List<CodeInstruction> codes = instructions.ToListQ<CodeInstruction>();

        //        //codes.InsertRange(insertion, instructionsToAdd);
        //        return codes.AsEnumerable<CodeInstruction>();
        //    }
        //}

    }
}
