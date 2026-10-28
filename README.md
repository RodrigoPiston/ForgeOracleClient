# Forge Oracle Client

A client-side Valheim mod that shows, **before you click**, what the next Forge of Potential upgrade will do:
succeed, lose a level, or destroy the item.

```
▲ NEXT UPGRADE: SUCCESS  · VERIFIED
```

Forge of Potential results are not random at the moment you click. Each item type and target level has a seed
stored on your character; the game turns it into a roll and stores the next seed. Forge Oracle Client reads that
state from the running game and replays the same computation on a private copy of Unity's random generator. It
**reveals** a result that is already fixed; it does not change it.

## Contents

1. [What it does](#what-it-does)
2. [How predictions work](#how-predictions-work)
3. [Supported game versions](#supported-game-versions)
4. [Installation](#installation)
5. [Configuration](#configuration)
6. [UI and controls](#ui-and-controls)
7. [Supported equipment and modded items](#supported-equipment-and-modded-items)
8. [Multiplayer](#multiplayer)
9. [Incompatible and partially supported mods](#incompatible-and-partially-supported-mods)
10. [Troubleshooting](#troubleshooting)
11. [Reporting a wrong prediction](#reporting-a-wrong-prediction)
12. [Building from source](#building-from-source)
13. [Credits](#credits)

## What it does

- A one-line prediction as the first line of the forge's own item description, in the game font, with a symbol
  and a color (green success, red destroyed, amber level loss, gray unknown; a colorblind palette is available).
  The symbol and the words carry the meaning, so color is never the only cue. It never covers the upgrade button
  and follows layout mods.
- An optional detailed panel beside the crafting panel (F8), docked automatically on the side with free screen
  space: item, target level, outcome, exact roll and thresholds, confidence, provider, seed source, and a forecast
  of 1 to 20 rows.
- Two forecast views:
  - **Next attempts at this level**: what the next attempts at the same target level give, one after another,
    with any copy of the item.
  - **Level by level**: the first attempt at each higher level, assuming every earlier step succeeds. Rows after a
    predicted failure are greyed and marked as hypothetical, since the item would not reach them.
- English and Spanish, following the game language or a setting.
- A validation log: after each real attempt the BepInEx log shows the prediction next to what actually happened.
- Console diagnostics: `forgeoracle status`, `item`, `predict`, `providers`, `history`, `selftest`, `refresh`,
  `debug`, `reload`, `resetpanel`.

It never changes probabilities, costs, seeds, inventory, saves or network traffic, registers no RPC, and needs
nothing on the server.

## How predictions work

The full reverse-engineering notes are in [docs/ALGORITHM.md](docs/ALGORITHM.md). In short (Valheim 1.0.21):

```text
key  = hash(recipe item prefab name) + target level
seed = stored seed for key, or hash(character name) + key
roll = Unity Random.value after InitState(seed); the next seed is Random.Range(0, 99999999)
success   if idol.upgradeChance >= roll
destroyed if idol.breakChance   >= 1 - roll
otherwise the item loses one level
```

- The seed comes from the live `Player` object, so attempts you made since the last save are included.
- The odds come from the idol of the recipe, read live (and adjusted by a supported mod when one is active).
- At startup the plugin checks its RNG copy against Unity's real generator on 256 seeds. The Unity RNG state is
  saved first and restored immediately, so the game's random sequence is untouched.

A prediction is labelled:

| Label | Meaning |
| --- | --- |
| VERIFIED | Exact algorithm, live seed, passing RNG self-test, and no unresolved mod change. |
| UNVERIFIED | Computed, but something could make the game decide differently (an unaudited forge patch, an untested mod version or provider). The panel lists the reason. |
| PREDICTION UNAVAILABLE | No safe prediction: the roll is replaced by another mod, the RNG self-test failed, or required data is missing. |

## Supported game versions

Valheim **1.0.21**: algorithm read from this build, RNG self-test 256/256, and 5 of 5 real attempts matched
exactly, two of them on a dedicated server (see [docs/VALIDATION.md](docs/VALIDATION.md)). Other 1.0.x builds are likely
to work; the startup self-test and the validation log tell you whether they do. BepInEx 5.4.23 (BepInExPack_Valheim). .NET Framework 4.8 assembly for the game's Mono.

## Installation

### r2modman / Thunderstore Mod Manager / Gale

Install **ForgeOracleClient** and its dependency BepInExPack_Valheim. Nothing else is required.

To install a local build, use *Settings → Import local mod* (r2modman) or *Import → Local mod* (Gale) and pick
`artifacts/ForgeOracleClient-<version>.zip`.

### Manual

1. Install [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
2. Copy `ForgeOracleClient.dll` and `ForgeOracle.Core.dll` into `BepInEx/plugins/ForgeOracleClient/`.
3. Start the game once; the config file `BepInEx/config/rodrigopiston.forgeoracleclient.cfg` is created.

### Linux (Steam, native or Proton)

- Native Linux build: the mod manager handles it; for a manual install, set the Steam launch options to
  `./start_game_bepinex.sh %command%` in the game folder, as described by BepInExPack.
- Proton: set the launch options to `WINEDLLOVERRIDES="winhttp=n,b" %command%` and install as on Windows.

## Configuration

`BepInEx/config/rodrigopiston.forgeoracleclient.cfg` (editable in game with a configuration manager; changes apply
on the next frame, and `forgeoracle reload` re-reads the file).

| Setting | Default | |
| --- | --- | --- |
| General / Enabled | true | Master switch. |
| General / Language | Auto | Auto, English, Spanish. |
| General / RefreshOnStateChanges | true | Recompute after each attempt. When off, the prediction is cleared after an attempt until you pick another recipe or run `forgeoracle refresh`. |
| General / DebugLogging | false | Log every recomputation. |
| General / ToggleHotkey | F8 | Show or hide the detailed panel while the forge is open. |
| Display / ShowCompactPrediction | true | The prediction line at the top of the forge description. |
| Display / ShowConfidence | true | VERIFIED / UNVERIFIED next to the prediction. |
| Display / ShowExactRoll | false | Roll and thresholds. |
| Display / ShowInternalPrefabId | false | The prefab name the algorithm uses. |
| Display / ShowSeedTransitions | false | Seed → next seed per forecast row. |
| Display / EnableTooltips | true | Explains the forecast views. |
| Display / ColorblindPalette | false | Blue / orange / yellow instead of green / red / amber. |
| Display / HideWhenUnverified | false | Show PREDICTION UNAVAILABLE instead of an unverified prediction. |
| Detailed panel / ShowDetailedPanel | false | The panel beside the crafting panel. |
| Detailed panel / ExpandedLayout | true | Show the forecast table. |
| Detailed panel / PredictionCount | 5 | Forecast rows, 1 to 20. |
| Detailed panel / ForecastMode | RepeatedAttempts | Or Progression. |
| Detailed panel / PanelOpacity | 0.9 | Background opacity. |
| Detailed panel / UiScale | 1.0 | Scale of the line and the panel. |
| Detailed panel / Anchor | Auto | Side of the crafting panel: Auto picks the side with free screen space. |
| Detailed panel / Draggable | true | Drag the panel by its title. |
| Detailed panel / PanelOffset | 0,0 | Saved after dragging. |
| Detailed panel / ResetPanelPosition | false | Set true to move the panel back. |
| Safety / ConfirmPredictedDestruction | false | Hold back the first click on an attempt predicted to destroy the item; a second click within 5 s goes through. It never changes the outcome. |
| Safety / EnableSounds | false | Click sound when a destruction becomes predicted. |
| Safety / RngSelfTest | true | Turn off only for troubleshooting; predictions then stay UNVERIFIED. |
| Safety / ValidationLog | true | Log predicted vs. observed outcome for every real attempt. |

## UI and controls

- Open a Forge of Potential, go to the upgrade list and select an item. The prediction appears at the top of the
  item description straight away; no extra click.
- **F8** shows or hides the detailed panel. Drag it by the title.
- In the panel, **[+]/[−] forecast** shows or hides the forecast table, **[switch view]** alternates between the two
  forecast views (hover it for the explanation).
- The upgrade button itself is unchanged.

## Supported equipment and modded items

There is no list of supported items. Anything the forge lists, vanilla or modded, is predicted from the identity the
game itself hashes: the recipe's item prefab name. That covers Warfare, Armory, ValheimArmory, Monstrum, Wizardry
and any other item mod whose items use the vanilla forge code, including renamed items, duplicate display names
(several prefabs sharing `$item_battleaxe_crystal`, for example) and items above their declared maximum quality.
Items without an idol recipe show no prediction.

## Multiplayer

The forge roll runs on your client against your character, so predictions work on dedicated servers without
installing anything there. The plugin adds no network traffic. Limits:

- A server can push synced settings for forge mods; supported ones (ImpactfulSkills, PotentialReforged) are read
  after sync.
- ReforgedPotential, or any unknown mod that patches the roll itself, makes predictions unavailable.

See [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md).

## Incompatible and partially supported mods

| Mod | Status |
| --- | --- |
| ReforgedPotential | Unsupported: rolls with the unseeded global RNG. |
| ImpactfulSkills (forging) | Supported through its own odds function; validated in game on 0.23.0 (5/5). Other versions show UNVERIFIED. Later forecast rows assume your current forging skill. |
| PotentialReforged | Supported through its own threshold functions; UNVERIFIED (audited, not yet validated in game). |
| ForesightForge | Vanilla mode: supported. TierScaled mode: supported through its own failure classifier; UNVERIFIED (audited, not yet validated in game). |
| Two forge overhauls at once | Unsupported. |

## Troubleshooting

- **Nothing appears**: check `forgeoracle status` in the F5 console, and that `Enabled` and
  `ShowCompactPrediction` are on. The line only appears at a Forge of Potential with an item selected.
- **PREDICTION UNAVAILABLE**: the panel (F8) and `forgeoracle status` give the reason.
- **RNG self-test failed** (BepInEx log): the game's Unity build changed its random generator. Predictions stay
  off until the plugin is updated.
- **UNVERIFIED**: open the panel to see which mod or check is responsible. `forgeoracle providers` lists every
  patch on the forge methods.

## Reporting a wrong prediction

Every real attempt is logged in `BepInEx/LogOutput.log` as a `[Validation]` line with the predicted and the
observed roll, next seed and outcome, and MATCH or MISMATCH. Please include:

1. The `[Validation]` line(s) of the attempt.
2. The output of `forgeoracle status` and `forgeoracle providers`.
3. Your mod list (from the mod manager) and whether you were on a server.

Do not send your character file; it is not needed.

## Building from source

Requirements: .NET SDK 10 (8 also works for the core and tests), a local Valheim install for the plugin build.

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
packaging/package.sh
```

- The game path defaults to `~/.steam/steam/steamapps/common/Valheim`; set `VALHEIM_DIR` (or
  `-p:ValheimDir=...`) otherwise. Without a game install, build and test the engine alone with
  `dotnet test --project tests/ForgeOracle.Core.Tests -c Release`.
- `dotnet build src/ForgeOracle.Plugin -c Release -p:Deploy=true` copies the plugin into the r2modman profile
  (`R2ModmanProfile`, default `~/.config/r2modmanPlus-local/Valheim/profiles/Default`).
- Golden vectors: clone <https://github.com/papptimus/Forge-Oracle> and run
  `node tools/golden/generate-vectors.mjs <Forge-Oracle/index.html> <commit>` to regenerate
  `tests/ForgeOracle.Core.Tests/Fixtures/golden-vectors.json` from the reference implementation.
- See [CONTRIBUTING.md](CONTRIBUTING.md) for the code layout, the read-only rule and how to add a forge mod.

Layout:

```text
src/ForgeOracle.Core      prediction engine (netstandard2.0, no Unity): hash, RNG copy, rules, forecasts, providers
src/ForgeOracle.Plugin    BepInEx plugin (net48): game state readers, patches, UI, compatibility, diagnostics
tests/ForgeOracle.Core.Tests   xUnit v3: golden vectors, exhaustive float checks, provider behaviour
tools/golden              golden vector generator
packaging                 manifest, icon, package script
docs                      algorithm, compatibility, validation
```

## Credits

- **Forge Oracle** by papptimus (MIT): the web tool at <https://papptimus.github.io/Forge-Oracle/> and its source at
  <https://github.com/papptimus/Forge-Oracle> first documented the Forge of Potential seed table and how to read it
  ahead of time. This mod is an in-game, client-side take on that idea; the web tool's reference implementation
  (`hash` and `rollNext` in its `index.html`) produced the golden test vectors. Full notice in
  [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
- **ValheimSaveParser** by Melichor: the web version at <https://melichor.github.io/ValheimSaveParser/> and its
  source at <https://github.com/Melichor/ValheimSaveParser>, an independent analysis of the same mechanics.
- Unofficial fan project, not affiliated with or endorsed by Iron Gate AB.

---

## Resumen en español

Forge Oracle Client muestra, antes de hacer clic, qué hará la próxima mejora en la Forja del Potencial: éxito,
pérdida de un nivel u objeto destruido. Lee la semilla del personaje en la partida en curso y repite el mismo
cálculo con una copia privada del generador de Unity, sin tocar el del juego. Revela un resultado ya determinado;
no lo cambia. Funciona solo en el cliente, en un solo jugador y en servidores dedicados, sin instalar nada en el
servidor. La interfaz está en inglés y español (sigue el idioma del juego). F8 abre el panel detallado; la consola
F5 tiene `forgeoracle status`. Si una predicción falla, el registro de BepInEx contiene una línea `[Validation]`
con lo previsto y lo observado: inclúyela en el reporte.
