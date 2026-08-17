====================================================================
  REALMS FORGOTTEN - CO-OP TEST BUILD  (read me first)
====================================================================

This lets two (or more) people play Realms Forgotten together over the
BannerlordCoop mod, with the War Sails DLC ON. It is an early test
build: expect rough edges and please send logs if something breaks
(see the last section).

Golden rule: EVERYONE must have the EXACT same mods, same versions.
Coop refuses the connection if the mod lists don't match. The easiest
way to be sure is to use the folders in this pack as-is.

--------------------------------------------------------------------
1) WHAT YOU NEED TO OWN YOURSELF (not included, can't be shared)
--------------------------------------------------------------------
- Mount & Blade II: Bannerlord (same game version as everyone else).
- The War Sails DLC (paid). Keep it INSTALLED and ENABLED - this build
  is made to run WITH it. Do NOT disable it.
- BannerlordCoop - subscribe to it on the Steam Workshop, and everyone
  updates it at the same time so the version matches.
- The usual dependency mods, same versions for everyone:
  Harmony, ButterLib, UIExtenderEx (and MCM if your setup uses it).
  (If unsure, ask the pack owner which versions to use.)

--------------------------------------------------------------------
2) WHAT'S IN THIS PACK (copy these into your Modules folder)
--------------------------------------------------------------------
Copy each folder into:
  ...\steamapps\common\Mount & Blade II Bannerlord\Modules\

- RF_Map
- RF_Races
- RealmsForgotten
- RF_Core_II
- RF_Core_III
- RF_Extension
- RF_CoopCompat   <-- REQUIRED. This is what lets Coop run with War
                      Sails and keeps Realms Forgotten stable. It is
                      not on the Workshop; it only comes from this pack.

If a folder already exists, replace it so the files are identical for
everyone.

--------------------------------------------------------------------
3) UNBLOCK THE DLLs (Windows blocks downloaded DLLs)
--------------------------------------------------------------------
Open PowerShell as Administrator and run (fix the path if needed):

  Get-ChildItem "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules" -Recurse | Unblock-File

--------------------------------------------------------------------
4) ENABLE MODULES IN THIS EXACT ORDER (top to bottom)
--------------------------------------------------------------------
In the Bannerlord launcher (Single Player tab), enable and order:

  1.  Native
  2.  SandBox Core
  3.  SandBox
  4.  Story Mode
  5.  Custom Battle
  6.  War Sails (NavalDLC)        <-- ON
  7.  Harmony
  8.  ButterLib
  9.  UIExtenderEx
  10. MCM                          (only if your setup uses it)
  11. RF_Map
  12. RF_Races
  13. RealmsForgotten
  14. RF_Core_II
  15. RF_Core_III
  16. RF_Extension
  17. Coop
  18. RF_CoopCompat                <-- MUST BE LAST

Everyone must use this same list and order. Do NOT enable RBM or Dual
Wield for this test.

--------------------------------------------------------------------
5) CONNECT (one person hosts; no separate server needed)
--------------------------------------------------------------------
Networking (easiest, no router setup):
  - Both install "Radmin VPN" (free).
  - One person creates a network (name + password); the other joins it.
  - The host's Radmin IP (looks like 26.x.x.x) is what the other types.

Host steps:
  1. Sandbox -> create a character -> load into the map -> SAVE -> back
     to main menu.
  2. Main menu -> "Host Coop Campaign" -> pick that save.

Joiner steps:
  - Coop join option -> type the host's Radmin IP.

War Sails stays ON on BOTH sides.

--------------------------------------------------------------------
6) IF SOMETHING BREAKS - what to send back
--------------------------------------------------------------------
- The exact on-screen error (especially any "module / version" message).
- The file:  Modules\RF_CoopCompat\rf_coop_compat.log   from BOTH the
  host and the joiner. Look for these lines:
     dlc-block: neutralizado ...ValidateNoDlc
     coop services resolved (broker=True, network=True, mapper=True)
     coop session STARTED, role=server   (host)
     coop session STARTED, role=client   (joiner)
- Coop's own logs:  bin\Win64_Shipping_Client\Coop_server.log (host)
  and Coop_client.log (joiner).

Most first-try failures are "mod lists don't match" - double check step
4 is identical on both machines.
====================================================================
