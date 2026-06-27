# RF War System - Current Mapping And Upgrade Plan

This is the current plain-language map of how campaign war logic is working right now in Realms Forgotten, plus the best path to make it much better.

## 1. What the game is doing today

The campaign war AI is not one single system.

Right now it is a stack:

1. vanilla Bannerlord diplomacy and target systems
2. RF diplomacy wrappers
3. RF war memory / focus / theater / objective layers
4. event war systems like crusade, alignment war, and collective defense
5. direct war-forcing scripts in some event systems

So the world can feel smart in one moment and very scripted in another because more than one brain is pushing it.

## 2. The real RF war brain today

These are the main files that currently matter most:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\RFWarSystemRegistrar.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Models\RFWarSystemDiplomacyModel.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Models\RFWarSystemTargetScoreCalculatingModel.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarStrategicMemoryBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarOperationalRhythmBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarTheaterBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarObjectiveChainBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarCampaignDirectorBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarDecisionPlannerBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarStrategicHeuristics.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarExternalFrontContext.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\RFWarExternalIntentApi.cs`

## 3. Who decides what

### A. War and peace score

`RFWarSystemDiplomacyModel` wraps the active diplomacy model and adjusts:

- `GetScoreOfDeclaringWar(...)`
- `GetScoreOfDeclaringPeace(...)`
- `GetScoreOfDeclaringPeaceForClan(...)`

It does not replace Bannerlord from zero.

It takes the vanilla score, then pushes it up or down using RF logic.

### B. What target armies prefer

`RFWarSystemTargetScoreCalculatingModel` wraps the active target score model and adjusts:

- `GetTargetScoreForFaction(...)`

Again, it keeps vanilla practical logic, then adds RF strategic weight.

### C. Which war proposal gets created

`RFWarDecisionPlannerBehavior` is the RF layer that scans kingdoms every day and tries to add:

- war proposals
- peace proposals

This is important because it means RF is already doing more than just flavor. It is actively steering kingdom decisions.

### D. Long memory and pressure

`RFWarStrategicMemoryBehavior` stores and exposes things like:

- momentum
- rivalry
- war commitment
- front momentum
- settlement heat
- home front pressure

That memory then feeds the scoring systems.

### E. Focus and campaign direction

`RFWarCampaignDirectorBehavior` is the higher strategic layer that tries to decide:

- which enemy matters most
- how strongly to stay focused
- how hard to resist opening a random extra war
- how hard to resist peacing out too early

### F. Theater and target chain

These two layers try to stop kingdoms from behaving like headless chickens:

- `RFWarTheaterBehavior`
- `RFWarObjectiveChainBehavior`

In plain words:

- theater says which front matters
- objective chain says what concrete place on that front should stay hot for a while

### G. Operational rhythm

`RFWarOperationalRhythmBehavior` gives the war a "tempo" state, like:

- muster
- advance
- besiege
- defend
- regroup
- exploit

This is the layer that reduces wobble.

## 4. What RF is already considering well

The current RF war layer is already much smarter than vanilla in several ways.

It already considers:

- strength ratio
- army readiness
- momentum memory
- rivalry memory
- war commitment
- frontier pressure
- cultural claim pressure
- alignment hostility
- treasury confidence or distress
- multi-front pressure
- home threat
- operational state
- theater focus
- objective chain
- coalition pull

So the base is not weak. The issue is not "there is no system."

The issue is "there are still multiple systems pushing at once."

## 5. External war systems currently feeding RF

### A. Crusade

File:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RFReligions\Behavior\CrusadeBehavior.cs`

What it does today:

- starts crusade state
- tracks crusade leader
- tracks crusade target kingdom
- tracks sacred target settlement
- tracks crusader kingdoms
- calls `RFWarExternalIntentApi.ReinforceHolyWar(...)`
- also directly declares war for crusade members

Meaning:

Crusade is partly integrated, but still partly brute-force.

### B. Alignment war

File:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\RF_Diplomacy\AlignmentWarManager.cs`

What it does today:

- builds good side
- builds evil side
- calls `RFWarExternalIntentApi.ReinforceAlignmentWar(...)`
- also directly declares war between sides

Meaning:

Again, partly integrated, partly forced.

### C. Aserai collective defense

File:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\AseraiCollectiveDefenseBehavior.cs`

What it does today:

- sees one Aserai realm attacked
- rallies other Aserai realms
- calls `RFWarExternalIntentApi.ReinforceCollectiveDefense(...)`
- also directly forces peace among defenders when needed
- also directly forces war on the attacker when needed

Meaning:

Same pattern again.

## 6. What the external intent bridge is doing

`RFWarExternalIntentApi` is already a good idea.

It currently pushes outside events into the RF war memory by raising:

- rivalry floors
- commitment floors
- settlement heat floors
- home pressure floors
- temporary enemy priority
- temporary target priority

This means the bridge already exists.

That is excellent news, because we do not need to invent the bridge from nothing.

## 7. What is still structurally weak

### Weakness 1: event wars still hard-force too much

Crusade, alignment war, and collective defense do not only suggest intent.

