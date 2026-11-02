using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A scripted move is stopped halfway through a step.</summary>
    internal sealed class WhenAScriptedMoveIsStoppedBetweenTiles
    {
        private TileField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);

            Physics2D.SyncTransforms();

            m_Field.Driver.MoveTo(m_Field.Centre(new Vector3Int(0, 2, 0)), 0f, true);
            m_Field.Driver.Tick(0.5f);
            m_Field.Driver.StopMove();
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItIsStillMovingUntilItReachesATile()
        {
            Assert.IsTrue(m_Field.Driver.IsMovingToTarget);
        }

        [Test]
        public void ThenItFinishesOnTheTileItWasSteppingOnto()
        {
            m_Field.Driver.Tick(1f);
            m_Field.Driver.Tick(1f);

            Assert.AreEqual(m_Field.Centre(new Vector3Int(0, 1, 0)), m_Field.Character.transform.position);
        }

        [Test]
        public void ThenTheMoveEndsOnceItIsThere()
        {
            m_Field.Driver.Tick(1f);

            Assert.IsFalse(m_Field.Driver.IsMovingToTarget);
        }
    }
}
