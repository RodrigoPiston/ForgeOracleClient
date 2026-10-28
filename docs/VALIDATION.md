# Validation

Status for version 1.0.0, Valheim 1.0.21 (Steam build 26249040), BepInEx 5.4.23.5, Linux native.

## Automated tests

`dotnet test -c Release` (or `dotnet test --project tests/ForgeOracle.Core.Tests -c Release` without a game
install) runs the engine tests on .NET 10:

| Group | What it checks |
| --- | --- |
| Golden vectors | Hash, key, seed, raw draw, roll and successor seed against the Forge Oracle reference implementation (`hash` / `rollNext`, commit 4ebded4): 23 hashes, 412 attempts (9 characters including non-Latin and surrogate-pair names × 9 vanilla and modded prefabs × 5 levels, plus stored seeds at 0, 1, 99999998, int.MaxValue, int.MinValue, -1), 7 chains of 12 repeated attempts, 8 boundary rolls (0, 1, 0.65±, 0.9±, 1.0), and 3 keys that wrap past int.MaxValue. |
| Outcome model | Expected outcomes from an independent JavaScript model of the DoCrafting branches (float32 via `Math.fround`), success / level loss / destruction, break chance 1, no floor at quality 0, mod failure models. |
| Exhaustive float checks | For all 2^23 draws: reciprocal multiply equals division; `float(1 - r)` never rounds down, so float and double evaluation pick the same branch. |
| Providers | Stored seed precedence, name-derived seed, missing name, repeated-attempt chaining, per-level keys, conditional rows, shared sequence across copies, different prefabs with one display name, confidence rules, blocking issues, provider exceptions, uncaptured keys. |

## In game

| Check | Result |
| --- | --- |
| Plugin loads through BepInEx | Yes, twice (`ForgeOracleClient 1.0.0 loaded (game 1.0.21)`). |
| RNG self-test against the live Unity RNG (256 seeds: InitState, value, Range(0, 99999999)) | **256/256 match**, in two separate game sessions. |
| Forge of Potential prefab | `UpgradeStation`, station name `$piece_upgradestation`, `m_upgrader = true`. |
| Real upgrade attempts compared with predictions | **5 of 5 MATCH** (table below). |
| Dedicated server session | 2 of the 5 attempts, nothing installed on the server. |
| UI | Checked on a 2532×1016 window with BetrValheim's layout (forge panel against the right edge): the prediction is the first line of the forge description, the panel docks on the free (left) side, no overlap with the upgrade button. Vanilla layout not checked. |

### Attempts

All with ImpactfulSkills 0.23.0 active (its odds applied), `BastardBlackmetal_TW` (Warfare), idol Black Metal
Battle Idol. "Match" means roll (float bits), next seed and outcome all equal.

| Time | Where | Step | Seed | Predicted | Observed | Odds at roll (success / break) | Match |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 17:51:43 | dedicated server | +1 → +2 | stored | destroyed, roll 0.926657 | destroyed, 0.926657 | 0.6720 / 0.2919 | yes |
| 17:52:41 | dedicated server | +1 → +2 | stored | destroyed, roll 0.790028 | destroyed, 0.790028 | 0.6720 / 0.2919 | yes |
| 18:09:32 | local world | +1 → +2 | stored | success, roll 0.046095 | success, q2 | 0.6720 / 0.2919 | yes |
| 18:10:22 | local world | +2 → +3 | from name | success, roll 0.027829 | success, q3 | 0.6720 / 0.2919 | yes |
| 18:11:08 | local world | +1 → +2 | stored | destroyed, roll 0.814871 | destroyed, q0 | 0.6740 / 0.2869 | yes |

Notes:

- The second attempt used the first one's successor seed, so the repeated-attempt chain is confirmed.
- Roll 0.790028 is a level loss under the code defaults (break 0.1) but a destruction with the ImpactfulSkills odds
  in force; the provider predicted the destruction.
- Forging XP after each attempt moved the odds (0.2919 → 0.2869); the prediction followed.
- The forge refuses an attempt while the main inventory has fewer free slots than the recipe's recoverable
  materials plus one (`OnCraftPressed`). That is the game's rule, not the mod's.

Still not covered: the vanilla provider without ImpactfulSkills, a level-loss outcome, PotentialReforged and
ForesightForge in game, UI scaling and the vanilla crafting layout.

## Manual validation procedure

1. Use a test character in a local world (attempts spend idols and can destroy items).
2. Open a Forge of Potential, select an item. Note the compact line (and F8 panel).
3. Upgrade. The BepInEx log gets a line like
   `[Validation] SwordIron +3->+4: predicted Success (roll 0.412653, next seed 1234, Verified, vanilla); observed
   Success -> q4 (roll 0.412653, next seed 1234, odds 0.6500/0.1000) => MATCH`.
4. `forgeoracle history` lists every attempt of the session with MATCH / MISMATCH.

Suggested coverage: 10 vanilla and 10 modded attempts, several repeated attempts at the same level with copies of
one item, at least one failure, a restart of the game between attempts, and a session on a dedicated server.

## Providers

| Provider | Audited | Validated in game |
| --- | --- | --- |
| vanilla | yes | indirectly (same roll and seed path; idol odds read directly) |
| impactfulskills (0.23.0) | yes | yes, 5/5 |
| potentialreforged (1.1.1) | yes | no |
| foresightforge (1.0.0, TierScaled) | yes | no |

Mod providers stay UNVERIFIED until they are listed in `Compatibility/LiveValidation.cs` after a validated run.
