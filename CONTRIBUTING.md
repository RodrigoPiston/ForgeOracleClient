# Contributing

Thanks for helping. Forge Oracle Client has one rule above all others: **it must never show a wrong prediction as
verified, and it must never change the game.** Every change is judged against that.

## Reporting a wrong prediction

Use the *Wrong prediction* issue template. The most useful data is the `[Validation]` line the plugin writes to
`BepInEx/LogOutput.log` after each real attempt, plus the output of `forgeoracle status` and `forgeoracle providers`
and your mod list. Please do not attach character files; they are not needed and contain personal data.

## Building

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

The plugin project references the game's assemblies from your local Valheim install; set `VALHEIM_DIR` (or
`-p:ValheimDir=...`) if the game is not in the default Steam folder. Without a game install you can still build and
test the engine:

```bash
dotnet test --project tests/ForgeOracle.Core.Tests -c Release
```

Never commit game assemblies, decompiled game code or character/world saves.

## Code layout

- `src/ForgeOracle.Core` is pure .NET (netstandard2.0): no Unity, no Valheim types. Algorithm changes go here, with
  tests.
- `src/ForgeOracle.Plugin` reads game state, draws the UI and detects other mods. It must stay read-only: postfixes
  or state-free prefixes, no RPCs, no writes to inventory, seeds, saves or game RNG.
- `tests/ForgeOracle.Core.Tests` uses xUnit v3. Expected values must come from an independent source (the Forge
  Oracle reference implementation through `tools/golden/generate-vectors.mjs`, or a model written separately in the
  test), never from the code under test.

## Adding support for a forge mod

1. Decompile the mod and write down every patch on `InventoryGui.DoCrafting`, `Player.GetItemUpgradeChance` and
   related methods: kind, conditions, what it changes.
2. If the roll stays the game's seeded roll and only thresholds or failure handling change, add an `IRulesResolver`
   that calls the mod's own functions or reads its config (do not re-implement its math if a pure function exists).
3. If the mod replaces the roll, add it to `KnownMods` as a roll replacer so predictions become unavailable.
4. Add the mod to `docs/COMPATIBILITY.md`. A provider stays UNVERIFIED until real in-game attempts matched; record
   them in `docs/VALIDATION.md` and only then add the GUID to `LiveValidation`.

## Style

- C# with nullable reference types, warnings as errors, 120 columns, small functions and files.
- English in code, comments and docs. UI strings go through `Localization/Texts.cs` (English and Spanish; new
  languages welcome).
- Conventional commits (`feat:`, `fix:`, `docs:`, `test:`), one logical change per commit.

## License

By contributing you agree that your contribution is licensed under the MIT License of this repository.
