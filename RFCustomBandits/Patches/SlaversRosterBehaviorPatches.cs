using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.LinQuick;

namespace RealmsForgotten.RFCustomBandits.Patches
{

    [HarmonyPatch(typeof(PartyBase), "PartySizeLimit", MethodType.Getter)]
    internal class GetPartySizeLimitPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilgenerator)
        {
            int insertion = 0;
            Label jumpLabel = ilgenerator.DefineLabel();

            var field_partyMemberSizeLastCheckVersion = AccessTools.Field("TaleWorlds.CampaignSystem.Party.PartyBase:_partyMemberSizeLastCheckVersion");
            var field_cachedPartyMemberSizeLimit = AccessTools.Field("TaleWorlds.CampaignSystem.Party.PartyBase:_cachedPartyMemberSizeLimit");
            List<CodeInstruction> codes = instructions.ToListQ<CodeInstruction>();

            codes[0].labels.Add(jumpLabel);
            List<CodeInstruction> instructionsToAdd = new()
            {
                new CodeInstruction(OpCodes.Ldarg_0, null),
                new CodeInstruction(OpCodes.Ldarg_0, null),
                new CodeInstruction(OpCodes.Call, AccessTools.Method("RealmsForgotten.RFCustomBandits.SlaversRosterBehavior:ChangeTotalSizeLimitIfSlavers")),
                new CodeInstruction(OpCodes.Stfld, field_cachedPartyMemberSizeLimit),
                new CodeInstruction(OpCodes.Ldarg_0, null),
                new CodeInstruction(OpCodes.Ldfld, field_cachedPartyMemberSizeLimit),
                new CodeInstruction(OpCodes.Brtrue, jumpLabel),
                };

            for (int index = 0; index < codes.Count; index++)
            {
                if (codes[Math.Abs(index-1)].opcode == OpCodes.Stfld && codes[Math.Abs(index - 1)].operand == (object)field_partyMemberSizeLastCheckVersion)
                {
                    insertion = index;
                }

                if (codes[index].opcode == OpCodes.Conv_I4 && codes[index+1].opcode == OpCodes.Stfld && codes[index+1].operand == (object)field_cachedPartyMemberSizeLimit)
                {
                    codes[index].labels.Add(jumpLabel);
                }
            }
            codes.InsertRange(insertion, instructionsToAdd);
            return codes.AsEnumerable<CodeInstruction>();
        }
    }
    [HarmonyPatch(typeof(PlayerCaptivityCampaignBehavior), nameof(PlayerCaptivityCampaignBehavior.CheckCaptivityChange))]
    internal class CheckCaptivityChangePatch
    {
        static int ticksNeeded = 1000;
        static int tickStart = 0;
        private static void Prefix(PlayerCaptivityCampaignBehavior __instance, float dt)
        {
            if(PlayerCaptivity.CaptorParty.Culture.StringId == "aserai")
            {
                if(tickStart < ticksNeeded)
                {
                    tickStart += 1;
                }
                else
                {
                    tickStart = 0;
                    GameMenu.SwitchToMenu("menu_captivity_end_by_ally_party_saved");
                    return;
                }

            }
        }
    }
}
