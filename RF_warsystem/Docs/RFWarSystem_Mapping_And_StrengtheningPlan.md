# RF War System Mapping And Strengthening Plan

This note explains, in plain language, how Bannerlord campaign war AI works today, what Realms Forgotten already changes, what the Army Commander port is doing, and what the best improvement path is.

## 1. How vanilla decides war

Vanilla does not have one single master brain.

It works in layers:

1. a clan proposes war or peace
2. the kingdom political system checks support
3. the diplomacy model gives the war or peace score
4. once war exists, military AI chooses army targets
5. party AI turns those targets into actual movement

## 2. Vanilla war proposal layer

Main file studied:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\Vanilla_1.3.x\MEGA_010.md`
  - `KingdomDecisionProposalBehavior`

Important vanilla conditions:

- campaign must be older than 5 days
- clan cannot be eliminated
- player clan does not use this normal AI path
- clan must belong to a kingdom
- clan must have at least 100 influence
- clan must have positive strength

For war:

- the kingdom cannot already have a pending war decision
- the candidate target cannot already be at war with them
- at least 20 days must have passed since last peace

Important weakness:

Vanilla first picks a random valid kingdom candidate, then checks if war against that kingdom is good.

So even before scoring, the candidate selection is already a bit noisy.

## 3. Vanilla war score layer

Main file studied:

- `DefaultDiplomacyModel`

Important hard gates:

- strength must be above 500
- at least 2 war parties
- enemy must be practically reachable

What the score looks at:

- our strength versus theirs
- value of enemy settlements
- exposure and reachability
- risk
- relations
- tribute pressure
- same-culture towns

Simple version:

Vanilla asks:

"Can we reach them, are we strong enough, and are their lands worth the trouble?"

## 4. Vanilla peace score layer

Still handled by `DefaultDiplomacyModel`, but with war progress pushing the result.

War progress includes things like:

- losses
- sieges
- captured settlements
- raids

So peace is not random, but it is broad and not very deep.

## 5. Vanilla target selection after war starts

Main file studied:

- `DefaultTargetScoreCalculatingModel`
- `AiMilitaryBehavior`
- `AiPartyThinkBehavior`

Vanilla army roles are mainly:

- defender
- besieger
- raider
- patrol

When scoring a target, vanilla looks at things like:

- distance
- local defenders
- nearby reinforcements
- whether we have enough strength
- walls
- food
- settlement value
- whether allies are already going there
- whether another siege is already active

Simple version:

Vanilla is actually decent at local, practical decisions.

Its weak point is not "can this army reach that place?"

Its weak point is the bigger strategic layer:

- who should be the next enemy
- which front matters most
- when to stop
- when to regroup

## 6. What RF already changes today

### RF war system

Main files:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Models\RFWarSystemDiplomacyModel.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Models\RFWarSystemTargetScoreCalculatingModel.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarStrategicHeuristics.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarStrategicIntent.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarStrategicMemoryBehavior.cs`

This layer already improves:

- war appetite
- peace appetite
- target scoring
- memory of momentum
- frontier pressure
- cultural claims
- alignment hostility
- strategic intent like reconquest, holy war, expansion, survival defense

This is good. It means we are no longer purely vanilla.

### Other RF diplomacy/event layers

Main files:

- `AlignmentDiplomacyModel.cs`
- `AseraiCollectiveDefenseBehavior.cs`
- `CrusadeBehavior.cs`
- alignment war behaviors

These add:

- alignment restrictions
- crusade-style coalition wars
- Aserai collective defense
- event-driven forced wars

This gives flavor, but it also means diplomacy is currently split across several separate systems.

## 7. Important current architecture truth

Today RF campaign war logic is not one clean unified brain.

It is more like:

- vanilla strategic base
- RF war scoring layer
- RF target layer
- special event war systems
- special coalition scripts

That is why the world can feel smart in one situation and scripted in another.

## 8. What Army Commander is doing

