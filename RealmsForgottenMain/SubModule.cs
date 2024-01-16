using HarmonyLib;
using RealmsForgotten.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using RealmsForgotten.Behaviors;
using RealmsForgotten.CustomSkills;
using RealmsForgotten.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using System.Reflection;
using MCM.Abstractions.Attributes;
using TaleWorlds.Engine.GauntletUI;
using Module = TaleWorlds.MountAndBlade.Module;
using Newtonsoft.Json.Linq;
using RealmsForgotten.Quest;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.GameMenus;
using RealmsForgotten.Patches;
using RealmsForgotten.Quest.SecondUpdate;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

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
                campaignGameStarter.AddBehavior(new RFFaithCampaignBehavior());

                campaignGameStarter.AddModel(new RFAgentApplyDamageModel());
                campaignGameStarter.AddModel(new RFAgentStatCalculateModel());
                campaignGameStarter.AddModel(new RFBuildingConstructionModel());
                campaignGameStarter.AddModel(new RFCombatXpModel());
                campaignGameStarter.AddModel(new RFDefaultCharacterDevelopmentModel());
                campaignGameStarter.AddModel(new RFPartyMoraleModel());
                campaignGameStarter.AddModel(new RFPartySpeedCalculatingModel());
                campaignGameStarter.AddModel(new RFPrisonerRecruitmentCalculationModel());
                campaignGameStarter.AddModel(new RFRaidModel());
                campaignGameStarter.AddModel(new RFVolunteerModel());
                campaignGameStarter.AddModel(new RFWageModel());
                campaignGameStarter.AddModel(new RFBattleCaptainModel());

                new RFAttribute().Initialize();
                new RFSkills().Initialize();
                new RFSkillEffects().InitializeAll();
                new RFPerks().Initialize();
            }
            if (CustomSettings.Instance != null)
                CheckInvalidKeys();
        }

        private void CheckInvalidKeys()
        {
            string[] keys = Enum.GetNames(typeof(InputKey));
            foreach (var property in AccessTools.GetDeclaredProperties(typeof(CustomSettings)))
                if (Attribute.GetCustomAttribute(property, typeof(SettingPropertyGroupAttribute))
                    is SettingPropertyGroupAttribute keyAttribute && keyAttribute.GroupName.Contains("KeyMapping"))
                {
                    if (property.GetValue(CustomSettings.Instance) is string key)
                    {
                        bool valid = false;
                        if (key.Length == 1 && keys.Contains(key.ToUpper()))
                        {
                            property.SetValue(CustomSettings.Instance, key.ToUpper());
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
                            property.SetValue(CustomSettings.Instance, defaultKey.DefaultValue);
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
                mission.AddMissionBehavior(new RFEnchantedWeaponsMissionBehavior());
                mission.AddMissionBehavior(new SpellAmmoMissionBehavior());
                mission.AddMissionBehavior(new NecromancerStaffMissionBehavior());

                if (Campaign.Current != null)
                {
                    ItemRosterElement elixir = PartyBase.MainParty.ItemRoster.FirstOrDefault(x => x.EquipmentElement.Item.StringId.Contains("elixir_rfmisc"));
                    ItemRosterElement berserker = PartyBase.MainParty.ItemRoster.FirstOrDefault(x => x.EquipmentElement.Item.StringId.Contains("berzerker_potion"));
                    if (!elixir.IsEmpty || !berserker.IsEmpty)
                        mission.AddMissionBehavior(new PotionsMissionBehavior(elixir, berserker));
                }
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
#pragma warning restore BHA0003 // Type was not found
            harmony.Patch(originalMethod, transpiler: new HarmonyMethod(typeof(PartyVMPatch), nameof(PartyVMPatch.PartyVMPopulatePartyListLabelPatch)));

            QuestPatches.PatchAll();

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
            harmony.PatchAll();
            base.OnSubModuleLoad();
            TextObject coreContentDisabledReason = new("Disabled during installation.", null);
            UIConfig.DoNotUseGeneratedPrefabs = true;
            
            RemoveSandboxAndStoryOptions();

            Module.CurrentModule.AddInitialStateOption(
                new InitialStateOption("RF", name: new TextObject("Realms Forgotten", null), 3,
                () => MBGameManager.StartNewGame(new RFCampaignManager()),
                () => (Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason))
            );
            harmony.PatchAll();
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
            QuestSubModule.OnGameLoaded(game, initializerObject);
            
        }

        public override void OnNewGameCreated(Game game, object initializerObject)
        {
            base.OnNewGameCreated(game, initializerObject);
            QuestSubModule.OnNewGameCreated(game, initializerObject);
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

        //Map border crash fix
        [HarmonyPatch(typeof(DefaultMapWeatherModel), "GetWeatherEventInPosition")]
        class ArrangeDestructedMeshesPatch
        {
            [HarmonyFinalizer]
#pragma warning disable IDE0051 // Remove unused private members
            static Exception Finalizer(Exception __exception, DefaultMapWeatherModel __instance)
#pragma warning restore IDE0051 // Remove unused private members
            {
                return null;
            }
        }
    }
}
