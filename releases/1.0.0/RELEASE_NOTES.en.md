# More Computer Games (MCG) 1.0.0

MCG turns the in-game computer into a shared home for mini-games. Use the native **Play Video Games** action to open the catalog on the monitor, keep the original Brick Breaker and add compatible games as separate mods.

## What's included

- A monitor-based game menu: **Up/Down** selects, **Enter** plays, **Backspace** returns to the catalog or cancels loading, and **Tab** leaves the computer. **Escape** keeps Big Ambitions' native pause menu.
- Game resources load only after selection. MCG handles loading, cancellation, retry, camera handoff and session cleanup.
- One local high-score system for vanilla and mod games. Only a higher completed score replaces a record; abandoned rounds do not count. Records are separated by Steam profile, game and rules.
- A fix for incomplete score files created by earlier builds. Managed JSON retains the record list and verifies the serialized data before writing. A known header-only legacy file is preserved as a backup on the next new record; missing scores cannot be reconstructed.
- A documented registration and lifecycle API for game authors. The public 0.2.0 signatures, assembly name, mod ID and record schema are preserved.

## Requirements and installation

Target: **Big Ambitions 1.0 Build 3670 on Windows**, Unity 2022.3.62f2 / Mono. Install **LIB BA Unified UI 1.0.2+** separately and enable both libraries. Close the game before replacing the MCG package in `ModsLocal/LIB_BA_MoreComputerGames`, then restart it.

MCG contains no additional game mod or bundled dependency DLL. [FlappyAmbitions](https://github.com/capisoft-lib/BigAmbitions_MCG_FlappyAmbitions), [Snacke](https://github.com/capisoft-lib/BigAmbitions_MCG_Snacke) and [Ambitions Invaders](https://github.com/capisoft-lib/BigAmbitions_MCG_AmbitionsInvaders) are separate examples. ComputerGameHighScore is not required. Existing local record files are not deleted by the installation.

## Roadmap and validation

We plan to explore adding a shared leaderboard soon. It is not available yet and has no confirmed release date. Scores remain local; this release does not upload them or require an additional account.

The [verification report](../../VERIFICATION.md) distinguishes compilation, isolated tests and native gameplay. Physical keyboard interactions, walking to the computer, HDRP monitor rendering, real Brick Breaker score capture and coexistence still need an in-game smoke test. English example images are isolated monitor renders, not screenshots of a running city.

These notes accompany the source/package release. They do not publish a Steam Workshop item or configure its Required Items.
