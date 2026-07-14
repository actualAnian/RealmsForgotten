using System;
using HarmonyLib;
using Homesteads.Models;
using Homesteads.Patches;
using Homesteads.Patches.CrashFixes;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Homesteads;

public class HomesteadsReloaded : MBSubModuleBase
{
	public const string ModuleId = "HomesteadsReloaded";

	public const string ModuleDisplayName = "Homesteads Reloaded";

	private const bool EnableAiVisitSettlementPatch = true;

	private const bool EnableMapTracksCrashPatch = true;

	private const bool EnableFunctioningPatches = true;

	private const bool EnableMapAppearancePatches = true;

	private const bool EnableMapAppearanceModelPatch = false;

	private const bool EnableMapAppearanceNameplatePatch = true;

	private const bool EnableMapAppearancePartyTrackerPatch = true;

	private const bool EnableMenuBackgroundPatch = false;

	private const bool EnableInteractPatch = true;

	private const bool EnableStopMovingPatches = true;

	private const bool EnableBattleMissionPatch = true;

	private const bool EnableFieldKitchenHearthPatch = true;

	private const bool EnableSaveLoadRescuePatch = false;

	private bool isInitialized;

	private bool isLoaded;

	private bool _lateAgentPatchesApplied;

	public static MCMSettings Settings { get; private set; }

	public static string ModName { get; private set; } = "Homesteads Reloaded";

	protected override void OnSubModuleLoad()
	{
		try
		{
			base.OnSubModuleLoad();
			TraceLogger.StartSession("OnSubModuleLoad");
			TraceLogger.Write("HomesteadsReloaded", "OnSubModuleLoad entered");
			if (isInitialized)
			{
				TraceLogger.Write("HomesteadsReloaded", "OnSubModuleLoad skipped because module is already initialized");
				return;
			}
			Harmony harmony = new Harmony("Bannerlord.Windwhistle.HomesteadsReloaded");
			TraceLogger.Write("HomesteadsReloaded", "Created Harmony instance " + harmony.Id);
			PatchSelectedHarmonyClasses(harmony);
			isInitialized = true;
			TraceLogger.Write("HomesteadsReloaded", "OnSubModuleLoad completed");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadsReloaded", "OnSubModuleLoad exception: " + ex);
			Utils.PrintDebugMessage($"{ModName}: OnSubModuleLoad threw exception {ex}", 255f, 80f, 80f);
		}
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		try
		{
			base.OnBeforeInitialModuleScreenSetAsRoot();
			TraceLogger.Write("HomesteadsReloaded", "OnBeforeInitialModuleScreenSetAsRoot entered");
			if (isLoaded)
			{
				TraceLogger.Write("HomesteadsReloaded", "OnBeforeInitialModuleScreenSetAsRoot skipped because module is already loaded");
				return;
			}
			ModName = "Homesteads Reloaded";
			TraceLogger.Write("HomesteadsReloaded", "Resolved module name to " + ModName);
			Settings = GlobalSettings<MCMSettings>.Instance ?? throw new NullReferenceException("Settings are null");
			TraceLogger.Write("HomesteadsReloaded", "Loaded MCM settings instance");
			Utils.PrintDebugMessage(ModName + " Loaded.", 80f, 255f, 80f);
			isLoaded = true;
			TraceLogger.Write("HomesteadsReloaded", "OnBeforeInitialModuleScreenSetAsRoot completed");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadsReloaded", "OnBeforeInitialModuleScreenSetAsRoot exception: " + ex);
			Utils.PrintDebugMessage($"{ModName}: OnBeforeInitialModuleScreenSetAsRoot threw exception {ex}", 255f, 80f, 80f);
		}
	}

