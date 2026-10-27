using NUnit.Framework;

namespace RPGFramework.Field.Tests.Interaction
{
    internal sealed class WhenTheOnlyPersonIsOutOfReach
    {
        [Test]
        public void ThenNobodyAnswers()
        {
            Assert.AreEqual(InteractionSearch.NONE, Around.Pick(Around.Person(1, 0f, Around.REACH + 0.5f)));
        }
    }
}
