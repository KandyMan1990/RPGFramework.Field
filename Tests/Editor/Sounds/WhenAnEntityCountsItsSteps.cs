using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Tests.Ladders;

namespace RPGFramework.Field.Tests.Sounds
{
    /// <summary>Where in its sound's order a walker's next step falls, and a climber's.</summary>
    internal sealed class WhenAnEntityCountsItsSteps
    {
        [Test]
        public void ThenAWalkersStepsCountUp()
        {
            FieldEntityComponents entity = new FieldEntityComponents();

            Assert.AreEqual(0, entity.TakeStep());
            Assert.AreEqual(1, entity.TakeStep());
        }

        [Test]
        public void ThenStoppingStartsThemOver()
        {
            FieldEntityComponents entity = new FieldEntityComponents();

            entity.TakeStep();
            entity.ClearWalkCycle();

            Assert.AreEqual(0, entity.TakeStep());
        }

        [Test]
        public void ThenAClimbersStepsCountUp()
        {
            LadderClimb climb = OnTheRungs();

            Assert.AreEqual(0, climb.TakeStep());
            Assert.AreEqual(1, climb.TakeStep());
        }

        [Test]
        public void ThenHoldingStillOnTheRungsStartsThemOver()
        {
            LadderClimb climb = OnTheRungs();

            climb.TakeStep();
            climb.Tick(0.1f, 0f);

            Assert.AreEqual(0, climb.TakeStep());
        }

        [Test]
        public void ThenClimbingOnKeepsCounting()
        {
            LadderClimb climb = OnTheRungs();

            climb.TakeStep();
            climb.Tick(0.1f, Ladder.ON);

            Assert.AreEqual(1, climb.TakeStep());
        }

        private static LadderClimb OnTheRungs()
        {
            LadderClimb climb = new LadderClimb(Ladder.At(0f), new LadderClimbArgs(true, Ladder.At(1f), Ladder.At(5f), Ladder.At(6f)), 2f, 1f, 1f, 1f);

            climb.Tick(Ladder.SECOND, Ladder.ON);

            return climb;
        }
    }
}
