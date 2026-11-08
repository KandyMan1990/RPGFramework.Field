using NUnit.Framework;

namespace RPGFramework.Field.Tests.Ladders
{
    /// <summary>The player, on the bottom of a ladder they are going up.</summary>
    internal sealed class WhenThePlayerSteersALadder
    {
        private const float UP_THE_SCREEN   = 1f;
        private const float DOWN_THE_SCREEN = -1f;

        private LadderClimb m_Climb;

        [SetUp]
        public void SetUp()
        {
            m_Climb = Ladder.Up();
            m_Climb.Tick(Ladder.SECOND, Ladder.ON);
        }

        [Test]
        public void ThenUpTheScreenClimbs()
        {
            m_Climb.Tick(0.5f, m_Climb.SteerFrom(UP_THE_SCREEN));

            Ladder.AssertIsAt(2f, m_Climb);
        }

        [Test]
        public void ThenLettingGoStaysPut()
        {
            m_Climb.Tick(0.5f, m_Climb.SteerFrom(UP_THE_SCREEN));
            m_Climb.Tick(5f, m_Climb.SteerFrom(0f));

            Ladder.AssertIsAt(2f, m_Climb);
        }

        [Test]
        public void ThenLettingGoHoldsTheAnimation()
        {
            m_Climb.Tick(0.25f, m_Climb.SteerFrom(UP_THE_SCREEN));
            m_Climb.Tick(5f, m_Climb.SteerFrom(0f));

            Ladder.AssertShows(LadderAnimation.Climb, 0.25f, m_Climb);
        }

        [Test]
        public void ThenALightPushIsNoPush()
        {
            Assert.AreEqual(0f, m_Climb.SteerFrom(0.2f));
        }

        [Test]
        public void ThenComingBackDownRunsTheAnimationBackwards()
        {
            m_Climb.Tick(0.5f, m_Climb.SteerFrom(UP_THE_SCREEN));
            m_Climb.Tick(0.25f, m_Climb.SteerFrom(DOWN_THE_SCREEN));

            Ladder.AssertShows(LadderAnimation.Climb, 0.25f, m_Climb);
        }

        [Test]
        public void ThenGoingBackPastTheBottomGetsOffBackwards()
        {
            GoBackPastTheBottom();
            m_Climb.Tick(0.25f, Ladder.ON);

            Ladder.AssertShows(LadderAnimation.GetOn, 0.75f, m_Climb);
        }

        [Test]
        public void ThenGoingBackPastTheBottomFinishesWhereItBegan()
        {
            GoBackPastTheBottom();
            m_Climb.Tick(Ladder.SECOND, Ladder.ON);

            Assert.IsTrue(m_Climb.IsFinished);
            Ladder.AssertIsAt(0f, m_Climb);
        }

        private void GoBackPastTheBottom()
        {
            m_Climb.Tick(0.1f, m_Climb.SteerFrom(DOWN_THE_SCREEN));
        }
    }
}
