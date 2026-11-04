using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Scrolling
{
    /// <summary>A field's view before any script scrolls it, the player walking about.</summary>
    internal sealed class WhenTheViewFollowsThePlayer
    {
        private CameraFocus m_Focus;

        [SetUp]
        public void SetUp()
        {
            m_Focus = new CameraFocus();
            m_Focus.Tick(0.1f, new Vector3(1f, 2f, 0f));
            m_Focus.Tick(0.1f, new Vector3(3f, 4f, 0f));
        }

        [Test]
        public void ThenItIsOnThePlayerEachTick()
        {
            Assert.AreEqual(new Vector3(3f, 4f, 0f), m_Focus.Position);
        }

        [Test]
        public void ThenItsTargetIsThePlayer()
        {
            Assert.AreEqual(CameraFocusTarget.Player, m_Focus.Target);
        }

        [Test]
        public void ThenItIsNotScrolling()
        {
            Assert.IsFalse(m_Focus.IsScrolling);
        }
    }
}
