using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;

namespace RPGFramework.Field.Tests.Ladders
{
    /// <summary>A ladder that begins where the entity stands and ends where the climb does, from 0 to 4 at 2 a second.</summary>
    internal sealed class WhenALadderHasNoEnds
    {
        private LadderClimb m_Climb;

        [SetUp]
        public void SetUp()
        {
            m_Climb = new LadderClimb(Ladder.At(0f), new LadderClimbArgs(true, Ladder.At(0f), Ladder.At(4f), Ladder.At(4f)), 2f, 1f, 1f, 1f);
        }

        [Test]
        public void ThenItStartsOnTheClimb()
        {
            Assert.AreEqual(LadderAnimation.Climb, m_Climb.Pose.Animation);
        }

        [Test]
        public void ThenItFinishesAsTheClimbEnds()
        {
            m_Climb.Tick(2f, Ladder.ON);

            Assert.IsTrue(m_Climb.IsFinished);
            Ladder.AssertIsAt(4f, m_Climb);
        }

        [Test]
        public void ThenGoingBackPastTheBottomFinishesThere()
        {
            m_Climb.Tick(0.1f, -1f);

            Assert.IsTrue(m_Climb.IsFinished);
            Ladder.AssertIsAt(0f, m_Climb);
        }
    }
}
