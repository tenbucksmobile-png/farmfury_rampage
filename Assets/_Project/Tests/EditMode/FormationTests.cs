using FarmFuryRampage.Sim;
using NUnit.Framework;
using Unity.Mathematics;

namespace FarmFuryRampage.Tests
{
    public sealed class FormationTests
    {
        const float Spacing = 0.28f;

        [Test]
        public void FirstSlot_IsHerdCentre()
        {
            Assert.AreEqual(0f, math.length(Formation.SlotOffset(0, Spacing)), 1e-6f);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(20)]
        [TestCase(60)]
        public void EverySlot_IsInsideTheFormationRadius(int drawn)
        {
            float radius = Formation.Radius(drawn, Spacing);
            for (int i = 0; i < drawn; i++)
                Assert.LessOrEqual(math.length(Formation.SlotOffset(i, Spacing)), radius + 1e-4f, $"slot {i} of {drawn}");
        }

        [TestCase(60)]
        public void FullHerd_FitsOnTheTrack(int drawn)
        {
            // A full drawn herd must leave room to steer on the default 9 m track.
            Assert.Less(Formation.FootprintRadius(drawn, Spacing) * 2f, 9f);
        }
    }
}
