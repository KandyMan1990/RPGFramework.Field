using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A script allowing or stopping random battles, which the field keeps on its context.</summary>
    internal sealed class WhenAScriptSwitchesRandomEncounters
    {
        private const ushort MARK = 1000;

        private TestField    m_Field;
        private FieldContext m_Context;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Context = new FieldContext(m_Field.Vm, new FieldEntityRuntime[0], new byte[0], new byte[0], 0);

            m_Field.Vm.RequestSetRandomEncountersEnabled += m_Context.SetRandomEncountersEnabled;
        }

        [Test]
        public void ThenAFieldStartsWithThemAllowed()
        {
            Assert.IsTrue(m_Context.RandomEncountersEnabled);
        }

        [Test]
        public void ThenOneCanStopThem()
        {
            Switch(false);

            Assert.IsFalse(m_Context.RandomEncountersEnabled);
        }

        [Test]
        public void ThenOneCanAllowThemAgain()
        {
            Switch(false);
            Switch(true);

            Assert.IsTrue(m_Context.RandomEncountersEnabled);
        }

        [Test]
        public void ThenTheScriptCarriesOn()
        {
            Switch(false);

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private void Switch(bool enabled)
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.RandomEncounters).Sources(Source.Immediate, Source.Immediate).Bool(enabled).SetByte(MARK, 1).Return());
        }
    }
}
