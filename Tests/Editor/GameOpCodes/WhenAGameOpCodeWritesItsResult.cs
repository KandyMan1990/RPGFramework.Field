using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.Tests.Vm;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>A game's opcode writes its result to the variable the script names, as the framework's opcodes do.</summary>
    internal sealed class WhenAGameOpCodeWritesItsResult
    {
        private const ushort RESULT = 60;
        private const ushort COPY   = 70;
        private const ushort TEMP   = 8;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField(new TestOpCodeDatabase().With(TestGameOpCode.Write, Write));
        }

        [Test]
        public void ThenAPersistentVariableHoldsIt()
        {
            m_Field.Run(new Script().UShort((ushort)TestGameOpCode.Write).Sources(Source.Persistent, Source.Immediate).UShort(RESULT).Int(42)
                                    .Return());

            Assert.AreEqual(42, m_Field.Memory.ReadInt(MemoryBank.Persistent, RESULT));
        }

        [Test]
        public void ThenTheScriptsOwnTempHoldsIt()
        {
            m_Field.Run(new Script().UShort((ushort)TestGameOpCode.Write).Sources(Source.Temp, Source.Immediate).UShort(TEMP).Int(42)
                                    .Op(FieldScriptOpCode.SetInt).Sources(Source.Persistent, Source.Temp).UShort(COPY).Variable(VariableWidth.Int, TEMP)
                                    .Return());

            Assert.AreEqual(42, m_Field.Memory.ReadInt(MemoryBank.Persistent, COPY));
        }

        private static void Write(FieldScriptContext context)
        {
            FieldScriptDestination result = context.ReadDestination();
            int                    value  = context.ReadInt();

            context.WriteInt(result, value);
        }
    }
}
