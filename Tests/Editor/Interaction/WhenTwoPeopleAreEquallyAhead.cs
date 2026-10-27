using NUnit.Framework;

namespace RPGFramework.Field.Tests.Interaction
{
    internal sealed class WhenTwoPeopleAreEquallyAhead
    {
        [Test]
        public void ThenTheLowerIdAnswers()
        {
            Assert.AreEqual(3, Around.Pick(Around.Person(7, 30f, 1f), Around.Person(3, -30f, 1f)));
        }
    }
}
