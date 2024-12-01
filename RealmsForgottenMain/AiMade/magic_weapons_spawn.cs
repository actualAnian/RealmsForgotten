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

        // Declare a public static event for item pickup
        public static event Action<Agent, SpawnedItemEntity> ItemPickedUp;

        public FindMagicItemsMissionBehavior()
        {
        }

        public override void AfterStart()
        {
            base.AfterStart();
            InformationManager.DisplayMessage(new InformationMessage($"You are on: {Mission.Current.SceneName}"));

            // Attach the OnItemPickup handler
            Mission.Current.OnItemPickUp += OnItemPickup;

            // Spawn items immediately
            CheckAndSpawnItems(Mission.Current.SceneName);
        }

        private void CheckAndSpawnItems(string sceneName)
        {
            switch (sceneName)
            {
                case "anorite_monastery":
                    if (!fireSwordSpawned)
                    {
                        SpawnItem("rfmisc_western_2hsword_t3_fire", new Vec3(484.57f, 873f, 94.36f), new Vec3(0.00f, -180.17f, 17.42f));
                        fireSwordSpawned = true;
                    }
                    break;
                default:
                    InformationManager.DisplayMessage(new InformationMessage($"No magic items to spawn in scene '{sceneName}'"));
                    break;
            }
        }

        private void SpawnItem(string itemId, Vec3 position, Vec3 rotation)
        {
            ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item != null)
            {
                MissionWeapon missionWeapon = new MissionWeapon(item, new ItemModifier(), Banner.CreateOneColoredEmptyBanner(1));
                MatrixFrame frame = new MatrixFrame(Mat3.CreateMat3WithForward(rotation), position);
                Mission.SpawnWeaponWithNewEntityAux(missionWeapon, Mission.WeaponSpawnFlags.WithStaticPhysics, frame, 0, null, false);
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
                string itemName = item.WeaponCopy.Item.Name.ToString();  // Fetching the item's name

                if (item.WeaponCopy.Item.StringId == "rfmisc_western_2hsword_t3_fire")
                {
                    // Add the item to the player's inventory
                    AddItemToPlayerInventory(item.WeaponCopy.Item);

                    InformationManager.DisplayMessage(new InformationMessage($"You found a {itemName}!"));
                }
            }
        }

        private void AddItemToPlayerInventory(ItemObject item)
        {
            var mainParty = MobileParty.MainParty.ItemRoster;
            mainParty.AddToCounts(item, 1);
            InformationManager.DisplayMessage(new InformationMessage($"The {item.Name} has been added to your inventory."));
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    }
}


