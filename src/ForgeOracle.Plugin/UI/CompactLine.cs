using TMPro;
using UnityEngine;

namespace ForgeOracle.Plugin.UI
{
    /// <summary>
    /// The one-line prediction, shown as the first line of the forge's own recipe description. Using the game's
    /// text box keeps the game font, follows any layout mod and never overlaps the upgrade button. The game rewrites
    /// the description every frame, so the line is prepended again after each <c>UpdateRecipe</c>.
    /// </summary>
    internal sealed class CompactLine
    {
        private string? _prefix;
        private GlyphSet? _glyphs;

        public void Apply(ViewModel model, InventoryGui gui, bool visible, float scale)
        {
            if (!visible)
            {
                _prefix = null;
                return;
            }

            GlyphSet glyphs = _glyphs ??= Glyphs.Detect(gui.m_recipeDecription.font);
            string glyph = Glyphs.For(model.Status, glyphs);
            int size = Mathf.RoundToInt(112f * scale);
            int noteSize = Mathf.RoundToInt(80f * scale);
            string note = model.CompactNote.Length == 0
                ? string.Empty
                : $"  <size={noteSize}%><color={Palette.Muted}>· {model.CompactNote}</color></size>";
            _prefix = $"<size={size}%><b><color={model.CompactColor}>{glyph} {model.CompactText}</color></b></size>" +
                note + "\n";
        }

        public void Clear()
        {
            _prefix = null;
        }

        /// <summary>Per frame, after the game (and other mods) wrote the description text.</summary>
        public void Decorate(InventoryGui gui)
        {
            TMP_Text description = gui.m_recipeDecription;
            if (_prefix != null && description.enabled)
            {
                description.text = _prefix + description.text;
            }
        }
    }
}
