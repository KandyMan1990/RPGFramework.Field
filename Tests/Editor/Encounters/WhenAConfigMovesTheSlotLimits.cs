using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>Four slots of encounters 10 to 13, a config dividing the roll at 64, 128 and 192.</summary>
    internal sealed class WhenAConfigMovesTheSlotLimits
    {
        private GameObject              m_Field;
        private FieldEncounterConfig    m_Config;
        private FakeFieldEncounterStore m_Store;

        [SetUp]
        public void SetUp()
        {
            m_Field  = EncounterTable.Make(16, 10, 11, 12, 13);
            m_Config = EncounterTable.SlotRollLimits(64, 128, 192);
            m_Store  = new FakeFieldEncounterStore();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Field);
            Object.DestroyImmediate(m_Config);
        }

        [TestCase(0x3F, 10)]
        [TestCase(0x40, 11)]
        [TestCase(0x80, 12)]
        [TestCase(0xC0, 13)]
        public void ThenTheRollChoosesByItsLimits(int roll, int encounter)
        {
            FieldEncounterSlot[] slots = m_Field.GetComponent<FieldEncounters>().Slots;

            Assert.AreEqual((ulong)encounter, new EncounterMeter(m_Store, m_Config, Rolls.Of((byte)roll)).Pick(slots).Encounter);
        }
    }
}
