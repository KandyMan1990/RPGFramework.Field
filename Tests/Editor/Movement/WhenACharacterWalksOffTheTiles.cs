using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A character on the top row of a tile field walks up, where there is no tile.</summary>
    internal sealed class WhenACharacterWalksOffTheTiles
    {
        private TileField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(new Vector3Int(0, 2, 0));
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
            Assert.AreEqual(m_Field.Centre(new Vector3Int(0, 2, 0)), m_Field.Character.transform.position);
        }

        [Test]
        public void ThenItFacesTheWayItCannotGo()
        {
            Assert.AreEqual(Vector3.up, m_Field.Driver.Facing);
        }
    }
}
