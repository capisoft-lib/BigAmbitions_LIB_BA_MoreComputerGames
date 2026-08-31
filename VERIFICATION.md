# Verification — MCG 1.0.0

The source repository contains the MCG library only. FlappyAmbition and the external Unity integration fixtures are not included. Compilation, isolated tests and actual Big Ambitions gameplay are separate levels of evidence.

## Reproducible checks in this repository

Verified on 2026-08-31:

- `dotnet run --project tools/Tests~/MCG.Tests.csproj -c Release`: **114 assertions passed** against the actual registry, monitor catalog, session, round and record-store sources, plus the repository's locale files. Unity presentation types are substituted; record JSON uses the actual managed serializer.
- Coverage includes registration without creating gameplay objects, duplicate ownership, lazy resource loading, cancellation, release after cancellation, session closure, display-scope lifecycle and subscriber failures.
- Monitor-catalog checks cover vanilla availability, Up/Down wraparound, selection preservation when registrations change, removal of the selected game and navigation without preparing any session.
- Record checks cover real temporary-file reload, 64-bit scores, atomic backup, strictly greater comparison, event ordering, write failures, corrupt/future/profile-mismatched files, rules separation and abandoned rounds. Vanilla round-state tests cover pending points on the last life and replay uniqueness.
- Locale checks cover all 22 native language codes, strict UTF-8 decoding, matching nonempty keys and placeholders, unique Unity metadata and every localization key referenced by MCG sources. The build also compares the locale files with the installed game's selectable-language index.
- `tools/build.ps1` compiles and packages MCG using only Big Ambitions 1.0 Build 3670 and Unity 2022.3.62f2 (`7670c08855a9`). No other mod library is supplied to the compiler. The build does not launch the game or modify an installation.
- The package contains one DLL, belonging to MCG. The output uses the game's Mono `mscorlib` profile and has no assembly reference to FlappyAmbition or ComputerGameHighScore. The builder rejects PDB references, embedded debug symbols, known private build paths and private build artifacts in the package.

See [the build guide](docs/COMPILATION.md) to reproduce these checks with your own dependencies. Generated compiler responses, references, test outputs and logs are private build material and are deliberately excluded from Git.

## Version 1.0.0 package checks

The latest native-button build keeps assembly version 1.0.0.0 and all 141 public signatures. Its 208 type references and 481 member references resolve against the installed game runtime. The existing 114 repository assertions and 168 isolated launcher/panel assertions pass with the [TAB] hint applied; the English panel render was inspected. This remains isolated evidence, not a restarted native-game test. The following paragraphs also retain earlier validation stages for context.

The manifest, public API version, DLL file/product metadata and assembly version agree on 1.0.0 (assembly identity 1.0.0.0). The 141 public type/member signatures from the 0.2.0 library are preserved. Removing the obsolete overlay-focus helper removes the last external mod-library reference; the native computer panel and shortcut guards are unchanged. Game and consumer reference checks are binary API validation, not a gameplay test of those mods.

After dependency removal, the DLL resolves all 192 type and 431 member references with only the game runtime supplied to the resolver. All 60 MCG references from the installed FlappyAmbition, Snake (Snacke) and AmbitionsInvaders assemblies resolve. The 89 repository assertions and 15 dynamic-record checks pass again. The compiler has no separate mod-library input, and the resulting DLL has no external mod-library assembly reference. Public API and gameplay keys are unchanged; this cleanup does not clear focus belonging to other interfaces.

The 1.0.0 DLL also passes the 15 dynamic-record checks described below, across separate write/read Unity Player processes. The launcher was rerun with the 1.0.0 API sources and passes 77 checks; the repository suite now passes 114, including 25 locale assertions. The package includes all 22 locale JSON files, the changelog and eight English/French release and Workshop text files. Source, existing Git history and package scans found no secrets; compiler checks found no private machine paths or debug symbols in the DLL.

## Language coverage and rendering

MCG provides 16 translation keys in each of the game's 22 selectable languages: **352 localized values** in total, including the native-panel return button. Existing translation values and locale Unity metadata are preserved. The native codes include `pt` for Brazilian Portuguese and `zh-cn` / `zh-tw` for Simplified/Traditional Chinese. Game names remain their own titles; separate game mods are responsible for their gameplay translations.

An isolated Unity 2022.3.62f2 probe loaded the actual locale JSON files and production menu view, then checked menu, loading and error states in every language: **66 states passed**, with no missing glyphs or detected text overflow on the validation machine. Representative Latin, Greek, Cyrillic, Japanese, Korean and Chinese renders were inspected at 960×540. The probe reproduced a stale selected-game heading when changing language while the menu remained open; that heading now refreshes with the other labels.

These rendering checks use the existing Unity runtime font and this machine's font fallback. They do not establish identical font availability on every player's system, nor replace a native Big Ambitions smoke test or review by native speakers. Public example screenshots remain in English.

