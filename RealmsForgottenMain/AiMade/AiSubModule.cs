using HarmonyLib;
using RealmsForgotten.AiMade.Career;
using RealmsForgotten.AiMade.Managers.RealmsForgotten.AiMade.Managers;
using RealmsForgotten.AiMade.Models;
using RealmsForgotten.AiMade.Patches;
using RealmsForgotten.Behaviors;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using SandBox.GameComponents;
using RealmsForgotten.AiMade.Enlistement;
using static RealmsForgotten.AiMade.ADODReinforcementsSystem;
using System.Linq;

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
                AddCustomModels(campaignStarter);
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
            campaignGameStarter.AddBehavior(new DivineShieldStateBehavior());
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
            campaignGameStarter.AddBehavior(new DocksMenuBehavior());
            campaignGameStarter.AddBehavior(new MyModEnlistmentBehavior());
            campaignGameStarter.AddBehavior(new MyModEnlistmentBehaviorExtension());
            campaignGameStarter.AddBehavior(new MyModEnlistmentDialogBehavior());
            campaignGameStarter.AddBehavior(new KingsguardSaveDataBehavior());
            campaignGameStarter.AddBehavior(new RaceCraftingStaminaBehavior());
            campaignGameStarter.AddBehavior(new ADODChamberlainsBehavior());
            campaignGameStarter.AddBehavior(new SlaveBehavior());
            campaignGameStarter.AddBehavior(new ADODCustomLocationsBehavior());
            campaignGameStarter.AddBehavior(new NasorianHordeInvasion());
            campaignGameStarter.AddBehavior(new FirstTreeTempleLocation());

        }
        private void AddCustomModels(CampaignGameStarter campaignGameStarter)
        {
            // Register the custom inventory capacity model
            campaignGameStarter.AddModel(new CustomInventoryCapacityModel());
            campaignGameStarter.AddModel(new CustomBerserkerApplyDamageModel(new SandboxAgentApplyDamageModel()));
            campaignGameStarter.AddModel(new RFMapWeatherModel());
        }
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            if (mission != null)
            {
                if ((mission.Mode == MissionMode.Battle || mission.Mode == MissionMode.StartUp || mission.Mode == MissionMode.Conversation)
                    && mission.CombatType != Mission.MissionCombatType.ArenaCombat)
                {
                    mission.AddMissionBehavior(new CustomBerserkerBehavior());
                    mission.AddMissionBehavior(new ADODFireArrowsMissionBehavior());
                }

                if (mission?.MissionLogics?.OfType<CustomBattleAgentLogic>().Any() != true)
                {
                    if (mission?.MissionLogics?.OfType<SiegeDeploymentMissionController>().Any() != true)
                    {
                        if (mission?.MissionLogics?.OfType<DeploymentMissionController>().Any() == true)
                        {
                            mission.AddMissionBehavior(new ADODReinforcementsRunner());
                        }
                    }
                }

                mission.AddMissionBehavior(new FindMagicItemsMissionBehavior());
            }
        }
        public static void InitializeCareerSystem()
        {
            CareerInitialization.InitializeCareers();
        }
    }
}
