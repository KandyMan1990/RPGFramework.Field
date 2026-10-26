using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A character in the middle of a tile field walks a tile at a time, in the grid's four directions.</summary>
    internal sealed class WhenACharacterStepsOnATileField
    {
        private TileField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenUpTakesItOneTileUp()
        {
            m_Field.StepOnce(Vector3.up);

            Assert.AreEqual(m_Field.Centre(new Vector3Int(0, 1, 0)), m_Field.Character.transform.position);
        }

        [Test]
        public void ThenTheStickTakesTheNearestOfTheFourDirections()
        {
            m_Field.StepOnce(new Vector3(0.9f, 0.3f, 0f));

            Assert.AreEqual(m_Field.Centre(new Vector3Int(1, 0, 0)), m_Field.Character.transform.position);
        }

        [Test]
        public void ThenItFacesTheWayItSteps()
        {
            m_Field.StepOnce(Vector3.left);

            Assert.AreEqual(Vector3.left, m_Field.Driver.Facing);
        }

        [Test]
        public void ThenHoldingTheInputCarriesItOnToTheNextTile()
        {
            m_Field.Driver.SetMoveInput(Vector3.up);
            m_Field.Driver.Tick(1f);
            m_Field.Driver.Tick(1f);

            Assert.AreEqual(m_Field.Centre(new Vector3Int(0, 2, 0)), m_Field.Character.transform.position);
        }

        [Test]
        public void ThenLettingGoFinishesTheStepUnderWay()
        {
            m_Field.Driver.SetMoveInput(Vector3.up);
            m_Field.Driver.Tick(0.5f);
            m_Field.Driver.SetMoveInput(Vector3.zero);
            m_Field.Driver.Tick(1f);

            Assert.AreEqual(m_Field.Centre(new Vector3Int(0, 1, 0)), m_Field.Character.transform.position);
        }
    }
}
