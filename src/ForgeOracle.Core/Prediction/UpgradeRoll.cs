using ForgeOracle.Core.Random;

namespace ForgeOracle.Core.Prediction
{
    /// <summary>
    /// One simulated call of <c>Player.GetItemUpgradeChance</c>: seed in, roll and successor seed out.
    /// </summary>
    public readonly struct UpgradeRoll
    {
        /// <summary>Exclusive upper bound of <c>Random.Range(0, 99999999)</c> used for the successor seed.</summary>
        public const int NextSeedBound = 99999999;

        private UpgradeRoll(int seed, uint raw, int nextSeed)
        {
            Seed = seed;
            RawDraw = raw;
            Roll = UnityFloat.FromRaw(raw);
            NextSeed = nextSeed;
        }

        public int Seed { get; }

        /// <summary>The first raw xorshift128 output after seeding.</summary>
        public uint RawDraw { get; }

        /// <summary>The value <c>GetItemUpgradeChance</c> returns (<c>Random.value</c> after seeding).</summary>
        public float Roll { get; }

        /// <summary>The seed the game stores for the same key after this attempt.</summary>
        public int NextSeed { get; }

        public static UpgradeRoll FromSeed(int seed)
        {
            var rng = UnityXorShift128.FromSeed(seed);
            uint raw = rng.NextUInt();
            int next = rng.NextRange(0, NextSeedBound);
            return new UpgradeRoll(seed, raw, next);
        }

        /// <summary>Builds a roll from already known draws (for tests).</summary>
        public static UpgradeRoll FromDraws(int seed, uint rawDraw, int nextSeed)
        {
            return new UpgradeRoll(seed, rawDraw, nextSeed);
        }
    }
}
