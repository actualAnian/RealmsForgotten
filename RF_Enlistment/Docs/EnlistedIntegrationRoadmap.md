# RF Enlistment - Enlisted Integration Roadmap

Date: 2026-06-16

## Goal

Pull the best ideas from `Enlisted` into `RF_Enlistment` without importing its full complexity.

## Current Status

- `Phase 1 core` is now implemented in code:
  - army time phase detection
  - light pressure-state evaluation
  - contextual duty weighting
  - service-status panel now shows army rhythm and pressure
- next real gain is to build more duties on top of this base instead of adding flat random events

This roadmap is focused on:

- things that improve gameplay rhythm
- things the player can actually feel
- systems that fit the current `RF_Enlistment` codebase
- avoiding bloated simulation for its own sake

## What We Already Have

These pieces are already in place in `RF_Enlistment`:

- enlistment petition and oath flow
- service attachment to a lord's party
- service UI while traveling with the commander
- service XP, trust, wages, rank flow
- passive service shift duties
- interactive local duties:
  - `Night Patrol`
  - `Treat Wounded`
  - `Train Recruits`
  - `Equipment Check`
  - `Inspect Defenses`
  - `Gate Watch`
  - `Quartermaster Shortage`
- detached field duties:
  - `Supply Delivery`
  - `Bandit Hunt`
  - `Scout Route`
  - `Forage`
  - `Road Patrol`
  - `Mounted Pursuit`
  - `Trusted Dispatch`
  - `Relief Dispatch`
  - `Recruitment Errand`

So the next gains are not "more random duties".

The next gains are structure, pacing, and better military context.

## Best Systems To Import From Enlisted

### 1. Camp schedule and opportunity pacing

This is the single best system still missing.

`Enlisted` does not only fire events.
It organizes camp life into time windows like:

- `Dawn`
- `Midday`
- `Dusk`
- `Night`

That means the game can decide:

- when training makes sense
- when medical help makes sense
- when social or quiet camp events make sense
- when the army should feel busy, tense, or calm

Why it is valuable:

- makes duties feel believable
- stops repetitive spam
- gives the army a real daily rhythm
- lets us say "this is a marching day", "this is a siege day", "this is a quiet garrison day"

Priority:

- `Highest`

### 2. Pressure states

`Enlisted` tracks broad army pressures such as:

- low supplies
- low morale
- high scrutiny
- exhaustion
- pre-battle readiness
- siege pressure

We do not need their whole stat jungle.
We only need a light version that changes what duties are likely.

Example:

- low supplies -> boost `Forage`, `Supply Delivery`
- many wounded -> boost `Treat Wounded`
- pre-battle -> boost `Scout Route`, `Equipment Check`, `Drill`
- night + camped -> boost `Night Patrol`
- high scrutiny -> reduce leisure-like events and increase strict duties

Why it is valuable:

- the same system can feel different from one campaign moment to the next
- duties begin to react to the army instead of appearing flatly

Priority:

- `Highest`

### 3. Officer-track duties

This is the next big step once pacing exists.

`Enlisted` has high-rank responsibilities such as:

- `Lead Patrol`
- `Inspect Defenses`
- `Coordinate Supply`
- `Strategic Planning`
- `Command Squad`
- `Interrogate Prisoner`

These are good because they change the fantasy of service:

- early game = ordinary soldier
- mid game = trusted veteran
- late game = junior officer or battlefield aide

Why it is valuable:

- gives rank real meaning
- makes promotions feel like responsibility, not just numbers
- fits perfectly with our trust and service XP systems

Priority:

- `High`

### 4. Quartermaster layer

`Enlisted` has a quartermaster structure with:

- equipment access
- provisions access
- upgrade access

We should not copy the whole equipment screen blindly.
But the idea is strong:

- better service rank unlocks better military issue
- supply-related service can open access to useful kit
- the player feels integrated into the army machine

Why it is valuable:

