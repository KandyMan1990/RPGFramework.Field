using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    internal sealed class WhenThePlayerWalksShortOfACheck
    {
        private FakeFieldEncounterStore m_Store;
        private FieldEncounterConfig    m_Config;
        private bool                    m_Battle;

        [SetUp]
        public void SetUp()
        {
            m_Config = EncounterTable.OneCheckASecond();
            m_Store  = new FakeFieldEncounterStore();
            m_Battle = new EncounterMeter(m_Store, m_Config, Rolls.Of()).Walk(0.5f, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Config);
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
