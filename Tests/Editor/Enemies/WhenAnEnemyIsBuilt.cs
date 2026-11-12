using NUnit.Framework;
using RPGFramework.Field.Editor;
using UnityEngine;

namespace RPGFramework.Field.Tests.Enemies
{
    /// <summary>An enemy's body in a 3D field, with no visuals.</summary>
    internal sealed class WhenAnEnemyIsBuilt
    {
        private GameObject  m_Root;
        private FieldEntity m_Enemy;
        private BoxCollider m_Trigger;

        [SetUp]
        public void SetUp()
        {
            m_Root    = new GameObject("Field");
            m_Enemy   = FieldEntityBodyBuilder.Build(m_Root, FieldDimension.ThreeD, FieldBodyPreset.Enemy, "Enemy3", null);
            m_Trigger = m_Enemy.GetComponentInChildren<FieldCollisionTrigger>().GetComponent<BoxCollider>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Root);
        }

        [Test]
        public void ThenItMovesAsACharacterDoes()
        {
            Assert.IsTrue(m_Enemy.GetComponent<Rigidbody>().isKinematic);
        }

        [Test]
        public void ThenNothingTalksToIt()
        {
            Assert.IsNull(m_Enemy.GetComponent<FieldInteractionTrigger>());
        }

        [Test]
        public void ThenItsTriggerIsATrigger()
        {
            Assert.IsTrue(m_Trigger.isTrigger);
        }

        [Test]
        public void ThenItsTriggerReachesPastItsBody()
        {
            CapsuleCollider body = m_Enemy.GetComponent<CapsuleCollider>();

            Assert.Greater(m_Trigger.size.x / 2f, body.radius);
            Assert.Greater(m_Trigger.size.z / 2f, body.radius);
        }

        [Test]
        public void ThenItsTriggerCoversItsHeight()
        {
            CapsuleCollider body = m_Enemy.GetComponent<CapsuleCollider>();

            Assert.AreEqual(body.center.y, m_Trigger.center.y);
            Assert.GreaterOrEqual(m_Trigger.size.y, body.height);
        }
    }
}