- gives service rewards a material shape
- ties rank and role to equipment access
- works well with our mod because gear matters a lot

Priority:

- `High`

### 5. Incident and personal news layer

`Enlisted` often turns outcomes into short personal reports:

- what happened
- why it mattered
- what changed

This is simple but powerful.

Example:

- "Contagion Risk: the fever did not spread."
- "War Stories: you learned something useful by the fire."
- "Scout Report: your warning saved the column from surprise."

Why it is valuable:

- makes the system feel alive
- helps the player understand what a duty actually did
- is much better than silent background math

Priority:

- `High`

### 6. Payment tension and loyalty events

`Enlisted` also models camp tension around:

- poor pay
- missing supplies
- internal suspicion
- growing unrest

We should use a light version.

Not a giant social simulation.
Just enough to create:

- unpaid wage complaints
- discipline trouble
- resentment after bad leadership
- better flavor around long campaigns

Why it is valuable:

- gives long enlistment campaigns consequence
- creates non-battle drama
- makes stewardship and command quality matter

Priority:

- `Medium`

## Systems Worth Taking Later

These are good, but not first priority.

### Social camp opportunities

Examples:

- war stories
- card games
- writing home
- casual firelight events

These are good flavor, but should come after military structure.

Priority:

- `Low`

### Baggage and personal storage flavor

This adds immersion when entering service, but it is not urgent for core gameplay.

Priority:

- `Low`

### Leave system

Useful, but should come after duty pacing and officer track are solid.

Priority:

- `Low`

### Rare side incidents

Examples:

- deserter extortion
- poacher event
- town thief
- gang fight

These are fun spice, but not backbone systems.

Priority:

- `Low`

## Recommended Implementation Order

### Phase 1 - Army rhythm

Build the systems that decide what kind of military day this is.

Implement:

1. light `camp schedule`
2. light `pressure states`
3. duty weighting by context

Success looks like:

- duty appearance becomes less repetitive
- nights feel different from day
- marching, siege, garrison, and recovery feel different

Status:

- `Core done`
- can still be tuned after live testing

### Phase 2 - Better military variety

Expand the current duty pool using the new rhythm system.

Implement:

1. `Equipment Check`
2. `Inspect Defenses`
3. `Quartermaster Shortage`
4. `Recruitment Errand`
5. `Gate Watch`

Success looks like:

- more duties without spam
- each role feels more distinct
- peaceful and siege contexts have their own content

Status:

- `First batch in code`
- needs live testing and tuning

### Phase 3 - Officer track

Turn promotions into real responsibility.

Implement:

1. `Lead Patrol`
2. `Coordinate Supply`
3. `Command Squad`
4. `Strategic Planning`

Success looks like:

- higher rank changes the kind of work you receive
- trust and rank matter in a visible way
- service fantasy evolves naturally over time

### Phase 4 - Reporting and memory

Make the system speak back to the player.

Implement:

1. personal news summaries
2. incident reports
3. light commander memory of repeated success or failure

Success looks like:

- the player understands outcomes
- repeated behavior feels acknowledged
- long campaigns become more personal

### Phase 5 - Logistics and tension

Add long-campaign friction.

Implement:

1. pay tension
2. low supply friction
3. camp discipline incidents
4. loyalty or unrest pressure

Success looks like:

- long service has internal drama
- bad campaigns feel bad for reasons beyond battle casualties

## What I Would Do Next

Now that the rhythm layer exists, the next concrete task should be:

### Next build target

Create the first batch of `context-aware new duties`.

Best first additions:

- `Equipment Check`
- `Inspect Defenses`
- `Gate Watch`
- `Quartermaster Shortage`
- `Recruitment Errand`

This is the cleanest next step because the new rhythm system can now decide when each of these makes sense.

## Design Rule

The lesson from `Enlisted` is not:

- add hundreds of events

The real lesson is:

- organize events around army life
- make the army's condition matter
- let rank change responsibility
- keep outcomes visible to the player

That is the version worth importing.
