using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Facing
{
    /// <summary>Walking, or facing another entity, faces a 2D entity along the screen at once.</summary>
    internal sealed class WhenA2DEntityFacesADirection
    {
        private SpriteFacing m_Facing;

        [SetUp]
        public void SetUp()
        {
            m_Facing = new SpriteFacing();
        }

        [Test]
        public void ThenItFacesAlongItOnTheScreen()
        {
            m_Facing.Face(new Vector3(2f, 2f, 5f));

            FacingAssert.Faces(new Vector3(1f, 1f, 0f), m_Facing.Facing);
        }

        [Test]
        public void ThenNoDirectionLeavesItFacingAsItWas()
        {
            m_Facing.Face(Vector3.right);
            m_Facing.Face(Vector3.zero);

            FacingAssert.Faces(Vector3.right, m_Facing.Facing);
        }
    }
}
