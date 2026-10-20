using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    internal sealed class WhenAScriptRunsWithoutWaiting
    {
        private const ushort RAN = 200;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Entity = m_Field.Run(new Script().Count(RAN, FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME * 2).Return());
        }

        [Test]
        public void ThenItRunsAFramesInstructionsAFrame()
        {
            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME, m_Field.CountAt(RAN));
        }

        [Test]
        public void ThenItCarriesOnTheNextFrameFromWhereItStopped()
        {
            m_Field.Tick(m_Entity);

            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME * 2, m_Field.CountAt(RAN));
        }
    }
}
