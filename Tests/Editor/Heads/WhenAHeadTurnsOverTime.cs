using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Heads
{
    /// <summary>A head sent 40 degrees to the right, over two seconds.</summary>
    internal sealed class WhenAHeadTurnsOverTime
    {
        private const float TOLERANCE = 0.01f;

        private static readonly Vector3 m_Point = new Vector3(Mathf.Tan(40f * Mathf.Deg2Rad), 0f, 1f);

        private HeadLook m_Head;

        [SetUp]
        public void SetUp()
        {
            m_Head = default;
            m_Head.LookAt(m_Point, FieldEntity.NO_ENTITY, 2f);
            m_Head.Aim(Quaternion.identity, Vector3.zero, m_Point);
        }

        [Test]
        public void ThenItIsTurning()
        {
            Assert.IsTrue(m_Head.IsTurning);
        }

        [Test]
        public void ThenItIsHalfwayHalfwayThrough()
        {
            m_Head.Tick(1f);

            Assert.AreEqual(20f, m_Head.Angles.x, TOLERANCE);
        }

        [Test]
        public void ThenItEasesAtTheStart()
        {
            m_Head.Tick(0.2f);

            Assert.Less(m_Head.Angles.x, 4f);
        }

        [Test]
        public void ThenItArrivesWhenTheTimeIsUp()
        {
            m_Head.Tick(2f);

            Assert.AreEqual(40f, m_Head.Angles.x, TOLERANCE);
            Assert.IsFalse(m_Head.IsTurning);
        }

        [Test]
        public void ThenItStaysWhereItWasSent()
        {
            m_Head.Tick(2f);
            m_Head.Tick(5f);

            Assert.AreEqual(40f, m_Head.Angles.x, TOLERANCE);
        }

        [Test]
        public void ThenWhereItIsGoingIsWorkedOutOnce()
        {
            Assert.IsFalse(m_Head.NeedsAim);
        }

        [Test]
        public void ThenASecondTurnStartsFromWhereTheHeadHasGot()
        {
            m_Head.Tick(1f);
            m_Head.LookForward(2f);
            m_Head.Tick(0f);

            Assert.AreEqual(20f, m_Head.Angles.x, TOLERANCE);
        }
    }
}
