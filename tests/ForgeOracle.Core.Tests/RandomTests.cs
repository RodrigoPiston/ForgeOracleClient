using System;
using ForgeOracle.Core.Random;
using Xunit;

namespace ForgeOracle.Core.Tests
{
    public sealed class RandomTests
    {
        [Fact]
        public void HashStopsAtTheFirstNulCharacterLikeTheGame()
        {
            // StringExtensionMethods.GetStableHashCode loops while str[i] != '\0' and breaks on str[i + 1] == '\0'.
            Assert.Equal(ValheimHash.GetStableHashCode("Ab"), ValheimHash.GetStableHashCode("Ab\0cd"));
            Assert.Equal(ValheimHash.GetStableHashCode("Abc"), ValheimHash.GetStableHashCode("Abc\0d"));
        }

        [Fact]
        public void HashOfEmptyStringIsTheBareSeedMix()
        {
            Assert.Equal(unchecked(5381 + 5381 * 1566083941), ValheimHash.GetStableHashCode(string.Empty));
        }

        [Fact]
        public void HashRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => ValheimHash.GetStableHashCode(null!));
        }

        [Fact]
        public void RangeReturnsMinForAnEmptyRange()
        {
            var rng = UnityXorShift128.FromSeed(7);
            Assert.Equal(42, rng.NextRange(42, 42));
        }

        [Fact]
        public void RangeRejectsDescendingBounds()
        {
            var rng = UnityXorShift128.FromSeed(7);
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextRange(5, 1));
        }

        [Fact]
        public void SuccessorSeedsStayInsideTheGameRange()
        {
            for (int seed = -5000; seed < 5000; seed++)
            {
                var rng = UnityXorShift128.FromSeed(seed);
                rng.NextUInt();
                Assert.InRange(rng.NextRange(0, 99999999), 0, 99999998);
            }
        }

        [Fact]
        public void UnitFloatCoversTheClosedInterval()
        {
            Assert.Equal(0f, UnityFloat.FromRaw(0u));
            Assert.Equal(1f, UnityFloat.FromRaw(0x7FFFFFu));
            Assert.Equal(1f, UnityFloat.FromRaw(0xFFFFFFFFu));
        }

        [Fact]
        public void ReciprocalAndDivisionAgreeForEveryDraw()
        {
            // Whichever way Unity writes Random.value, the float result is the same for all 2^23 inputs.
            for (uint raw = 0; raw <= UnityFloat.MantissaMask; raw++)
            {
                float division = (float)((float)raw / 8388607f);
                Assert.Equal(division, UnityFloat.FromRaw(raw));
            }
        }
    }
}
