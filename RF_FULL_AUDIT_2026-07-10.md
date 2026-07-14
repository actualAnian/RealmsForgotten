# Realms Forgotten Full Mod Audit

Date: 2026-07-10  
Branch: `RF_V13_Warsails-(BlackRose)`  
Scope: every active project and resource that forms the mod under `RF_Warsails_AI`.

## Audit Rules

- Read-only audit. No source, XML, UI, build, or runtime file was changed.
- Backup folders, checkpoints, decompiled reference mods, and inactive tools were excluded from active findings unless they supplied evidence about integration.
- A successful build proves API/type compatibility only. It does not prove that Bannerlord calls a behavior, patch, model, menu, or mission flow.
- Findings marked **Confirmed** follow directly from registration or execution paths in the source. Findings marked **Runtime verification** need an in-game reproduction or trace before modification.

## Executive Summary

The mod is not currently in a state where every implemented system can be considered active and reliable. The largest problem is architectural: `AiSubModule` contains models, patches, and mission behaviors, but it is not declared as a submodule. The main submodule calls only its campaign-behavior helper, so substantial portions of that code never run.

Positive results:

- All 12 active projects compile with zero errors when deployment is disabled.
- 67 source XML/XSLT files and 247 installed XML/XSLT files were parsed successfully; no malformed active XML was found.
- Every XML path declared by the installed `SubModule.xml` exists.
- No duplicate `SaveableTypeDefiner` base ID was found among the active definers inspected.
- The battle-scene index guard is currently disabled, so it is not mutating native index data.
- The war-system trace logger is currently disabled.

The highest-priority repairs are: reconnecting the missing main-module lifecycle, removing the `NotImplementedException` from the custom agent origin, making manual Harmony targets safe, correcting Necromancy exception handling, and establishing one reliable packaging/dependency process.

## Projects Audited

1. `RealmsForgottenMain` / `RealmsForgotten`
2. `RFCustomScenes` / `RFCustomSettlements`
3. `NecromancyAndSummoning`
4. `RFSmithing`
5. `RFReligions`
6. `HuntableHerds`
7. `RF_AIDialog`
8. `RF_BattleAI`
9. `RF_Promoted`
10. `RF_warsystem`
11. `RF_Settlers`
12. `RF_Enlistment`

`rf_adventures`, `RF_XmlValidator`, backup projects, `_checkpoints`, and decompiled reference mods are not active solution components.

## Critical Findings

### RF-001 - Main AI lifecycle is only partially connected

**Status:** Confirmed  
**Files:**

- `RealmsForgottenMain/AiMade/AiSubModule.cs:30`
- `RealmsForgottenMain/SubModule.cs:109`
- `RealmsForgottenMain/_Module/SubModule.xml`

`AiSubModule` inherits `MBSubModuleBase`, but it is not declared in `SubModule.xml`. The active `RealmsForgotten.SubModule` calls only `AiSubModule.AddCampaignBehaviors(...)`.

Consequences:

- `AiSubModule.OnGameStart()` never runs as a lifecycle method.
- `AddCustomModels()` is not called anywhere else. The following models therefore appear inactive:
  - `UrkhaiPartySizeModel`
  - `AlignmentDiplomacyModel`
  - `ClimateAwareVillageProductionModel`
  - `ClimateAwareSettlementFoodModel`
  - `CustomTradeItemPriceFactorModel`
  - `RFDiplomacyModel`
- `ApplyDelayedJoinEncounterPatch()` is not called, so its manual encounter patches appear inactive.
- `AiSubModule.OnMissionBehaviorInitialize()` is not delegated by the active main submodule. These mission behaviors therefore appear inactive:
  - `CustomBerserkerBehavior`
  - `ADODFireArrowsMissionBehavior`
  - `InfectionMissionBehavior`
  - `ADODReinforcementsRunner`
  - `CommanderSwapMissionBehavior`
  - `FindMagicItemsMissionBehavior`

Do not fix this by simply adding `AiSubModule` to `SubModule.xml`: that would register the campaign behaviors twice. Merge or explicitly delegate the missing lifecycle responsibilities into the one active main submodule.

### RF-002 - Custom agent origin contains a live unimplemented interface method

**Status:** Confirmed  
**File:** `RFCustomScenes/RFAgentOrigin.cs:159`

