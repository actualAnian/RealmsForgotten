using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Homesteads;

public class HomesteadBuiltSettlement
{
	[SaveableField(1)]
	public string StringId;

	[SaveableField(2)]
	public string Xml;

	[SaveableField(3)]
	public string DisplayName;

	[SaveableField(4)]
	public string OwnerClanId;

	[SaveableField(5)]
	public string DefaultBuildingId;

	[SaveableField(6)]
	public float Prosperity = -1f;

	[SaveableField(7)]
	public float Militia;

	[SaveableField(8)]
	public float Hearth;

	[SaveableField(9)]
	public float FoodStocks;

	[SaveableField(10)]
	public bool GarrisonAutoRecruitDisabled;

	[SaveableField(11)]
	public int GarrisonWagePaymentLimit;

	[SaveableField(12)]
	public bool InRebelliousState;

	[SaveableField(13)]
	public float SettlementHitPoints;

	[SaveableField(14)]
	public List<float> WallSectionHealth;

	[SaveableField(15)]
	public int BribePaid;

	[SaveableField(16)]
	public bool HasVisited;

	[SaveableField(17)]
	public int VillageTradeTax;

	[SaveableField(18)]
	public List<string> StashItems;

	[SaveableField(19)]
	public List<string> BuildingState;

	[SaveableField(20)]
	public List<string> BuildingQueue;
}
