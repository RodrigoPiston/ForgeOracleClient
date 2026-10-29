using System;
using BepInEx.Configuration;
using UnityEngine;

namespace ForgeOracle.Plugin.Configuration
{
    internal enum LanguageOption
    {
        Auto,
        English,
        Spanish,
    }

    internal enum ForecastMode
    {
        /// <summary>The next attempts at the same target quality, by any copy of the item.</summary>
        RepeatedAttempts,

        /// <summary>The first attempt at each following quality level, assuming every step succeeds.</summary>
        Progression,
    }

    internal enum PanelSide
    {
        /// <summary>The side of the crafting panel with enough free screen space.</summary>
        Auto,
        Right,
        Left,
    }

    /// <summary>All user settings, bound to BepInEx/config/heido.valheim.forgeoracleclient.cfg.</summary>
    internal sealed class PluginConfig
    {
        private readonly ConfigFile _file;

        public PluginConfig(ConfigFile file)
        {
            _file = file;
            Enabled = file.Bind("1 - General", "Enabled", true, "Show Forge of Potential predictions.");
            Language = file.Bind("1 - General", "Language", LanguageOption.Auto,
                "Interface language. Auto follows the game language (English or Spanish, else English).");
            RefreshOnStateChanges = file.Bind("1 - General", "RefreshOnStateChanges", true,
                "Recompute automatically after an upgrade attempt. When off, the prediction is cleared after each " +
                "attempt until the selection changes or 'forgeoracle refresh' is run.");
            DebugLogging = file.Bind("1 - General", "DebugLogging", false, "Log every recomputation.");
            ToggleHotkey = file.Bind("1 - General", "ToggleHotkey", new KeyboardShortcut(KeyCode.F8),
                "Shows or hides the detailed panel while the forge is open.");
            BindDisplay(file);
            BindPanel(file);
            BindSafety(file);
            file.SettingChanged += (_, __) => Version++;
        }

        public int Version { get; private set; }

        public ConfigEntry<bool> Enabled { get; }
        public ConfigEntry<LanguageOption> Language { get; }
        public ConfigEntry<bool> RefreshOnStateChanges { get; }
        public ConfigEntry<bool> DebugLogging { get; }
        public ConfigEntry<KeyboardShortcut> ToggleHotkey { get; }

        public ConfigEntry<bool> ShowCompact { get; private set; } = null!;
        public ConfigEntry<bool> ShowConfidence { get; private set; } = null!;
        public ConfigEntry<bool> ShowExactRoll { get; private set; } = null!;
        public ConfigEntry<bool> ShowPrefabId { get; private set; } = null!;
        public ConfigEntry<bool> ShowSeedTransitions { get; private set; } = null!;
        public ConfigEntry<bool> EnableTooltips { get; private set; } = null!;
        public ConfigEntry<bool> ColorblindPalette { get; private set; } = null!;
        public ConfigEntry<bool> HideWhenUnverified { get; private set; } = null!;

        public ConfigEntry<bool> ShowDetailPanel { get; private set; } = null!;
        public ConfigEntry<bool> ExpandedLayout { get; private set; } = null!;
        public ConfigEntry<int> PredictionCount { get; private set; } = null!;
        public ConfigEntry<ForecastMode> Forecast { get; private set; } = null!;
        public ConfigEntry<float> PanelOpacity { get; private set; } = null!;
        public ConfigEntry<float> UiScale { get; private set; } = null!;
        public ConfigEntry<PanelSide> Side { get; private set; } = null!;
        public ConfigEntry<bool> Draggable { get; private set; } = null!;
        public ConfigEntry<Vector2> PanelOffset { get; private set; } = null!;
        public ConfigEntry<bool> ResetPanelPosition { get; private set; } = null!;

        public ConfigEntry<bool> ConfirmDestruction { get; private set; } = null!;
        public ConfigEntry<bool> EnableSounds { get; private set; } = null!;
        public ConfigEntry<bool> RngSelfTest { get; private set; } = null!;
        public ConfigEntry<bool> ValidationLog { get; private set; } = null!;

