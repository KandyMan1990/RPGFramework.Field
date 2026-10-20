using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    internal sealed class WhenAScriptYields
    {
        private const ushort AFTER = 200;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Entity = m_Field.Run(new Script().Yield().Count(AFTER, 40).Return());
        }

        [Test]
        public void ThenNothingAfterItRunsThatFrame()
        {
            Assert.AreEqual(0, m_Field.CountAt(AFTER));
        }

        [Test]
        public void ThenTheScriptKeepsItsSlot()
        {
            Assert.IsTrue(m_Entity.IsSlotOccupied((byte)FieldScriptPriority.Main));
        }

        [Test]
        public void ThenWhatFollowsRunsOnTheVeryNextFrame()
        {
            m_Field.Tick(m_Entity);

            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME, m_Field.CountAt(AFTER));
        }
    }
}
