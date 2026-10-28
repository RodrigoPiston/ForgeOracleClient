using System;

namespace ForgeOracle.Core.Random
{
    /// <summary>
    /// A private copy of the xorshift128 generator behind <c>UnityEngine.Random</c>.
    /// </summary>
    /// <remarks>
    /// Predictions run on this copy so they never seed, advance or read the game's global Unity RNG.
    /// <c>Random.InitState(seed)</c> fills the state with a Mersenne-Twister style recurrence; each draw is one
    /// xorshift128 step. The plugin compares this class with the live Unity RNG at startup (and restores the Unity
    /// state afterwards); predictions are only reported as verified when that self-test passes.
    /// </remarks>
    public struct UnityXorShift128
    {
        private const uint InitMultiplier = 1812433253u;

        private uint _x;
        private uint _y;
        private uint _z;
        private uint _w;

        /// <summary>Equivalent of <c>UnityEngine.Random.InitState(seed)</c>.</summary>
        public static UnityXorShift128 FromSeed(int seed)
        {
            unchecked
            {
                var state = new UnityXorShift128 { _x = (uint)seed };
                state._y = state._x * InitMultiplier + 1u;
                state._z = state._y * InitMultiplier + 1u;
                state._w = state._z * InitMultiplier + 1u;
                return state;
            }
        }

        /// <summary>Advances the generator one step and returns the raw 32-bit output.</summary>
        public uint NextUInt()
        {
            unchecked
            {
                uint t = _x ^ (_x << 11);
                _x = _y;
                _y = _z;
                _z = _w;
                _w = _w ^ (_w >> 19) ^ t ^ (t >> 8);
                return _w;
            }
        }

        /// <summary>Equivalent of <c>UnityEngine.Random.value</c>.</summary>
        public float NextValue()
        {
            return UnityFloat.FromRaw(NextUInt());
        }

        /// <summary>
        /// Equivalent of <c>UnityEngine.Random.Range(int, int)</c> for <paramref name="min"/> &lt;= max.
        /// </summary>
        public int NextRange(int min, int max)
        {
            if (min > max)
            {
                throw new ArgumentOutOfRangeException(nameof(min), "Only ascending ranges are modelled.");
            }

            if (min == max)
            {
                return min;
            }

            unchecked
            {
                uint span = (uint)((long)max - min);
                return (int)(min + NextUInt() % span);
            }
        }
    }
}
