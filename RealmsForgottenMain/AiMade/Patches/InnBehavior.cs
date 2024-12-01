using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using RealmsForgotten.AiMade.Patches.ADODVillageInnsHelper;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;

namespace RealmsForgotten.AiMade.Patches
{
    internal class ADODInnBehavior : CampaignBehaviorBase
    {
        public Action<Village> OnInnEntered;
        public Action<Village> OnInnLeft;

        public static Location _inn = new Location("village_inn", new TextObject("{=ADOD_Inn_GameMenu_Inn}Inn"), new TextObject("{=ADOD_Inn_GameMenu_Inn}Inn"), 30, true, false, "CanAlways", "CanAlways", "CanAlways", "CanAlways", new string[]
        {
            "VillageLargeTavern", "VillageLargeTavern", "VillageLargeTavern", "VillageLargeTavern"
        }, null);

        public bool IsInInn = false;
        public bool _isInnInitialized = false;

        public override void RegisterEvents()
        {
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddGameMenus);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEnter);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, OnAfterSettlementEnter);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_isInnInitialized", ref _isInnInitialized);
            dataStore.SyncData("IsInInn", ref IsInInn);

            // Sync _inn if needed
            // Example (assuming Location is serializable or can be converted to a serializable form):
            // dataStore.SyncData("_inn", ref _inn);
        }

        void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            if (IsInInn)
            {
                IsInInn = false;
                GameMenu.SwitchToMenu("village");
            }
        }

        void OnAfterSettlementEnter(MobileParty party, Settlement settlement, Hero hero) { }

        void OnSettlementEnter(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != MobileParty.MainParty || !settlement.IsVillage)
                return;

            FieldInfo field = LocationComplex.Current.GetType().GetField("_locations", BindingFlags.Instance | BindingFlags.NonPublic);
            Dictionary<string, Location> dictionary = (Dictionary<string, Location>)field.GetValue(LocationComplex.Current);

            if (dictionary.ContainsKey("village_inn"))
            {
                dictionary.Remove("village_inn");
            }

            _inn.SetOwnerComplex(settlement.LocationComplex);
            string cultureId = settlement.Culture.StringId;

            string sceneName = GetSceneNameForCulture(cultureId);
            _inn.SetSceneName(0, sceneName);

            AddWanderersToInn(settlement); // Add wanderers to the inn

            dictionary.Add("village_inn", _inn);
            if (field != null)
            {
                field.SetValue(LocationComplex.Current, dictionary);
            }
        }

        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            CleanUpInn();
        }

        [GameMenuInitializationHandler("village_inn")]
        public static void InnMenuSoundOnInit(MenuCallbackArgs args)
        {
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/tavern");
        }

        private void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption("village", "village_inn", "{=ADOD_Inn_GameMenu_GoToInn}Go to the village inn", CanGoInnOnCondition, delegate
            {
                OnInnEntered?.Invoke(Hero.MainHero.CurrentSettlement.Village);
                GameMenu.SwitchToMenu("village_inn");
                IsInInn = true;
            }, false, 1, false);

            campaignGameStarter.AddGameMenu("village_inn", "{=ADOD_Inn_GameMenu_InInn}You are in the village inn", VillageInnOnInit, GameOverlays.MenuOverlayType.SettlementWithCharacters, GameMenu.MenuFlags.None, null);

            campaignGameStarter.AddGameMenuOption("village_inn", "village_inn_visit", "{=ADOD_Inn_GameMenu_VisitTheInn}Visit the inn", VisitInnOnCondition, VisitInnOnConsequence, false, 0, false);

            campaignGameStarter.AddGameMenuOption("village_inn", "village_inn_back", "{=ADOD_Inn_GameMenu_Back}Back to village", BackOnCondition, delegate
            {
                OnInnLeft?.Invoke(Hero.MainHero.CurrentSettlement.Village);
                GameMenu.SwitchToMenu("village");
                IsInInn = false;
            }, true, 5, false);
        }

        public static bool CanGoInnOnCondition(MenuCallbackArgs args)
        {
            SettlementAccessModel settlementAccessModel = Campaign.Current.Models.SettlementAccessModel;
            Settlement currentSettlement = Settlement.CurrentSettlement;
            settlementAccessModel.CanMainHeroEnterSettlement(currentSettlement, out var accessDetails);
            if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess && accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.VillageIsLooted)
            {
                return false;
            }
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        }

        private static void VillageInnOnInit(MenuCallbackArgs args)
        {
            Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty.CurrentSettlement;
            string cultureId = settlement.Culture.StringId;
            string sceneName = GetSceneNameForCulture(cultureId);
            SetScenes(sceneName);
            args.MenuTitle = new TextObject("{=ADOD_Inn_GameMenu_Inn}Inn");
        }

        private static bool VisitInnOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            return true;
        }

        private static void VisitInnOnConsequence(MenuCallbackArgs args)
        {
            OpenMissionWithSettingPreviousLocation("village_center", "village_inn");
        }

        private static void OpenMissionWithSettingPreviousLocation(string previousLocationId, string missionLocationId)
        {
            Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId(missionLocationId);
            Campaign.Current.GameMenuManager.PreviousLocation = LocationComplex.Current.GetLocationWithId(previousLocationId);
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation, null, null, null);
            Campaign.Current.GameMenuManager.NextLocation = null;
            Campaign.Current.GameMenuManager.PreviousLocation = null;
        }

        private static bool BackOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private static void SetScenes(string SceneName)
        {
            for (int i = 0; i < 4; i++)
            {
                _inn.SetSceneName(i, SceneName);
            }
        }

        private void CleanUpInn()
        {
            if (_isInnInitialized)
            {
                _inn.RemoveAllCharacters();
                _inn.SetOwnerComplex(null);
                _isInnInitialized = false;
            }
        }

        public void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
        {
            if (CampaignMission.Current.Location.StringId != "village_inn" || _isInnInitialized)
            {
                return;
            }
            Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
            AddPeopleToTownTavern(settlement, unusedUsablePointCount);
            _isInnInitialized = true;
        }

        private void AddPeopleToTownTavern(Settlement settlement, Dictionary<string, int> unusedUsablePointCount)
        {
            Location locationWithId = LocationComplex.Current.GetLocationWithId("village_inn");

            List<Hero> allNotables = settlement.Notables.ToList();
            foreach (Hero notable in allNotables)
            {
                ADODInnHelper.AddNotableLocationCharacter(notable, settlement, _inn);
            }

            // Add wanderers to the inn
            AddWanderersToInn(settlement);

            if (unusedUsablePointCount.TryGetValue("spawnpoint_tavernkeeper", out var num) && num > 0)
            {
                locationWithId.AddLocationCharacters(CreateTavernkeeper, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
            }
            if (unusedUsablePointCount.TryGetValue("sp_tavern_wench", out num) && num > 0)
            {
                locationWithId.AddLocationCharacters(CreateTavernWench, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
            }
            if (unusedUsablePointCount.TryGetValue("musician", out num) && num > 0)
            {
                locationWithId.AddLocationCharacters(CreateMusician, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num);
            }

            if (unusedUsablePointCount.TryGetValue("npc_dancer", out var num2) && num2 > 0)
            {
                LocationComplex.Current.GetLocationWithId("village_inn").AddLocationCharacters(CreateDancer, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num2);
            }
            if (unusedUsablePointCount.TryGetValue("npc_common", out num))
            {
                int num8 = (int)((float)num * 0.3f);
                if (num8 > 0)
                {
                    locationWithId.AddLocationCharacters(CreateTownsManForTavern, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num8);
                }
                int num9 = (int)((float)num * 0.3f);
                if (num9 > 0)
                {
                    locationWithId.AddLocationCharacters(CreateTownsWomanForTavern, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, num9);
                }
                int num10 = (int)((float)num * 0.3f);
                if (num10 > 0)
                {
                    locationWithId.AddLocationCharacters(CreateMaleChildForTavern, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, TaleWorlds.Library.MathF.Ceiling(num10 / 4f));
                    locationWithId.AddLocationCharacters(CreateMaleTeenagerForTavern, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, TaleWorlds.Library.MathF.Ceiling(num10 / 4f));
                    locationWithId.AddLocationCharacters(CreateFemaleChildForTavern, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, TaleWorlds.Library.MathF.Ceiling(num10 / 4f));
                    locationWithId.AddLocationCharacters(CreateFemaleTeenagerForTavern, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, TaleWorlds.Library.MathF.Ceiling(num10 / 4f));
                }
            }
        }
        private void AddWanderersToInn(Settlement settlement)
        {
            var wanderers = settlement.HeroesWithoutParty.Where(hero => hero.IsWanderer && hero.CompanionOf == null).ToList();
            foreach (var wanderer in wanderers)
            {
                Monster monster = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(wanderer.CharacterObject.Race, "_settlement");
                AgentData agentData = new AgentData(new SimpleAgentOrigin(wanderer.CharacterObject))
                    .Monster(monster)
                    .Age((int)wanderer.Age); // Cast to int

                LocationCharacter wandererCharacter = new LocationCharacter(
                    agentData,
                    new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors),
                    "wanderer",
                    true,
                    LocationCharacter.CharacterRelations.Neutral,
                    ActionSetCode.GenerateActionSetNameWithSuffix(monster, wanderer.IsFemale, ""),
                    true,
                    false,
                    null,
                    false,
                    false,
                    true);

                _inn.AddLocationCharacters(delegate { return wandererCharacter; }, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
            }
        }


        private static LocationCharacter CreateDancer(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject femaleDancer = culture.FemaleDancer;
            Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(femaleDancer.Race, "_settlement");
            int minValue;
            int maxValue;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(femaleDancer, out minValue, out maxValue, "Dancer");
            AgentData agentData = new AgentData(new SimpleAgentOrigin(femaleDancer, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
            return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_dancer", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_dancer"), true, false, null, false, false, true);
        }

        private static LocationCharacter CreateTavernkeeper(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject tavernkeeper = CharacterObject.CreateFrom(culture.Tavernkeeper);
            TextObject value2 = new TextObject("{=ADOD_Inn_OwnerName}Innkeeper", null);
            FieldInfo field2 = tavernkeeper.GetType().GetField("_basicName", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field2 != null)
            {
                field2.SetValue(tavernkeeper, value2);
            }
            tavernkeeper.StringId = "inn_keeper";
            Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(tavernkeeper.Race, "_settlement");
            int minValue;
            int maxValue;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(tavernkeeper, out minValue, out maxValue, "");
            AgentData agentData = new AgentData(new SimpleAgentOrigin(tavernkeeper, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
            return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "spawnpoint_tavernkeeper", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_tavern_keeper"), true, false, null, false, false, true);
        }

        private static LocationCharacter CreateMusician(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject musician = culture.Musician;
            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(musician.Race, "_settlement");
            int minValue;
            int maxValue;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(musician, out minValue, out maxValue, "");
            AgentData agentData = new AgentData(new SimpleAgentOrigin(musician, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
            return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "musician", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_musician"), true, false, null, false, false, true);
        }

        private static LocationCharacter CreateTavernWench(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject tavernWench = CharacterObject.CreateFrom(culture.TavernWench);
            TextObject value2 = new TextObject("{=ADOD_Inn_HelperName}Inn Helper", null);
            FieldInfo field2 = tavernWench.GetType().GetField("_basicName", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field2 != null)
            {
                field2.SetValue(tavernWench, value2);
            }
            tavernWench.StringId = "inn_helper";
            int minValue;
            int maxValue;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(tavernWench, out minValue, out maxValue, "");
            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(tavernWench.Race, "_settlement");
            AgentData agentData = new AgentData(new SimpleAgentOrigin(tavernWench, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
            return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_tavern_wench", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_barmaid"), true, false, null, false, false, true)
            {
                PrefabNamesForBones =
                {
                    {
                        agentData.AgentMonster.OffHandItemBoneIndex,
                        "kitchen_pitcher_b_tavern"
                    }
                }
            };
        }


        private static LocationCharacter CreateTownsManForTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townsman = culture.Villager;
            Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townsman.Race, "_settlement_slow");
            string actionSetCode;
            if (culture.StringId.ToLower() == "aserai" || culture.StringId.ToLower() == "khuzait")
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townsman.IsFemale, "_villager_in_aserai_tavern");
            }
            else
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townsman.IsFemale, "_villager_in_tavern");
            }
            int minValue;
            int maxValue;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out minValue, out maxValue, "TavernVisitor");
            return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(18, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, relation, actionSetCode, true, false, null, false, false, true);
        }

        private static LocationCharacter CreateTownsWomanForTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townswoman = culture.VillageWoman;
            Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(townswoman.Race, "_settlement_slow");
            string actionSetCode;
            if (culture.StringId.ToLower() == "aserai" || culture.StringId.ToLower() == "khuzait")
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townswoman.IsFemale, "_warrior_in_aserai_tavern");
            }
            else
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, townswoman.IsFemale, "_warrior_in_tavern");
            }
            int minValue;
            int maxValue;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out minValue, out maxValue, "TavernVisitor");
            return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(18, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, relation, actionSetCode, true, false, null, false, false, true);
        }

        public static LocationCharacter CreateMaleChildForTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {

            CharacterObject characterObject = culture.VillagerMaleChild;


            Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement_slow");
            string actionSetCode;
            if (culture.StringId.ToLower() == "aserai" || culture.StringId.ToLower() == "khuzait")
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, characterObject.IsFemale, "_villager_in_aserai_tavern");
            }
            else
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, characterObject.IsFemale, "_villager_in_tavern");
            }
            return new LocationCharacter(new AgentData(new SimpleAgentOrigin(characterObject, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, relation, actionSetCode, true, false, null, false, false, true);
        }

        public static LocationCharacter CreateFemaleChildForTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {

            CharacterObject characterObject = culture.VillagerFemaleChild;


            Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement_slow");
            string actionSetCode;
            if (culture.StringId.ToLower() == "aserai" || culture.StringId.ToLower() == "khuzait")
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, characterObject.IsFemale, "_villager_in_aserai_tavern");
            }
            else
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, characterObject.IsFemale, "_villager_in_tavern");
            }
            return new LocationCharacter(new AgentData(new SimpleAgentOrigin(characterObject, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, relation, actionSetCode, true, false, null, false, false, true);
        }

        public static LocationCharacter CreateMaleTeenagerForTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {

            CharacterObject characterObject = culture.VillagerMaleTeenager;


            Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement_slow");
            string actionSetCode;
            if (culture.StringId.ToLower() == "aserai" || culture.StringId.ToLower() == "khuzait")
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, characterObject.IsFemale, "_villager_in_aserai_tavern");
            }
            else
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, characterObject.IsFemale, "_villager_in_tavern");
            }
            return new LocationCharacter(new AgentData(new SimpleAgentOrigin(characterObject, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, relation, actionSetCode, true, false, null, false, false, true);
        }
        public static LocationCharacter CreateFemaleTeenagerForTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {

            CharacterObject characterObject = culture.VillagerFemaleTeenager;


            Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement_slow");
            string actionSetCode;
            if (culture.StringId.ToLower() == "aserai" || culture.StringId.ToLower() == "khuzait")
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, characterObject.IsFemale, "_villager_in_aserai_tavern");
            }
            else
            {
                actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, characterObject.IsFemale, "_villager_in_tavern");
            }
            return new LocationCharacter(new AgentData(new SimpleAgentOrigin(characterObject, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, relation, actionSetCode, true, false, null, false, false, true);
        }
        private static string GetSceneNameForCulture(string cultureId)
        {
            return cultureId switch
            {
                "empire" => "empire_house_c_interior_tavern",
                "sturgia" => "sturgia_house_d_interior_tavern",
                "aserai" => "arabian_house_new_c_interior_c_tavern",
                "vlandia" => "vlandia_city_house_a_interior_tavern",
                "khuzait" => "khuzait_house_e_interior_b_tavern",
                "battania" => "battania_town_house_b_interior_b_tavern",
                _ => "empire_house_c_interior_tavern"
            };
        }
    }
}