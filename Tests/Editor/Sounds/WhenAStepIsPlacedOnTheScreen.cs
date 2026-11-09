using NUnit.Framework;

namespace RPGFramework.Field.Tests.Sounds
{
    /// <summary>How a step sounds from where its entity is.</summary>
    internal sealed class WhenAStepIsPlacedOnTheScreen
    {
        private const float TOLERANCE = 0.001f;

        [Test]
        public void ThenTheMiddleOfTheScreenIsCentred()
        {
            Assert.AreEqual(0f, Footsteps.Pan(0.5f), TOLERANCE);
        }

        [Test]
        public void ThenTheLeftEdgeIsPannedAQuarterOrSoLeft()
        {
            Assert.AreEqual(-0.3125f, Footsteps.Pan(0f), TOLERANCE);
        }

        [Test]
        public void ThenFarOffTheScreenStopsAtFull()
        {
            Assert.AreEqual(1f, Footsteps.Pan(10f), TOLERANCE);
        }

        [Test]
        public void ThenBesideTheCameraIsFull()
        {
            Assert.AreEqual(1f, Footsteps.Volume(0f, 30f), TOLERANCE);
        }

        [Test]
        public void ThenTheFarDistanceIsAtItsQuietest()
        {
            Assert.AreEqual(0.13f, Footsteps.Volume(30f, 30f), TOLERANCE);
        }

        [Test]
        public void ThenBeyondItGetsNoQuieter()
        {
            Assert.AreEqual(0.13f, Footsteps.Volume(100f, 30f), TOLERANCE);
        }

        [Test]
        public void ThenNoFarDistanceKeepsItNear()
        {
            Assert.AreEqual(1f, Footsteps.Volume(100f, 0f), TOLERANCE);
        }
    }
}
