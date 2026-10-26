using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A character walks up at a tile with only a trigger on it, as a gateway or a save point is.</summary>
    internal sealed class WhenACharacterWalksIntoATrigger
    {
        private TileField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);
            m_Field.AddSolid(new Vector3Int(0, 1, 0), true);
            m_Field.StepOnce(Vector3.up);
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItStepsOntoIt()
        {
            Assert.AreEqual(m_Field.Centre(new Vector3Int(0, 1, 0)), m_Field.Character.transform.position);
        }
    }
}
