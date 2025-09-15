using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using RealmsForgotten.Quest.FourthUpdate;

namespace RealmsForgotten.Quest
{
    
    public class EighthQuestBehavior : CampaignBehaviorBase
    {
        private readonly bool _isNewGame;

        public EighthQuestBehavior(bool isNewGame)
        {
            _isNewGame = isNewGame;
        }

        public override void RegisterEvents()
        {
            if (_isNewGame)
            {
                // No novo jogo, começamos a quest via HourlyTick só uma vez
                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickForNewGame);
            }

            // Após carregar save, rearma listeners da quest existente
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, AfterLoad);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // sem variáveis salvas aqui
        }

        /// <summary>
        /// Hora inicial de novo jogo: dispara a quest.
        /// </summary>
        private void HourlyTickForNewGame()
        {
            var qm = Campaign.Current.QuestManager;

            // só cria se ainda não existir
            if (!qm.Quests.Any(q => q is EighthQuest))
            {
                var quest = new EighthQuest("rf_eighth_quest", Hero.MainHero, CampaignTime.Never, 0);
                quest.StartQuest();

#if DEBUG
                InformationManager.DisplayMessage(
                    new InformationMessage("[EighthQuestBehavior] Nova quest iniciada.", Colors.Green));
#endif
            }
        }

        /// <summary>
        /// Após load, rearma listeners e hideout na quest ativa.
        /// </summary>
        private void AfterLoad()
        {
            var quest = Campaign.Current.QuestManager.Quests.FirstOrDefault(q => q is EighthQuest) as EighthQuest;
            if (quest != null)
            {
                quest.PostLoadRewire();

#if DEBUG
                InformationManager.DisplayMessage(
                    new InformationMessage("[EighthQuestBehavior] Post-load rewire executado.", Colors.Green));
#endif
            }
        }
    }
}
