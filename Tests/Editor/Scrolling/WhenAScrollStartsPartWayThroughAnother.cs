using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Scrolling
{
    /// <summary>A second scroll asked for halfway through the first.</summary>
    internal sealed class WhenAScrollStartsPartWayThroughAnother
    {
        private static readonly Vector3 m_Second = new Vector3(2f, 4f, 0f);

        private CameraFocus m_Focus;

        [SetUp]
        public void SetUp()
        {
            m_Focus = new CameraFocus();
            m_Focus.Tick(0f, Vector3.zero);
            m_Focus.ScrollToPoint(new Vector3(4f, 0f, 0f), 2f, Easing.Linear);
            m_Focus.Tick(1f, new Vector3(4f, 0f, 0f));
            m_Focus.ScrollToPoint(m_Second, 1f, Easing.Linear);
            m_Focus.Tick(0.5f, m_Second);
        }

        [Test]
        public void ThenItRunsOnFromWhereTheViewHadGot()
        {
            Assert.AreEqual(new Vector3(2f, 2f, 0f), m_Focus.Position);
        }
    }
}
