using System.Collections.Generic;
using ForgeOracle.Core.Abstractions;

namespace ForgeOracle.Core.Prediction
{
    /// <summary>The outcome of a prediction request, including the forecast rows and why it can be trusted.</summary>
    public sealed class PredictionResult
    {
        private static readonly IReadOnlyList<AttemptForecast> NoRows = new AttemptForecast[0];
        private static readonly IReadOnlyList<CompatibilityIssue> NoIssues = new CompatibilityIssue[0];

        private PredictionResult(
            PredictionStatus status, PredictionConfidence confidence, UnknownReason reason, string providerId,
            string providerName)
        {
            Status = status;
            Confidence = confidence;
            Reason = reason;
            ProviderId = providerId ?? string.Empty;
            ProviderName = providerName ?? string.Empty;
        }

        public PredictionStatus Status { get; }

        public PredictionConfidence Confidence { get; }

        public UnknownReason Reason { get; }

        public string ProviderId { get; }

        public string ProviderName { get; }

        /// <summary>The real next attempt, or null when unknown.</summary>
        public AttemptForecast? Next { get; private set; }

        public UpgradeRules? Rules { get; private set; }

        /// <summary>The next attempts at the same target quality, in order (row 0 is <see cref="Next"/>).</summary>
        public IReadOnlyList<AttemptForecast> RepeatedAttempts { get; private set; } = NoRows;

        /// <summary>
        /// First attempt at each of the following quality levels, assuming every earlier step succeeds.
        /// </summary>
        public IReadOnlyList<AttemptForecast> Progression { get; private set; } = NoRows;

        public IReadOnlyList<CompatibilityIssue> Issues { get; private set; } = NoIssues;

        /// <summary>Exception text when a provider failed, for diagnostics.</summary>
        public string Error { get; private set; } = string.Empty;

        public bool IsKnown => Status != PredictionStatus.Unknown;

        public static PredictionResult Unknown(
            UnknownReason reason, string providerId, string providerName,
            IReadOnlyList<CompatibilityIssue>? issues = null, string error = "")
        {
            var result = new PredictionResult(
                PredictionStatus.Unknown, PredictionConfidence.Unsupported, reason, providerId, providerName);
            result.Issues = issues ?? NoIssues;
            result.Error = error ?? string.Empty;
            return result;
        }

        public static PredictionResult Known(
            AttemptForecast next, PredictionConfidence confidence, string providerId, string providerName,
            UpgradeRules rules, PredictionRows rows, IReadOnlyList<CompatibilityIssue> issues)
        {
            var result = new PredictionResult(next.Status, confidence, UnknownReason.None, providerId, providerName)
            {
                Next = next,
                Rules = rules,
                RepeatedAttempts = rows.Repeated,
                Progression = rows.Progression,
                Issues = issues ?? NoIssues,
            };
            return result;
        }
    }

    /// <summary>The two forecast tables of a known prediction.</summary>
    public sealed class PredictionRows
    {
        public PredictionRows(IReadOnlyList<AttemptForecast> repeated, IReadOnlyList<AttemptForecast> progression)
        {
            Repeated = repeated;
            Progression = progression;
        }

        public IReadOnlyList<AttemptForecast> Repeated { get; }

        public IReadOnlyList<AttemptForecast> Progression { get; }
    }
}
