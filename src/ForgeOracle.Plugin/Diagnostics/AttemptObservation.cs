using System;
using System.Globalization;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Prediction;

namespace ForgeOracle.Plugin.Diagnostics
{
    /// <summary>One real forge attempt: what was predicted beforehand and what the game actually did.</summary>
    internal sealed class AttemptObservation
    {
        public AttemptObservation(string prefab, int currentQuality, PredictionResult predicted)
        {
            Time = DateTime.Now;
            Prefab = prefab;
            CurrentQuality = currentQuality;
            Predicted = predicted;
        }

        public DateTime Time { get; }

        public string Prefab { get; }

        public int CurrentQuality { get; }

        public PredictionResult Predicted { get; }

        /// <summary>Value returned by the game's <c>GetItemUpgradeChance</c>; null if no roll happened.</summary>
        public float? ActualRoll { get; set; }

        public int? ActualNextSeed { get; set; }

        /// <summary>Idol odds as the game saw them at roll time (after any mod override).</summary>
        public float SuccessAtRoll { get; set; }

        public float BreakAtRoll { get; set; }

        public PredictionStatus Observed { get; set; } = PredictionStatus.Unknown;

        public int ObservedQuality { get; set; }

        public bool RollMatches => ActualRoll.HasValue && Predicted.Next != null
            && Predicted.Next.Roll.Roll == ActualRoll;

        public bool SeedMatches => ActualNextSeed.HasValue && Predicted.Next != null
            && Predicted.Next.Roll.NextSeed == ActualNextSeed;

        public bool OutcomeMatches => Predicted.IsKnown && Predicted.Status == Observed;

        public bool Matches => RollMatches && SeedMatches && OutcomeMatches;

        public override string ToString()
        {
            var c = CultureInfo.InvariantCulture;
            string predictedRoll = Predicted.Next?.Roll.Roll.ToString("0.000000", c) ?? "-";
            string predictedSeed = Predicted.Next?.Roll.NextSeed.ToString(c) ?? "-";
            string verdict = !Predicted.IsKnown ? "NOT PREDICTED" : Matches ? "MATCH" : "MISMATCH";
            return $"{Time:HH:mm:ss} {Prefab} +{CurrentQuality}->+{CurrentQuality + 1}: " +
                $"predicted {Predicted.Status} (roll {predictedRoll}, next seed {predictedSeed}, " +
                $"{Predicted.Confidence}, {Predicted.ProviderId}); observed {Observed} -> q{ObservedQuality} " +
                $"(roll {ActualRoll?.ToString("0.000000", c) ?? "-"}, " +
                $"next seed {ActualNextSeed?.ToString(c) ?? "-"}, " +
                $"odds {SuccessAtRoll.ToString("0.0000", c)}/{BreakAtRoll.ToString("0.0000", c)}) => {verdict}";
        }
    }
}
