using NUnit.Framework;

namespace RPGFramework.Field.Tests.Ladders
{
    /// <summary>A ladder gone down from the top, which runs its three animations backwards.</summary>
    internal sealed class WhenALadderIsTakenDown
    {
        private LadderClimb m_Climb;

        [SetUp]
        public void SetUp()
        {
            m_Climb = Ladder.Down();
        }

        [Test]
        public void ThenGettingOnPlaysGetOffBackwards()
        {
            m_Climb.Tick(0.25f, Ladder.ON);

            Ladder.AssertShows(LadderAnimation.GetOff, 0.75f, m_Climb);
        }

        [Test]
        public void ThenTheClimbRunsBackwards()
        {
            m_Climb.Tick(Ladder.SECOND, Ladder.ON);
            m_Climb.Tick(0.25f, Ladder.ON);

            Ladder.AssertShows(LadderAnimation.Climb, 0.75f, m_Climb);
        }

        [Test]
        public void ThenGettingOffPlaysGetOnBackwards()
        {
            m_Climb.Tick(Ladder.SECOND, Ladder.ON);
            m_Climb.Tick(2f, Ladder.ON);
            m_Climb.Tick(0.25f, Ladder.ON);

            Ladder.AssertShows(LadderAnimation.GetOn, 0.75f, m_Climb);
        }

        [Test]
        public void ThenDownTheScreenGoesOn()
        {
            Assert.AreEqual(1f, m_Climb.SteerFrom(-1f));
        }

        [Test]
        public void ThenUpTheScreenGoesBack()
        {
            Assert.AreEqual(-1f, m_Climb.SteerFrom(1f));
        }

        [Test]
        public void ThenGoingBackPlaysGetOffForwards()
        {
            m_Climb.Tick(Ladder.SECOND, Ladder.ON);
            m_Climb.Tick(0.1f, m_Climb.SteerFrom(1f));
            m_Climb.Tick(0.25f, Ladder.ON);

            Ladder.AssertShows(LadderAnimation.GetOff, 0.25f, m_Climb);
        }
    }
}
