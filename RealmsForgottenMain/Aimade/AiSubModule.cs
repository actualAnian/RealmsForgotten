using RealmsForgotten.AiMade.Career;
using RealmsForgotten.AiMade.Models;
using RealmsForgotten.AiMade.Religions;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade
{
    public static class AiSubModule
    {
        public static void AddCampaignBehaviors(CampaignGameStarter campaignGameStarter)
        {
            // Initialize quest behaviors
            var ceremonyQuestBehavior = new CeremonyQuestBehavior();
            var processionEscortQuestBehavior = new ProcessionEscortQuestBehavior();
            var priestCampaignBehavior = new PriestCampaignBehavior(ceremonyQuestBehavior, processionEscortQuestBehavior);

            // Add quest behaviors
            campaignGameStarter.AddBehavior(ceremonyQuestBehavior);
            campaignGameStarter.AddBehavior(processionEscortQuestBehavior);
            campaignGameStarter.AddBehavior(priestCampaignBehavior);

            // Add other behaviors
            campaignGameStarter.AddBehavior(new MercenaryOfferBehavior());
            campaignGameStarter.AddBehavior(new HouseTroopsTownsBehavior());
            campaignGameStarter.AddBehavior(new CultureAppropriateTroopsBehavior());
            campaignGameStarter.AddBehavior(new MaestersTowerBehavior());
            campaignGameStarter.AddBehavior(new TavernRecruitmentBehavior());
            campaignGameStarter.AddBehavior(new HouseTroopsCastleBehavior());
            campaignGameStarter.AddBehavior(new HelpPeregrineBehavior());
            campaignGameStarter.AddBehavior(new MerchantEventBehavior());
            campaignGameStarter.AddBehavior(new StorytellerBehavior());
            campaignGameStarter.AddBehavior(new ListeningToStoryBehavior());
            campaignGameStarter.AddBehavior(new RaidLootBonusBehavior());
            campaignGameStarter.AddBehavior(new DefendVillagersOrCaravansBehavior());
            campaignGameStarter.AddBehavior(new QuestCompletionBehavior());
            campaignGameStarter.AddBehavior(new BanditDefeatChivalryBehavior());
            campaignGameStarter.AddBehavior(new DivineShieldStateBehavior()); // Add the state behavior here
            campaignGameStarter.AddBehavior(new BattleCryStateBehavior());
            campaignGameStarter.AddBehavior(new VisitLibrary());
            campaignGameStarter.AddBehavior(new CareerProgressionBehavior());
            campaignGameStarter.AddBehavior(new BanditHideoutClearedBehavior());
        }

        public static void InitializeCareerSystem()
        {
            CareerInitialization.InitializeCareers();
        }
    }
}
