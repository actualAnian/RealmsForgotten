using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade
{
    public class FirstTreeTempleLocation : CampaignBehaviorBase
    {
        private Dictionary<Settlement, List<Location>> settlementCustomLocations = new Dictionary<Settlement, List<Location>>();
        private HashSet<string> customLocationIds = new HashSet<string>();

        private bool itemsDelivered = false;

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("itemsDelivered", ref itemsDelivered);
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            AddCustomLocationsToSettlements();

            foreach (var settlementEntry in settlementCustomLocations)
            {
                var settlement = settlementEntry.Key;
                var customLocations = settlementEntry.Value;

                foreach (var location in customLocations)
                {
                    var loc = location;
                    string menuOptionId = $"visit_{settlement.StringId}_{loc.StringId}";
                    TextObject menuText = new TextObject("Visit {LOCATION_NAME}");
                    menuText.SetTextVariable("LOCATION_NAME", loc.Name.ToString());

                    campaignGameStarter.AddGameMenuOption(
                        "town",
                        menuOptionId,
                        menuText.ToString(),
                        args => CustomLocationOptionCondition(args, settlement),
                        args => CustomLocationOptionConsequence(args, loc),
                        false,
                        -1,
                        false
                    );
                }
            }

            AddDialogs();
            AddItemDeliveryDialog();
        }


        private void AddCustomLocationsToSettlements()
        {
            var settlementsToAddLocations = new Dictionary<string, List<(string id, string name, string sceneName)>>
        {
            { "town_FirstTree", new List<(string id, string name, string sceneName)>
                {
                    ("firsttreetemple", "The First Tree Temple", "first_tree_temple_inside"),
                }
            },
        };

            foreach (var settlementEntry in settlementsToAddLocations)
            {
                var settlement = Settlement.Find(settlementEntry.Key);
                if (settlement != null)
                {
                    foreach (var locationEntry in settlementEntry.Value)
                    {
                        var (id, name, sceneName) = locationEntry;
                        AddCustomLocationToSettlement(settlement, id, name, sceneName);
                    }
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Settlement with key '{settlementEntry.Key}' not found."));
                }
            }
        }

        private void AddCustomLocationToSettlement(Settlement settlement, string locationId, string locationName, string sceneName)
        {
            if (settlement.LocationComplex != null)
            {
                if (settlement.LocationComplex.GetLocationWithId(locationId) != null)
                {
                    return;
                }

                Location location = new Location(
                    locationId,
                    new TextObject(locationName),
                    new TextObject(locationName),
                    1000,
                    false,
                    true,
                    "CanNever",
                    "CanAlways",
                    "CanNever",
                    "CanNever",
                    new string[] { sceneName, sceneName, sceneName, sceneName },
                    null
                );

                FieldInfo locationsField = typeof(LocationComplex).GetField("_locations", BindingFlags.NonPublic | BindingFlags.Instance);
                if (locationsField != null)
                {
                    var locationsDict = locationsField.GetValue(settlement.LocationComplex) as Dictionary<string, Location>;
                    if (locationsDict != null)
                    {
                        locationsDict.Add(locationId, location);
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Failed to retrieve locations dictionary for settlement '{settlement.Name}'."));
                    }
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Failed to access LocationComplex._locations via reflection."));
                }
                if (!settlementCustomLocations.ContainsKey(settlement))
                {
                    settlementCustomLocations[settlement] = new List<Location>();
                }
                settlementCustomLocations[settlement].Add(location);
                customLocationIds.Add(locationId);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage($"Settlement {settlement.Name} has no LocationComplex."));
            }
        }

        private bool CustomLocationOptionCondition(MenuCallbackArgs args, Settlement settlement)
        {
            Settlement currentSettlement = Settlement.CurrentSettlement;
            if (currentSettlement != settlement)
            {
                return false;
            }
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            return true;
        }

        private void CustomLocationOptionConsequence(MenuCallbackArgs args, Location location)
        {
            PlayerEncounter.RestartPlayerEncounter(MobileParty.MainParty.Party, Settlement.CurrentSettlement.Party);
            PlayerEncounter.EnterSettlement();
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(location);
        }

        private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
        {
            Settlement settlement = PlayerEncounter.LocationEncounter?.Settlement;
            if (settlement == null || !settlement.IsTown)
                return;

            Location currentLocation = CampaignMission.Current?.Location;
            if (currentLocation == null)
                return;

            if (IsCustomLocation(currentLocation))
            {
                AddPeopleToCustomLocation(settlement, currentLocation, unusedUsablePointCount);
                AddSpecificNpcToLocation(settlement, currentLocation); // Call the new method here
            }
        }

        private bool IsCustomLocation(Location location)
        {
            return customLocationIds.Contains(location.StringId);
        }

        private void AddPeopleToCustomLocation(Settlement settlement, Location location, Dictionary<string, int> unusedUsablePointCount)
        {
            CultureObject culture = settlement.Culture;

            unusedUsablePointCount.TryGetValue("npc_common", out var commonNpcCount);
            unusedUsablePointCount.TryGetValue("npc_common_limited", out var limitedNpcCount);

            int totalAvailablePoints = commonNpcCount + limitedNpcCount;
            if (totalAvailablePoints == 0)
                return;
            float maleSpawnRate = 0.3f;
            float femaleSpawnRate = 0.25f;

            int maleCount = (int)(totalAvailablePoints * maleSpawnRate);
            int femaleCount = (int)(totalAvailablePoints * femaleSpawnRate);

            if (maleCount > 0)
            {
                location.AddLocationCharacters(CreateTownsMan, culture, LocationCharacter.CharacterRelations.Neutral, maleCount);
            }
            if (femaleCount > 0)
            {
                location.AddLocationCharacters(CreateTownsWoman, culture, LocationCharacter.CharacterRelations.Neutral, femaleCount);
            }
            int childCount = (int)(totalAvailablePoints * 0.1f);
            if (childCount > 0)
            {
                location.AddLocationCharacters(CreateMaleChild, culture, LocationCharacter.CharacterRelations.Neutral, childCount / 2);
                location.AddLocationCharacters(CreateFemaleChild, culture, LocationCharacter.CharacterRelations.Neutral, childCount / 2);
            }
        }

        private void AddSpecificNpcToLocation(Settlement settlement, Location location)
        {
            // Check for the specific location ID
            if (location.StringId == "firsttreetemple")
            {
                // Find the specific NPC by ID
                CharacterObject npc = CharacterObject.Find("elvean_frist_tree_druid");
                if (npc != null)
                {
                    // Create a new LocationCharacter for the NPC
                    int minAge, maxAge;
                    Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(npc, out minAge, out maxAge, "");
                    AgentData agentData = new AgentData(new SimpleAgentOrigin(npc))
                        .Age(MBRandom.RandomInt(minAge, maxAge));

                    LocationCharacter locationCharacter = new LocationCharacter(
                        agentData,
                        SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors,
                        "npc_common",
                        true, // isTalkable
                        LocationCharacter.CharacterRelations.Neutral,
                        null,
                        true,  // isVisibleOnMap
                        false, // hasWeapon
                        null,
                        false, // canBeTeleportedTo
                        false, // canBeKilled
                        true   // isStatic
                    );

                    // Add the NPC to the location using AddCharacter
                    location.AddCharacter(locationCharacter);
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("First Tree Druid character not found."));
                }
            }
        }


        private void AddDialogs()
        {
            DialogFlow dialogTreeDruid = DialogFlow.CreateDialogFlow("start", 125)
    .NpcLine("Welcome to our humble temple. How may I assist you?")
    .Condition(() => IsTreeDruid(CharacterObject.OneToOneConversationCharacter))
    .BeginPlayerOptions()
        .PlayerOption("What is this place for?")
            .NpcLine("You are at the heart of the First Tree. As you walk through our town, you’ll notice all these trees are part of one—the First Tree—spreading its roots and branches. Here, we care for its offspring and prepare them for the world.")
        .PlayerLine("Interesting... How old is the tree?")
            .NpcLine("Our songs say it has stood here since our ancestors, the Elivor, first arrived in Aeurth. Its age is estimated to exceed two thousand years.")
        .PlayerLine("That’s a long time ago. What is your role here?")
            .NpcLine("I care for the temple, lead prayers, guide the devoted, receive their offerings, and manage temple resources. Everyone who visits departs with blessings.")
        .PlayerLine("Can I make a donation?")
            .NpcLine("Of course. The tree treasures gifts, but what it loves most is the water from hidden springs.")
            .PlayerLine("Water from hidden springs? What is that?")
                .NpcLine("These are ancient springs with waters of unique essence, providing extraordinary nourishment. They hold great power.")
                .PlayerLine("Where can I find them?")
                    .NpcLine("Anyone can learn to locate them, though some have a natural talent. There are many mysteries in our world, forgotten places where mystical powers still flourish.")
                    .PlayerLine("Tell me more.")
                        .NpcLine("The Xilantlacay people have extensive knowledge of these springs. They were once the guardians of such sites, but the Pharun of Athas drove them from their lands to exploit these rare waters. If true, the springs are hidden somewhere in the vast desert of Athas.")
                        .PlayerLine("But Athas is just an endless desert, barren and lifeless.")
                            .NpcLine("Indeed, but it is also ancient and full of secrets. Some say it holds the rarest springs of all. If you bring such a gift, you will earn the tree’s greatest blessings.")
                            .Consequence(() =>
                            {
                                // Implement logic to give the player a beginner's guide
                                InformationManager.DisplayMessage(new InformationMessage("Search for the rare springs."));
                            })
                            .BeginPlayerOptions()
                                .PlayerOption("What else can I give?")
                                    .NpcLine("Anything you offer sincerely from your heart. The value lies in your intention, not the gift itself.")
                                .PlayerLine("I want to donate something truly valuable.")
                                    .NpcLine("Our temple welcomes rare items such as precious minerals, exotic furs, or gemstones. These will sustain the temple, and the blessings you receive will reflect the worth of your offering.")
                                    .Consequence(() =>
                                    {
                                        InformationManager.DisplayMessage(new InformationMessage("Go into the world and search for rare items to be delivered to the temple."));
                                    })
                                    .CloseDialog()
                            .EndPlayerOptions() // For "What else can I give?"
                            .EndPlayerOptions();

            Campaign.Current.ConversationManager.AddDialogFlow(dialogTreeDruid);
        }
        private void AddItemDeliveryDialog()
        {
            // Separate dialog flow that triggers only if the player has the item
            DialogFlow itemDeliveryDialog = DialogFlow.CreateDialogFlow("start", 128)
                .NpcLine("Ah, I sense you carry something special with you.")
                .Condition(() => IsTreeDruid(CharacterObject.OneToOneConversationCharacter) && PlayerHasItem("spring_water"))
                .BeginPlayerOptions()
                    .PlayerOption("Yes, I have the water. Where should i place it?")
                        .NpcLine("Just give it to me, i will administrate it. You will be blessed.")
                        .BeginPlayerOptions()
                            .PlayerOption("Thank you.")
                                .Consequence(() =>
                                {
                                    RemoveItemFromPlayer("spring_water");

                                    int goldReward = 500; // Adjust the amount as needed
                                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, goldReward);

                                    InformationManager.DisplayMessage(new InformationMessage($"You have delivered the water to the temple and received {goldReward} gold."));
                                })
                                .NpcLine("Thank you! Here is a reward for your generosity.")
                                .CloseDialog()
                           .EndPlayerOptions()
                    .PlayerOption("No, I don't have anything special.")
                        .NpcLine("My mistake. Let me know if you find anything of interest.")
                        .CloseDialog()
                .EndPlayerOptions();

            Campaign.Current.ConversationManager.AddDialogFlow(itemDeliveryDialog);
        }
        private bool IsTreeDruid(CharacterObject character)
        {
            return character != null && character.StringId == "elvean_frist_tree_druid";
        }


        private LocationCharacter CreateTownsMan(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townsman = culture.Townsman;
            Monster monster = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townsman.Race, "_settlement");
            string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(monster, townsman.IsFemale, "_villager");

            int minimumAge, maximumAge;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out minimumAge, out maximumAge);

            return new LocationCharacter(
                new AgentData(new SimpleAgentOrigin(townsman)).Monster(monster).Age(MBRandom.RandomInt(minimumAge, maximumAge)),
                SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors,
                "npc_common",
                false,
                relation,
                actionSet,
                true
            );
        }

        private LocationCharacter CreateTownsWoman(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townswoman = culture.Townswoman;
            Monster monster = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townswoman.Race, "_settlement_slow");
            string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(monster, townswoman.IsFemale, "_villager_2");

            int minimumAge, maximumAge;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out minimumAge, out maximumAge);

            return new LocationCharacter(
                new AgentData(new SimpleAgentOrigin(townswoman)).Monster(monster).Age(MBRandom.RandomInt(minimumAge, maximumAge)),
                SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors,
                "npc_common",
                false,
                relation,
                actionSet,
                true
            );
        }

        private LocationCharacter CreateMaleChild(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject child = culture.TownsmanChild;
            Monster monster = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(child.Race, "_child");
            string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(monster, child.IsFemale, "_child");

            int minimumAge, maximumAge;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(child, out minimumAge, out maximumAge, "Child");

            return new LocationCharacter(
                new AgentData(new SimpleAgentOrigin(child)).Monster(monster).Age(MBRandom.RandomInt(minimumAge, maximumAge)),
                SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors,
                "npc_common_limited",
                false,
                relation,
                actionSet,
                true
            );
        }

        private LocationCharacter CreateFemaleChild(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject child = culture.TownswomanChild;
            Monster monster = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(child.Race, "_child");
            string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(monster, child.IsFemale, "_child");

            int minimumAge, maximumAge;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(child, out minimumAge, out maximumAge, "Child");

            return new LocationCharacter(
                new AgentData(new SimpleAgentOrigin(child)).Monster(monster).Age(MBRandom.RandomInt(minimumAge, maximumAge)),
                SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors,
                "npc_common_limited",
                false,
                relation,
                actionSet,
                true
            );
        }
        private bool PlayerHasRequiredItems()
        {
            return PlayerHasItem("spring_water");
        }

        private bool PlayerHasItem(string itemId)
        {
            ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error: Item '{itemId}' not found."));
                return false;
            }
            int itemCount = MobileParty.MainParty.ItemRoster.GetItemNumber(item);
            return itemCount > 0;
        }

        private void OnItemsDelivered()
        {
            RemoveItemFromPlayer("spring_water");
            itemsDelivered = true;
            GiveRewardToPlayer();
            InformationManager.DisplayMessage(new InformationMessage("You have delivered the items to the Druid."));
        }

        private void RemoveItemFromPlayer(string itemId)
        {
            ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item != null)
            {
                MobileParty.MainParty.ItemRoster.AddToCounts(item, -1);
            }
        }

        private void GiveRewardToPlayer()
        {
            int goldReward = 1000;
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, goldReward);
            Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(Game.Current.ObjectManager.GetObject<ItemObject>("elixir_rfmisc60"), 1);

            // Display a message confirming the reward
            InformationManager.DisplayMessage(new InformationMessage("You have received the Elixir of the Hidden Springs as a blessing! The elixir can heal your health whenever you need it."));
            InformationManager.DisplayMessage(new InformationMessage($"You have received {goldReward} gold as a reward."));
        }
    }
}