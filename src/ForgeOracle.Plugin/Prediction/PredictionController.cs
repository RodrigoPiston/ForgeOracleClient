using System;
using BepInEx.Logging;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Plugin.Configuration;
using ForgeOracle.Plugin.Diagnostics;
using ForgeOracle.Plugin.Game;
using ForgeOracle.Plugin.Localization;
using ForgeOracle.Plugin.UI;

namespace ForgeOracle.Plugin.Prediction
{
    /// <summary>
    /// Runs from the <c>InventoryGui.UpdateRecipe</c> postfix. Every frame it only compares a small key
    /// (selection, quality, player, generation, config and language versions); the prediction is recomputed only
    /// when that key changes. Upgrade attempts, config changes and the refresh command bump the generation.
    /// </summary>
    internal sealed class PredictionController
    {
        private readonly PluginConfig _cfg;
        private readonly Localizer _loc;
        private readonly PredictionPipeline _pipeline;
        private readonly ForgeView _view;
        private readonly ViewModelBuilder _builder;
        private readonly Func<RngSelfTestResult> _rng;
        private readonly ManualLogSource _log;
        private SelectionKey _key;
        private int _generation;
        private bool _stale;

        public PredictionController(PluginConfig cfg, Localizer loc, PredictionPipeline pipeline,
            Func<RngSelfTestResult> rng, ManualLogSource log)
        {
            _cfg = cfg;
            _loc = loc;
            _pipeline = pipeline;
            _rng = rng;
            _log = log;
            _view = new ForgeView(cfg, loc);
            _builder = new ViewModelBuilder(cfg, loc);
        }

        public PredictionResult? Current { get; private set; }

        public ForgeSelection? CurrentSelection { get; private set; }

        public PredictionPipeline Pipeline => _pipeline;

        /// <summary>Forces a recompute on the next frame. After an attempt this may mark the result stale.</summary>
        public void Invalidate(bool afterAttempt)
        {
            _generation++;
            _stale |= afterAttempt && !_cfg.RefreshOnStateChanges.Value;
        }

        public void Refresh()
        {
            _stale = false;
            _generation++;
        }

        public void OnUpdateRecipe(InventoryGui gui, Player player)
        {
            _loc.Refresh();
            if (!_cfg.Enabled.Value || ForgeSelection.CurrentUpgrader(player) == null)
            {
                Hide();
                return;
            }

            if (_cfg.ToggleHotkey.Value.IsDown())
            {
                _cfg.ShowDetailPanel.Value = !_cfg.ShowDetailPanel.Value;
            }

            var key = SelectionKey.Read(gui, player, _generation, _cfg.Version, _loc.Version);
            if (!key.Equals(_key))
            {
                _stale &= key.SameRecipe(_key);
                _key = key;
                Recompute(gui, player);
            }

            _view.Decorate(gui);
        }

        public void Hide()
        {
            _view.Hide();
            _key = default;
        }

        public void OnGuiDestroyed(InventoryGui gui) => _view.Forget(gui);

        public void Unload() => _view.Destroy();

        private void Recompute(InventoryGui gui, Player player)
        {
            SelectionState state = ForgeSelection.FromGui(gui, player, out ForgeSelection? selection);
            CurrentSelection = selection;
            if (state == SelectionState.NoSelection)
            {
                Current = null;
                _view.Hide();
                return;
            }

            ViewModel model = _stale ? _builder.Stale(selection) : BuildModel(state, selection);
            _view.Render(gui, model);
        }

        private ViewModel BuildModel(SelectionState state, ForgeSelection? selection)
        {
            PredictionResult result = state == SelectionState.Ready && selection != null
                ? Predict(selection)
                : _pipeline.Unknown(UnknownReason.NoUpgraderResource);
            Current = result;
            string? note = _pipeline.LastEnvironment?.Resolver.ForecastNoteKey;
            return _builder.Build(result, selection, note, _rng());
        }

        private PredictionResult Predict(ForgeSelection selection)
        {
            try
            {
                PredictionResult result = _pipeline.Predict(selection, _cfg.ClampedCount);
                LogResult(selection, result);
                return result;
            }
            catch (Exception ex)
            {
                _log.LogError($"Prediction failed for {selection.Recipe.m_item?.name}: {ex}");
                return _pipeline.Unknown(UnknownReason.ProviderError, ex.Message);
            }
        }

        private void LogResult(ForgeSelection selection, PredictionResult result)
        {
            if (result.Reason == UnknownReason.ProviderError)
            {
                _log.LogError($"Provider {result.ProviderId} failed: {result.Error}");
            }
            else if (_cfg.DebugLogging.Value)
            {
                _log.LogInfo($"Predicted {selection.Recipe.m_item.name} +{selection.TargetQuality}: {result.Status} " +
                    $"({result.Confidence}, {result.ProviderId}, roll {result.Next?.Roll.Roll:R})");
            }
        }
    }
}
