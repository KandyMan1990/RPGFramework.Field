using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Flow
{
    /// <summary>
    /// IF, ELSE and END_IF as the compiler lays them out: a comparison jumping past the IF body and its jump when it
    /// fails, and the IF body jumping past the ELSE body when it ends.
    /// </summary>
    internal sealed class WhenAnIfHasAnElse
    {
        private const ushort TAKEN = 40;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenAComparisonThatHoldsRunsTheIfBodyAlone()
        {
            m_Field.Run(IfElse(1));

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, TAKEN));
        }

        [Test]
        public void ThenAComparisonThatFailsRunsTheElseBodyAlone()
        {
            m_Field.Run(IfElse(2));

            Assert.AreEqual(2, m_Field.Memory.ReadByte(MemoryBank.Persistent, TAKEN));
        }

        private static Script IfElse(byte value)
        {
            return new Script().Op(FieldScriptOpCode.CompareByte).Sources(Source.Immediate, Source.Immediate)
                               .Byte(1).Byte(value).Byte((byte)ScriptComparison.Equal).Int(12)
                               .SetByte(TAKEN, 1)
                               .Goto(6)
                               .SetByte(TAKEN, 2)
                               .Return();
        }
    }
}
