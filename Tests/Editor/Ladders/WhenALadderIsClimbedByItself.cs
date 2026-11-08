using NUnit.Framework;

namespace RPGFramework.Field.Tests.Ladders
{
    /// <summary>A climb always going on, as anything but the player's is, from the start of the ladder.</summary>
    internal sealed class WhenALadderIsClimbedByItself
    {
        private LadderClimb m_Climb;

        [SetUp]
        public void SetUp()
        {
            m_Climb = Ladder.Up();
        }

        [Test]
        public void ThenHalfWayThroughGettingOnItIsHalfWayToTheLadder()
        {
            m_Climb.Tick(0.5f, Ladder.ON);

            Ladder.AssertIsAt(0.5f, m_Climb);
        }

        [Test]
        public void ThenGettingOnShowsTheGetOnAnimation()
        {
            m_Climb.Tick(0.5f, Ladder.ON);

            Ladder.AssertShows(LadderAnimation.GetOn, 0.5f, m_Climb);
        }

        [Test]
        public void ThenHalfWayUpTheClimbItIsHalfWayUpTheLadder()
        {
            m_Climb.Tick(Ladder.SECOND, Ladder.ON);
            m_Climb.Tick(1f, Ladder.ON);

            Ladder.AssertIsAt(3f, m_Climb);
        }

        [Test]
        public void ThenTheClimbShowsTheClimbAnimation()
        {
            m_Climb.Tick(Ladder.SECOND, Ladder.ON);
            m_Climb.Tick(0.25f, Ladder.ON);

            Ladder.AssertShows(LadderAnimation.Climb, 0.25f, m_Climb);
        }

        [Test]
        public void ThenGettingOffShowsTheGetOffAnimation()
        {
            GetToTheTop();
            m_Climb.Tick(0.5f, Ladder.ON);

            Ladder.AssertShows(LadderAnimation.GetOff, 0.5f, m_Climb);
        }

        [Test]
        public void ThenItIsNotFinishedWhileGettingOff()
        {
            GetToTheTop();
            m_Climb.Tick(0.5f, Ladder.ON);

            Assert.IsFalse(m_Climb.IsFinished);
        }

        [Test]
        public void ThenItFinishesAtTheLastPoint()
        {
            GetToTheTop();
            m_Climb.Tick(Ladder.SECOND, Ladder.ON);

            Assert.IsTrue(m_Climb.IsFinished);
            Ladder.AssertIsAt(6f, m_Climb);
        }

        private void GetToTheTop()
        {
            m_Climb.Tick(Ladder.SECOND, Ladder.ON);
            m_Climb.Tick(2f, Ladder.ON);
        }
    }
}
