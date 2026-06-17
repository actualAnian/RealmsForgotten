using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Adventurer
{
    public class DuelChallengeDialogueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Lord opens challenge
            starter.AddDialogLine("rf_duel_challenge_start", "start", "rf_duel_challenge_response",
                "{=rf_duel_greeting}You think you can walk these streets without answering for your arrogance?",
                () => Hero.OneToOneConversationHero != null &&
                      Hero.OneToOneConversationHero.IsLord &&
                      !Hero.OneToOneConversationHero.IsFriend(Hero.MainHero),
                null);

            // Player accepts
            starter.AddPlayerLine("rf_duel_accept", "rf_duel_challenge_response", "close_window",
                "{=rf_duel_accept}I’ll take you on, right here and now.",
                null,
                () =>
                {
                    Hero target = Hero.OneToOneConversationHero;
                    if (target != null)
                    {
                        var behavior = Campaign.Current.GetCampaignBehavior<PendingDuelMissionBehavior>();
                        behavior.QueueDuel(target);
                    }
                });

            // Player declines
            starter.AddPlayerLine("rf_duel_decline", "rf_duel_challenge_response", "close_window",
                "{=rf_duel_decline}Not today. I’ve got better things to do.",
                null,
                () =>
                {
                    InformationManager.DisplayMessage(new InformationMessage("You chose to walk away... for now."));
                });
        }
    }
}
