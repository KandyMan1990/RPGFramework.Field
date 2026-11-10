using NUnit.Framework;
using RPGFramework.Battle.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>
    /// Every slot group 7, a danger no roll can pass, and a battle mode holding a random battle's flag and a game-over
    /// rule.
    /// </summary>
    internal sealed class WhenARandomBattleStarts
    {
        private const BattleFlags MODE = BattleFlags.USE_CURRENT_MUSIC | BattleFlags.DISABLE_GAME_OVER_ON_DEFEAT;

        private FakeFieldEncounterStore m_Store;
        private GameObject              m_Field;
        private bool                    m_Started;
        private BattleArgs              m_Battle;

        [SetUp]
        public void SetUp()
        {
            m_Store        = new FakeFieldEncounterStore();
            m_Store.Danger = 300;
            m_Field        = EncounterTable.Make(10f, 7, 7, 7, 7);

            IFieldEncounterDriver driver = new RandomFieldEncounterDriver(m_Store, new FakeBattleModeStore(MODE));

            driver.Enter(m_Field);

            m_Started = driver.TryStartBattle(new PlayerWalkArgs(5f, 1f, Vector3.zero), out m_Battle);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Field);
        }

        [Test]
        public void ThenOneStarts()
        {
            Assert.IsTrue(m_Started);
        }

        [Test]
        public void ThenItFightsTheSlotsEnemies()
        {
            Assert.AreEqual(7, m_Battle.EnemyGroup);
        }

        [Test]
        public void ThenItIsFoughtInTheSlotsArena()
        {
            Assert.AreEqual(EncounterTable.ARENA, m_Battle.Arena);
        }

        [Test]
        public void ThenItTakesTheModesBattleFlagsAlone()
        {
            Assert.AreEqual(BattleFlags.USE_CURRENT_MUSIC, m_Battle.Flags);
        }

        [Test]
        public void ThenTheRouterChoosesItsCover()
        {
            Assert.AreEqual(0UL, m_Battle.Transition);
        }

        [Test]
        public void ThenTheDangerIsCleared()
        {
            Assert.AreEqual(0, m_Store.Danger);
        }
    }
}
