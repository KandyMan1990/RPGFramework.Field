using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Entities
{
    /// <summary>A jump of 4 units along x in 2 seconds, peaking 1 above the straight line.</summary>
    internal sealed class WhenAnEntityIsInTheAir
    {
        private const float TOLERANCE = 0.001f;

        private ScriptedJump m_Jump;

        [SetUp]
        public void SetUp()
        {
            m_Jump = new ScriptedJump(Vector3.zero, new Vector3(4f, 0f, 0f), 2f, 1f);
        }

        [Test]
        public void ThenItLeavesFromTheLine()
        {
            Assert.AreEqual(0f, m_Jump.Lift, TOLERANCE);
        }

        [Test]
        public void ThenHalfWayItIsHalfWayAlong()
        {
            m_Jump.Tick(1f);

            Assert.AreEqual(2f, m_Jump.Line.x, TOLERANCE);
        }

        [Test]
        public void ThenHalfWayItIsAtItsHeight()
        {
            m_Jump.Tick(1f);

            Assert.AreEqual(1f, m_Jump.Lift, TOLERANCE);
        }

        [Test]
        public void ThenAQuarterOfTheWayItIsThreeQuartersOfItsHeight()
        {
            m_Jump.Tick(0.5f);

            Assert.AreEqual(0.75f, m_Jump.Lift, TOLERANCE);
        }

        [Test]
        public void ThenItHasNotLandedBeforeItsTime()
        {
            m_Jump.Tick(1.9f);

            Assert.IsFalse(m_Jump.HasLanded);
        }

        [Test]
        public void ThenItLandsOnTheLine()
        {
            m_Jump.Tick(2f);

            Assert.IsTrue(m_Jump.HasLanded);
            Assert.AreEqual(0f, m_Jump.Lift, TOLERANCE);
        }

        [Test]
        public void ThenNoTimeLandsAtOnce()
        {
            ScriptedJump jump = new ScriptedJump(Vector3.zero, Vector3.one, 0f, 1f);

            jump.Tick(0f);

            Assert.IsTrue(jump.HasLanded);
        }
    }
}
