# LIB BA More Computer Games — preview 0.2.0

A shared library for adding mini-games to the computers inside Big Ambitions.

![More Computer Games promotional artwork](Thumbnail.jpg)

**This repository contains MCG only.** FlappyAmbition is a separate mod: its sources, assets, tests and binaries are not included. The original brick-breaker game remains available without installing any additional games.

## Documentation

The detailed guides below are currently in French:

- [Install and use MCG](docs/UTILISATION.md).
- [Create a compatible game](docs/CREER_UN_JEU.md), then read the [full API reference](API.md).
- [Build and verify MCG](docs/COMPILATION.md).
- [Source and package privacy](docs/CONFIDENTIALITE.md).

The Steam display title is **LIB BA More Computer Games**, also stored in **release-assets/WORKSHOP_TITLE.txt** for publication. The short name is **More Computer Games (MCG)**. Install the package in **ModsLocal/LIB_BA_MoreComputerGames**. The technical mod ID and assembly name remain **LIB_BaComputerGames** to preserve references from game mods and the C# API. Big Ambitions uses the folder name in its local mod list; the Workshop title is entered separately when publishing.

The Steam thumbnail is **Thumbnail.jpg**. The original PNG and generation prompt are in **release-assets/**. This promotional artwork illustrates the library's purpose; it is not a screenshot or a list of included games.

## What MCG provides

When MCG is active, the computer's native video-game action opens the game catalog. No extra button is added: the original button's text, position and availability conditions are preserved. The original brick-breaker remains in the list even without additional game mods, and the time-passing action is unchanged. Disabling MCG restores the original play action.

Game authors provide a description, gameplay and an optional resource loader. MCG handles the catalog, monitor integration, standard controls and session cleanup.

MCG also saves local high scores for the original brick-breaker and added games through the same round-completion event. Only a strictly higher score replaces a record. Records are separated by Steam profile, game and rules, shared across saves and stored outside ModsLocal. No additional online account or sharing is required. Abandoned rounds do not count.

See [API.md](API.md) for the interfaces, a minimal example and AssetBundle loading.

## Available games

- **Brick Breaker** — the original Big Ambitions game, available without installing another game mod.
- **[FlappyAmbitions](https://github.com/capisoft-lib/BigAmbitions_MCG_FlappyAmbitions)** — fly a banknote between office towers. A separate MCG game mod and an example for developers creating their own games.
- **[Snacke](https://github.com/capisoft-lib/BigAmbitions_MCG_Snacke)** — grow a bread snake into a sandwich by eating lettuce, tomatoes and rare cheese. A separate MCG game mod translated into all 22 game languages.

Additional games must be installed separately. MCG does not include or automatically download them; FlappyAmbitions is not required to install, build or use this library.

## Loading lifecycle

- On city load: register game metadata and read the small local-record file; no gameplay objects or bundles are preloaded.
- After selection and arrival at the computer: load optional game resources.
- During native game instantiation: create the gameplay and its camera.
- On session closure, game removal or library unload: stop gameplay and release resources.
- DLLs stay loaded in the process, as with other Unity mods. This mechanism does not unload assemblies.

Games can register before or after the library becomes active. A namespaced identifier such as **mystudio:my-game** must be unique in the catalog; duplicates are explicitly rejected. Each registration owns a token that removes only its own game and sessions.

## Dependencies and distribution

Target: **Big Ambitions 1.0 Build 3670 / Unity 2022.3.62f2**, with **LIB_BaUnifiedUI 1.0.2+** installed separately. Do not bundle MCG or BAUI DLLs inside individual game mods. Add the dependencies to Steam Required Items when publishing.

This preview is not published on the Workshop and does not yet have a Steam item ID. API 0.2.0 is experimental. Mod loading remains under the official SDK: MCG does not scan the disk for DLLs, download code or use a network service.

## Build and verification

From the repository root, with the .NET 8 SDK installed:

```powershell
dotnet run --project tools/Tests~/MCG.Tests.csproj -c Release
```

The [build guide](docs/COMPILATION.md) explains how to build MCG alone using your own Big Ambitions, Unity and BAUI installations. Proprietary dependencies and generated binaries are not tracked in Git. GitHub's "Code" ZIP contains sources, not a ready-to-play mod package.

The included tests do not launch Big Ambitions or touch its saves. Earlier Unity checks using external game fixtures are documented separately in [VERIFICATION.md](VERIFICATION.md); those fixtures are not included in this repository. Native computer UI and in-game compatibility checks are still pending, as described there. The build script does not install or publish anything.

Original sources are released under the MIT license. Big Ambitions, Unity and other dependencies retain their respective licenses.

## ☕ Support MCG

If you enjoy MCG, you can help support its development with a coffee. Someone still has to keep the developer working.

**[Buy me a coffee](https://buymeacoffee.com/capitaine)**
