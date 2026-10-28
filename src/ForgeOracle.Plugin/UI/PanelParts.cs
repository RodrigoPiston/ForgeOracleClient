using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeOracle.Plugin.UI
{
    /// <summary>
    /// The child elements of the detail panel: header (icon, title), body, forecast and footer buttons.
    /// </summary>
    internal sealed class PanelParts
    {
        private PanelParts(Image background, Image icon, TextMeshProUGUI title, TextMeshProUGUI body,
            TextMeshProUGUI forecast, TextMeshProUGUI expand, TextMeshProUGUI mode, UITooltip? modeTooltip)
        {
            Background = background;
            Icon = icon;
            Title = title;
            Body = body;
            Forecast = forecast;
            Expand = expand;
            Mode = mode;
            ModeTooltip = modeTooltip;
        }

        public Image Background { get; }
        public Image Icon { get; }
        public TextMeshProUGUI Title { get; }
        public TextMeshProUGUI Body { get; }
        public TextMeshProUGUI Forecast { get; }
        public TextMeshProUGUI Expand { get; }
        public TextMeshProUGUI Mode { get; }
        public UITooltip? ModeTooltip { get; }

        public static PanelParts Build(RectTransform root, InventoryGui gui, Action toggleExpand, Action toggleMode)
        {
            TMP_Text style = gui.m_recipeDecription;
            Image background = UiFactory.Background(root, gui, 0.9f);
            RectTransform header = Row("Header", root, 32f);
            Image icon = UiFactory.Rect("Icon", header).gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            Fixed(icon.gameObject, 32f, 32f);
            TextMeshProUGUI title = UiFactory.Text("Title", header, style, 20f, TextAlignmentOptions.Left);
            title.raycastTarget = true;
            Flexible(title.gameObject);
            TextMeshProUGUI body = Paragraph("Body", root, style, 16f);
            TextMeshProUGUI forecast = Paragraph("Forecast", root, style, 15f);
            RectTransform footer = Row("Footer", root, 22f);
            TextMeshProUGUI expand = UiFactory.TextButton("Expand", footer, style, 15f, () => toggleExpand());
            TextMeshProUGUI mode = UiFactory.TextButton("Mode", footer, style, 15f, () => toggleMode());
            Flexible(expand.gameObject);
            Flexible(mode.gameObject);
            UITooltip? tooltip = UiFactory.Tooltip(mode.gameObject, gui, "Forge Oracle", string.Empty);
            return new PanelParts(background, icon, title, body, forecast, expand, mode, tooltip);
        }

        private static TextMeshProUGUI Paragraph(string name, Transform parent, TMP_Text style, float size)
        {
            TextMeshProUGUI text = UiFactory.Text(name, parent, style, size, TextAlignmentOptions.TopLeft);
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static RectTransform Row(string name, Transform parent, float height)
        {
            RectTransform row = UiFactory.Rect(name, parent);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            Fixed(row.gameObject, -1f, height);
            return row;
        }

        private static void Fixed(GameObject go, float width, float height)
        {
            LayoutElement element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minHeight = height;
        }

        private static void Flexible(GameObject go)
        {
            go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }
    }
}
