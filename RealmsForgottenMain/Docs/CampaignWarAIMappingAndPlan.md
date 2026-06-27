# Campaign War AI Mapping And Improvement Plan

This note maps how Bannerlord 1.3.x decides wars and military targets, then compares that with the current Realms Forgotten layers.

The goal is simple:

1. understand what vanilla already does
2. understand where RF overrides it
3. define a practical plan to make campaign war AI much better

## Files Studied

### Vanilla

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\Vanilla_1.3.x\MEGA_010.md`
  - `KingdomDecisionProposalBehavior`
  - `DeclareWarDecision`
  - `MakePeaceKingdomDecision`
  - `DefaultDiplomacyModel`
  - `AiMilitaryBehavior`
  - `DefaultTargetScoreCalculatingModel`
  - `AiPartyThinkBehavior`

### Realms Forgotten

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\AiSubModule.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\RF_Diplomacy\AlignmentDiplomacyModel.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\RF_Diplomacy\AlignmentWarManager.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\RF_Diplomacy\AlignmentMomentumBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\AseraiCollectiveDefenseBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RFReligions\Behavior\CrusadeBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\RFCampaignAITraceBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\RFCampaignAITraceLog.cs`

## Vanilla War Decision Flow

Vanilla does not use one single "war AI brain".

It is split into layers:

1. a clan decides whether to propose a war or peace vote
2. the kingdom vote system decides whether that proposal has support
3. the diplomacy model gives the actual war or peace score
4. once war exists, military AI chooses concrete targets on the map

### 1. Proposal Layer: who tries to start war

`KingdomDecisionProposalBehavior` runs daily on clans.

Important gates:

- the campaign must be older than 5 days
- clan must not be eliminated
- clan must not be the player clan
- clan must belong to a kingdom
- clan must have at least 100 influence
- clan must have positive total strength

For war specifically:

- the kingdom must not already have an unresolved `DeclareWarDecision`
- the target kingdom must not already be at war with them
- more than 20 days must have passed since the last peace
- the proposer must have enough influence to pay the proposal cost

Important vanilla limitation:

- the system first picks a random valid kingdom candidate
- then checks if that candidate is worth attacking

So vanilla is not fully searching all enemies and picking the single best one every time.

### 2. Diplomacy Score Layer: is the war worth it

`DefaultDiplomacyModel.GetScoreOfDeclaringWar(...)` is the core score.

Hard blocks:

- declaring faction strength must be above `500`
- declaring faction must have at least `2` war parties
- exposure to the enemy must be valid; if the enemy is too impractical to reach, war is rejected

Main formula:

`sameCultureTownScore + benefitScore * exposureScore * allianceFactor - riskScore + relationScore`

In plain language, vanilla looks at:

- how strong we are
- how strong they are
- how exposed or reachable they are
- how valuable their lands are
- how risky the war is
- whether they hold same-culture lands
- current relations
- tribute pressure
- ally and anti-ally effects

### 3. Political Support Layer: can the war pass internally

Even if the war has a good diplomacy score, it still goes through `DeclareWarDecision`.

`DeclareWarDecision.DetermineSupport(...)` uses:

- `DeclareWarBarterable` value converted into influence
- lord personality traits
  - `Valor` pushes support up
  - `Mercy` pushes support down

So a war can be strategically tempting, but politically weaker if the internal clan mood is wrong.

Vanilla also adds a final chance gate:

- support must be above a threshold
- election likelihood must be good enough
- a random roll still decides whether the proposal is actually created

This is one reason campaign diplomacy can feel uneven or inconsistent.

### 4. Peace Layer

Peace uses the same general pattern:

- proposal behavior
- diplomacy model score
- internal kingdom decision

`DefaultDiplomacyModel` also uses war progress to affect peace appetite.

War progress includes:

- casualties
- sieges
- captured settlements
- raids

So vanilla does react to how the war is going, but in a fairly broad way.

## Vanilla Military Target Selection

Once war exists, a different system chooses what parties and armies do on the map.

### 1. Military role selection

