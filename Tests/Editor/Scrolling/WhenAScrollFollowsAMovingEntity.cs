using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Scrolling
{
    /// <summary>
    /// A two-second scroll onto an entity walking a unit a second, which starts four units away, and a second after the
    /// scroll has arrived.
    /// </summary>
    internal sealed class WhenAScrollFollowsAMovingEntity
    {
        private const int ENTITY = 7;

        private CameraFocus m_Focus;
        private Vector3     m_Arrived;

        [SetUp]
        public void SetUp()
        {
            m_Focus = new CameraFocus();
            m_Focus.Tick(0f, Vector3.zero);
            m_Focus.ScrollToEntity(ENTITY, 2f, Easing.Linear);
            m_Focus.Tick(1f, new Vector3(5f, 0f, 0f));
            m_Focus.Tick(1f, new Vector3(6f, 0f, 0f));
            m_Arrived = m_Focus.Position;
            m_Focus.Tick(1f, new Vector3(7f, 0f, 0f));
        }

        [Test]
        public void ThenItArrivesWhereTheEntityIsThen()
        {
            Assert.AreEqual(new Vector3(6f, 0f, 0f), m_Arrived);
        }

        [Test]
        public void ThenItKeepsFollowingIt()
        {
            Assert.AreEqual(new Vector3(7f, 0f, 0f), m_Focus.Position);
        }

        [Test]
        public void ThenItsTargetIsTheEntity()
        {
            Assert.AreEqual(CameraFocusTarget.Entity, m_Focus.Target);
            Assert.AreEqual(ENTITY, m_Focus.EntityId);
        }
    }
}
