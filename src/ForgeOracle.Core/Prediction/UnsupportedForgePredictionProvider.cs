using ForgeOracle.Core.Abstractions;

namespace ForgeOracle.Core.Prediction
{
    /// <summary>
    /// Used when the forge algorithm has been replaced or cannot be inspected. It always answers
    /// <see cref="PredictionStatus.Unknown"/>, so a wrong guess is never shown as a prediction.
    /// </summary>
    public sealed class UnsupportedForgePredictionProvider : IForgePredictionProvider
    {
        private readonly UnknownReason _reason;

        public UnsupportedForgePredictionProvider(string id, string displayName, UnknownReason reason)
        {
            Id = id;
            DisplayName = displayName;
            _reason = reason;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public bool CanPredict(ForgeContext context) => true;

        public PredictionResult Predict(ForgeContext context)
        {
            return PredictionResult.Unknown(_reason, Id, DisplayName, context.Issues);
        }
    }
}
