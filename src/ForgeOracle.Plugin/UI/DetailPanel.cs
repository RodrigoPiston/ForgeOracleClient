using System;
using ForgeOracle.Plugin.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeOracle.Plugin.UI
{
    /// <summary>The collapsible Forge Oracle panel docked beside the crafting panel.</summary>
    internal sealed class DetailPanel
    {
        public const string ObjectName = "ForgeOracle_Panel";
        private const float Width = 340f;
        private const float Gap = 12f;

        private readonly RectTransform _root;
        private readonly Image _background;
        private readonly Color _backgroundColor;
        private readonly Image _icon;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _body;
        private readonly TextMeshProUGUI _forecast;
        private readonly TextMeshProUGUI _expand;
        private readonly TextMeshProUGUI _mode;
        private readonly UITooltip? _modeTooltip;
        private static readonly Vector3[] Corners = new Vector3[4];
        private PanelSide _side = PanelSide.Right;

        private DetailPanel(RectTransform root, PanelParts parts)
        {
            _root = root;
            _background = parts.Background;
            _backgroundColor = parts.Background.color;
            _icon = parts.Icon;
            _title = parts.Title;
            _body = parts.Body;
            _forecast = parts.Forecast;
            _expand = parts.Expand;
            _mode = parts.Mode;
            _modeTooltip = parts.ModeTooltip;
        }

        public bool Alive => _root != null;

        public static DetailPanel Create(InventoryGui gui, PluginConfig cfg, Action toggleExpand, Action toggleMode)
        {
            Transform? stale = gui.m_crafting.Find(ObjectName);
            if (stale != null)
            {
                UnityEngine.Object.Destroy(stale.gameObject);
            }

            RectTransform root = UiFactory.Rect(ObjectName, gui.m_crafting);
            root.sizeDelta = new Vector2(Width, 0f);
            Layout(root);
            PanelParts parts = PanelParts.Build(root, gui, toggleExpand, toggleMode);
            var panel = new DetailPanel(root, parts);
            var drag = parts.Title.gameObject.AddComponent<PanelDrag>();
            drag.Target = root;
            drag.Enabled = () => cfg.Draggable.Value;
            drag.Dropped = pos => cfg.PanelOffset.Value = pos - BasePosition(panel._side);
            return panel;
        }

        public void Apply(ViewModel model, PluginConfig cfg, string title, string expandLabel, string modeLabel,
            string modeTooltip)
        {
            _root.gameObject.SetActive(cfg.ShowDetailPanel.Value);
            if (!cfg.ShowDetailPanel.Value)
            {
                return;
            }

            Place(cfg);
            UiFactory.SetOpacity(_background, _backgroundColor, cfg.PanelOpacity.Value);
            _title.text = $"<b>{title}</b>";
            _icon.sprite = model.Icon;
            _icon.enabled = model.Icon != null;
            _body.text = model.PanelBody;
            _forecast.text = model.Forecast;
            _forecast.gameObject.SetActive(model.Forecast.Length > 0);
            _expand.text = expandLabel;
            _mode.text = modeLabel;
            _mode.gameObject.SetActive(cfg.ExpandedLayout.Value);
            _modeTooltip?.Set(title, cfg.EnableTooltips.Value ? modeTooltip : string.Empty);
        }

        public void SetVisible(bool visible)
        {
            if (Alive)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        public void Destroy()
        {
            if (Alive)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }
        }

        private void Place(PluginConfig cfg)
        {
            if (cfg.ResetPanelPosition.Value)
            {
                cfg.ResetPanel();
            }

            _side = cfg.Side.Value == PanelSide.Auto ? FreeSide(cfg.UiScale.Value) : cfg.Side.Value;
            bool right = _side == PanelSide.Right;
            _root.anchorMin = _root.anchorMax = new Vector2(right ? 1f : 0f, 1f);
            _root.pivot = new Vector2(right ? 0f : 1f, 1f);
            _root.anchoredPosition = BasePosition(_side) + cfg.PanelOffset.Value;
            _root.localScale = Vector3.one * cfg.UiScale.Value;
        }

        /// <summary>
        /// The side of the crafting panel with room for this panel on screen. Layout mods move the crafting panel
        /// (some put it against the right edge), so a fixed side can push the panel off-screen.
        /// </summary>
        private PanelSide FreeSide(float scale)
        {
            var crafting = (RectTransform)_root.parent;
            Canvas? canvas = crafting.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return PanelSide.Right;
            }

            Canvas root = canvas.rootCanvas;
            Camera? camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            crafting.GetWorldCorners(Corners);
            float left = RectTransformUtility.WorldToScreenPoint(camera, Corners[0]).x;
            float right = Screen.width - RectTransformUtility.WorldToScreenPoint(camera, Corners[2]).x;
            float needed = (Width + Gap) * scale * root.scaleFactor;
            if (right >= needed)
            {
                return PanelSide.Right;
            }

            return left >= needed || left > right ? PanelSide.Left : PanelSide.Right;
        }

        private static Vector2 BasePosition(PanelSide side) => new Vector2(side == PanelSide.Right ? Gap : -Gap, 0f);

        private static void Layout(RectTransform root)
        {
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
    }
}
