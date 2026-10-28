namespace ForgeOracle.Core.Abstractions
{
    public enum PredictionStatus
    {
        Success,
        Destruction,
        LevelLoss,
        NoChange,
        Unknown,
    }

    public enum PredictionConfidence
    {
        /// <summary>
        /// Exact algorithm, live seed state and a passing RNG self-test, with no unresolved mod changes.
        /// </summary>
        Verified,

        /// <summary>Computed, but something could make the game decide differently (see the issues).</summary>
        Unverified,

        /// <summary>No prediction is possible for this state.</summary>
        Unsupported,
    }

    /// <summary>Where the seed of an attempt came from.</summary>
    public enum SeedOrigin
    {
        /// <summary>Read from the character's live seed table.</summary>
        Stored,

        /// <summary>No table entry yet: <c>hash(player name) + key</c>, exactly as the game derives it.</summary>
        DerivedFromName,

        /// <summary>The successor of an earlier simulated attempt in the same forecast.</summary>
        Simulated,
    }

    public enum IssueSeverity
    {
        Info,

        /// <summary>The prediction is shown but cannot be called verified.</summary>
        Unverified,

        /// <summary>No prediction can be made.</summary>
        Blocking,
    }

    /// <summary>Why a prediction is <see cref="PredictionStatus.Unknown"/>. The UI localizes these codes.</summary>
    public enum UnknownReason
    {
        None,
        Disabled,
        NotAtUpgrader,
        NoSelection,
        NoUpgraderResource,
        MissingPlayerName,
        MissingThresholds,
        ModifiedAlgorithm,
        RngMismatch,
        ProviderError,
    }
}
