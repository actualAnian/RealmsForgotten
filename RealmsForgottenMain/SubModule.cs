﻿using HarmonyLib;
using RealmsForgotten.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RealmsForgotten.Behaviors;
using RealmsForgotten.CustomSkills;
using RealmsForgotten.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten
{
    internal class SubModule : MBSubModuleBase
    {
        internal static readonly Random random = new();
        internal static Dictionary<string, Tuple<string, string, string, string>> villagerMin = new();
        internal static Dictionary<string, Tuple<string, string, string, string>> villagerMax = new();
        internal static Dictionary<string, Tuple<string, string, string, string>> fighterMin = new();
        internal static Dictionary<string, Tuple<string, string, string, string>> fighterMax = new();
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
            if (game.GameType is Campaign)
            {
                CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
                campaignGameStarter.AddBehavior(new BaseGameDebugCampaignBehavior());
                campaignGameStarter.AddBehavior(new RFCampaignBehavior());

                campaignGameStarter.AddModel(new RFAgentStatCalculateModel());
                campaignGameStarter.AddModel(new RFAgentApplyDamageModel());
                campaignGameStarter.AddModel(new RFVolunteerModel());
                campaignGameStarter.AddModel(new RFCombatXpModel());



                new RFAttribute().Initialize();
                new RFSkills().Initialize();
                new RFPerks().Initialize();

            }
        }
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            if (mission != null)
            {
                mission.AddMissionBehavior(new RFEnchantedWeaponsBehavior());

                ItemRosterElement elixir = PartyBase.MainParty.ItemRoster.FirstOrDefault(x => x.EquipmentElement.Item.StringId.Contains("elixir_rfmisc"));
                ItemRosterElement berserker = PartyBase.MainParty.ItemRoster.FirstOrDefault(x => x.EquipmentElement.Item.StringId.Contains("berzerker_potion"));
                if (!elixir.IsEmpty || !berserker.IsEmpty)
                    mission.AddMissionBehavior(new HealingPotionMissionBehavior(elixir, berserker));
            }

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
            TextObject coreContentDisabledReason = new("Disabled during installation.", null);

            RemoveSandboxAndStoryOptions();

            Module.CurrentModule.AddInitialStateOption(
                new InitialStateOption("RF", name: new TextObject("Realms Forgotten", null), 3,
                () => MBGameManager.StartNewGame(new RFCampaignManager()),
                () => (Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason))
            );
            new Harmony("mods.bannerlord.realmsforgotten").PatchAll();
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
            static Exception Finalizer(Exception __exception, DefaultMapWeatherModel __instance)
            {
                return null;
            }
        }
    }
}
