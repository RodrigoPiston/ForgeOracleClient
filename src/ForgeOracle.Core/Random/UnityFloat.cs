namespace ForgeOracle.Core.Random
{
    /// <summary>
    /// How an xorshift128 draw becomes <c>UnityEngine.Random.value</c>: the low 23 bits over 8388607.
    /// </summary>
    /// <remarks>
    /// <c>k * (1f / 8388607f)</c> and <c>k / 8388607f</c> round to the same float for every 23-bit k (checked
    /// exhaustively by the unit tests), so the exact way Unity writes the division does not matter. The plugin's
    /// startup self-test still compares the whole generator with the live Unity RNG.
    /// </remarks>
    public static class UnityFloat
    {
        public const uint MantissaMask = 0x007FFFFFu;
        public const float Divisor = 8388607f;
        private const float Reciprocal = 1f / Divisor;

        /// <summary>Converts a raw draw to the unit float. Explicit casts force single-precision rounding.</summary>
        public static float FromRaw(uint raw)
        {
            return (float)((float)(raw & MantissaMask) * Reciprocal);
        }

        /// <summary>The same value in double precision, as the reference tools compute it.</summary>
        public static double FromRawDouble(uint raw)
        {
            return (raw & MantissaMask) / 8388607.0;
        }
    }
}
