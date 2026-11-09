using NUnit.Framework;

namespace RPGFramework.Field.Tests.Ladders
{
    /// <summary>What a ladder's footsteps read from a climb under way.</summary>
    internal sealed class WhenALaddersRungsAreClimbed
    {
        [Test]
        public void ThenGettingOnIsNotClimbing()
        {
            Assert.IsFalse(Ladder.Up().IsClimbing);
        }

        [Test]
        public void ThenOnTheRungsItIsClimbing()
        {
            LadderClimb climb = Ladder.Up();

            climb.Tick(Ladder.SECOND, Ladder.ON);

            Assert.IsTrue(climb.IsClimbing);
        }

        [Test]
        public void ThenTheCycleMovesWithTheClimb()
        {
            LadderClimb climb = Ladder.Up();

            climb.Tick(Ladder.SECOND, Ladder.ON);
            climb.Tick(0.25f, Ladder.ON);

            Assert.AreEqual(0.25f, climb.Cycle, 0.001f);
        }

        [Test]
        public void ThenGoingUpRunsTheCycleForward()
        {
            Assert.Greater(Ladder.Up().CycleDirection(Ladder.ON), 0f);
        }

        [Test]
        public void ThenGoingDownRunsTheCycleBackward()
        {
            Assert.Less(Ladder.Down().CycleDirection(Ladder.ON), 0f);
        }

        [Test]
        public void ThenHoldingStillDoesNotRunIt()
        {
            Assert.AreEqual(0f, Ladder.Up().CycleDirection(0f));
        }
    }
}
