# RF War System Mapping And Betterment Plan

This is the clean map of what the `RF_warsystem` is doing right now, and the best plan to make it much better without turning it into a mess.

## 1. The simple truth

The campaign war AI is already no longer "just vanilla".

Right now the real chain is:

1. Bannerlord still gives the base diplomacy score and base target score
2. `RF_warsystem` wraps both of those
3. RF strategic layers keep changing pressure, focus, front, objective, and tempo
4. event systems like crusade, alignment war, and collective defense inject extra pressure
5. the final army and party behavior still belongs to Bannerlord practical movement AI

So the strategic brain is increasingly RF, while the last movement execution is still mostly vanilla.

That is a good base.

## 2. What files matter most today

Main registration:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\RFWarSystemRegistrar.cs`

Main model wrappers:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Models\RFWarSystemDiplomacyModel.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Models\RFWarSystemTargetScoreCalculatingModel.cs`

Main strategic logic:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarStrategicHeuristics.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarStrategicIntent.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarFrontEvaluator.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarStrategicProfiles.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarExternalFrontContext.cs`

Main campaign behaviors:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarStrategicMemoryBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarCoalitionRoleBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarOperationalRhythmBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarTheaterBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarFrontlineBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarObjectiveChainBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarCampaignDirectorBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarDecisionPlannerBehavior.cs`

