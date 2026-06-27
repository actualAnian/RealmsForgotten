# RF War System - Master Improvement Plan

This is the practical plan to take the current `RF_warsystem` from "already promising" to "consistently strong".

## 1. What the system already does well

Right now the system already has a real strategic spine:

- diplomacy scores are being reshaped by RF
- target scores are being reshaped by RF
- kingdoms already build pressure, focus, front, objective, and war tempo
- special systems like crusade and collective defense already feed RF intent
- coalition role, frontline anchor, and campaign phase logic already exist

That means we do **not** need a rewrite.

We need stronger order, stronger authority, and cleaner persistence.

## 2. The biggest remaining weaknesses

These are the things still holding the system back most:

### A. Split authority

Some special war systems still act like parallel diplomats instead of feeding one final RF war brain.

Result:

- contradictory war behavior
- wars that can feel forced instead of strategic
- sudden shifts that break immersion

### B. Campaign sequencing is still not strong enough

The system often knows **who** to fight and **where** to fight, but it still needs to become stricter about **what order** the campaign should follow.

Result:

- some good targets chosen at the wrong time
- occasional front wobble

### C. Front commitment can still break too early

The system is already much better than vanilla, but a kingdom can still lose shape if a live campaign is not held tightly enough.

Result:

- target hopping
- unfinished offensives
- fronts that cool down too early

### D. Faction identity can still go deeper

Profiles exist, but kingdoms can still feel too close to each other in strategic personality.

Result:

- wars are smart, but not always culturally distinctive

### E. Diagnostics need to stay useful without becoming heavy

We need enough logs to understand why the AI chose something, but not enough to drag performance down.

## 3. The best improvement order

This is the order with the best cost-to-impact ratio.

### Phase 1 - One final authority

Goal:

- RF becomes the clear final war brain

Do:

- audit special war systems
- reduce direct brute-force war / peace actions where RF intent can carry the same outcome
- keep crusade, alignment war, and collective defense as strong intent injectors

Success looks like:

- fewer contradictory declarations
- fewer weird peace flips
- cleaner strategic continuity

### Phase 2 - Stronger campaign sequencing

Goal:

- wars unfold in a more believable order

Do:

- deepen campaign phase usage
- make phase transitions stricter
- ensure the system clearly distinguishes:
  - breaking the border
  - stripping support
  - pressing castles
  - pressing towns
  - stabilizing gains
  - exploiting breakthroughs

Success looks like:

- less random objective hopping
- clearer operational flow
- wars that feel like campaigns instead of isolated target picks

### Phase 3 - Harder front persistence

Goal:

- when a front is active, kingdoms stay on that front until there is a real reason to leave it

Do:

- strengthen same-sector and same-cluster bias
- increase pressure to finish unresolved fronts
- penalize opening extra concerns while a front is still alive and promising

Success looks like:

- fewer half-finished offensives
- stronger regional pressure
- cleaner "one theater at a time" behavior

### Phase 4 - Better coalition coordination

Goal:

- allies stop behaving like copies

Do:

- deepen coalition role effects
- make role matter more for:
  - war appetite
  - target type
  - pressure tolerance
  - peace resistance
  - sacred-war commitment

Success looks like:

- coalitions feel organized
- defensive alliances hold shape better
- crusade-style wars feel more intentional

### Phase 5 - Better enemy selection before war starts

Goal:

- remove the last traces of "why this enemy?"

Do:

- strengthen kingdom-to-kingdom war selection with:
  - border quality
  - claim density
  - enemy overextension
  - sacred target pull
  - coalition readiness
  - current front exposure

Success looks like:

- fewer nonsense wars
- more believable escalation

### Phase 6 - Better peace discipline

Goal:

- peace happens because it makes strategic sense, not because math drifted that way too early

Do:

- keep wars alive longer when:
  - sacred objectives still matter
  - homeland danger still exists
  - the coalition still has purpose
  - the active front is still unresolved
- only let peace pressure dominate when the war is genuinely stalling or damaging

Success looks like:

- stronger war arcs
- less anticlimactic peace
- more believable exhaustion logic

### Phase 7 - Deeper faction personalities

Goal:

- kingdoms become strategically recognizable

Do:

- expand `RFWarStrategicProfiles`
- deepen axes such as:
  - sacred zeal
  - opportunism
  - frontier paranoia
  - siege patience
  - deep-strike bias
  - coalition loyalty
  - revenge depth

Success looks like:

- each culture starts to "feel" different in war
- war behavior becomes readable by faction identity

### Phase 8 - Smarter diagnostics

Goal:

- understand the AI without hurting performance

Do:

- log only major transitions:
  - enemy focus change
  - theater change
  - frontline change
  - objective change
  - campaign phase change
  - coalition role change
  - war proposal winner
  - peace proposal winner

Success looks like:

- useful traces
- less lag
- easier tuning

## 4. What I would improve immediately

If the goal is "make it much better fast", the first three practical pushes should be:

1. strengthen faction personalities
2. strengthen front persistence
3. strengthen peace discipline

Why these three first:

- they reuse the architecture we already built
- they do not require a rewrite
- they produce visible behavior gains faster

## 5. A clean implementation track

This is the safest order for actual code work:

1. expand strategic profiles
2. wire the new profile axes into heuristics and intent
3. strengthen campaign lock and unresolved-front pressure
4. tighten peace-release conditions
5. deepen coalition role influence
6. trim diagnostics to major transitions only

## 6. Validation checklist

When we implement each phase, we should verify with simple checks:

### War-start validation

- kingdoms choose enemies that make geographic and political sense
- fewer random new wars open while a strong campaign is already active

### Campaign validation

- active war stays centered on one real front for longer
- castles, towns, and support settlements are attacked in a more logical order

### Peace validation

- kingdoms do not bail too early from meaningful wars
- weak exhausted fronts do eventually cool down

### Identity validation

- at least three different cultures show visibly different war personalities in test runs

### Performance validation

- no heavy spam logging
- no noticeable campaign lag increase from tracing

## 7. Bottom line

The system is already good enough to build on seriously.

The path to "much better" is not adding more random modifiers.

It is:

- one authority
- stronger sequencing
- stronger front commitment
- cleaner peace logic
- deeper faction identity

That is the route from "smart sometimes" to "consistently believable grand strategy".