        public void Reload() => _file.Reload();

        public void ResetPanel()
        {
            PanelOffset.Value = Vector2.zero;
            ResetPanelPosition.Value = false;
        }

        private void BindDisplay(ConfigFile file)
        {
            const string s = "2 - Display";
            ShowCompact = file.Bind(s, "ShowCompactPrediction", true,
                "Show the one-line prediction next to the upgrade button.");
            ShowConfidence = file.Bind(s, "ShowConfidence", true, "Show VERIFIED / UNVERIFIED next to the prediction.");
            ShowExactRoll = file.Bind(s, "ShowExactRoll", false, "Show the exact roll and threshold values.");
            ShowPrefabId = file.Bind(s, "ShowInternalPrefabId", false,
                "Show the internal prefab name used by the algorithm.");
            ShowSeedTransitions = file.Bind(s, "ShowSeedTransitions", false,
                "Advanced: show seed -> next seed for each row.");
            EnableTooltips = file.Bind(s, "EnableTooltips", true, "Explain the forecast modes in tooltips.");
            ColorblindPalette = file.Bind(s, "ColorblindPalette", false,
                "Use a blue / orange palette instead of green / red.");
            HideWhenUnverified = file.Bind(s, "HideWhenUnverified", false,
                "Show PREDICTION UNAVAILABLE instead of an unverified prediction.");
        }

        private void BindPanel(ConfigFile file)
        {
            const string s = "3 - Detailed panel";
            ShowDetailPanel = file.Bind(s, "ShowDetailedPanel", false, "Show the detailed Forge Oracle panel.");
            ExpandedLayout = file.Bind(s, "ExpandedLayout", true, "Show the forecast table inside the panel.");
            PredictionCount = file.Bind(s, "PredictionCount", 5, new ConfigDescription(
                "Forecast rows (3, 5, 10 or 20 are typical).", new AcceptableValueRange<int>(1, 20)));
            Forecast = file.Bind(s, "ForecastMode", ForecastMode.RepeatedAttempts, "Which forecast table to show.");
            PanelOpacity = file.Bind(s, "PanelOpacity", 0.9f, new ConfigDescription(
                "Background opacity of the panel.", new AcceptableValueRange<float>(0.2f, 1f)));
            UiScale = file.Bind(s, "UiScale", 1f, new ConfigDescription(
                "Scale of the compact line and the panel.", new AcceptableValueRange<float>(0.5f, 2f)));
            Side = file.Bind(s, "Anchor", PanelSide.Auto, "Which side of the crafting panel the panel attaches to.");
            Draggable = file.Bind(s, "Draggable", true, "Allow dragging the panel by its title.");
            PanelOffset = file.Bind(s, "PanelOffset", Vector2.zero,
                "Panel offset from its anchor, saved after dragging.");
            ResetPanelPosition = file.Bind(s, "ResetPanelPosition", false,
                "Set to true to move the panel back to its anchor.");
        }

        private void BindSafety(ConfigFile file)
        {
            const string s = "4 - Safety and diagnostics";
            ConfirmDestruction = file.Bind(s, "ConfirmPredictedDestruction", false,
                "Ask for a second click when the next attempt is predicted to destroy the item. This only delays the " +
                "click locally; it never changes the outcome.");
            EnableSounds = file.Bind(s, "EnableSounds", false,
                "Play the UI click sound when a destruction is predicted.");
            RngSelfTest = file.Bind(s, "RngSelfTest", true,
                "At startup, compare the private RNG copy with Unity's (the Unity RNG state is saved and restored). " +
                "Predictions are only VERIFIED when this passes.");
            ValidationLog = file.Bind(s, "ValidationLog", true,
                "After each real attempt, log the predicted and the observed outcome side by side.");
        }

        public int ClampedCount => Math.Max(1, Math.Min(20, PredictionCount.Value));
    }
}
