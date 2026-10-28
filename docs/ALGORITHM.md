# How the Forge of Potential decides an upgrade

Read from the decompiled `assembly_valheim.dll` and `assembly_utils.dll` of **Valheim 1.0.21** (Steam build
26249040), then checked against the live Unity runtime and real attempts (see [VALIDATION.md](VALIDATION.md)).

## Where it runs

Everything happens on the client that presses the button. `InventoryGui.DoCrafting(Player)` runs locally after the
craft timer, rolls with the local `Player`, edits the local inventory and stores the new seed in the local character
(`.fch`). The server never sees the roll. On a dedicated server the only server-side influence is mods that sync
their configuration to clients (ImpactfulSkills, PotentialReforged); this plugin reads those values after sync.

## Methods and fields

| Member | Role |
| --- | --- |
| `InventoryGui.UpdateRecipe(Player, float)` (private) | Per frame while the crafting UI is open; runs the craft timer and calls `DoCrafting` when it ends. |
| `InventoryGui.OnCraftPressed()` (private) | Captures `m_craftRecipe = m_selectedRecipe.Recipe` and `m_craftUpgradeItem = m_selectedRecipe.ItemData`, starts the timer. |
| `InventoryGui.DoCrafting(Player)` (private) | The decision (below). |
| `InventoryGui.m_selectedRecipe` (private `RecipeDataPair`) | `Recipe` and `ItemData` selected in the list. |
| `Player.GetItemUpgradeChance(int hash)` (public) | Seed lookup, roll and successor seed. |
| `Player.m_itemUpgradeSeeds` (private `Dictionary<int, int>`) | Per-character seed table. |
| `Player.Save(ZPackage)` / `Player.Load(ZPackage)` | Write / read the table at the end of the player data (`PlayerData >= PostLaunch`). |
| `CraftingStation.m_upgrader` | True for the Forge of Potential (`UpgradeStation`, `$piece_upgradestation`). |
| `Piece.Requirement.m_upgraderResource` | Marks the idol requirement of a recipe. |
| `ItemDrop.ItemData.SharedData.m_upgradeChance` (default 0.65), `m_breakChance` (default 0.1), `m_breakReturnIngreientsAmount` (default 0.5) | Decision inputs, read from the idol. |
| `StringExtensionMethods.GetStableHashCode(string)` | The string hash. |

## The decision

```text
target   = m_craftUpgradeItem.m_quality + 1
hash     = m_craftRecipe.m_item.gameObject.name.GetStableHashCode() + target        // int, wraps
idol     = first requirement of m_craftRecipe with m_upgraderResource
roll     = Player.m_localPlayer.GetItemUpgradeChance(hash)

if idol.m_shared.m_upgradeChance >= roll              -> success: quality = target
else if idol.m_shared.m_breakChance >= 1f - roll      -> destroyed (part of the materials returned)
else                                                  -> quality = target - 2 (one level lost, no floor)
```

`GetItemUpgradeChance(hash)`:

```text
seed = m_itemUpgradeSeeds.TryGetValue(hash) ? stored : GetPlayerName().GetStableHashCode() + hash
UnityEngine.Random.InitState(seed)
roll = UnityEngine.Random.value
m_itemUpgradeSeeds[hash] = UnityEngine.Random.Range(0, 99999999)
return roll
```

Consequences:

- The identity is the **recipe's item prefab name**, not the inventory item, not the display name. Two copies of the
  same prefab share a sequence per target level; a different level is a different sequence.
- The upgrade list matches inventory items to recipes by the shared name token, so a modded item with the same
  token as a vanilla one is listed under both recipes; the selected recipe's prefab decides the hash (and the
  prefab of the resulting item).
- The forge allows upgrading past `m_maxQuality`.
- The odds come from the idol of the recipe, so they can differ per idol and per mod.
- Vanilla has three outcomes. With the code defaults (0.65 / 0.1) a roll in (0.65, 0.9) loses a level and a roll
  of 0.9 or more destroys the item. Idol prefabs can carry other values; the plugin always reads the live ones.
  (Forge Oracle and ValheimSaveParser report `m_breakChance = 1` on the idols, which makes every failure destroy.)

## Exact reproduction without touching the game's RNG

`UnityEngine.Random` is a global xorshift128. Calling it from a predictor would reseed the game's RNG, so the
plugin uses a private copy (`UnityXorShift128`):

```text
InitState(seed): x = (uint)seed; y = x * 1812433253 + 1; z = y * 1812433253 + 1; w = z * 1812433253 + 1
next():          t = x ^ (x << 11); x = y; y = z; z = w; w = w ^ (w >> 19) ^ t ^ (t >> 8); return w
value:           (float)(next() & 0x7FFFFF) * (1f / 8388607f)      // [0, 1] inclusive
Range(0, n):     next() % n
```

`GetStableHashCode` walks UTF-16 code units in pairs with two djb2 accumulators, stops at NUL, and returns
`even + odd * 1566083941`, all with wrapping 32-bit arithmetic.

Precision notes, proven exhaustively by the unit tests:

- `k * (1f / 8388607f)` and `k / 8388607f` give the same float for every 23-bit `k`, so it does not matter how Unity
  writes the division.
- The game compares `m_breakChance >= 1f - roll` without an explicit cast, so a runtime could keep `1 - roll` in
  higher precision. That never changes the branch: for `roll >= 0.5` the subtraction is exact, and for every smaller
  roll the float result rounds up, so no float threshold lies between the two values.

## Live self-test

At startup the plugin saves `UnityEngine.Random.state`, runs `InitState / value / Range(0, 99999999)` for 256 seeds
on the real Unity build and compares the results with the private copy, then restores the state in `finally` (main
thread, before any other code can draw). Predictions are VERIFIED only when all 256 match. On Valheim 1.0.21 the
result is `256/256`.

## Seed source

The plugin reads `Player.m_itemUpgradeSeeds` from the live `Player` with `TryGetValue` only, so it sees attempts
made since the last save. It never falls back to the `.fch` file. When the key has no entry it derives the seed
from `Player.GetPlayerName()` exactly as the game does.
