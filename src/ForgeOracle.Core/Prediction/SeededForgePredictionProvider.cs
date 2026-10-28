using System.Collections.Generic;
using ForgeOracle.Core.Abstractions;

namespace ForgeOracle.Core.Prediction
{
    /// <summary>
    /// Predicts the vanilla seeded roll (Valheim 1.0.21 <c>GetItemUpgradeChance</c> + <c>DoCrafting</c> branches).
    /// Mod variants that only change thresholds reuse it with their own id; the thresholds arrive in the context.
    /// </summary>
    public class SeededForgePredictionProvider : IForgePredictionProvider
    {
        public SeededForgePredictionProvider(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public static SeededForgePredictionProvider Vanilla { get; } =
            new SeededForgePredictionProvider("vanilla", "Vanilla");

        public string Id { get; }

        public string DisplayName { get; }

        public virtual bool CanPredict(ForgeContext context)
        {
            return !HasBlockingIssue(context.Issues) && context.RulesByTarget.ContainsKey(context.TargetQuality);
        }

        public PredictionResult Predict(ForgeContext context)
        {
            if (HasBlockingIssue(context.Issues))
            {
                return PredictionResult.Unknown(UnknownReason.ModifiedAlgorithm, Id, DisplayName, context.Issues);
            }

            if (!context.RulesByTarget.TryGetValue(context.TargetQuality, out UpgradeRules rules))
            {
                return PredictionResult.Unknown(UnknownReason.MissingThresholds, Id, DisplayName, context.Issues);
            }

            IReadOnlyList<AttemptForecast> repeated = ForecastBuilder.Repeated(context, rules);
            if (repeated.Count == 0)
            {
                return PredictionResult.Unknown(UnknownReason.MissingPlayerName, Id, DisplayName, context.Issues);
            }

            var rows = new PredictionRows(repeated, ForecastBuilder.Progression(context));
            AttemptForecast next = repeated[0];
            return PredictionResult.Known(
                next, Assess(context, next), Id, DisplayName, rules, rows, context.Issues);
        }

        /// <summary>Verified only with a confirmed RNG and no unresolved mod findings.</summary>
        public static PredictionConfidence Assess(ForgeContext context, AttemptForecast next)
        {
            if (!context.Rng.Verified)
            {
                return PredictionConfidence.Unverified;
            }

            foreach (CompatibilityIssue issue in context.Issues)
            {
                if (issue.Severity != IssueSeverity.Info)
                {
                    return PredictionConfidence.Unverified;
                }
            }

            return PredictionConfidence.Verified;
        }

        private static bool HasBlockingIssue(IReadOnlyList<CompatibilityIssue> issues)
        {
            foreach (CompatibilityIssue issue in issues)
            {
                if (issue.Severity == IssueSeverity.Blocking)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
