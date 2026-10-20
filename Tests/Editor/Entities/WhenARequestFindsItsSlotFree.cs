using NUnit.Framework;

namespace RPGFramework.Field.Tests.Entities
{
    internal sealed class WhenARequestFindsItsSlotFree
    {
        private FieldEntityRuntime m_Entity;
        private bool               m_Accepted;

        [SetUp]
        public void SetUp()
        {
            m_Entity   = new FieldEntityRuntime(1, new[] { 100 }, new byte[1]);
            m_Accepted = m_Entity.TryRequestScript(200, 3);
        }

        [Test]
        public void ThenItIsAccepted()
        {
            Assert.IsTrue(m_Accepted);
        }

        [Test]
        public void ThenTheSlotHoldsIt()
        {
            Assert.IsTrue(m_Entity.HoldsScript(200, 3));
        }
    }
}
