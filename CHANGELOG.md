# Changelog

## 1.0.0 — 2026-08-31

- Add a translated Return to menu [Backspace] button beside Leave in the native computer panel. It uses the same return/cancellation path as Backspace and leaves the native Leave action unchanged.

- Translate the MCG interface into all 22 selectable game languages, preserving existing English/French strings. Refresh the selected-game heading when changing language while the monitor menu is open.
- Validate locale coverage against the native language index during builds, with key/UTF-8/placeholder/metadata checks in the repository test suite.
- Remove the obsolete external UI-library dependency. MCG builds and runs with the game's native computer integration and Unity UI, without another mod library.
- Open the game catalog directly on the computer monitor through the native Play Video Games action.
- Select with Up/Down and Enter; return to the catalog with Backspace and leave the computer with Tab. Escape keeps the native pause menu.
- Load game resources only after selection, with cancellation, retry and cleanup when leaving or switching games.
- Keep the original Brick Breaker alongside separately installed MCG game mods.
- Save local high scores through one round-completion API, separated by Steam profile, game and rules. Abandoned rounds do not create results.
- Fix incomplete record files produced by Unity serialization of dynamically loaded mod types. Managed JSON preserves full records and backs up the known legacy header on the next successful record.
- Preserve the public 0.2.0 API signatures, technical mod ID, assembly name and record schema while promoting version metadata to 1.0.0.
- Document the game-author API, existing games, build/privacy workflow and planned leaderboard. The leaderboard is not implemented and has no confirmed release date.

Release notes: [English](releases/1.0.0/RELEASE_NOTES.en.md) · [Français](releases/1.0.0/RELEASE_NOTES.fr.md).

Compilation and isolated tests do not replace a native Big Ambitions gameplay check. See [VERIFICATION.md](VERIFICATION.md) for the evidence and remaining boundaries.
