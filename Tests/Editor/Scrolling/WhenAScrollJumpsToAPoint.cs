using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Scrolling
{
    /// <summary>A scroll of no seconds, to a point.</summary>
    internal sealed class WhenAScrollJumpsToAPoint
    {
        private static readonly Vector3 m_Point = new Vector3(4f, 0f, 0f);

        private CameraFocus m_Focus;

        [SetUp]
        public void SetUp()
        {
            m_Focus = new CameraFocus();
            m_Focus.Tick(0f, Vector3.zero);
            m_Focus.ScrollToPoint(m_Point, 0f, Easing.Linear);
        }

        [Test]
        public void ThenAScriptWaitingOnItCarriesOnAtOnce()
        {
            Assert.IsFalse(m_Focus.IsScrolling);
        }

        [Test]
        public void ThenTheViewIsThereAfterOneTick()
        {
            m_Focus.Tick(0.1f, m_Point);

            Assert.AreEqual(m_Point, m_Focus.Position);
        }
    }
}
