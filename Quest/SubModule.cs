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
using static Quest.RescueUliahBehavior;
using Module = TaleWorlds.MountAndBlade.Module;

namespace Quest
{



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
    public class QuestTypeDefiner : SaveableTypeDefiner
    {
        public QuestTypeDefiner() : base(585820)
        {
        }

        protected override void DefineClassTypes()
        {
            base.AddClassDefinition(typeof(RescueUliahQuest), 1);
            base.AddClassDefinition(typeof(QueenQuest), 2);
            base.AddClassDefinition(typeof(AnoritFindRelicsQuest), 3);
        }
    }

}