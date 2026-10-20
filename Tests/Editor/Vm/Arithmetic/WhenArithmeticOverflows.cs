using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Arithmetic
{
    /// <summary>Arithmetic that is not clamped wraps, as the integer types do, and never throws.</summary>
    internal sealed class WhenArithmeticOverflows
    {
        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenAByteAddedPast255Wraps()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 250);

            m_Field.Run(new Script().Op(FieldScriptOpCode.AddByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(10).Return());

            Assert.AreEqual(4, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAByteTakenPast0Wraps()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 5);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SubtractByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(10).Return());

            Assert.AreEqual(251, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAnSByteAddedPast127WrapsToMinus128()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 127);

            m_Field.Run(new Script().Op(FieldScriptOpCode.AddSByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(1).Return());

            Assert.AreEqual(-128, (sbyte)m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAUIntMultipliedPastItsLargestWraps()
        {
            m_Field.Memory.WriteInt(MemoryBank.Persistent, 120, int.MaxValue);

            m_Field.Run(new Script().Op(FieldScriptOpCode.MultiplyUInt).Sources(Source.Persistent, Source.Immediate).UShort(120).UInt(2).Return());

            Assert.AreEqual(0xFFFFFFFE, unchecked((uint)m_Field.Memory.ReadInt(MemoryBank.Persistent, 120)));
        }

        [Test]
        public void ThenAByteIncrementedPast255WrapsTo0()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 255);

            m_Field.Run(new Script().Op(FieldScriptOpCode.IncrementByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Return());

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAByteDecrementedPast0WrapsTo255()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 0);

            m_Field.Run(new Script().Op(FieldScriptOpCode.DecrementByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Return());

            Assert.AreEqual(255, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenMinus128DividedByMinus1Wraps()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 0x80);

            m_Field.Run(new Script().Op(FieldScriptOpCode.DivideSByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(0xFF).Return());

            Assert.AreEqual(-128, (sbyte)m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenTheSmallestLongDividedByMinus1Wraps()
        {
            m_Field.Memory.WriteUlong(MemoryBank.Persistent, 100, unchecked((ulong)long.MinValue));

            m_Field.Run(new Script().Op(FieldScriptOpCode.DivideLong).Sources(Source.Persistent, Source.Immediate).UShort(100).Long(-1).Return());

            Assert.AreEqual(long.MinValue, (long)m_Field.Memory.ReadUlong(MemoryBank.Persistent, 100));
        }

        [Test]
        public void ThenTheSmallestLongsRemainderByMinus1Is0()
        {
            m_Field.Memory.WriteUlong(MemoryBank.Persistent, 100, unchecked((ulong)long.MinValue));

            m_Field.Run(new Script().Op(FieldScriptOpCode.RemainderLong).Sources(Source.Persistent, Source.Immediate).UShort(100).Long(-1).Return());

            Assert.AreEqual(0UL, m_Field.Memory.ReadUlong(MemoryBank.Persistent, 100));
        }
    }
}
