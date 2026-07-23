using HarmonyLib;
using RealmsForgotten.AiMade.Career;
using RealmsForgotten.AiMade.Managers.RealmsForgotten.AiMade.Managers;
using RealmsForgotten.AiMade.Models;
using RealmsForgotten.AiMade.Patches;
using RealmsForgotten.AiMade.StrategicIntrigue.Campaign;
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
using System.Reflection;
using RealmsForgotten.AiMade.Village_Inn_Quests;
using RealmsForgotten.AiMade.Village_Inn_Quests.RealmsForgotten.AiMade.Village_Inn_Quests;
using RealmsForgotten.Chamberlain;
using RealmsForgotten.AiMade.Infect;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;


namespace RealmsForgotten.AiMade
{
    // LIFECYCLE OWNERSHIP (audit 2026-07-18): this class is NOT listed in
    // SubModule.xml, so the engine never instantiates it — its MBSubModuleBase
    // overrides were dead code that silently killed 6 models, 5 mission
    // behaviors and 2 manual patches for as long as they lived here. Everything
    // is now owned by RealmsForgotten.SubModule (behaviors via
    // AddCampaignBehaviors below; models in its OnGameStart; mission behaviors
    // in its OnMissionBehaviorInitialize; the join-encounter menu patch in its
    // OnGameInitializationFinished). NOTE: AgentVisualsDataMonsterFix was tried
    // there and reverted — it caused a native AccessViolation (see that method's
    // comment in SubModule.cs); it stays dead.
    //
    // DO NOT add this class to SubModule.xml and DO NOT re-add lifecycle
    // overrides here: with SubModule also registering, every behavior would be
    // added twice and every uncategorized Harmony patch would run under two ids.
    public class AiSubModule : MBSubModuleBase
    {
        public static void AddCampaignBehaviors(CampaignGameStarter campaignGameStarter)
        {
            // Initialize quest behaviors
            var customItemCategories = new CustomItemCategories();
            customItemCategories.Initialize();

            // Add other behaviors
            campaignGameStarter.AddBehavior(new StrategicIntrigueCampaignBehavior());
            campaignGameStarter.AddBehavior(new StrategicIntrigueConversationBehavior());
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
            campaignGameStarter.AddBehavior(new BanditMutualAidBehavior());
            campaignGameStarter.AddBehavior(new UndeadHordeBehavior());
            campaignGameStarter.AddBehavior(new BarbarianHordeInvasion());
            campaignGameStarter.AddBehavior(new ADODInnBehavior());
            campaignGameStarter.AddBehavior(new BanditIncrease());
            campaignGameStarter.AddBehavior(new BanditPartyManager());
            campaignGameStarter.AddBehavior(new DocksMenuBehavior());
            campaignGameStarter.AddBehavior(new RaceCraftingStaminaBehavior());
            campaignGameStarter.AddBehavior(new RFChamberlainsBehavior());
            campaignGameStarter.AddBehavior(new SlaveBehavior());
            campaignGameStarter.AddBehavior(new ADODCustomLocationsBehavior());
            campaignGameStarter.AddBehavior(new NasorianHordeInvasion());
            campaignGameStarter.AddBehavior(new FirstTreeTempleLocation());
            campaignGameStarter.AddBehavior(new AggressiveDwarfUrkhaiBehavior());
            campaignGameStarter.AddBehavior(new MineBehavior());
            campaignGameStarter.AddBehavior(new SturgiaCultureChangerBehavior());
            campaignGameStarter.AddBehavior(new AlignmentWarBehavior());
            campaignGameStarter.AddBehavior(new AlignmentMomentumBehavior());
            campaignGameStarter.AddBehavior(new AseraiCollectiveDefenseBehavior());
            campaignGameStarter.AddBehavior(new MilitaryAidDiplomacyBehavior());
            // Heavy campaign AI tracing is disabled during normal play because it generates large logs and noticeable campaign-map lag.
            // campaignGameStarter.AddBehavior(new RFCampaignAITraceBehavior());
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
            campaignGameStarter.AddBehavior(new RFJoinRaidEncounterBehavior());
            //campaignGameStarter.AddBehavior(new CommanderDefenseBehavior());
        }
        // Models moved to RealmsForgotten.SubModule.OnGameStart (this method never
        // ran — see the class comment). Chain order matters there:
        // Urkhai before RFPartySizeLimitModel; RFDiplomacy before Alignment.
        // CustomTradeItemPriceFactorModel is now revived too (reworked to extend
        // the Default model so it composes with the Homesteads discount patch).
        // SpearAwareBattleSpawnModel stays off as before.
        // Mission behaviors moved to RealmsForgotten.SubModule.OnMissionBehaviorInitialize
        // (this class's override never ran — see the class comment). Two were left
        // out on purpose: InfectionMissionBehavior (attached lazily by
        // RealmsForgottenInfectPatch.EnsureOn) and CommanderSwapMissionBehavior
        // (parent CommanderDefenseBehavior is deliberately disabled).

        private static bool _menuPatchApplied = false;

        internal static void ApplyDelayedJoinEncounterPatch()
        {
            if (_menuPatchApplied)
                return;

            try
            {
                var harmony = new Harmony("com.realmsforgotten.aimade.delayed.joinencounter");

                harmony.Patch(
                    AccessTools.Method(
                        typeof(EncounterGameMenuBehavior),
                        "game_menu_join_encounter_help_attackers_on_condition"
                    ),
                    prefix: new HarmonyMethod(
                        typeof(RFHideVanillaJoinEncounterHelpOptionsPatch),
                        nameof(RFHideVanillaJoinEncounterHelpOptionsPatch
                            .HideVanillaHelpAttackersInVillageRaid)
                    )
                );

                harmony.Patch(
                    AccessTools.Method(
                        typeof(EncounterGameMenuBehavior),
                        "game_menu_join_encounter_help_defenders_on_condition"
                    ),
                    prefix: new HarmonyMethod(
                        typeof(RFHideVanillaJoinEncounterHelpOptionsPatch),
                        nameof(RFHideVanillaJoinEncounterHelpOptionsPatch
                            .HideVanillaHelpDefendersInVillageRaid)
                    )
                );
                harmony.Patch(
                    AccessTools.Method(
                        typeof(EncounterGameMenuBehavior),
                        "game_menu_join_encounter_leave_no_army_on_condition"
                    ),
                    prefix: new HarmonyMethod(
                        typeof(RFJoinEncounterLeaveTextPatch),
                        nameof(RFJoinEncounterLeaveTextPatch
                            .JoinEncounterLeaveConditionPrefix)
                    )
                );

                _menuPatchApplied = true;

                InformationManager.DisplayMessage(
                    new InformationMessage("RF: Join Encounter menu patch applied safely.")
                );
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"RF ERROR applying menu patch: {ex.Message}", Colors.Red)
                );
            }
        }
    }
}
