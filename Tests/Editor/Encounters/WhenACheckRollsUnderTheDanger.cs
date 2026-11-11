using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>A danger of 10 and a roll of 11: the check raises it to 12 before rolling.</summary>
    internal sealed class WhenACheckRollsUnderTheDanger
    {
        private FakeFieldEncounterStore m_Store;
        private FieldEncounterConfig    m_Config;
        private bool                    m_Battle;

        [SetUp]
        public void SetUp()
        {
            m_Config       = EncounterTable.OneCheckASecond();
            m_Store        = new FakeFieldEncounterStore();
            m_Store.Danger = 10;

            m_Battle = new EncounterMeter(m_Store, m_Config, Rolls.Of(11)).Walk(1f, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Config);
        }

        [Test]
        public void ThenABattleStarts()
        {
            Assert.IsTrue(m_Battle);
        }

        [Test]
        public void ThenTheDangerIsCleared()
        {
            Assert.AreEqual(0, m_Store.Danger);
        }
    }
}
