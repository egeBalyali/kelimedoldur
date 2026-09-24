# Level categories

## Active trivia pack

The playable sequence now contains **Trivia_001 through Trivia_050**, in order.
The first level is Music and the second is Sports. There are 14 Easy, 23 Medium,
and 13 Hard levels; each category has seven levels except General Knowledge,
which has eight. Difficulty is stored on LevelData and in JSON.

The full import file is `Assets/trivia_levels_50.json`. `TRIVIA_LEVELS.md` lists
all questions and answers for review. Every level contains exactly two sentences:
one complete question followed by one fully hidden answer. The question also has
selected missing letters. All 100 pages fit the current 8-character / 8-line
layout. The prior level assets have been converted to two pages but remain outside
the active sequence. Saved player progress is unchanged; use Player Progress to
return to Level 1 if needed.

Run `Tests/TriviaPackTests.ps1` to check the content, layout, pools, and sequence.
`Tools/Build-TriviaLevels.ps1` regenerates the pack from its authored question list,
preserving existing trivia asset GUIDs; it overwrites the pack's assets/JSON and
sequence, so use it only when intentionally rebuilding this pack.

## Visual level designer

Open **WordGame > Level Designer**, double-click a LevelData asset, or use its
**Open Visual Level Designer** Inspector button.

1. Choose New or an existing level, then select its category and difficulty.
2. Type the complete question on **Question**, then its answer on **Answer**.
   The designer always has exactly these two pages. Legacy question pages are
   combined when an older asset is opened. The JSON importer rejects other counts.
3. Click individual letter tiles to make them blanks. Blue tiles are hidden from
   players. **Hide all letters** and **Reveal all** work on the selected page.
4. Review the player preview and enable **Show solutions** to compare. Read the
   current scene's line limits or adjust the preview limits to explore layouts.
   You can optionally assign a LetterSpriteLibrary to preview character artwork.
5. **Save level** updates the asset and regenerates its complete letter pool.
   **Save as new** creates a separate asset. **Include on save** appends it to the
   selected sequence if it is not already present.

The **Letter Pool** preview has **Shuffle** and **Question / answer order**
controls. Click a pool tile, then use **Move left**, **Move right**, or enter a
one-based **Position** and press **Move to**. The rows match the game's pool
layout and read left to right, top to bottom. Duplicate letters remain separate
tiles. Pool changes support Undo/Redo and are saved to the level asset; gameplay
uses that exact order.

Changing question/answer blanks preserves the remaining tiles' relative order,
removes surplus occurrences, and appends newly required letters. JSON export
includes `letterOrder`; the importer validates its complete letter counts before
changing assets, then preserves it. JSON without `letterOrder` still derives the
pool in question/answer order. The legacy `letters` field remains ignored.
Run `Tests/LetterOrderTests.ps1` to check shuffling and manual ordering.

Draft edits support Undo/Redo and are retained across script reloads. The designer
prompts before discarding unsaved edits. Existing asset filenames determine their
level names; choose a new filename with Save as new to make a named copy.

The preview uses the game's word wrapping and pool row distribution. It is an
approximation, not a rendered game scene: preview controls do not change runtime
layout settings. Warnings identify long words and pages beyond the chosen line
limit. Saving regenerates required tiles and removes hand-authored extra tiles.

**Export JSON** exports the current draft in the importer format. Importing that
single-level file replaces the sequence with that one level; combine its entry
with the full JSON level list before importing to retain a full sequence.

Run `Tests/LevelAuthoringTests.ps1` in PowerShell to check gap editing, JSON
round-trips and word wrapping without starting Unity.

## Category data

Levels play in one linear sequence. History, Science, Sports, Music, Movies,
Geography, and General Knowledge describe individual levels; they do not filter
or reorder gameplay. A Music level can be followed by a Sports level.

Select a generated LevelData asset in Unity and use its **Category** field to
assign a category. Include the asset in the LevelSequenceData list to make it
playable. LevelManager's LevelCount is the full sequence length and
CurrentLevelIndex is its zero-based current position.

