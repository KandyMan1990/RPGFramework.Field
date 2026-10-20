using NUnit.Framework;

namespace RPGFramework.Field.Tests.Entities
{
    internal sealed class WhenAnEventIsResolvedToAScript
    {
        private FieldEntityRuntime m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Entity = new FieldEntityRuntime(1, new[] { 100, 101 }, new byte[2]);
        }

        [Test]
        public void ThenAnEventIsItsEntitysScript()
        {
            m_Entity.TryGetScriptId(1, out int scriptId);

            Assert.AreEqual(101, scriptId);
        }

        [TestCase(2)]
        [TestCase(-1)]
        public void ThenAnEventTheEntityDoesNotHaveIsRefused(int eventId)
        {
            Assert.IsFalse(m_Entity.TryGetScriptId(eventId, out _));
        }
    }
}
