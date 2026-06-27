# RF War System - Vanilla War Decision Map And Upgrade Plan

This is the clean map of how vanilla Bannerlord really decides war, peace, and campaign targets, plus the best plan to make `RF_warsystem` much better than that.

## 1. The simple truth about vanilla

Vanilla does not use one grand strategic brain.

It uses a chain of smaller systems:

1. one system decides whether a clan even proposes war or peace
2. one system scores whether that war or peace is worthwhile
3. one system scores military settlement targets
4. one system makes parties pick the strongest current behavior
5. one local initiative system reacts to nearby threats and opportunities

So vanilla is not truly random, but it is also not truly campaign-minded.

That is the key weakness.

## 2. Where vanilla really decides war

### A. Proposal birth

Main file:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\Vanilla_1.3.x\MEGA_010.md`

Main class:

- `KingdomDecisionProposalBehavior`

What it does:

- every day, eligible clans may try to propose something
- the proposal type is partly chance-driven
- war and peace are not scanned in one clean ranked list first
- unresolved duplicate decisions are filtered out

Important details:

- the clan must have enough influence
- the kingdom must not already have that same unresolved decision
- the enemy candidate is chosen from a random valid kingdom candidate, not from a full strategic ranking pass
- after that, vanilla runs a support check before the proposal becomes real

So the first weakness is here:

- vanilla often decides what to consider by randomness before it decides what is best

### B. War score

Main class:

- `DefaultDiplomacyModel`

Main method:

- `GetScoreOfDeclaringWar(...)`

Vanilla war score mainly uses:

- minimum total strength
- minimum number of war parties
- border exposure
- benefit versus risk
- current relation with the enemy ruler/clan
- same-culture settlement pull
- alliance factor
- active tribute pressure

In plain words:

- can we realistically reach them
- are they worth attacking
- are we strong enough
- do we hate them
- do they hold culturally tempting land

That is better than pure randomness, but it is still mostly a one-shot check.

### C. Peace score

Main methods:

- `GetScoreOfDeclaringPeaceForClan(...)`
- `GetScoreOfDeclaringPeace(...)`

Vanilla peace score mainly uses:

- border exposure
- benefit versus risk in reverse
- war progress
- current relation
- same-culture settlement pull
- tribute logic

In plain words:

- is this war still worth the cost
- are we winning or bleeding
- are we too stretched to keep going

So vanilla peace is not dumb, but it is still reactive more than strategic.

## 3. Where vanilla really decides military targets

### A. Target generation

Main class:

- `AiMilitaryBehavior`

This is the most important military campaign class in vanilla.

It builds military behaviors around:

- `DefendSettlement`
- `BesiegeSettlement`
- `RaidSettlement`

For each possible settlement, vanilla multiplies a target score by practical filters such as:

- distance
- navigation type
- food
- army cohesion
- party size health
- objective bias
- whether an army can or should be created

That means vanilla is actually pretty decent at local practicality.

Its weakness is not "can this party reach that town".

Its weakness is "why are we pushing this front at all, and why are we changing our mind so often".

### B. Target score math

The military target score still depends on base target scoring and local army conditions.

So vanilla is strongest at:

- nearby feasibility
- supply awareness
- local defensive urgency
- immediate siege and raid opportunities

It is weakest at:

- long campaign persistence
- keeping one main enemy
- resisting tempting but bad side targets
- coordinating several allied kingdoms as one war machine

## 4. Where vanilla really decides behavior in the field map

### A. Party behavior choice

Main class:

- `AiPartyThinkBehavior`

This class does not invent strategy by itself.

It reads behavior scores gathered by other systems and then picks the best available action.

It can also:

- create an army
- disband an army
- switch a leader to raid, siege, defend, patrol, escort, or settlement movement

So this class is the execution switchboard.

### B. Nearby reaction logic

Main class:

- `DefaultMobilePartyAIModel`

Main method:

- `GetBestInitiativeBehavior(...)`

This is the short-range reaction brain.

It reacts to:

- nearby enemy parties
- nearby allied strength
- sieges
- settlement defense
- movement speed
- local tactical danger

This is why vanilla can look smart for a few minutes but still dumb over a whole war.

It is good at the local moment.
It is weak at the long campaign.

## 5. The real vanilla flow in one simple chain

Vanilla war flow is roughly this:

1. a clan gets a daily chance to propose war or peace
2. one valid enemy candidate is often picked through filtered randomness
3. `DefaultDiplomacyModel` scores whether that choice is worthwhile
4. kingdom support is checked
5. if war exists, `AiMilitaryBehavior` scores defend, siege, and raid targets
6. `AiPartyThinkBehavior` applies the highest valid behavior
7. `DefaultMobilePartyAIModel` keeps interrupting with local reactions

That last part matters a lot.

It means vanilla has several brains, but no strong central campaign director.

## 6. Why vanilla can feel weak in long wars

These are the main weaknesses from the mapping:

1. war candidate selection starts too randomly
2. there is no strong persistent "main enemy" authority
3. there is no deep theater memory
4. there is no strong frontline lock
5. local practical reactions can pull parties away from the bigger campaign
6. coalitions do not feel like one coordinated machine
7. different cultures do not feel strategically distinct enough over long wars

## 7. What RF should keep from vanilla

We should not throw vanilla away.

Vanilla is still useful for:

- local target feasibility
- food and cohesion sanity checks
- navigation practicality
- immediate local reaction
- battle-nearby emergency behavior

So the right path is:

- let RF dominate strategy
- let vanilla keep practical local movement judgment

## 8. The best upgrade plan

### Stage 1. Make RF the true authority for war birth

Goal:

- stop random enemy selection from deciding the future of a kingdom

What to do:

- in `RFWarDecisionPlannerBehavior`, replace random-valid-enemy style thinking with a full ranked scan of all valid enemy kingdoms
- score every candidate on the same pass
- pick the best window, not a random acceptable one

Expected gain:

- wars start for clearer reasons
- fewer strange target picks

Current RF status:

- this ranked scan is already in place
- RF now also compares the best war window against the second-best one before allowing a fresh war proposal
- that means a kingdom needs a clearly dominant target, not just a barely acceptable one
- RF also pushes the lead requirement even higher when the candidate would open an off-axis side war during an already active main campaign

### Stage 2. Keep one main enemy for a real campaign span

Goal:

- kingdoms should feel committed to a war, not distracted every few days

What to do:

- keep strengthening `RFWarCampaignDirectorBehavior`
- maintain a focused enemy with hold time
- punish opening side wars unless pressure is very strong
- resist peace while the live campaign is still active and favorable

Expected gain:

- wars become readable
- players can understand what the AI is trying to do

Current RF status:

- the campaign director already keeps a focused enemy with a hold window
- that persistence has now been strengthened further during active operations like advance, besiege, press castle, and press town
- in plain terms, once a kingdom is truly committed, it is now less eager to mentally jump away to a new target
- the same active-campaign signals now also make RF more stubborn about accepting peace too early

### Stage 3. Strengthen theater, frontline, and objective rails

Goal:

- make armies push on one real axis instead of scattering

What to do:

- use `RFWarTheaterBehavior`, `RFWarFrontlineBehavior`, and `RFWarObjectiveChainBehavior` as the hard campaign spine
- make off-axis targets lose more score during active campaigns
- keep local vanilla target scoring only inside those rails

Expected gain:

- fewer pointless side raids
- better castle and town pressure chains

Current RF status:

- RF already had theater, frontline, and objective rails in target scoring
- this stage has now been tightened so off-axis targets lose more score during active campaigns
- targets against the kingdom's real focused enemy are rewarded more strongly, while lateral drift is punished harder

### Stage 4. Add stronger war-state transitions

Goal:

- make a kingdom know whether it is mustering, probing, pressing, finishing, or recovering

What to do:

- deepen `RFWarOperationalRhythmBehavior`
- connect rhythm to diplomacy appetite, army gathering appetite, and target score
- make collapse states and regroup states more explicit

Expected gain:

- better pacing
- fewer mood swings

Current RF status:

- operational rhythm was already separating muster, advance, besiege, defend, regroup, and exploit
- this stage has now been tightened so active campaigns fall into regroup earlier when pressure stays high but readiness, strength, or momentum are slipping
- regroup also now holds a bit longer when the kingdom was deeply committed to that campaign

### Stage 5. Make coalitions act like one machine

Goal:

- allied kingdoms should divide labor

What to do:

- keep strengthening `RFWarCoalitionRoleBehavior`
- make one kingdom more likely to spearhead
- one more likely to shield the homeland
- one more likely to strip villages and supply
- one more likely to finish sieges

Expected gain:

- crusades and multi-kingdom wars become believable

Current RF status:

- coalition roles already existed, but now they react more strongly to campaign lock, objective commitment, and decisive pressure
- overcrowded roles are punished more and underfilled roles are rewarded more during coalition role selection
- active spearheads and siege finishers are now less eager to abandon their job in the middle of a committed campaign
- coalition lane crowding now hurts target choice more when the campaign is already disciplined and focused

### Stage 6. Deepen faction strategic identity

Goal:

- different cultures should wage war differently

What to do:

- keep expanding `RFWarStrategicProfiles`
- feed those profiles into:
  - war appetite
  - peace patience
  - raid appetite
  - siege patience
  - coalition obedience
  - frontier paranoia
  - expansion greed

Expected gain:

- the player starts recognizing each faction's style

### Stage 7. Build better diagnostics with low lag

Goal:

- understand decisions without choking the campaign

What to do:

- log only major transitions
- log:
  - chosen enemy
  - chosen theater
  - chosen frontline
  - chosen objective
  - campaign phase
  - role assignment
  - war proposal winner
  - peace proposal rejection reason

Expected gain:

- faster tuning
- much less performance cost

## 9. The most important practical rule

Do not try to replace vanilla local movement logic all at once.

That would be too messy.

The smarter path is:

1. RF decides the campaign
2. RF narrows the valid target window
3. vanilla chooses the practical move inside that window

That is the cleanest way to get a much smarter war AI without breaking everything.

## 10. Best file targets inside RF

If we want the biggest gains first, these are the most important RF files:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarDecisionPlannerBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarCampaignDirectorBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarTheaterBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarFrontlineBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarObjectiveChainBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarOperationalRhythmBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Behaviors\RFWarCoalitionRoleBehavior.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarStrategicProfiles.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Logic\RFWarStrategicHeuristics.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Models\RFWarSystemDiplomacyModel.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_warsystem\Models\RFWarSystemTargetScoreCalculatingModel.cs`

## 11. Bottom line

After mapping vanilla, the answer is clear:

vanilla is strongest at local practical reactions, but weak at sustained campaign intention.

So to make RF much better, we should not fight vanilla everywhere.

We should dominate:

- enemy selection
- war continuation
- peace discipline
- theater choice
- frontline persistence
- coalition coordination
- faction personality

And we should keep vanilla mostly for:

- immediate practical feasibility
- short-range danger response
- local pathing and movement sanity

That is the path from "better than vanilla" to "consistently impressive over long campaigns".
