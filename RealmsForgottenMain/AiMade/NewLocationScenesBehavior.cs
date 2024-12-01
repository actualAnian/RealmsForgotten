using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade
{
    public class ADODCustomLocationsBehavior : CampaignBehaviorBase
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
            { "town_EM1", new List<(string id, string name, string sceneName)>
                {
                    ("arcanelibrary", "the Arcane Library", "arcane_keep_interior"),
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
            if (location.StringId == "arcanelibrary")
            {
                // Find the specific NPC by ID
                CharacterObject npc = CharacterObject.Find("arcane_library_maester");
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
                    InformationManager.DisplayMessage(new InformationMessage("Arcane Library Maester character not found."));
                }
            }
        }


        private void AddDialogs()
        {
            DialogFlow dialogArcaneLibrarian = DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Welcome to the Arcane Library. How may I assist you?")
                .Condition(() => IsArcaneLibrarian(CharacterObject.OneToOneConversationCharacter))
                .BeginPlayerOptions()
                  // **First Group of Options**
                  .PlayerOption("I am looking for knowledge.")
    .NpcLine("What kind of knowledge are you seeking?")
    .BeginPlayerOptions()
        .PlayerOption("Tell me about spells.")
            .NpcLine("Spells are powerful but dangerous. For thousands of years, magic has been gone from the world, only found stored inside ancient relics like wands, elixirs, and ancient equipment. We know very little about the origin of such items, but our order has managed to manipulate them and use their stored power.")
            .BeginPlayerOptions()
                .PlayerOption("Interesting. Can anyone learn?")
                    .NpcLine("Yes, anyone can learn... but some have more talent than others.")
                    .BeginPlayerOptions()
                        .PlayerOption("How can I learn?")
                            .NpcLine("If you are a complete novice or already started your path you can always find knowlegde inside books and scrolls. Also, the world is full of magic itens and exotic ingredients. Things like a cyclops eye or monster horn can be donated to our library in exchange of knowledge in spells.")
                            .PlayerOption("interesting... and If i need to start from zero?")
                            .NpcLine("Start buy reading an arcane scroll, it will open your third eye to the mistical world.")
                            .Consequence(() =>
                            {
                                // Implement logic to give the player a beginner's guide
                                InformationManager.DisplayMessage(new InformationMessage("Return to the Town menu and click in the Read books option."));
                            })
                            .BeginPlayerOptions()
                                .PlayerOption("Actually, I'm interested in more advanced knowledge..")
                                    .NpcLine("Advanced knowledge requires experience. Study more scrolls or complete some tasks for us, in exchange for advanced spells.")
                                     .PlayerOption("What kind of tasks?")
                                     .NpcLine("Our library always welcome rare items, like a horn of a magical creature for example. The world is full of them, bring such a thing and you will be rewarded.")
                                    .Consequence(() =>
                                    {
                                       InformationManager.DisplayMessage(new InformationMessage("Go into the world and search for a horn of a mistical creature to be delivered to the library."));
                                    })
                                    .CloseDialog()
                            .EndPlayerOptions()
                        .EndPlayerOptions()
                    .EndPlayerOptions()
                .PlayerOption("On second thought, maybe not now.")
                    .NpcLine("Wisdom comes in knowing your limits.")
                    .CloseDialog()
            .EndPlayerOptions()
        .PlayerOption("Actually, never mind.")
            .NpcLine("Very well. Let me know if you need anything else.")
            .CloseDialog()
    .EndPlayerOptions();

            Campaign.Current.ConversationManager.AddDialogFlow(dialogArcaneLibrarian);
        }
        private void AddItemDeliveryDialog()
        {
            // Separate dialog flow that triggers only if the player has the item
            DialogFlow itemDeliveryDialog = DialogFlow.CreateDialogFlow("start", 128)
                .NpcLine("Ah, I sense you carry something special with you.")
                .Condition(() => IsArcaneLibrarian(CharacterObject.OneToOneConversationCharacter) && PlayerHasItem("cyclops_eye"))
                .BeginPlayerOptions()
                    .PlayerOption("Yes, I have the horn. Are you still interested?")
                        .NpcLine("Indeed! Such a rare artifact would be invaluable to our research. Would you consider parting with it?")
                        .BeginPlayerOptions()
                            .PlayerOption("Yes, you can have it.")
                                .Consequence(() =>
                                {
                                    RemoveItemFromPlayer("cyclops_eye");

                                    int goldReward = 500; // Adjust the amount as needed
                                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, goldReward);

                                   InformationManager.DisplayMessage(new InformationMessage($"You have delivered the cyclops eye to the Arcane Librarian and received {goldReward} gold."));
                                })
                                .NpcLine("Thank you! Here is a reward for your generosity.")
                                .CloseDialog()
                            .PlayerOption("No, I think I'll keep it.")
                                .NpcLine("Very well. If you change your mind, please let me know.")
                                .CloseDialog()
                        .EndPlayerOptions()
                    .PlayerOption("No, I don't have anything special.")
                        .NpcLine("My mistake. Let me know if you find anything of interest.")
                        .CloseDialog()
                .EndPlayerOptions();

            Campaign.Current.ConversationManager.AddDialogFlow(itemDeliveryDialog);
        }
        private bool IsArcaneLibrarian(CharacterObject character)
        {
            return character != null && character.StringId == "arcane_library_maester";
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
            return PlayerHasItem("cyclops_eye") && PlayerHasItem("monster_horn");
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
            RemoveItemFromPlayer("cyclops_eye");
            RemoveItemFromPlayer("monster_horn");
            itemsDelivered = true;
            GiveRewardToPlayer();
            InformationManager.DisplayMessage(new InformationMessage("You have delivered the items to the Arcane Librarian."));
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
            InformationManager.DisplayMessage(new InformationMessage($"You have received {goldReward} gold as a reward."));
        }
    }
}

