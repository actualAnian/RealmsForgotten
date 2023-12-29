using System;
using System.Collections.Generic;
using System.Reflection;
using RealmsForgotten.NecromancyAndSummoning.CustomClass;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.NecromancyAndSummoning
{
    internal class NecromancyAndSummoningLogic : MissionLogic
	{
        public override void AfterStart()
		{
			NecroSummon.ResetCount();
		
		}

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

		public static bool IsInBattle()
		{
			return Mission.Current.Mode == MissionMode.Battle || Mission.Current.Mode == MissionMode.Deployment;
		}

		private static bool battleVictory;
	}
}
