using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;
using RPGFramework.Field.Tests.Encounters;
using UnityEngine;

namespace RPGFramework.Field.Tests.Export
{
    internal sealed class WhenAFieldsRandomBattlesAreChecked
    {
        private GameObject m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Field);
        }

        [Test]
        public void ThenAFieldWithoutATablePasses()
        {
            m_Field = new GameObject("Field");

            Assert.IsEmpty(Problems(EncounterTable.ARENA_NAME));
        }

        [Test]
        public void ThenATableOnTheRootInAKnownArenaPasses()
        {
            m_Field = EncounterTable.Make(15f, 1, 1, 2, 3);

            Assert.IsEmpty(Problems(EncounterTable.ARENA_NAME));
        }

        [Test]
        public void ThenEachSlotInAnArenaNoDatabaseHoldsIsRefused()
        {
            m_Field = EncounterTable.Make(15f, 1, 1, 2, 3);

            Assert.AreEqual(FieldEncounters.SLOT_COUNT, Problems("Cave").Count);
        }

        [Test]
        public void ThenATableAwayFromTheRootIsRefused()
        {
            m_Field = new GameObject("Field");

            EncounterTable.Make(15f, 1, 1, 2, 3).transform.SetParent(m_Field.transform);

            Assert.AreEqual(1, Problems(EncounterTable.ARENA_NAME).Count);
        }

        private List<string> Problems(string arena)
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateEncounters(m_Field, new List<string> { arena }, problems);

            return problems;
        }
    }
}
