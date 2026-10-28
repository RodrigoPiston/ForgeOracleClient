using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ForgeOracle.Plugin.UI
{
    /// <summary>Builds UI elements that borrow the game's own fonts, materials, sprites and tooltip prefab.</summary>
    internal static class UiFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, TMP_Text style, float size,
            TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = style.font;
            text.fontSharedMaterial = style.fontSharedMaterial;
            text.color = Color.white;
            text.fontSize = size;
            text.alignment = alignment;
            text.richText = true;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A clickable text label (no sprite), styled like the game's text.</summary>
        public static TextMeshProUGUI TextButton(string name, Transform parent, TMP_Text style, float size,
            UnityAction onClick)
        {
            TextMeshProUGUI label = Text(name, parent, style, size, TextAlignmentOptions.Left);
            label.raycastTarget = true;
            Button button = label.gameObject.AddComponent<Button>();
            button.targetGraphic = label;
            button.onClick.AddListener(onClick);
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.85f, 0.5f);
            button.colors = colors;
            return label;
        }

        /// <summary>Copies the background sprite of the crafting panel, or falls back to a plain dark panel.</summary>
        public static Image Background(RectTransform target, InventoryGui gui, float opacity)
        {
            Image image = target.gameObject.AddComponent<Image>();
            Image? source = FindPanelImage(gui);
            if (source != null)
            {
                image.sprite = source.sprite;
                image.type = source.type;
                image.material = source.material;
                image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            }

            SetOpacity(image, source != null ? source.color : new Color(0.1f, 0.09f, 0.08f), opacity);
            return image;
        }

        public static void SetOpacity(Image image, Color baseColor, float opacity)
        {
            image.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(opacity));
        }

        /// <summary>Adds the game's tooltip component, reusing the craft button's tooltip prefab.</summary>
        public static UITooltip? Tooltip(GameObject target, InventoryGui gui, string topic, string text)
        {
            UITooltip? template = gui.m_craftButton.GetComponent<UITooltip>();
            if (template == null || template.m_tooltipPrefab == null)
            {
                return null;
            }

            UITooltip tooltip = target.AddComponent<UITooltip>();
            tooltip.m_tooltipPrefab = template.m_tooltipPrefab;
            tooltip.Set(topic, text);
            return tooltip;
        }

        private static Image? FindPanelImage(InventoryGui gui)
        {
            Image? own = gui.m_crafting.GetComponent<Image>();
            if (own != null && own.sprite != null)
            {
                return own;
            }

            foreach (Image image in gui.m_crafting.GetComponentsInChildren<Image>(true))
            {
                string name = image.gameObject.name.ToLowerInvariant();
                if (image.sprite != null && (name.Contains("bkg") || name.Contains("background")))
                {
                    return image;
                }
            }

            return null;
        }
    }
}
