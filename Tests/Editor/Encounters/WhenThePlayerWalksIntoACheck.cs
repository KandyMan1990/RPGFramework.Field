using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>Three quarters of the way to a check, half a check more, and a roll of 255.</summary>
    internal sealed class WhenThePlayerWalksIntoACheck
    {
        private FakeFieldEncounterStore m_Store;
        private FieldEncounterConfig    m_Config;
        private bool                    m_Battle;

        [SetUp]
        public void SetUp()
        {
            m_Config             = EncounterTable.OneCheckASecond();
            m_Store              = new FakeFieldEncounterStore();
            m_Store.StepFraction = 0.75f;

            m_Battle = new EncounterMeter(m_Store, m_Config, Rolls.Of(255)).Walk(0.5f, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Config);
        }

        [Test]
        public void ThenTheDangerRisesByTwo()
        {
            Assert.AreEqual(2, m_Store.Danger);
        }

        [Test]
        public void ThenARollAboveTheDangerStartsNoBattle()
        {
            Assert.IsFalse(m_Battle);
        }

        [Test]
        public void ThenTheRestCountsTowardTheNext()
        {
            Assert.AreEqual(0.25f, m_Store.StepFraction, 1e-5f);
        }
    }
}