`RFAgentOrigin.SetBanner(Banner banner)` throws `NotImplementedException`. Any engine or mission flow that calls this interface method will crash immediately. Implement the expected state update or a safe no-op consistent with the current Bannerlord `IAgentOriginBase` contract.

## High-Severity Findings

### RF-003 - Religion startup uses the wrong base lifecycle method

**Status:** Confirmed  
**File:** `RFReligions/SubModule.cs:27`

`OnGameStart(...)` calls `base.OnCampaignStart(...)` instead of `base.OnGameStart(...)`. This bypasses the expected base lifecycle and can produce version-dependent initialization behavior. Call the matching base method.

### RF-004 - Religion and quest manual patches can abort on one renamed private method

**Status:** Confirmed  
**Files:**

- `RFReligions/SubModule.cs:61-66`
- `RealmsForgottenMain/Quest/QuestPatches.cs:21-30`

The religion module patches `EncyclopediaHeroPageVM:Refresh` without checking whether `AccessTools.Method` returned null. `QuestPatches.PatchAll()` performs eight similar unguarded patches. If Bannerlord renames one private method, Harmony throws; in `QuestPatches`, the first failure also prevents all later patches from being installed.

Resolve and validate each target separately, log a precise warning, and continue installing independent patches.

### RF-005 - Necromancy exception handling can replace the real error with a NullReferenceException

**Status:** Confirmed  
**Files:**

- `NecromancyAndSummoning/Patch/SummoningAndRaiseCorpsePatch.cs:36`
- `NecromancyAndSummoning/Patch/PartyPatch.cs:38`
- `NecromancyAndSummoning/Patch/NecromancyPatch.cs:45`
- `NecromancyAndSummoning/NecromancyBehaviour.cs:41`

The catch blocks build a condition that is effectively always true and then dereference `ex.InnerException.Message`, even when `InnerException` is null. This can hide the original failure and throw a secondary exception. Preserve and rethrow the original exception, or wrap it with `ex` as the inner exception.

### RF-006 - Source module staging is not a self-contained distributable package

**Status:** Confirmed  
**Locations:**

- `RealmsForgottenMain/_Module/SubModule.xml`
- `RealmsForgottenMain/_Module/bin/Win64_Shipping_Client`

The source `SubModule.xml` declares all active DLLs, but the source staging bin currently lacks at least:

- `RFCustomSettlements.dll`
- `NecromancyAndSummoning.dll`
- `RFSmithing.dll`
- `RFReligions.dll`
- `HuntableHerds.dll`
- `RF_AIDialog.dll`

The installed Steam module contains them only because separate project post-build steps copy to different destinations. A clean release assembled from the source `_Module` directory is therefore incomplete. Establish one package target that collects every active DLL, dependency, XML, prefab, and string file into one staging directory and validates it before deployment.

### RF-007 - Bundled MCM and undeclared dependencies create load-order/API conflict risk

**Status:** Confirmed configuration risk  
**File:** `RealmsForgottenMain/_Module/SubModule.xml`

The module directly loads two submodule classes from its bundled `MCMv5.dll`, while the project also compiles against MCM, ButterLib, Harmony, and UIExtenderEx. Only UIExtenderEx and vanilla modules are declared as dependencies. This can load a stale bundled MCM beside a separately installed MCM and has already been associated with `MissingMethodException` failures during this project.

Choose one dependency policy: supported external framework modules with explicit metadata, or fully bundled dependencies verified against the target game API. Do not mix both silently.

### RF-008 - Custom campaign loading manager remains tightly coupled to a private loading state machine

**Status:** Runtime verification  
**Files:**

- `RealmsForgottenMain/Managers/RFCampaignManager.cs:48-117`
- `RealmsForgottenMain/SubModule.cs:421`

The mod replaces the normal campaign manager and manually reproduces Bannerlord's loading states. The current code calls `Game.DoLoading()` in `SecondInitializeThirdState` and no longer calls it in `FinishLoadingFifthStep`, which is better than the previous double-entry state. However, the implementation still duplicates a version-sensitive engine state machine. Recent native crashes occurred in `GetBattleSceneIndexMap` while this manager was active.

Diff this class line by line against the exact 1.3.x vanilla manager and create a new-game plus save-load smoke test. Static compilation cannot certify native loading safety.

### RF-009 - Castle troop menu is registered twice during one session launch

**Status:** Confirmed  
**File:** `RealmsForgottenMain/AiMade/special_troops_castle.cs:35-41`

