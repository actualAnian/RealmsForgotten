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
using TaleWorlds.Localization;
using System;

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
            CustomSettlementMissionLogic logic;
            if ((logic = Mission.Current.GetMissionBehavior<CustomSettlementMissionLogic>()) == null) return;
            if (!((MissionScreen)curMisScrInfo.Invoke(__instance, null)).SceneLayer.Input.IsGameKeyPressed(13)) return;
            var c = (Mission)curMisInfo.Invoke(__instance, null);
            Agent agent;
            if ((agent = (Agent)__instance.CurrentFocusedObject) != null && IsDeadHuntableHerdAnimal(agent))
            {
                logic.OnAgentLooted(agent);
            }
            UsablePlace? usablePlace;
            if ((usablePlace = (__instance.CurrentFocusedMachine as UsablePlace)) != null && IsRFObject(usablePlace) && CanInteract)
            {
                ((CustomSettlementMissionLogic)c.MissionBehaviors.Where(m => m is CustomSettlementMissionLogic).ElementAt(0)).OnObjectUsed(usablePlace);
            }
        }
    }
    [HarmonyPatch(typeof(AgentInteractionInterfaceVM), "SetUsableMachine")]
    public class AgentInteractionInterfaceVMSetUsableMachinePatch
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
                            try
                            {
                                TextObject itemName = MBObjectManager.Instance.GetObject<ItemObject>(itemId).Name;
                                __instance.PrimaryInteractionMessage = button + " " + itemName;
                            }
                            catch (NullReferenceException)
                            {
                                RealmsForgotten.HuntableHerds.SubModule.PrintDebugMessage($"Error, can not find an item with id \"{itemId}\"", 255, 0, 0);
                            }
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

    [HarmonyPatch(typeof(AgentInteractionInterfaceVM), "OnFocusGained")]
    public class AgentInteractionInterfaceVMOnFocusGainedPatch
    {
        [HarmonyPatch(typeof(MissionMainAgentInteractionComponent), "OnFocusGained")]
        internal static IEnumerable<CodeInstruction> OnFocusGainedPatch(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var codes = instructions.ToList();
            var insertionAgentHandle = 0;
            Label VanillaAgentHandleJumpLabel = ilGenerator.DefineLabel();
            for (var index = 0; index < codes.Count; index++)
            {
                if (codes[index].opcode == OpCodes.Ldloc_0
                    && codes[index + 1].opcode == OpCodes.Callvirt
                    && codes[index + 2].opcode == OpCodes.Brfalse_S
                    && codes[index + 3].opcode == OpCodes.Ldarg_0
                    && codes[index + 4].opcode == OpCodes.Ldarg_1)
                    {
                        codes[index].labels.Add(VanillaAgentHandleJumpLabel);
                        insertionAgentHandle = index;
                    }
            }
            var handle_rf__agents_instr_list = new List<CodeInstruction>
            {
                new(OpCodes.Ldloc_0),
                new(OpCodes.Call, AccessTools.Method(typeof(Helper), nameof(IsDeadHuntableHerdAnimal))),
                new(OpCodes.Brfalse, VanillaAgentHandleJumpLabel),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, AccessTools.Method(typeof(Helper), nameof(SetVMLook))),
                new(OpCodes.Ret)
            };
            codes.InsertRange(insertionAgentHandle, handle_rf__agents_instr_list);
            return codes.AsEnumerable();
        }

    }
    internal class MissionMainAgentInteractionComponentFocusTickPatch
    {
        internal static IEnumerable<CodeInstruction> FocusTickPatch(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var codes = instructions.ToList();
            var insertionObjectCheck = 0;
            var insertionAgentCheck = 0;
            Label startVanillaRecruitjumpLabel = ilGenerator.DefineLabel();
            Label isRFInteractableAgentjumpLabel = ilGenerator.DefineLabel();
            for (var index = 0; index < codes.Count; index++)
            {
                if (codes[index].opcode == OpCodes.Ldarg_0 // checks if interactable objects is used by RFCustomSettlements
                    && codes[index + 1].opcode == OpCodes.Ldloc_0
                    && codes[index + 2].opcode == OpCodes.Ldloc_1
                    && codes[index + 3].opcode == OpCodes.Ldloc_S
                    && codes[index - 1].opcode == OpCodes.Stloc_S)
                    insertionObjectCheck = index;

                if (codes[index].opcode == OpCodes.Ldarg_0 // jump to if not interactable objects
                    && codes[index + 1].opcode == OpCodes.Ldloc_0
                    && codes[index + 2].opcode == OpCodes.Ldloc_1
                    && codes[index + 3].opcode == OpCodes.Ldloc_S)
                    codes[index].labels.Add(startVanillaRecruitjumpLabel);


                if (codes[index].opcode == OpCodes.Ldloc_S // checks if agent is used by RFCustomSettlements
                    && codes[index + 1].opcode == OpCodes.Brfalse_S
                    && codes[index + 2].opcode == OpCodes.Ldloc_S
                    && codes[index + 3].opcode == OpCodes.Callvirt
                    && codes[index + 4].opcode == OpCodes.Brfalse_S)
                    insertionAgentCheck = index;
                if (codes[index].opcode == OpCodes.Ldloc_S // jump to if interactable agent
                    && codes[index + 1].opcode == OpCodes.Stloc_S
                    && codes[index + 2].opcode == OpCodes.Ldloc_S
                    && codes[index + 3].opcode == OpCodes.Stloc_0)
                        codes[index].labels.Add(isRFInteractableAgentjumpLabel);
            }
            var set_interactable_instr_list = new List<CodeInstruction> // checks for interactable objects
            {
                new(OpCodes.Ldloc_0, null),
                new(OpCodes.Call, AccessTools.Method(typeof(Helper), nameof(IsRFObject))),
                new(OpCodes.Brfalse, startVanillaRecruitjumpLabel),
                new(OpCodes.Ldloc_S, 4),
                new(OpCodes.Ldloc_0, null),
                new(OpCodes.Call, AccessTools.Method(typeof(Helper), nameof(IsCloseEnough))),
                new(OpCodes.Stloc_S, 21),
            };
            var check_interactable_agent_instr_list = new List<CodeInstruction> // checks for interactable agents
            {
                new(OpCodes.Ldloc_S, 14),
                new(OpCodes.Call, AccessTools.Method(typeof(Helper), nameof(RayCastToCheckForRFInteractableAgent))),
                new(OpCodes.Stloc_S, 14),
                new(OpCodes.Ldloc_S, 14),
                new(OpCodes.Brtrue, isRFInteractableAgentjumpLabel),
            };
            codes.InsertRange(insertionObjectCheck, set_interactable_instr_list);
            codes.InsertRange(insertionAgentCheck, check_interactable_agent_instr_list);
            return codes.AsEnumerable();
        }

    }
#pragma warning restore IDE0051 // Remove unused private members
}
