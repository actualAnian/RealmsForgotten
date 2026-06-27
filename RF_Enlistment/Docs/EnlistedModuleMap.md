# Enlisted Module Map

This note maps what the old `Enlisted` module appears to do, using only evidence that is trustworthy in this workspace:

- `Enlisted/SubModule.xml`
- runtime logs in `Enlisted/Debugging`
- JSON/XML data in `Enlisted/ModuleData`
- patch inventory from `Conflicts-A_2026-01-04_15-46-00.log`
- decompiled filenames in `tmp/Enlistment_Decompiled/Enlistment`

Important limit:

- the DLL decompile is heavily obfuscated and partially broken
- because of that, exact method-by-method gameplay logic cannot be trusted from decompiled bodies
- the architecture below is still strong, because it is backed by logs, configs, and patch names

## 1. What this mod is

`Enlisted` is not just a small "serve as soldier" mod.

It is a full military-life layer on top of Bannerlord campaign play. It adds:

- enlistment into a lord's force
- hidden service-state campaign flow
- custom army wait / service status menus
- battle reserve and encounter handling
- camp life activities and random opportunities
- quartermaster gear and provisions systems
- pay, muster, promotion, and service records
- officer / commander progression
- retinue management at high rank
- naval compatibility
- an optional battle AI submodule

## 2. Module structure

Confirmed in `Enlisted/SubModule.xml`:

- module id: `Enlisted`
- target game version: `1.3.13`
- required dependency: `Bannerlord.Harmony`
- submodule 1: `Enlisted.Mod.Entry.SubModule`
- submodule 2: `Enlisted.Features.Combat.BattleAI.BattleAISubModule`

So the mod is split into:

1. core service/campaign system
2. optional battle AI layer

## 3. Core campaign model

The logs strongly suggest this mod does **not** do a clean literal merge of the player into the lord's party in the pure vanilla sense.

Instead, it runs a **service mode** with custom campaign control:

- player party can be hidden
- native `army_wait` is overridden into `enlisted_status`
- the player remains in a managed enlisted state while the mod controls flow

Evidence from `Session-A_2026-01-04_15-46-00.log`:

- `HidePartyNamePlatePatch` is applied
- repeated menu override:
  - `army_wait -> enlisted_status`
- state tracking includes:
  - `active=True/False`
  - `visible=True/False`
  - `attachedTo=...`
  - `army=...`

So conceptually the system works like this:

1. you enlist
2. the mod switches you into a managed service state
3. the campaign keeps running under a custom "enlisted status" layer
4. native encounter/army/settlement menus are intercepted and redirected when needed

## 4. Behaviors it registers

From `Conflicts-A_2026-01-04_15-46-00.log`, the module registers 32 campaign behaviors:

- `CampLifeBehavior`
- `CampMenuHandler`
- `CampOpportunityGenerator`
- `CompanySimulationBehavior`
- `ContentOrchestrator`
- `EnlistedCombatLogBehavior`
- `EnlistedDialogManager`
- `EnlistedEncounterBehavior`
- `EnlistedIncidentsBehavior`
- `EnlistedMenuBehavior`
- `EnlistedNewsBehavior`
- `EnlistedStatusManager`
- `EnlistmentBehavior`
- `EquipmentManager`
- `EscalationManager`
- `EventDeliveryManager`
- `MusterMenuHandler`
- `OrderManager`
- `OrderProgressionBehavior`
- `PlayerConditionBehavior`
- `PromotionBehavior`
- `QuartermasterEquipmentSelectorBehavior`
- `QuartermasterManager`
- `RetinueCasualtyTracker`
- `RetinueLifecycleHandler`
- `RetinueTrickleSystem`
- `ServiceRecordManager`
- `TroopSelectionManager`

That tells us the mod is built as a group of subsystems, not one monolithic enlistment script.

## 5. What happens when you enlist

The session log shows this startup sequence:

- quartermaster inventory is initialized
- muster tracking is initialized
- the player joins the lord's kingdom as a mercenary while enlisted
- starting recruit gear is assigned
- starting ration is issued

This means enlistment is tied to:

- faction alignment
- service records
- gear provisioning
- pay/muster timeline
- supply state

## 6. Menus and UI flow

