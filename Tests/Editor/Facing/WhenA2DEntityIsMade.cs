using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Facing
{
    /// <summary>A 2D entity stands facing the camera until something turns it.</summary>
    internal sealed class WhenA2DEntityIsMade
    {
        private SpriteFacing m_Facing;

        [SetUp]
        public void SetUp()
        {
            m_Facing = new SpriteFacing();
        }

        [Test]
        public void ThenItFacesDownTheScreen()
        {
            FacingAssert.Faces(Vector3.down, m_Facing.Facing);
        }

        [Test]
        public void ThenItIsNotTurning()
        {
            Assert.IsFalse(m_Facing.Turn.Active);
        }
    }
}