`AiMilitaryBehavior` evaluates three main military mission types:

- `Defender`
- `Besieger`
- `Raider`

The system checks:

- army cohesion
- food
- party size ratio
- objective type
- whether the party is already attached to another army

### 2. What targets are even considered

For `Defender`:

- only own faction settlements are considered

For `Besieger` and `Raider`:

- only settlements of factions already at war are considered

### 3. How target score is built

`DefaultTargetScoreCalculatingModel.GetTargetScoreForFaction(...)` is the core settlement score.

It looks at:

- travel distance
- local defender strength
- nearby reinforcements
- our strength versus required strength
- town walls
- town food
- settlement value
- whether we already target that settlement
- whether a friendly siege is already there
- relation to the owner clan
- objective bias

For defenders it also cares about:

- current attacker
- how many allies are already responding
- whether the attacker is still close enough to matter

For raiding and besieging it also cares about:

- whether we have enough food
- whether the target is already occupied
- whether the target is strong enough to resist
- how much enemy relief force is likely nearby

### 4. Execution Layer

`AiPartyThinkBehavior` is the layer that finally picks the winning AI behavior and applies it:

- defend settlement
- besiege settlement
- raid settlement
- patrol
- visit settlement

It can also decide that a lord should create an army first, then run the mission through that army.

## What Realms Forgotten Currently Changes

RF currently changes campaign war logic in more than one way.

### 1. `AlignmentDiplomacyModel`

This model wraps vanilla diplomacy.

Important current behavior:

- it blocks some diplomacy outcomes across opposed alignments
- it blocks peace while `AlignmentWarBehavior.IsActive`
- it does **not** replace vanilla `GetScoreOfDeclaringWar(...)`

Meaning:

- vanilla still does the normal war scoring in many ordinary cases
- RF mostly changes what is allowed, not the full strategic brain

### 2. `AlignmentWarBehavior`

This is not organic diplomacy.

It is a global event system:

- once triggered, it places good and evil kingdoms onto opposite sides
- kingdoms added to opposite sides are forced into war
- the war remains active as a special state

So this layer can bypass normal vanilla war buildup entirely.

### 3. `AlignmentMomentumBehavior`

This layer pushes the alignment war forward by:

- tracking battle wins
- tracking settlement captures
- promoting neutral cultures to good or evil after momentum thresholds
- restoring war if peace happens between opposing alignment blocs

This makes the alignment war very stable, but also much less organic than vanilla.

### 4. `CrusadeBehavior`

This is also event-driven rather than normal vanilla diplomacy.

It checks:

- piety
- religion hostility
- valid target settlement

Then it:

- declares war on the target kingdom
- calls same-faith kingdoms into the crusade

This is strong for flavor, but currently separate from a larger strategic planner.

### 5. `AseraiCollectiveDefenseBehavior`

This is another special-case coalition system.

When an external kingdom attacks an Aserai realm:

- Aserai internal wars can be ended
- Aserai kingdoms can be rallied
- multiple Aserai kingdoms can be forced into war against the attacker

Again, this is a hand-authored override, not the vanilla general-purpose war brain.

### 6. `RFCampaignAITraceBehavior`

This behavior logs lord campaign AI changes:

- behavior changes
- target settlement changes
- target party changes
- army attachment changes
- nearby siege state
- nearest enemy distance

This is useful for study, but it writes every change to disk and can create noticeable lag if always on.

In `AiSubModule.cs` it is currently disabled for normal play, which is good.

## Current Diagnosis

This is the most important takeaway.

RF has many strong war-themed systems, but they are not yet unified into one strategic campaign AI layer.

### What vanilla does well

- it already understands basic reachability
- it already understands strength comparison
- it already understands food and cohesion
- it already distinguishes defense, siege, and raid
- it already reacts to war progress for peace

### What vanilla does poorly

- target kingdom selection is partly random at proposal time
- there is weak long-term memory
- there is weak theater-level planning
- lords can look indecisive or wander too much
- there is limited sense of phased war operations
- there is no deep identity per kingdom beyond existing traits and scores

