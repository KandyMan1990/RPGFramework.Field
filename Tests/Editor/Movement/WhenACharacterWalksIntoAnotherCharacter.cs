using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A character holds up at a tile another character, solid, stands on.</summary>
    internal sealed class WhenACharacterWalksIntoAnotherCharacter
    {
        private TileField   m_Field;
        private FieldEntity m_Other;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);
            m_Other = m_Field.AddEntity(new Vector3Int(0, 1, 0), true);

            Physics2D.SyncTransforms();

            m_Field.Driver.SetMoveInput(Vector3.up);
            m_Field.Driver.Tick(1f);
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
        public void ThenItPushesTheOther()
        {
            CollectionAssert.AreEqual(new[] { m_Other }, m_Field.Driver.Pushed);
        }

        [Test]
        public void ThenLettingGoStopsThePush()
        {
            m_Field.Driver.SetMoveInput(Vector3.zero);

            CollectionAssert.IsEmpty(m_Field.Driver.Pushed);
        }
    }
}
