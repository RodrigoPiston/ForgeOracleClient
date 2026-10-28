using System;
using System.Collections.Generic;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Items;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Core.Random;
using Xunit;

namespace ForgeOracle.Core.Tests
{
    public sealed class PredictionTests
    {
        private static readonly SeededForgePredictionProvider Vanilla = SeededForgePredictionProvider.Vanilla;

        [Fact]
        public void StoredSeedTakesPrecedenceOverTheName()
        {
            int key = UpgradeKeys.ComputeKey(TestData.Hammer, 4);
            var table = new Dictionary<int, int> { [key] = 12345678 };
            PredictionResult result = Vanilla.Predict(TestData.Context(TestData.Hammer, 3, "Thor", table));
            Assert.Equal(SeedOrigin.Stored, result.Next!.SeedOrigin);
            Assert.Equal(12345678, result.Next.Roll.Seed);
        }

        [Fact]
        public void MissingSeedIsDerivedFromTheName()
        {
            PredictionResult result = Vanilla.Predict(TestData.Context(TestData.Hammer, 3, "Thor"));
            int key = UpgradeKeys.ComputeKey(TestData.Hammer, 4);
            Assert.Equal(SeedOrigin.DerivedFromName, result.Next!.SeedOrigin);
            Assert.Equal(unchecked(ValheimHash.GetStableHashCode("Thor") + key), result.Next.Roll.Seed);
        }

        [Fact]
        public void MissingNameAndSeedIsUnknown()
        {
            PredictionResult result = Vanilla.Predict(TestData.Context(TestData.Hammer, 3, string.Empty));
            Assert.Equal(PredictionStatus.Unknown, result.Status);
            Assert.Equal(UnknownReason.MissingPlayerName, result.Reason);
        }

        [Fact]
        public void RepeatedAttemptsConsumeSuccessorSeedsOfTheSameKey()
        {
            PredictionResult result = Vanilla.Predict(TestData.Context(TestData.Hammer, 3, "Thor", count: 4));
            Assert.Equal(4, result.RepeatedAttempts.Count);
            for (int i = 1; i < 4; i++)
            {
                AttemptForecast row = result.RepeatedAttempts[i];
                Assert.Equal(result.RepeatedAttempts[i - 1].Roll.NextSeed, row.Roll.Seed);
                Assert.Equal(4, row.TargetQuality);
                Assert.Equal(SeedOrigin.Simulated, row.SeedOrigin);
                Assert.True(row.Hypothetical);
            }
        }

        [Fact]
        public void ProgressionUsesAnIndependentKeyPerLevel()
        {
            PredictionResult result = Vanilla.Predict(TestData.Context(TestData.Hammer, 1, "Thor", count: 5));
            Assert.Equal(5, result.Progression.Count);
            for (int i = 0; i < 5; i++)
            {
                AttemptForecast row = result.Progression[i];
                Assert.Equal(2 + i, row.TargetQuality);
                Assert.Equal(UpgradeKeys.ComputeKey(TestData.Hammer, 2 + i), row.Key);
            }
        }

        [Fact]
        public void ProgressionAfterAPredictedFailureIsConditional()
        {
            PredictionResult result = Vanilla.Predict(TestData.Context(TestData.Hammer, 1, "Thor", count: 20));
            int firstFailure = FindIndex(result.Progression, r => r.Status != PredictionStatus.Success);
            Assert.True(firstFailure >= 0, "expected at least one failure in 20 levels");
            for (int i = 0; i < result.Progression.Count; i++)
            {
                Assert.Equal(i > firstFailure, result.Progression[i].Conditional);
            }
        }

        [Fact]
        public void TwoCopiesOfTheSamePrefabShareASequence()
        {
            var first = new ItemIdentity("Hammer", "$item_hammer", 3);
            var second = new ItemIdentity("Hammer", "$item_renamed_hammer", 5);
            PredictionResult a = Vanilla.Predict(TestData.Context(first, 2, "Thor"));
            PredictionResult b = Vanilla.Predict(TestData.Context(second, 2, "Thor"));
            Assert.Equal(first, second);
            Assert.Equal(a.Next!.Roll.Seed, b.Next!.Roll.Seed);
        }

