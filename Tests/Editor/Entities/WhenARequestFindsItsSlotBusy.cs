using NUnit.Framework;

namespace RPGFramework.Field.Tests.Entities
{
    /// <summary>A request never replaces a script already running in its slot.</summary>
    internal sealed class WhenARequestFindsItsSlotBusy
    {
        private const int INIT = 100;

        private FieldEntityRuntime m_Entity;
        private bool               m_Accepted;

        [SetUp]
        public void SetUp()
        {
            m_Entity   = new FieldEntityRuntime(1, new[] { INIT }, new byte[1]);
            m_Accepted = m_Entity.TryRequestScript(200, (byte)FieldScriptPriority.Main);
        }

        [Test]
        public void ThenItIsRefused()
        {
            Assert.IsFalse(m_Accepted);
        }

        [Test]
        public void ThenTheSlotKeepsItsScript()
        {
            Assert.IsTrue(m_Entity.HoldsScript(INIT, (byte)FieldScriptPriority.Main));
        }
    }
}
