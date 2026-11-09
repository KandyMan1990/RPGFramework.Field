using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Heads
{
    /// <summary>A head at 45 degrees to the right, sent back over a second.</summary>
    internal sealed class WhenAHeadLooksForward
    {
        private const float TOLERANCE = 0.01f;

        private HeadLook m_Head;

        [SetUp]
        public void SetUp()
        {
            Vector3 point = new Vector3(1f, 0f, 1f);

            m_Head = default;
            m_Head.LookAt(point, FieldEntity.NO_ENTITY, 0f);
            m_Head.Aim(Quaternion.identity, Vector3.zero, point);
            m_Head.Tick(0f);
            m_Head.LookForward(1f);
        }

        [Test]
        public void ThenThereIsNowhereToAimAt()
        {
            Assert.IsFalse(m_Head.NeedsAim);
        }

        [Test]
        public void ThenItHasNotGoneForwardYet()
        {
            m_Head.Tick(0.5f);

            Assert.IsFalse(m_Head.HasGoneForward);
        }

        [Test]
        public void ThenItEndsWithNoTurnLeft()
        {
            m_Head.Tick(1f);

            Assert.AreEqual(0f, m_Head.Angles.x, TOLERANCE);
        }

        [Test]
        public void ThenTheHeadIsItsAnimationsAgain()
        {
            m_Head.Tick(1f);

            Assert.IsTrue(m_Head.HasGoneForward);
        }

        [Test]
        public void ThenLookingAtSomethingAgainCancelsIt()
        {
            m_Head.LookAt(Vector3.forward, FieldEntity.NO_ENTITY, 1f);
            m_Head.Tick(1f);

            Assert.IsFalse(m_Head.HasGoneForward);
        }
    }
}
