using RealmsForgotten.AiMade.Career;
using RealmsForgotten.AiMade.Models;
using RealmsForgotten.AiMade.Patches;
using RealmsForgotten.AiMade.Religions;
using RealmsForgotten.Behaviors;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;

namespace RealmsForgotten.AiMade
{
    public static class AiSubModule
    {
        public static readonly Dictionary<string, InputKey> PossibleKeys = new();
        
        public static void AddCampaignBehaviors(CampaignGameStarter campaignGameStarter)
        {
            // Initialize quest behaviors
           
            var customItemCategories = new RealmsForgotten.Behaviors.CustomItemCategories();
            customItemCategories.Initialize();

            // Add quest behaviors
          
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
            campaignGameStarter.AddBehavior(new AggressiveSturgiaBehavior());
            campaignGameStarter.AddBehavior(new HumanCohesionBehavior());
            campaignGameStarter.AddBehavior(new BanditPartyGrowthBehavior());
            campaignGameStarter.AddBehavior(new BanditHordeBehavior());
            campaignGameStarter.AddBehavior(new UndeadHordeBehavior());
            campaignGameStarter.AddBehavior(new BarbarianHordeInvasion());
            campaignGameStarter.AddBehavior(new ADODInnBehavior());
        }

        public static void InitializeCareerSystem()
        {
            CareerInitialization.InitializeCareers();
        }
    }
}
