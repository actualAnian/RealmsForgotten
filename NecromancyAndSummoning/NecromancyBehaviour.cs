using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace RealmsForgotten.NecromancyAndSummoning
{
	// Token: 0x02000003 RID: 3
	internal class NecromancyBehaviour : CampaignBehaviorBase
	{
		// Token: 0x06000011 RID: 17 RVA: 0x000028F0 File Offset: 0x00000AF0
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
				throw new Exception((str + str2 + str3 + ((innerException != null) ? innerException.ToString() : null) != null) ? ex.InnerException.Message.ToString() : "");
			}
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002A04 File Offset: 0x00000C04
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
