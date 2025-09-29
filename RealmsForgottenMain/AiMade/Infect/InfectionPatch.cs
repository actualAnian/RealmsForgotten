using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Infect
{
    [HarmonyPatch(typeof(Mission))]
    [HarmonyPatch("OnAgentRemoved")]
    public static class RealmsForgottenInfectPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Agent affectedAgent, Agent affectorAgent)
        {
            if (Mission.Current == null || !Mission.Current.IsLoadingFinished)
                return;

            if (affectedAgent == null || affectorAgent == null)
                return;

            if (!affectedAgent.IsHuman || affectedAgent.IsHero)
                return;

            if (Mission.Current.Mode != MissionMode.Battle &&
                Mission.Current.Mode != MissionMode.Stealth &&
                Mission.Current.Mode != MissionMode.Duel)
                return;

            InfectionMissionBehavior.EnsureOn(Mission.Current).Enqueue(affectedAgent, affectorAgent);
        }
    }
}
