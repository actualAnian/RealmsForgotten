using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade
{
    public class FindUlvorShieldMissionBehavior : MissionBehavior
    {
       private bool fireJavelinSpawned = false;
        private CharacterObject characterID;


        // Declare a public static event for item pickup
        public static event Action<Agent, SpawnedItemEntity> ItemPickedUp;

        public FindUlvorShieldMissionBehavior()
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
               case "allkhuur_temple_inside":
                    if (!fireJavelinSpawned)
                    {
                        SpawnItem("rfmisc_eastern_javelin_3_t4", new Vec3(138.62f, 161.10f, 23.00f, -1f), new Vec3(0.00f, 0.00f, 177.79f, -1f));
                        fireJavelinSpawned = true;
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

            if (agent.IsMainAgent && item.WeaponCopy.Item.StringId == "rfmisc_eastern_javelin_3_t4")
            {
                // Fetch the CharacterObjects by their identifiers
                CharacterObject character1 = CharacterObject.FindFirst(character => character.StringId == "cs_devils_bandits_chief");
                CharacterObject character2 = CharacterObject.FindFirst(character => character.StringId == "cs_devils_bandits_boss");

                if (character1 != null && character2 != null)
                {
                    // Create and display the scene notification
                    var notificationItem = new MeetingEvilLordSceneNotificationItem(character1, character2);
                    MBInformationManager.ShowSceneNotification(notificationItem);

                    // Optional: Display a message or handle additional logic
                    string itemName = "Fire Javelin";
                    InformationManager.DisplayMessage(new InformationMessage($"You found a {itemName} and something ominous is about to happen!"));
                }
                else
                {
                    // Handle the case where one or both characters could not be found
                    InformationManager.DisplayMessage(new InformationMessage("Could not find one or more specified characters for the scene."));
                }
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
