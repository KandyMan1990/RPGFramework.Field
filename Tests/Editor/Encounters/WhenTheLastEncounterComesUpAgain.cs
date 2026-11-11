using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>Four slots of encounters 10 to 13, a roll landing on the slot of the last encounter fought.</summary>
    internal sealed class WhenTheLastEncounterComesUpAgain
    {
        private GameObject              m_Field;
        private FieldEncounterSlot[]    m_Slots;
        private FieldEncounterConfig    m_Config;
        private FakeFieldEncounterStore m_Store;

        [SetUp]
        public void SetUp()
        {
            m_Field  = EncounterTable.Make(16, 10, 11, 12, 13);
            m_Slots  = m_Field.GetComponent<FieldEncounters>().Slots;
            m_Config = EncounterTable.Config();
            m_Store  = new FakeFieldEncounterStore();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Field);
            Object.DestroyImmediate(m_Config);
        }

        [Test]
        public void ThenTheNextSlotIsTaken()
        {
            Assert.AreEqual(11UL, Pick(0x00, 10));
        }

        [Test]
        public void ThenALaterRollFallsToTheSlotAfter()
        {
            Assert.AreEqual(12UL, Pick(0x80, 11));
        }

        [Test]
        public void ThenTheFourthIsTakenRegardless()
        {
            Assert.AreEqual(13UL, Pick(0xF0, 13));
        }

        private ulong Pick(byte roll, ulong last)
        {
            m_Store.LastEncounter = last;

            return new EncounterMeter(m_Store, m_Config, Rolls.Of(roll)).Pick(m_Slots).Encounter;
        }
    }
}
