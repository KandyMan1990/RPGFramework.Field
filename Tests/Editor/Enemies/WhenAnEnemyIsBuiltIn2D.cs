using NUnit.Framework;
using RPGFramework.Field.Editor;
using UnityEngine;

namespace RPGFramework.Field.Tests.Enemies
{
    /// <summary>An enemy's body in a 2D field, where a tile is one unit.</summary>
    internal sealed class WhenAnEnemyIsBuiltIn2D
    {
        private GameObject    m_Root;
        private FieldEntity   m_Enemy;
        private BoxCollider2D m_Trigger;

        [SetUp]
        public void SetUp()
        {
            m_Root    = new GameObject("Field");
            m_Enemy   = FieldEntityBodyBuilder.Build(m_Root, FieldDimension.TwoD, FieldBodyPreset.Enemy, "Enemy3", null);
            m_Trigger = m_Enemy.GetComponentInChildren<FieldCollisionTrigger>().GetComponent<BoxCollider2D>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Root);
        }

        [Test]
        public void ThenItMovesAsACharacterDoes()
        {
            Assert.AreEqual(RigidbodyType2D.Kinematic, m_Enemy.GetComponent<Rigidbody2D>().bodyType);
        }

        [Test]
        public void ThenItsTriggerIsATrigger()
        {
            Assert.IsTrue(m_Trigger.isTrigger);
        }

        [Test]
        public void ThenItsTriggerReachesTheTilesBesideIt()
        {
            Assert.Greater(m_Trigger.size.x / 2f, 0.5f);
            Assert.Greater(m_Trigger.size.y / 2f, 0.5f);
        }

        [Test]
        public void ThenItsTriggerStopsShortOfTheCornerTiles()
        {
            Assert.LessOrEqual(m_Trigger.size.y / 2f, 0.75f);
        }
    }
}
