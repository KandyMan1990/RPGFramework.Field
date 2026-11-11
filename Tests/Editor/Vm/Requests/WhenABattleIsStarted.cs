using NUnit.Framework;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A battle carries its encounter, its arena, its flags and its transition, and ends the script's frame.</summary>
    internal sealed class WhenABattleIsStarted
    {
        private const BattleFlags FLAGS      = BattleFlags.DISABLE_ESCAPE | BattleFlags.USE_CURRENT_MUSIC;
        private const ulong       ENCOUNTER  = 0x0102030405060708UL;
        private const ulong       ARENA      = 0x8877665544332211UL;
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

            m_Field.Run(new Script().Op(FieldScriptOpCode.StartBattle).ULong(ENCOUNTER).ULong(ARENA).Sources(Source.Immediate, Source.Immediate)
                                    .UShort((ushort)FLAGS).ULong(TRANSITION)
                                    .SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItAsksForItsEncounter()
        {
            Assert.AreEqual(ENCOUNTER, m_Battle.Encounter);
        }

        [Test]
        public void ThenItAsksForItsArena()
        {
            Assert.AreEqual(ARENA, m_Battle.Arena);
        }

        [Test]
        public void ThenItAsksForItsFlags()
        {
            Assert.AreEqual(FLAGS, m_Battle.Flags);
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
