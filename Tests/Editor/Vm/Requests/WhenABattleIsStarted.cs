using NUnit.Framework;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A battle carries its own arena, enemies, flags, level and transition, and ends the script's frame.</summary>
    internal sealed class WhenABattleIsStarted
    {
        private const BattleFlags FLAGS      = BattleFlags.DISABLE_ESCAPE | BattleFlags.USE_CURRENT_MUSIC;
        private const ulong       TRANSITION = 0x1122334455667788UL;
        private const ushort      MARK       = 1000;

        private TestField  m_Field;
        private BattleArgs m_Battle;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Battle = default;

            m_Field.Vm.RequestStartBattle += OnStartBattle;

            m_Field.Run(new Script().Op(FieldScriptOpCode.StartBattle).Sources(Source.Immediate, Source.Immediate).UShort(7).UShort(3)
                                    .Sources(Source.Immediate, Source.Immediate).UShort((ushort)FLAGS).Byte(12).ULong(TRANSITION)
                                    .SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItAsksForItsArena()
        {
            Assert.AreEqual(7, m_Battle.Arena);
        }

        [Test]
        public void ThenItAsksForItsEnemies()
        {
            Assert.AreEqual(3, m_Battle.EnemyGroup);
        }

        [Test]
        public void ThenItAsksForItsFlags()
        {
            Assert.AreEqual(FLAGS, m_Battle.Flags);
        }

        [Test]
        public void ThenItAsksForItsEnemiesLevel()
        {
            Assert.AreEqual(12, m_Battle.EnemyLevel);
        }

        [Test]
        public void ThenItAsksForItsTransition()
        {
            Assert.AreEqual(TRANSITION, m_Battle.Transition);
        }

        [Test]
        public void ThenTheScriptsFrameEnds()
        {
            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private void OnStartBattle(BattleArgs battle)
        {
            m_Battle = battle;
        }
    }
}
