using HarmonyLib;
using MCM.Abstractions.Attributes;
using Newtonsoft.Json.Linq;
using RealmsForgotten.AiMade;
using RealmsForgotten.Behaviors;
using RealmsForgotten.Career;
using RealmsForgotten.Career.Ability;
using RealmsForgotten.Career.Logic;
using RealmsForgotten.CharacterCreation;
using RealmsForgotten.CustomBandits;
using RealmsForgotten.CustomSkills;
using RealmsForgotten.LegendaryTroops;
using RealmsForgotten.Managers;
using RealmsForgotten.Models;
using RealmsForgotten.Patches;
using RealmsForgotten.Quest;
using RealmsForgotten.Quest.FourthUpdate;
using RealmsForgotten.RFCustomBandits;
using RealmsForgotten.RFCustomHorses;
using RealmsForgotten.RFEffects;
using RealmsForgotten.RFMissionLogic;
using RealmsForgotten.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using Module = TaleWorlds.MountAndBlade.Module;

namespace RealmsForgotten
{
    public class SubModule : MBSubModuleBase
    {
        public static readonly Harmony harmony = new("RealmsForgotten");

        internal static readonly Random random = new();
        internal static Dictionary<string, Tuple<string, string, string, string>> villagerMin = new();
        internal static Dictionary<string, Tuple<string, string, string, string>> villagerMax = new();
        internal static Dictionary<string, Tuple<string, string, string, string>> fighterMin = new();
        internal static Dictionary<string, Tuple<string, string, string, string>> fighterMax = new();
        private bool manualPatchesHaveFired;
        public Dictionary<string, InputKey> KeysConfig;

        public static SubModule Instance;

        public SubModule()
        {
            KeysConfig = new();
            Instance = this;
        }

        internal static readonly string[] cultures = new string[]
        {
            "battania",
            "aserai",
            "empire",
            "khuzait",
            "sturgia",
            "vlandia"
        };
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (gameStarterObject is CampaignGameStarter campaignGameStarter)
            {
                campaignGameStarter.AddBehavior(new BaseGameDebugCampaignBehavior());
                campaignGameStarter.AddBehavior(new RFEnchantmentVendorBehavior());
                //Faith bhv comes before cultures bhv
                campaignGameStarter.AddBehavior(new RFFaithCampaignBehavior());
                campaignGameStarter.AddBehavior(new CulturesCampaignBehavior());
                
                campaignGameStarter.AddBehavior(RFHorseSpawningCampaignBehavior.Instance);
                campaignGameStarter.AddBehavior(new RFCareerCampaignBehavior());

                campaignGameStarter.AddBehavior(new SlaversRosterBehavior());
                campaignGameStarter.AddBehavior(new AiSlaversPatrollingBehavior());
                campaignGameStarter.AddBehavior(new RFLegendaryTroopsPlayerVisitTownCampaignBehavior());
                campaignGameStarter.AddBehavior(new RFLegendaryTroopsNotableBehaviors());
                campaignGameStarter.AddBehavior(new RFLegendaryTroopsAIRecruitment());

                campaignGameStarter.AddModel(new RFAgentApplyDamageModel(campaignGameStarter.GetExistingModel<AgentApplyDamageModel>()));
                campaignGameStarter.AddModel(new RFBuildingConstructionModel(campaignGameStarter.GetExistingModel<BuildingConstructionModel>()));
                campaignGameStarter.AddModel(new RFCombatXpModel(campaignGameStarter.GetExistingModel<CombatXpModel>()));
                campaignGameStarter.AddModel(new RFDefaultCharacterDevelopmentModel(campaignGameStarter.GetExistingModel<CharacterDevelopmentModel>()));
                campaignGameStarter.AddModel(new RFPartyMoraleModel(campaignGameStarter.GetExistingModel<PartyMoraleModel>()));
                campaignGameStarter.AddModel(new RFPartySpeedCalculatingModel(campaignGameStarter.GetExistingModel<PartySpeedModel>()));
                campaignGameStarter.AddModel(new RFCharacterStatsModel(campaignGameStarter.GetExistingModel<CharacterStatsModel>()));
                campaignGameStarter.AddModel(new RFPartyHealingModel(campaignGameStarter.GetExistingModel<PartyHealingModel>()));
                campaignGameStarter.AddModel(new RFClanPoliticsModel(campaignGameStarter.GetExistingModel<ClanPoliticsModel>()));
                campaignGameStarter.AddModel(new RFPrisonerRecruitmentCalculationModel(campaignGameStarter.GetExistingModel<PrisonerRecruitmentCalculationModel>()));
                campaignGameStarter.AddModel(new RFRaidModel(campaignGameStarter.GetExistingModel<RaidModel>()));
                campaignGameStarter.AddModel(new RFVolunteerModel(campaignGameStarter.GetExistingModel<VolunteerModel>()));
                campaignGameStarter.AddModel(new RFWageModel(campaignGameStarter.GetExistingModel<PartyWageModel>()));
                campaignGameStarter.AddModel(new RFBattleCaptainModel(campaignGameStarter.GetExistingModel<BattleCaptainModel>()));
                campaignGameStarter.AddModel(new RFInventoryCapacityModel(campaignGameStarter.GetExistingModel<InventoryCapacityModel>()));
                campaignGameStarter.AddModel(new RFBanditDensityModel(campaignGameStarter.GetExistingModel<BanditDensityModel>()));
                campaignGameStarter.AddModel(new RFClanFinanceModel(campaignGameStarter.GetExistingModel<ClanFinanceModel>()));
                campaignGameStarter.AddModel(new RFMapVisibilityModel(campaignGameStarter.GetExistingModel<MapVisibilityModel>()));
                campaignGameStarter.AddModel(new RFPartySizeLimitModel(campaignGameStarter.GetExistingModel<PartySizeLimitModel>()));
                campaignGameStarter.AddModel(new RFClanTierModel());
                campaignGameStarter.AddModel(new RFStrikeMagnitudeModel());
                campaignGameStarter.AddModel(new RFSettlementValueModel(campaignGameStarter.GetExistingModel<SettlementValueModel>()));

                new RFAttributes().Initialize();
                new RFSkills().Initialize();
                new RFSkillEffects().InitializeAll();
                new RFPerks().Initialize();

                AiSubModule.AddCampaignBehaviors(campaignGameStarter);

                ReadConfigFile();
            }
            if (RFSettings.Instance != null)
                CheckInvalidKeys();
        }

