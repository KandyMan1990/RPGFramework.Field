using NUnit.Framework;

namespace RPGFramework.Field.Tests.Interaction
{
    /// <summary>One near and well to the side, one further off but nearly straight ahead.</summary>
    internal sealed class WhenTwoPeopleAreInReach
    {
        [Test]
        public void ThenTheOneMoreNearlyAheadAnswers()
        {
            Assert.AreEqual(2, Around.Pick(Around.Person(1, 40f, 0.5f), Around.Person(2, -10f, 1.8f)));
        }
    }
}
