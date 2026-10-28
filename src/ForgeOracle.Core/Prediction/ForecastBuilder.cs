using System.Collections.Generic;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Items;

namespace ForgeOracle.Core.Prediction
{
    /// <summary>Builds attempt rows by replaying <c>Player.GetItemUpgradeChance</c> on the private RNG copy.</summary>
    public static class ForecastBuilder
    {
        /// <summary>
        /// Resolves the seed the game would use for the first attempt at <paramref name="targetQuality"/>:
        /// the stored table entry, else <c>hash(player name) + key</c>.
        /// </summary>
        public static bool TryResolveSeed(ForgeContext context, int targetQuality, out int key, out int seed,
            out SeedOrigin origin)
        {
            key = UpgradeKeys.ComputeKey(context.Item, targetQuality);
            origin = SeedOrigin.Stored;
            seed = 0;
            if (!context.Seeds.WasCaptured(key))
            {
                return false;
            }

            if (context.Seeds.TryGetStored(key, out seed))
            {
                return true;
            }

            origin = SeedOrigin.DerivedFromName;
            if (context.PlayerName.Length == 0)
            {
                return false;
            }

            seed = UpgradeKeys.InitialSeed(context.PlayerName, key);
            return true;
        }

        /// <summary>
        /// The next <see cref="ForgeContext.ForecastCount"/> attempts at the current target quality, by any copy of
        /// this item. Each attempt consumes the previous successor seed, exactly as the game advances the table.
        /// </summary>
        public static IReadOnlyList<AttemptForecast> Repeated(ForgeContext context, UpgradeRules rules)
        {
            var rows = new List<AttemptForecast>();
            int target = context.TargetQuality;
            if (!TryResolveSeed(context, target, out int key, out int seed, out SeedOrigin origin))
            {
                return rows;
            }

            for (int i = 0; i < context.ForecastCount; i++)
            {
                var roll = UpgradeRoll.FromSeed(seed);
                var decision = OutcomeRules.Decide(roll, rules, target);
                rows.Add(new AttemptForecast(i, target, key, origin, roll, decision, conditional: false));
                seed = roll.NextSeed;
                origin = SeedOrigin.Simulated;
            }

            return rows;
        }

        /// <summary>
        /// The first attempt at each following quality level, assuming every earlier step succeeds. Rows after a
        /// predicted failure are marked conditional, because the item would not reach them.
        /// </summary>
        public static IReadOnlyList<AttemptForecast> Progression(ForgeContext context)
        {
            var rows = new List<AttemptForecast>();
            bool blocked = false;
            for (int i = 0; i < context.ForecastCount; i++)
            {
                int target = context.TargetQuality + i;
                if (!context.RulesByTarget.TryGetValue(target, out UpgradeRules rules)
                    || !TryResolveSeed(context, target, out int key, out int seed, out SeedOrigin origin))
                {
                    break;
                }

                var roll = UpgradeRoll.FromSeed(seed);
                var decision = OutcomeRules.Decide(roll, rules, target);
                rows.Add(new AttemptForecast(i, target, key, origin, roll, decision, blocked));
                blocked |= decision.Status != PredictionStatus.Success;
            }

            return rows;
        }
    }
}
