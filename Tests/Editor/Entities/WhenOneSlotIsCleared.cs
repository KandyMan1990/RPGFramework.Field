using NUnit.Framework;

namespace RPGFramework.Field.Tests.Entities
{
    internal sealed class WhenOneSlotIsCleared
    {
        private FieldEntityRuntime m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Entity = new FieldEntityRuntime(1, new[] { 100 }, new byte[1]);

            m_Entity.TryRequestScript(200, 3);
            m_Entity.ClearSlot((byte)FieldScriptPriority.Main);
        }

        [Test]
        public void ThenItIsEmpty()
        {
            Assert.IsFalse(m_Entity.IsSlotOccupied((byte)FieldScriptPriority.Main));
        }

        [Test]
        public void ThenTheOthersKeepTheirScripts()
        {
            Assert.IsTrue(m_Entity.HoldsScript(200, 3));
        }
    }
}
