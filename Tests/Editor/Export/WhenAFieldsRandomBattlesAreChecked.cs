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

            Assert.IsEmpty(Problems(EncounterTable.ENCOUNTER_NAME, EncounterTable.ARENA_NAME));
        }

        [Test]
        public void ThenATableOnTheRootNamingKnownEncountersAndArenasPasses()
        {
            m_Field = Table();

            Assert.IsEmpty(Problems(EncounterTable.ENCOUNTER_NAME, EncounterTable.ARENA_NAME));
        }

        [Test]
        public void ThenEachSlotNamingAnEncounterNoDatabaseHoldsIsRefused()
        {
            m_Field = Table();

            Assert.AreEqual(FieldEncounters.SLOT_COUNT, Problems("Dragons", EncounterTable.ARENA_NAME).Count);
        }

        [Test]
        public void ThenEachSlotInAnArenaNoDatabaseHoldsIsRefused()
        {
            m_Field = Table();

            Assert.AreEqual(FieldEncounters.SLOT_COUNT, Problems(EncounterTable.ENCOUNTER_NAME, "Cave").Count);
        }

        [Test]
        public void ThenATableAwayFromTheRootIsRefused()
        {
            m_Field = new GameObject("Field");

            Table().transform.SetParent(m_Field.transform);

            Assert.AreEqual(1, Problems(EncounterTable.ENCOUNTER_NAME, EncounterTable.ARENA_NAME).Count);
        }

        private static GameObject Table()
        {
            ulong encounter = EncounterTable.ENCOUNTER;

            return EncounterTable.Make(16, encounter, encounter, encounter, encounter);
        }

        private List<string> Problems(string encounter, string arena)
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateEncounters(m_Field, new List<string> { encounter }, new List<string> { arena }, problems);

            return problems;
        }
    }
}