### What RF currently does well

- strong thematic war events
- religion and alignment matter
- coalition responses exist
- some custom diplomatic blocking already exists

### What RF currently does poorly

- several systems force wars without sharing one unified planner
- strategic target choice is still mostly vanilla
- special war systems can feel scripted instead of intelligent
- campaign AI can still drift, delay, or attack in poor sequence
- there is no single RF layer deciding:
  - why this war now
  - why this target now
  - why this army should regroup now
  - why this theater matters more than another

## Improvement Plan

The best path is not to rewrite everything at once.

The best path is to add stronger RF strategic layers above vanilla, one phase at a time.

## Phase 1: Clean Observation Layer

Goal:

- make campaign AI readable without reintroducing heavy lag

What to do:

- keep `RFCampaignAITraceBehavior` disabled by default
- add a lightweight toggleable trace mode
- only log major state changes:
  - war declared
  - peace declared
  - army created
  - army type selected
  - target settlement selected
  - target abandoned
  - siege started
  - raid started
  - regroup mode entered

Why this matters:

- without clean observation, future AI tuning becomes guesswork

Risk:

- low

Priority:

- very high

## Phase 2: Better Strategic War Evaluation

Goal:

- replace “good enough vanilla war score” with a more believable RF war appetite

Recommended shape:

- extend the current diplomacy model with an RF strategic war evaluation service
- do not throw away vanilla numbers; use them as one input

New factors worth adding:

- number of open fronts
- army readiness
- treasury health
- food pressure
- recent battle losses
- recent siege failures
- frontier vulnerability
- coastline and naval access
- religious hostility
- alignment hostility
- same-culture claims
- crusade target value
- coalition availability

What this improves:

- fewer dumb wars
- better timing
- more believable pauses after major defeats
- more credible holy wars and coalition wars

Risk:

- medium

Priority:

- very high

## Phase 3: Better Target Selection

Goal:

- make kingdoms choose smarter objectives after war starts

Recommended shape:

- introduce an RF target scoring layer on top of vanilla `DefaultTargetScoreCalculatingModel`
- do not replace the entire thing immediately

New target factors worth adding:

- theater importance
- border continuity
- whether capturing this target opens the next one
- relief risk
- nearby allied support
- strategic choke points
- capital pressure
- supply reach
- whether target is already isolated
- whether target belongs to a hated faith or alignment

Also add anti-stupidity penalties:

- avoid far-away low-value raids while a home siege is active
- avoid splitting armies on low-value raids during major wars
- avoid besieging impossible targets with low food

What this improves:

- fewer random-feeling raids
- more coherent front lines
- better campaign pacing

Risk:

- medium

Priority:

- very high

## Phase 4: Operational Rhythm For Lords And Armies

Goal:

- stop the feeling that lords keep spinning, drifting, or changing plans too fast

Recommended shape:

- create an RF operational state layer for campaign armies

Suggested states:

- muster
- advance
- contain
- besiege
- raid
- defend
- recover
- exploit
- regroup

Important rule:

- each state should have a minimum hold time unless something serious changes

Examples:

- a siege army should not abandon target every few hours
- a defeated army should enter `recover` or `regroup`
- a strong advantage after victory should enter `exploit`

What this improves:

- less wobbling
- more readable wars
- more believable generals

Risk:

- medium to high

Priority:

- high

## Phase 5: Kingdom Strategic Personality

Goal:

- make different kingdoms wage war differently

Recommended shape:

- define kingdom doctrine profiles

Examples:

- crusading kingdom:
  - values holy targets highly
  - tolerates longer campaigns
  - more likely to join coalition war

- raider kingdom:
  - prefers villages and weak border regions
  - avoids deep sieges unless advantage is large

- imperial kingdom:
  - values same-culture recovery and road continuity
  - prefers fortification chains

- desperate kingdom:
  - avoids offensive wars
  - prioritizes defense and peace

This should affect:

- war appetite
- target weighting
- peace appetite
- army operation choice

Risk:

- medium

Priority:

