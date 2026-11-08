using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Arithmetic
{
    internal sealed class WhenBitsAreFlipped
    {
        private const ushort ADDRESS = 40;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenAByteIsFlipped()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, ADDRESS, 0b0000_1111);

            Flip(FieldScriptOpCode.BitwiseNotByte);

            Assert.AreEqual(0b1111_0000, m_Field.Memory.ReadByte(MemoryBank.Persistent, ADDRESS));
        }

        [Test]
        public void ThenAShortStaysInsideItsWidth()
        {
            m_Field.Memory.WriteUshort(MemoryBank.Persistent, ADDRESS, 0x00FF);
            m_Field.Memory.WriteByte(MemoryBank.Persistent, ADDRESS + 2, 0x11);

            Flip(FieldScriptOpCode.BitwiseNotShort);

            Assert.AreEqual(0xFF00, m_Field.Memory.ReadUshort(MemoryBank.Persistent, ADDRESS));
            Assert.AreEqual(0x11, m_Field.Memory.ReadByte(MemoryBank.Persistent, ADDRESS + 2));
        }

        [Test]
        public void ThenAnIntIsFlipped()
        {
            m_Field.Memory.WriteInt(MemoryBank.Persistent, ADDRESS, 5);

            Flip(FieldScriptOpCode.BitwiseNotInt);

            Assert.AreEqual(~5, m_Field.Memory.ReadInt(MemoryBank.Persistent, ADDRESS));
        }

        [Test]
        public void ThenAULongsTopBitIsReached()
        {
            m_Field.Memory.WriteUlong(MemoryBank.Persistent, ADDRESS, 0UL);

            Flip(FieldScriptOpCode.BitwiseNotULong);

            Assert.AreEqual(ulong.MaxValue, m_Field.Memory.ReadUlong(MemoryBank.Persistent, ADDRESS));
        }

        private void Flip(FieldScriptOpCode opCode)
        {
            m_Field.Run(new Script().Op(opCode).Sources(Source.Persistent, Source.Immediate).UShort(ADDRESS).Return());
        }
    }
}
