using ForgeOracle.Core.Items;

namespace ForgeOracle.Plugin.Game
{
    internal enum SelectionState
    {
        /// <summary>Not at a Forge of Potential (any station without <c>m_upgrader</c>).</summary>
        NotAtUpgrader,

        /// <summary>At the forge, but no item to upgrade is selected.</summary>
        NoSelection,

        /// <summary>The recipe has no requirement flagged <c>m_upgraderResource</c>.</summary>
        NoUpgraderResource,

        Ready,
    }

    /// <summary>
    /// What the forge would upgrade, read with the same expressions <c>InventoryGui.DoCrafting</c> uses. Nothing
    /// here writes to the game.
    /// </summary>
    internal sealed class ForgeSelection
    {
        private ForgeSelection(Player player, CraftingStation station, Recipe recipe, ItemDrop.ItemData item,
            ItemDrop? idol)
        {
            Player = player;
            Station = station;
            Recipe = recipe;
            Item = item;
            Idol = idol;
        }

        public Player Player { get; }

        public CraftingStation Station { get; }

        public Recipe Recipe { get; }

        /// <summary>The inventory item being upgraded (its quality decides the target).</summary>
        public ItemDrop.ItemData Item { get; }

        /// <summary>The upgrader resource; its shared data holds the vanilla chances.</summary>
        public ItemDrop? Idol { get; }

        public int CurrentQuality => Item.m_quality;

        public int TargetQuality => Item.m_quality + 1;

        /// <summary>The station the player is using, or null when it is not a Forge of Potential.</summary>
        public static CraftingStation? CurrentUpgrader(Player player)
        {
            CraftingStation station = player.GetCurrentCraftingStation();
            return station != null && station.m_upgrader ? station : null;
        }

        /// <summary>Reads the recipe and item selected in the forge UI.</summary>
        public static SelectionState FromGui(InventoryGui gui, Player player, out ForgeSelection? selection)
        {
            return From(player, gui.m_selectedRecipe.Recipe, gui.m_selectedRecipe.ItemData, out selection);
        }

        /// <summary>Reads the recipe and item the game captured when the upgrade button was pressed.</summary>
        public static SelectionState FromCraft(InventoryGui gui, Player player, out ForgeSelection? selection)
        {
            return From(player, gui.m_craftRecipe, gui.m_craftUpgradeItem, out selection);
        }

        public ItemIdentity Identity()
        {
            ItemDrop.ItemData.SharedData shared = Recipe.m_item.m_itemData.m_shared;
            return new ItemIdentity(Recipe.m_item.gameObject.name, shared.m_name, shared.m_maxQuality);
        }

        private static SelectionState From(Player player, Recipe? recipe, ItemDrop.ItemData? item,
            out ForgeSelection? selection)
        {
            selection = null;
            CraftingStation? station = CurrentUpgrader(player);
            if (station == null)
            {
                return SelectionState.NotAtUpgrader;
            }

            if (recipe == null || item == null || recipe.m_item == null)
            {
                return SelectionState.NoSelection;
            }

            ItemDrop? idol = FindIdol(recipe);
            selection = new ForgeSelection(player, station, recipe, item, idol);
            return idol == null ? SelectionState.NoUpgraderResource : SelectionState.Ready;
        }

        /// <summary>The first requirement with <c>m_upgraderResource</c>, as in DoCrafting.</summary>
        private static ItemDrop? FindIdol(Recipe recipe)
        {
            if (recipe.m_resources == null)
            {
                return null;
            }

            foreach (Piece.Requirement requirement in recipe.m_resources)
            {
                if (requirement.m_upgraderResource)
                {
                    return requirement.m_resItem;
                }
            }

            return null;
        }
    }
}
