using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade
{
    public class EquipItemsAfterBattleBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {
            base.AfterStart();
            Mission.Current.OnItemPickUp += OnItemPickup; // Correct event subscription
        }

        private void OnItemPickup(Agent agent, SpawnedItemEntity itemEntity)
        {
            if (agent == Agent.Main && itemEntity != null)
            {
                var itemObject = itemEntity.WeaponCopy.Item; // Correctly access the ItemObject
                if (itemObject != null)
                {
                    EquipmentIndex slot = FindEmptySlotForItem(itemObject);
                    if (slot != EquipmentIndex.None)
                    {
                        Agent.Main.Character.Equipment[slot] = new EquipmentElement(itemObject);
                        InformationManager.DisplayMessage(new InformationMessage($"Equipped {itemObject.Name} to player."));
                    }
                }
            }
        }

        private EquipmentIndex FindEmptySlotForItem(ItemObject item)
        {
            // Assume we are equipping either weapons or shields
            if (item.Type == ItemObject.ItemTypeEnum.OneHandedWeapon ||
                item.Type == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
                item.Type == ItemObject.ItemTypeEnum.Polearm ||
                item.Type == ItemObject.ItemTypeEnum.Bow ||
                item.Type == ItemObject.ItemTypeEnum.Crossbow ||
                item.Type == ItemObject.ItemTypeEnum.Thrown ||
                item.Type == ItemObject.ItemTypeEnum.Shield)
            {
                EquipmentIndex[] weaponSlots = { EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3 };
                foreach (var slot in weaponSlots)
                {
                    if (Agent.Main.Character.Equipment[slot].IsEmpty)
                        return slot;
                }
            }
            return EquipmentIndex.None;
        }
    }
}