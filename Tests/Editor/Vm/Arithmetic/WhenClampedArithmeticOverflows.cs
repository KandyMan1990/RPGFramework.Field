using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Arithmetic
{
    /// <summary>Clamped arithmetic holds at the type's limit rather than wrapping.</summary>
    internal sealed class WhenClampedArithmeticOverflows
    {
        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenAByteHoldsAt255()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 250);

            m_Field.Run(new Script().Op(FieldScriptOpCode.AddByteClamped).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(10).Return());

            Assert.AreEqual(255, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAByteHoldsAt0()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 5);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SubtractByteClamped).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(10).Return());

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAUShortHoldsAt65535()
        {
            m_Field.Memory.WriteUshort(MemoryBank.Persistent, 40, 65530);

            m_Field.Run(new Script().Op(FieldScriptOpCode.AddUShortClamped).Sources(Source.Persistent, Source.Immediate).UShort(40).UShort(100).Return());

            Assert.AreEqual(65535, m_Field.Memory.ReadUshort(MemoryBank.Persistent, 40));
        }

        [Test]
        public void ThenAnSByteHoldsAt127()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 120);

            m_Field.Run(new Script().Op(FieldScriptOpCode.AddSByteClamped).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(20).Return());

            Assert.AreEqual(127, (sbyte)m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAnSByteHoldsAtMinus128()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, unchecked((byte)(sbyte)-120));

            m_Field.Run(new Script().Op(FieldScriptOpCode.SubtractSByteClamped).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(20).Return());

            Assert.AreEqual(-128, (sbyte)m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenANegativeSByteIsAdded()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 10);

            m_Field.Run(new Script().Op(FieldScriptOpCode.AddSByteClamped).Sources(Source.Persistent, Source.Immediate).UShort(10)
                                    .Byte(unchecked((byte)(sbyte)-30)).Return());

            Assert.AreEqual(-20, (sbyte)m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenALongHoldsAtItsLargest()
        {
            m_Field.Memory.WriteUlong(MemoryBank.Persistent, 100, long.MaxValue);

            m_Field.Run(new Script().Op(FieldScriptOpCode.AddLongClamped).Sources(Source.Persistent, Source.Immediate).UShort(100).Long(1).Return());

            Assert.AreEqual(long.MaxValue, (long)m_Field.Memory.ReadUlong(MemoryBank.Persistent, 100));
        }

        [Test]
        public void ThenALongHoldsAtItsSmallest()
        {
            m_Field.Memory.WriteUlong(MemoryBank.Persistent, 100, unchecked((ulong)long.MinValue));

            m_Field.Run(new Script().Op(FieldScriptOpCode.SubtractLongClamped).Sources(Source.Persistent, Source.Immediate).UShort(100).Long(1).Return());

            Assert.AreEqual(long.MinValue, (long)m_Field.Memory.ReadUlong(MemoryBank.Persistent, 100));
        }

        [Test]
        public void ThenAULongHoldsAtItsLargest()
        {
            m_Field.Memory.WriteUlong(MemoryBank.Persistent, 100, ulong.MaxValue - 1);

            m_Field.Run(new Script().Op(FieldScriptOpCode.AddULongClamped).Sources(Source.Persistent, Source.Immediate).UShort(100).ULong(5).Return());

            Assert.AreEqual(ulong.MaxValue, m_Field.Memory.ReadUlong(MemoryBank.Persistent, 100));
        }

        [Test]
        public void ThenAULongHoldsAt0()
        {
            m_Field.Memory.WriteUlong(MemoryBank.Persistent, 100, 3);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SubtractULongClamped).Sources(Source.Persistent, Source.Immediate).UShort(100).ULong(5).Return());

            Assert.AreEqual(0UL, m_Field.Memory.ReadUlong(MemoryBank.Persistent, 100));
        }

        [Test]
        public void ThenAnIncrementedIntHoldsAtItsLargest()
        {
            m_Field.Memory.WriteInt(MemoryBank.Persistent, 120, int.MaxValue);

            m_Field.Run(new Script().Op(FieldScriptOpCode.IncrementIntClamped).Sources(Source.Persistent, Source.Immediate).UShort(120).Return());

            Assert.AreEqual(int.MaxValue, m_Field.Memory.ReadInt(MemoryBank.Persistent, 120));
        }

        [Test]
        public void ThenADecrementedShortHoldsAtItsSmallest()
        {
            m_Field.Memory.WriteUshort(MemoryBank.Persistent, 40, 0x8000);

            m_Field.Run(new Script().Op(FieldScriptOpCode.DecrementShortClamped).Sources(Source.Persistent, Source.Immediate).UShort(40).Return());

            Assert.AreEqual(short.MinValue, (short)m_Field.Memory.ReadUshort(MemoryBank.Persistent, 40));
        }

        [Test]
        public void ThenAnIncrementedByteHoldsAt255()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 255);

            m_Field.Run(new Script().Op(FieldScriptOpCode.IncrementByteClamped).Sources(Source.Persistent, Source.Immediate).UShort(10).Return());

            Assert.AreEqual(255, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenADecrementedByteHoldsAt0()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 0);

            m_Field.Run(new Script().Op(FieldScriptOpCode.DecrementByteClamped).Sources(Source.Persistent, Source.Immediate).UShort(10).Return());

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }
    }
}
