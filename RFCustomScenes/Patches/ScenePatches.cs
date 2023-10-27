using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static RealmsForgotten.RFCustomSettlements.Helper;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using System.Reflection.Emit;
using TaleWorlds.MountAndBlade.View.MissionViews;
using RealmsForgotten.RFCustomSettlements;
using SandBox.Objects.Usables;
using System.Reflection;
using TaleWorlds.MountAndBlade.View.Screens;

namespace RFCustomSettlements.Patches
{

#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(MissionMainAgentInteractionComponent), "FocusStateCheckTick")]
    public class FocusStateCheckTickPatch
    {
        static readonly MethodInfo curMisScrInfo = AccessTools.PropertyGetter("MissionMainAgentInteractionComponent:CurrentMissionScreen");
        static readonly MethodInfo curMisInfo = AccessTools.PropertyGetter("MissionMainAgentInteractionComponent:CurrentMission");
        static void Postfix(MissionMainAgentInteractionComponent __instance)
        {
            UsablePlace? usablePlace;
            if ((usablePlace = (__instance.CurrentFocusedMachine as UsablePlace)) != null)
            {

                if (((MissionScreen)curMisScrInfo.Invoke(__instance, null)).SceneLayer.Input.IsGameKeyPressed(13) && IsRFObject(usablePlace) && CanInteract)
                {
                    var c = (Mission)curMisInfo.Invoke(__instance, null);
                    ((CustomSettlementMissionLogic)c.MissionBehaviors.Where(m => m is CustomSettlementMissionLogic).ElementAt(0)).OnObjectUsed(usablePlace);
                }
            }
        }
    }
    [HarmonyPatch(typeof(AgentInteractionInterfaceVM), "SetUsableMachine")]
    public class AgentInteractionInterfaceVMOnFocusGainedPatch
    {
        static void Postfix(UsableMachine machine, AgentInteractionInterfaceVM __instance)
        {
            if (machine.GameEntity.Name.StartsWith("rf"))
            {
                GameKey key = HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(13);
                string button = CanInteract ? $@"<img src=""General\InputKeys\{key.ToString().ToLower()}"" extend=""24"">" : "";
                string[] objectName = machine.GameEntity.Name.Split('_');
                if (objectName.Length < 2) return;
                switch (ChooseObjectType(machine.GameEntity.Name))
                {
                    case RFUsableObjectType.Pickable:
                        string itemId = GetRFPickableObjectName(objectName);
                        if (itemId == "gold")
                        {
                            int amount = GetGoldAmount(objectName);
                            __instance.PrimaryInteractionMessage = button + GetNameOfGoldObject(amount);
                        }
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
    [HarmonyPatch(typeof(MissionMainAgentInteractionComponent), "FocusTick")]
    internal class MissionMainAgentInteractionComponentFocusTickPatch
    {
        internal static IEnumerable<CodeInstruction> FocusTickPatch(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var codes = instructions.ToList();
            var insertion = 0;
            var startVanillaRecruitjumpLabel = ilGenerator.DefineLabel();
            for (var index = 0; index < codes.Count; index++)
            {
                if (codes[index].opcode == OpCodes.Ldarg_0
                    && codes[index + 1].opcode == OpCodes.Ldloc_0
                    && codes[index + 2].opcode == OpCodes.Ldloc_1
                    && codes[index + 3].opcode == OpCodes.Ldloc_S
                    && codes[index - 1].opcode == OpCodes.Stloc_S)
                    insertion = index;

                if (codes[index].opcode == OpCodes.Ldarg_0
                    && codes[index + 1].opcode == OpCodes.Ldloc_0
                    && codes[index + 2].opcode == OpCodes.Ldloc_1
                    && codes[index + 3].opcode == OpCodes.Ldloc_S)
                    codes[index].labels.Add(startVanillaRecruitjumpLabel);
            }
            var instr_list = new List<CodeInstruction>
                {
                    new(OpCodes.Ldloc_0, null),
                    new(OpCodes.Call, AccessTools.Method(typeof(Helper), nameof(IsRFObject))),
                    new(OpCodes.Brfalse, startVanillaRecruitjumpLabel),
                    new(OpCodes.Ldloc_S, 4),
                    new(OpCodes.Ldloc_0, null),
                    new(OpCodes.Call, AccessTools.Method(typeof(Helper), nameof(IsCloseEnough))),
                    new(OpCodes.Stloc_S, 21),
                };
            codes.InsertRange(insertion, instr_list);
            return codes.AsEnumerable();
        }
    }
#pragma warning restore IDE0051 // Remove unused private members
}
