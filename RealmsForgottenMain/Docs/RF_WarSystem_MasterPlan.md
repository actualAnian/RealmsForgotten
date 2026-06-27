# RF War System Master Plan

This is the practical plan for turning campaign war AI into something much stronger, more thematic, and less random.

It is based on the mapping already documented in:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\Docs\CampaignWarAIMappingAndPlan.md`

## Simple Diagnosis

Vanilla already knows some useful things:

- who is strong
- who is weak
- what target is close
- whether food and cohesion are bad
- whether a settlement is worth defending or besieging

But vanilla is still weak in the places that matter most for a grand campaign:

- it does not plan wars like a real ruler
- it does not keep a strong memory of what just happened
- it changes direction too easily
- it does not give kingdoms a deep strategic identity
- it does not unify special RF events into one brain

So the right path is:

1. keep the useful vanilla base
2. add an RF strategic layer above it
3. improve decisions in phases

## Final Design Goal

The final RF war system should make kingdoms feel like they:

- know when to start a war
- know when to avoid a war
- know what front matters most
- know which target is worth the cost
- know when to regroup after failure
- know when to exploit success
- behave differently depending on culture, faith, and doctrine

That means campaign war should stop feeling like:

- random declarations
- drifting lords
- pointless raids
- armies spinning in circles
- special events forcing behavior with no shared logic

## The Improvement Plan

## Phase 1: Stronger War Declaration Logic

Goal:

- make kingdoms choose wars for better reasons

What we add:

- pressure from same-culture claims
- pressure from religion or alignment hatred
- penalty for too many active wars
- penalty for weak treasury
- penalty for threatened homeland
- bonus for strong army readiness
- bonus for frontier opportunity
- memory of recent defeat

What this changes in gameplay:

- fewer stupid wars
- fewer wars started at the worst possible moment
- more believable pauses after losses
- more believable holy wars and revenge wars

Success condition:

- kingdoms attack when they are ready and see a real opportunity

## Phase 2: Stronger Peace Logic

Goal:

- stop kingdoms from fighting forever with no judgment

What we add:

- peace pressure after repeated defeats
- peace pressure when homeland is under threat
- peace pressure when treasury is collapsing
- peace pressure when army power drops too far
- lower peace appetite when victory is close

What this changes:

- wars end for understandable reasons
- weak kingdoms try to survive instead of dying pointlessly
- strong kingdoms press on instead of making silly peace too early

Success condition:

- peace feels earned by battlefield reality

## Phase 3: Better Target Choice

Goal:

- make armies choose better settlements and better fronts

What we add:

- value border continuity
- value targets that open a road to the next target
- prefer meaningful forts and towns over random low-value targets
- avoid deep targets when home is in danger
- avoid impossible sieges
- avoid weak raids while the main war is elsewhere
- weight religious or alignment targets when relevant

What this changes:

- cleaner front lines
- fewer nonsense marches
- better campaign pacing

Success condition:

- when war starts, kingdoms push in a way that looks intentional

## Phase 4: Operational States

Goal:

- stop the “wandering lord” feeling

What we add:

- a state machine for armies and major parties

Main states:

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

- once a kingdom or army chooses a state, it should stay in it for a minimum time unless something big changes

What this changes:

- fewer abrupt plan changes
- fewer armies walking back and forth
- more readable war behavior

Success condition:

- players can understand what an army is trying to do just by watching it

## Phase 5: Strategic Memory

Goal:

- make kingdoms remember what just happened

What we add:

- recent victory memory
- recent defeat memory
- failed siege memory
- repeated target failure memory
- betrayal or coalition memory
- trust in specific war directions

Examples:

- if a kingdom fails twice at the same fortress, it should stop throwing itself there
- if a frontier keeps collapsing, the kingdom should fear that front more
- if one enemy is repeatedly aggressive, that enemy should become a higher strategic priority

What this changes:

- AI stops looking forgetful
- repeated mistakes become less common

Success condition:

- kingdoms learn, even if only in a controlled and simple way

## Phase 6: Kingdom Doctrine

Goal:

- make different factions wage war differently

What we add:

- doctrine profiles by culture, realm identity, and special RF alignment

Examples:

- imperial-type realms:
  - prefer forts, roads, continuity, reconquest

- raider-type realms:
  - prefer weak borders, villages, mobility, harassment

- crusading realms:
  - value holy targets more than normal strategic value

- desperate realms:
  - defend, recover, and seek peace faster

What this changes:

- kingdoms get personality
- war identity becomes visible on the map

Success condition:

- players can feel the difference between fighting one culture and another

## Phase 7: Unify RF Special Wars

Goal:

- stop crusades, alignment wars, and coalition wars from feeling disconnected

What we add:

- one central RF war planner that receives “intent” from special systems

Meaning:

- crusade system says: this target is sacred
- alignment war says: these blocs are true enemies
- coalition defense says: this attack threatens the whole bloc

Then the main planner decides:

- whether to defend first
- whether to counterattack
- which target matters most
- whether to besiege, raid, or regroup

What this changes:

- less script feel
- more unified campaign behavior
- fewer contradictions between RF systems

Success condition:

- special wars feel organic, not just forced

## Phase 8: Theater Logic

Goal:

- make kingdoms think in fronts, not just in isolated settlements

What we add:

- northern front
- southern front
- island or naval front
- crusade front
- homeland emergency front

Each front gets:

- pressure score
- opportunity score
- risk score
- priority score

What this changes:

- kingdoms stop scattering effort
- armies concentrate where the war really matters

Success condition:

- one or two real fronts become visible instead of total chaos

## Phase 9: Campaign-Level Debug Layer

Goal:

- let us understand why the AI did something without causing heavy lag

What we add:

- logging only for major strategic decisions

Good things to log:

- war score result
- peace score result
- chosen front
- chosen target
- reason for abandoning a target
- reason for entering regroup
- reason for joining or refusing coalition war

What this changes:

- tuning becomes practical
- we stop guessing

Success condition:

- when something looks wrong, we can read the reason quickly

## Recommended Build Order

This is the order that gives the best result for the least chaos:

1. stronger war declaration logic
2. stronger peace logic
3. better target choice
4. operational states
5. strategic memory
6. kingdom doctrine
7. unify RF special wars
8. theater logic
9. campaign debug layer

## What “Much Better” Should Mean In Practice

If this plan is done well, the player should notice:

- wars start at smarter times
- kingdoms do not attack while exhausted for no reason
- armies stop drifting as much
- major fronts become clearer
- same-culture reconquest becomes more believable
- crusades and bloc wars feel intentional
- one defeat does not instantly make the AI act brain-dead
- one victory does not make the AI overextend foolishly

## What We Should Avoid

To keep the system good, we should avoid:

- fully replacing everything vanilla already does well
- too many hard scripts
- too many always-on heavy logs
- too many faction exceptions
- too much randomness
- massive rewrites before we validate each phase

## Practical Next Step

The first real implementation pass should focus only on:

1. war declaration scoring
2. peace scoring
3. target scoring

Why:

- this gives the biggest gain first
- it keeps the change surgical
- it can be tested more easily than a huge rewrite

## Bottom Line

The right version of `RF_warsystem` is not a total replacement of vanilla.

It is a smarter war brain layered above vanilla, with:

- better judgment
- better memory
- better front logic
- better target choice
- stronger RF identity

That is the path to make campaign war feel truly much better.
