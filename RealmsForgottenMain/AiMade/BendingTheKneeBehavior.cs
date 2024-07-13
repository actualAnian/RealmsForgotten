using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade
{
    public class KeepItemsAfterBattleBehavior : CampaignBehaviorBase
    {
        private int _goldRequirementForFealty;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, InitializeDialogue);
        }

        private void InitializeDialogue(CampaignGameStarter starter)
        {
            ConfigureClanLeaderDialogues(starter);
        }

        private void ConfigureClanLeaderDialogues(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("StartConversation", "hero_main_options", "BeginRecruitmentDialogue",
                "The winds of change sweep over the world. Bend the knee and pledge fealty to me as your Sovereign, or your House will burn.",
                IsRecruitmentPossible, null, 200);

            starter.AddDialogLine("AcceptanceLine", "BeginRecruitmentDialogue", "lord_pretalk",
                "Your cause is just, and your will, strong. I pledge fealty to you and your kin, from this day until my last day. Long may your grace reign.",
                HeroFavorsPlayer, JoinPlayerKingdom, 100);

            starter.AddDialogLine("RejectionLine", "BeginRecruitmentDialogue", "lord_pretalk",
                "Then my House will burn! I will never kneel to you, craven!",
                HeroOpposesPlayer, null, 90);

            starter.AddDialogLine("FinancialAidRequest", "BeginRecruitmentDialogue", "ConfirmFinancialAidDialogue",
                "I am ready to swear fealty to you, your grace, but first, I must secure provisions for my men. A chest of Gold Dragons should suffice.",
                null, null, 80);

            ConfigureFinancialAidDialogues(starter);
        }

        private void ConfigureFinancialAidDialogues(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("ProvideAid", "ConfirmFinancialAidDialogue", "AidAcceptedDialogue",
                "The Gold is yours. Together, we shall unite the world.", null, null, 100, CheckGoldAvailability);

            starter.AddPlayerLine("DenyAid", "ConfirmFinancialAidDialogue", "AidDeniedDialogue",
                "A high price for loyalty. We shall see if the other Houses of my Realm agree.",
                PlayerLacksGold, null, 90);

            starter.AddDialogLine("AidProvided", "AidAcceptedDialogue", "lord_pretalk",
                "My loyalty is yours. To victory, in your name, your grace.", null, JoinPlayerKingdom, 100);

            starter.AddDialogLine("AidDenied", "AidDeniedDialogue", "lord_pretalk",
                "Very well, we will meet again when fortunes favor you. Farewell.", null, null, 100);
        }

        private bool IsRecruitmentPossible()
        {
            var conversationHero = Hero.OneToOneConversationHero;
            if (conversationHero?.Clan?.Kingdom != null || Hero.MainHero.Clan.Kingdom?.Leader != Hero.MainHero)
            {
                return false;
            }

            _goldRequirementForFealty = 8000 * conversationHero.Clan.Tier;
            return true;
        }

        private bool HeroFavorsPlayer() => Hero.OneToOneConversationHero.GetRelationWithPlayer() >= 30f;

        private bool HeroOpposesPlayer() => Hero.OneToOneConversationHero.GetRelationWithPlayer() <= -40f;

        private bool CheckGoldAvailability(out TextObject explanation)
        {
            explanation = new TextObject($"{_goldRequirementForFealty} gold needed");
            return Hero.MainHero.Gold >= _goldRequirementForFealty;
        }

        private bool PlayerLacksGold() => Hero.MainHero.Gold < _goldRequirementForFealty;

        private void JoinPlayerKingdom()
        {
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, _goldRequirementForFealty, false);
            ChangeKingdomAction.ApplyByJoinToKingdom(Hero.OneToOneConversationHero.Clan, Hero.MainHero.Clan.Kingdom, true);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
