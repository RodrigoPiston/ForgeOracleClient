# Changelog

## 1.0.0

- Predicts the next Forge of Potential attempt (success, level loss, destruction, no change) as the first line of
  the forge description, from the live character seed table and the live idol odds.
- Detailed panel with roll, thresholds, confidence, provider and a forecast of up to 20 rows in two views (next
  attempts at the same level, level by level).
- Private copy of Unity's xorshift128 RNG, checked against the real Unity RNG at startup (state saved and restored).
- Providers for vanilla, ImpactfulSkills, PotentialReforged and ForesightForge (TierScaled); ReforgedPotential and
  unknown patches on the roll are reported as unsupported.
- Harmony patch scan of the forge methods with VERIFIED / UNVERIFIED / UNAVAILABLE classification.
- English and Spanish; colorblind palette; draggable, scalable panel that docks on the free side; F8 toggle.
- Validation log comparing each real attempt with its prediction; `forgeoracle` console commands.
- Optional confirmation click when a destruction is predicted (off by default).
