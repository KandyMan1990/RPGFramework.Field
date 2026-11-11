using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>Four slots of encounters 10 to 13, none of them the last encounter fought, divided as a config is made.</summary>
    internal sealed class WhenASlotIsPicked
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

            m_Store.LastEncounter = 99;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Field);
            Object.DestroyImmediate(m_Config);
        }

        [TestCase(0x00, 10)]
        [TestCase(0x7F, 10)]
        [TestCase(0x80, 11)]
        [TestCase(0xBF, 11)]
        [TestCase(0xC0, 12)]
        [TestCase(0xEF, 12)]
        [TestCase(0xF0, 13)]
        [TestCase(0xFF, 13)]
        public void ThenTheRollChoosesTheSlot(int roll, int encounter)
        {
            Assert.AreEqual((ulong)encounter, Pick((byte)roll).Encounter);
        }

        [Test]
        public void ThenItIsFoughtInTheSlotsArena()
        {
            Assert.AreEqual(EncounterTable.ARENA, Pick(0).Arena);
        }

        [Test]
        public void ThenItsEncounterIsRemembered()
        {
            Pick(0x80);

            Assert.AreEqual(11UL, m_Store.LastEncounter);
        }

        private FieldEncounterSlot Pick(byte roll)
        {
            return new EncounterMeter(m_Store, m_Config, Rolls.Of(roll)).Pick(m_Slots);
        }
    }
}
