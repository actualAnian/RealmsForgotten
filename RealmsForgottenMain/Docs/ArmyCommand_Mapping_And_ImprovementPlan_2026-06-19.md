# Army Command Mapping And Improvement Plan

This note maps what the current `ArmyCommand` module already does, why it is fragile right now, and how to turn it into a much stronger system.

The goal is simple:

1. understand what the current module is trying to do
2. separate the good ideas from the brittle implementation
3. define the cleanest path to make it much better

## Files Studied

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandPatches.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandMixins.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandViewModels.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandHelpers.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandContexts.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandActions.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand\RFArmyCommandPrefabPatches.cs`
- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RealmsForgottenMain\SubModule.cs`

## 1. What this module is trying to be

This is not a tiny UI tweak.

The module is trying to give the player a kingdom-level army command layer, not just the vanilla "manage my own army" screen.

In plain words, it wants to let the player:

- see all armies in the player's kingdom
- select one specific army
- inspect its state
- open management for that selected army
- add or remove parties
- create a new army under another leader
- change its target settlement
- change its behavior
- send influence to support that army

So the ambition is good.

It is trying to become a real strategic command tool.

## 2. What each file does

### A. `RFArmyCommandPatches.cs`

This is the core integration file.

It patches:

- map screen army overlay behavior
- army overlay open logic
- `CampaignUIHelper.GetCanManageCurrentArmyWithReason`
- party eligibility checks for army joining
- `ArmyManagementVM`
- army gather / army disperse updates

This file is doing the heavy lifting.

It is also the most fragile part.

### B. `RFArmyCommandMixins.cs`

This adds new UI behavior on top of vanilla view models.

Main jobs:

- add army target selection
- add army behavior selection
- add influence support button
- maintain selected army summary
- refresh left-side custom army overlay

This part is conceptually strong.

### C. `RFArmyCommandViewModels.cs`

This builds the custom list rows and army summary widgets.

It shows:

- parties attached / assigned
- men count
- food
- influence
- cohesion
- cohesion restore cost

This is good player-facing information.

### D. `RFArmyCommandHelpers.cs`

This is the utility brain.

It decides things like:

- whether an army is available
- whether the player is busy
- whether a settlement is a valid target
- default army behavior
- target settlement naming
- summary numbers for overlay rows

This file is simple and mostly healthy.

### E. `RFArmyCommandContexts.cs`

This stores current UI state:

- selected army
- selected leader party
- current target settlement
- current army behavior
- whether the movie is loaded
- influence already sent in this screen

This is fine as a short-term UI state holder.

### F. `RFArmyCommandActions.cs`

Right now this only transfers influence from the player clan to another clan.

Very small, very safe.

### G. `RFArmyCommandPrefabPatches.cs`

This injects the custom widgets into:

- `ArmyManagement`
- `ArmyOverlay`
- chat log margin

This part is normal UI extender work.

### H. `SubModule.cs`

This is where the patches get activated.

Important detail:

- `harmony.PatchAll()` runs broadly
- `RFArmyCanManagePatch.Apply(harmony)` runs as a delayed direct patch

So `ArmyCommand` currently depends on Harmony patching at startup.

## 3. Current functional flow

This is what the module is doing when it works.

### Flow 1: custom army overlay

1. the map army overlay is opened
2. the RF overlay context is created
3. all armies in the player's kingdom are listed
4. the player selects one army
5. the right side summary updates

This part is mostly clean.

### Flow 2: open management for a selected army

1. the player opens army management
2. the module intercepts `ArmyManagementVM`
3. it rebuilds party lists and cart lists
4. it decides who the "main party" of that screen is
5. it refreshes the screen with RF-only context

This is the dangerous part.

The module is not lightly extending vanilla here.

It is practically rebuilding the VM state by hand.

### Flow 3: create or modify an army

When the player confirms:

- if the selected leader has no army yet, it creates a new army
- if the army already exists, it changes membership
- then it applies the chosen order

The order application is currently simple:

- if there is a target settlement -> `army.Gather(targetSettlement, null)`
- if there is no target -> `army.FinishArmyObjective()`

That means the system already controls real campaign behavior, but only at a basic level.

## 4. What is already good

These parts are worth keeping.

### Good 1. Kingdom-wide overlay idea

This is strong.

Vanilla does not give this broad kingdom command feeling.

### Good 2. Player-readable army summary

Food, cohesion, men, influence, and joining status are useful.

This is good strategy UI.

### Good 3. Order target + order behavior split

The fact that the player can choose both:

- where the army should go
- what kind of mission it should perform

is a good design direction.

### Good 4. Support influence action

This fits the fantasy of backing another lord's army instead of only controlling your own.

### Good 5. Reusing vanilla army objects

The module is not inventing fake armies.

It works on real `Army`, `MobileParty`, and `Settlement` objects.

That is the correct foundation.

## 5. What is weak right now

This is the important part.

### Weak 1. The `ArmyManagementVM` patch is too invasive

The module uses:

- private field refs
- reflected private methods
- constructor patching
- manual rebuilding of VM collections

That makes it very sensitive to any base-game update.

This is exactly why the current crash appeared.

### Weak 2. The constructor patch target is brittle

Your crash showed:

