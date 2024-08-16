using HarmonyLib;
using RealmsForgotten.AiMade.Career;
using RealmsForgotten.AiMade.Managers;
using RealmsForgotten.AiMade.Managers.RealmsForgotten.AiMade.Managers;
using RealmsForgotten.AiMade.Models;
using RealmsForgotten.AiMade.Patches;
using RealmsForgotten.AiMade.Religions;
using RealmsForgotten.Behaviors;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade
{
    public class AiSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                var harmony = new Harmony("com.realmsforgotten.aimade");
                harmony.PatchAll();
                InformationManager.DisplayMessage(new InformationMessage("RealmsForgotten: Harmony patches applied successfully."));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"RealmsForgotten: Failed to apply Harmony patches. {ex.Message}"));
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (game.GameType is Campaign)
            {
                var campaignStarter = (CampaignGameStarter)gameStarterObject;
                AddCampaignBehaviors(campaignStarter);
            }
        }

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
            campaignGameStarter.AddBehavior(new BanditIncrease());
            campaignGameStarter.AddBehavior(new BanditPartyManager());

        }

        public static void InitializeCareerSystem()
        {
            CareerInitialization.InitializeCareers();
        }
    }
}
