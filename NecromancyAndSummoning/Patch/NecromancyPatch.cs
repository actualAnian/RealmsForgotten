using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.NecromancyAndSummoning.Patch
{
	// Token: 0x0200000A RID: 10
	[HarmonyPatch(typeof(Mission), "OnAgentRemoved")]
	internal class NecromancyPatch
	{
		// Token: 0x0600006B RID: 107 RVA: 0x000056E0 File Offset: 0x000038E0
		public static void Postfix(Agent affectedAgent, Agent affectorAgent)
		{
			bool flag = !NecromancyAndSummoningLogic.IsInBattle();
			if (!flag)
			{
				bool flag2 = false;
				bool flag3 = affectorAgent == null || affectedAgent == null || affectorAgent.Character == null || affectedAgent.Character == null || !affectedAgent.IsHuman;
				if (!flag3)
				{
					bool flag4 = affectedAgent.State == AgentState.Killed && NecroSummon.IsImmuneUnit(affectedAgent);
					if (!flag4)
					{
						try
						{
							bool flag7 = NecromancyPatch.itemInfect;
							if (flag7)
							{
								bool flag8 = NecroSummon.IsValidInfectItem(affectorAgent) && !flag2;
								if (flag8)
								{
									string infectedTroopId = NecroSummon.GetItemInfectedUnitId(affectorAgent.WieldedWeapon.Item.StringId);
									flag2 = NecroSummon.Infection(affectorAgent, affectedAgent, infectedTroopId);
								}
							}
						}
						catch (Exception ex)
						{
							string str = "Necromancy Patch Error: ";
							string str2 = ex.Message.ToString();
							string str3 = "\n";
							Exception innerException = ex.InnerException;
							throw new Exception((str + str2 + str3 + ((innerException != null) ? innerException.ToString() : null) != null) ? ex.InnerException.Message.ToString() : "");
						}
					}
				}
			}
		}

		// Token: 0x0400000F RID: 15
		internal static bool troopInfect = SubModule.Config.EnableTroopInfect;

		// Token: 0x04000010 RID: 16
		internal static bool itemInfect = SubModule.Config.EnableItemInfect;
	}
}
