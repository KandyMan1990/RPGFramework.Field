using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>An entity is one actor: only its most urgent occupied slot runs, the slots below waiting untouched.</summary>
    internal sealed class WhenAMoreUrgentSlotHoldsAScript
    {
        private const ushort LOWER = 200;
        private const ushort UPPER = 204;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Entity = m_Field.Start(new Script().Count(LOWER, 40).Return());

            m_Entity.TryRequestScript(m_Field.Add(new Script().Count(UPPER, 40).Return()), 1);
            m_Field.Tick(m_Entity);
        }

        [Test]
        public void ThenItRunsAlone()
        {
            Assert.AreEqual(0, m_Field.CountAt(LOWER));
        }

        [Test]
        public void ThenItHasTheFramesInstructions()
        {
            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME, m_Field.CountAt(UPPER));
        }

        [Test]
        public void ThenTheScriptBelowBeginsOnceTheSlotIsEmpty()
        {
            m_Entity.ClearSlot(1);
            m_Field.Tick(m_Entity);

            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME, m_Field.CountAt(LOWER));
        }
    }
}
