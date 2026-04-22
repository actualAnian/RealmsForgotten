using System;
using System.Collections.Generic;
using System.Reflection;
using NecromancyAndSummoning.CustomClass;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NecromancyAndSummoning
{
    internal class NecromancyAndSummoningLogic : MissionLogic
    {
        public override void AfterStart()
		{
			NecroSummon.ResetCount();
		}

		public override void OnMissionResultReady(MissionResult missionResult)
		{
			if (missionResult.PlayerVictory)
			{
                battleVictory = true;
			}
			else
			{
                battleVictory = false;
			}
		}
        public override void OnEndMissionInternal()
		{
			if (battleVictory)
			{
				List<SummonKillRecord> totalSummonKill = NecroSummon.GetTotalSummonKill();
				if (totalSummonKill.Count > 0)
					NecroSummon.DistrubuteExperience(totalSummonKill);
				if (SubModule.Config.EnableBuildTroopFromPart)
					BuildingTroopFromParts.GetBodyPart();
			}
        }
		public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
		{
			if (IsInBattle())
			{
				ItemObject wieldedItem = NecroSummon.GetWieldedItem(attacker);
				if (!NecroSummon.IsAgentOverLimit())
				{
					if (wieldedItem != null && (
						SubModule.Config.EnablePlayerSummon && attacker == Agent.Main)
						|| (SubModule.Config.EnableTroopSummon && attacker != Agent.Main))
						NecroSummon.Summoning(attacker, collisionData.CollisionGlobalPosition);
				}
			}
		}
        public override void OnAgentPanicked(Agent affectedAgent)
		{
			if (affectedAgent != null)
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
			if (affectedAgent != null)
			{
				if (affectedAgent.Character != null)
				{
					if (NecroSummon.IsReanimatedTroop(affectedAgent.Character))
					{
						affectedAgent.StopRetreating();
						affectedAgent.ChangeMorale(100f);
					}
				}
			}
		}

		public static bool IsInBattle()
		{
			return !Mission.Current.IsFriendlyMission;
		}

		private static bool battleVictory;
	}
}
