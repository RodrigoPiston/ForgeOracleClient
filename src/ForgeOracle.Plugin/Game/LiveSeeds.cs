using System.Collections.Generic;
using ForgeOracle.Core.Items;
using ForgeOracle.Core.Prediction;

namespace ForgeOracle.Plugin.Game
{
    /// <summary>
    /// Read-only access to the live per-character seed table (<c>Player.m_itemUpgradeSeeds</c>). The table is only
    /// ever read with <c>TryGetValue</c>; it is never written, cleared or enumerated for output.
    /// </summary>
    internal static class LiveSeeds
    {
        public static bool TryGet(Player player, int key, out int seed)
        {
            Dictionary<int, int>? table = player.m_itemUpgradeSeeds;
            if (table == null)
            {
                seed = 0;
                return false;
            }

            return table.TryGetValue(key, out seed);
        }

        public static int Count(Player player) => player.m_itemUpgradeSeeds?.Count ?? 0;

        /// <summary>Copies the entries for the target level and the forecast levels.</summary>
        public static SeedSnapshot Snapshot(Player player, ItemIdentity item, int currentQuality, int count)
        {
            IEnumerable<int> keys = Core.Abstractions.ForgeContext.KeysFor(item, currentQuality, count);
            return SeedSnapshot.Capture(keys, (int key, out int seed) => TryGet(player, key, out seed));
        }

        /// <summary>
        /// The seed the game will use for <paramref name="key"/>, or null without table entry or name.
        /// </summary>
        public static int? EffectiveSeed(Player player, int key, out bool stored)
        {
            stored = TryGet(player, key, out int seed);
            if (stored)
            {
                return seed;
            }

            string name = player.GetPlayerName();
            return name.Length == 0 ? (int?)null : UpgradeKeys.InitialSeed(name, key);
        }
    }
}
