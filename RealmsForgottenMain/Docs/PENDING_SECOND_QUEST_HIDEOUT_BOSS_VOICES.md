# Pending: Part Two Hideout Boss Dialogues and Voices

Status: waiting for the custom ElevenLabs voice files and final English dialogue text.

## Scope

Add quest-exclusive dialogue and voice playback for the Hellbound bosses encountered in these Part Two hideouts:

- `hideout_seaside_13`
- `hideout_seaside_14`
- `hideout_seaside_11`

The new dialogue must never replace or play during ordinary Bannerlord hideouts.

## Findings

- The character `hellbound_boss` (Ashael) is correctly configured as a bandit and as the Hellbound culture boss.
- The vanilla hideout controller does start a boss conversation, but the vanilla dialogue line also requires `PlayerEncounter.EncounteredParty` to be recognized as a bandit party attached to a hideout.
- The Part Two quest creates its Hellbound defender parties manually, so that vanilla condition can fail even though the correct boss agent appears.
- Part Three already demonstrates the appropriate solution: a quest-specific boss dialogue with priority `125`, above the vanilla priority of `100`.

## Planned implementation

1. Register a custom boss `DialogFlow` in `SecondQuest`.
2. Require all of the following:
   - Part Two quest is active;
   - conversation character is `hellbound_boss`;
   - current settlement is one of the three listed hideouts;
   - map-fragment progress corresponds to that hideout;
   - the standard `HideoutMissionController` is present.
3. Display the custom English text and play the matching voice when the NPC line begins.
4. Preserve both normal outcomes:
   - accept a duel via `HideoutMissionController.StartBossFightDuelMode`;
   - refuse the duel and fight with companions via `HideoutMissionController.StartBossFightBattleMode`.
5. Keep the text functional if an audio file is missing, so the mission cannot become blocked.
6. Reuse the existing `module_sounds.xml` and Realms Forgotten mission audio infrastructure rather than creating a global voice replacement.

## Expected audio files

- `sq_hideout_boss_seaside13.ogg`
- `sq_hideout_boss_seaside14.ogg`
- `sq_hideout_boss_seaside11.ogg`

Recommended format: OGG Vorbis, mono, 44.1 or 48 kHz, approximately 8-15 seconds, no background music.

## Still needed from the user

- Final English subtitle text for each hideout.
- The three corresponding voice files, or confirmation that one shared line/audio should be used for all three.