The mod creates its own menu layer rather than using only vanilla town/castle/army menus.

Confirmed by logs:

- `Camp menus registered successfully`
- `Muster menus registered successfully`
- `Encounter behavior initialized ... battle wait menu and reserve options ready`
- `All enlisted dialog flows registered successfully`

Main UI ideas visible from logs and strings:

- `enlisted_status` menu
- camp menu
- muster menu
- reserve/battle wait options
- service record views
- quartermaster screens
- company history/status panels

The strings file shows sections such as:

- `SINCE LAST MUSTER`
- `UPCOMING`
- `RECENT ACTIVITY`
- `YOUR STATUS`
- lifetime service summary
- current enlistment
- XP sources
- company history

So the user-facing experience is meant to feel like a military career dashboard.

## 7. Camp life system

This is one of the biggest systems in the mod.

From `camp_schedule.json`, camp time is divided into phases:

- Dawn
- Midday
- Dusk
- Night

Each phase has weighted activity slots such as:

- formation
- training
- work
- social
- economic
- recovery
- special

The schedule changes based on pressure and context:

- low morale
- low supplies
- high scrutiny
- exhausted
- pre-battle
- siege
- marching

It also changes by lord situation:

- PeacetimeGarrison
- WarMarching
- WarActiveCampaign
- SiegeAttacking
- SiegeDefending

So the mod tries to simulate army life as a daily routine, not just battle spam.

## 8. Opportunity and decision system

This is the engine behind "things happen while you serve."

Confirmed by logs:

- `CampOpportunityGenerator registered`
- `Content Orchestrator registered`
- `DecisionManager registered`
- `Loaded 37 decisions`
- `Loaded 36 opportunity definitions`

The orchestrator appears to:

1. detect strategic context
2. schedule opportunities by day phase
3. convert opportunities into decisions/events
4. refresh the enlisted UI when something becomes available

Examples from logs:

- `opp_equipment_maintenance`
- `dec_maintain_gear`
- `opp_help_wounded`
- `dec_help_wounded`
- `opp_storytelling_circle`
- `dec_social_storytelling`

Examples from `decisions.json`:

- maintain your gear
- write a letter home
- high stakes gambling

So the player is not just passively waiting. The mod keeps feeding small military-life events into service mode.

## 9. Order system

The mod has rank-based order packs:

- `orders_t1_t3.json`
- `orders_t4_t6.json`
- `orders_t7_t9.json`

Low-tier orders are ordinary soldier duties:

- guard duty
- camp patrol
- firewood collection
- equipment inspection
- muster inspection
- sentry post

These orders can affect:

- officer reputation
- soldier reputation
- company needs
- trait XP
- skill XP

High-tier orders shift toward leadership:

- command a squad
- strategic planning
- coordinate supply
- inspect company readiness

So as rank rises, the gameplay changes from "do assigned duty" into "help command the force."

## 10. Quartermaster system

This is a full subsystem, not a simple item shop.

Evidence:

- dedicated quartermaster behavior
- quartermaster UI prefabs
- quartermaster dialogue JSON
- equipment pricing config
- provisions UI
- upgrade UI

Quartermaster dialogue includes options like:

- browse equipment
- upgrade equipment
- officers' armory
- sell equipment quietly
- request provisions
- ask supply status
- ask baggage status

Access is gated by:

- rank/tier
- officer status
- reputation/trust
- supply level

So the quartermaster is part shop, part military logistics, part progression gate.

## 11. Progression and ranks

From `progression_config.json`:

- there are 9 tiers
- tier 2 unlocks formation selection
- higher tiers unlock better equipment
- tier 5-6 are officer track
- tier 7-9 are commander track

The config also defines culture-based rank names, so progression is flavored differently for:

- empire
- vlandia
- sturgia
- khuzait
- battania
- aserai
- mercenary

This is not just level-up text. It changes what systems the player can access.

## 12. Pay, muster, retirement, and records

From `enlisted_config.json`, the service economy includes:

- payday interval
- wage formula
- probation
- desertion grace period
- leave limits
- first term / renewal term
- reenlistment bonus
- severance / pension
- retirement rewards

The strings file also confirms service tracking:

- lifetime service summary
- current enlistment
- terms completed
- days served
- pending discharge
- pending pay
- owed backpay
- combat statistics