- high

## Phase 6: Unify RF Event Wars With Strategic AI

Goal:

- stop crusades, alignment wars, and cultural coalitions from feeling like disconnected scripts

Recommended shape:

- event systems should declare strategic intent
- the new RF planner should decide how that intent is executed

Examples:

- crusade:
  - defines sacred target
  - boosts support and target score for crusader kingdoms
  - does not need to manually hard-force every downstream behavior

- alignment war:
  - defines enemy bloc
  - increases hostility and reduces peace willingness
  - still lets the planner pick the best theater and target

- Aserai collective defense:
  - defines coalition response
  - planner decides whether the response is:
    - defend homeland
    - counter-invade
    - relieve siege

What this improves:

- less script feel
- more consistent campaign logic
- fewer contradictory systems

Risk:

- medium to high

Priority:

- high

## Recommended Implementation Order

If the goal is best gain for lowest pain, the order should be:

1. Phase 1: clean observation layer
2. Phase 2: better strategic war evaluation
3. Phase 3: better target selection
4. Phase 4: operational rhythm
5. Phase 6: unify event wars with planner
6. Phase 5: kingdom personalities

Why this order:

- first we need clarity
- then we improve war timing
- then we improve target choice
- then we improve execution rhythm
- only after that should we heavily specialize factions

## Concrete Next Build Targets

The next implementation pass should likely create these pieces:

### 1. `RFKingdomWarEvaluationService`

Purpose:

- compute RF strategic war appetite
- combine vanilla diplomacy score with RF-specific factors

### 2. `RFTargetScoreModel`

Purpose:

- extend or wrap vanilla target scoring
- push armies toward better settlements and better fronts

### 3. `RFOperationalCampaignBehavior`

Purpose:

- give parties and armies sticky operational states
- reduce wandering and jitter

### 4. `RFCoalitionWarCoordinator`

Purpose:

- unify crusade, alignment war, and collective defense outputs

### 5. `RFCampaignAIDebugSnapshot`

Purpose:

- record important strategic decisions without the lag of full high-frequency tracing

## Bottom Line

Right now, RF has strong war flavor but not yet one unified strategic war brain.

Vanilla already gives:

- war scoring
- voting
- settlement target scoring
- food and cohesion checks

RF should not throw that away.

RF should build above it:

- better reasons to fight
- better reasons to wait
- better target priorities
- better operational rhythm
- stronger kingdom identity
- cleaner integration of crusade, alignment, and coalition wars

That is the path that should make campaign wars feel both smarter and more thematic.

## Where RF_warsystem Stands Now

Since this note started, the dedicated `RF_warsystem` layer has already moved beyond the first draft.

These pieces now exist:

- `RFWarSystemDiplomacyModel`
  - wraps vanilla diplomacy war and peace scores
- `RFWarSystemTargetScoreCalculatingModel`
  - wraps vanilla target scoring
- `RFWarStrategicMemoryBehavior`
  - stores rivalry, momentum, settlement heat, home-front pressure, and front momentum
- `RFWarOperationalRhythmBehavior`
  - gives kingdoms a campaign rhythm like pressure, recovery, and siege appetite
- `RFWarTheaterBehavior`
  - tracks homeland defense and theater importance
- `RFWarCampaignDirectorBehavior`
  - gives each kingdom a primary enemy focus and reduces random front-hopping
- `RFWarDecisionPlannerBehavior`
  - daily planner that searches war and peace candidates instead of letting vanilla stay mostly random

In plain language:

- vanilla still provides the base numbers
- `RF_warsystem` now pushes those numbers with memory, focus, pressure, theater logic, and kingdom doctrine
- this already gives a much stronger campaign-level brain than stock vanilla

## Practical Plan To Make It Much Better

The next best improvements are no longer the basic foundation. That part is already in.

What we need now is refinement, visibility, and deeper control.

### Phase A: Clear Strategic Telemetry

Goal:

- see exactly why a kingdom chose war, peace, or a target

What to add:

