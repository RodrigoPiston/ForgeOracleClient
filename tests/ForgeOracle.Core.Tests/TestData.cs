using System.Collections.Generic;
using System.Linq;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Items;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Core.Random;

namespace ForgeOracle.Core.Tests
{
    /// <summary>Builders for contexts used by the behaviour tests.</summary>
    internal static class TestData
    {
        public static readonly ItemIdentity Hammer = new ItemIdentity("Hammer", "$item_hammer", 3);

        public static UpgradeRules VanillaRules(float success = 0.65f, float brk = 0.1f) =>
            new UpgradeRules(success, brk, 0.5f, "Upgrader0Weapon");

        public static RngProfile VerifiedRng { get; } =
            new RngProfile(true, "test");

        public static ForgeContext Context(
            ItemIdentity item, int current, string player, IReadOnlyDictionary<int, int>? table = null,
            int count = 5, UpgradeRules? rules = null, RngProfile? rng = null,
            IReadOnlyList<CompatibilityIssue>? issues = null)
        {
            IEnumerable<int> keys = ForgeContext.KeysFor(item, current, count);
            SeedSnapshot seeds = SeedSnapshot.FromTable(table ?? new Dictionary<int, int>(), keys);
            UpgradeRules effective = rules ?? VanillaRules();
            Dictionary<int, UpgradeRules> byTarget = Enumerable.Range(current + 1, count)
                .ToDictionary(level => level, _ => effective);
            return new ForgeContext(item, current, player, seeds, byTarget, rng ?? VerifiedRng, count, issues);
        }

        /// <summary>Searches seeds for a first roll that satisfies <paramref name="accept"/>.</summary>
        public static int SeedWhere(System.Func<float, bool> accept)
        {
            for (int seed = 0; ; seed++)
            {
                if (accept(UpgradeRoll.FromSeed(seed).Roll))
                {
                    return seed;
                }
            }
        }
    }
}
