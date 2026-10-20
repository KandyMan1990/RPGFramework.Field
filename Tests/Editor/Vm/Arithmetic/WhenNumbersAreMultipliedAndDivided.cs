using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Arithmetic
{
    internal sealed class WhenNumbersAreMultipliedAndDivided
    {
        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenBytesMultiply()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 6);

            m_Field.Run(new Script().Op(FieldScriptOpCode.MultiplyByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(7).Return());

            Assert.AreEqual(42, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAByteDividedLosesItsRemainder()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 42);

            m_Field.Run(new Script().Op(FieldScriptOpCode.DivideByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(5).Return());

            Assert.AreEqual(8, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAByteTakesItsRemainder()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 43);

            m_Field.Run(new Script().Op(FieldScriptOpCode.RemainderByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(5).Return());

            Assert.AreEqual(3, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenANegativeIntDividedRoundsTowards0()
        {
            m_Field.Memory.WriteInt(MemoryBank.Persistent, 120, -7);

            m_Field.Run(new Script().Op(FieldScriptOpCode.DivideInt).Sources(Source.Persistent, Source.Immediate).UShort(120).Int(2).Return());

            Assert.AreEqual(-3, m_Field.Memory.ReadInt(MemoryBank.Persistent, 120));
        }

        [Test]
        public void ThenANegativeIntsRemainderTakesItsSign()
        {
            m_Field.Memory.WriteInt(MemoryBank.Persistent, 120, -7);

            m_Field.Run(new Script().Op(FieldScriptOpCode.RemainderInt).Sources(Source.Persistent, Source.Immediate).UShort(120).Int(2).Return());

            Assert.AreEqual(-1, m_Field.Memory.ReadInt(MemoryBank.Persistent, 120));
        }

        [Test]
        public void ThenFloatsAddMultiplyAndDivide()
        {
            m_Field.Memory.WriteFloat(MemoryBank.Persistent, 110, 3f);

            m_Field.Run(new Script().Op(FieldScriptOpCode.AddFloat).Sources(Source.Persistent, Source.Immediate).UShort(110).Float(0.5f)
                                    .Op(FieldScriptOpCode.MultiplyFloat).Sources(Source.Persistent, Source.Immediate).UShort(110).Float(2f)
                                    .Op(FieldScriptOpCode.DivideFloat).Sources(Source.Persistent, Source.Immediate).UShort(110).Float(4f)
                                    .Return());

            Assert.AreEqual(1.75f, m_Field.Memory.ReadFloat(MemoryBank.Persistent, 110));
        }
    }
}
