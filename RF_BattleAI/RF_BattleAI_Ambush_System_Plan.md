# RF Battle AI Ambush System Plan

Goal: add a real ambush layer for field battles where some formations can stay hidden until a reveal condition happens.

This is not just a scene prefab problem.
Vanilla tactical positions help AI choose where to fight, but vanilla team queries still know enemy positions globally.
So the ambush system must combine scene markers with custom AI awareness rules.

## 1. What Vanilla Already Gives Us

- `TacticalPosition`: battle point used by AI for defend/high ground/choke point logic.
- `TacticalRegion`: region that groups useful tactical positions.
- `StrategicArea`: mainly for detachments and holding useful spots.

Useful conclusion:
- We can use scene prefabs and tags as ambush anchors.
- But hidden troops need custom logic on top.

## 2. Core Design

We create a small system with 3 layers:

1. Scene Layer
- New prefab/script marker for ambush spots.
- Example name: `RFAmbushPosition`
- It stores:
  - ambush side or neutral
  - radius
  - reveal distance
  - optional direction/front arc
  - optional terrain type hint like forest, rocks, ridge

2. Mission Layer
- New mission behavior tracks:
  - all ambush positions in the scene
  - formations assigned to ambush
  - which ambush groups are still hidden
  - reveal state and timing

3. AI Awareness Layer
- Hidden formations should be ignored or heavily discounted by enemy tactical reasoning until reveal.
- That means overriding our own RF doctrine/tactic logic first.
- Later, if needed, we can patch deeper vanilla queries.

## 3. Safe First Version

The first version should be simple and stable:

- Only for `FieldBattle`
- Only for full formations, not individual agents
- Only for ambush-ready formations like:
  - infantry
  - archers
  - maybe light cavalry later
- Only our RF tactics react to hidden state at first

Reveal triggers:

- enemy enters reveal radius
- hidden formation attacks
- hidden formation moves too far from ambush anchor
- optional timer fail-safe

## 4. Phase Breakdown

### Phase A - Scene Marker

Create `RFAmbushPosition` script component or equivalent mission object.

Needed data:
- `AmbushId`
- `AllowedSide`
- `Radius`
- `RevealDistance`
- `FacingDirection`
- `OneUse`

Result:
- scene can declare exact ambush points

### Phase B - Mission Registry

Create `RFAmbushMissionBehavior`.

Responsibilities:
- scan mission for ambush markers
- register available ambush spots
- assign candidate formations
- track hidden/revealed state

Result:
- battle has a clean ambush state manager

### Phase C - Hidden Formation Model

Create an internal record like:

- formation
- ambush anchor
- hidden bool
- reveal reason
- reveal time

Result:
- we can query if a formation is hidden at any moment

### Phase D - RF AI Integration

Update RF tactic selection and behavior logic:

- friendly AI can hold ambush troops in place
- main line acts as bait if doctrine wants it
- hidden troops are not counted as fully committed until reveal

Good fits:
- bandit doctrine
- feigned retreat
- refused flank
- future dedicated ambush doctrine

### Phase E - Enemy Awareness Suppression

First pass:
- our RF logic ignores hidden enemy formations for doctrine scoring and behavior decisions

Second pass, only if needed:
- patch deeper vanilla team/formation query usage
- especially average enemy position and target formation selection

This phase is the risky one.

## 5. Best Tactical Use Cases

The best first tactics for this system:

1. Bandit ambush
- small hidden archer/infantry force in forest

2. Refused flank trap
- one wing looks weak
- hidden reserve on the side

3. Feigned retreat trap
- visible force pulls enemy forward
- hidden force hits flank/rear after reveal

4. Future dedicated doctrine
- `TacticAmbushCountermarch`
- or `TacticHiddenReserveStrike`

## 6. Known Limits

- Vanilla still tracks enemy agents very broadly.
- So true stealth is not free.
- A prefab alone will not create hidden troops.
- Real ambush requires custom handling of AI knowledge.

Most realistic target:
- enemy behaves as if hidden formations are not tactically relevant until reveal
- not perfect physical invisibility of all AI systems on day one

## 7. Recommended Build Order

When we return to this:

1. build scene marker
2. build mission registry
3. add hidden formation state
4. plug into RF tactics only
5. test reveal logic
6. only then decide if deeper vanilla query patches are necessary

## 8. Minimum Success Criteria

We can call version 1 successful if:

- mapper can place ambush markers in battle scenes
- one formation can start hidden on that marker
- enemy RF AI does not react to that formation before reveal
- reveal happens by distance or attack
- hidden formation joins battle cleanly after reveal

## 9. Future Expansion

Later we can add:

- culture-based ambush skill
- tactics-skill requirement
- terrain-specific ambush bonuses
- player ambush deployment option
- lord traits influencing reveal discipline
- false retreat into hidden reserve combo

## 10. File Target For Future Work

Suggested future files:

- `RFAmbushPosition.cs`
- `RFAmbushMissionBehavior.cs`
- `BattleAIAmbushController.cs`
- `BattleAIAmbushState.cs`
- integration into doctrine/tactic files as needed

Status:
- research done
- implementation intentionally postponed
