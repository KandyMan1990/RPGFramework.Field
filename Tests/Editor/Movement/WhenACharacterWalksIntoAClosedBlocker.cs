using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A character walks up at a tile a closed blocker's tilemap closes.</summary>
    internal sealed class WhenACharacterWalksIntoAClosedBlocker
    {
        private TileField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);
            m_Field.AddClosedBlocker(new Vector3Int(0, 1, 0));
            m_Field.StepOnce(Vector3.up);
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItStaysWhereItIs()
        {
            Assert.AreEqual(m_Field.Centre(Vector3Int.zero), m_Field.Character.transform.position);
        }
    }
}