`HouseTroopsCastleBehavior.OnSessionLaunched` adds one configuration, calls `AddGameMenus`, adds the second configuration, then calls `AddGameMenus` again. This attempts to register the same menu and option IDs twice and can duplicate options or make registration order-dependent. Populate the full configuration dictionary first, then register the menu once.

## Medium-Severity Findings

### RF-010 - Several implemented behaviors appear unregistered

**Status:** Registration review required  

No active construction or `AddBehavior` registration was found for the following `CampaignBehaviorBase` classes:

- `ArcaneLibraryCampaignBehavior`
- `CustomKeepMenuBehavior`
- `KeepItemsAfterBattleBehavior`
- `DailyMessageBehavior`
- `DesertionBehavior`
- `DuelSpawnBehavior`
- `RFKnightCampaignBehavior`
- `StoryManager`
- `AlignmentWarStarter`
- `CustomCampaignBehaviorWrapper`
- `MountTrackingBehavior`
- `CastlePatrols`
- `RFCustomBanditSpawnBehavior`
- `VisitArcaneHall`
- `KnightOfferCampaignBehavior`
- `OllamaTestBehavior`

Some may be intentionally dormant or superseded. Classify each as active, obsolete, or experimental. Register active ones once and remove only confirmed obsolete residue in a later cleanup task.

### RF-011 - Tier 7 troop module is dead lifecycle code

**Status:** Confirmed  
**File:** `RealmsForgottenMain/AiMade/TroopUnlocker/T7TroopUnlockerModule.cs`

`T7TroopUnlockerModule` is another `MBSubModuleBase` class not declared in `SubModule.xml` and not called by the active main submodule. Its replacement of `DefaultCharacterStatsModel` therefore does not run. Decide whether tier-7 support is still required; if so, integrate the model through the active submodule rather than adding another overlapping lifecycle class.

### RF-012 - Battle AI performs seven systems from a mission frame hook

**Status:** Performance review required  
**File:** `RF_BattleAI/Patches/FieldBattleAIPatches.cs:123-137`

Every `MissionState.OnTick` invokes hotkeys, spear splitting, cavalry stabilization, adaptive memory, captain tuning, runtime tracing, and tactic telemetry. Several systems throttle internally, but this remains a high-frequency aggregation point and future additions can easily introduce battle stutter.

Give each subsystem an explicit cadence and measure total time. Keep input checks per frame only where needed; move tactical scans to fixed intervals.

### RF-013 - Battle AI writes duplicate synchronous log files

**Status:** Confirmed  
**Files:**

- `RF_BattleAI/BattleAIRuntimeTracer.cs:278-288`
- `RF_BattleAI/BattleAIAdaptiveMemory.cs:307-317`
- `RF_BattleAI/BattleAITacticTelemetry.cs:160`
- `RF_BattleAI/BanditTrapTelemetry.cs:168`

Runtime trace and adaptive memory append synchronously to multiple distinct paths. During tracing, one logical event can cause three filesystem writes, and formation-level events multiply that cost. This can produce short battle stalls.

Use one canonical log location, buffer writes, and make diagnostic telemetry explicitly configurable. Adaptive memory data should have one authoritative persistence file, separate from human-readable diagnostics.

### RF-014 - Enlistment performs target and attachment maintenance on every campaign tick

**Status:** Performance review required  
**File:** `RF_Enlistment/RFEnlistmentCampaignBehavior.cs:826-862`

While enlisted, every campaign tick calls `EnsureDutyMissionTargetVisibility`, repairs commander state, checks pending menus/encounters, and evaluates attachment state. Attachment attempts are time-throttled, but visibility and several state checks are not. This is a plausible contributor to campaign-map overhead during long service sessions.

Move non-visual repair work to hourly/event-driven paths. Keep only the smallest UI-sensitive operation on the frame tick, with an explicit short interval.

### RF-015 - AI dialog rebuilds file-backed memory daily and suppresses many failures

**Status:** Confirmed design risk  
**Files:**

- `RF_AIDialog/src/AIMemorySummaryBehavior.cs:12-22`
- `RF_AIDialog/src/AIMemoryStore.cs`
- `RF_AIDialog/src/WorldContext.cs`

The summary behavior rebuilds external summaries on load and every day. The subsystem performs many synchronous reads/writes and contains a very high number of empty catch blocks. Best-effort behavior is appropriate for optional AI features, but silent failures make broken memory, letters, or context look successful.

