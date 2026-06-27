# RF Enlistment Duty Pool Blueprint

Date: 2026-06-15

## Goal

Define a practical pool of duties for `RF_Enlistment` that:

- gives the player regular military activity
- does not force every duty to become a detached field mission
- respects campaign context
- can scale from simple popup duties to real detached assignments

This blueprint was informed by:

- `Enlisted\ModuleData\Enlisted\Orders\order_events\*.json`
- `tmp\ServeAsSoldier_Decompiled\ServeAsSoldier\Test.cs`
- current `RFEnlistmentCampaignBehavior.cs`

## Duty Types

The system should use three duty layers.

### 1. Passive service duty

The player remains attached to the commander and simply serves for a few hours.

Examples:

- camp watch
- garrison watch
- siege shift
- shipboard watch
- quartermaster shift
- medical shift

Use when:

- the commander is in settlement
- the commander is in siege or blockade
- the commander is at sea
- the player should not be detached

### 2. Interactive in-place duty

The player remains with the commander, but receives a popup with a small decision and outcome.

Examples:

- night patrol
- sentry watch
- train recruits
- treat wounded
- quartermaster shortage
- suspicious activity in camp

Use when:

- the player needs activity
- the army is stationary or marching
- we want flavor, trust changes, and skill XP
- we do not want a map detachment

### 3. Detached field duty

The player leaves the column, performs a real campaign task, then returns to report.

Examples:

- supply run
- forage
- road patrol
- scout route
- bandit hunt
- mounted pursuit
- trusted dispatch
- relief dispatch

Use when:

- the objective needs travel
- the player needs agency on the map
- the task has a real destination or target

## Priority Pool

These are the best duties to build first.

### Tier 1: Core duties

These should become the backbone of the system.

1. `Supply run`
2. `Bandit hunt`
3. `Scout route`
4. `Night patrol`
5. `Train recruits`
6. `Treat wounded`
7. `Trusted dispatch`
8. `Relief dispatch`

### Tier 2: Strong follow-ups

These deepen variety after Tier 1 is stable.

1. `Forage`
2. `Road patrol`
3. `Mounted pursuit`
4. `Quartermaster shortage`
5. `Gate watch`
6. `Equipment inspection`
7. `Recruitment errand`

### Tier 3: Rich campaign flavor

These are good, but not urgent.

1. `Catch deserter`
2. `Town guard incident`
3. `Escort messenger`
4. `Recover stolen stores`
5. `Medical evacuation`
6. `Engineer support`

## Duty Blueprints

### Supply run

Type:

- detached field duty

Trigger:

- commander party low on food
- not at sea
- no active duty

Flow:

1. player receives order
2. player detaches
3. player travels to target settlement
4. player must carry required food
5. food is handed over at destination
6. player returns and reports

Skills:

- Steward
- Medicine
- Engineering

Risk:

- travel delay
- lack of food in player inventory

Status:

- implemented as real food delivery

### Bandit hunt

Type:

- detached field duty

Trigger:

- infantry-heavy field duty
- roads unsafe
- commander trust not very low

Flow:

1. hostile band marked near the host
2. player detaches
3. player hunts and defeats target
4. player returns and reports

Skills:

- Tactics
- OneHanded
- Athletics

Risk:

- target escapes
- player loses fight

Status:

- implemented as detached target-hunt

### Scout route

Type:

- detached field duty with branching popup events

Inspired by:

- `scout_route_events.json`

Trigger:

- marching army
- scout-style commander or archer/scout profile

Flow:

1. player detaches to scout ahead
2. travel toward route marker or nearby settlement corridor
3. receive event popups on route:
   - bad weather
   - lone rider
   - suspicious trail
   - shortcut found
4. result improves trust, route safety, or gives failure
5. player returns and reports

Skills:

- Scouting
- Tactics
- Riding

Risk:

- ambush
- wrong call on route decision

Status:

- not yet implemented

