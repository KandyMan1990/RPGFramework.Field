using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    internal sealed class WhenAWalkCoversSeveralChecks
    {
        private FakeFieldEncounterStore m_Store;
        private FieldEncounterConfig    m_Config;

        [SetUp]
        public void SetUp()
        {
            m_Config = EncounterTable.OneCheckASecond();
            m_Store  = new FakeFieldEncounterStore();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Config);
        }

        [Test]
        public void ThenEachIsTaken()
        {
            new EncounterMeter(m_Store, m_Config, Rolls.Of(255, 255, 255)).Walk(3.5f, 1f);

            Assert.AreEqual(6, m_Store.Danger);
        }

        [Test]
        public void ThenWhatIsLeftCountsTowardTheNext()
        {
            new EncounterMeter(m_Store, m_Config, Rolls.Of(255, 255, 255)).Walk(3.5f, 1f);

            Assert.AreEqual(0.5f, m_Store.StepFraction, 1e-5f);
        }

        [Test]
        public void ThenNoneIsTakenAfterOneStartsABattle()
        {
            Assert.IsTrue(new EncounterMeter(m_Store, m_Config, Rolls.Of(0)).Walk(3f, 1f));
        }
    }
}