        [Fact]
        public void DifferentPrefabsWithTheSameDisplayNameDoNot()
        {
            var vanilla = new ItemIdentity("SwordIron", "$item_sword_iron", 4);
            var modded = new ItemIdentity("SwordIron_TW", "$item_sword_iron", 4);
            PredictionResult a = Vanilla.Predict(TestData.Context(vanilla, 2, "Thor"));
            PredictionResult b = Vanilla.Predict(TestData.Context(modded, 2, "Thor"));
            Assert.NotEqual(a.Next!.Key, b.Next!.Key);
        }

        [Fact]
        public void ConfirmedRngWithoutIssuesIsVerified()
        {
            PredictionResult result = Vanilla.Predict(TestData.Context(TestData.Hammer, 3, "Thor"));
            Assert.True(result.IsKnown);
            Assert.Equal(PredictionConfidence.Verified, result.Confidence);
        }

        [Fact]
        public void UnconfirmedRngIsUnverified()
        {
            ForgeContext context = TestData.Context(TestData.Hammer, 3, "Thor", rng: RngProfile.Unchecked);
            Assert.Equal(PredictionConfidence.Unverified, Vanilla.Predict(context).Confidence);
        }

        [Fact]
        public void UnverifiedIssueDowngradesConfidence()
        {
            var issue = new CompatibilityIssue("unknown-forge-patch", IssueSeverity.Unverified, "some.mod", "prefix");
            ForgeContext context = TestData.Context(TestData.Hammer, 3, "Thor", issues: new[] { issue });
            PredictionResult result = Vanilla.Predict(context);
            Assert.True(result.IsKnown);
            Assert.Equal(PredictionConfidence.Unverified, result.Confidence);
        }

        [Fact]
        public void BlockingIssueNeverProducesAPrediction()
        {
            var issue = new CompatibilityIssue("foreign-roll-patch", IssueSeverity.Blocking, "some.mod", "prefix");
            ForgeContext context = TestData.Context(TestData.Hammer, 3, "Thor", issues: new[] { issue });
            var service = new ForgePredictionService(new IForgePredictionProvider[]
            {
                Vanilla,
                new UnsupportedForgePredictionProvider("unsupported", "Unsupported", UnknownReason.ModifiedAlgorithm),
            });
            PredictionResult result = service.Predict(context);
            Assert.Equal(PredictionStatus.Unknown, result.Status);
            Assert.Equal(UnknownReason.ModifiedAlgorithm, result.Reason);
            Assert.Equal(PredictionConfidence.Unsupported, result.Confidence);
        }

        [Fact]
        public void ProviderExceptionsBecomeUnknownWithTheError()
        {
            var service = new ForgePredictionService(new IForgePredictionProvider[] { new ThrowingProvider() });
            PredictionResult result = service.Predict(TestData.Context(TestData.Hammer, 3, "Thor"));
            Assert.Equal(UnknownReason.ProviderError, result.Reason);
            Assert.Contains("boom", result.Error, StringComparison.Ordinal);
        }

        [Fact]
        public void UncapturedKeysAreNotGuessed()
        {
            var seeds = SeedSnapshot.FromTable(new Dictionary<int, int>(), new int[0]);
            var rules = new Dictionary<int, UpgradeRules> { [4] = TestData.VanillaRules() };
            var context = new ForgeContext(TestData.Hammer, 3, "Thor", seeds, rules, TestData.VerifiedRng, 1);
            Assert.False(ForecastBuilder.TryResolveSeed(context, 4, out _, out _, out _));
        }

        private static int FindIndex(IReadOnlyList<AttemptForecast> rows, Predicate<AttemptForecast> match)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (match(rows[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private sealed class ThrowingProvider : IForgePredictionProvider
        {
            public string Id => "throwing";

            public string DisplayName => "Throwing";

            public bool CanPredict(ForgeContext context) => true;

            public PredictionResult Predict(ForgeContext context) => throw new InvalidOperationException("boom");
        }
    }
}
