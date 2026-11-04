using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Scrolling
{
    /// <summary>A view scrolled away to a point, scrolled back to the player.</summary>
    internal sealed class WhenTheViewScrollsBackToThePlayer
    {
        private CameraFocus m_Focus;

        [SetUp]
        public void SetUp()
        {
            m_Focus = new CameraFocus();
            m_Focus.Tick(0f, Vector3.zero);
            m_Focus.ScrollToPoint(new Vector3(4f, 0f, 0f), 0f, Easing.Linear);
            m_Focus.Tick(0.1f, new Vector3(4f, 0f, 0f));
            m_Focus.ScrollToPlayer(1f, Easing.Smooth);
        }

        [Test]
        public void ThenItsTargetIsThePlayerAgain()
        {
            Assert.AreEqual(CameraFocusTarget.Player, m_Focus.Target);
        }

        [Test]
        public void ThenItStartsFromWhereTheViewWas()
        {
            m_Focus.Tick(0f, Vector3.zero);

            Assert.AreEqual(new Vector3(4f, 0f, 0f), m_Focus.Position);
        }
    }
}
