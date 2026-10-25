using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Facing
{
    /// <summary>A script's rotation is read as Unity's 2D rotations are, and faces the entity rather than turning it.</summary>
    internal sealed class WhenA2DEntityIsGivenARotation
    {
        private SpriteFacing m_Facing;

        [SetUp]
        public void SetUp()
        {
            m_Facing = new SpriteFacing();
        }

        [Test]
        public void ThenNoTurnFacesUpTheScreen()
        {
            m_Facing.SetRotation(Quaternion.identity);

            FacingAssert.Faces(Vector3.up, m_Facing.Facing);
        }

        [Test]
        public void ThenAQuarterTurnFacesLeft()
        {
            m_Facing.SetRotation(Quaternion.Euler(0f, 0f, 90f));

            FacingAssert.Faces(Vector3.left, m_Facing.Facing);
        }

        [Test]
        public void ThenATurnAboutAnotherAxisLeavesItFacingUpTheScreen()
        {
            m_Facing.SetRotation(Quaternion.Euler(0f, 90f, 0f));

            FacingAssert.Faces(Vector3.up, m_Facing.Facing);
        }

        [Test]
        public void ThenItsRotationGivesTheSameFacingBack()
        {
            m_Facing.SetRotation(Quaternion.Euler(0f, 0f, 135f));

            SpriteFacing restored = new SpriteFacing();
            restored.SetRotation(m_Facing.Rotation);

            FacingAssert.Faces(m_Facing.Facing, restored.Facing);
        }
    }
}