They also directly do diplomacy actions themselves.

That causes split authority.

### Weakness 2: no single coalition command layer

RF knows how to bias enemies and settlements, but it still lacks one clean coalition-level planner that answers:

- who is coalition leader
- what is the shared front
- what is sacred or mandatory
- who is supposed to defend homeland
- who is supposed to keep pressure

### Weakness 3: target logic is strong, but front logic can still be strengthened

The system is already better than vanilla, but it still works mostly by weighting settlements after many layers, not by doing a clean:

1. choose theater
2. choose front
3. choose objective inside that front

### Weakness 4: diplomacy model stack can still become contradictory

`AlignmentDiplomacyModel` and `RFWarSystemDiplomacyModel` both wrap diplomacy.

That is fine only if order stays correct.

If order changes later, weird behavior can appear.

### Weakness 5: Army Commander is currently a technical blocker

There is a separate issue not directly about war intelligence, but it matters:

- `RFArmyManagementVMPatches` is patching `ArmyManagementVM`
- the game API changed
- Harmony is now hitting an undefined target / wrong constructor hook path

So part of the command UI layer is unstable right now.

That should be treated as a stabilization task before expanding the player war-command feature further.

## 8. How a war decision is actually born today

This is the real flow in plain words.

### A. Vanilla still gives the first raw answer

Bannerlord still produces the first raw diplomacy and target scores through:

- `DefaultDiplomacyModel`
- `DefaultTargetScoreCalculatingModel`

So vanilla still answers:

- "is war with this kingdom broadly acceptable?"
- "is peace with this kingdom broadly acceptable?"
- "is this settlement a good operational target?"

More specifically, vanilla war birth is split in two very different parts:

1. `KingdomDecisionProposalBehavior` decides whether a clan even tries to put a war vote on the table
2. `DefaultDiplomacyModel.GetScoreOfDeclaringWar(...)` decides whether that specific war looks worthwhile

That first part is important because vanilla is not doing a clean "scan every enemy and pick the best one" flow.

It first goes through a proposal chance gate, then often lands on a random valid candidate, and only after that checks whether the war is good enough.

That is one of the main reasons vanilla diplomacy can feel erratic.

### B. RF then reshapes that answer

RF does not throw vanilla away.

Instead it wraps it:

- `RFWarSystemDiplomacyModel`
- `RFWarSystemTargetScoreCalculatingModel`

So the RF layer is acting like:

- take vanilla common sense
- add long memory
- add front awareness
- add identity
- add coalition pressure
- add special-war context

### C. Then the planner chooses whether to actually propose war or peace

`RFWarDecisionPlannerBehavior` runs every day and checks:

1. is the kingdom eligible
2. is it on cooldown
3. is there already a pending decision
4. does vanilla diplomacy score pass threshold
5. does sponsor support exist
6. does election likelihood exist
7. do RF strategic biases push it higher or lower

Only then does a proposal get created.

This is important:

the RF system is not just coloring the AI. It is helping decide which kingdom decision is even placed on the table.

This is already better than vanilla in one major way:

- vanilla proposal logic is partly chance-driven and candidate-randomized
- RF planner scans kingdoms more deliberately and tries to pick the strongest proposal actually available that day

And this has now been strengthened one step further:

- `RFWarDecisionPlannerBehavior` now also listens for newly added kingdom decisions
- if an AI war or peace proposal comes in with a weaker strategic direction than RF's best evaluated option, RF can replace it immediately
- in plain words, the system is no longer only adding better proposals on its own day tick
- it is also filtering bad AI proposals after they appear
- and now it also refuses to open a new war unless the best target is clearly ahead of the second-best target
- kingdoms that are already busy, cautious, or under pressure now demand a bigger lead before starting another war
- off-axis side wars now face an extra gate when a kingdom is already committed to a main enemy
- kingdoms get a looser gate only when the best target matches their real campaign enemy or carries special-war pressure

That matters because it directly attacks one of vanilla's worst habits:

- random valid proposals that technically pass, but do not fit the best strategic direction

### D. Then the campaign director tries to stop the AI from fighting everyone at once

`RFWarCampaignDirectorBehavior` tries to answer:

- who is my real enemy right now
- should I stay locked on this enemy
- should I resist opening another war
- should I resist peacing out too early

This is the anti-chaos layer.

And this layer is now doing more than a simple favorite-enemy mark.

It also:

- holds focus on a chosen enemy for a variable duration
- increases that hold when the war already has front commitment or objective commitment
- raises resistance to opening another war when a real campaign is already in motion
- makes it harder to drop a meaningful enemy just because another candidate briefly scored a little higher

### E. Then coalition role tells each ally what job it should do

`RFWarCoalitionRoleBehavior` now assigns roles like:

- spearhead
- border shield
- siege finisher
- raider
- reserve

So not every allied kingdom behaves like the same copy.

### F. Then theater, front, objective, and phase try to turn intent into a campaign

The war is then shaped by:

- `RFWarTheaterBehavior`
- `RFWarFrontlineBehavior`
- `RFWarObjectiveChainBehavior`
- `RFWarCampaignPhaseBehavior`
- `RFWarOperationalRhythmBehavior`

