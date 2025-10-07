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
using static RealmsForgotten.AiMade.ADODReinforcementsSystem;
using System.Linq;
using RealmsForgotten.AiMade.RF_Diplomacy;
using RealmsForgotten.AiMade.MercenaryFaction;
using Bannerlord.UIExtenderEx;
using RealmsForgotten.AiMade.TradePact;
using RealmsForgotten.AiMade.CustomOrderofBattle;
using RealmsForgotten.AiMade.Village_Inn_Quests;
using RealmsForgotten.AiMade.Village_Inn_Quests.RealmsForgotten.AiMade.Village_Inn_Quests;
using SandBox.Missions.MissionLogics;


namespace RealmsForgotten.AiMade
{
    public class AiSubModule : MBSubModuleBase
    {
        private UIExtender _extender;
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
            campaignGameStarter.AddBehavior(new MerchantDeliveryBehavior());
            campaignGameStarter.AddBehavior(new StorytellerBehavior());
            campaignGameStarter.AddBehavior(new ListeningToStoryBehavior());
            campaignGameStarter.AddBehavior(new BanditDefeatChivalryBehavior());
            campaignGameStarter.AddBehavior(new DivineShieldStateBehavior());
            campaignGameStarter.AddBehavior(new BattleCryStateBehavior());
            campaignGameStarter.AddBehavior(new VisitLibrary());
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
            campaignGameStarter.AddBehavior(new RaceCraftingStaminaBehavior());
            //campaignGameStarter.AddBehavior(new ADODChamberlainsBehavior()); @TODO
            campaignGameStarter.AddBehavior(new SlaveBehavior());
            campaignGameStarter.AddBehavior(new ADODCustomLocationsBehavior());
            campaignGameStarter.AddBehavior(new NasorianHordeInvasion());
            campaignGameStarter.AddBehavior(new FirstTreeTempleLocation());
            campaignGameStarter.AddBehavior(new AggressiveDwarfUrkhaiBehavior());
            campaignGameStarter.AddBehavior(new MineBehavior());
            campaignGameStarter.AddBehavior(new SturgiaCultureChangerBehavior());
            campaignGameStarter.AddBehavior(new AlignmentWarBehavior());
            campaignGameStarter.AddBehavior(new AlignmentMomentumBehavior());
            //campaignGameStarter.AddBehavior(new TickProfilerBehavior());
            //campaignGameStarter.AddBehavior(new RFSnowBattleSceneBehavior());
            //campaignGameStarter.AddBehavior(new EncounterSystemBehavior());
            //campaignGameStarter.AddBehavior(new ALordDialogueCampaignBehavior());
            //campaignGameStarter.AddBehavior(new ARandomEncountersBehavior());
            //campaignGameStarter.AddBehavior(new PendingDuelMissionBehavior());
            //campaignGameStarter.AddBehavior(new DuelChallengeDialogueBehavior());
            campaignGameStarter.AddBehavior(new CapitulationSystemBehavior());
            campaignGameStarter.AddBehavior(new MercenaryHireBehavior());
            campaignGameStarter.AddBehavior(new CaravanTradePactBehavior());
            campaignGameStarter.AddBehavior(new EconomicPactBehavior());
            campaignGameStarter.AddBehavior(new RFWeatherCampaignBehavior());
            campaignGameStarter.AddBehavior(new AIBreakInBehavior());
            campaignGameStarter.AddBehavior(new VassalPromotionBehavior());
            campaignGameStarter.AddBehavior(new MercenaryFactionWarPactBehavior());
            campaignGameStarter.AddBehavior(new ConsulHallRecruitmentBehavior());
            campaignGameStarter.AddBehavior(new VillageInnNPCBehavior());
            campaignGameStarter.AddBehavior(new PeregrinQuestBehavior());
            campaignGameStarter.AddBehavior(new WerewolfVillageMenuBehavior());
            campaignGameStarter.AddBehavior(new RuinsQuestBehavior());
        }
        private void AddCustomModels(CampaignGameStarter campaignGameStarter)
        {
            // Register the custom inventory capacity model
            campaignGameStarter.AddModel(new UrkhaiPartySizeModel());
            campaignGameStarter.AddModel(new AlignmentDiplomacyModel(Campaign.Current.Models.DiplomacyModel));
            campaignGameStarter.AddModel(new CustomTradeItemPriceFactorModel());
            //campaignGameStarter.AddModel(new SpearAwareBattleSpawnModel());
            campaignGameStarter.AddModel(new RFDiplomacyModel());
        }
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            if (mission == null)
                return;

            // Add Custom Berserker Behavior for specific mission modes
            if ((mission.Mode == MissionMode.Battle || mission.Mode == MissionMode.StartUp || mission.Mode == MissionMode.Conversation)
                && mission.CombatType != Mission.MissionCombatType.ArenaCombat)
            {
                var berserkerBehavior = new CustomBerserkerBehavior();
                mission.AddMissionBehavior(berserkerBehavior);

                //mission.AddMissionBehavior(new ForceWinterMissionBehavior());
                mission.AddMissionBehavior(new ADODFireArrowsMissionBehavior());
                //mission.AddMissionBehavior(new AttachWallSegmentDebugBehavior()); @TODO
            }

            // Add Reinforcements Runner if DeploymentMissionController is present
            if (mission.MissionLogics.OfType<DeploymentMissionController>().Any()
                && !mission.MissionLogics.OfType<CustomBattleAgentLogic>().Any()
                && !mission.MissionLogics.OfType<SiegeDeploymentMissionController>().Any())
            {
                mission.AddMissionBehavior(new ADODReinforcementsRunner());
            }

            if (mission.Scene != null
                 && mission.CombatType == Mission.MissionCombatType.Combat
                 && Mission.Current?.HasMissionBehavior<CampaignMissionComponent>() == true)
            {
                mission.AddMissionBehavior(new AutoOOBConfigMissionBehavior()); // optional but nice for OOB cards
                mission.AddMissionBehavior(new InfantrySpearSorterOnSpawn());   // the actual splitter using SpawnEquipment
            }

            // Add Find Magic Items behavior to all missions
            mission.AddMissionBehavior(new FindMagicItemsMissionBehavior());
            

        }
    }

}
