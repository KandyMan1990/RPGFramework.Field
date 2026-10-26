using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A character walks up at a tile a character whose solidity is off stands on.</summary>
    internal sealed class WhenACharacterWalksOntoANonSolidCharacter
    {
        private TileField   m_Field;
        private FieldEntity m_Other;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);
            m_Other = m_Field.AddEntity(new Vector3Int(0, 1, 0), false);

            Physics2D.SyncTransforms();

            m_Field.Driver.SetMoveInput(Vector3.up);
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItPushesTheOther()
        {
            CollectionAssert.AreEqual(new[] { m_Other }, m_Field.Driver.Pushed);
        }

        [Test]
        public void ThenItStepsOntoItsTile()
        {
            m_Field.Driver.SetMoveInput(Vector3.zero);
            m_Field.Driver.Tick(2f);

            Assert.AreEqual(m_Field.Centre(new Vector3Int(0, 1, 0)), m_Field.Character.transform.position);
        }
    }
}