So Enlisted treats soldiering like a long-term career loop.

## 13. Commander / retinue system

From `retinue_config.json`:

- retinue unlocks at commander track (`T7+`)
- T1-T6 are companions only
- higher ranks can recruit/manage soldier types

Supported retinue types:

- infantry
- archers
- cavalry
- horse archers

There are also:

- faction overrides
- replenishment trickle
- requisition cooldowns
- upkeep
- desertion

So at high rank the mod stops being pure soldier-roleplay and starts becoming a small command layer.

## 14. Strategic context system

From `strategic_context_config.json`, the mod classifies the army's broader situation into contexts such as:

- coordinated_offensive
- desperate_defense
- raid_operation
- siege_operation
- patrol_peacetime
- garrison_duty
- recruitment_drive
- winter_camp

Each context changes:

- what kinds of orders are appropriate
- what kinds are suppressed
- predicted company needs
- scheduling tone and activity

This is important because it means the mod is trying to make army life react to the campaign situation instead of running a flat event table.

## 15. Battle and encounter layer

The patch inventory shows a lot of encounter control:

- start party encounter
- leave encounter
- join encounter
- capture enemy
- loot party
- siege wait leave
- battle deployment
- observer/agent removal

Session logs also show:

- battle reserve state
- post-battle cleanup
- large battle waiting in reserve
- return to enlisted status after battle

So the battle side of the mod is doing at least three things:

1. getting the player into battles under service rules
2. managing reserve/wait behavior
3. restoring campaign service flow cleanly afterward

## 16. Naval support

This mod clearly expects naval content.

Logs and patch inventory confirm fixes for:

- player ship suitability
- captain assignment
- troop allocation
- ship removal safety
- OnAgentRemoved safety
- naval formation AI
- sea-specific order variants

So `Enlisted` is not land-only. It has explicit sea-service support.

## 17. Optional battle AI

The second submodule is dedicated to combat AI:

- `Enlisted.Features.Combat.BattleAI.BattleAISubModule`

Runtime log:

- `Battle AI SubModule loaded - Advanced combat AI enabled`
- `Battle AI systems initialized`

I cannot safely map the exact internal decision code from the decompile, but the module definitely ships with a separate battle-AI layer on top of the core enlistment system.

## 18. What the patch list says about implementation style

The patch list is important because it shows *how* the mod works.

It patches:

- army wait behavior
- encounter menus
- player town return-to-army behavior
- clan/kingdom UI
- visibility/active state of player party
- party roles like scout / engineer / surgeon / quartermaster
- siege and naval edge cases

That means the author did not build this as a neat self-contained gameplay island.

Instead, the mod works by:

1. inserting campaign behaviors
2. overriding native menu flow
3. patching army/encounter edge cases
4. maintaining a custom service state machine

## 19. What I can say with confidence about "how it works"

High confidence:

- it is a service-state system layered over campaign
- it uses menu redirection very heavily
- it relies on many Harmony patches
- it has a strong data-driven design through JSON catalogs
- it includes progression, QM, camp life, pay, decisions, retinue, and naval support

Medium confidence:

- the player is functionally treated as attached/managed under a lord rather than cleanly merged into party logic everywhere
- many gameplay beats are driven by a daily/hourly orchestrator instead of only direct triggers

Low confidence:

- exact internal call order inside the DLL
- exact code paths for enlistment transitions
- exact timing details not visible in the logs

## 20. What is reusable for RF_Enlistment

Based on this study, the most reusable design ideas are:

1. data-driven ranks, orders, events, and decisions
2. quartermaster as a real military logistics system
3. service record / muster / pay loop
4. strategic-context-based content scheduling
5. retinue unlock at higher ranks
6. encounter cleanup guards and army wait interception

The least reusable part is probably the exact hidden-player state implementation, because that is where these systems usually become brittle.

## 21. Final read

Short version:

`Enlisted` is a military-career framework, not just an enlist button.

Its real pillars are:

- service mode
- army/campaign menu takeover
- daily camp life simulation
- dynamic duty/order system
- QM logistics/equipment
- promotion/pay/retirement loop
- late-game retinue command
- battle and naval compatibility

If we want to learn from it, the best thing to copy is the **system design**, not the broken old code paths.
