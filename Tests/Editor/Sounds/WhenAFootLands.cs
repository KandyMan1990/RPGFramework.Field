using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Sounds
{
    /// <summary>A body whose feet land at the start of the cycle and halfway through it.</summary>
    internal sealed class WhenAFootLands
    {
        private GameObject  m_Object;
        private FieldEntity m_Body;

        [SetUp]
        public void SetUp()
        {
            m_Object = new GameObject("Body");
            m_Body   = m_Object.AddComponent<FieldEntity>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Object);
        }

        [Test]
        public void ThenPassingHalfWayIsAStep()
        {
            Assert.IsTrue(m_Body.FootLandedBetween(0.4f, 0.6f));
        }

        [Test]
        public void ThenBetweenThePointsIsNoStep()
        {
            Assert.IsFalse(m_Body.FootLandedBetween(0.6f, 0.9f));
        }

        [Test]
        public void ThenGoingRoundPastTheStartIsAStep()
        {
            Assert.IsTrue(m_Body.FootLandedBetween(0.9f, 0.1f));
        }

        [Test]
        public void ThenStandingStillIsNoStep()
        {
            Assert.IsFalse(m_Body.FootLandedBetween(0.5f, 0.5f));
        }
    }
}
