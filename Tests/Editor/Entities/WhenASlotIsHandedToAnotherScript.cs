using NUnit.Framework;

namespace RPGFramework.Field.Tests.Entities
{
    /// <summary>Handing over is the one case that replaces, as the script being replaced is the one asking.</summary>
    internal sealed class WhenASlotIsHandedToAnotherScript
    {
        private FieldEntityRuntime m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Entity = new FieldEntityRuntime(1, new[] { 100 }, new byte[1]);

            m_Entity.TryRequestScript(300, 5);
            m_Entity.ReplaceScriptInSlot(400, 5);
        }

        [Test]
        public void ThenTheSlotHoldsTheNewScript()
        {
            Assert.IsTrue(m_Entity.HoldsScript(400, 5));
        }
    }
}
