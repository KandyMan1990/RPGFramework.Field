using NUnit.Framework;

namespace RPGFramework.Field.Tests.Encounters
{
    internal sealed class WhenThePlayerWalksShortOfACheck
    {
        private FakeFieldEncounterStore m_Store;
        private bool                    m_Battle;

        [SetUp]
        public void SetUp()
        {
            m_Store  = new FakeFieldEncounterStore();
            m_Battle = new EncounterMeter(m_Store, Rolls.Of()).Walk(0.5f, 1f);
        }

        [Test]
        public void ThenNoBattleStarts()
        {
            Assert.IsFalse(m_Battle);
        }

        [Test]
        public void ThenTheDangerIsUnchanged()
        {
            Assert.AreEqual(0, m_Store.Danger);
        }

        [Test]
        public void ThenTheWalkCountsTowardTheNext()
        {
            Assert.AreEqual(0.5f, m_Store.StepFraction, 1e-5f);
        }
    }
}
