using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>
    /// A script walks a character up to another, two tiles across and two up from it, stopping a tile short.
    /// </summary>
    internal sealed class WhenAScriptedMoveGoesUpToAnotherCharacter
    {
        private TileField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(new Vector3Int(-1, 0, 0));

            FieldEntity other = m_Field.AddEntity(new Vector3Int(1, 2, 0), true);

            Physics2D.SyncTransforms();

            m_Field.Driver.MoveTo(other.transform.position, 1f, true);
            m_Field.TickWhileMoving();
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItStopsOnTheTileBesideIt()
        {
            Assert.AreEqual(m_Field.Centre(new Vector3Int(1, 1, 0)), m_Field.Character.transform.position);
        }

        [Test]
        public void ThenTheMoveEnds()
        {
            Assert.IsFalse(m_Field.Driver.IsMovingToTarget);
        }
    }
}
