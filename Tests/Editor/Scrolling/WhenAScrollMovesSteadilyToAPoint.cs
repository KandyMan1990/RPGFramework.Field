using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Scrolling
{
    /// <summary>A two-second linear scroll four units across, a second in.</summary>
    internal sealed class WhenAScrollMovesSteadilyToAPoint
    {
        private static readonly Vector3 m_Point = new Vector3(4f, 0f, 0f);

        private CameraFocus m_Focus;

        [SetUp]
        public void SetUp()
        {
            m_Focus = new CameraFocus();
            m_Focus.Tick(0f, Vector3.zero);
            m_Focus.ScrollToPoint(m_Point, 2f, Easing.Linear);
            m_Focus.Tick(1f, m_Point);
        }

        [Test]
        public void ThenItIsHalfwayAfterHalfItsTime()
        {
            Assert.AreEqual(new Vector3(2f, 0f, 0f), m_Focus.Position);
        }

        [Test]
        public void ThenItIsStillScrolling()
        {
            Assert.IsTrue(m_Focus.IsScrolling);
        }

        [Test]
        public void ThenItArrivesWhenItsTimeIsUp()
        {
            m_Focus.Tick(1f, m_Point);

            Assert.AreEqual(m_Point, m_Focus.Position);
            Assert.IsFalse(m_Focus.IsScrolling);
        }

        [Test]
        public void ThenItsTargetIsThePoint()
        {
            Assert.AreEqual(CameraFocusTarget.Point, m_Focus.Target);
            Assert.AreEqual(m_Point, m_Focus.Point);
        }
    }
}
