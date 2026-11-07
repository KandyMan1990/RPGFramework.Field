using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.Tests.Vm;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>A game's opcode runs the handler the game added for it, which reads its arguments as the VM's own do.</summary>
    internal sealed class WhenAGameOpCodeRuns
    {
        private const ushort MARK   = 1000;
        private const ushort AMOUNT = 50;

        private TestField m_Field;
        private byte      m_Count;
        private int       m_Amount;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField(new TestOpCodeDatabase().With(TestGameOpCode.TakeTwo, TakeTwo));
            m_Count  = 0;
            m_Amount = 0;

            m_Field.Memory.WriteInt(MemoryBank.Persistent, AMOUNT, 1234);

            m_Field.Run(new Script().UShort((ushort)TestGameOpCode.TakeTwo).Sources(Source.Immediate, Source.Persistent).Byte(7).UShort(AMOUNT)
                                    .SetByte(MARK, 1)
                                    .Return());
        }

        [Test]
        public void ThenItsHandlerReadsAWrittenArgument()
        {
            Assert.AreEqual(7, m_Count);
        }

        [Test]
        public void ThenItsHandlerReadsAnArgumentFromAVariable()
        {
            Assert.AreEqual(1234, m_Amount);
        }

        [Test]
        public void ThenTheNextInstructionRuns()
        {
            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private void TakeTwo(FieldScriptContext context)
        {
            m_Count  = context.ReadByte();
            m_Amount = context.ReadInt();
        }
    }
}
