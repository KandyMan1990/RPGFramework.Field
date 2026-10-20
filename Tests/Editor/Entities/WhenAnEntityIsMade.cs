using NUnit.Framework;

namespace RPGFramework.Field.Tests.Entities
{
    internal sealed class WhenAnEntityIsMade
    {
        private const int INIT = 100;

        private FieldEntityRuntime m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Entity = new FieldEntityRuntime(1, new[] { INIT, 101 }, new byte[2]);
        }

        [Test]
        public void ThenItsInitIsInMain()
        {
            Assert.IsTrue(m_Entity.HoldsScript(INIT, (byte)FieldScriptPriority.Main));
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(7)]
        public void ThenItsOtherSlotsAreEmpty(int priority)
        {
            Assert.IsFalse(m_Entity.IsSlotOccupied((byte)priority));
        }
    }
}
