using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>
    /// A scripted move given to a character standing part-way between tiles, as one kept across a battle is.
    /// </summary>
    internal sealed class WhenAScriptedMoveStartsBetweenTiles
    {
        private TileField m_Field;
        private Vector3[] m_Path;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);
            m_Field.Character.transform.position = m_Field.Centre(Vector3Int.zero) + new Vector3(0f, 0.3f, 0f);

            Physics2D.SyncTransforms();

            m_Field.Driver.MoveTo(m_Field.Centre(new Vector3Int(2, 0, 0)), 0f, true);
            m_Path = m_Field.TickWhileMoving();
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItSettlesOnTheTileItIsInFirst()
        {
            Assert.AreEqual(m_Field.Centre(Vector3Int.zero), m_Path[0]);
        }

        [Test]
        public void ThenItEndsOnItsPoint()
        {
            Assert.AreEqual(m_Field.Centre(new Vector3Int(2, 0, 0)), m_Field.Character.transform.position);
        }
    }
}
