# RF Battle AI Doctrine Design

## Goal

Expand Bannerlord battle AI without replacing the vanilla system.

`RF_BattleAI` should act as a doctrine layer on top of vanilla tactics:

- vanilla keeps handling the base battle logic
- `RF_BattleAI` adds advanced tactical choices
- not every army knows every doctrine
- doctrine access depends on commander quality, army composition, and battle context

This keeps the mod believable, stable, and easier to maintain.

## Core Principle

The system should **complement**, not override, vanilla AI.

That means:

- if no custom doctrine fits, the army still behaves correctly through vanilla
- custom doctrines only activate when their conditions make sense
- advanced doctrines should feel rare enough to matter

## Doctrine Tiers

### Tier 0: Vanilla Baseline

Always available.

Examples:

- standard advance
- hold ground
- simple charge
- basic ranged skirmish
- fallback behavior already available in vanilla

Use this when:

- commander is weak
- army is mixed but unremarkable
- battle situation does not justify a doctrine

### Tier 1: Simple RF Doctrines

Unlocked for modest commanders and common army types.

Examples:

- `ElasticDefense`
- `ReserveCounterattack`
- `BanditAdaptiveSkirmish`
- `ShieldwallAdvance`
- `HarassingAdvance`

These should be readable in battle and not require perfect timing.

### Tier 2: Advanced RF Doctrines

Unlocked for stronger commanders with better `Tactics` skill.

Examples:

- `HammerAndAnvil`
- `ObliqueOrder`
- `RefusedFlank`
- `EncirclementPressure`
- `DisciplinedStagedAdvance`

These should require stronger composition checks.

### Tier 3: Elite / Signature Doctrines

Rare doctrines for very capable lords or highly specialized armies.

Examples:

- `CannaeEnvelopment`
- `FeignedRetreat`
- `HorseArcherLure`
- `CentralTrap`

These should be uncommon and should fail gracefully back into simpler states if the battle changes too much.

## Main Unlock Conditions

Each doctrine should be gated by a mix of the following:

- commander `Tactics` skill
- faction or culture profile
- army composition
- power ratio
- terrain or engagement distance
- troop quality / discipline proxy

No doctrine should depend on a single condition only.

## Recommended Tactics Skill Thresholds

Suggested starting thresholds:

- `0-59`: only vanilla + simplest RF behaviors
- `60-99`: simple doctrines
- `100-139`: disciplined battlefield doctrines
- `140-179`: advanced flank and trap doctrines
- `180+`: elite doctrines like feigned retreat or full envelopment

This does not need to be hardcoded forever, but it is a good first balance pass.

## Army Composition Checks

Doctrine choice should inspect composition before activation.

Useful metrics:

- infantry ratio
- ranged ratio
- cavalry ratio
- horse archer ratio
- total power ratio
- remaining power ratio
- formation count and size

Examples:

- `HammerAndAnvil`
  - needs meaningful cavalry presence
  - needs infantry strong enough to pin the enemy

- `ObliqueOrder`
  - works best with strong infantry core and one reinforced wing

- `FeignedRetreat`
  - should strongly prefer mounted skirmishers or horse archers

- `ShieldwallAdvance`
  - should prefer infantry-heavy armies with modest ranged support

- `BanditAdaptiveSkirmish`
  - should prefer light troops, weaker direct power, and flexible spacing

## Doctrine Families

### 1. Defensive Doctrines

Used by cautious, disciplined, or outmatched commanders.

Candidates:

- `ElasticDefense`
- `FallbackLine`
- `ShieldwallAdvance`
- `RefusedFlank`

Expected behavior:

- preserve formation integrity
- avoid reckless full charge
- trade space for time when needed

### 2. Counterattack Doctrines

Used when the army wants to absorb pressure and strike back.

Candidates:

- `ReserveCounterattack`
- `HammerAndAnvil`
- `CorneredStand`

Expected behavior:

- hold the line
- commit reserve at the right moment
- punish enemy overextension

### 3. Maneuver Doctrines

Used by trained commanders with organized armies.

Candidates:

- `ObliqueOrder`
- `DisciplinedStagedAdvance`
- `RefusedFlank`
- `EncirclementPressure`

Expected behavior:

- attack one wing first
- delay parts of the line
- reposition before committing fully

### 4. Deception / Trap Doctrines

Used only by high-skill commanders or special army types.

Candidates:

- `FeignedRetreat`
- `CentralTrap`
- `CannaeEnvelopment`
- `HorseArcherLure`

Expected behavior:

- bait enemy advance
- create local overextension
- close flanks once the enemy center is exposed

### 5. Irregular Doctrines

Used by bandits, raiders, light cavalry groups, and poorly disciplined forces.

Candidates:

- `BanditAdaptiveSkirmish`
- `HarassingAdvance`
- `AmbushPressure`
- `PanicRush`

Expected behavior:

- avoid fair fights
- prefer mobility and spacing
- attack only when local advantage appears
- break off if the enemy is too strong

## Signature Historical Inspirations

These should be adapted to Bannerlord instead of copied literally.

### `HammerAndAnvil`

Historical idea:

