using ForgeOracle.Core.Abstractions;

namespace ForgeOracle.Plugin.UI
{
    /// <summary>Everything the view draws, already localized and colored (TMP rich text).</summary>
    internal sealed class ViewModel
    {
        public ViewModel(PredictionStatus status, string compactText, string compactColor, string panelBody,
            string forecast)
        {
            Status = status;
            CompactText = compactText;
            CompactColor = compactColor;
            PanelBody = panelBody;
            Forecast = forecast;
        }

        /// <summary>Muted note after the compact line, for example the confidence; may be empty.</summary>
        public string CompactNote { get; set; } = string.Empty;

        public PredictionStatus Status { get; }

        /// <summary>Text after the status glyph, for example "NEXT UPGRADE: SUCCESS".</summary>
        public string CompactText { get; }

        /// <summary>Hex color of the compact line.</summary>
        public string CompactColor { get; }

        public string PanelBody { get; }

        /// <summary>The forecast table, or empty when collapsed.</summary>
        public string Forecast { get; }

        public UnityEngine.Sprite? Icon { get; set; }
    }

    /// <summary>Status colors. The colorblind set avoids relying on red versus green.</summary>
    internal static class Palette
    {
        public static string For(PredictionStatus status, bool colorblind)
        {
            switch (status)
            {
                case PredictionStatus.Success:
                    return colorblind ? "#4DA6FF" : "#6BD66B";
                case PredictionStatus.Destruction:
                    return colorblind ? "#FF9900" : "#FF5C5C";
                case PredictionStatus.LevelLoss:
                    return colorblind ? "#FFE066" : "#FFB34D";
                case PredictionStatus.NoChange:
                    return "#D0D0D0";
                default:
                    return "#9A9A9A";
            }
        }

        public const string Muted = "#A8A8A8";
        public const string Warning = "#FFCC66";
    }

    internal enum GlyphSet
    {
        Symbols,
        Latin,
    }

    /// <summary>Status glyphs, so color is never the only cue; Latin-1 fallbacks when the font lacks symbols.</summary>
    internal static class Glyphs
    {
        private const string Symbols = "▲✖▼■";

        public static GlyphSet Detect(TMPro.TMP_FontAsset? font)
        {
            return font != null && font.HasCharacters(Symbols, out _, true) ? GlyphSet.Symbols : GlyphSet.Latin;
        }

        public static string For(PredictionStatus status, GlyphSet set)
        {
            bool symbols = set == GlyphSet.Symbols;
            switch (status)
            {
                case PredictionStatus.Success:
                    return symbols ? "▲" : "»";
                case PredictionStatus.Destruction:
                    return symbols ? "✖" : "×";
                case PredictionStatus.LevelLoss:
                    return symbols ? "▼" : "«";
                case PredictionStatus.NoChange:
                    return symbols ? "■" : "=";
                default:
                    return "?";
            }
        }
    }
}
