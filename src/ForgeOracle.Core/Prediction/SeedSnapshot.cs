using System;
using System.Collections.Generic;

namespace ForgeOracle.Core.Prediction
{
    /// <summary>
    /// An immutable copy of the seed-table entries a prediction needs, taken from the live character state.
    /// It records which keys were looked up, so "not stored" is never confused with "not captured".
    /// </summary>
    public sealed class SeedSnapshot
    {
        private readonly Dictionary<int, int> _stored;
        private readonly HashSet<int> _captured;

        private SeedSnapshot(Dictionary<int, int> stored, HashSet<int> captured)
        {
            _stored = stored;
            _captured = captured;
        }

        public static SeedSnapshot Empty { get; } = new SeedSnapshot(new Dictionary<int, int>(), new HashSet<int>());

        /// <summary>Copies the entries for <paramref name="keys"/> using a read-only lookup.</summary>
        public static SeedSnapshot Capture(IEnumerable<int> keys, TryGetSeed lookup)
        {
            if (keys == null || lookup == null)
            {
                throw new ArgumentNullException(keys == null ? nameof(keys) : nameof(lookup));
            }

            var stored = new Dictionary<int, int>();
            var captured = new HashSet<int>();
            foreach (int key in keys)
            {
                captured.Add(key);
                if (lookup(key, out int seed))
                {
                    stored[key] = seed;
                }
            }

            return new SeedSnapshot(stored, captured);
        }

        /// <summary>Builds a snapshot from a complete table, as read from a save file or a test fixture.</summary>
        public static SeedSnapshot FromTable(IReadOnlyDictionary<int, int> table, IEnumerable<int> keys)
        {
            return Capture(keys, (int key, out int seed) => table.TryGetValue(key, out seed));
        }

        public bool WasCaptured(int key) => _captured.Contains(key);

        public bool TryGetStored(int key, out int seed) => _stored.TryGetValue(key, out seed);

        public int StoredCount => _stored.Count;
    }

    public delegate bool TryGetSeed(int key, out int seed);
}
