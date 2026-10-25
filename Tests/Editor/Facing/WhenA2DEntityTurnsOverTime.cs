using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Facing
{
    /// <summary>
    /// A 2D entity facing up the screen turns over a second to face left — a quarter turn anticlockwise, or three
    /// quarters clockwise — and is looked at halfway.
    /// </summary>
    internal sealed class WhenA2DEntityTurnsOverTime
    {
        private SpriteFacing m_Facing;

        [SetUp]
        public void SetUp()
        {
            m_Facing = new SpriteFacing();
            m_Facing.Face(Vector3.up);
        }

        [Test]
        public void ThenClockwiseItGoesTheLongWayRound()
        {
            TurnToLeft(RotationDirection.Clockwise);
            m_Facing.Tick(0.5f);

            FacingAssert.Faces(new Vector3(1f, -1f, 0f), m_Facing.Facing);
        }

        [Test]
        public void ThenAnticlockwiseItGoesTheShortWayRound()
        {
            TurnToLeft(RotationDirection.CounterClockwise);
            m_Facing.Tick(0.5f);

            FacingAssert.Faces(new Vector3(-1f, 1f, 0f), m_Facing.Facing);
        }

        [Test]
        public void ThenTheClosestWayIsTheShortWayRound()
        {
            TurnToLeft(RotationDirection.Closest);
            m_Facing.Tick(0.5f);

            FacingAssert.Faces(new Vector3(-1f, 1f, 0f), m_Facing.Facing);
        }

        [Test]
        public void ThenItFacesLeftOnceTheTurnIsDone()
        {
            TurnToLeft(RotationDirection.Clockwise);
            m_Facing.Tick(1f);

            FacingAssert.Faces(Vector3.left, m_Facing.Facing);
        }

        [Test]
        public void ThenTheTurnEndsWhenItIsDone()
        {
            TurnToLeft(RotationDirection.Clockwise);
            m_Facing.Tick(1f);

            Assert.IsFalse(m_Facing.Turn.Active);
        }

        [Test]
        public void ThenATurnResumedHalfwayCarriesOnTheSameWay()
        {
            TurnToLeft(RotationDirection.Clockwise);
            m_Facing.Tick(0.5f);

            SpriteFacing resumed = new SpriteFacing();
            resumed.ResumeTurn(m_Facing.Turn);
            resumed.Tick(0.25f);

            // Three quarters of the way through three quarters of a turn clockwise.
            FacingAssert.Faces(Quaternion.Euler(0f, 0f, -202.5f) * Vector3.up, resumed.Facing);
        }

        private void TurnToLeft(RotationDirection direction)
        {
            m_Facing.StartTurn(new SetEntityRotationAsyncArgs(Quaternion.Euler(0f, 0f, 90f), direction, 1f, RotationInterpolation.Linear));
        }
    }
}
