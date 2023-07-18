using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NecromancyAndSummoning.Patch
{
    internal class SummoningAndRaiseCorpsePatch
    {
        public static void Postfix(Vec3 missileStartingPosition, Vec3 missilePosition, int numDamagedAgents, Agent attacker, Agent victim)
        {
            try
            {
                if (NecromancyAndSummoningLogic.IsInBattle())
                {
                    ItemObject wieldedItem = NecroSummon.GetWieldedItem(attacker);
                    if (!NecroSummon.IsAgentOverLimit())
                    {
                        if (SubModule.Config.EnablePlayerSummon && wieldedItem != null && attacker == Agent.Main)
                        {
                            NecroSummon.Summoning(attacker, missilePosition);
                        }
                        if (SubModule.Config.EnableTroopSummon && wieldedItem != null && attacker != Agent.Main)
                        {
                            NecroSummon.Summoning(attacker, missilePosition);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string str = "SummoningAndRaiseCorpsePatch Error: ";
                string str2 = ex.Message.ToString();
                string str3 = "\n";
                Exception innerException = ex.InnerException;
                throw new Exception((str + str2 + str3 + ((innerException != null) ? innerException.ToString() : null) != null) ? ex.InnerException.Message.ToString() : "");
            }
        }
    }
}
