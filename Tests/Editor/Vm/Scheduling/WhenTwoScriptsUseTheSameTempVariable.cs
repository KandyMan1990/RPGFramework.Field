using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>
    /// Main writes its temp variable and yields; a more urgent script writes its own of the same address and copies it
    /// out; then Main copies out its own.
    /// </summary>
    internal sealed class WhenTwoScriptsUseTheSameTempVariable
    {
        private const ushort TEMP         = 5;
        private const ushort MAINS        = 60;
        private const ushort INTERRUPTERS = 61;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();

            int main        = m_Field.Add(CopyTemp(WriteTemp(new Script(), 42).Yield(), MAINS).Return());
            int interrupter = m_Field.Add(CopyTemp(WriteTemp(new Script(), 99), INTERRUPTERS).Return());

            FieldEntityRuntime entity = m_Field.AddEntity(1, new byte[] { 0, 1 }, main, interrupter);

            m_Field.Tick(entity);
            entity.TryRequestScript(interrupter, 1);
            m_Field.Tick(entity);
        }

        [Test]
        public void ThenEachKeepsItsOwnValue()
        {
            Assert.AreEqual(42, m_Field.Memory.ReadByte(MemoryBank.Persistent, MAINS));
        }

        [Test]
        public void ThenTheOtherReadsItsOwnValue()
        {
            Assert.AreEqual(99, m_Field.Memory.ReadByte(MemoryBank.Persistent, INTERRUPTERS));
        }

        private static Script WriteTemp(Script script, byte value)
        {
            return script.Op(FieldScriptOpCode.SetByte).Sources(Source.Temp, Source.Immediate).UShort(TEMP).Byte(value);
        }

        private static Script CopyTemp(Script script, ushort to)
        {
            return script.Op(FieldScriptOpCode.SetByte).Sources(Source.Persistent, Source.Temp).UShort(to).Variable(VariableWidth.Byte, TEMP);
        }
    }
}
