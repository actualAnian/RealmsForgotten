using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(CharacterTableau), nameof(CharacterTableau.SetRace))]
    public class SetRacePostFix
    {
        static void Postfix(CharacterTableau __instance)
        {
            var _agentVisuals = AccessTools.Field(typeof(CharacterTableau), "_agentVisuals")?.GetValue(__instance) as AgentVisuals;
            _agentVisuals?.Reset();
            var _oldAgentVisuals = AccessTools.Field(typeof(CharacterTableau), "_oldAgentVisuals")?.GetValue(__instance) as AgentVisuals;
            _oldAgentVisuals?.Reset();
            AccessTools.Method(typeof(CharacterTableau), "InitializeAgentVisuals").Invoke(__instance, new object[] { });
        }
    }
}
