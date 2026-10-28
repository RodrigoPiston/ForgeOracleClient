using System;
using System.Collections.Generic;
using System.Linq;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Items;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Core.Random;
using Xunit;

namespace ForgeOracle.Core.Tests
{
    /// <summary>Compares the C# engine with values computed by the Forge Oracle reference implementation.</summary>
    public sealed class GoldenVectorTests
    {
        private static readonly GoldenVectors Vectors = GoldenVectors.Current;

        public static IEnumerable<object[]> Hashes() => Vectors.Hashes.Select(h => new object[] { h });

        public static IEnumerable<object[]> Attempts() =>
            Vectors.Attempts.Concat(Vectors.Wrapping).Select(a => new object[] { a });

        public static IEnumerable<object[]> Chains() => Vectors.Chains.Select(c => new object[] { c });

        public static IEnumerable<object[]> Boundaries() => Vectors.Boundaries.Select(b => new object[] { b });

        [Theory]
        [MemberData(nameof(Hashes))]
        public void StableHashMatchesReference(HashVector vector)
        {
            Assert.Equal(vector.Hash, ValheimHash.GetStableHashCode(vector.Text));
        }

        [Theory]
        [MemberData(nameof(Attempts))]
        public void KeyAndSeedMatchReference(AttemptVector vector)
        {
            var item = new ItemIdentity(vector.Prefab, string.Empty, 4);
            int key = UpgradeKeys.ComputeKey(item, vector.TargetLevel);
            Assert.Equal(vector.Key, key);
            int seed = vector.StoredSeed ?? UpgradeKeys.InitialSeed(vector.Character, key);
            Assert.Equal(vector.Seed, seed);
        }

        [Theory]
        [MemberData(nameof(Attempts))]
        public void RollAndSuccessorMatchReference(AttemptVector vector)
        {
            AssertDraw(vector, UpgradeRoll.FromSeed(vector.Seed));
        }

        [Theory]
        [MemberData(nameof(Attempts))]
        public void OutcomeMatchesIndependentModel(AttemptVector vector)
        {
            var roll = UpgradeRoll.FromSeed(vector.Seed);
            var rules = new UpgradeRules(Vectors.Rules.Success, Vectors.Rules.Break, 0.5f, "golden");
            OutcomeDecision decision = OutcomeRules.Decide(roll, rules, vector.TargetLevel);
            Assert.Equal(vector.ExpectedOutcome, decision.Status.ToString());
            Assert.False(vector.PrecisionSensitive, "the independent model must agree in float and double");
            Assert.Equal(vector.ReferenceClass == "S", decision.Status == PredictionStatus.Success);
        }

        [Theory]
        [MemberData(nameof(Chains))]
        public void RepeatedAttemptsFollowTheSuccessorChain(ChainVector chain)
        {
            int seed = chain.Start;
            foreach (DrawVector step in chain.Steps)
            {
                Assert.Equal(step.Seed, seed);
                var roll = UpgradeRoll.FromSeed(seed);
                AssertDraw(step, roll);
                seed = roll.NextSeed;
            }
        }

        [Theory]
        [MemberData(nameof(Boundaries))]
        public void BoundaryRollsMatchReference(BoundaryVector vector)
        {
            var roll = UpgradeRoll.FromSeed(vector.Seed);
            AssertDraw(vector, roll);
            var rules = new UpgradeRules(Vectors.Rules.Success, Vectors.Rules.Break, 0.5f, "golden");
            OutcomeDecision decision = OutcomeRules.Decide(roll, rules, 5);
            Assert.Equal(vector.Expected, decision.Status.ToString());
            Assert.False(vector.Sensitive, "the independent model must agree in float and double");
        }

        [Fact]
        public void VectorsCoverTheRequiredCases()
        {
            List<AttemptVector> attempts = Vectors.Attempts;
            Assert.Contains(attempts, a => a.Seed < 0);
            Assert.Contains(attempts, a => a.Key < 0);
            Assert.Contains(attempts, a => a.StoredSeed.HasValue);
            Assert.Contains(attempts, a => a.Character.Any(c => c > 127));
            Assert.Contains(attempts, a => a.Character.Any(char.IsSurrogate));
            Assert.Contains(attempts, a => a.Prefab.EndsWith("_TW", StringComparison.Ordinal));
            Assert.Contains(attempts, a => SeedSumWraps(a));
            Assert.Equal(3, attempts.Select(a => a.ExpectedOutcome).Distinct().Count());
            Assert.Contains(Vectors.Boundaries, b => b.RawBits23 == 0x7FFFFF);
        }

        [Fact]
        public void WrappingKeysAreCovered()
        {
            Assert.NotEmpty(Vectors.Wrapping);
            Assert.All(Vectors.Wrapping, w => Assert.True(w.Key < 0, "hash + target must wrap past int.MaxValue"));
        }

        private static bool SeedSumWraps(AttemptVector a)
        {
            long exact = (long)ValheimHash.GetStableHashCode(a.Character) + a.Key;
            return a.StoredSeed == null && (exact > int.MaxValue || exact < int.MinValue);
        }

        private static void AssertDraw(DrawVector expected, UpgradeRoll actual)
        {
            Assert.Equal(expected.RawBits23, actual.RawDraw & UnityFloat.MantissaMask);
            Assert.Equal(expected.NextSeed, actual.NextSeed);
            Assert.Equal(expected.Roll, UnityFloat.FromRawDouble(actual.RawDraw), 15);
            Assert.InRange(Math.Abs(actual.Roll - expected.Roll), 0.0, 6e-8);
        }
    }
}
