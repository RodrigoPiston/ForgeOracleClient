## What and why

## Checks

- [ ] `dotnet test --project tests/ForgeOracle.Core.Tests -c Release` passes
- [ ] The plugin still only reads game state (no new RPCs, no writes to inventory, seeds, saves or game RNG)
- [ ] New predictions that cannot be proven exact are shown as UNVERIFIED or unavailable
- [ ] Docs updated (README, docs/COMPATIBILITY.md or docs/VALIDATION.md) when behaviour changes