- infantry pins
- cavalry strikes flank or rear

Bannerlord version:

- infantry engages frontally
- cavalry waits for enemy fixation
- cavalry commits after contact stabilizes

### `ObliqueOrder`

Historical idea:

- reinforce one wing
- delay the rest

Bannerlord version:

- one side advances earlier
- weaker wing stays conservative
- center avoids reckless push

### `CannaeEnvelopment`

Historical idea:

- softer center absorbs
- wings close inward

Bannerlord version:

- center gives limited ground
- flanks hold stronger posture
- reserve or cavalry closes once enemy center advances too deep

### `FeignedRetreat`

Historical idea:

- lure the enemy into disorder

Bannerlord version:

- mounted or light units retreat under control
- enemy is drawn forward
- hidden or delayed force counterattacks

This should be hard to unlock and mainly used by suitable factions or compositions.

### `ShieldwallAdvance`

Historical idea:

- dense line advances with discipline

Bannerlord version:

- infantry keeps cohesion
- archers support from behind
- cavalry protects sides rather than charging early

## Culture / Faction Bias

Not all factions should prefer the same doctrine pool.

The idea is not to lock everything absolutely, but to bias selection.

Example style map:

- empire-style armies:
  - `ElasticDefense`
  - `ReserveCounterattack`
  - `HammerAndAnvil`
  - `DisciplinedStagedAdvance`

- sturgia-style armies:
  - `ShieldwallAdvance`
  - `CorneredStand`
  - `RefusedFlank`

- vlandia-style armies:
  - `HammerAndAnvil`
  - `AggressiveWingPressure`

- khuzait-style armies:
  - `FeignedRetreat`
  - `HorseArcherLure`
  - `HarassingAdvance`

- battania-style armies:
  - `AmbushPressure`
  - `ObliqueOrder`
  - `HarassingAdvance`

- bandits / raiders:
  - `BanditAdaptiveSkirmish`
  - `AmbushPressure`
  - `PanicRush`
  - `CorneredStand`

For your mod, custom cultures can have their own doctrine table later.

## Doctrine Selection Model

Recommended model:

1. Build a list of candidate doctrines for the team.
2. Remove doctrines that fail hard requirements.
3. Score the remaining doctrines.
4. Select the highest score.
5. If no doctrine reaches a minimum score, fall back to vanilla or simple RF doctrine.

### Hard Requirements

Examples:

- minimum `Tactics` skill
- required cavalry ratio
- required horse archer ratio
- not enough troops for multi-phase doctrine
- faction not allowed to use that doctrine

### Soft Score Inputs

Examples:

- commander `Tactics`
- army composition fit
- current power ratio
- terrain suitability
- current distance to enemy
- whether the army is attacker or defender
- morale / cohesion proxy if available

## Doctrine Runtime States

Each doctrine should be state-driven, like the current bandit tactic.

Recommended pattern:

- `Approach`
- `Probe`
- `Commit`
- `Exploit`
- `Recover`
- `Fallback`

Not every doctrine needs every state.

Example:

- `HammerAndAnvil`
  - `Approach`
  - `PinEnemy`
  - `FlankCommit`
  - `CollapseRear`

- `FeignedRetreat`
  - `Harass`
  - `Lure`
  - `EnemyOverextended`
  - `TurnAndStrike`

## Failure Handling

This part is important.

A doctrine should not stay active if its logic stops making sense.

Examples:

- cavalry for `HammerAndAnvil` is wiped out
- line cohesion breaks during `ObliqueOrder`
- enemy refuses pursuit during `FeignedRetreat`

When that happens:

- degrade to a simpler doctrine
- or fall back to vanilla behavior

This is what will keep the system from feeling fake or brittle.

## First Implementation Roadmap

Recommended order:

1. keep current system as the base
2. add doctrine eligibility scoring
3. add one infantry doctrine
4. add one cavalry doctrine
5. add one deception doctrine
6. add faction bias
7. add `Tactics` skill gating

### Suggested First Pack

Best first pack for real gameplay value:

1. `ShieldwallAdvance`
2. `HammerAndAnvil`
3. `ObliqueOrder`
4. `FeignedRetreat`
5. improved `BanditAdaptiveSkirmish`

## Recommended First Coding Pass

The safest next coding step is:

1. create a doctrine definition model
2. add commander `Tactics` skill lookup
3. score available doctrines before team tactic registration
4. keep current existing tactics as eligible candidates
5. print the chosen doctrine through the current debug system

That gives immediate visibility in battle without risking a full rewrite.

## Success Criteria

This system is successful when:

- vanilla still handles battles correctly when no doctrine fits
- weak lords use simpler tactics
- strong lords use smarter and rarer tactics
- army composition visibly affects doctrine choice
- cultures feel different without becoming scripted robots
- debug output clearly shows which doctrine was selected and why

## Short Version

The design direction is:

- keep vanilla as the foundation
- add RF doctrines as optional tactical overlays
- unlock better doctrines through `Tactics` skill
- bias doctrine pools by faction and army composition
- use state-based tactics that can gracefully fall back

That makes the AI feel smarter without making it feel artificial.