Main files studied:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandPatches.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandMixins.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandHelpers.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandContexts.cs`

In simple terms, the ported Army Commander system is trying to add two big things:

1. a custom army overlay on the campaign map
2. a custom army management flow where the player can:
   - select an army
   - choose target settlement
   - choose behavior like defend, besiege, raid, patrol
   - send influence
   - inspect army stats

So the idea is good.

It is basically a player strategic command UI on top of Bannerlord.

## 9. Why the current Army Commander crash happens

The crash shown in Visual Studio:

- `ArgumentException: Undefined target method for patch method ... RFArmyManagementVMPatches::ConstructorPostfix(...)`

points to this patch:

- `RFArmyCommandPatches.cs`
- patch target:
  - `ArmyManagementVM` constructor with `new[] { typeof(Action) }`

That means our patch is trying to hook an old constructor signature.

Plain version:

The game's `ArmyManagementVM` changed, but our patch is still trying to enter through the old door.

So Harmony cannot find the method and crashes on load.

## 10. What is already strong

### Vanilla strong points

- local practicality
- target reachability
- food/cohesion awareness
- basic defense/siege/raid logic

### RF strong points

- better war meaning
- culture and alignment matter
- memory already exists
- target logic is already better than vanilla

## 11. What is still weak

### Vanilla weak points

- random enemy candidate selection
- weak long memory
- weak theater planning
- lords can wobble and change direction too much

### RF weak points

- too many separate diplomacy/event layers
- no single planner controlling all war systems
- Army Commander port is brittle against API changes
- campaign intent is stronger than vanilla, but operational follow-through is still not complete

## 12. The best plan to make this much better

### Phase 1: stabilize the architecture

Goal:

- stop conflicts between systems

Do:

- keep one final diplomacy wrapper chain
- make sure `RFWarSystemDiplomacyModel` is the last diplomacy layer added
- convert crusade, alignment war, and coalition systems into "intent providers" instead of each one acting like its own full war brain

Expected gain:

- less contradiction
- fewer weird diplomacy outcomes

### Phase 2: improve enemy selection before war

Goal:

- stop the kingdom from feeling random before war begins

Do:

- replace random candidate-first logic with scan-all-candidates logic inside our RF layer
- score every valid enemy kingdom
- choose the best few candidates
- bias by:
  - frontier contact
  - same-culture claims
  - religious hostility
  - treasury
  - recent losses
  - number of active fronts

Expected gain:

- smarter war starts
- less nonsense wars

### Phase 3: improve theater planning after war starts

Goal:

- make kingdoms care about fronts, not just individual settlements

Do:

- create front clusters
- score whole fronts first
- only then score targets inside the chosen front

Examples:

- homeland defense front
- reconquest front
- crusade front
- expansion front

Expected gain:

- more coherent wars
- fewer armies wandering all over the map

### Phase 4: add operational rhythm

Goal:

- reduce wobble

Do:

- armies should enter states like:
  - muster
  - advance
  - besiege
  - defend
  - regroup
  - exploit
- each state should have a minimum hold time
- only strong reasons should break the state early

Expected gain:

- armies stop changing their minds every few hours

### Phase 5: unify event wars with the planner

Goal:

- stop crusade and coalition wars from feeling detached

Do:

- crusade should set sacred target and war pressure
- alignment war should set hatred and peace resistance
- Aserai defense should set coalition response priority
- then the same RF planner decides the actual military target and pace

Expected gain:

- more organic large wars

### Phase 6: kingdom personalities

Goal:

- make factions feel different

Do:

- keep the current doctrine profiles
- expand them into:
  - raid appetite
  - siege patience
  - home guard bias
  - coalition loyalty
  - revenge memory
  - naval appetite

Expected gain:

- wars feel like they belong to those cultures

### Phase 7: Army Commander hardening

Goal:

- make the player command layer stable and future-proof

Do:

- remap every Harmony patch against the real 1.3.x signatures
- avoid constructor patching when a safer method hook exists
- isolate UI mixins from fragile constructor assumptions
- build a small compatibility layer for:
  - army management VM
  - map overlay VM
  - widget prefab names

Expected gain:

- far fewer startup crashes
- easier maintenance after game updates

## 13. Best implementation order

If we want the biggest gain for the least pain:

1. fix Army Commander patch targets
2. unify diplomacy layering
3. improve pre-war enemy selection
4. add front-based theater planner
5. add operational rhythm
6. fold crusade/alignment/coalition into the same planner
7. expand kingdom personalities

## 14. Bottom line

The good news is this:

we do not need to rebuild campaign AI from zero.

Vanilla already gives us a decent tactical skeleton for campaign movement.

What we really need is:

- smarter choice of enemy
- smarter choice of front
- less indecision
- one unified strategic planner
- a safer Army Commander UI layer

That is the path that will make the system feel not just stronger, but much more believable.
