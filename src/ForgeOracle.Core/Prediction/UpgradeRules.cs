namespace ForgeOracle.Core.Prediction
{
    /// <summary>
    /// The decision inputs for one target quality: the idol's <c>m_upgradeChance</c>, <c>m_breakChance</c> and
    /// <c>m_breakReturnIngreientsAmount</c> (after any verified mod adjustment) and what a failure does.
    /// </summary>
    public sealed class UpgradeRules
    {
        public UpgradeRules(float successChance, float breakChance, float breakRefund, string source)
            : this(successChance, breakChance, breakRefund, source, FailureModel.Vanilla)
        {
        }

        public UpgradeRules(float successChance, float breakChance, float breakRefund, string source,
            FailureModel failure)
        {
            SuccessChance = successChance;
            BreakChance = breakChance;
            BreakRefund = breakRefund;
            Source = source ?? string.Empty;
            Failure = failure ?? FailureModel.Vanilla;
        }

        /// <summary>The attempt succeeds when <c>SuccessChance &gt;= roll</c>.</summary>
        public float SuccessChance { get; }

        /// <summary>
        /// With the vanilla failure rule a failed attempt destroys the item when <c>BreakChance &gt;= 1 - roll</c>.
        /// </summary>
        public float BreakChance { get; }

        /// <summary>Fraction of recoverable ingredients returned when the item is destroyed.</summary>
        public float BreakRefund { get; }

        /// <summary>Where the values came from, for diagnostics (for example the idol prefab name).</summary>
        public string Source { get; }

        public FailureModel Failure { get; }
    }

    public enum FailureKind
    {
        Destroyed,
        LevelLoss,
        NoChange,
    }

    /// <summary>Classifies a failed roll for forge variants that replace the vanilla break rule.</summary>
    public delegate FailureKind FailureClassifier(float roll);

    /// <summary>What happens when the roll is above the success chance.</summary>
    public sealed class FailureModel
    {
        private FailureModel(int levelsLost, int minimumQuality, FailureClassifier? classifier, string description)
        {
            LevelsLost = levelsLost;
            MinimumQuality = minimumQuality;
            Classifier = classifier;
            Description = description;
        }

        /// <summary>
        /// Vanilla 1.0.21: destroyed when <c>breakChance &gt;= 1 - roll</c>, else quality becomes target - 2.
        /// </summary>
        public static FailureModel Vanilla { get; } = new FailureModel(1, int.MinValue, null, "vanilla");

        /// <summary>Vanilla break test, with a different level loss and a quality floor.</summary>
        public static FailureModel WithLevelLoss(int levelsLost, int minimumQuality, string description)
        {
            return new FailureModel(levelsLost, minimumQuality, null, description);
        }

        /// <summary>
        /// A mod-specific classifier, with the level loss used for <see cref="FailureKind.LevelLoss"/>.
        /// </summary>
        public static FailureModel Custom(FailureClassifier classifier, int levelsLost, int minimumQuality,
            string description)
        {
            return new FailureModel(levelsLost, minimumQuality, classifier, description);
        }

        /// <summary>Levels lost relative to the current quality on a level-loss failure (vanilla: 1).</summary>
        public int LevelsLost { get; }

        /// <summary>Lowest quality a level-loss failure can produce (vanilla: no floor).</summary>
        public int MinimumQuality { get; }

        public FailureClassifier? Classifier { get; }

        public string Description { get; }

        /// <summary>Quality after a level-loss failure for an attempt at <paramref name="targetQuality"/>.</summary>
        public int QualityAfterLoss(int targetQuality)
        {
            int quality = targetQuality - 1 - LevelsLost;
            return quality < MinimumQuality ? MinimumQuality : quality;
        }
    }
}
