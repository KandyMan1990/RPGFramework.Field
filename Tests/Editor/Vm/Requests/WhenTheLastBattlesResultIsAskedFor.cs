using NUnit.Framework;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenTheLastBattlesResultIsAskedFor
    {
        private const ushort RESULT = 72;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
            m_Field.Vm.GetLastBattleResult = Escaped;

            m_Field.Run(new Script().Op(FieldScriptOpCode.GetLastBattleResult).Sources(Source.Persistent, Source.Immediate).UShort(RESULT)
                                    .Return());
        }

        [Test]
        public void ThenItIsStoredWhereTheScriptAsks()
        {
            Assert.AreEqual((byte)BattleCompleteState.ESCAPE, m_Field.Memory.ReadByte(MemoryBank.Persistent, RESULT));
        }

        private static BattleCompleteState Escaped()
        {
            return BattleCompleteState.ESCAPE;
        }
    }
}