        private void CheckInvalidKeys()
        {
            string[] keys = Enum.GetNames(typeof(InputKey));
            foreach (var property in AccessTools.GetDeclaredProperties(typeof(ICustomSettingsProvider)))
                if (Attribute.GetCustomAttribute(property, typeof(SettingPropertyGroupAttribute))
                    is SettingPropertyGroupAttribute keyAttribute && keyAttribute.GroupName.Contains("KeyMapping"))
                {
                    if (property.GetValue(RFSettings.Instance) is string key)
                    {
                        bool valid = false;
                        if (key.Length == 1 && keys.Contains(key.ToUpper()))
                        {
                            property.SetValue(RFSettings.Instance, key.ToUpper());
                            key = key.ToUpper();
                            valid = true;
                        }
                        else if (key.Length > 1 && keys.Contains(key))
                            valid = true;
                        else if (!keys.Contains(key))
                        {
                            DefaultKey defaultKey = (DefaultKey)Attribute.GetCustomAttribute(property, typeof(DefaultKey));
                            
                            InformationManager.ShowInquiry(new InquiryData("Error", $"Invalid key at {property.Name}, setting to default ({defaultKey.DefaultValue})", true,
                                false, GameTexts.FindText("str_done").ToString(), "", null, null), true);
                            property.SetValue(RFSettings.Instance, defaultKey.DefaultValue);
                        }

                        if (valid)
                        {
                            if (!KeysConfig.ContainsKey(property.Name))
                                KeysConfig.Add(property.Name, (InputKey)Enum.Parse(typeof(InputKey), key));
                            else
                                KeysConfig[property.Name] = (InputKey)Enum.Parse(typeof(InputKey), key);
                        }
                    }
                }
        }
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            if (mission != null)
            {
                //temp
                if (mission.SceneName == "witch_lair_inside_final")
                    mission.AddMissionBehavior(new WingedWitchFinalMissionLogic());
                if (mission.SceneName == "witch_lair_canyon")
                    mission.AddMissionBehavior(new WitchCanyonMissionLogic());
                //
                mission.AddMissionBehavior(new SpawnAgentMissionLogic());
                mission.AddMissionBehavior(new AbilityManagerMissionLogic());
                mission.AddMissionBehavior(new AbilityHUDMissionView());
                if ((mission.Mode == MissionMode.Battle || mission.Mode == MissionMode.StartUp) && mission.CombatType != Mission.MissionCombatType.ArenaCombat)
                {
                    mission.AddMissionBehavior(new RFEnchantedWeaponsMissionBehavior());
                    mission.AddMissionBehavior(new NecromancerStaffMissionBehavior());
                    mission.AddMissionBehavior(new SpecialDamageMissionLogic());
                    mission.AddMissionBehavior(new DemonLordsAmbushLogic());
                    mission.AddMissionBehavior(new GandalfStaffMissionBehavior());
                }
                mission.AddMissionBehavior(new SpellAmmoMissionBehavior());

                if (Campaign.Current != null)
                {
                    ItemRosterElement elixir = PartyBase.MainParty.ItemRoster.FirstOrDefault(x => x.EquipmentElement.Item.StringId.Contains("elixir_rfmisc"));
                    ItemRosterElement berserker = PartyBase.MainParty.ItemRoster.FirstOrDefault(x => x.EquipmentElement.Item.StringId.Contains("berzerker_potion"));
                    if (!elixir.IsEmpty || !berserker.IsEmpty)
                        mission.AddMissionBehavior(new PotionsMissionBehavior(elixir, berserker));
                }
                mission.AddMissionBehavior(new HealOnKillMissionBehavior());
            }
            if (Game.Current.GameType is Campaign)
            {
                mission.AddMissionBehavior(new CareerPerkMissionBehavior());
            }
            mission.AddMissionBehavior(new MagicEffectsBehavior());
            mission.AddMissionBehavior(new WeaponParticlesBehavior());
            mission.AddMissionBehavior(new MeteorMissionLogic());
        }
        public override void BeginGameStart(Game game)
        {
            if (game.GameType is Campaign campaign)
            {
                game.ObjectManager.RegisterType<CareerObject>("Career", "Careers", 103U, true);
                game.ObjectManager.RegisterType<CareerChoiceObject>("CareerChoice", "CareerChoices", 104U, true);
                game.ObjectManager.RegisterType<CareerChoiceGroupObject>("CareerChoiceGroup", "CareerChoiceGroups", 105U, true);

                _ = new RFCareers();
                _ = new RFCareerChoiceGroups();
                _ = new RFCareerChoices();

                //campaign start
                CampaignGameStarter starter = campaign.SandBoxManager.GameStarter;
                starter.RemoveBehaviors<CharacterCreationCampaignBehavior>();
                starter.AddBehavior(new RFCharacterCreationCampaignBehavior());
            }
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot() { }
        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);

            //Globals.SetRacesIds();
            if (!manualPatchesHaveFired)
            {
                manualPatchesHaveFired = true;
                RunManualPatches();
            }
        }
        private void RunManualPatches()
        {
#pragma warning disable BHA0003 // Type was not found
            MethodInfo originalMethod = AccessTools.Method("PartyVM:PopulatePartyListLabel");
            //            MethodInfo beardGetterMethod = AccessTools.Method("FaceGenVM:UpdateRaceAndGenderBasedResources");
#pragma warning restore BHA0003 // Type was not found
            harmony.Patch(originalMethod, transpiler: new HarmonyMethod(typeof(PartyVMPatch), nameof(PartyVMPatch.PartyVMPopulatePartyListLabelPatch)));
            //          harmony.Patch(beardGetterMethod, transpiler: new HarmonyMethod(typeof(PartyVMPatch), nameof(PartyVMPatch.PartyVMPopulatePartyListLabelPatch)));
            // run manually to remove broken bones when viewing characters
            MethodInfo ammoMethod = AccessTools.Method("Agent:OnWeaponAmmoReload");
            MethodInfo damageInfo = AccessTools.Method("Agent:HandleBlow");
            harmony.Patch(ammoMethod, prefix: new HarmonyMethod(typeof(RFSpellAmmo), nameof(RFSpellAmmo.OnWeaponAmmoReloadPatch)));
            harmony.Patch(damageInfo, prefix: new HarmonyMethod(typeof(DamagePatch), nameof(DamagePatch.PreHandleBlow)));
            QuestPatches.PatchAll();


            var target = AccessTools.Method(typeof(BanditSpawnCampaignBehavior), "IsLooterFaction", new Type[] { typeof(IFaction) });
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(BanditSpawnPatch), nameof(BanditSpawnPatch.Prefix)));
            var hideoutMenuInit = AccessTools.Method(typeof(HideoutCampaignBehavior), "game_menu_hideout_place_on_init");
            var hideoutSendTroops = AccessTools.Method(typeof(HideoutCampaignBehavior), "game_menu_send_troops_hideout_on_condition");
            var hideoutSneakIn = AccessTools.Method(typeof(HideoutCampaignBehavior), "game_menu_hideout_sneak_in_on_condition");
            var hideoutAssault = AccessTools.Method(typeof(HideoutCampaignBehavior), "game_menu_assault_hideout_parties_on_condition");
            harmony.Patch(hideoutMenuInit, postfix: new HarmonyMethod(typeof(GameMenuPatches), nameof(GameMenuPatches.HideoutInitPostfix)));
            harmony.Patch(hideoutSendTroops, postfix: new HarmonyMethod(typeof(GameMenuPatches), nameof(GameMenuPatches.SendTroopsPostfix)));
            harmony.Patch(hideoutSneakIn, postfix: new HarmonyMethod(typeof(GameMenuPatches), nameof(GameMenuPatches.SneakInPostfix)));
            harmony.Patch(hideoutAssault, postfix: new HarmonyMethod(typeof(GameMenuPatches), nameof(GameMenuPatches.AssaultHideoutPostfix)));
        }

        private void RemoveSandboxAndStoryOptions()
        {
            List<InitialStateOption> initialOptionsList = Module.CurrentModule.GetInitialStateOptions().ToList();
            initialOptionsList.RemoveAll(x => x.Id == "SandBoxNewGame" || x.Id == "StoryModeNewGame");
            Module.CurrentModule.ClearStateOptions();
            foreach(InitialStateOption initialStateOption in initialOptionsList)
            {
                Module.CurrentModule.AddInitialStateOption(initialStateOption);
            }
        }
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            ViewModelExtensionManager.Initialize(); //has to happen before harmony PatchAll
            harmony.PatchAll();


            TextObject coreContentDisabledReason = new("Disabled during installation.", null);
            UIConfig.DoNotUseGeneratedPrefabs = true;
            
            RemoveSandboxAndStoryOptions();

            Module.CurrentModule.AddInitialStateOption(
                new InitialStateOption("RF", name: new TextObject("Realms Forgotten", null), 3,
                () => MBGameManager.StartNewGame(new RFCampaignManager()),
                () => (Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason))
            );

            foreach (var method in AccessTools.GetDeclaredMethods(typeof(WeaponEffectConsequences)).Where(x => x.IsPublic))
            {
                WeaponEffectConsequences.Methods.Add(method.Name, (VictimAgentConsequence)method.CreateDelegate(typeof(VictimAgentConsequence)));
            }
        }
        public static Dictionary<string, int> undeadRespawnConfig { get; private set; }
        private void ReadConfigFile()
        {
            string jsonFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "undead_respawn_config.json");
            JObject jsonObject = JObject.Parse(File.ReadAllText(jsonFilePath));

            if (jsonObject.TryGetValue("characters", out JToken charactersToken))
            {
                JObject charactersObject = (JObject)charactersToken;
                undeadRespawnConfig = new();
                foreach (var character in charactersObject)
                {

                    string characterName = character.Key;
                    int characterValue = character.Value.Value<int>();
                    if (characterValue > 100)
                        characterValue = 100;
                    if (characterValue < 1)
                        characterValue = 1;
                    undeadRespawnConfig.Add(characterName, characterValue);
                }

            }
            else
            {
                Console.WriteLine("Error in undead_respawn_config.json");
            }
        }

        public override void OnGameLoaded(Game game, object initializerObject)
        {
            base.OnGameLoaded(game, initializerObject);

            if (initializerObject is CampaignGameStarter campaignGameStarter)
            {
                QuestSubModule.OnGameLoaded(campaignGameStarter);
                RFAgentStatCalculateModel rfAgentStatCalculateModel = new RFAgentStatCalculateModel(campaignGameStarter.GetExistingModel<AgentStatCalculateModel>());
                campaignGameStarter.AddModel(rfAgentStatCalculateModel);
                
                AccessTools.Property(typeof(MissionGameModels), "AgentStatCalculateModel").SetValue(MissionGameModels.Current, rfAgentStatCalculateModel);
            }
        }

        public override void OnNewGameCreated(Game game, object initializerObject)
        {
            base.OnNewGameCreated(game, initializerObject);
            QuestSubModule.OnNewGameCreated((CampaignGameStarter)initializerObject);
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);
            XmlDocument xmlDocument = new();
            xmlDocument.Load(Path.Combine(ModuleHelper.GetModuleFullPath("SandBoxCore"), "ModuleData/sandboxcore_bodyproperties.xml"));
            SubModule.villagerMax = new Dictionary<string, Tuple<string, string, string, string>>();
            SubModule.villagerMin = new Dictionary<string, Tuple<string, string, string, string>>();
            SubModule.fighterMax = new Dictionary<string, Tuple<string, string, string, string>>();
            SubModule.fighterMin = new Dictionary<string, Tuple<string, string, string, string>>();
            foreach (XmlNode xmlNode in xmlDocument.ChildNodes)
            {
                if (xmlNode.Name == "BodyProperties")
                {
                    foreach (XmlNode xmlNode2 in xmlNode)
                    {
                        if (xmlNode2.Name == "BodyProperty")
                        {
                            XmlAttribute idAttribute = xmlNode2.Attributes["id"];
                            for (int i = 0; i < SubModule.cultures.Length; i++)
                            {
                                foreach (XmlElement xmlElement in xmlNode2.ChildNodes)
                                {
                                    if (idAttribute.FirstChild.Value.Contains(SubModule.cultures[i]))
                                    {
                                        if (!SubModule.villagerMin.TryGetValue(SubModule.cultures[i], out Tuple<string, string, string, string> tuple) && xmlElement.Name == "BodyPropertiesMin" && idAttribute.Value.Contains("villager"))
                                        {
                                            SubModule.villagerMin.Add(SubModule.cultures[i], new Tuple<string, string, string, string>(xmlElement.Attributes["age"].Value, xmlElement.Attributes["weight"].Value, xmlElement.Attributes["build"].Value, xmlElement.Attributes["key"].Value));
                                        }
                                        else if (!SubModule.villagerMax.TryGetValue(SubModule.cultures[i], out tuple) && xmlElement.Name == "BodyPropertiesMax" && xmlNode2.Attributes["id"].Value.Contains("villager"))
                                        {
                                            SubModule.villagerMax.Add(SubModule.cultures[i], new Tuple<string, string, string, string>(xmlElement.Attributes["age"].Value, xmlElement.Attributes["weight"].Value, xmlElement.Attributes["build"].Value, xmlElement.Attributes["key"].Value));
                                        }
                                        else if (!SubModule.fighterMin.TryGetValue(SubModule.cultures[i], out tuple) && xmlElement.Name == "BodyPropertiesMin" && xmlNode2.Attributes["id"].Value.Contains("fighter"))
                                        {
                                            SubModule.fighterMin.Add(SubModule.cultures[i], new Tuple<string, string, string, string>(xmlElement.Attributes["age"].Value, xmlElement.Attributes["weight"].Value, xmlElement.Attributes["build"].Value, xmlElement.Attributes["key"].Value));
                                        }
                                        else if (!SubModule.fighterMax.TryGetValue(SubModule.cultures[i], out tuple) && xmlElement.Name == "BodyPropertiesMax" && xmlNode2.Attributes["id"].Value.Contains("fighter"))
                                        {
                                            SubModule.fighterMax.Add(SubModule.cultures[i], new Tuple<string, string, string, string>(xmlElement.Attributes["age"].Value, xmlElement.Attributes["weight"].Value, xmlElement.Attributes["build"].Value, xmlElement.Attributes["key"].Value));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

//        //Map border crash fix
//        [HarmonyPatch(typeof(DefaultMapWeatherModel), "GetWeatherEventInPosition")]
//        class ArrangeDestructedMeshesPatch
//        {
//            [HarmonyFinalizer]
//#pragma warning disable IDE0051 // Remove unused private members
//            static Exception Finalizer(Exception __exception, DefaultMapWeatherModel __instance)
//#pragma warning restore IDE0051 // Remove unused private members
//            {
//                return null;
//            }
//        }
    }
}