Add bounded file sizes, avoid rewriting unchanged summaries, and log one rate-limited warning per failure category. Do not write API keys or full sensitive prompts to diagnostics.

### RF-016 - Religion code assumes its singleton is initialized in several public flows

**Status:** Runtime verification  
**Files:**

- `RFReligions/Models/ReligionSettlementLoyaltyModel.cs:30`
- `RFReligions/Models/ReligionPartyMoraleModel.cs:31`
- `RFReligions/Behavior/ClericConversionDialogueBehavior.cs:201-233`
- `RFReligions/Behavior/CrusadeBehavior.cs:93-339`

Several paths dereference `ReligionBehavior.Instance` directly. The behavior is normally registered before model use, but loading failures or lifecycle changes can turn this into a startup/save-load crash. Models should return the vanilla/base result when religion state is unavailable; dialogues and crusades should fail closed with a clear diagnostic.

### RF-017 - Army/party UI insertion depends on version-sensitive prefab paths

**Status:** Confirmed maintenance risk  
**Files:**

- `RealmsForgottenMain/AiMade/ArmyCommand/RFClanPartyCommandPrefabPatches.cs:6`
- `RealmsForgottenMain/AiMade/ArmyCommand/RFArmyCommandPrefabPatches.cs:6`
- `RFSmithing/PrefabExtensions/*.cs`

Army Command and Smithing inject UI through exact prefab names and XPath expressions. These are valid mechanisms, but small vanilla UI changes can make the insertion silently disappear or land in the wrong layer. This area has already required repeated runtime correction.

Add a startup diagnostic that confirms each critical prefab patch matched exactly one node. Maintain screenshot tests at the supported resolution/aspect ratios after each Bannerlord update.

### RF-018 - Build and deployment are machine-specific and can modify the live module unexpectedly

**Status:** Confirmed  
**Files:**

- `RealmsForgottenMain/RealmsForgotten.csproj:42-58,133-156`
- project post-build targets across the solution

The main project contains hard-coded Steam paths, and projects use a mixture of `AfterTargets="Build"` and `AfterTargets="PostBuildEvent"`. `Bannerlord.BuildResources` copy targets can still execute independently unless `ModuleId` is cleared; a validation build attempted to touch the live module and failed on a DLL locked by the running game.

Separate `Build`, `Package`, and `Deploy` into explicit targets. A normal build must never clean or overwrite the installed mod. Use `GameFolder`/shared props instead of absolute paths.

### RF-019 - Necromancy targets a different framework and Harmony generation

**Status:** Confirmed compatibility risk  
**File:** `NecromancyAndSummoning/NecromancyAndSummoning.csproj`

This project targets `netstandard2.0` and compiles against `Bannerlord.Lib.Harmony 2.2.2`, while the current projects use `net472` and Harmony 2.4.2. It compiles, but the mixed runtime contract increases the chance of patching and dependency conflicts. Align it with the supported module baseline after behavior tests.

### RF-020 - Localization ownership is duplicated

**Status:** Confirmed source ambiguity  

The audit found 285 repeated string IDs across source XML files. Not every duplicate becomes a runtime conflict, because some are language copies or overwrite the same packaged path, but ownership is unclear. Important examples include:

- `promoted_strings.xml` in both the main module and `RF_Promoted`
- quest strings repeated in `Languages/str_quest.xml`, `quest/quest_strings.xml`, and `str_quest_copy.xml`

Choose one owning source file per string ID and generate translations/copies from it. Add duplicate-ID validation that understands language folders.

### RF-021 - Mojibake is present in AI dialog source text

**Status:** Confirmed  
**Files:** `RF_AIDialog/src/WorldHistoryBehavior.cs` and related AI dialog sources

Examples include corrupted punctuation such as `â€”` and corrupted decorative comment text. At least one world-chronicle label was previously observed with broken UTF-8 text. Normalize these files to UTF-8 and replace corrupted user-facing literals.

### RF-022 - `BaseGameDebugCampaignBehavior` is misleadingly named and always active

**Status:** Confirmed  
**Files:**

- `RealmsForgottenMain/SubModule.cs:91`
- `RealmsForgottenMain/BaseGameDebugCampaignBehavior.cs`