External event bridge:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\RFWarExternalIntentApi.cs`

## 3. What each layer is really doing

### A. `RFWarSystemDiplomacyModel`

This layer does not replace Bannerlord from zero.

It takes the vanilla diplomacy score and pushes it up or down.

So RF already changes:

- desire to start war
- desire to accept peace
- resistance to leaving important wars too early

### B. `RFWarSystemTargetScoreCalculatingModel`

This does the same thing for target choice.

Bannerlord still gives the base practical score.
RF then reshapes that score with strategy.

So parties and armies are not being told to ignore the base game.
They are being steered.

### C. `RFWarDecisionPlannerBehavior`

This is the proposal engine.

Every day it checks kingdoms and tries to create:

- war proposals
- peace proposals

That means RF is not only "changing numbers".
It is actively pushing political decisions into the kingdom system.

### D. `RFWarCampaignDirectorBehavior`

This is the layer that answers:

- who is our main enemy right now
- how hard should we stay focused on that enemy
- how much should we resist opening a random new war
- how much should we resist peacing out too early

This is one of the most important files in the whole system.

### E. `RFWarTheaterBehavior`

This decides the broad mode of a war:

- homeland defense
- offensive push

So this is the "which side of the war matters most" layer.

### F. `RFWarFrontlineBehavior`

This is newer and important.

It does not just say "fight that kingdom".
It tries to choose an anchor settlement for the active front.

So the system now has a real idea of:

- where the line of war should be
- what cluster of settlements should stay hot

### G. `RFWarObjectiveChainBehavior`

This narrows things even further.

It tries to keep a concrete operational objective alive long enough that the AI stops behaving like a headless chicken.

So the chain is now roughly:

1. choose enemy
2. choose theater
3. choose frontline anchor
4. choose current objective

That is already much better than raw vanilla.

### H. `RFWarOperationalRhythmBehavior`

This gives the war a tempo state.

Examples:

- muster
- advance
- besiege
- defend
- regroup
- exploit

This is what reduces wobble.

### I. `RFWarCoalitionRoleBehavior`

This is the coalition brain.

It lets kingdoms in a broader war drift into different roles such as:

- spearhead
- border shield
- siege finisher
- raider
- reserve

That is a very good direction, because allied kingdoms should not all behave like clones.

### J. `RFWarStrategicMemoryBehavior`

This is the memory bank.

It stores and exposes things like:

- momentum
- rivalry
- war commitment
- settlement heat
- front momentum
- home front pressure

Without this layer, the AI forgets too fast.

## 4. What the target scoring already considers

The current target score in `RFWarStrategicHeuristics.AdjustTargetScore(...)` already mixes a lot:

- defense urgency
- culture claim
- faction doctrine
- frontier contact
- alignment hostility
- settlement memory
- front memory
- rivalry
- war commitment
- front evaluation
- strategic intent
- theater
- frontline anchor
- campaign director focus
- objective chain
- operational state
- coalition role
- offensive overextension penalty

So the system is not shallow.

The problem is not lack of ingredients.

The problem is still authority and sequencing.

## 5. What the war-start logic already considers

The war and peace planner already uses:

- war intent
- opportunity window
- enemy focus
- coalition pull
- frontier pressure
- claim pressure
- enemy multi-front weakness
- treasury distress
- home pressure
- momentum
- peace hold
- external enemy priority

This means the RF war-start logic is already much smarter than a random declare-war system.

## 6. What the external systems are doing

Outside systems already talk to RF through:

- `ReinforceHolyWar(...)`
- `ReinforceCollectiveDefense(...)`
- `ReinforceAlignmentWar(...)`

and through the external context RF now stores:

- holy war pressure
- collective defense pressure
- alignment war pressure
- sacred target factor

That bridge is good and worth keeping.

## 7. The real current weakness

The biggest remaining weakness is this:

RF has many strong layers, but some outside event systems still behave like separate war brains.

So the world can still get pulled by:

- the RF strategic planner
- crusade hard-force logic
- alignment war hard-force logic
- collective defense hard-force logic

That is where weirdness is born.

## 8. What is already strong enough to keep

These parts are worth keeping and deepening, not throwing away:

- diplomacy wrapper approach
- target wrapper approach
- strategic memory
- campaign director
- theater focus
- frontline anchor
- objective chain
- operational rhythm
- coalition roles
- external intent bridge

This is the core of the real long-term war brain.

## 9. What is still missing for the system to feel truly excellent

### Missing piece 1: one clean campaign phase layer

Right now the system knows enemy, front, and objective.

What it still lacks is a clearer "campaign phase" on top of that, such as:

- break the border
- strip support settlements
- isolate fortress
- press town
- deep exploitation
- stabilize gains

Without this, the AI can still choose good targets but not always in the best order.

### Missing piece 2: cleaner authority over special wars

Crusade and similar systems should mostly feed intent, not act like parallel diplomats forever.

### Missing piece 3: stronger peace discipline

Peace should feel earned or necessary, not merely mathematically available.

### Missing piece 4: better front persistence

Even with frontline anchors, long campaigns can still lose shape if the system does not lock the front hard enough during a real operation.

### Missing piece 5: safer player command integration

The Army Commander side is still brittle because the patch entry point changed in the current API.

That should not be mixed into the strategic work until the strategic layer is stable and the patch target is corrected.

## 10. The best plan to make it much better

### Phase 1. Unify authority

Goal:

- make RF the clear final war brain

Do:

- keep `RFWarSystemDiplomacyModel` as the final diplomacy wrapper
- audit all special war systems that still directly force war/peace
- reduce brute-force declarations where RF intent can now handle the same result
- let event systems say "this war matters" instead of constantly saying "declare right now"

Expected gain:

- fewer contradictions
- fewer weird flips
- more believable grand strategy

### Phase 2. Add a campaign phase system

Goal:

- make wars advance in a more logical order

Do:

- add a dedicated behavior for campaign phases per `kingdom -> enemy`
- store a phase with a short hold timer
- base the phase on:
  - current theater
  - current frontline
  - current objective
  - momentum
  - homeland pressure
  - whether border castles are still standing
  - whether same-cluster villages are still alive

Suggested phases:

- `BreakFront`
- `StripSupport`
- `PressCastle`
- `PressTown`
- `DeepStrike`
- `Stabilize`

Expected gain:

- less target hopping
- better order of operations
- wars feel more like campaigns

### Phase 3. Make frontline logic even stronger

Goal:

- make whole fronts more persistent

Do:

- keep the current frontline anchor system
- increase weight for same-cluster objectives while the phase is active
- add "front exhausted" and "front unresolved" states
- let the system explicitly prefer finishing a front before opening a different one

Expected gain:

- less scattered pressure
- cleaner offensives

### Phase 4. Deepen coalition behavior

Goal:

- make coalitions behave like organized coalitions

Do:

- keep the existing role system
- make role choice also influence:
  - war appetite
  - peace resistance
  - front choice
  - objective type
- let holy wars and collective defense automatically bias role assignment

Expected gain:

- allies stop acting like copies of each other
- crusades and defense leagues become much more readable

### Phase 5. Improve war-start target kingdom selection

Goal:

- remove the last "why this enemy?" feeling

Do:

- keep the current planner
- deepen the enemy selection score with:
  - border quality
  - real claim density
  - sacred target pull
  - enemy overextension
  - coalition readiness
  - naval exposure later if needed

Expected gain:

- smarter escalation
- fewer nonsense wars

### Phase 6. Improve peace logic

Goal:

- stop peace from undercutting a live campaign too early

Do:

- only let peace pressure rise hard when:
  - momentum is truly bad
  - treasury distress is real
  - homeland is threatened
  - coalition purpose is broken
  - current front has clearly stalled
- make sacred wars and homeland emergency wars harder to abandon

Expected gain:

- more dramatic and believable war arcs

### Phase 7. Deepen faction personalities

Goal:

- make kingdoms feel different

Do:

- expand `RFWarStrategicProfiles`
- deepen axes like:
  - coalition loyalty
  - revenge depth
  - siege patience
  - home guard instinct
  - opportunism
  - raid obsession
  - sacred zeal
  - naval appetite

Expected gain:

- wars become culturally recognizable

### Phase 8. Improve diagnostics without heavy lag

Goal:

- understand decisions without spamming the game

Do:

- keep logs focused on major transitions only
- log only:
  - focused enemy changes
  - theater changes
  - frontline changes
  - objective changes
  - coalition role changes
  - campaign phase changes
  - war and peace proposal winners
- avoid high-frequency spam loops

Expected gain:

- easier tuning
- less performance drag

### Phase 9. Only after that, harden Army Commander

Goal:

- connect player war control to a stable strategic layer

Do:

- fix the broken patch target in `RFArmyManagementVMPatches`
- avoid constructor-only hooks if the API moved
- connect Army Commander to the real RF front, objective, and campaign-phase data

Expected gain:

- safer UI
- player command actually reflects the strategic brain

## 11. Best execution order

If the goal is maximum gain with minimum chaos, the order should be:

1. unify authority
2. add campaign phases
3. strengthen frontline persistence
4. deepen coalition behavior
5. improve war-start selection
6. improve peace discipline
7. deepen faction personalities
8. trim diagnostics and make them cleaner
9. only then harden Army Commander around the stable system

## 12. Bottom line

The important thing is this:

we do not need a full rewrite.

The system already has a serious foundation.

To make it much better, the next leap is not "add more random modifiers".

The next leap is:

- clearer authority
- better campaign sequencing
- stronger front persistence
- better coalition behavior
- cleaner peace discipline

That is the path from "already interesting" to "consistently strong and believable".
