using System.Collections.Generic;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Core.Random;

namespace ForgeOracle.Plugin.Diagnostics
{
    internal sealed class RngSelfTestResult
    {
        public RngSelfTestResult(bool ran, int samples, int mismatches, string detail)
        {
            Ran = ran;
            Samples = samples;
            Mismatches = mismatches;
            Detail = detail;
        }

        public static RngSelfTestResult Skipped { get; } = new RngSelfTestResult(false, 0, 0, "disabled in config");

        public bool Ran { get; }

        public int Samples { get; }

        public int Mismatches { get; }

        public string Detail { get; }

        public bool Passed => Ran && Samples > 0 && Mismatches == 0;

        public RngProfile ToProfile() => new RngProfile(Passed, ToString());

        public override string ToString()
        {
            return Ran ? $"{Samples - Mismatches}/{Samples} seeds match Unity ({Detail})" : $"not run ({Detail})";
        }
    }

    /// <summary>
    /// Compares the private xorshift128 copy with <c>UnityEngine.Random</c> on the running Unity build:
    /// <c>InitState(seed)</c>, <c>value</c>, <c>Range(0, 99999999)</c>, exactly the calls of GetItemUpgradeChance.
    /// The global RNG state is saved first and restored in <c>finally</c>, on the main thread, before any other
    /// code can draw from it, so the game's random sequence is left exactly as it was.
    /// </summary>
    internal static class RngSelfTest
    {
        private static readonly int[] EdgeSeeds = { 0, 1, -1, int.MinValue, int.MaxValue, 99999998, 12345678 };

        public static RngSelfTestResult Run()
        {
            UnityEngine.Random.State saved = UnityEngine.Random.state;
            int mismatches = 0;
            string first = "all equal";
            List<int> seeds = Seeds();
            try
            {
                foreach (int seed in seeds)
                {
                    if (!Matches(seed, out string detail) && mismatches++ == 0)
                    {
                        first = detail;
                    }
                }
            }
            finally
            {
                UnityEngine.Random.state = saved;
            }

            return new RngSelfTestResult(true, seeds.Count, mismatches, first);
        }

        private static bool Matches(int seed, out string detail)
        {
            UnityEngine.Random.InitState(seed);
            float value = UnityEngine.Random.value;
            int next = UnityEngine.Random.Range(0, UpgradeRoll.NextSeedBound);
            UpgradeRoll mine = UpgradeRoll.FromSeed(seed);
            detail = $"seed {seed}: unity {value:R}/{next}, managed {mine.Roll:R}/{mine.NextSeed}";
            return value == mine.Roll && next == mine.NextSeed;
        }

        private static List<int> Seeds()
        {
            var seeds = new List<int>(EdgeSeeds);
            var source = UnityXorShift128.FromSeed(20261028);
            while (seeds.Count < 256)
            {
                seeds.Add(unchecked((int)source.NextUInt()));
            }

            return seeds;
        }
    }
}
