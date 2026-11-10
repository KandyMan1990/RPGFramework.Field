using NUnit.Framework;
using RPGFramework.Battle.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>A danger no roll can pass, so only the field's table or the rate can stop the battle.</summary>
    internal sealed class WhenARandomBattleCannotCome
    {
        private FakeFieldEncounterStore m_Store;
        private IFieldEncounterDriver   m_Driver;
        private GameObject              m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Store        = new FakeFieldEncounterStore();
            m_Store.Danger = 300;
            m_Driver       = new RandomFieldEncounterDriver(m_Store, new FakeBattleModeStore(BattleFlags.DEFAULT));
            m_Field        = null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Field);
        }

        [Test]
        public void ThenAFieldWithoutATableHasNone()
        {
            m_Field = new GameObject("Field");

            Assert.IsFalse(Walk());
        }

        [Test]
        public void ThenATableOfNoSecondsHasNone()
        {
            m_Field = EncounterTable.Make(0f, 7, 7, 7, 7);

            Assert.IsFalse(Walk());
        }

        [Test]
        public void ThenARateOfNoneHasNone()
        {
            m_Field      = EncounterTable.Make(10f, 7, 7, 7, 7);
            m_Store.Rate = 0f;

            Assert.IsFalse(Walk());
        }

        private bool Walk()
        {
            m_Driver.Enter(m_Field);

            return m_Driver.TryStartBattle(new PlayerWalkArgs(5f, 1f, Vector3.zero), out _);
        }
    }
}
