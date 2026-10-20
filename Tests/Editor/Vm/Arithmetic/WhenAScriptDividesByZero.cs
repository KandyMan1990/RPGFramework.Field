using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.Field.Tests.Vm.Arithmetic
{
    /// <summary>A division by zero is an authoring mistake the VM reports, leaving the value as it was.</summary>
    internal sealed class WhenAScriptDividesByZero
    {
        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenADividedByteIsLeftAlone()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 43);
            LogAssert.Expect(LogType.Error, new Regex("Divide by zero"));

            m_Field.Run(new Script().Op(FieldScriptOpCode.DivideByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(0).Return());

            Assert.AreEqual(43, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenAByteTakingARemainderIsLeftAlone()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 10, 43);
            LogAssert.Expect(LogType.Error, new Regex("Modulo by zero"));

            m_Field.Run(new Script().Op(FieldScriptOpCode.RemainderByte).Sources(Source.Persistent, Source.Immediate).UShort(10).Byte(0).Return());

            Assert.AreEqual(43, m_Field.Memory.ReadByte(MemoryBank.Persistent, 10));
        }

        [Test]
        public void ThenADividedFloatIsLeftAlone()
        {
            m_Field.Memory.WriteFloat(MemoryBank.Persistent, 110, 1.75f);
            LogAssert.Expect(LogType.Error, new Regex("Divide by zero"));

            m_Field.Run(new Script().Op(FieldScriptOpCode.DivideFloat).Sources(Source.Persistent, Source.Immediate).UShort(110).Float(0f).Return());

            Assert.AreEqual(1.75f, m_Field.Memory.ReadFloat(MemoryBank.Persistent, 110));
        }
    }
}
