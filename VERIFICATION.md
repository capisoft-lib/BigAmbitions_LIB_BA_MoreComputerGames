# Verification — MCG preview 0.2.0

The source repository contains the MCG library only. FlappyAmbition and the external Unity integration fixtures are not included. Compilation, isolated tests and actual Big Ambitions gameplay are separate levels of evidence.

## Reproducible checks in this repository

Verified on 2026-08-31:

- `dotnet run --project tools/Tests~/MCG.Tests.csproj -c Release`: **77 assertions passed** against the actual registry, session, round and record-store sources. Unity types and JSON serialization are substituted in this .NET test executable.
- Coverage includes registration without creating gameplay objects, duplicate ownership, lazy resource loading, cancellation, release after cancellation, session closure, display-scope lifecycle and subscriber failures.
- Record checks cover real temporary-file reload, 64-bit scores, atomic backup, strictly greater comparison, event ordering, write failures, corrupt/future/profile-mismatched files, rules separation and abandoned rounds. Vanilla round-state tests cover pending points on the last life and replay uniqueness.
- `tools/build.ps1` successfully compiles and packages MCG alone against Big Ambitions 1.0 Build 3670, Unity 2022.3.62f2 (`7670c08855a9`) and a separate BAUI 1.0.2 assembly. The build does not launch the game or modify an installation.
- The package contains one DLL, belonging to MCG. The output uses the game's Mono `mscorlib` profile and has no assembly reference to FlappyAmbition or ComputerGameHighScore. The builder rejects PDB references, embedded debug symbols, known private build paths and private build artifacts in the package.

See [the build guide](docs/COMPILATION.md) to reproduce these checks with your own dependencies. Generated compiler responses, references, test outputs and logs are private build material and are deliberately excluded from Git.

## Earlier external integration checks

Before extraction into this repository, an isolated Unity player harness exercised actual AssetBundle loading/unloading, cancellation/reload, Addressables session ownership, Unity button-event replacement/restoration and session-owned display-profile copies. The 28 record/native-round cases also passed with real Unity `JsonUtility`. External game fixtures exercised the consumer lifecycle and camera rendering.

Those fixtures are not shipped here, so the 77-check command above does not reproduce that Unity coverage. Historical fixture results do not establish that the native Big Ambitions computer UI works in a live game.

## Remaining runtime validation

**The native computer catalog, walking/loading/cancel interactions, HDRP monitor, vanilla score capture and coexistence with other computer mods still need an in-game smoke test. No native-game or visual catalog validation is claimed.**

The compiler checks references, not native UI behavior or future game-version compatibility. This repository publication does not perform a new ModsLocal deployment, modify a player's saves, or publish a Steam Workshop item.
