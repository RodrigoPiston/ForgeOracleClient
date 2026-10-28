using ForgeOracle.Core.Abstractions;
using ForgeOracle.Plugin.Configuration;
using ForgeOracle.Plugin.Localization;
using UnityEngine;

namespace ForgeOracle.Plugin.UI
{
    /// <summary>
    /// Owns the compact line (a decoration of the game's description text) and the detail panel. The panel is
    /// built once per <see cref="InventoryGui"/> instance (old copies are removed by name), so reopening the forge
    /// never duplicates it.
    /// </summary>
    internal sealed class ForgeView
    {
        private readonly PluginConfig _cfg;
        private readonly Localizer _loc;
        private InventoryGui? _owner;
        private readonly CompactLine _compact = new CompactLine();
        private DetailPanel? _panel;
        private ViewModel? _model;

        public ForgeView(PluginConfig cfg, Localizer loc)
        {
            _cfg = cfg;
            _loc = loc;
        }

        public void Render(InventoryGui gui, ViewModel model)
        {
            EnsureBuilt(gui);
            PlayWarningSound(gui, model);
            _model = model;
            _compact.Apply(model, gui, _cfg.ShowCompact.Value, _cfg.UiScale.Value);
            string mode = _cfg.Forecast.Value.ToString();
            _panel!.Apply(model, _cfg, _loc.Get("panel.title"),
                _loc.Get(_cfg.ExpandedLayout.Value ? "panel.collapse" : "panel.expand"),
                _loc.Get("panel.mode"), _loc.Get("tooltip." + mode));
        }

        public void Hide()
        {
            _compact.Clear();
            _panel?.SetVisible(false);
            _model = null;
        }

        /// <summary>Every frame while the forge is open: re-adds the compact line to the description.</summary>
        public void Decorate(InventoryGui gui) => _compact.Decorate(gui);

        /// <summary>Called when the GUI is destroyed (scene change, logout): Unity destroys the children.</summary>
        public void Forget(InventoryGui gui)
        {
            if (ReferenceEquals(gui, _owner))
            {
                _owner = null;
                _compact.Clear();
                _panel = null;
                _model = null;
            }
        }

        /// <summary>Removes the elements; used when the plugin is unloaded.</summary>
        public void Destroy()
        {
            _compact.Clear();
            _panel?.Destroy();
            _owner = null;
            _panel = null;
        }

        private void EnsureBuilt(InventoryGui gui)
        {
            if (ReferenceEquals(gui, _owner) && _panel != null && _panel.Alive)
            {
                return;
            }

            Destroy();
            _owner = gui;
            _panel = DetailPanel.Create(gui, _cfg,
                () => _cfg.ExpandedLayout.Value = !_cfg.ExpandedLayout.Value,
                () => _cfg.Forecast.Value = _cfg.Forecast.Value == ForecastMode.Progression
                    ? ForecastMode.RepeatedAttempts : ForecastMode.Progression);
        }

        private void PlayWarningSound(InventoryGui gui, ViewModel model)
        {
            bool newlyDestroying = model.Status == PredictionStatus.Destruction
                && (_model == null || _model.Status != PredictionStatus.Destruction);
            if (!_cfg.EnableSounds.Value || !newlyDestroying)
            {
                return;
            }

            GameObject? sfx = gui.m_craftButton.GetComponent<ButtonSfx>()?.m_sfxPrefab;
            if (sfx != null)
            {
                Object.Instantiate(sfx);
            }
        }
    }
}
