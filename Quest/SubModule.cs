using HarmonyLib;
using SandBox;
using SandBox.Conversation.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using Module = TaleWorlds.MountAndBlade.Module;

namespace Quest
{


    [HarmonyPatch(typeof(DefaultMapWeatherModel), "GetWeatherEventInPosition")]
    class ArrangeDestructedMeshesPatch
    {
        [HarmonyFinalizer]
        static Exception Finalizer(Exception __exception, DefaultMapWeatherModel __instance)
        {
            //If there is a exception it will increase the _weatherDataCache size
            if (__exception != null)
                AccessTools.Field(typeof(DefaultMapWeatherModel), "_weatherDataCache").SetValue(__instance, new MapWeatherModel.WeatherEvent[4096]);
            return null;
        }
    }
    public class SubModule : MBSubModuleBase
    {
        protected override void OnApplicationTick(float dt)
        {
            
        }
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Harmony harmony = new Harmony("rf_quest_patch");
            harmony.PatchAll();
        }
        public override void OnCampaignStart(Game game, object starterObject)
        {
            int i = 0;
        }
        public override void OnNewGameCreated(Game game, object initializerObject)
        {
            base.OnGameLoaded(game, initializerObject);
            CampaignGameStarter gameStarter = (CampaignGameStarter)initializerObject;
            AddQuestBehaviors(gameStarter, true);
        }
        public override void OnGameLoaded(Game game, object initializerObject)
        {
            base.OnGameLoaded(game, initializerObject);
            CampaignGameStarter gameStarter = (CampaignGameStarter)initializerObject;
            AddQuestBehaviors(gameStarter, false);
        
        }
        private void AddQuestBehaviors(CampaignGameStarter gameStarter, bool isNewGame)
        {
            if (gameStarter != null)
            {

                gameStarter.AddBehavior(new RescueUliahBehavior(gameStarter, isNewGame));
            }
        }
    }

}