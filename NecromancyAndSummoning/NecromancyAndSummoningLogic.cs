using System;
using System.Collections.Generic;
using System.Reflection;
using NecromancyAndSummoning.CustomClass;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NecromancyAndSummoning
{
	// Token: 0x02000004 RID: 4
	internal class NecromancyAndSummoningLogic : MissionLogic
	{
		// Token: 0x06000014 RID: 20 RVA: 0x00002A10 File Offset: 0x00000C10
		public override void AfterStart()
		{
			NecroSummon.ResetCount();
		
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002A1C File Offset: 0x00000C1C
		public override void OnMissionResultReady(MissionResult missionResult)
		{
			bool playerVictory = missionResult.PlayerVictory;
			if (playerVictory)
			{
				NecromancyAndSummoningLogic.battleVictory = true;
			}
			else
			{
				NecromancyAndSummoningLogic.battleVictory = false;
			}
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002A48 File Offset: 0x00000C48
		public override void OnEndMissionInternal()
		{
			bool flag = NecromancyAndSummoningLogic.battleVictory;
			if (flag)
			{
				List<SummonKillRecord> totalSummonKill = NecroSummon.GetTotalSummonKill();
				bool flag2 = totalSummonKill.Count > 0;
				if (flag2)
				{
					NecroSummon.DistrubuteExperience(totalSummonKill);
				}
				bool spawnPartyMode = SubModule.Config.SpawnPartyMode;
				if (spawnPartyMode)
				{
					try
					{
					}
					catch (Exception ex)
					{
						throw new Exception(ex.Message + ex.InnerException.Message);
					}
				}
				bool enableBuildTroopFromPart = SubModule.Config.EnableBuildTroopFromPart;
				if (enableBuildTroopFromPart)
				{
					BuildingTroopFromParts.GetBodyPart();
				}
			}
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002ADC File Offset: 0x00000CDC
		public override void OnAgentPanicked(Agent affectedAgent)
		{
			bool flag = affectedAgent != null;
			if (flag)
			{
				bool flag2 = affectedAgent.Character != null;
				if (flag2)
				{
					bool flag3 = NecroSummon.IsReanimatedTroop(affectedAgent.Character);
					if (flag3)
					{
						affectedAgent.ChangeMorale(100f);
						CommonAIComponent component = affectedAgent.GetComponent<CommonAIComponent>();
						PropertyInfo propertyInfo = (PropertyInfo)Util.GetInstanceProperty<CommonAIComponent>(component, "IsPanicked");
						propertyInfo.SetValue(component, false);
					}
				}
			}
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002B48 File Offset: 0x00000D48
		public override void OnAgentFleeing(Agent affectedAgent)
		{
			bool flag = affectedAgent != null;
			if (flag)
			{
				bool flag2 = affectedAgent.Character != null;
				if (flag2)
				{
					bool flag3 = NecroSummon.IsReanimatedTroop(affectedAgent.Character);
					if (flag3)
					{
						affectedAgent.StopRetreating();
						affectedAgent.ChangeMorale(100f);
					}
				}
			}
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002B94 File Offset: 0x00000D94
		public static bool IsInBattle()
		{
			return Mission.Current.Mode == MissionMode.Battle || Mission.Current.Mode == MissionMode.Deployment;
		}

		// Token: 0x04000002 RID: 2
		private static bool battleVictory;
	}
}