- a lightweight `RFWarSystemTraceLog`
- one log line for:
  - chosen primary enemy
  - focus strength
  - war proposal accepted or rejected
  - peace pressure accepted or rejected
  - top 3 target settlements
  - home-front panic level
  - multi-front penalty
  - coalition pull

Why first:

- without this, future tuning becomes blind guesswork

Current status:

- mostly started already
- `RFWarSystemTraceLog` now records:
  - focus changes
  - war proposals
  - peace proposals
  - target score windows
- next refinement here should be:
  - top 3 target dumps instead of only best-window reporting
  - cleaner throttling so long campaigns do not create useless spam
  - optional per-kingdom filtering for easier reading

### Phase B: Make Kingdom Focus More Persistent

Goal:

- stop kingdoms from changing strategic enemy too easily

What to improve:

- longer hold time on primary enemy
- stronger penalties for opening a second or third war while the main front is unresolved
- stronger peace resistance when the focused enemy still threatens homeland fiefs
- better distinction between:
  - revenge war
  - border war
  - holy war
  - opportunistic expansion

Expected result:

- cleaner fronts
- fewer nonsense wars
- more readable long campaigns

### Phase C: Smarter Target Chains Instead Of Single Targets

Goal:

- make armies think in sequences, not isolated picks

What to add:

- "capture chain" logic:
  - border castle
  - supporting village belt
  - adjacent town
- prefer settlements that open the next strategic step
- avoid targets that create a deep isolated pocket
- reward targets that cut roads, ports, or reinforcement paths

Expected result:

- wars feel like campaigns instead of random raids

### Phase D: Stronger Army-State Discipline

Goal:

- reduce wobbling, spinning, and abandoning plans too fast

What to improve:

- stronger minimum hold time on states like:
  - muster
  - advance
  - besiege
  - recover
  - regroup
- explicit break conditions for changing state
- post-defeat recovery before the kingdom resumes offensive ambition
- post-victory exploitation if the front is collapsing

Expected result:

- armies behave like they have commanders, not mood swings

### Phase E: Better Coalition Intelligence

Goal:

- make crusades, alignment blocs, and collective defense act like one strategic layer

What to improve:

- event systems should feed intent into `RF_warsystem`
- `RF_warsystem` should decide:
  - which enemy matters most
  - which front gets priority
  - whether to defend, invade, or relieve
- reduce hard-forced war scripting where possible

Expected result:

- more organic religious and alignment wars
- fewer contradictions between systems

### Phase F: Kingdom Personalities That Really Matter

Goal:

- make each kingdom feel different in campaign war

What to deepen:

- doctrine profiles should influence:
  - when they start wars
  - how long they stay in wars
  - what they target
  - whether they raid, besiege, or defend
  - how much they care about same-culture reconquest
  - how much they respect coalition calls

Expected result:

- Nord, Imperial, Aserai, raider, crusader, and defensive realms stop feeling like the same AI with different skins

## Best Execution Order From Here

The strongest order now is:

1. finish telemetry refinement
2. focus persistence
3. target chains
4. army-state discipline
5. coalition intelligence
6. deeper kingdom personality

This order matters because:

- first we need visibility
- then we stabilize the strategic brain
- then we improve map choices
- then we improve army follow-through
- only after that do we specialize factions harder

## Short Practical Read

If the goal is to make campaign war AI feel much smarter in play, the real priorities are these:

1. make kingdoms stick to one main enemy long enough
2. make them attack in strategic chains instead of hopping to random fiefs
3. make armies hold a campaign state longer before changing their mind
4. let crusades, alignment wars, and coalition events feed one shared planner
5. make faction personality affect not just war score, but actual campaign behavior

That combination is what turns "better numbers" into a war AI that actually feels intentional.

## Simple Conclusion

The project is no longer at the stage of "can we improve vanilla at all?"

That part is already yes.

Now the real work is:

- making decisions visible
- making focus persistent
- making campaigns sequential
- making armies commit better
- and making faction identity shape the entire war

That is the route to a war AI that feels truly smarter, not just more aggressive or more scripted.
