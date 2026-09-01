# More Computer Games (MCG) 1.0.1

MCG turns the in-game computer into a shared home for mini-games. Use the native **Play Video Games** action to open the catalog on the monitor, keep the original Brick Breaker and add compatible games as separate mods.

## Fixes and improvements

- Document and enforce the safe bootstrap for games distributed as separate Workshop items. A game's `RegisterModClass` entry must be loadable with BAModAPI alone; it can register its MCG definition from `OnLoad` after the dependency is available.
- Avoid calling native `VideoGameSetup.Finish()` after Big Ambitions has begun shutting down or tearing down the city. MCG still disposes its own session and references, while normal live-session closing retains the native finish path.
- Add configurable **Return to menu** and **Leave** shortcuts under **Options > Mods > More Computer Games**. Existing **Backspace** and **Tab** bindings remain the defaults.
- Save exact keyboard chords in the official mod-options preference namespace, with capture, clear, reset and duplicate-binding feedback. Changes apply immediately to input, monitor instructions and both native buttons in all 22 interface languages.

## Existing features

- A translated **Return to menu** button beside **Leave** in the native panel below the monitor, available during gameplay, loading or a launch error. Both buttons display the current bindings.
- MCG interface translations for all **22 selectable game languages**, following the language chosen in Big Ambitions. Individual game mods provide their own gameplay translations.
- A monitor-based game menu: **Up/Down** selects, **Enter** plays, **Backspace** returns to the catalog or cancels loading, and **Tab** leaves the computer. **Escape** keeps Big Ambitions' native pause menu.
- Game resources load only after selection. MCG handles loading, cancellation, retry, camera handoff and session cleanup.
- One local high-score system for vanilla and mod games. Only a higher completed score replaces a record; abandoned rounds do not count. Records are separated by Steam profile, game and rules.
- A fix for incomplete score files created by earlier builds. Managed JSON retains the record list and verifies the serialized data before writing. A known header-only legacy file is preserved as a backup on the next new record; missing scores cannot be reconstructed.
- A documented registration and lifecycle API for game authors. The public 0.2.0 signatures, assembly name, mod ID and record schema are preserved.

**For game authors:** rebuild compatible game mods against MCG **1.0.1**, assembly **1.0.1.0**. Keep the class decoded by `RegisterModClass` free of MCG base classes, fields and signatures: Big Ambitions may inspect it before resolving MCG from another Workshop item. Ship MCG as a separate Workshop dependency; do not bundle its DLL.

## Requirements and installation

Target: **Big Ambitions 1.0 Build 3670 on Windows**, Unity 2022.3.62f2 / Mono. **MCG requires no other mod library.** Enable MCG in the mod list. Close the game before replacing its package in `ModsLocal/LIB_BA_MoreComputerGames`, then restart it.

MCG contains no additional game mod or bundled dependency DLL. [FlappyAmbitions](https://github.com/capisoft-lib/BigAmbitions_MCG_FlappyAmbitions), [Snacke](https://github.com/capisoft-lib/BigAmbitions_MCG_Snacke), [Ambitions Invaders](https://github.com/capisoft-lib/BigAmbitions_MCG_AmbitionsInvaders) and [Tetrix](https://github.com/capisoft-lib/BigAmbitions_MCG_Tetrix) are separate examples. ComputerGameHighScore is not required. Existing local record files are not deleted by the installation.

## Roadmap and validation

We plan to explore adding a shared leaderboard soon. It is not available yet and has no confirmed release date. Scores remain local; this release does not upload them or require an additional account.

The [verification report](../../VERIFICATION.md) distinguishes compilation, isolated tests and native gameplay. Physical keyboard interactions, walking to the computer, HDRP monitor rendering, real Brick Breaker score capture and coexistence still need an in-game smoke test. English example images are isolated monitor renders, not screenshots of a running city.

Ready-to-copy English/French descriptions and change notes are listed in the [Steam publishing checklist](PUBLISHING_CHECKLIST.md). **MCG itself has no Steam Required Items.** Preparing these files or pushing GitHub does not publish the Steam item.
