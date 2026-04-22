using System;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace NecromancyAndSummoning.Patch
{
	// Token: 0x02000008 RID: 8
	[HarmonyPatch(typeof(DefaultClanTierModel), "GetPartyLimitForTier")]
	internal class PartyPatch
	{
		// Token: 0x06000067 RID: 103 RVA: 0x000054D8 File Offset: 0x000036D8
		public static void Postfix(Clan clan, int clanTierToCheck, ref int __result)
		{
			try
			{
				bool spawnPartyMode = SubModule.Config.SpawnPartyMode;
				if (spawnPartyMode)
				{
					bool flag = clan == Clan.PlayerClan;
					if (flag)
					{
						int count = (from x in clan.WarPartyComponents
						where x.Party.Id.Contains("dead_horde")
						select x).ToList<WarPartyComponent>().Count;
						__result += count;
					}
				}
			}
			catch (Exception ex)
			{
				string str = "PartyPatch Error: ";
				string str2 = ex.Message.ToString();
				string str3 = "\n";
				Exception innerException = ex.InnerException;
				throw new Exception((str + str2 + str3 + ((innerException != null) ? innerException.ToString() : null) != null) ? ex.InnerException.Message.ToString() : "");
			}
		}
	}
}