In simple terms:

- theater = which zone matters most
- frontline = which local front line is alive
- objective chain = which concrete place should stay hot
- phase = what stage the war is in
- rhythm = current operational tempo

So the current RF brain is already a layered strategy system, not a simple score hack.

## 9. What I would do to make it much better

This is the improvement plan in the order that gives the biggest return.

### Step 1. Finish unifying authority

Goal:

- one final RF authority for special wars

What to do:

- make crusade, alignment war, and collective defense feed `RFWarSpecialAuthorityBehavior`
- remove remaining direct side-door war forcing where possible
- let event systems express pressure, not bypass judgment

Why this matters:

- it removes contradictory behavior
- it makes big wars feel chosen, not teleported into existence

Current status:

- `RFWarSpecialAuthorityBehavior` no longer jumps to force immediately after a short grace window
- it now first tries to promote the request through `RFWarDecisionPlannerBehavior`
- this means special wars get a better chance to appear as normal kingdom proposals instead of bypassing the strategic planner
- direct force is still present, but now acts as fallback when the planner path does not resolve the request in time
- behavior registration order was also changed so the decision planner ticks before the special authority fallback
- the diplomacy score itself is now also aware of pending special mandates
  - pending special war requests now lift war willingness earlier
  - pending special peace requests now lift peace willingness earlier
  - this reduces the gap between "event pressure exists" and "the kingdom diplomatically agrees with it"
- the campaign director now punishes off-axis enemy focus more aggressively when a live campaign already exists
  - if a kingdom is already committed to a meaningful war, unrelated enemies lose score
  - non-war distractions are punished even harder than secondary active enemies
- pending special authority pressure now also feeds the campaign director's special mandate view
  - this helps focus form earlier around event wars before fallback force becomes necessary
 - special war requests now also carry differentiated intensity
   - holy war leader pressure can enter stronger than peripheral holy participants
   - attacked kingdoms in collective defense enter stronger than secondary defenders
   - intrigue, mercenary, rivalry, and alignment requests no longer all feel equally urgent
 - that intensity now changes how fast special authority pressure rises
   - stronger requests ramp sooner
   - weaker requests still enter the same brain, but with less rush
 - fallback force logging now also exposes request intensity
   - this should make balancing much easier later

### Step 2. Strengthen enemy selection before war starts

Goal:

- kingdoms choose enemies for cleaner reasons

What to do:

- deepen `RFWarDecisionPlannerBehavior`
- make enemy selection care even more about:
  - border quality
  - enemy overextension
  - current coalition role
  - sacred targets
  - claim pressure
  - active campaign lock

Why this matters:

- fewer nonsense wars
- stronger geographic logic

Concrete implementation direction:

- let `RFWarDecisionPlannerBehavior` always score all valid enemies
- make the final winner much more dependent on current focused enemy, live theater, and campaign phase
- give extra weight to enemy border collapse, exposed towns, and current allied pressure on the same front

Current status:

- `RFWarDecisionPlannerBehavior` has now been strengthened further to:
  - push harder toward the kingdom's current primary enemy
  - reward collapse opportunities against already weakened enemies
  - penalize off-axis new wars while a live campaign is still underway somewhere else
  - resist peace more strongly when a real offensive chain is still alive
  - audit newly added AI war and peace proposals and replace weaker off-axis proposals with the best RF-ranked option when the gap is meaningful
 - `RFWarCampaignDirectorBehavior` is now also stricter when a live campaign is already real
   - opening a new off-axis war is penalized more strongly when there is already:
     - campaign lock
     - objective commitment
     - frontline commitment
     - consolidation on the same sector
     - active siege / advance pressure
   - switching away from the current enemy now needs a clearer score lead when the current war is already materially underway
 - `RFWarStrategicHeuristics` now pushes target choice harder into the active campaign chain
   - targets aligned with current objective / frontline / theater get a stronger bonus when campaign pressure is high
   - off-chain targets get a stronger penalty during a real push
   - targets against non-focused enemies now lose more value while the main campaign is locked
 - the system now also pushes harder to finish a war once the enemy is visibly cracking
   - `RFWarCampaignDirectorBehavior` now treats enemy collapse as part of opportunity scoring
   - `RFWarDecisionPlannerBehavior` now resists peace more strongly when the enemy is near collapse and the campaign is still alive
   - `RFWarObjectiveChainBehavior` now favors follow-up objectives more strongly when the enemy has few fiefs left, is already under siege, or is collapsing in a local sector

### Step 3. Make active campaigns harder to abandon

Goal:

- once a kingdom commits, it follows through longer

What to do:

- strengthen campaign lock
- strengthen unresolved front pressure
- strengthen objective commitment
- further punish opening unrelated wars during live campaigns

Why this matters:

- fewer fake offensives
- fewer sudden mood swings
- more believable campaigns

Concrete implementation direction:

- make `RFWarCampaignDirectorBehavior` even more conservative about switching away from a live enemy
- tie peace appetite more tightly to unresolved fronts and unfinished objective chains
- punish "new war while current war still incomplete" more strongly unless the new threat is truly exceptional

