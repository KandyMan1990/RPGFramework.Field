using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>
    /// A jump back that never waits can be authored. Were the frame's budget to go, this would not fail but hang the
    /// editor, as the field would.
    /// </summary>
    internal sealed class WhenAScriptLoopsWithoutWaiting
    {
        private const ushort LOOPS = 200;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();

            // A count is five bytes and the jump six, so the jump goes back eleven to the count.
            m_Field.Run(new Script().Count(LOOPS, 1).Goto(-11));
        }

        [Test]
        public void ThenItGivesTheFrameBackWhenItsInstructionsAreSpent()
        {
            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME / 2, m_Field.CountAt(LOOPS));
        }
    }
}
