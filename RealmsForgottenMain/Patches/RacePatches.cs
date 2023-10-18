using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch(typeof(MissionCombatMechanicsHelper), "DecideAgentShrugOffBlow")]
    static class MissionCombatMechanicsHelperPatch
    {
        // patch to prevent half-giants from being staggered
        public static void Postfix(Agent victimAgent, ref bool __result)
        {
            if (victimAgent.Character != null && victimAgent.Character.Race == Globals.GiantsRaceId)
            {
                __result = true;
            };
        }
    }
}