### Step 4. Make target choice more sequential

Goal:

- war follows an order instead of bouncing

What to do:

- make theater and objective chain stricter
- bias target score more by phase
- prefer:
  - border break
  - support stripping
  - castle pressure
  - town pressure
  - consolidation
  - deep strike only when justified

Why this matters:

- wars become readable
- players can understand what the enemy is trying to do

Concrete implementation direction:

- use theater as the top-level filter first
- inside that theater, force a front choice
- inside that front, let only a narrow set of objective settlements receive the strongest bonus
- reduce cross-map wobble by sharply discounting off-chain targets unless they are emergency defense targets

Current status:

- `RFWarStrategicHeuristics.AdjustTargetScore(...)` now includes an extra campaign-sequence factor
- that factor checks whether the chosen target is coherent with:
  - current theater anchor
  - current frontline anchor
  - current objective settlement
- exact and bound-village matches now get a stronger push
- off-axis targets now receive a clearer penalty
- offensive targets are now penalized much harder when the theater is currently in `HomelandDefense`

### Step 5. Deepen faction personality

Goal:

- kingdoms feel different, not just stronger or weaker

What to do:

- keep expanding `RFWarStrategicProfiles`
- wire those profile axes deeper into:
  - war appetite
  - peace patience
  - frontier stubbornness
  - siege tolerance
  - raid appetite
  - coalition obedience

Why this matters:

- the same map produces more varied stories
- players start recognizing cultures by behavior

### Step 6. Make coalition wars feel coordinated

Goal:

- allies act like parts of one larger campaign

What to do:

- increase the effect of coalition role on:
  - war proposal appetite
  - peace resistance
  - target type
  - homeland priority
  - siege preference

Why this matters:

- holy wars and alliance wars stop feeling like five unrelated AIs happening at once

Concrete implementation direction:

- let coalition spearheads value offensive continuation more
- let shields value homeland defense and peace caution more
- let reserve states avoid overcommitting unless the front begins to break
- let siege finishers strongly prefer the same fortification already pressured by allied kingdoms

### Step 7. Improve diagnostics without causing lag

Goal:

- understand AI choices without killing performance

What to do:

- keep only high-value transition logs
- log major decision points, not every little score wobble
- focus trace on:
  - chosen enemy
  - chosen role
  - chosen theater
  - chosen phase
  - chosen target window
  - war / peace proposal winners

Why this matters:

- easier tuning
- less campaign stutter

## 10. Best practical order for implementation

If the aim is "make it much better fast", this is the order I would use:

1. finish authority unification
2. deepen war/peace proposal logic
3. harden campaign lock and front persistence
4. tighten target sequencing by phase
5. deepen coalition role effects
6. deepen faction personality
7. trim diagnostics

## 11. Bottom line

The important conclusion from the mapping is this:

the RF war system is already a real strategy framework.

It is not missing a brain.

What it still needs is:

- one clear authority
- stricter sequencing
- harder commitment
- deeper faction identity
- cleaner coalition coordination

That is the path from "good and promising" to "consistently excellent".

## 11.5 The simplest honest summary

Vanilla war AI is not stupid, but it is fragmented and partly random.

RF war AI is already more strategic because it adds:

- memory
- enemy focus
- theater awareness
- objective continuity
- coalition pressure
- personality

So the next leap is not to invent a brain from zero.

It is to make the existing RF brain:

- more unified
- more stubborn
- more sequential
- more geographically disciplined
- more coordinated in coalition wars

## 11.6 What vanilla really uses to decide war

This is the important part of the mapping.

Vanilla does not create war from one clean master planner.

It is split into two layers:

1. `KingdomDecisionProposalBehavior`
2. `DefaultDiplomacyModel`

### A. Proposal layer

`KingdomDecisionProposalBehavior` is the first gate.

What it does:

- every day, an eligible clan may try to create a kingdom proposal
- war is only one possible proposal among others
- the chance is partly random
- the enemy candidate is also picked from a random valid kingdom
- only after that does the game ask if this war is actually good enough

So vanilla often behaves like this:

- maybe think about war today
- grab one legal enemy
- test that one

Instead of:

- score all enemies
- rank all enemies
- pick the best one

That is one of the main reasons vanilla diplomacy can feel erratic.

### B. War score layer

`DefaultDiplomacyModel.GetScoreOfDeclaringWar(...)` then checks whether the chosen war looks worthwhile.

The biggest factors are:

- minimum strength gate
- number of war parties
- border exposure
- benefit of enemy settlements
- risk to your own settlements
- enemy and own total strength
- other active enemies
- relation hatred
- same-culture settlement pressure
- alliance penalties and bonuses

So vanilla war desire is mostly:

- can we reach them
- are we strong enough
- are they valuable
- are we too exposed
- do we hate them
- are they holding lands we culturally want back

### C. Peace score layer

`DefaultDiplomacyModel.GetScoreOfDeclaringPeace(...)` uses many of the same parts, but in reverse.

It cares about:

