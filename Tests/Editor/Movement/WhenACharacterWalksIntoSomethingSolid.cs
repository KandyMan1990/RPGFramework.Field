using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A character walks up at a tile with something solid standing on it.</summary>
    internal sealed class WhenACharacterWalksIntoSomethingSolid
    {
        private TileField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);
            m_Field.AddSolid(new Vector3Int(0, 1, 0), false);
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

        [Test]
        public void ThenItFacesWhatStopsIt()
        {
            Assert.AreEqual(Vector3.up, m_Field.Driver.Facing);
        }
    }
}
