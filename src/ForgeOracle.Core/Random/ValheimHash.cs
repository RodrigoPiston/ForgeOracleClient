namespace ForgeOracle.Core.Random
{
    /// <summary>
    /// Valheim's deterministic string hash (<c>StringExtensionMethods.GetStableHashCode</c> in assembly_utils).
    /// </summary>
    /// <remarks>
    /// Ported from the decompiled game code of Valheim 1.0.21. It walks UTF-16 code units two at a time, keeps two
    /// djb2-style accumulators, stops at the first NUL character, and relies on wrapping 32-bit arithmetic.
    /// </remarks>
    public static class ValheimHash
    {
        private const int Seed = 5381;
        private const int Mix = 1566083941;

        public static int GetStableHashCode(string text)
        {
            if (text == null)
            {
                throw new System.ArgumentNullException(nameof(text));
            }

            unchecked
            {
                int even = Seed;
                int odd = Seed;
                for (int i = 0; i < text.Length && text[i] != '\0'; i += 2)
                {
                    even = ((even << 5) + even) ^ text[i];
                    if (i == text.Length - 1 || text[i + 1] == '\0')
                    {
                        break;
                    }

                    odd = ((odd << 5) + odd) ^ text[i + 1];
                }

                return even + odd * Mix;
            }
        }
    }
}
