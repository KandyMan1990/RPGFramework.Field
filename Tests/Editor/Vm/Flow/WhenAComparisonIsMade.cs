using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Flow
{
    /// <summary>A comparison that holds carries on; one that fails jumps by the distance it carries.</summary>
    internal sealed class WhenAComparisonIsMade
    {
        private const ushort TAKEN = 40;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenEqualVariablesHold()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 5);
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 12, 5);

            Assert.AreEqual(1, Branch(new Script().Op(FieldScriptOpCode.CompareByte).Sources(Source.Persistent, Source.Persistent)
                                                  .Variable(VariableWidth.Byte, 10).Variable(VariableWidth.Byte, 12)
                                                  .Byte((byte)ScriptComparison.Equal)));
        }

        [Test]
        public void ThenUnequalVariablesJump()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 5);
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 12, 6);

            Assert.AreEqual(2, Branch(new Script().Op(FieldScriptOpCode.CompareByte).Sources(Source.Persistent, Source.Persistent)
                                                  .Variable(VariableWidth.Byte, 10).Variable(VariableWidth.Byte, 12)
                                                  .Byte((byte)ScriptComparison.Equal)));
        }

        [Test]
        public void ThenAVariableIsComparedWithAValue()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 5);

            Assert.AreEqual(1, Branch(new Script().Op(FieldScriptOpCode.CompareByte).Sources(Source.Persistent, Source.Immediate)
                                                  .Variable(VariableWidth.Byte, 10).Byte(5).Byte((byte)ScriptComparison.Equal)));
        }

        [Test]
        public void ThenAByteIsUnsigned()
        {
            Assert.AreEqual(2, Branch(new Script().Op(FieldScriptOpCode.CompareByte).Sources(Source.Immediate, Source.Immediate)
                                                  .Byte(255).Byte(1).Byte((byte)ScriptComparison.LessThan)));
        }

        [Test]
        public void ThenAnSByteIsSigned()
        {
            Assert.AreEqual(1, Branch(new Script().Op(FieldScriptOpCode.CompareSByte).Sources(Source.Immediate, Source.Immediate)
                                                  .Byte(255).Byte(1).Byte((byte)ScriptComparison.LessThan)));
        }

        [Test]
        public void ThenAnIntReadsAByteVariable()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 200);

            Assert.AreEqual(1, Branch(new Script().Op(FieldScriptOpCode.CompareInt).Sources(Source.Persistent, Source.Immediate)
                                                  .Variable(VariableWidth.Byte, 10).Int(199).Byte((byte)ScriptComparison.GreaterThan)));
        }

        [Test]
        public void ThenABitPastTheLastIsNeverSet()
        {
            Assert.AreEqual(2, Branch(new Script().Op(FieldScriptOpCode.CompareInt).Sources(Source.Immediate, Source.Immediate)
                                                  .Int(-1).Int(40).Byte((byte)ScriptComparison.BitIsSet)));
        }

        [Test]
        public void ThenFloatsCompare()
        {
            Assert.AreEqual(1, Branch(new Script().Op(FieldScriptOpCode.CompareFloat).Sources(Source.Immediate, Source.Immediate)
                                                  .Float(1.5f).Float(1f).Byte((byte)ScriptComparison.GreaterThan)));
        }

        [TestCase(true,  true,  1)]
        [TestCase(true,  false, 2)]
        public void ThenABoolIsOneByte(bool a, bool b, byte taken)
        {
            Assert.AreEqual(taken, Branch(new Script().Op(FieldScriptOpCode.CompareBool).Sources(Source.Immediate, Source.Immediate)
                                                      .Bool(a).Bool(b).Byte((byte)ScriptComparison.Equal)));
        }

        [Test]
        public void ThenABoolVariableIsCompared()
        {
            m_Field.Memory.WriteBool(MemoryBank.Persistent, 50, true);

            Assert.AreEqual(1, Branch(new Script().Op(FieldScriptOpCode.CompareBool).Sources(Source.Persistent, Source.Immediate)
                                                  .Variable(VariableWidth.Bool, 50).Bool(false).Byte((byte)ScriptComparison.NotEqual)));
        }

        [Test]
        public void ThenAJumpReachesPast65535Bytes()
        {
            // Anything not jumped over is read as RETURN, which ends the script before the mark.
            m_Field.Run(new Script().Op(FieldScriptOpCode.CompareByte).Sources(Source.Immediate, Source.Immediate)
                                    .Byte(1).Byte(2).Byte((byte)ScriptComparison.Equal).Int(70000)
                                    .Bytes(new byte[70000]).SetByte(TAKEN, 2).Return());

            Assert.AreEqual(2, m_Field.Memory.ReadByte(MemoryBank.Persistent, TAKEN));
        }

        /// <summary>
        /// Runs the comparison, then writes 1 if it carried on or 2 if it jumped: the eight it jumps is the first
        /// branch's SET_BYTE and RETURN.
        /// </summary>
        private byte Branch(Script comparison)
        {
            m_Field.Run(comparison.Int(8).SetByte(TAKEN, 1).Return().SetByte(TAKEN, 2).Return());

            return m_Field.Memory.ReadByte(MemoryBank.Persistent, TAKEN);
        }
    }
}
