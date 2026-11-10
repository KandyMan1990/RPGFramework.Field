using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>Four slots of groups 10 to 13, a roll landing on the slot of the last group fought.</summary>
    internal sealed class WhenTheLastGroupComesUpAgain
    {
        private GameObject              m_Field;
        private FieldEncounterSlot[]    m_Slots;
        private FakeFieldEncounterStore m_Store;

        [SetUp]
        public void SetUp()
        {
            m_Field = EncounterTable.Make(10f, 10, 11, 12, 13);
            m_Slots = m_Field.GetComponent<FieldEncounters>().Slots;
            m_Store = new FakeFieldEncounterStore();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Field);
        }

        [Test]
        public void ThenTheNextSlotIsTaken()
        {
            Assert.AreEqual(11, Pick(0x00, 10).EnemyGroup);
        }

        [Test]
        public void ThenALaterRollFallsToTheSlotAfter()
        {
            Assert.AreEqual(12, Pick(0x80, 11).EnemyGroup);
        }

        [Test]
        public void ThenTheFourthIsTakenRegardless()
        {
            Assert.AreEqual(13, Pick(0xF0, 13).EnemyGroup);
        }

        private FieldEncounterSlot Pick(byte roll, ushort last)
        {
            m_Store.LastEnemyGroup = last;

            return new EncounterMeter(m_Store, Rolls.Of(roll)).Pick(m_Slots);
        }
    }
}
