using ForgeOracle.Core.Abstractions;

namespace ForgeOracle.Core.Prediction
{
    /// <summary>The branch logic of <c>InventoryGui.DoCrafting</c> at an upgrader station (Valheim 1.0.21).</summary>
    public static class OutcomeRules
    {
        /// <summary>Decides the outcome of one roll at <paramref name="targetQuality"/>.</summary>
        /// <remarks>
        /// The game evaluates <c>m_breakChance &gt;= 1f - roll</c> without an explicit float cast, so a runtime may
        /// keep <c>1 - roll</c> in higher precision. That never changes the branch: for r &gt;= 0.5 the subtraction is
        /// exact, and for every smaller 23-bit roll the float result rounds up to the nearest float, so no float
        /// threshold lies between the two values. The unit tests check this for all 2^23 rolls.
        /// </remarks>
        public static OutcomeDecision Decide(UpgradeRoll roll, UpgradeRules rules, int targetQuality)
        {
            return ToDecision(Classify(roll.Roll, rules), rules.Failure, targetQuality);
        }

        /// <summary>Returns null for success, otherwise the failure kind.</summary>
        private static FailureKind? Classify(float roll, UpgradeRules rules)
        {
            if (rules.SuccessChance >= roll)
            {
                return null;
            }

            if (rules.Failure.Classifier != null)
            {
                return rules.Failure.Classifier(roll);
            }

            return rules.BreakChance >= (float)(1f - roll) ? FailureKind.Destroyed : FailureKind.LevelLoss;
        }

        private static OutcomeDecision ToDecision(FailureKind? kind, FailureModel failure, int target)
        {
            int current = target - 1;
            if (kind == null)
            {
                return new OutcomeDecision(PredictionStatus.Success, target);
            }

            if (kind == FailureKind.Destroyed)
            {
                return new OutcomeDecision(PredictionStatus.Destruction, 0);
            }

            int quality = kind == FailureKind.NoChange ? current : failure.QualityAfterLoss(target);
            var status = quality == current ? PredictionStatus.NoChange : PredictionStatus.LevelLoss;
            return new OutcomeDecision(status, quality);
        }
    }

    public readonly struct OutcomeDecision
    {
        public OutcomeDecision(PredictionStatus status, int resultingQuality)
        {
            Status = status;
            ResultingQuality = resultingQuality;
        }

        public PredictionStatus Status { get; }

        /// <summary>Item quality after the attempt; 0 when the item is destroyed.</summary>
        public int ResultingQuality { get; }
    }
}