- border exposure
- benefit of stopping the war
- risk of continuing
- war progress
- strength comparison
- number of other enemies
- settlement value at risk

So vanilla peace is not random.

But it is broad and generic.

It does not really think:

- this front is almost broken
- this siege is close to payoff
- this coalition role should hold longer
- this war is strategically unfinished

That is exactly where RF can become much better.

## 11.7 What vanilla really uses to decide targets

Vanilla target choice comes from `DefaultTargetScoreCalculatingModel`.

This model is actually decent at local practical judgment.

It cares about:

- distance
- our strength versus local defenders
- nearby enemy reinforcements
- nearby friendly reinforcements
- walls
- food stocks during siege
- settlement value
- raid value
- relation to owner
- current behavior already in progress
- current behavior priority

So vanilla's real weakness is not local math.

Its weakness is the higher strategic layer above the math.

That means the correct RF design is:

- keep vanilla local practicality
- strengthen RF strategic intention above it

## 11.8 Plan to make RF war AI much better

Now that the mapping is clear, this is the practical plan.

### Stage 1. Replace random war feel with ranked enemy selection

Goal:

- kingdoms should evaluate all valid enemies and prefer the best one

Main file:

- `RFWarDecisionPlannerBehavior`

What to strengthen:

- primary enemy weight
- active campaign lock
- border quality
- exposed frontier
- direct threat to own fiefs
- live siege / recent attacker pressure
- active theater pressure
- front and objective pressure
- coalition convergence
- sacred target pressure
- claim pressure
- enemy collapse opportunity
- off-axis war penalty

Expected result:

- fewer nonsense wars
- much cleaner map logic

What was just improved:

- enemy ranking now reacts much harder to danger on the kingdom's own frontier
- direct local danger is now separated from generic rivalry and opportunity
- homeland-defense theater pressure now pushes enemy selection more strongly
- front/objective commitment now feeds enemy ranking earlier, before target choice
- pre-war enemy choice now checks for a real breakthrough corridor on the border
  - soft frontier forts and towns near the attacker's line now matter more
  - isolated nuisance targets matter less than a chain that can become a real campaign

### Stage 2. Turn war into a sequence, not a mood swing

Goal:

- once war starts, it should follow a recognizable campaign path

Main files:

- `RFWarCampaignDirectorBehavior`
- `RFWarTheaterBehavior`
- `RFWarFrontlineBehavior`
- `RFWarObjectiveChainBehavior`
- `RFWarCampaignPhaseBehavior`

Desired order:

1. choose enemy
2. choose theater
3. choose frontline
4. choose objective chain
5. choose phase
6. let target scoring refine inside that box

Expected result:

- less wobble
- more believable offensives

### Stage 3. Make kingdoms hold commitment longer

Goal:

- stop dropping meaningful wars too early

What to strengthen:

- focus hold duration
- front commitment
- objective commitment
- peace resistance while a live operation is unresolved

Expected result:

- fewer fake campaigns
- stronger war continuity

### Stage 4. Deepen faction personality

Goal:

- cultures should feel different in war

Main file:

- `RFWarStrategicProfiles`

Best axes to keep pushing:

- offensive drive
- defensive discipline
- raid appetite
- siege patience
- deep strike bias
- caution
- persistence
- coalition loyalty
- sacred zeal
- frontier paranoia

Expected result:

- players recognize kingdoms by behavior

### Stage 5. Make coalition wars feel coordinated

Goal:

- crusades, alignment wars, and collective defense should feel like one campaign

Main files:

- `RFWarCoalitionRoleBehavior`
- `RFWarSpecialAuthorityBehavior`

Desired jobs:

- spearhead
- siege finisher
- border shield
- raider
- reserve

Expected result:

- allies stop behaving like unrelated solo AIs

What is now stronger:

- peer kingdoms now influence role distribution more clearly
  - too many spearheads pushes some allies toward siege finishing or support roles
  - thin shield coverage pushes more pressure toward border defense
  - over-saturated raiding makes raider assignment less attractive
- coalition roles now react more directly to the real war moment
  - `Advance` and `Exploit` now push kingdoms more toward `Spearhead` and `Raider`
  - `Besiege` and `Muster` now push kingdoms more toward `SiegeFinisher`
  - `Defend` and `Regroup` now push kingdoms more toward `BorderShield` and `Reserve`
- target choice now gets an extra coalition-lane bonus
  - targets near coalition objective/front/theater anchors are favored more strongly
- target preference now also follows operational state inside the chosen role
  - spearheads push harder while advancing
  - siege finishers lean harder into fortification pressure during siege state
  - shields and reserves lean harder into defensive targets when the campaign is stabilizing
- coalition role trace now shows peer role shares
  - this makes it easier to see whether a kingdom changed role because of coalition structure, not only because of its own profile
 - coalition lanes are now less willing to over-stack in the same place
   - excessive spearhead, siege, shield, or raider crowding now pushes some peers away from that same role
 - raiders now lose appetite for converging on the same packed lane as the main assault
 - coalition role selection now listens harder to campaign lock and objective commitment
   - active campaigns pull more kingdoms toward the role that is actually missing
   - overcrowded spearhead or siege lanes are pushed back harder during role choice
 - active spearheads and siege finishers now hold their job more stubbornly
   - if the campaign is already committed, they are less likely to flip roles too early
 - collapse state now feeds coalition role choice more directly
   - when the enemy is visibly cracking, siege finishers become more attractive
   - raider and reserve pressure drops during endgame closing moments
   - this helps the coalition shift from "harass and posture" into "finish the war"

