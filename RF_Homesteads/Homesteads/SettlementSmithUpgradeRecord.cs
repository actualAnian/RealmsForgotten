using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace Homesteads;

public class SettlementSmithUpgradeRecord
{
	[SaveableField(1)]
	public string? ItemId;

	[SaveableField(2)]
	public string? ModifierId;

	[SaveableField(3)]
	public bool IsCivilian;

	[SaveableField(4)]
	public int Slot;

	[SaveableField(5)]
	public float ReadyDay;

	[SaveableField(6)]
	public bool ReadyNotified;

	[SaveableField(7)]
	public ItemObject? Item;

	[SaveableField(8)]
	public bool FromInventory;
}
