using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>Init runs until it returns, before the field's first frame, rather than for a frame's instructions.</summary>
    internal sealed class WhenAnInitRunsLongerThanAFrame
    {
        private const ushort RAN = 200;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();

            int init = m_Field.Add(new Script().Count(RAN, FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME * 4).Return());

            m_Entity = m_Field.AddEntity(1, new byte[2], init, m_Field.Add(new Script().Return()));
            m_Entity.RunInitScript(m_Field.Vm);
        }

        [Test]
        public void ThenItRunsToItsReturn()
        {
            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME * 4, m_Field.CountAt(RAN));
        }

        [Test]
        public void ThenItsSlotIsEmptied()
        {
            Assert.IsFalse(m_Entity.IsSlotOccupied((byte)FieldScriptPriority.Main));
        }
    }
}
