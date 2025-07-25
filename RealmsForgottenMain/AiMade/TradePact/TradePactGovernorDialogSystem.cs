using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.TradePact
{
    public class TradePactDialogBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No data to sync in dialog behavior, but good to keep for consistency.
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Generic greeting for any noble you talk to in a settlement
            starter.AddDialogLine(
                "trade_pact_greeting_generic", // New unique ID
                "start", // From game start state
                "trade_pact_propose_pact_state", // To our custom state
                "{=trade_pact_greeting}Greetings, {CONVERSATION_HERO.NAME}. What brings you to my {SETTLEMENT.NAME}?", // More generic
                () =>
                {
                    // Condition: Ensure we are talking to a hero who is a clan leader or governor
                    var hero = Hero.OneToOneConversationHero;
                    var settlement = Hero.MainHero.CurrentSettlement;
                    return settlement != null && hero != null &&
                           (settlement.Town?.Governor == hero || settlement.OwnerClan?.Leader == hero);
                },
                null, // No on-enter action
                100 // Priority to ensure it's checked early
            );

            // Player line to propose a trade pact
            starter.AddPlayerLine(
                "trade_pact_propose_pact_player_line", // New unique ID
                "trade_pact_propose_pact_state", // From our custom state
                "trade_pact_response_pact_state", // To the response state
                "{=trade_pact_player_proposal}I'd like to propose a trade pact between our kingdoms.",
                () =>
                {
                    // Condition: Both main hero and conversation hero must be part of a kingdom, and not the same kingdom.
                    var myKingdom = Hero.MainHero.MapFaction as Kingdom;
                    var otherKingdom = Hero.OneToOneConversationHero?.MapFaction as Kingdom;
                    return myKingdom != null && otherKingdom != null && myKingdom != otherKingdom;
                },
                null // No on-enter action
            );

            // Governor/Leader accepts the pact
            starter.AddDialogLine(
                "trade_pact_accept_pact_dialog_line", // New unique ID
                "trade_pact_response_pact_state", // From the response state
                "close_window", // Closes the dialogue
                "{=trade_pact_accepted}A wise idea. I will inform my liege. Let prosperity grow between us.",
                () =>
                {
                    // Condition: This line always shows if we reach this state and the conditions for the player line were met.
                    // No additional conditions needed here.
                    return true;
                },
                () =>
                {
                    // On-enter action: Establish the trade pact
                    var myKingdom = Hero.MainHero.MapFaction as Kingdom;
                    var otherKingdom = Hero.OneToOneConversationHero?.MapFaction as Kingdom;

                    if (myKingdom != null && otherKingdom != null)
                    {
                        var behavior = Campaign.Current.GetCampaignBehavior<TradePactCampaignBehavior>();

                        if (!behavior.HasTradePact(myKingdom, otherKingdom))
                        {
                            behavior.AddTradePact(myKingdom, otherKingdom);

                            InformationManager.DisplayMessage(new InformationMessage(
                                $"Trade pact established between {myKingdom.Name} and {otherKingdom.Name}."));
                        }
                    }
                }
            );

            // Governor/Leader rejects the pact (optional, but good for completeness)
            starter.AddDialogLine(
                "trade_pact_reject_pact_dialog_line", // New unique ID
                "trade_pact_response_pact_state", // From the response state
                "close_window", // Closes the dialogue
                "{=trade_pact_rejected}I am not interested in such an arrangement at this time.",
                () =>
                {
                    // Condition: This line will currently never show because the accept line always returns true.
                    // You would need more complex logic here for actual rejection (e.g., relations, ongoing wars, etc.).
                    return false;
                },
                null
            );
        }
    }
}