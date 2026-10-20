using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    internal sealed class WhenAScriptReadsTempItNeverWrote
    {
        private const ushort COPY = 61;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
            m_Field.Memory.WriteByte(MemoryBank.Persistent, COPY, 0xFF);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetByte).Sources(Source.Persistent, Source.Temp).UShort(COPY)
                                    .Variable(VariableWidth.Byte, 5).Return());
        }

        [Test]
        public void ThenItReadsZero()
        {
            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, COPY));
        }
    }
}