Despite its name, this behavior is production persistence: it records every living hero's race before save and restores it on session start. The work is legitimate but scales with all heroes and can be mistaken for removable debug code. Rename/document it as race persistence and verify whether custom race data should instead live in a dedicated save model.

## Low-Severity and Maintenance Findings

### RF-023 - Save-definer number formatting is difficult to review

No active base-ID collision was found, but values such as `2_87656_493` and `199235856_7` are visually ambiguous. Preserve their numeric values for save compatibility, but document the evaluated decimal ID beside each constant and maintain a central registry. Never renumber an ID already present in released saves.

### RF-024 - `RFCustomScenes` still contains explicitly temporary and test-marked flows

`RFMissions.cs`, `ScenePatches.cs`, `Helper.cs`, arena code, and several quest paths retain `TODO test` or temporary markers. These do not prove a defect, but they identify flows lacking a defined acceptance test. Prioritize custom arena transitions, interaction messages, scene switching, and reward equipment.

### RF-025 - Manual player Battle AI override expires after a fixed window

The tactic controller retains a manual doctrine for about 45 seconds, after which normal AI selection may replace it. This is not a code failure, but it can contradict the player's expectation that a numpad order persists through the maneuver. Make the duration/state transition visible or retain the order until completion, cancellation, or an explicit emergency condition.

## Systems That Passed Static Checks

- All 12 active projects compile with zero errors under an isolated x64 Debug build.
- Active XML/XSLT files are well formed.
- Installed XML paths referenced by `SubModule.xml` exist.
- `RF_warsystem` registers its ten behaviors and its trace logger is disabled (`Enabled = false`).
- `RF_Promoted` registers both campaign and mission behaviors and records kills from the player's party side.
- `RF_Settlers`, `RF_Enlistment`, `RF_Promoted`, `RFCustomScenes`, `RFReligions`, `HuntableHerds`, and the main project use distinct save-definer base IDs in the inspected source.
- The custom battle-scene index guard has its Harmony attribute commented out and is not explicitly installed elsewhere.

## Recommended Repair Order

1. **Lifecycle:** merge the missing `AiSubModule` model, mission, and manual-patch responsibilities into the active main submodule without double registration.
2. **Crash blockers:** implement `RFAgentOrigin.SetBanner`; fix Necromancy exception handlers; guard every manual Harmony target.
3. **Loading stability:** compare `RFCampaignManager` with the exact supported vanilla 1.3.x implementation and run new-game/save-load smoke tests.
4. **Packaging:** create one deterministic package/deploy target and declare a coherent dependency policy.
5. **Duplicate registration:** fix `HouseTroopsCastleBehavior` and classify unregistered behaviors.
6. **Performance:** profile Enlistment tick work, Battle AI frame work, and AI-dialog filesystem activity.
7. **UI and localization:** add prefab-match diagnostics, screenshot checks, and consolidate duplicate/corrupted strings.
8. **Regression suite:** test campaign creation, old-save load, character/party/inventory screens, land/naval battle, siege, custom battle, enlistment attachment/detachment, religion conversion/crusade, promoted soldiers, settlers, and army/party commands.

## Required Runtime Test Matrix

Static analysis cannot prove these flows. After the repairs, record pass/fail and logs for:

| Area | Minimum test |
|---|---|
| Loading | New campaign, old save, save-reload after 30 campaign days |
| UI | Character, inventory, clan parties, army manager, smithing, religion menu |
| Battles | Field, siege attack/defense, naval, custom battle, retreat |
| Battle AI | Player manual doctrine, enemy autonomous doctrine, bandit infantry, horse archers, lancers |
| Enlistment | Petition, oath, travel, settlement, raid, siege, defeat, detached duty, reattachment |
| War system | War proposal, peace, permanent rivalry, coalition, capitulation, long simulation |
| Religion | Cleric start, faith/deeds, conversion persuasion, crusade, religious war |
| Persistence | Intrigue, promoted heroes, settlers, religion, enlistment record, army orders |
| AI dialog | Conversation, letters, memory rebuild, missing/invalid API configuration |

## Final Assessment

The mod has a large amount of implemented functionality and currently compiles cleanly, but compilation is masking disconnected lifecycle code and brittle runtime integration. The critical work is not a broad rewrite. It is a focused stabilization pass: make one submodule own each lifecycle, make optional patches fail independently, remove known crash paths, and make packaging reproducible. Only after those items should balance and feature expansion resume.
