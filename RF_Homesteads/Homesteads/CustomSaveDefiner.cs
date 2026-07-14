using System.Collections.Generic;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.SaveSystem;

namespace Homesteads;

public class CustomSaveDefiner : SaveableTypeDefiner
{
	public CustomSaveDefiner()
		: base(321601531)
	{
	}

	protected override void DefineClassTypes()
	{
		AddClassDefinition(typeof(Homestead), 1);
		AddClassDefinition(typeof(HomesteadScene), 2);
		AddClassDefinition(typeof(HomesteadSceneSavedEntity), 3);
		AddClassDefinition(typeof(HomesteadScenePlaceable), 4);
		AddClassDefinition(typeof(HomesteadScenePlaceableProducedItem), 5);
		AddClassDefinition(typeof(HomesteadRecruiterComponent), 6);
		AddClassDefinition(typeof(HomesteadPatrolPartyComponent), 8);
		AddClassDefinition(typeof(HomesteadNotableFavorQuest), 9);
		AddClassDefinition(typeof(HomesteadPackageDeliveryQuest), 10);
		AddClassDefinition(typeof(HomesteadRaiderPartyComponent), 11);
		AddClassDefinition(typeof(HomesteadRaidEventQuest), 12);
		AddClassDefinition(typeof(HomesteadNotableApparelQuest), 13);
		AddClassDefinition(typeof(HomesteadBuildingRequestQuest), 14);
		AddClassDefinition(typeof(HomesteadApprenticeQuest), 15);
		AddClassDefinition(typeof(HomesteadArmsMasterRecruitQuest), 16);
		AddClassDefinition(typeof(HomesteadMasterSmithRecruitQuest), 17);
		AddClassDefinition(typeof(HomesteadStableMasterRecruitQuest), 18);
		AddClassDefinition(typeof(HomesteadTrackStats), 19);
		AddClassDefinition(typeof(HomesteadHeadmanTrustQuest), 20);
		AddClassDefinition(typeof(HomesteadAngryVillagerPartyComponent), 21);
		AddClassDefinition(typeof(HomesteadAngryVillagersQuest), 22);
		AddClassDefinition(typeof(HomesteadLandPatentQuest), 23);
		AddClassDefinition(typeof(HomesteadSettlementCharterQuest), 24);
		AddClassDefinition(typeof(HomesteadBuiltSettlement), 25);
		AddClassDefinition(typeof(SettlementSmithUpgradeRecord), 26);
	}

	protected override void DefineEnumTypes()
	{
		AddEnumDefinition(typeof(HomesteadRecruiterState), 7);
	}

	protected override void DefineContainerDefinitions()
	{
		ConstructContainerDefinition(typeof(Dictionary<MobileParty, Homestead>));
		ConstructContainerDefinition(typeof(Dictionary<string, int>));
		ConstructContainerDefinition(typeof(Dictionary<string, string>));
		ConstructContainerDefinition(typeof(List<HomesteadSceneSavedEntity>));
		ConstructContainerDefinition(typeof(List<HomesteadBuiltSettlement>));
		ConstructContainerDefinition(typeof(List<HomesteadScenePlaceableProducedItem>));
		ConstructContainerDefinition(typeof(List<Hero>));
		ConstructContainerDefinition(typeof(List<int>));
		ConstructContainerDefinition(typeof(List<string>));
		ConstructContainerDefinition(typeof(List<float>));
		ConstructContainerDefinition(typeof(Dictionary<string, HomesteadTrackStats>));
		ConstructContainerDefinition(typeof(Dictionary<string, SettlementSmithUpgradeRecord>));
	}
}
