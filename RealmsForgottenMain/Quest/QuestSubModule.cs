using HarmonyLib;
using SandBox;
using SandBox.Conversation.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RealmsForgotten.Quest.SecondUpdate;
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
using RealmsForgotten.Quest.KnightQuest;

namespace RealmsForgotten.Quest
{
    public static class QuestSubModule
    {
        public static void OnNewGameCreated(Game game, object initializerObject)
        {
            CampaignGameStarter gameStarter = (CampaignGameStarter)initializerObject;
            AddQuestBehaviors(gameStarter, true);
        }

        public static void OnGameLoaded(Game game, object initializerObject)
        {
            CampaignGameStarter gameStarter = (CampaignGameStarter)initializerObject;
            AddQuestBehaviors(gameStarter, false);
        }

        private static void AddQuestBehaviors(CampaignGameStarter gameStarter, bool isNewGame)
        {
            if (gameStarter != null)
            {
                gameStarter.AddBehavior(new RescueUliahBehavior(isNewGame));
                gameStarter.AddBehavior(new SaveCurrentQuestCampaignBehavior());
                gameStarter.AddBehavior(new SpawnNpcInLordsHallBecomeKnightBehavior()); // Ensure this is added
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
            AddClassDefinition(typeof(RescueUliahBehavior.RescueUliahQuest), 1);
            AddClassDefinition(typeof(SecondQuest), 2);
            AddClassDefinition(typeof(AnoritFindRelicsQuest), 3);
            AddClassDefinition(typeof(ThirdQuest), 4);
            AddClassDefinition(typeof(QuestCaravanPartyComponent), 5);
            AddClassDefinition(typeof(FourthQuest), 6);
            AddClassDefinition(typeof(FifthQuest), 7);
            AddClassDefinition(typeof(SixthQuest), 8);
            AddClassDefinition(typeof(SpawnNpcInLordsHallBecomeKnightBehavior), 20);
            AddClassDefinition(typeof(BecomeKnightQuest), 21); // Correctly registered
        }
    }
}