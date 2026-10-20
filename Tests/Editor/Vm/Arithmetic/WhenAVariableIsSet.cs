using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Arithmetic
{
    internal sealed class WhenAVariableIsSet
    {
        private const ushort MARK = 1000;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenAByteTakesTheValueWritten()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(42).Return());

            Assert.AreEqual(42, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAByteTakesAnotherVariablesValue()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 20, 99);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetByte).Sources(Source.Persistent, Source.Persistent).UShort(10)
                                    .Variable(VariableWidth.Byte, 20).Return());

            Assert.AreEqual(99, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAByteTakesASessionVariablesValue()
        {
            m_Field.Memory.WriteByte(MemoryBank.Session, 5, 77);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetByte).Sources(Source.Persistent, Source.Session).UShort(10)
                                    .Variable(VariableWidth.Byte, 5).Return());

            Assert.AreEqual(77, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAnAddressPast255IsReached()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetByte).Sources(Source.Persistent, Source.Immediate).UShort(900).Byte(123).Return());

            Assert.AreEqual(123, m_Field.Memory.ReadByte(MemoryBank.Persistent, 900));
        }

        [Test]
        public void ThenAUShortTakesTheValueWritten()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetUShort).Sources(Source.Persistent, Source.Immediate).UShort(40).UShort(40000).Return());

            Assert.AreEqual(40000, m_Field.Memory.ReadUshort(MemoryBank.Persistent, 40));
        }

        [Test]
        public void ThenALongTakesTheValueWritten()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetLong).Sources(Source.Persistent, Source.Immediate).UShort(100).Long(-5).Return());

            Assert.AreEqual(-5L, (long)m_Field.Memory.ReadUlong(MemoryBank.Persistent, 100));
        }

        [Test]
        public void ThenALongsValueIsEightBytes()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetLong).Sources(Source.Persistent, Source.Immediate).UShort(100).Long(-5)
                                    .SetByte(MARK, 1).Return());

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        [Test]
        public void ThenAFloatTakesTheValueWritten()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetFloat).Sources(Source.Persistent, Source.Immediate).UShort(110).Float(1.5f).Return());

            Assert.AreEqual(1.5f, m_Field.Memory.ReadFloat(MemoryBank.Persistent, 110));
        }

        [Test]
        public void ThenABoolTakesTheValueWritten()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetBool).Sources(Source.Persistent, Source.Immediate).UShort(50).Bool(true).Return());

            Assert.IsTrue(m_Field.Memory.ReadBool(MemoryBank.Persistent, 50));
        }

        [Test]
        public void ThenABoolTakesASessionVariablesValue()
        {
            m_Field.Memory.WriteBool(MemoryBank.Persistent, 50, true);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetBool).Sources(Source.Persistent, Source.Session).UShort(50)
                                    .Variable(VariableWidth.Bool, 8).Return());

            Assert.IsFalse(m_Field.Memory.ReadBool(MemoryBank.Persistent, 50));
        }

        [Test]
        public void ThenABoolReadFromAVariableIsAWidthAndAnAddress()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetBool).Sources(Source.Persistent, Source.Session).UShort(50)
                                    .Variable(VariableWidth.Bool, 8).SetByte(MARK, 1).Return());

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }
    }
}
