using RealmsForgotten.Quest.SecondUpdate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using RealmsForgotten.Quest.KnightQuest;
using RealmsForgotten.Quest.AI_Quest;

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
                gameStarter.AddBehavior(new SpawnNpcInLordsHallBecomeKnightBehavior());
                gameStarter.AddBehavior(new SpawnNpcInLordsHallBehavior());
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
            AddClassDefinition(typeof(BecomeKnightQuest), 21);
            AddClassDefinition(typeof(SpawnNpcInLordsHallBehavior.RetrieveArtifactQuest), 12);
        }
    }
}