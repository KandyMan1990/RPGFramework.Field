using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.Tests.Vm;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>
    /// A game's opcode its database left out, among ones it added, stops the script, as an opcode the framework has no
    /// handler for does.
    /// </summary>
    internal sealed class WhenAGameOpCodeWasNotAdded
    {
        private const ushort MARK = 1000;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField(new TestOpCodeDatabase().With(TestGameOpCode.TakeTwo, Nothing).With(TestGameOpCode.Name, Nothing));

            m_Field.Run(new Script().UShort((ushort)TestGameOpCode.Write).Sources(Source.Persistent, Source.Immediate).UShort(60).Int(42)
                                    .SetByte(MARK, 1)
                                    .Return());
        }

        [Test]
        public void ThenTheScriptStopsThere()
        {
            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private static void Nothing(FieldScriptContext context)
        {
        }
    }
}
