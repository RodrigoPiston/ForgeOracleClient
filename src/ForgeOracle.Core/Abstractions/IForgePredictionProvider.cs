using ForgeOracle.Core.Prediction;

namespace ForgeOracle.Core.Abstractions
{
    /// <summary>
    /// Predicts forge outcomes for one forge implementation (vanilla, or a verified mod variant).
    /// Implementations must be pure: they read the snapshot and never touch game state.
    /// </summary>
    public interface IForgePredictionProvider
    {
        /// <summary>Stable id shown in diagnostics, for example <c>vanilla</c>.</summary>
        string Id { get; }

        string DisplayName { get; }

        bool CanPredict(ForgeContext context);

        PredictionResult Predict(ForgeContext context);
    }
}
