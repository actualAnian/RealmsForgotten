using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.Core;


namespace RealmsForgotten.AiMade.TradePact
{
    public class TradePactSettlementMenuBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Option to talk to the settlement owner (Clan Leader or Governor) in a town
            starter.AddGameMenuOption(
                "town", // Applies to towns
                "talk_to_settlement_owner_town", // Unique ID for this option
                "{=talk_to_leader}Talk to the Leader", // Localized text for the option
                args =>
                {
                    // Condition: Only show if there's a leader/governor to talk to
                    args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                    // Check if there's a town governor OR a clan leader in the settlement
                    return Hero.MainHero.CurrentSettlement?.Town?.Governor != null || Hero.MainHero.CurrentSettlement?.OwnerClan?.Leader != null;
                },
                args =>
                {
                    Hero conversationHero = null;
                    // Prioritize governor if available, otherwise the clan leader
                    if (Hero.MainHero.CurrentSettlement?.Town?.Governor != null)
                    {
                        conversationHero = Hero.MainHero.CurrentSettlement.Town.Governor;
                    }
                    else if (Hero.MainHero.CurrentSettlement?.OwnerClan?.Leader != null)
                    {
                        conversationHero = Hero.MainHero.CurrentSettlement.OwnerClan.Leader;
                    }

                    if (conversationHero != null)
                    {
                        CampaignMapConversation.OpenConversation(
                            new ConversationCharacterData(Hero.MainHero.CharacterObject),
                            new ConversationCharacterData(conversationHero.CharacterObject)
                        );
                    }
                }
            );

            // Add a similar option for keeps or castles if you want to talk to leaders there
            starter.AddGameMenuOption(
                "keep", // Applies to keeps
                "talk_to_settlement_owner_keep", // Unique ID for this option
                "{=talk_to_leader}Talk to the Leader",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                    return Hero.MainHero.CurrentSettlement?.OwnerClan?.Leader != null;
                },
                args =>
                {
                    Hero conversationHero = Hero.MainHero.CurrentSettlement.OwnerClan.Leader;
                    if (conversationHero != null)
                    {
                        CampaignMapConversation.OpenConversation(
                            new ConversationCharacterData(Hero.MainHero.CharacterObject),
                            new ConversationCharacterData(conversationHero.CharacterObject)
                        );
                    }
                }
            );
        }
    }
}