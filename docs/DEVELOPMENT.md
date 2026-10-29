# Development

## Building

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Requirements: .NET SDK 10 and a local Valheim install for the plugin. The game path defaults to
`~/.steam/steam/steamapps/common/Valheim`; override with `-p:ValheimDir=...` (or `VALHEIM_DIR`). Without the game,
build and test the engine alone:

```bash
dotnet test --project tests/ForgeOracle.Core.Tests -c Release
```

- `dotnet build src/ForgeOracle.Plugin -c Release -p:Deploy=true` copies the plugin into
  `BepInEx/plugins/ForgeOracleClient` of the r2modman `Default` profile (override with `-p:R2ModmanProfile=...` or
  `-p:BepInExDir=...`).
- Release builds map local paths to `/_/` (`PathMap` in `Directory.Build.props`), so the DLLs carry no user name
  or home folder.

## Thunderstore package

```bash
thunderstore/package.sh
```

Result: `artifacts/ForgeOracleClient-<version>.zip`, laid out as Thunderstore expects:

```
manifest.json
icon.png        256x256
README.md       this repository's README, links and screenshots made absolute
CHANGELOG.md, LICENSE, THIRD_PARTY_NOTICES.md
plugins/ForgeOracleClient.dll
plugins/ForgeOracle.Core.dll
```

Thunderstore only renders images from absolute URLs, so screenshot links point at
`raw.githubusercontent.com/<website_url>/main/...`; the screenshots must be pushed first.
`screenshots/CAPTURE.md` lists the images the README expects.

Before uploading:

1. Bump `<Version>` in `Directory.Build.props`, `Version` in `ForgeOracleClientPlugin.cs` and `version_number` in
   `thunderstore/manifest.json`. The script refuses to build when they disagree.
2. Add the release notes to `CHANGELOG.md`.
3. Upload the zip at <https://thunderstore.io/c/valheim/create/> under the team.

## Golden vectors

`tests/ForgeOracle.Core.Tests/Fixtures/golden-vectors.json` comes from the Forge Oracle reference implementation.
To regenerate it, clone <https://github.com/papptimus/Forge-Oracle> and run

```bash
node tools/golden/generate-vectors.mjs <Forge-Oracle/index.html> <commit>
```

## Layout

| Folder | Contents |
|---|---|
| `src/ForgeOracle.Core/` | Prediction engine (netstandard2.0, no Unity): string hash, RNG copy, rules, forecasts, providers |
| `src/ForgeOracle.Plugin/Game/` | Read-only access to the forge selection and the character's seed table |
| `src/ForgeOracle.Plugin/Compatibility/` | Harmony patch scan, known mods, rules resolvers for forge mods |
| `src/ForgeOracle.Plugin/Prediction/` | Per-frame change detection, prediction pipeline, destruction confirmation |
| `src/ForgeOracle.Plugin/UI/` | Description line, detail panel, formatting |
| `src/ForgeOracle.Plugin/Diagnostics/` | RNG self-test, validation log, console commands |
| `src/ForgeOracle.Plugin/Patches/` | `InventoryGui` and `Player.GetItemUpgradeChance` hooks |
| `tests/ForgeOracle.Core.Tests/` | xUnit v3: golden vectors, exhaustive float checks, provider behaviour |
| `tools/golden/` | Golden vector generator |
| `thunderstore/` | Manifest, icon, package script |

## How it reads the game

- **Selection.** `InventoryGui.m_selectedRecipe` (recipe and item) and the current station's `m_upgrader` flag. The
  identity is `Recipe.m_item.gameObject.name`, exactly what `DoCrafting` hashes.
- **Seeds.** `Player.m_itemUpgradeSeeds`, read with `TryGetValue` only, for the target level and the forecast
  levels. Without an entry the seed is derived from `Player.GetPlayerName()` as the game does.
- **Odds.** The idol's `m_upgradeChance`, `m_breakChance` and `m_breakReturnIngreientsAmount`, or the values a
  supported forge mod computes with its own functions.
- **Change detection.** The `UpdateRecipe` postfix compares a small key every frame (recipe, item, quality,
  player, config and language versions) and recomputes only when it changes. Each attempt bumps the key.
- **Display.** The prediction is prepended to `m_recipeDecription` every frame after the game rewrites it (the
  postfix runs last, after mods that add their own lines). The panel is built once per `InventoryGui` instance and
  docks on the side of the forge panel with free screen space.
- **Validation.** A `DoCrafting` prefix (void, first) records the prediction; the `GetItemUpgradeChance` postfix
  records the real roll and the stored seed; the `DoCrafting` postfix counts the item by quality to see what
  happened.

More detail, including the decompiled members, in [FINDINGS.md](FINDINGS.md).