- Harmony cannot find the expected target method for `RFArmyManagementVMPatches::ConstructorPostfix(...)`

In simple words:

the game changed the door, and this patch is still trying to walk through the old door.

So even before "design" problems, there is a hard compatibility problem.

### Weak 3. Too much logic lives in the UI patch

The screen patch currently does all of this:

- selects the leader party
- rebuilds army membership state
- calculates costs
- creates armies
- disbands armies
- applies campaign orders

That is too much responsibility in one place.

The result is fragile and hard to reason about.

### Weak 4. Army orders are still shallow

Right now the final order layer is basically:

- gather at settlement
- or clear objective

That is enough for a first version, but not enough for a truly strong army-command system.

It still lacks:

- front-aware targeting
- threat-aware targeting
- multi-step objectives
- regroup logic
- reinforcement timing
- offensive vs defensive staging

### Weak 5. No deep connection yet with RF war intelligence

The module knows armies.

The `RF_warsystem` knows:

- fronts
- theaters
- operational rhythm
- campaign phase
- strategic focus

These two brains should talk to each other much more.

Right now they are still too separate.

## 6. Why the current crash makes sense

The screenshot error:

- `ArgumentException: Undefined target method for patch method ... RFArmyManagementVMPatches::ConstructorPostfix(...)`

means the Harmony patch signature no longer matches the game's current `ArmyManagementVM`.

So this is not a random bug.

It is a structural fragility problem.

Very likely causes:

- the base game constructor changed
- the deployed DLL is older than the current source
- the patch is still assuming constructor data that no longer exists

## 7. The real opportunity

The good news is this:

the idea is much better than the current implementation.

So we should not throw the concept away.

We should rebuild the weak center while keeping the strong features.

## 8. Best plan to make it much better

This is the practical path.

### Stage 1. Stabilize the module

Goal:

make the module stop depending on a brittle constructor patch.

Work:

- inspect the current Bannerlord 1.3.x `ArmyManagementVM` signature
- retarget the patch to the real constructor or a safer refresh/init method
- remove any stale patch signature
- confirm only one deployed DLL is being loaded

Success check:

- game starts without Harmony patch failure
- army overlay opens
- army management opens reliably

### Stage 2. Reduce patch invasiveness

Goal:

stop rebuilding more of vanilla than necessary.

Work:

- keep the custom UI mixins
- keep the custom overlay
- move campaign logic out of the VM patch where possible
- let vanilla keep more of its own party/cart lifecycle

Success check:

- fewer private field refs
- fewer manual VM resets
- less risk on future game updates

### Stage 3. Create a real Army Command service layer

Goal:

separate "screen logic" from "army logic".

Work:

- create one internal service that handles:
  - army creation
  - army modification
  - army disband
  - order application
  - target validation
- keep the UI as only a caller

Success check:

- UI code becomes simpler
- army behavior can be tested or audited separately

### Stage 4. Upgrade the order system

Goal:

make army commands feel intelligent, not just manual target clicks.

Work:

- add order presets such as:
  - defend frontier
  - relieve siege
  - raid enemy villages
  - gather for campaign
  - intercept hostile army
  - hold reserve
- let each preset choose a better target automatically

Success check:

- player can command in a strategic way without micromanaging every settlement

### Stage 5. Connect Army Command to `RF_warsystem`

Goal:

make the command screen use the actual war brain.

Work:

- pull suggested targets from:
  - frontline data
  - theater data
  - campaign phase
  - operational rhythm
  - coalition role
- show recommended army missions based on current war state

Success check:

- the system stops feeling like a separate menu
- it starts feeling like the military hand of the RF war AI

### Stage 6. Improve player readability

Goal:

make the player understand why an order is good or bad.

Work:

- show simple labels like:
  - "front under pressure"
  - "enemy siege risk"
  - "safe rally point"
  - "army low on food"
  - "cohesion too low for deep push"
- add recommended target reason text

Success check:

- the player understands the command choice without reading code or guessing

### Stage 7. Add smarter army personalities later

Goal:

make different lords feel different when selected as commanders.

Work:

- some commanders prefer:
  - fast raids
  - defensive gathering
  - deep sieges
  - frontier patrol
- later this can connect to traits, culture, religion, or RF strategic profiles

Success check:

- armies feel like they are led by people, not identical robots

## 9. Best order to implement

This is the order I would follow.

1. fix the constructor/patch compatibility
2. isolate army actions into a service layer
3. keep the overlay and UI mixin structure
4. connect target suggestions to `RF_warsystem`
5. expand command presets
6. add richer commander personality

This is the safest path because it fixes the broken center before adding more features on top.

## 10. My honest recommendation

If we want this system to become really good, we should not keep stacking patches on the current VM-rebuild approach.

The right move is:

- keep the overlay idea
- keep the strategic command idea
- keep the target/behavior controls
- rebuild the center more cleanly

In short:

the idea is strong, the current center is brittle, and the best future is a cleaner second pass instead of more emergency patches.

## 11. What I would do next

Immediate next step:

1. remap the live `ArmyManagementVM` entry point for the current game version
2. fix the broken Harmony target
3. prove the module can open and survive the management screen again

After that:

4. start extracting the army create / modify / disband / order logic into a dedicated service

That gives us a stable base before the "make it much better" phase.
