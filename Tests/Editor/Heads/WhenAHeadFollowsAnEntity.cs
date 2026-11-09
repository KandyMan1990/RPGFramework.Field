using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Heads
{
    /// <summary>A head that has finished turning to entity 3, straight ahead, and keeps looking at it.</summary>
    internal sealed class WhenAHeadFollowsAnEntity
    {
        private const float TOLERANCE = 0.01f;

        private HeadLook m_Head;

        [SetUp]
        public void SetUp()
        {
            m_Head = default;
            m_Head.LookAt(Vector3.forward, 3, 0f);
            m_Head.Aim(Quaternion.identity, Vector3.zero, Vector3.forward);
            m_Head.Tick(0f);
        }

        [Test]
        public void ThenItIsNotTurning()
        {
            Assert.IsFalse(m_Head.IsTurning);
        }

        [Test]
        public void ThenWhereItIsGoingIsWorkedOutEveryFrame()
        {
            Assert.IsTrue(m_Head.NeedsAim);
        }

        [Test]
        public void ThenItKeepsUpWithASmallMove()
        {
            AimAt(new Vector3(0.1f, 0f, 1f));
            m_Head.Tick(0.1f);

            Assert.AreEqual(5.71f, m_Head.Angles.x, TOLERANCE);
        }

        [Test]
        public void ThenATargetThatJumpsIsGoneAfterRatherThanSnappedTo()
        {
            AimAt(Vector3.right);
            m_Head.Tick(0.1f);

            Assert.AreEqual(27f, m_Head.Angles.x, TOLERANCE);
        }

        private void AimAt(Vector3 point)
        {
            m_Head.Aim(Quaternion.identity, Vector3.zero, point);
        }
    }
}
