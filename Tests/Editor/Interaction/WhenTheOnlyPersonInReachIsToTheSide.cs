using NUnit.Framework;

namespace RPGFramework.Field.Tests.Interaction
{
    internal sealed class WhenTheOnlyPersonInReachIsToTheSide
    {
        [Test]
        public void ThenTheyAnswer()
        {
            Assert.AreEqual(1, Around.Pick(Around.Person(1, 80f, 1f)));
        }
    }
}
