using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>
    /// An init stopped at the ceiling, rather than hanging the field's load. Were the ceiling to go, this would not fail
    /// but hang the editor.
    /// </summary>
    internal sealed class WhenAnInitNeverReturns
    {
        private const int    ENTITY = 1;
        private const ushort MAIN   = 200;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private int                m_Main;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Main   = m_Field.Add(new Script().Count(MAIN, 40).Return());
            m_Entity = m_Field.AddEntity(ENTITY, new byte[2], m_Field.Add(new Script().Goto(-6)), m_Main);

            m_Entity.RunInitScript(m_Field.Vm);
        }

        [Test]
        public void ThenItIsStoppedStillHoldingItsSlot()
        {
            Assert.IsTrue(m_Entity.IsSlotOccupied((byte)FieldScriptPriority.Main));
        }

        [Test]
        public void ThenTheDefaultScriptReplacesIt()
        {
            m_Field.Vm.StartDefaultScript(ENTITY, 1);

            Assert.IsTrue(m_Entity.HoldsScript(m_Main, (byte)FieldScriptPriority.Main));
        }

        [Test]
        public void ThenTheDefaultScriptStartsFromItsBeginning()
        {
            m_Field.Vm.StartDefaultScript(ENTITY, 1);
            m_Field.Tick(m_Entity);

            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME, m_Field.CountAt(MAIN));
        }
    }
}