### Stage 6. Keep vanilla target math, but only inside RF strategic rails

Goal:

- vanilla should judge local feasibility
- RF should decide strategic intention

What was just strengthened:

- offensive targets that drift away from the currently focused enemy now get a much harder penalty
- a live campaign with strong front/objective commitment now resists target wobble more aggressively
- homeland-defense mode now punishes offensive drift more sharply
- focus hold duration is longer when the kingdom is already inside a real castle/town push
- enemy switching now requires a clearer lead when the current front is still unresolved

That is the best mix because:

- vanilla is good at local practical numbers
- RF is where memory, identity, and coalition logic already live

### Stage 7. Keep diagnostics useful without causing lag

Goal:

- understand decisions without killing performance

Current practical signals in `RF_WarSystemTrace.log`:

- `focus ...`
  - when a kingdom locks onto an enemy or meaningfully changes focus strength
- `focus_hold ...`
  - when the kingdom wanted to change enemy but the current campaign lock/front commitment held it in place
- `proposal ...`
  - when RF ranks a war or peace proposal
- `proposal_replace ...`
  - when RF overrides a weaker vanilla proposal
- `target ...`
  - current best target window for a party, including vanilla base score, final adjusted score, multiplier, focused enemy, and live campaign lock

Log only:

- enemy selected
- focus strength
- campaign lock
- phase
- coalition role
- proposal winner
- target shortlist winner

Avoid:

- constant score spam every tick

## 11.9 Best implementation order

If the aim is highest return first:

1. strengthen ranked enemy selection
2. strengthen campaign lock and sequence
3. deepen coalition role influence
4. deepen strategic profiles
5. tighten target rails by phase and front
6. trim diagnostics

## 11.10 Simplest conclusion

Vanilla already has:

- decent local target practicality
- acceptable broad war math

Vanilla is weak at:

- consistency
- long memory
- sequencing
- coalition coordination
- cultural identity

So the best RF path is not to throw vanilla away.

It is to dominate the higher strategic layer while still reusing vanilla's local practical math.

## 12. Remaining external callers after the latest authority pass

After centralizing:

- `ReligiousWarBehavior`
- `AseraiCollectiveDefenseBehavior`
- `AlignmentMomentumBehavior`
- `MercenaryFactionWarPactBehavior` for kingdom-vs-kingdom contract wars
- `StrategicIntrigueCampaignBehavior` for foreign intervention alliance triggers
- `ApplyInciteBreakAction` for intrigue-driven defection and restoration wars

the main remaining non-centralized war / peace callers still worth reviewing are:

- `CapitulationSystemBehavior`
- `AlignmentWarStarter`

`AlignmentWarStarter` exists as a source file, but it is not currently registered in `AiSubModule`.

So it is not an active runtime authority right now.

`AggressiveDwarfUrkhaiBehavior` and `AggressiveSturgiaBehavior` now route their pressure through:

- `RFWarExternalIntentApi.ReinforceEnduringRivalryWar(...)`
- `RFWarSpecialAuthorityBehavior`

So they are no longer direct side-door runtime war declarations.

Not all of the remaining callers should be pulled into the center in the same way.

The best priority order is:

1. `CapitulationSystemBehavior`
2. `AlignmentWarStarter`

