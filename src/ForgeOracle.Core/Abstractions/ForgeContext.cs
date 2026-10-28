using System;
using System.Collections.Generic;
using ForgeOracle.Core.Items;
using ForgeOracle.Core.Prediction;

namespace ForgeOracle.Core.Abstractions
{
    /// <summary>
    /// An immutable snapshot of everything one prediction needs. The plugin fills it from live game state; tests
    /// and offline tools fill it from fixtures. Nothing in it refers to Unity objects.
    /// </summary>
    public sealed class ForgeContext
    {
        private static readonly IReadOnlyList<CompatibilityIssue> NoIssues = new CompatibilityIssue[0];

        public ForgeContext(
            ItemIdentity item,
            int currentQuality,
            string playerName,
            SeedSnapshot seeds,
            IReadOnlyDictionary<int, UpgradeRules> rulesByTarget,
            RngProfile rng,
            int forecastCount,
            IReadOnlyList<CompatibilityIssue>? issues = null)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            CurrentQuality = currentQuality;
            PlayerName = playerName ?? string.Empty;
            Seeds = seeds ?? throw new ArgumentNullException(nameof(seeds));
            RulesByTarget = rulesByTarget ?? throw new ArgumentNullException(nameof(rulesByTarget));
            Rng = rng ?? throw new ArgumentNullException(nameof(rng));
            ForecastCount = Math.Max(1, forecastCount);
            Issues = issues ?? NoIssues;
        }

        public ItemIdentity Item { get; }

        public int CurrentQuality { get; }

        /// <summary>The quality the next attempt aims for: current + 1, as in <c>DoCrafting</c>.</summary>
        public int TargetQuality => CurrentQuality + 1;

        /// <summary>The character name as the game reads it (<c>Player.GetPlayerName()</c>).</summary>
        public string PlayerName { get; }

        public SeedSnapshot Seeds { get; }

        /// <summary>Effective rules per target quality, for the next attempt and the forecast levels.</summary>
        public IReadOnlyDictionary<int, UpgradeRules> RulesByTarget { get; }

        public RngProfile Rng { get; }

        /// <summary>How many forecast rows to compute (repeated attempts and progression each).</summary>
        public int ForecastCount { get; }

        /// <summary>Environment findings (mods, patches) that apply to this prediction.</summary>
        public IReadOnlyList<CompatibilityIssue> Issues { get; }

        /// <summary>Every seed-table key this context may need, for <see cref="SeedSnapshot.Capture"/>.</summary>
        public static IEnumerable<int> KeysFor(ItemIdentity item, int currentQuality, int forecastCount)
        {
            for (int i = 0; i < Math.Max(1, forecastCount); i++)
            {
                yield return UpgradeKeys.ComputeKey(item, currentQuality + 1 + i);
            }
        }
    }

    /// <summary>Whether the live self-test confirmed that the private RNG copy matches Unity's.</summary>
    public sealed class RngProfile
    {
        public RngProfile(bool verified, string description)
        {
            Verified = verified;
            Description = description ?? string.Empty;
        }

        public static RngProfile Unchecked { get; } = new RngProfile(false, "not self-tested");

        public bool Verified { get; }

        public string Description { get; }
    }
}
