using NUnit.Framework;

namespace RPGFramework.Field.Tests.Interaction
{
    internal sealed class WhenTheOnlyPersonInReachIsBehind
    {
        [Test]
        public void ThenNobodyAnswers()
        {
            Assert.AreEqual(InteractionSearch.NONE, Around.Pick(Around.Person(1, 120f, 1f)));
        }
    }
}