Reality check after the latest inspection:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\CapitulationSystemBehavior.cs`
  - is live
  - but its war / peace pressure is already mostly routed through `RFWarExternalIntentApi`
  - so it is no longer the first priority for authority cleanup

- `AlignmentWarStarter`
  - still contains a direct `FactionManager.DeclareWar(...)`
  - but is not registered in `AiSubModule`
  - so it is currently a dead-path cleanup item, not an active runtime authority

That means the practical next priority is no longer "rip out capitulation first".

It is:

1. continue strengthening the real RF planner and campaign lock
2. clean remaining dead or legacy direct-war paths when convenient

## 13. Practical upgrade roadmap

This is the cleanest plan if the goal is to make the system feel much smarter in play, not just more complicated in code.

### Phase A. One authority, one campaign

Goal:

- a kingdom should feel like it is really pursuing one war at a time unless a true emergency happens

What to improve:

- make `RFWarCampaignDirectorBehavior` even stricter when a live campaign already has:
  - focused enemy
  - frontline anchor
  - active objective
  - strong campaign phase
- make side enemies lose more score while the main front is unresolved

Success result:

- fewer random war pivots
- fewer "start war here, stop there, drift elsewhere" moments

### Phase B. Turn the chain into a hard sequence

Goal:

- strategic choice should flow in one direction:
  - enemy
  - theater
  - frontline
  - objective
  - phase
  - target

What to improve:

- make `GetTargetCampaignSequenceFactor(...)` even more decisive
- reward targets that match all active layers together
- punish targets that only match one layer but break the rest of the campaign chain

Success result:

- armies stop behaving like they are picking targets from a bag
- offensives feel like real pushes

### Phase C. Teach the system when a war is worth finishing

Goal:

- kingdoms should smell weakness and close wars properly

What to improve:

- deepen endgame scoring when the enemy:
  - has few fiefs left
  - has a collapsing border
  - is losing towns in the same sector
  - is trapped in multi-front pressure
- increase "finish the job" pressure in:
  - `RFWarDecisionPlannerBehavior`
  - `RFWarCampaignDirectorBehavior`
  - `RFWarObjectiveChainBehavior`

Success result:

- fewer half-finished wars
- more believable conquest sequences

### Phase D. Make coalition wars feel coordinated

Goal:

- allied kingdoms should feel like parts of one war machine, not separate tourists

What to improve:

- keep expanding coalition role pressure
- make role assignment lean harder on:
  - distance to front
  - army quality
  - siege suitability
  - naval suitability
  - sacred or mandatory target pressure
- make target preference follow role more sharply

Success result:

- one ally holds border
- one ally raids support villages
- one ally keeps siege pressure
- reserve kingdoms stop doing silly side work

### Phase E. Finish removing brute-force event wars

Goal:

- crusade, alignment war, defense pacts, and intrigue wars should feel politically born inside the same brain

What to improve:

- keep moving special-event war pressure into:
  - `RFWarExternalIntentApi`
  - `RFWarSpecialAuthorityBehavior`
  - `RFWarDecisionPlannerBehavior`
- keep direct force only as a late fallback

Success result:

- big holy or alignment wars still happen
- but they feel integrated instead of scripted from outside

### Phase F. Add logs only where they answer real questions

Goal:

- understand why the AI chose a war or a target without causing lag

What to improve:

- trace only:
  - enemy focus changes
  - objective changes
  - campaign phase changes
  - proposal winner
  - target shortlist winner
- avoid per-tick spam

Success result:

- easier balancing
- less performance waste

## 14. Best next implementation order

If we want the biggest jump in quality with the least chaos, the best order is:

1. harden campaign lock and off-axis war penalties
2. harden the frontline -> objective -> phase -> target chain
3. deepen enemy collapse / finish-the-war behavior
4. deepen coalition role pressure
5. continue removing brute-force event declarations
6. trim diagnostics to the highest-value traces only

## 15. Practical plan to make it much better

This is the simple, real execution plan.

Not "add more systems."

The goal is:

- fewer nonsense wars
- fewer random pivots
- more believable campaigns
- better coalition behavior
- cleaner behavior in late war

### Phase 1. Stop strategic wobble

Main goal:

- once a kingdom commits to a real enemy, it should stay on that job longer

What we improve:

- stronger campaign lock
- stronger off-axis war penalty
- stronger penalty for abandoning a live front too early
- stronger pressure to keep following the current theater and frontline

What success looks like:

- kingdoms stop opening silly side wars while already pushing a real campaign
- the map feels less random from week to week

Risk:

- if overdone, kingdoms can become too stubborn

So the check is:

- they should resist distraction
- but still react to true homeland emergencies

Current status:

- the system now has an extra "decisive campaign pressure" signal
- this signal grows when a real war has become materially active, especially during:
  - live siege pressure
  - town or castle push phases
  - active advance / besiege rhythm
- that pressure now feeds:
  - enemy focus stability
  - hold duration
  - campaign lock
  - unresolved front weight
  - off-axis war resistance
  - peace resistance during live operations
- campaign lock now leans harder on:
  - frontline commitment
  - objective commitment
  - local sector consolidation
- frontline and objective holds now stay alive longer during real offensive phases
  - especially during `PressCastle` and `PressTown`
- regroup no longer automatically throws away an objective if that objective is still aligned with the current frontline

Practical meaning:

- kingdoms should now be less likely to abandon a real campaign just because another enemy briefly looks tempting
- once the war is truly "on", the RF brain now treats that war as more expensive to drop
- if a kingdom is already pushing the correct sector, it should now keep pressing that same axis more consistently instead of wobbling off it too early

### Phase 2. Make war follow a chain

Main goal:

- war should feel like a campaign, not a mood swing

What we improve:

- enemy choice
- theater choice
- frontline choice
- objective choice
- phase choice
- target choice inside that phase

In simple words:

1. decide who matters
2. decide where the real front is
3. decide what place matters most there
4. keep local army target logic inside that box

What success looks like:

- armies stop drifting across the map for weak reasons
- players can read what a kingdom is trying to do

Current status:

- target scoring now punishes campaign drift more aggressively
- the system now checks whether a target is breaking the active chain:
  - theater
  - frontline
  - objective
  - phase
- when a live campaign has strong pressure, off-axis targets now lose much more score
- the sequence bonus and focused-enemy rail are now stronger during a live push
- exact or cluster-matching castle and town targets now hold value better in `PressCastle` and `PressTown`
- deep non-frontier off-axis targets now lose extra value when they break both objective and frontline alignment at the same time

Practical meaning:

- if the war is supposed to press a castle line, random side targets should now fall away more often
- if the war is supposed to strip support, villages tied to the real objective/front should rise while unrelated targets sink
- if the war is already in a serious push, geography now matters more than before

### Phase 3. Teach kingdoms to finish wars properly

Main goal:

- when the enemy is cracking, the AI should smell blood and close the war

What we improve:

- stronger enemy-collapse detection
- stronger pressure to keep siege momentum
- lower peace appetite when conquest is close
- stronger follow-up objective chaining after a breakthrough

What success looks like:

- fewer wars that stop right before payoff
- more believable conquest sequences

Current status:

- the system now exposes a shared enemy-collapse signal to more layers
- peace logic now resists exit more strongly when the enemy is materially collapsing
- objective logic now leans harder toward fortifications on the current axis when collapse is underway
- unrelated villages lose value more sharply during late-war closing moments
- focused campaigns now hold longer when the enemy is already collapsing
- towns and castles on the active axis now gain extra closing pressure during collapse, especially towns
- enemy selection and peace resistance now both treat late-war collapse as a stronger reason to keep pressing instead of drifting away

Practical meaning:

- once the enemy is down to a weak late-war state, RF should now be less likely to drift into soft targets or early peace
- the campaign should more often convert pressure into an actual closing siege line

### Phase 4. Make coalition wars feel like one machine

Main goal:

- allied kingdoms should stop acting like separate tourists

What we improve:

- coalition role assignment
- role pressure on target choice
- role pressure on peace and war appetite
- role pressure on homeland defense versus forward push

The intended feeling:

- one kingdom spearheads
- one keeps siege pressure
- one guards the border
- one raids support territory
- reserves stop wandering pointlessly

What success looks like:

- crusades, alignment wars, and defense coalitions feel coordinated

Current status:

- coalition roles now resist overstacking more strongly
  - too many spearheads reduce appetite for more spearheads
  - too many raiders reduce appetite for more raiders
  - heavy assault saturation now makes reserve more attractive
- siege finisher now depends more clearly on a real assault context
  - it loses value when there is no meaningful spearhead pressure already present
- raider now backs off more when home threat is real
- target preference now reflects the role more sharply
  - spearhead leans harder into the active lane
  - siege finisher punishes villages and random non-fortification drift more strongly
  - raider avoids piling directly onto the exact main objective lane
  - border shield and reserve are less willing to wander deep off the frontier

Practical meaning:

- allied kingdoms should now distribute jobs more cleanly during the same war
- one coalition should be less likely to produce several kingdoms all pretending to be the same spearhead
- raiders should feel dirtier and more lateral, while siege kingdoms should feel more committed to the real push

### Phase 5. Finish unifying special wars under one brain

Main goal:

- holy wars and event wars should feel politically born, not teleported in by script

What we improve:

- keep routing special war pressure through:
  - `RFWarExternalIntentApi`
  - `RFWarSpecialAuthorityBehavior`
  - `RFWarDecisionPlannerBehavior`
- leave brute-force diplomacy only as a late fallback

What success looks like:

- major event wars still happen
- but they happen through one central RF logic path

### Phase 6. Deepen faction identity

Main goal:

- kingdoms should feel different in how they wage war

What we improve:

- strategic profiles
- offensive appetite
- caution
- raid appetite
- siege patience
- frontier paranoia
- coalition obedience
- sacred zeal

What success looks like:

- players start recognizing behavior patterns by culture and kingdom

### Phase 7. Trim logs and keep only useful ones

Main goal:

- understand decisions without creating lag

What we improve:

- keep only major transition logs
- remove score spam
- keep trace around:
  - chosen enemy
  - focus change
  - coalition role
  - campaign phase
  - chosen proposal
  - chosen target window

What success looks like:

- easier tuning
- less campaign stutter

## 16. Best order to build this without breaking everything

This is the order I recommend.

### Step A. Strengthen commitment first

Why first:

- it gives the fastest visible improvement
- it also makes all later tuning easier because the AI stops thrashing

### Step B. Strengthen the theater -> frontline -> objective chain

Why second:

- after commitment is better, we need the AI to commit to the right place

### Step C. Strengthen endgame and finish-the-war logic

Why third:

- only after the AI holds a line well does it make sense to teach it how to finish a campaign

### Step D. Deepen coalition coordination

Why fourth:

- this gives the biggest jump for crusades, alignment wars, and multi-kingdom conflicts

### Step E. Deepen faction personality

Why fifth:

- personality matters more after the core strategic behavior is already stable

### Step F. Remove remaining brute-force leftovers

Why sixth:

- this is cleanup after the central brain is already stronger

### Step G. Final log cleanup

Why last:

- first we need the information
- then we can safely prune it

## 17. The honest bottom line

The system does not need a brand-new brain.

It already has one.

What it needs is:

- stronger commitment
- stricter sequencing
- better coalition discipline
- better endgame instinct
- deeper faction personality
- less outside brute-force interference

That is the path from "already smarter than vanilla" to "consistently impressive in long campaigns."
