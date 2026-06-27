# RF Enlistment Service Flow Audit

Date: 2026-06-15

## Goal

Audit the rebuilt enlistment service flow as a full state machine instead of fixing only isolated crashes.

Main flows checked:

1. Petition -> oath -> service start
2. Commander follow on campaign map
3. Commander enters settlement
4. Commander enters battle
5. Return from battle to service mode
6. Leave service

## Current model

The rebuild is using a service-mode approach, not a literal stable vanilla party merge.

That means:

- the player main party is hidden
- the player main party is set inactive
- campaign control is redirected into `rf_enlistment_service_wait`
- battle joining is intercepted from the commander state

This is closer to old Serve as Soldier behavior than to normal vanilla subordinate army membership.

## Problems confirmed during audit

### 1. Settlement wait crash

Problem:

- when the commander entered a settlement, the mod could fall into vanilla settlement wait flow
- vanilla expected a valid `PlayerEncounter`
- this produced null-reference crashes

Fix applied:

- do not push the enlisted player into vanilla settlement wait/town wait
- keep the player inside `rf_enlistment_service_wait` while the commander is in settlement

### 2. Service mode was not recognized as a valid attached state

Problem:

- the code only considered these states as "correctly attached":
  - same army
  - directly attached to commander party
  - escort behavior
- hidden inactive service mode was not treated as valid
- result: repeated reattach attempts even after successful service-mode entry

Fix applied:

- `IsMainPartyInCommanderArmy`
- `NeedsCommanderAttachment`
- `ShouldAutoJoinCommanderBattle`
- `IsMainPartyFollowingCommanderParty`

now all recognize hidden service mode as a real commander-follow state.

### 3. Commander inside settlement still looked like "missing attachment"

Problem:

- even after entering service mode, if the commander entered a settlement the code could again think attachment was missing
- that reopened the path to repeated attach attempts

Fix applied:

- service mode recognition no longer depends on the commander being outside settlement
- if the player is hidden, inactive, and in the service menu, that already counts as valid service mode

### 4. Leaving service could restore the player into an invalid campaign context

Problem:

- discharge restored visibility/activity
- but it did not reliably place the player back into a valid campaign context
- if the commander was inside a settlement, the player could be left outside with menu state still coming from service mode

Fix applied:

- discharge now captures the commander's current settlement before clearing service state
- after release, the player is placed into that settlement when appropriate
- if there is no settlement destination, service wait menu is closed cleanly

### 5. Battle transition restored the player party too early

Problem:

- service wait battle tick restored the player party before encounter creation/join was confirmed
- if battle interception failed or was delayed, the player party could leak back onto the campaign map unnecessarily

Fix applied:

- early restore was removed from the service-wait battle branch
- party restoration now happens only inside the actual encounter-join/create methods

### 6. Native wait menus could still break service mode before our old guard reacted

Problem:

- vanilla can request several different menus while the commander enters a settlement or the map state refreshes
- examples include `army_wait`, `army_wait_at_settlement`, `town_wait_menus`, and `town_outside`
- the old guard only reacted to a narrow subset and mainly during pending reattach logic
- that meant a wrong native menu could still be requested before service mode took control again

Fix applied:

- a new Harmony patch now intercepts `GameMenuManager.SetNextMenu`
- when the player is already hidden in commander service mode, or is in the short pending-attach transition, native wait/settlement menus are redirected back into `rf_enlistment_service_wait`
- battle menus are still allowed when the commander is genuinely entering combat

## Current expected behavior

After these fixes, the intended flow is:

1. Player takes the oath
2. Main party enters hidden service mode
3. Service menu stays active while commander travels
4. If commander enters settlement, service menu remains active without forcing town wait
5. If commander enters battle, player party is restored only for encounter creation/join
6. After battle, service mode can take over again
7. Leaving service restores main party visibility and activity

## Remaining known risks

These are not yet confirmed as broken in code, but they still deserve attention:

### A. `PauseEnterSettlement` setting is effectively not wired

The setting still exists in `RFEnlistmentSettings`, but current flow no longer uses it in a meaningful way.

This is a cleanup/design issue, not the cause of the main crash.

### B. Discharge while commander is inside a settlement

This was corrected during the audit.

### C. Temporary service UI

The current service menu is functional, but still clearly a temporary custom layer.

That is a polish problem, not a structural blocker.

### D. Fresh runtime confirmation still needed

The old runtime logs clearly showed repeated `TryAttachPlayerToCommanderDutySuccess` spam before the newest fixes.

The state-machine logic has now been corrected in code, but a fresh runtime trace is still needed to confirm:

- repeated reattach spam is gone
- settlement entry no longer falls into invalid vanilla menus
- battle join/return does not briefly leak the player party back onto the map

## Files most central to this audit

- `RFEnlistmentCampaignBehavior.cs`
- `RFMapStateEnlistmentPatch.cs`
- `RFLordConversationsCampaignBehaviorPatch.cs`
- `RFEnlistmentServiceRecord.cs`

## Audit result

The most dangerous problems were state-machine problems, not content problems:

- wrong settlement menu path
- service mode not recognized as valid
- repeated reattach logic
- native menu requests slipping past service mode

Those have now been corrected in code.
