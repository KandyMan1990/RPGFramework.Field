using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.Tests.GameOpCodes;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>
    /// A field jump reuses the VM: the field left behind loses its scripts, entities and running scripts, and the VM keeps
    /// its table of handlers, a game's included, for the next.
    /// </summary>
    internal sealed class WhenTheVmIsReset
    {
        private const ushort COUNT = 100;
        private const ushort MARK  = 1000;

        private TestField m_Field;
        private int       m_GameRuns;

        [SetUp]
        public void SetUp()
        {
            m_Field    = new TestField(new TestOpCodeDatabase().With(TestGameOpCode.Name, Name));
            m_GameRuns = 0;
        }

        [Test]
        public void ThenTheLastFieldsScriptsNoLongerRun()
        {
            FieldEntityRuntime entity = m_Field.Start(new Script().Count(COUNT, 1).Return());

            m_Field.Vm.Reset();
            m_Field.Tick(entity);

            Assert.AreEqual(0, m_Field.CountAt(COUNT));
        }

        [Test]
        public void ThenTheNextFieldsScriptsRun()
        {
            m_Field.Start(new Script().Count(COUNT, 1).Return());
            m_Field.Vm.Reset();

            m_Field.Run(new Script().SetByte(MARK, 1).Return());

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        [Test]
        public void ThenAGamesOpCodesStillRun()
        {
            m_Field.Vm.Reset();

            m_Field.Run(new Script().UShort((ushort)TestGameOpCode.Name).ULong(1).Return());

            Assert.AreEqual(1, m_GameRuns);
        }

        private void Name(FieldScriptContext context)
        {
            context.ReadName();

            m_GameRuns++;
        }
    }
}