### Night patrol

Type:

- interactive in-place duty

Inspired by:

- `sentry_duty_events.json`

Trigger:

- army stationary
- night time
- not in battle

Flow:

1. popup duty is offered
2. player accepts watch
3. one event popup fires:
   - movement in darkness
   - late relief
   - suspicious noise
   - possible enemy scout
4. result gives trust, minor injury, or commendation

Skills:

- Scouting
- Bow or Crossbow
- Tactics

Risk:

- false alarm
- fatigue
- missed threat

Status:

- not yet implemented

### Train recruits

Type:

- interactive in-place duty

Inspired by:

- `train_recruits_events.json`
- old `TrainTroopsEvent.cs`

Trigger:

- commander recruiting
- in settlement or training pause
- enough recruits in host

Flow:

1. player is asked to drill new men
2. popup event chain:
   - difficult recruit
   - injured recruit
   - talented recruit
   - officer watching the drills
3. outcome affects trust and training profile

Skills:

- Leadership
- Polearm / OneHanded
- Medicine

Risk:

- poor discipline
- recruit injury

Status:

- not yet implemented

### Treat wounded

Type:

- interactive in-place duty

Inspired by:

- `treat_wounded_events.json`

Trigger:

- recent battle
- wounded in host
- especially good for support role

Flow:

1. player is called to assist the wounded
2. popup event chain:
   - supply shortage
   - infection risk
   - triage choice
3. outcome affects wounded recovery, trust, and skill XP

Skills:

- Medicine
- Steward

Risk:

- bad judgment
- shortage of supplies

Status:

- not yet implemented

### Trusted dispatch

Type:

- detached field duty

Trigger:

- high commander trust
- several duty successes

Flow:

1. sealed order or diplomatic message
2. travel to valid lord or settlement
3. deliver and return

Skills:

- Scouting
- Tactics
- Leadership

Risk:

- deadline failure

Status:

- implemented

### Relief dispatch

Type:

- detached field duty

Trigger:

- siege or blockade
- trust high enough

Flow:

1. commander needs help
2. player breaks from host
3. delivers urgent relief request
4. returns if possible

Skills:

- Leadership
- Scouting

Risk:

- route blocked
- commander situation changes before return

Status:

- currently folded into trusted dispatch behavior during siege/blockade

### Forage

Type:

- detached field duty

Inspired by:

- old Serve as Soldier `Foraging`

Trigger:

- supplies low
- villages nearby
- not at sea

Flow:

1. player is sent to gather provisions near a village zone
2. result may be:
   - good haul
   - angry villagers
   - bandit trouble
   - poor returns
3. player returns with food or loses time

Skills:

- Riding
- Roguery
- Steward

Risk:

- village relation hit
- bandit fight

Status:

- not yet implemented

## Context Rules

### Never offer detached land duties when:

- player is at sea
- commander is at sea
- player is already inside settlement service mode
- another active detached duty exists

### Prefer passive or interactive duties when:

- commander is in settlement
- commander is in siege
- commander is in blockade
- player has just returned from a field duty

### Prefer detached field duties when:

- army is marching
- roads are unsafe
- nearby bandits exist
- a target settlement or target party makes sense

## Recommended Implementation Order

### Phase 1

1. `Night patrol`
2. `Treat wounded`
3. `Train recruits`

These add activity fast without new detachment complexity.

### Phase 2

1. `Scout route`
2. `Forage`
3. `Relief dispatch` as separate duty id

These deepen map gameplay.

### Phase 3

1. `Quartermaster shortage`
2. `Recruitment errand`
3. `Gate watch`
4. `Town guard incident`

These round out peaceful and settlement service.

## Design Note

The best version of this system is not "more duties at any cost".

The best version is:

- fewer duties
- each one clearly different
- each one tied to a real army context
- each one producing a believable military rhythm

That means a pool of 6 to 8 strong duties is better than 20 shallow ones.
