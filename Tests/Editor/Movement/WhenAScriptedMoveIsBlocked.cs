using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A script sends a character up through a tile another character stands on.</summary>
    internal sealed class WhenAScriptedMoveIsBlocked
    {
        private TileField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);
            m_Field.AddEntity(new Vector3Int(0, 1, 0), true);

            Physics2D.SyncTransforms();

            m_Field.Driver.MoveTo(m_Field.Centre(new Vector3Int(0, 2, 0)), 0f, true);
            m_Field.Driver.Tick(1f);
            m_Field.Driver.Tick(1f);
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItWaitsOnItsTile()
        {
            Assert.AreEqual(m_Field.Centre(Vector3Int.zero), m_Field.Character.transform.position);
        }

        [Test]
        public void ThenTheMoveCarriesOn()
        {
            Assert.IsTrue(m_Field.Driver.IsMovingToTarget);
        }

        [Test]
        public void ThenItFacesTheWayItIsGoing()
        {
            Assert.AreEqual(Vector3.up, m_Field.Driver.Facing);
        }
    }
}
