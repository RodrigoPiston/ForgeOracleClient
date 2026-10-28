using ForgeOracle.Core.Abstractions;

namespace ForgeOracle.Core.Prediction
{
    /// <summary>One predicted forge attempt.</summary>
    public sealed class AttemptForecast
    {
        public AttemptForecast(
            int index, int targetQuality, int key, SeedOrigin origin, UpgradeRoll roll, OutcomeDecision decision,
            bool conditional)
        {
            Index = index;
            TargetQuality = targetQuality;
            Key = key;
            SeedOrigin = origin;
            Roll = roll;
            Status = decision.Status;
            ResultingQuality = decision.ResultingQuality;
            Conditional = conditional;
        }

        /// <summary>0 for the real next attempt; higher rows are hypothetical.</summary>
        public int Index { get; }

        public int TargetQuality { get; }

        public int Key { get; }

        public SeedOrigin SeedOrigin { get; }

        public UpgradeRoll Roll { get; }

        public PredictionStatus Status { get; }

        /// <summary>Item quality after the attempt; 0 when the item is destroyed.</summary>
        public int ResultingQuality { get; }

        /// <summary>Only reachable if an earlier row, predicted to fail, had succeeded instead.</summary>
        public bool Conditional { get; }

        /// <summary>True for every row except the real next attempt.</summary>
        public bool Hypothetical => Index > 0;

        /// <summary>Levels lost by a level-loss outcome, relative to the quality before the attempt.</summary>
        public int LevelsLost => Status == PredictionStatus.LevelLoss ? TargetQuality - 1 - ResultingQuality : 0;
    }
}
