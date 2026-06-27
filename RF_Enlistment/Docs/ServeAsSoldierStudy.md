# ServeAsSoldier Study Notes

Study target:

- `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\ServeAsSoldier\bin\Win64_Shipping_Client\ServeAsSoldier.dll`
- decompiled to:
  `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\tmp\ServeAsSoldier_Decompiled`

## Main takeaway

The old `ServeAsSoldier` mod does **not** fundamentally model service as "the player is truly merged into a stable vanilla army system".

Its core model is:

- store the enlisted commander in a global state: `Test.followingHero`
- hide the main player party
- keep re-forcing campaign/map/battle state around that commander
- patch vanilla behaviors so the player looks and behaves as if attached to that lord

This confirms that the safest long-term direction for `RF_Enlistment` is:

- track service under a commander party
- control follow/attach state directly
- patch only the pieces needed for campaign feel
- avoid temporary fake armies as a primary solution

## Files worth studying

Core:

- `ServeAsSoldier\Test.cs`
- `ServeAsSoldier\SubModule.cs`

Important support patches:

- `ServeAsSoldier\AttachPatch.cs`
- `ServeAsSoldier\MainPartyMapEventPatch.cs`
- `ServeAsSoldier\NoDisperseMessagePatch.cs`
- `ServeAsSoldier\ConversationAttackPatch.cs`

Secondary but useful later:

- `ServeAsSoldier\SoldierMission.cs`
- `ServeAsSoldier\BattleCommandsPatch.cs`
- `ServeAsSoldier\NoRetreatPatch.cs`
- `ServeAsSoldier\NavalBattlePatch.cs`

## How old SaS starts service

Relevant method:

- `Test.JoinPartyAction()`

What it does:

1. Ends current mission / encounter.
2. Sets `followingHero = Hero.OneToOneConversationHero`.
3. Stores enlistment time, rank, xp state.
4. Calls diplomacy sync on next tick.
5. Hides the main party.
6. Stores old player inventory and gear.
7. Gives state-issued gear.
8. Clears menus and transitions into service mode.

Important lesson:

- old SaS treats enlistment as a **campaign mode switch**, not just a dialogue result

## How old SaS keeps the player under the commander

Relevant area:

- `Test.cs` around the large service maintenance tick logic

What it repeatedly does:

- makes sure the main party still contains the player hero
- restores the player as leader of main party if needed
- clears disbanding state
- forces the `party_wait` menu when the commander is not in a battle
- moves `MobileParty.MainParty.Position` to the commander's party position
- hides the player party again
- sets `MobileParty.MainParty.IsActive = false`
- if the commander enters battle, it tries to create/repair army context and merge the main party into it

Important lesson:

- the "always with the commander" feeling came mostly from:
  - hidden player party
  - forced menu state
  - position sync
  - battle interception

Not from a clean vanilla subordinate-army API.

## What is good and reusable conceptually

1. Persistent commander service state
   - `followingHero` is crude, but the idea is right
   - our `_serviceRecord.CommanderId` is a cleaner version

2. Service as a mode
   - enlistment changes campaign behavior, menus, diplomacy, equipment, and battle entry
   - this should stay true in `RF_Enlistment`

3. Hide/limit normal player party behavior while enlisted
   - the player should feel carried by the command structure
   - this is more important than literal party merge

4. Explicit battle interception
   - when commander enters a battle, service mode must redirect the player cleanly
   - this is one of the most important old SaS patterns

5. Clean leave-service recovery
   - restore gear
   - restore visibility
   - clear commander-follow state
   - undo diplomacy overrides

## What should NOT be copied directly

1. Fake emergency army creation
   - old SaS creates temporary army state when needed
   - this is exactly the kind of structure that later caused instability in our rebuild

2. Heavy global static state
   - `Test.followingHero`
   - many static flags
   - this worked for the old mod but is brittle and harder to reason about

3. Constant hard forcing of map/menu state without guardrails
   - useful as reference
   - risky as direct modern implementation

4. Broad diplomacy forcing as-is
   - old SaS temporarily aligns the player faction war/peace state with the commander's faction
   - this is powerful but can become dangerous in a larger overhaul stack

## Best architectural reading for RF_Enlistment

The old mod suggests three layers:

1. Service record layer
   - who is the commander
   - rank/xp/assignment/status

2. Campaign control layer
   - follow commander on map
   - suppress or redirect normal player movement
   - route menus into service-specific state

3. Battle interception layer
   - join the commander's battle
   - support reserve / scripted enlistment battle handling
   - restore player state after battle

Our rebuild already has layer 1.
We are partially through layer 2.
Layer 3 still has room to improve using this study.

## Immediate conclusions for RF_Enlistment

Worth porting as ideas:

- stronger "service mode" while enlisted
- more reliable battle interception
- explicit reserve / battle wait flow if desired later
- commander-context menus that behave differently from normal free-roam campaign

Worth avoiding:

- relying on temporary created armies
- assuming vanilla army membership is the correct backbone for soldier service

## Next recommended study targets

If we continue mining this mod, the next best files are:

- `ServeAsSoldier\SoldierMission.cs`
  - battle behavior while enlisted
- `ServeAsSoldier\BattleCommandsPatch.cs`
  - command-layer alterations
- `ServeAsSoldier\NoRetreatPatch.cs`
  - service battle restrictions
- `ServeAsSoldier\NavalBattlePatch.cs`
  - naval compatibility patterns
