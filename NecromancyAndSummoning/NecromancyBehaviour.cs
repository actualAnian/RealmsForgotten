using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace NecromancyAndSummoning
{
	internal class NecromancyBehaviour : CampaignBehaviorBase
	{
		public override void RegisterEvents()
		{
			try
			{
				bool battleSimulationMode = SubModule.Config.BattleSimulationMode;
				if (battleSimulationMode)
				{
					CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(NecroSummon.BattleSimulationReanimation));
				}
				bool enableBuildTroopFromPart = SubModule.Config.EnableBuildTroopFromPart;
				if (enableBuildTroopFromPart)
				{
					CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(BuildingTroopFromParts.BuildTroopMenu));
				}
				bool enableRaiseCrimeRating = SubModule.Config.EnableRaiseCrimeRating;
				if (enableRaiseCrimeRating)
				{
					CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(NecroSummon.RaiseCrimeRating));
				}
				bool spawnPartyMode = SubModule.Config.SpawnPartyMode;
				if (spawnPartyMode)
				{
					CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(NecroSummon.FoodSupply));
				}
			}
			catch (Exception ex)
			{
				string str = "RF Error: ";
				string str2 = ex.Message.ToString();
				string str3 = "\n";
				Exception innerException = ex.InnerException;
				throw new Exception(str + str2 + str3 + (innerException != null ? innerException.ToString() : ""));
			}
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002A04 File Offset: 0x00000C04
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
