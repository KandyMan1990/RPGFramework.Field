using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>A character waiting on a scripted move finds the one in its way gone.</summary>
    internal sealed class WhenTheWayOfAScriptedMoveClears
    {
        private TileField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TileField(Vector3Int.zero);

            FieldEntity other = m_Field.AddEntity(new Vector3Int(0, 1, 0), true);

            Physics2D.SyncTransforms();

            m_Field.Driver.MoveTo(m_Field.Centre(new Vector3Int(0, 2, 0)), 0f, true);
            m_Field.Driver.Tick(1f);

            other.transform.position = m_Field.Centre(new Vector3Int(-2, -2, 0));
            Physics2D.SyncTransforms();

            m_Field.TickWhileMoving();
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItCarriesOnToItsPoint()
        {
            Assert.AreEqual(m_Field.Centre(new Vector3Int(0, 2, 0)), m_Field.Character.transform.position);
        }
    }
}
