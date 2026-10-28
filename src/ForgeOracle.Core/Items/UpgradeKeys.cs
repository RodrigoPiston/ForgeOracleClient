using ForgeOracle.Core.Random;

namespace ForgeOracle.Core.Items
{
    /// <summary>Key and seed derivation used by <c>Player.GetItemUpgradeChance</c>.</summary>
    public static class UpgradeKeys
    {
        /// <summary>The seed-table key: <c>prefabName.GetStableHashCode() + targetQuality</c> (wrapping).</summary>
        public static int ComputeKey(ItemIdentity item, int targetQuality)
        {
            return unchecked(item.PrefabHash + targetQuality);
        }

        /// <summary>The seed used when the table has no entry: <c>playerName.GetStableHashCode() + key</c>.</summary>
        public static int InitialSeed(string playerName, int key)
        {
            return unchecked(ValheimHash.GetStableHashCode(playerName) + key);
        }
    }
}
