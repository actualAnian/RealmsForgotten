
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RealmsForgotten.Behaviors
{
    public class MyModEnlistmentDialog
    {
        public void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            // Add a player line to ask for enlistment in the correct context (e.g., talking to a lord).
            campaignGameStarter.AddPlayerLine(
                "enlistment_start",             // Unique ID for this dialogue option
                "lord_talk",                    // The existing conversation state where this line will be available
                "enlistment_offer",             // The state to transition to after selecting this option
                "{=enlistment_line}I would like to enlist in your party.", // Dialogue text
                IsEnlistmentPossible,           // Condition to check if enlistment is possible
                null                            // No need for additional action here (we handle it in the response)
            );

            // Add a lord's response to the enlistment offer
            campaignGameStarter.AddDialogLine(
                "enlistment_offer_response",     // Unique ID for the lord's response
                "enlistment_offer",              // State this response is tied to (after the player selects the enlistment option)
                "close_window",                  // What happens after the response (in this case, close the conversation)
                "{=enlistment_offer_response}Very well, you can join my party. We will find a place for you.", // Response text
                null,                            // No condition needed for this response
                OfferEnlistment                 // Action to perform when the player enlists
            );
        }

        // Condition to check if the enlistment dialogue option should be shown
        private bool IsEnlistmentPossible()
        {
            MyModEnlistmentBehavior enlistmentBehavior = Campaign.Current.GetCampaignBehavior<MyModEnlistmentBehavior>();
            return !enlistmentBehavior.IsEnlisted // Now using IsEnlisted instead of IsPlayerEnlisted
                   && Hero.OneToOneConversationHero != null
                   && Hero.OneToOneConversationHero.IsLord
                   && !Hero.OneToOneConversationHero.IsPlayerCompanion;
        }

        // Logic to handle the enlistment process
        private void OfferEnlistment()
        {
            Hero playerHero = Hero.MainHero;
            Hero lordHero = Campaign.Current.ConversationManager.OneToOneConversationHero;
            MyModEnlistmentBehavior enlistmentBehavior = Campaign.Current.GetCampaignBehavior<MyModEnlistmentBehavior>();

            if (enlistmentBehavior != null && enlistmentBehavior.EnlistPlayer(lordHero))
            {
                InformationManager.DisplayMessage(new InformationMessage($"You have enlisted in {lordHero.Name}'s party."));
            }
        }
    }
}