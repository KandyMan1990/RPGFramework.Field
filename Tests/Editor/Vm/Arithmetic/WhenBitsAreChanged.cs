using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.Field.Tests.Vm.Arithmetic
{
    internal sealed class WhenBitsAreChanged
    {
        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [TestCase(FieldScriptOpCode.BitwiseAndUShort, (ushort)0b1010, (ushort)0b1000)]
        [TestCase(FieldScriptOpCode.BitwiseOrUShort,  (ushort)0b0001, (ushort)0b1101)]
        [TestCase(FieldScriptOpCode.BitwiseXorUShort, (ushort)0b1111, (ushort)0b0011)]
        public void ThenAUShortIsCombinedBitByBit(FieldScriptOpCode opCode, ushort operand, ushort result)
        {
            m_Field.Memory.WriteUshort(MemoryBank.Persistent, 40, 0b1100);

            m_Field.Run(new Script().Op(opCode).Sources(Source.Persistent, Source.Immediate).UShort(40).UShort(operand).Return());

            Assert.AreEqual(result, m_Field.Memory.ReadUshort(MemoryBank.Persistent, 40));
        }

        [Test]
        public void ThenABitIsSet()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetBitByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(3).Return());

            Assert.AreEqual(0b0000_1000, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenABitIsCleared()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 0b0000_1001);

            m_Field.Run(new Script().Op(FieldScriptOpCode.UnsetBitByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(3).Return());

            Assert.AreEqual(0b0000_0001, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAULongsTopBitIsReached()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetBitULong).Sources(Source.Persistent, Source.Immediate).UShort(100).Byte(63).Return());

            Assert.AreEqual(1UL << 63, m_Field.Memory.ReadUlong(MemoryBank.Persistent, 100));
        }

        [Test]
        public void ThenABitPastTheLastIsReportedAndNothingChanges()
        {
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("Bit index [9] is out of range")));

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetBitByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(9).Return());

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }
    }
}
