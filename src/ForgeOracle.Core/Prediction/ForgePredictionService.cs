using System;
using System.Collections.Generic;
using ForgeOracle.Core.Abstractions;

namespace ForgeOracle.Core.Prediction
{
    /// <summary>Runs the first provider that accepts a context and turns provider failures into Unknown.</summary>
    public sealed class ForgePredictionService
    {
        private readonly IReadOnlyList<IForgePredictionProvider> _providers;

        public ForgePredictionService(IReadOnlyList<IForgePredictionProvider> providers)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        }

        public IReadOnlyList<IForgePredictionProvider> Providers => _providers;

        public IForgePredictionProvider? Select(ForgeContext context)
        {
            foreach (IForgePredictionProvider provider in _providers)
            {
                if (provider.CanPredict(context))
                {
                    return provider;
                }
            }

            return null;
        }

        /// <summary>
        /// Predicts with the selected provider. A provider exception becomes an Unknown result that carries the
        /// exception text; the caller is expected to log it.
        /// </summary>
        public PredictionResult Predict(ForgeContext context)
        {
            IForgePredictionProvider? provider = Select(context);
            if (provider == null)
            {
                return PredictionResult.Unknown(UnknownReason.MissingThresholds, "none", "None", context.Issues);
            }

            try
            {
                return provider.Predict(context);
            }
            catch (Exception ex)
            {
                return PredictionResult.Unknown(
                    UnknownReason.ProviderError, provider.Id, provider.DisplayName, context.Issues, ex.ToString());
            }
        }
    }
}
