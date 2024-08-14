using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade
{
    public class FindMagicItemsMissionBehavior : MissionBehavior
    {
        private bool fireSwordSpawned = false;
        private bool misticPolearmSpawned = false;

        // Declare a public static event for item pickup
        public static event Action<Agent, SpawnedItemEntity> ItemPickedUp;

        public FindMagicItemsMissionBehavior()
        {
        }

        public override void AfterStart()
        {
            base.AfterStart();
            InformationManager.DisplayMessage(new InformationMessage($"You are on: {Mission.Current.SceneName}"));
            CheckAndSpawnItems(Mission.Current.SceneName);
        }

        private void CheckAndSpawnItems(string sceneName)
        {
            switch (sceneName)
            {
                case "ice_tower":
                    if (!fireSwordSpawned)
                    {
                        SpawnItem("rfmisc_western_2hsword_t4_fire", new Vec3(95.54f, 150.13f, 49.42f, -1f), new Vec3(0.34f, -91.21f, -90.24f, -1f));
                        fireSwordSpawned = true;
                    }
                    break;
                case "ice_tower_inside":
                    if (!misticPolearmSpawned)
                    {
                        SpawnItem("rfmisc_mistic_polearm", new Vec3(122.42f, 192.97f, 4.03f, -1f), new Vec3(0.00f, 0.00f, -30.00f, -1f));
                        misticPolearmSpawned = true;
                    }
                    break;
                default:
                    InformationManager.DisplayMessage(new InformationMessage($"No magic items to spawn in scene '{sceneName}'"));
                    break;
            }
        }

        private void SpawnItem(string itemId, Vec3 position, Vec3 rotation)
        {
            var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item != null)
            {
                var missionWeapon = new MissionWeapon(item, new ItemModifier(), Banner.CreateOneColoredEmptyBanner(1));
                Mission.SpawnWeaponWithNewEntityAux(missionWeapon, Mission.WeaponSpawnFlags.WithStaticPhysics, new MatrixFrame(Mat3.CreateMat3WithForward(rotation), position), 0, null, false);
                Mission.Current.OnItemPickUp += OnItemPickup;

                InformationManager.DisplayMessage(new InformationMessage($"Successfully spawned item '{itemId}' at {position}."));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error: The magic item with ID '{itemId}' couldn't be found."));
            }
        }

        private void OnItemPickup(Agent agent, SpawnedItemEntity item)
        {
            // Invoke the public static event
            ItemPickedUp?.Invoke(agent, item);

            if (agent.IsMainAgent)
            {
                string sceneID = "";  // Default scene ID
                string itemName = item.WeaponCopy.Item.Name.ToString();  // Fetching the item's name

                // Determine the appropriate scene based on the item ID
                if (item.WeaponCopy.Item.StringId == "rfmisc_western_2hsword_t4_fire")
                {
                    sceneID = "scn_mage_staff";
                }
                else if (item.WeaponCopy.Item.StringId == "rfmisc_mistic_polearm")
                {
                    sceneID = "scn_mage_staff";
                }

                // Create and show the notification with the specific scene
                var notification = new MagicItemFoundSceneNotification(itemName, sceneID, () => AddItemToPlayerInventory(item.WeaponCopy.Item));
                InformationManager.DisplayMessage(new InformationMessage($"You found a {itemName}!"));
                MBInformationManager.ShowSceneNotification(notification);
            }
        }

        private void AddItemToPlayerInventory(ItemObject item)
        {
            var mainParty = MobileParty.MainParty.ItemRoster;
            mainParty.AddToCounts(item, 1);
            InformationManager.DisplayMessage(new InformationMessage($"The {item.Name} has been added to the inventory."));
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    }
}

