using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>A config raising the danger by 10 a check, and a check walked into.</summary>
    internal sealed class WhenAConfigSetsTheDangerStep
    {
        private FakeFieldEncounterStore m_Store;
        private FieldEncounterConfig    m_Config;

        [SetUp]
        public void SetUp()
        {
            m_Store  = new FakeFieldEncounterStore();
            m_Config = EncounterTable.DangerPerCheckWalking(10);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Config);
        }

        [Test]
        public void ThenTheDangerRisesByIt()
        {
            new EncounterMeter(m_Store, m_Config, Rolls.Of(255)).Walk(1f, 1f);

            Assert.AreEqual(10, m_Store.Danger);
        }

        [Test]
        public void ThenARollUnderItStartsABattle()
        {
            Assert.IsTrue(new EncounterMeter(m_Store, m_Config, Rolls.Of(9)).Walk(1f, 1f));
        }
    }
}
