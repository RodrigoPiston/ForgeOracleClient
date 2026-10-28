using ForgeOracle.Core.Abstractions;
using ForgeOracle.Plugin.Configuration;
using ForgeOracle.Plugin.Localization;
using UnityEngine;

namespace ForgeOracle.Plugin.Prediction
{
    /// <summary>Holds back the first click on an attempt predicted to destroy the item (opt-in).</summary>
    internal sealed class ConfirmationGuard
    {
        private const float WindowSeconds = 5f;
        private readonly PluginConfig _cfg;
        private readonly Localizer _loc;
        private readonly PredictionController _controller;
        private object? _armedItem;
        private float _armedAt = float.NegativeInfinity;

        public ConfirmationGuard(PluginConfig cfg, Localizer loc, PredictionController controller)
        {
            _cfg = cfg;
            _loc = loc;
            _controller = controller;
        }

        public bool ShouldBlock(InventoryGui gui)
        {
            ItemDrop.ItemData? item = gui.m_selectedRecipe.ItemData;
            if (!_cfg.ConfirmDestruction.Value || item == null || !PredictsDestruction(item))
            {
                return false;
            }

            bool confirmed = ReferenceEquals(item, _armedItem) && Time.unscaledTime - _armedAt <= WindowSeconds;
            _armedItem = confirmed ? null : item;
            _armedAt = confirmed ? float.NegativeInfinity : Time.unscaledTime;
            if (!confirmed)
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, _loc.Get("confirm.destroy"));
            }

            return !confirmed;
        }

        private bool PredictsDestruction(ItemDrop.ItemData item)
        {
            return _controller.Current != null
                && _controller.Current.Status == PredictionStatus.Destruction
                && ReferenceEquals(_controller.CurrentSelection?.Item, item);
        }
    }
}
