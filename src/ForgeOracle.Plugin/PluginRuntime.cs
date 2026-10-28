using BepInEx.Logging;
using ForgeOracle.Plugin.Configuration;
using ForgeOracle.Plugin.Diagnostics;
using ForgeOracle.Plugin.Localization;
using ForgeOracle.Plugin.Prediction;

namespace ForgeOracle.Plugin
{
    /// <summary>The plugin's live components, created once in Awake and reached by the Harmony patches.</summary>
    internal sealed class PluginRuntime
    {
        public PluginRuntime(PluginConfig config, RngSelfTestResult rng, string harmonyId, ManualLogSource log)
        {
            Config = config;
            Rng = rng;
            Localizer = new Localizer(config);
            Pipeline = new PredictionPipeline(harmonyId, () => Rng);
            Controller = new PredictionController(config, Localizer, Pipeline, () => Rng, log);
            Confirmation = new ConfirmationGuard(config, Localizer, Controller);
            Recorder = new AttemptRecorder(log);
        }

        public PluginConfig Config { get; }

        public RngSelfTestResult Rng { get; set; }

        public Localizer Localizer { get; }

        public PredictionPipeline Pipeline { get; }

        public PredictionController Controller { get; }

        public ConfirmationGuard Confirmation { get; }

        public AttemptRecorder Recorder { get; }

        public void BeginAttempt(InventoryGui gui, Player player)
        {
            if (Config.ValidationLog.Value)
            {
                Recorder.Begin(gui, player, Pipeline);
            }
        }

        public void ObserveRoll(Player player, int hash, float roll)
        {
            Recorder.OnRoll(player, hash, roll);
            Controller.Invalidate(afterAttempt: true);
        }

        public void EndAttempt(Player player)
        {
            Recorder.End(player);
            Controller.Invalidate(afterAttempt: true);
        }
    }
}
