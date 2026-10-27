using NUnit.Framework;

namespace RPGFramework.Field.Tests.Interaction
{
    /// <summary>Within reach along the ground, but higher up: on a step, or on the floor above.</summary>
    internal sealed class WhenSomeoneStandsAboveThePlayer
    {
        [Test]
        public void ThenOneOnAStepAnswers()
        {
            Assert.AreEqual(1, Around.Pick(Around.Person(1, 0f, 1f, 0.5f)));
        }

        [Test]
        public void ThenOneOnTheFloorAboveDoesNot()
        {
            Assert.AreEqual(InteractionSearch.NONE, Around.Pick(Around.Person(1, 0f, 1f, 3f)));
        }
    }
}
