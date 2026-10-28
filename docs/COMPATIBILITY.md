# Compatibility

Forge Oracle Client finds out at run time which forge implementation is live, from two sources:

1. **Harmony patches** on `Player.GetItemUpgradeChance` and `InventoryGui.DoCrafting`, read with
   `Harmony.GetPatchInfo` (owner id and kind).
2. **Loaded BepInEx plugins** (`Chainloader.PluginInfos`) and, for known mods, their own configuration values.

| Finding | Severity | Effect |
| --- | --- | --- |
| Any other mod patches `GetItemUpgradeChance` | Blocking | `PREDICTION UNAVAILABLE — MODIFIED FORGE ALGORITHM` |
| ReforgedPotential is loaded | Blocking | Same; it rolls with the unseeded global RNG |
| Two or more forge overhauls active at once | Blocking | Same; the combination is not modelled |
| Unknown mod adds a prefix or transpiler to `DoCrafting` | Unverified | Prediction shown, marked UNVERIFIED |
| Unknown mod adds a postfix or finalizer to `DoCrafting` | Info | Runs after the decision |
| Known mod (table below) | per row | |

Equipment mods that only add items (Warfare, Armory, Monstrum, Wizardry, ValheimArmory, Jötunn items) need no
special handling: their items go through the vanilla forge code, and the plugin hashes the recipe's prefab name, so
any item with an idol recipe is supported without an allowlist.

## Audited mods

Audited from decompiled source (file and line notes were collected during the audit; summary here).

| Mod | GUID / Harmony id | Version | Classification | What it does to the forge |
| --- | --- | --- | --- | --- |
| ImpactfulSkills | `MidnightsFX.ImpactfulSkills` | 0.23.0 | Threshold modifier, provider `impactfulskills` | DoCrafting prefix overwrites the idol's odds from the forging skill and a per-level decay, finalizer restores them. Same values as its `ForgingRefinement.TryGetChances`, which the provider calls. A downgrade never goes below quality 1. XP after every attempt can change later rows. All settings are server-synced. |
| PotentialReforged | `Dyls123.PotentialReforged` | 1.1.1 | Threshold modifier, provider `potentialreforged` | Transpiler swaps the success and break thresholds and the failure quality for `Plugin.GetSuccessThreshold`, `GetBreakThreshold`, `GetFailureQuality` (called by the provider). Roll and seeds are vanilla. ServerSync. |
| ForesightForge | `com.drakosdj.foresightforge` | 1.0.0 | Threshold modifier in TierScaled mode, provider `foresightforge`; vanilla in Vanilla mode | TierScaled replaces DoCrafting but rolls through the real `GetItemUpgradeChance`. Failures are classified by its public `UpgradeSimulator.EvaluateFailure` using the idol tier: no change, downgrade (min 1) or break. Its own predictor saves and restores `Random.state`. It also writes `m_itemCraftType` every frame; this plugin uses its own UI elements, so the two do not collide. |
| ReforgedPotential | `akuichi.ReforgedPotential` | 2.0.8 | Roll replacer | DoCrafting prefix returns false and rolls `Random.Range(0f, 1f)` twice on the global RNG. Not predictable. |
| BetrValheim | `dezzycode.valheim.betrvalheim` | 5.1.5 | Benign | Salvage and batch crafting prefixes only act outside the forge; extra inventory routing does not change the outcome. |
| AdventureBackpacks | `vapok.mods.adventurebackpacks` | 2.3.4 | Benign | Keeps backpack contents across upgrades; adds idol requirements to backpack recipes. |
| WorkbenchesPlus | `com.morda.workbenchesplus` | 1.0.16 | Benign | Dismantle mode, not available at the forge. |
| Armory (Therzie) | `Therzie.Armory` | 1.4.3 | Benign | Socket hook inactive without Jewelcrafting. |
| VNEI, CraftingSearch | | 0.17.6, 1.0.2 | Not involved | No DoCrafting patches. |

A known mod at a different version than audited adds an UNVERIFIED note. A mod provider that has not yet been
validated against real attempts (see [VALIDATION.md](VALIDATION.md)) also stays UNVERIFIED.

## Not audited

Jötunn, Warfare, Monstrum, Wizardry, Equipment and Quick Slots and Extended Item Data Framework add items or UI but,
as far as the audit of the installed profile showed, no forge decision logic. If one of them patches the forge
methods in a later version, the patch scan reports it at run time. The following Thunderstore forge mods were found
but not audited and are therefore treated by the generic rules above: Sansara-ForgeOdds, WiresForgeOfGoodtential,
WiresForgeOfPotentialRework, ForgeOfPotentialRedux, ForgingPotential, MySix_ForgeOfPotential, ForgeRework,
ForgeOfIdols, ForgeControl, OdinBet_ForgeOfPotential, ForgeOfTruePotential, SurePotential, ForgeOfPotentialSafe,
OdinsBlessingForgeOfPotential, PotentialDraught, ServersideQoL_ObliteratorOfPotential.

**Limit of the patch scan:** a mod that changes the outcome without Harmony (for example by editing idol prefab
values at load) is invisible as a patch. That case is still predicted exactly, because the plugin reads the live
idol values. A mod that rewrites the decision without Harmony and without touching those values is not detectable.

## Multiplayer

- No RPC, no server component, no protocol change. The plugin is idle on a headless server process.
- The roll is local to the client, so the prediction is valid on dedicated servers, provided the server does not
  push a forge-changing mod configuration that the plugin cannot read. ImpactfulSkills and PotentialReforged
  synced values are read after sync. ForesightForge's TierScaled mode on a remote client follows the server's
  mode as ForesightForge itself reports it.
- A server-side-only mod cannot change the roll (it never runs on the server), but a server can run a different set
  of client-required mods; the patch scan covers what is loaded on your client.
