using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A script sends a character two tiles across and two up on a tile field.</summary>
    internal sealed class WhenAScriptedMoveCrossesATileField
    {
        private TileField m_Field;
        private Vector3[] m_Path;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(new Vector3Int(-1, -1, 0));

            Physics2D.SyncTransforms();

            m_Field.Driver.MoveTo(m_Field.Centre(new Vector3Int(1, 1, 0)), 0f, true);
            m_Path = m_Field.TickWhileMoving();
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItWalksAcrossBeforeGoingUpATileAtATime()
        {
            Vector3[] expected =
            {
                m_Field.Centre(new Vector3Int(0, -1, 0)),
                m_Field.Centre(new Vector3Int(1, -1, 0)),
                m_Field.Centre(new Vector3Int(1, 0, 0)),
                m_Field.Centre(new Vector3Int(1, 1, 0))
            };

            CollectionAssert.AreEqual(expected, m_Path);
        }

        [Test]
        public void ThenTheMoveEnds()
        {
            Assert.IsFalse(m_Field.Driver.IsMovingToTarget);
        }

        [Test]
        public void ThenItFacesTheWayItLastWent()
        {
            Assert.AreEqual(Vector3.up, m_Field.Driver.Facing);
        }
    }
}
