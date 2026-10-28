using System;
using System.Runtime.CompilerServices;

namespace ForgeOracle.Plugin.Prediction
{
    /// <summary>
    /// The inputs that decide whether the shown prediction is still current. Reading it allocates nothing and
    /// touches no Unity string properties, so it is safe to build every frame.
    /// </summary>
    internal readonly struct SelectionKey : IEquatable<SelectionKey>
    {
        private readonly int _recipe;
        private readonly int _item;
        private readonly int _quality;
        private readonly int _player;
        private readonly int _generation;
        private readonly int _config;
        private readonly int _language;

        private SelectionKey(int recipe, int item, int quality, int player, int generation, int config, int language)
        {
            _recipe = recipe;
            _item = item;
            _quality = quality;
            _player = player;
            _generation = generation;
            _config = config;
            _language = language;
        }

        public static SelectionKey Read(InventoryGui gui, Player player, int generation, int config, int language)
        {
            Recipe recipe = gui.m_selectedRecipe.Recipe;
            ItemDrop.ItemData item = gui.m_selectedRecipe.ItemData;
            return new SelectionKey(
                recipe != null ? recipe.GetInstanceID() : 0,
                item != null ? RuntimeHelpers.GetHashCode(item) : 0,
                item?.m_quality ?? -1,
                player.GetInstanceID(),
                generation,
                config,
                language);
        }

        /// <summary>
        /// Same recipe and player. The item is left out on purpose: an attempt replaces the item object, and the
        /// game reselects the new one, which should not count as the player choosing something else.
        /// </summary>
        public bool SameRecipe(SelectionKey other)
        {
            return _recipe == other._recipe && _player == other._player;
        }

        public bool Equals(SelectionKey other)
        {
            return SameRecipe(other) && _item == other._item && _quality == other._quality
                && _generation == other._generation && _config == other._config && _language == other._language;
        }

        public override bool Equals(object? obj) => obj is SelectionKey other && Equals(other);

        public override int GetHashCode() => _recipe ^ (_item * 397) ^ _generation;
    }
}
