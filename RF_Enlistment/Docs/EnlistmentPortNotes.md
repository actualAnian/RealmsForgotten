# RF Enlistment Port Notes

This project is the clean rebuild base for the old `Enlistment` mod.

## What we recovered safely

- MCM setting names and categories
- String resources
- Gauntlet event popup prefab and textures
- General feature map from patch/type names

## What the old DLL showed

- The mod is heavily patched with Harmony.
- A large part of the assembly is obfuscated or contains broken IL.
- Full direct modernization of the original binary is not reliable.

## Feature groups identified

- Core enlistment / serve under a lord
- Rank and promotion flow
- Armorer / equipment flow
- Combat rewards and wages
- Training flow
- Bandit and scout ambush events
- Battle formation helper
- Town, siege, tournament, army, and menu patches
- Naval/WarSails-related compatibility patches

## Current port strategy

1. Rebuild the project shell in the RealmsForgotten pattern.
2. Bring over safe assets and settings first.
3. Rebuild the campaign logic in clean source code.
4. Port only the parts that still make sense with the current API and your mod stack.

## Important compatibility note

An older `ServeAsSoldier.dll` existed in the local RealmsForgotten module during the audit.
It has now been removed from the project wiring and should not be treated as a dependency for `RF_Enlistment`.

That means the port path is now:

- rebuild enlistment gameplay in clean source code
- reuse only safe assets, strings, and feature ideas from legacy mods
- avoid reviving broken legacy dependencies

## Current functional checkpoint

The rebuilt `RF_Enlistment` project now has a working gameplay loop:

- enlist in town/castle or directly through lord conversation
- store commander, contract time, rank, assignment, wages, service XP, and equipment issue state
- daily service progression with wages, leadership XP, assignment XP, and promotion checks
- contract expiry, renewal, and discharge
- transfer between infantry, archer, cavalry, and support duty
- armorer-style service equipment issuing by rank and duty, with return or payoff handling
- troop drilling once per day for extra progress
- battle kill rewards during missions
- campaign battle credit when the player fights with the enlisted commander present
- random service duty events while traveling on campaign
- persistent recon sweep duty missions that spawn a hostile target party on the campaign map and must be reported back to the commander
- duty mission variety by assignment:
  - infantry gets road patrol duties
  - archers get recon sweep duties
  - cavalry gets mounted pursuit duties
  - support gets supply delivery duties
- persistent service memory:
  - commander trust rises and falls with success, failure, and campaign performance
  - service record now tracks duty successes, failures, and major context counts
  - status view now shows trust level, a simple career profile, and conduct state
- commander trust now affects what kind of duty the player is likely to receive and how strong the reward package is
- low-trust service now pushes the player into plain, closely watched duty instead of premium assignments
- very low trust can temporarily block requested duty missions until the player rebuilds standing through normal service
- very high-trust service can now trigger a special trusted dispatch duty with stronger rewards
- army service tracking when the player marches inside the commander's army
- siege duty tracking and siege completion rewards under the commander
- naval and blockade-aware service tracking while enlisted under the commander
- role-based battle rewards that react differently in field, siege, blockade, and naval contexts
- siege and tournament rewards now react more clearly to the enlisted assignment
- tournament victory credit while enlisted
- higher-station recommendation exit for long-serving top soldiers
- pause-on-settlement-entry option while enlisted

## Still intentionally deferred

These are not part of the current finished core and should be treated as later expansions:

- deeper ambush / recon chain with hidden encounters and custom scenes
- advanced tournament / siege / naval event chains from the legacy mod
- old Harmony patch compatibility layers that depended on broken legacy code

## Audit checkpoint - 2026-06-14

What was confirmed during the latest source audit:

- `RF_Enlistment` is no longer a dead stub. It is a real clean rebuild with working core flow.
- save/load support is present through `RFEnlistmentSaveDefiner`
- project build is healthy enough for continued work
- the project has now been added into the main `RealmsForgotten.sln`

Safe legacy value we reused:

- legacy `strings.xml` was already present in the rebuilt module
- main enlistment dialogue lines now respect the `CustomText` setting instead of leaving that option effectively unused

Most likely next worthwhile expansions:

- companion-facing enlistment interactions from the legacy concept set
- richer commissioning / higher-station flow
- more specialized duty mission branches by assignment and context
- stronger use of the existing gauntlet assets if we decide to restore a dedicated enlistment UI layer

Latest refinement added after that audit:

- higher-station recommendation now checks actual service merit instead of only rank, days, and leadership
- stronger service careers now receive a better recommendation package and flavor text on exit
- service status now tells the player what still blocks commission readiness
- companions in the player's party now show up as service support through their party roles
- relevant companion roles now slightly improve drill quality and duty-mission rewards during enlistment
- companion support is no longer flat: better-skilled companions now give stronger service support
- status text now shows the rough quality of each supporting companion role
- quartermasters now matter directly for contract and equipment administration
- engineers now strengthen siege and blockade service gains
- scouts now strengthen naval watch and other field-facing service contexts
- surgeons now matter more during support-oriented duty outcomes
- commander context now pushes duty flavor and offer weighting more clearly during army, naval, siege, and blockade phases
- trusted dispatch and duty assignment logic now reacts better to the live campaign situation instead of only the player's static assignment
- service status is no longer just a small text line: it now opens a clearer `Service Record` panel with commander, role, trust, duty, support, and commission-readiness details
- commander personality is now surfaced more directly in the service view so the player can understand the command style behind assignments
- leaving service now has stronger career consequences:
  - good service can grant renown, skill XP, and better commander relations
  - poor service can damage relations and leave a worse military record
  - commission exits now give a stronger political and social payoff than a normal discharge

What still should not be trusted from the old mod:

- large direct patch ports from the decompiled DLL
- obfuscated gameplay branches that cannot be validated against current Bannerlord API