## Earlier external integration checks

Before extraction into this repository, an isolated Unity player harness exercised actual AssetBundle loading/unloading, cancellation/reload, Addressables session ownership, Unity button-event replacement/restoration and session-owned display-profile copies. The 28 record/native-round cases also passed with real Unity `JsonUtility`. External game fixtures exercised the consumer lifecycle and camera rendering.

Those fixtures are not shipped here, so the 114-check command above does not reproduce that Unity coverage. Historical fixture results do not establish that the native Big Ambitions computer UI works in a live game.

## Native-monitor launcher checks

The native-panel **Return to menu [Backspace]** button is covered by an expanded isolated Unity run: **168 assertions pass**, including the earlier 77 launcher checks. The added checks cover pending-load cancellation and late asset disposal, mod/native/error returns, blocked clicks, persistent native Leave-listener isolation, abandoned rounds, pointer raycasting, horizontal/manual layout and cleanup. All 22 translated button labels fit beside Leave with no missing glyphs in the fixture; representative English, French, Cyrillic and Asian renders were inspected. The fixture uses a representative native-style panel with private test fonts; production clones the actual native Leave button and keeps its font/style. No fonts are shipped with MCG. A restarted native-game smoke remains required for the new button.

The native Leave caption now receives **[TAB]** while an MCG session owns the computer. Its localization component and click event are not replaced. The hint follows native caption changes, avoids duplication and restores its own text/temporary autosizing when the session closes. The return-button clone retains the template's maximum font size. The same 168-check fixture was rerun with this hint; its English panel rendering is separate from native gameplay verification.

The separate catalog popup has been replaced by a menu rendered inside the computer monitor. An isolated Unity 2022.3.62f2 harness using the exact production launcher, provider, loader and API sources passes **77 assertions**, with inspected English camera renders at 960×540, 1280×720, 800×600 and 1920×1080.

Coverage includes metadata-only opening, navigation/paging, loading before construction, cancelled and overlapping loads, retry after failure, mod removal, native-prefab cancellation, camera/RenderTexture handoff, music preference, display-effect ownership and cleanup. Existing FlappyAmbition gameplay sources run unchanged through the launcher. Mod rounds still update the common local record store; menu navigation and abandoned loads emit no fake results.

Tab exits the computer from the menu, a game or loading; the tests cover all three states, denied exit when the native shortcut guard is active, and repeated cleanup without fabricated scores. Backspace returns to the catalog. Escape remains the native pause shortcut and is neither consumed nor reset by MCG. MCG input and Tick are suspended while the native pause menu or options are open. English examples show the updated control hints.

The native prefab fixture implements the real `IVideoGame` interface and is instantiated through real Addressables. It verifies native-game hosting and score-adapter unwrapping, but does not execute the proprietary Brick Breaker gameplay. Its real round capture still requires a native-game check. Input tests invoke the menu's input handler; they are not physical keyboard automation in Big Ambitions. These external fixtures are not included in the 114-check command.

English examples are available under `release-assets/screenshots/` in the source repository. They are direct isolated monitor renders, not screenshots of a running Big Ambitions city. They contain no workstation paths or user-save information; Flappy remains a separate mod.

## External-mod record persistence correction — 2026-08-31

The user's missing-record failure was reproduced with the installed release DLL loaded by `Assembly.Load(byte[])` in an isolated Unity 2022.3.62f2 Mono Player. Unity `JsonUtility` wrote the document header but omitted its list of mod-defined record objects. Reload then rejected that incomplete file. Earlier tests compiled the record types into the Player and therefore missed this external-assembly case.

The store now uses `DataContractJsonSerializer`, verifies every serialized field before touching disk and keeps atomic replacement with backup. Only the exact known legacy header is accepted as an empty history; its first subsequent record preserves the original bytes in `.bak`. Unknown/incomplete/cross-profile data still fails closed. Scores absent from the original and backup cannot be reconstructed.

The corrected release DLL passes 15 checks in two separate Unity Player processes: completed record write/reload, independent games/rules, 64-bit values, strict improvement, old-file backup, legacy recovery and invalid-file protection. The original DLL reproducer passes two checks confirming the failure. The serializer, XML and core runtime assemblies in that Player are byte-identical to the installed game's libraries. An additional 120-check isolated Ambitions Invaders run passes with the corrected API source. These are external fixtures, not native game/save interaction.

## Native runtime boundary

**Native walking, physical keyboard/exit interactions, HDRP monitor rendering, real vanilla score capture and coexistence with other computer mods still need an in-game smoke test. Isolated rendered validation does not replace that test.**

The compiler checks references, not native UI behavior or future game-version compatibility. Building or publishing this source repository does not automatically install a ModsLocal package, modify a player's saves or publish a Steam Workshop item. Installation and its file comparisons are separate steps.
