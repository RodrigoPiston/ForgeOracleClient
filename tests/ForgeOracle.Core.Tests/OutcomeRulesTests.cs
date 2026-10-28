using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Core.Random;
using Xunit;

namespace ForgeOracle.Core.Tests
{
    /// <summary>The DoCrafting branches (InventoryGui.cs, Valheim 1.0.21) and the mod failure models.</summary>
    public sealed class OutcomeRulesTests
    {
        [Theory]
        [InlineData(0.30f, PredictionStatus.Success, 6)]
        [InlineData(0.80f, PredictionStatus.LevelLoss, 4)]
        [InlineData(0.95f, PredictionStatus.Destruction, 0)]
        public void VanillaBranches(float target, PredictionStatus expected, int quality)
        {
            int seed = TestData.SeedWhere(r => System.Math.Abs(r - target) < 0.01f);
            var roll = UpgradeRoll.FromSeed(seed);
            OutcomeDecision decision = OutcomeRules.Decide(roll, TestData.VanillaRules(), 6);
            Assert.Equal(expected, decision.Status);
            Assert.Equal(quality, decision.ResultingQuality);
        }

        [Fact]
        public void BreakChanceOfOneDestroysOnEveryFailure()
        {
            int seed = TestData.SeedWhere(r => r > 0.66f && r < 0.7f);
            var roll = UpgradeRoll.FromSeed(seed);
            OutcomeDecision decision = OutcomeRules.Decide(roll, TestData.VanillaRules(brk: 1f), 3);
            Assert.Equal(PredictionStatus.Destruction, decision.Status);
        }

        [Fact]
        public void VanillaLevelLossFromQualityOneHasNoFloor()
        {
            int seed = TestData.SeedWhere(r => r > 0.7f && r < 0.85f);
            var roll = UpgradeRoll.FromSeed(seed);
            OutcomeDecision decision = OutcomeRules.Decide(roll, TestData.VanillaRules(), 2);
            Assert.Equal(PredictionStatus.LevelLoss, decision.Status);
            Assert.Equal(0, decision.ResultingQuality);
        }

        [Fact]
        public void LevelLossModelWithZeroLevelsIsNoChange()
        {
            int seed = TestData.SeedWhere(r => r > 0.7f && r < 0.85f);
            var roll = UpgradeRoll.FromSeed(seed);
            var rules = new UpgradeRules(0.65f, -1f, 0.5f, "pr", FailureModel.WithLevelLoss(0, 1, "no reduce"));
            OutcomeDecision decision = OutcomeRules.Decide(roll, rules, 5);
            Assert.Equal(PredictionStatus.NoChange, decision.Status);
            Assert.Equal(4, decision.ResultingQuality);
        }

        [Fact]
        public void LevelLossModelRespectsQualityFloor()
        {
            int seed = TestData.SeedWhere(r => r > 0.7f && r < 0.85f);
            var roll = UpgradeRoll.FromSeed(seed);
            var rules = new UpgradeRules(0.65f, -1f, 0.5f, "pr", FailureModel.WithLevelLoss(3, 1, "lose 3"));
            OutcomeDecision decision = OutcomeRules.Decide(roll, rules, 3);
            Assert.Equal(PredictionStatus.LevelLoss, decision.Status);
            Assert.Equal(1, decision.ResultingQuality);
        }

        [Theory]
        [InlineData(FailureKind.NoChange, PredictionStatus.NoChange, 4)]
        [InlineData(FailureKind.LevelLoss, PredictionStatus.LevelLoss, 3)]
        [InlineData(FailureKind.Destroyed, PredictionStatus.Destruction, 0)]
        public void CustomClassifierDecidesFailures(FailureKind kind, PredictionStatus expected, int quality)
        {
            int seed = TestData.SeedWhere(r => r > 0.7f);
            var roll = UpgradeRoll.FromSeed(seed);
            var model = FailureModel.Custom(_ => kind, 1, 1, "custom");
            var rules = new UpgradeRules(0.65f, 0f, 0.5f, "ff", model);
            OutcomeDecision decision = OutcomeRules.Decide(roll, rules, 5);
            Assert.Equal(expected, decision.Status);
            Assert.Equal(quality, decision.ResultingQuality);
        }

        [Fact]
        public void BreakComplementNeverRoundsDownForAnyDraw()
        {
            // With round-to-nearest, a float complement that never rounds down leaves no float threshold between
            // (float)(1f - r) and the exact 1 - r, so float and double evaluation pick the same branch.
            for (uint raw = 0; raw <= UnityFloat.MantissaMask; raw++)
            {
                float r = UnityFloat.FromRaw(raw);
                Assert.True((float)(1f - r) >= 1.0 - r);
            }
        }
    }
}