## Saved progress and main menu

`PlayerLevelProgress` stores the one-based sequence position in PlayerPrefs under
`LevelCount`, defaulting to 1. Completing a level saves the next position. A saved
number beyond the sequence length means all levels are complete; Loop Levels
instead wraps back to 1. Category selection and separate category progression
are no longer used.

Open **WordGame > Player Progress** to set the saved number for testing or reset
to Level 1. Changes immediately refresh the menu and reload the active level in
Play Mode. Other PlayerPrefs are preserved. The sequence field in this tool is
only a preview of which level/category the number refers to.

MutluTema's **MainMenuCanvas > MainMenuController** shows the saved level number
and its category on the Play button, applies a category-specific button tint, and
opens LevelCanvas on Play. It returns to the menu after the last level. Assign
optional background sprites in **Category Themes** to change the menu artwork
for each category; empty slots use **Default Background**. Existing category
themes supply tints, while the current background artwork is retained by default.

Assign the gameplay controls canvas to MainMenuController's **Button Canvas**
field. Play opens both canvases; Home and final completion hide them. The existing
controls canvas in MutluTema is wired to **ButtonCanvasController**, with its Home
and Reset buttons assigned. You can replace these references by dragging objects
into the Inspector. Retry preserves fully correct words on every page and returns
all tiles belonging to incomplete or incorrect words to the pool. It starts at
the first sentence without changing the saved level number. Even correct letters
inside an unfinished word are returned; a word is kept only when all its blanks
are filled correctly. Restored words do not replay sparkle, sound or haptics.

## Haptic feedback

Both game scenes have **HapticFeedbackManager** alongside SentenceView. It listens
to the same question-word completion event as the sparkle/sound for happy feedback,
and LevelFlowManager's AnswerIncorrect event for sad feedback on a failed Submit
(including an incomplete answer). Wrong individual letter taps do not trigger sad
feedback. **Haptics Enabled** disables both; **Log In Editor** logs feedback events
during Play Mode. Component context menus provide happy/sad test actions.

Android uses native confirmation/rejection feedback with tap/long-press fallbacks
on older devices, respecting system touch-feedback settings. iOS uses native
success/error notification feedback through the included iOS plugin. Desktop
builds do nothing. Native haptics require physical-device verification; the iOS
plugin must also be compiled by Xcode as part of the iOS build.

API references: [Android haptic feedback](https://developer.android.com/develop/ui/views/haptics/haptics-apis)
and [Apple notification feedback](https://developer.apple.com/documentation/uikit/uinotificationfeedbackgenerator).

Run `Tests/ResetHapticsTests.ps1` for isolated tests of correct-word preservation,
tile return, repeated reset, and feedback subscriptions using the real flow logic.

Run `Tests/PlayerProgressTests.ps1` in PowerShell for isolated progression tests.
They run the real progress and manager logic against stand-in storage and scene
objects; they do not replace Unity Play Mode verification.

Import `Assets/trivia_levels_50.json` using **WordGame > Import Levels From JSON...**.
The importer updates assets by name and rebuilds the level sequence. Both
`example_levels.json` and `example_levels2.json` are now three-level subsets of
the new pack; importing a subset replaces the sequence with those three levels.

```json
{
  "levels": [
    {
      "name": "Level_Music",
      "category": "Music",
      "difficulty": "Easy",
      "sentences": ["WHAT CO_LOR ARE MOST KEYS ON A PIA_NO", "_W_H_I_T_E"]
    }
  ]
}
```

Category names are case-insensitive. `GeneralKnowledge` is also accepted.
Missing/blank categories default to General Knowledge for older JSON. Unknown
categories reject the entire import before assets are changed.

Use `_x` to hide a letter that the player must fill. Letter pools are derived from
these gaps on import, including duplicates. Existing hand-authored extra letters
remain in the generated assets until those assets are reimported.
