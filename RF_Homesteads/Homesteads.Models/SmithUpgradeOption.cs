using TaleWorlds.Core;

namespace Homesteads.Models;

public class SmithUpgradeOption
{
	public bool IsInventory;

	public bool IsCivilian;

	public EquipmentIndex Slot;

	public ItemObject Item;

	public EquipmentElement Current;

	public ItemModifier Target;

	public int Cost;
}
