using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>A player walking at two units a second, its body moved only by the physics step.</summary>
    internal sealed class WhenThePlayersWalkIsMeasured
    {
        private const float SPEED     = 2f;
        private const float FRAME     = 0.008f;
        private const float PHYSICS   = 0.02f;
        private const float TOLERANCE = 1e-5f;

        private PlayerWalk m_Walk;

        [SetUp]
        public void SetUp()
        {
            m_Walk = new PlayerWalk();

            Take(Vector3.zero);
        }

        [Test]
        public void ThenAFrameWithoutAStepWalksNoTime()
        {
            Assert.AreEqual(0f, Take(Vector3.zero).Seconds);
        }

        [Test]
        public void ThenTheFramesAddUpToTheStep()
        {
            float seconds = Take(Vector3.zero).Seconds + Take(new Vector3(0.04f, 0f, 0f)).Seconds;

            Assert.AreEqual(PHYSICS, seconds, TOLERANCE);
        }

        [Test]
        public void ThenBeingPutFarAwayIsAFrameAndAStepAtMost()
        {
            Assert.AreEqual(FRAME + PHYSICS, Take(new Vector3(100f, 0f, 0f)).Seconds, TOLERANCE);
        }

        [Test]
        public void ThenNoSpeedWalksNoTime()
        {
            Assert.AreEqual(0f, m_Walk.Take(Vector3.one, 0f, FRAME, PHYSICS).Seconds);
        }

        private PlayerWalkArgs Take(Vector3 position)
        {
            return m_Walk.Take(position, SPEED, FRAME, PHYSICS);
        }
    }
}