	public override void RegisterSubModuleObjects(bool isSavedCampaign)
	{
		base.RegisterSubModuleObjects(isSavedCampaign);
		try
		{
			if (isSavedCampaign && MBObjectManager.Instance != null && Campaign.Current != null)
			{
				HomesteadSettlementBuilder.ReregisterFromSaveFile();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadsReloaded", "RegisterSubModuleObjects re-register failed: " + ex.Message);
		}
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarter)
	{
		base.OnGameStart(game, gameStarter);
		TraceLogger.Write("HomesteadsReloaded", "OnGameStart entered for " + (game.GameType?.GetType().FullName ?? "unknown game type"));
		if (game.GameType is Campaign)
		{
			CampaignGameStarter obj = (CampaignGameStarter)gameStarter;
			obj.AddBehavior(new HomesteadBehavior());
			obj.AddBehavior(new HomesteadSettlementBehavior());
			obj.AddBehavior(new RFNomadKingdomBehavior());
			TraceLogger.Write("HomesteadsReloaded", "Registered HomesteadBehavior + HomesteadSettlementBehavior + RFNomadKingdomBehavior for campaign game");
		}
		ApplyLateAgentPatches();
	}

	protected override void OnApplicationTick(float dt)
	{
		base.OnApplicationTick(dt);
		HomesteadBehavior.Instance?.OnApplicationTick(dt);
	}

	private static void PatchSelectedHarmonyClasses(Harmony harmony)
	{
		TraceLogger.Write("HomesteadsReloaded", $"Patch toggles - AiVisit: {true}, MapTracksCrash: {true}, Functioning: {true}, MapAppearance: {true}, MapModel: {false}, MapNameplate: {true}, MapTracker: {true}, MenuBackground: {false}, Interact: {true}, StopMoving: {true}, BattleMission: {true}, SaveLoadRescue: {false}");
		PatchClassSafe(harmony, typeof(HomesteadHwpRemoveHeroDiagnosticPatch));
		PatchClassSafe(harmony, typeof(HomesteadStayingInSettlementDiagnosticPatch));
		PatchClassSafe(harmony, typeof(HomesteadChangeDefaultBuildingDiagPatch));
		PatchClassSafe(harmony, typeof(HomesteadPartyBannerPatch));
		PatchClassSafe(harmony, typeof(HomesteadAiVisitSettlementCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadMapTracksCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadCaravanSpawnCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadPrisonerEscapePatch));
		PatchClassSafe(harmony, typeof(HomesteadUsableMachineStandingPointCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadMapScenePatch));
		PatchClassSafe(harmony, typeof(HomesteadSettlementEntryCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadGetAllPropsCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadMapCameraInputPatch));
		PatchClassSafe(harmony, typeof(HomesteadSettlementOverlayCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadTownDailyTickCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadVillageDailyTickCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadDailyTickSettlementDispatchCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadHourlyTickSettlementDispatchCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadCraftingOrderCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadCraftingCampaignBehaviorCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadVillageNameplateCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadTavernPassagePatch));
		PatchClassSafe(harmony, typeof(HomesteadForgeOrdersPatch));
		PatchClassSafe(harmony, typeof(HomesteadForgeCulturePatch));
		PatchClassSafe(harmony, typeof(HomesteadForgeCompleteOrderPatch));
		PatchClassSafe(harmony, typeof(HomesteadForgeCraftNamePatch));
		PatchClassSafe(harmony, typeof(HomesteadRaiderAiEngagePatch));
		PatchClassSafe(harmony, typeof(HomesteadRaiderAiVisitPatch));
		PatchClassSafe(harmony, typeof(HomesteadRaiderAiMilitaryPatch));
		PatchClassSafe(harmony, typeof(HomesteadRaiderAiPatrollingPatch));
		PatchClassSafe(harmony, typeof(HomesteadDesertionPatch));
		PatchClassSafe(harmony, typeof(HomesteadPartySizePatch));
		PatchClassSafe(harmony, typeof(HomesteadPartyScreenTalkGuardPatch));
		PatchClassSafe(harmony, typeof(HomesteadMedicalMoralePatch));
		PatchClassSafe(harmony, typeof(HomesteadMedicalHealingPatch));
		PatchClassSafe(harmony, typeof(HomesteadPatrolSpeedPatch));
		PatchClassSafe(harmony, typeof(HomesteadTradeDiscountPatch));
		PatchClassSafe(harmony, typeof(HomesteadFieldKitchenHearthPatch));
		PatchClassSafe(harmony, typeof(HomesteadMapAppearanceNameplatePatch));
		PatchClassSafe(harmony, typeof(HomesteadMapAppearancePartyTrackerPatch));
		PatchClassSafe(harmony, typeof(HomesteadMapAppearancePartyTrackerTickPatch));
		PatchClassSafe(harmony, typeof(HomesteadMapAppearancePartyTrackerRefreshPatch));
		PatchClassSafe(harmony, typeof(InteractWithHomesteadPartyPatch));
		PatchClassSafe(harmony, typeof(SkipHomesteadMeetingPatch));
		PatchClassSafe(harmony, typeof(HomesteadEncounterInitPatch));
		PatchClassSafe(harmony, typeof(DisableVanillaInspectTroopsPatch));
		PatchClassSafe(harmony, typeof(StopSparringConversationPatch));
		DSPlusHomesteadMaskPatch.TryApply(harmony);
		NavalDLCCrashFixPatch.TryApply(harmony);
		HomesteadNavalDLCCompatibilityPatch.TryApply(harmony);
		PatchClassSafe(harmony, typeof(HomesteadMapPlacementClickPatch));
		PatchClassSafe(harmony, typeof(StopHomesteadPartyMovingDefaultBehaviorPatch));
		PatchClassSafe(harmony, typeof(StopHomesteadPartyMovingShortTermBehaviorPatch));
		PatchClassSafe(harmony, typeof(HomesteadMissionStateOpenNewPatch));
		PatchClassSafe(harmony, typeof(HomesteadSandboxBattleMissionRecordPatch));
		PatchClassSafe(harmony, typeof(HomesteadSandboxBattleMissionStringPatch));
		PatchClassSafe(harmony, typeof(HomesteadCampaignBattleMissionRecordPatch));
		PatchClassSafe(harmony, typeof(HomesteadCampaignBattleMissionStringPatch));
		PatchClassSafe(harmony, typeof(HomesteadSandboxCaravanBattleMissionPatch));
		PatchClassSafe(harmony, typeof(HomesteadCampaignCaravanBattleMissionPatch));
		PatchClassSafe(harmony, typeof(HomesteadBattleSimulationPowerPatch));
		PatchClassSafe(harmony, typeof(HomesteadDeploymentBoundaryCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadNotableSpawnPatch));
		PatchClassSafe(harmony, typeof(HomesteadConversationCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadNotableSpawnPointCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadWorkshopSignCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadGarrisonInitializeCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadClanPartyItemCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadSettlementIntroTextCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadTownMenuInitCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadTownManagementRefreshCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadSettlementOwnerCultureLoyaltyPatch));
		PatchClassSafe(harmony, typeof(HomesteadSettlementGovernorCultureLoyaltyPatch));
		PatchClassSafe(harmony, typeof(HomesteadNameMarkerCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadIncidentCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadSpawnNotablesSkipPatch));
		PatchClassSafe(harmony, typeof(HomesteadArenaMasterConvoCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadPatrolTalkCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadGarrisonPartyOwnerCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadGarrisonPartySizeLimitCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadPatrolPartySizeLimitCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadMobilePartyMapFactionCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadAiVisitSettlementDictionaryCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadAiVisitSettlementRefreshCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadSettlementVisualCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadWorkshopProductionSpeedCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadRunTownWorkshopCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadPartyTrainingExperienceCrashPatch));
		PatchClassSafe(harmony, typeof(GhostSettlementCleanupPatch));
		PatchClassSafe(harmony, typeof(TownPreAfterLoadPatch));
		PatchClassSafe(harmony, typeof(HomesteadClanFortificationRemovedCrashPatch));
		PatchClassSafe(harmony, typeof(HomesteadCompanionLimitPatch));
	}

	private void ApplyLateAgentPatches()
	{
		if (_lateAgentPatchesApplied)
		{
			return;
		}
		_lateAgentPatchesApplied = true;
		try
		{
			Harmony harmony = new Harmony("Bannerlord.Windwhistle.HomesteadsReloaded.LateAgent");
			TraceLogger.Write("HomesteadsReloaded", "Created late-Agent Harmony instance " + harmony.Id);
			PatchClassSafe(harmony, typeof(HomesteadBallistaFriendlyFirePatch));
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadsReloaded", $"ApplyLateAgentPatches failed: {arg}");
		}
	}

	private static void PatchClassSafe(Harmony harmony, Type patchType)
	{
		try
		{
			TraceLogger.Write("HomesteadsReloaded", "Patching " + patchType.Name);
			harmony.CreateClassProcessor(patchType).Patch();
			TraceLogger.Write("HomesteadsReloaded", "Patched " + patchType.Name + " successfully");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadsReloaded", $"Failed to patch {patchType.Name}: {ex}");
			Utils.PrintDebugMessage(ModName + ": Failed to patch " + patchType.Name + ": " + ex.Message, 255f, 80f, 80f);
		}
	}
}
