using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A script slides a character two tiles across, neither turning nor walking.</summary>
    internal sealed class WhenAScriptedMoveSlides
    {
        private TileField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);

            Physics2D.SyncTransforms();

            m_Field.Driver.MoveTo(m_Field.Centre(new Vector3Int(2, 0, 0)), 0f, false);
            m_Field.TickWhileMoving();
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItGetsThere()
        {
            Assert.AreEqual(m_Field.Centre(new Vector3Int(2, 0, 0)), m_Field.Character.transform.position);
        }

        [Test]
        public void ThenItStillFacesTheWayItFaced()
        {
            Assert.AreEqual(Vector3.down, m_Field.Driver.Facing);
        }
    }
}
