using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Scrolling
{
    /// <summary>A two-second eased scroll four units across, a quarter of the way through its time.</summary>
    internal sealed class WhenAScrollEasesToAPoint
    {
        private static readonly Vector3 m_Point = new Vector3(4f, 0f, 0f);

        private CameraFocus m_Focus;

        [SetUp]
        public void SetUp()
        {
            m_Focus = new CameraFocus();
            m_Focus.Tick(0f, Vector3.zero);
            m_Focus.ScrollToPoint(m_Point, 2f, Easing.Smooth);
            m_Focus.Tick(0.5f, m_Point);
        }

        // A smooth step a quarter through is 0.15625 of the way, where a steady scroll is a quarter of it.
        [Test]
        public void ThenItHasMovedLessThanASteadyScrollWould()
        {
            Assert.AreEqual(0.625f, m_Focus.Position.x, 0.0001f);
        }
    }
}
